using AllowanceTracker.Data;
using AllowanceTracker.DTOs.Gifting;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class GiftService : IGiftService
{
    private readonly AllowanceContext _context;
    private readonly IGiftLinkService _giftLinkService;
    private readonly INotificationService? _notificationService;
    private readonly IEmailService? _emailService;

    public GiftService(
        AllowanceContext context,
        IGiftLinkService giftLinkService,
        INotificationService? notificationService = null,
        IEmailService? emailService = null)
    {
        _context = context;
        _giftLinkService = giftLinkService;
        _notificationService = notificationService;
        _emailService = emailService;
    }

    public async Task<GiftPortalDataDto> GetPortalDataAsync(string token)
    {
        var link = await _giftLinkService.ValidateTokenAsync(token);

        if (link == null)
        {
            throw new InvalidOperationException("Invalid or expired gift link.");
        }

        var child = await _context.Children
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == link.ChildId);

        if (child == null)
        {
            throw new InvalidOperationException("Child not found.");
        }

        List<PortalSavingsGoalDto>? savingsGoals = null;

        // Include savings goals if visibility allows
        if (link.Visibility == GiftLinkVisibility.WithGoals || link.Visibility == GiftLinkVisibility.Full)
        {
            var goals = await _context.SavingsGoals
                .Where(g => g.ChildId == child.Id && g.Status == GoalStatus.Active)
                .OrderByDescending(g => g.Priority)
                .ToListAsync();

            savingsGoals = goals.Select(g => new PortalSavingsGoalDto(
                g.Id,
                g.Name,
                g.TargetAmount,
                g.CurrentAmount,
                g.ProgressPercentage,
                g.ImageUrl
            )).ToList();
        }

        return new GiftPortalDataDto(
            child.User.FirstName,
            child.EquippedAvatarUrl,
            link.MinAmount,
            link.MaxAmount,
            link.DefaultOccasion,
            link.Visibility,
            savingsGoals
        );
    }

    public async Task<GiftSubmissionResultDto> SubmitGiftAsync(string token, SubmitGiftDto dto)
    {
        var link = await _giftLinkService.ValidateTokenAsync(token);

        if (link == null)
        {
            throw new InvalidOperationException("Invalid or expired gift link.");
        }

        // Validate amount against limits
        if (link.MinAmount.HasValue && dto.Amount < link.MinAmount.Value)
        {
            throw new InvalidOperationException($"Gift amount must be at least {link.MinAmount:C}. The minimum for this link is {link.MinAmount:C}.");
        }

        if (link.MaxAmount.HasValue && dto.Amount > link.MaxAmount.Value)
        {
            throw new InvalidOperationException($"Gift amount cannot exceed {link.MaxAmount:C}. The maximum for this link is {link.MaxAmount:C}.");
        }

        var child = await _context.Children
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == link.ChildId);

        if (child == null)
        {
            throw new InvalidOperationException("Child not found.");
        }

        var gift = new Gift
        {
            Id = Guid.NewGuid(),
            GiftLinkId = link.Id,
            ChildId = link.ChildId,
            GiverName = dto.GiverName,
            GiverEmail = dto.GiverEmail,
            GiverRelationship = dto.GiverRelationship,
            Amount = dto.Amount,
            Occasion = dto.Occasion,
            CustomOccasion = dto.CustomOccasion,
            Message = dto.Message,
            Status = GiftStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Gifts.Add(gift);
        await _context.SaveChangesAsync();

        // Increment usage count
        await _giftLinkService.IncrementUsageCountAsync(link.Id);

        // Send confirmation email to giver if email provided
        if (!string.IsNullOrEmpty(dto.GiverEmail) && _emailService != null)
        {
            try
            {
                await _emailService.SendGiftConfirmationEmailAsync(
                    dto.GiverEmail,
                    dto.GiverName,
                    child.User.FirstName,
                    dto.Amount);
            }
            catch
            {
                // Don't fail the submission if email fails
            }
        }

        // Notify parents about the new gift
        if (_notificationService != null)
        {
            try
            {
                await _notificationService.SendFamilyNotificationAsync(
                    link.FamilyId,
                    NotificationType.GiftReceived,
                    "New Gift Received!",
                    $"{dto.GiverName} sent a gift of {dto.Amount:C} to {child.User.FirstName}. Awaiting your approval.",
                    excludeUserId: child.UserId);
            }
            catch
            {
                // Don't fail if notification fails
            }
        }

        return new GiftSubmissionResultDto(
            gift.Id,
            child.User.FirstName,
            dto.Amount,
            $"Thank you for your gift to {child.User.FirstName}! The parents will be notified and the gift will be added once approved."
        );
    }

    public async Task<List<GiftDto>> GetPendingGiftsAsync(Guid familyId)
    {
        var gifts = await _context.Gifts
            .Include(g => g.Child)
                .ThenInclude(c => c.User)
            .Include(g => g.AllocateToGoal)
            .Where(g => g.GiftLink.FamilyId == familyId && g.Status == GiftStatus.Pending)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return gifts.Select(ToDto).ToList();
    }

    public async Task<List<GiftDto>> GetChildGiftsAsync(Guid childId)
    {
        var gifts = await _context.Gifts
            .Include(g => g.Child)
                .ThenInclude(c => c.User)
            .Include(g => g.AllocateToGoal)
            .Include(g => g.ThankYouNote)
            .Where(g => g.ChildId == childId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return gifts.Select(ToDto).ToList();
    }

    public async Task<GiftDto?> GetGiftByIdAsync(Guid giftId)
    {
        var gift = await _context.Gifts
            .Include(g => g.Child)
                .ThenInclude(c => c.User)
            .Include(g => g.AllocateToGoal)
            .Include(g => g.ThankYouNote)
            .FirstOrDefaultAsync(g => g.Id == giftId);

        return gift == null ? null : ToDto(gift);
    }

    public async Task<GiftDto> ApproveGiftAsync(Guid giftId, ApproveGiftDto dto, Guid approvedById)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var gift = await _context.Gifts
                .Include(g => g.Child)
                    .ThenInclude(c => c.User)
                .Include(g => g.GiftLink)
                .FirstOrDefaultAsync(g => g.Id == giftId);

            if (gift == null)
            {
                throw new InvalidOperationException("Gift not found.");
            }

            if (gift.Status != GiftStatus.Pending)
            {
                throw new InvalidOperationException("Gift has already been processed.");
            }

            var child = gift.Child;
            decimal spendingAmount = gift.Amount;
            decimal savingsAmount = 0;
            SavingsGoal? goal = null;

            // Handle allocation to savings goal
            if (dto.AllocateToGoalId.HasValue)
            {
                goal = await _context.SavingsGoals.FindAsync(dto.AllocateToGoalId.Value);
                if (goal != null && goal.ChildId == child.Id)
                {
                    goal.CurrentAmount += gift.Amount;
                    spendingAmount = 0; // All goes to goal
                }
                gift.AllocateToGoalId = dto.AllocateToGoalId;
            }
            // Handle allocation by percentage to general savings
            else if (dto.SavingsPercentage.HasValue && dto.SavingsPercentage > 0)
            {
                savingsAmount = gift.Amount * dto.SavingsPercentage.Value / 100m;
                spendingAmount = gift.Amount - savingsAmount;
                child.SavingsBalance += savingsAmount;
                gift.SavingsPercentage = dto.SavingsPercentage;
            }

            // Add to spending balance (unless all went to goal)
            child.CurrentBalance += spendingAmount;

            // Create transaction record
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                ChildId = child.Id,
                Amount = gift.Amount,
                Type = TransactionType.Credit,
                Category = TransactionCategory.Gift,
                Description = $"Gift from {gift.GiverName}" + (gift.Occasion != GiftOccasion.JustBecause ? $" ({gift.Occasion})" : ""),
                BalanceAfter = child.CurrentBalance,
                CreatedById = approvedById,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);

            // Update gift status
            gift.Status = GiftStatus.Approved;
            gift.ProcessedById = approvedById;
            gift.ProcessedAt = DateTime.UtcNow;
            gift.TransactionId = transaction.Id;

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            // Send notification to child
            if (_notificationService != null)
            {
                try
                {
                    await _notificationService.SendNotificationAsync(
                        child.UserId,
                        NotificationType.GiftReceived,
                        "You received a gift!",
                        $"{gift.GiverName} sent you {gift.Amount:C}!",
                        new { giftId = gift.Id, amount = gift.Amount },
                        gift.Id,
                        "Gift");
                }
                catch
                {
                    // Don't fail if notification fails
                }
            }

            // Reload with includes for DTO
            await _context.Entry(gift).Reference(g => g.AllocateToGoal).LoadAsync();

            return ToDto(gift);
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<GiftDto> RejectGiftAsync(Guid giftId, RejectGiftDto dto, Guid rejectedById)
    {
        var gift = await _context.Gifts
            .Include(g => g.Child)
                .ThenInclude(c => c.User)
            .Include(g => g.AllocateToGoal)
            .FirstOrDefaultAsync(g => g.Id == giftId);

        if (gift == null)
        {
            throw new InvalidOperationException("Gift not found.");
        }

        if (gift.Status != GiftStatus.Pending)
        {
            throw new InvalidOperationException("Gift has already been processed.");
        }

        gift.Status = GiftStatus.Rejected;
        gift.RejectionReason = dto.Reason;
        gift.ProcessedById = rejectedById;
        gift.ProcessedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ToDto(gift);
    }

    public async Task<int> ExpireOldPendingGiftsAsync(int daysOld = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

        var oldGifts = await _context.Gifts
            .Where(g => g.Status == GiftStatus.Pending && g.CreatedAt < cutoffDate)
            .ToListAsync();

        foreach (var gift in oldGifts)
        {
            gift.Status = GiftStatus.Expired;
            gift.ProcessedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return oldGifts.Count;
    }

    private static GiftDto ToDto(Gift gift)
    {
        return new GiftDto(
            gift.Id,
            gift.ChildId,
            gift.Child.User.FirstName,
            gift.GiverName,
            gift.GiverEmail,
            gift.GiverRelationship,
            gift.Amount,
            gift.Occasion,
            gift.CustomOccasion,
            gift.Message,
            gift.Status,
            gift.RejectionReason,
            gift.ProcessedById,
            gift.ProcessedAt,
            gift.AllocateToGoalId,
            gift.AllocateToGoal?.Name,
            gift.SavingsPercentage,
            gift.CreatedAt,
            gift.ThankYouNote != null
        );
    }
}
