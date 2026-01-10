# Spending Approval Workflow Specification

## Overview

The Spending Approval Workflow provides parents with granular control over children's spending while creating teachable moments. Parents can set approval thresholds, category restrictions, and spending limits. Children submit spending requests that parents can approve or deny with optional feedback.

### Goals
- Give parents appropriate oversight without micromanaging
- Teach children about thoughtful spending decisions
- Create natural opportunities for financial conversations
- Support gradual trust-building with adjustable thresholds
- Provide clear visibility into pending and past requests

### Scope
- Per-child approval settings with customizable thresholds
- Category-based restrictions and limits
- Spending request submission and approval queue
- Parent feedback as "learning moments"
- Spending limit tracking (daily/weekly/monthly)
- Auto-approval rules for trusted spending patterns

---

## User Stories

### Parent Stories
1. As a parent, I want to set approval thresholds so purchases above a certain amount require my approval
2. As a parent, I want to restrict certain spending categories so my child can't spend on inappropriate items
3. As a parent, I want to set daily/weekly/monthly spending limits to teach budgeting
4. As a parent, I want to see all pending approval requests in one place
5. As a parent, I want to approve or deny requests with a comment explaining my decision
6. As a parent, I want to see my child's spending request history
7. As a parent, I want to configure auto-approval for trusted categories/amounts
8. As a parent, I want to receive notifications when approval is needed
9. As a parent, I want to set different rules for different children based on age/maturity
10. As a parent, I want to temporarily pause all spending for a child if needed

### Child Stories
1. As a child, I want to submit a spending request with details about what I want to buy
2. As a child, I want to see the status of my pending requests
3. As a child, I want to understand why a request was denied
4. As a child, I want to know my remaining spending limits
5. As a child, I want small purchases to go through without waiting for approval
6. As a child, I want to cancel a pending request if I change my mind
7. As a child, I want to see my spending history with parent feedback

---

## Database Schema

### Enums

```csharp
public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Denied = 2,
    Cancelled = 3,
    Expired = 4
}

public enum SpendingLimitPeriod
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2
}

public enum CategoryRestriction
{
    Allowed = 0,         // No restrictions
    RequiresApproval = 1, // Always needs approval regardless of amount
    Blocked = 2          // Cannot spend in this category at all
}

public enum AutoApprovalRule
{
    None = 0,
    BelowThreshold = 1,
    TrustedCategories = 2,
    RecurringPurchases = 3
}
```

### Models

```csharp
/// <summary>
/// Per-child approval settings configured by parent
/// </summary>
public class ApprovalSettings
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    // Global settings
    public bool IsEnabled { get; set; } = true;
    public bool IsPaused { get; set; } = false; // Emergency pause all spending
    public string? PauseReason { get; set; }

    // Threshold settings
    public decimal ApprovalThreshold { get; set; } = 10.00m; // Require approval above this
    public decimal? MaxSinglePurchase { get; set; } // Hard limit per transaction

    // Auto-approval settings
    public bool AutoApproveUnderThreshold { get; set; } = true;
    public bool AutoApproveTrustedCategories { get; set; } = false;
    public List<Guid> TrustedCategoryIds { get; set; } = new();

    // Request expiration
    public int RequestExpirationHours { get; set; } = 72; // Expire after 3 days

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid UpdatedById { get; set; }

    // Navigation
    public virtual ICollection<CategoryApprovalRule> CategoryRules { get; set; } = new List<CategoryApprovalRule>();
    public virtual ICollection<SpendingLimit> SpendingLimits { get; set; } = new List<SpendingLimit>();
}

/// <summary>
/// Category-specific approval rules
/// </summary>
public class CategoryApprovalRule
{
    public Guid Id { get; set; }
    public Guid ApprovalSettingsId { get; set; }
    public virtual ApprovalSettings ApprovalSettings { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public virtual TransactionCategory Category { get; set; } = null!;

    public CategoryRestriction Restriction { get; set; }
    public decimal? CategoryThreshold { get; set; } // Override global threshold for this category
    public string? RestrictionReason { get; set; } // Explain to child why restricted

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Spending limits by time period
/// </summary>
public class SpendingLimit
{
    public Guid Id { get; set; }
    public Guid ApprovalSettingsId { get; set; }
    public virtual ApprovalSettings ApprovalSettings { get; set; } = null!;

    public SpendingLimitPeriod Period { get; set; }
    public decimal LimitAmount { get; set; }
    public bool IncludesPendingRequests { get; set; } = true; // Count pending toward limit

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Child's spending request awaiting parent approval
/// </summary>
public class SpendingRequest
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    // Request details
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ItemName { get; set; }
    public string? StoreName { get; set; }
    public string? Reason { get; set; } // Why they want it
    public Guid? CategoryId { get; set; }
    public virtual TransactionCategory? Category { get; set; }

    // Optional: Link to wish list item
    public Guid? WishListItemId { get; set; }
    public virtual WishListItem? WishListItem { get; set; }

    // Status
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public DateTime? ExpiresAt { get; set; }

    // Parent response
    public Guid? RespondedById { get; set; }
    public virtual ApplicationUser? RespondedBy { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? ParentComment { get; set; } // Learning moment / feedback
    public bool IsLearningMoment { get; set; } = false; // Flag important feedback

    // If approved, the resulting transaction
    public Guid? TransactionId { get; set; }
    public virtual Transaction? Transaction { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Tracks spending against limits per period
/// </summary>
public class SpendingLimitTracker
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    public SpendingLimitPeriod Period { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public decimal LimitAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal PendingAmount { get; set; } // From pending requests

    public decimal RemainingAmount => LimitAmount - SpentAmount - PendingAmount;
    public decimal PercentUsed => LimitAmount > 0 ? (SpentAmount + PendingAmount) / LimitAmount * 100 : 0;

    public DateTime UpdatedAt { get; set; }
}
```

### Entity Configuration

```csharp
public class ApprovalSettingsConfiguration : IEntityTypeConfiguration<ApprovalSettings>
{
    public void Configure(EntityTypeBuilder<ApprovalSettings> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.Child)
            .WithOne()
            .HasForeignKey<ApprovalSettings>(a => a.ChildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.ApprovalThreshold)
            .HasPrecision(18, 2);

        builder.Property(a => a.MaxSinglePurchase)
            .HasPrecision(18, 2);

        builder.Property(a => a.TrustedCategoryIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
            );

        builder.HasIndex(a => a.ChildId).IsUnique();
    }
}

public class SpendingRequestConfiguration : IEntityTypeConfiguration<SpendingRequest>
{
    public void Configure(EntityTypeBuilder<SpendingRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.Child)
            .WithMany()
            .HasForeignKey(r => r.ChildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Category)
            .WithMany()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.WishListItem)
            .WithMany()
            .HasForeignKey(r => r.WishListItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.Transaction)
            .WithMany()
            .HasForeignKey(r => r.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(r => r.Amount)
            .HasPrecision(18, 2);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.HasIndex(r => new { r.ChildId, r.Status });
        builder.HasIndex(r => r.ExpiresAt);
    }
}

public class SpendingLimitTrackerConfiguration : IEntityTypeConfiguration<SpendingLimitTracker>
{
    public void Configure(EntityTypeBuilder<SpendingLimitTracker> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasOne(t => t.Child)
            .WithMany()
            .HasForeignKey(t => t.ChildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.LimitAmount)
            .HasPrecision(18, 2);

        builder.Property(t => t.SpentAmount)
            .HasPrecision(18, 2);

        builder.Property(t => t.PendingAmount)
            .HasPrecision(18, 2);

        builder.Ignore(t => t.RemainingAmount);
        builder.Ignore(t => t.PercentUsed);

        builder.HasIndex(t => new { t.ChildId, t.Period, t.PeriodStart }).IsUnique();
    }
}
```

---

## DTOs

### Request DTOs

```csharp
public record UpdateApprovalSettingsDto(
    bool IsEnabled,
    bool IsPaused,
    string? PauseReason,
    decimal ApprovalThreshold,
    decimal? MaxSinglePurchase,
    bool AutoApproveUnderThreshold,
    bool AutoApproveTrustedCategories,
    List<Guid>? TrustedCategoryIds,
    int RequestExpirationHours
);

public record SetCategoryRuleDto(
    Guid CategoryId,
    CategoryRestriction Restriction,
    decimal? CategoryThreshold,
    string? RestrictionReason
);

public record SetSpendingLimitDto(
    SpendingLimitPeriod Period,
    decimal LimitAmount,
    bool IncludesPendingRequests
);

public record CreateSpendingRequestDto(
    decimal Amount,
    string Description,
    string? ItemName,
    string? StoreName,
    string? Reason,
    Guid? CategoryId,
    Guid? WishListItemId
);

public record RespondToRequestDto(
    bool Approved,
    string? Comment,
    bool IsLearningMoment
);

public record BulkRespondDto(
    List<Guid> RequestIds,
    bool Approved,
    string? Comment
);
```

### Response DTOs

```csharp
public record ApprovalSettingsDto(
    Guid Id,
    Guid ChildId,
    bool IsEnabled,
    bool IsPaused,
    string? PauseReason,
    decimal ApprovalThreshold,
    decimal? MaxSinglePurchase,
    bool AutoApproveUnderThreshold,
    bool AutoApproveTrustedCategories,
    List<Guid> TrustedCategoryIds,
    int RequestExpirationHours,
    List<CategoryApprovalRuleDto> CategoryRules,
    List<SpendingLimitDto> SpendingLimits,
    DateTime UpdatedAt
);

public record CategoryApprovalRuleDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    CategoryRestriction Restriction,
    decimal? CategoryThreshold,
    string? RestrictionReason
);

public record SpendingLimitDto(
    Guid Id,
    SpendingLimitPeriod Period,
    decimal LimitAmount,
    bool IncludesPendingRequests
);

public record SpendingRequestDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    decimal Amount,
    string Description,
    string? ItemName,
    string? StoreName,
    string? Reason,
    Guid? CategoryId,
    string? CategoryName,
    string? CategoryIcon,
    Guid? WishListItemId,
    string? WishListItemName,
    ApprovalStatus Status,
    DateTime? ExpiresAt,
    string? RespondedByName,
    DateTime? RespondedAt,
    string? ParentComment,
    bool IsLearningMoment,
    DateTime CreatedAt
);

public record SpendingRequestSummaryDto(
    int PendingCount,
    int ApprovedToday,
    int DeniedToday,
    decimal PendingTotal,
    List<SpendingRequestDto> RecentPending
);

public record SpendingLimitStatusDto(
    SpendingLimitPeriod Period,
    decimal LimitAmount,
    decimal SpentAmount,
    decimal PendingAmount,
    decimal RemainingAmount,
    decimal PercentUsed,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    bool IsNearLimit, // > 80%
    bool IsAtLimit    // >= 100%
);

public record ChildSpendingStatusDto(
    Guid ChildId,
    string ChildName,
    bool ApprovalRequired,
    decimal ApprovalThreshold,
    bool IsPaused,
    string? PauseReason,
    List<SpendingLimitStatusDto> LimitStatuses,
    int PendingRequestCount,
    decimal PendingRequestTotal
);

public record SpendingCheckResultDto(
    bool CanSpend,
    bool RequiresApproval,
    string? BlockReason,
    List<string> Warnings
);

public record LearningMomentDto(
    Guid RequestId,
    string ItemDescription,
    decimal Amount,
    string ParentComment,
    bool WasApproved,
    DateTime Date
);
```

---

## Service Layer

### Interface

```csharp
public interface ISpendingApprovalService
{
    // Settings management (Parent)
    Task<ApprovalSettingsDto> GetSettingsAsync(Guid childId);
    Task<ApprovalSettingsDto> UpdateSettingsAsync(Guid childId, UpdateApprovalSettingsDto dto, Guid parentId);
    Task<ApprovalSettingsDto> SetCategoryRuleAsync(Guid childId, SetCategoryRuleDto dto, Guid parentId);
    Task RemoveCategoryRuleAsync(Guid childId, Guid categoryId, Guid parentId);
    Task<ApprovalSettingsDto> SetSpendingLimitAsync(Guid childId, SetSpendingLimitDto dto, Guid parentId);
    Task RemoveSpendingLimitAsync(Guid childId, SpendingLimitPeriod period, Guid parentId);
    Task<ApprovalSettingsDto> PauseSpendingAsync(Guid childId, string reason, Guid parentId);
    Task<ApprovalSettingsDto> ResumeSpendingAsync(Guid childId, Guid parentId);

    // Spending check (Child/System)
    Task<SpendingCheckResultDto> CheckSpendingAsync(Guid childId, decimal amount, Guid? categoryId);
    Task<ChildSpendingStatusDto> GetSpendingStatusAsync(Guid childId);
    Task<List<SpendingLimitStatusDto>> GetLimitStatusesAsync(Guid childId);

    // Request management (Child)
    Task<SpendingRequestDto> CreateRequestAsync(Guid childId, CreateSpendingRequestDto dto);
    Task<SpendingRequestDto> GetRequestAsync(Guid requestId);
    Task<List<SpendingRequestDto>> GetChildRequestsAsync(Guid childId, ApprovalStatus? status = null);
    Task<SpendingRequestDto> CancelRequestAsync(Guid requestId, Guid childId);

    // Request management (Parent)
    Task<SpendingRequestSummaryDto> GetPendingRequestsAsync(Guid familyId);
    Task<List<SpendingRequestDto>> GetFamilyRequestsAsync(Guid familyId, ApprovalStatus? status = null, int limit = 50);
    Task<SpendingRequestDto> RespondToRequestAsync(Guid requestId, RespondToRequestDto dto, Guid parentId);
    Task<List<SpendingRequestDto>> BulkRespondAsync(BulkRespondDto dto, Guid parentId);

    // Learning moments
    Task<List<LearningMomentDto>> GetLearningMomentsAsync(Guid childId, int limit = 10);

    // Background processing
    Task ExpireOldRequestsAsync();
    Task ResetPeriodTrackersAsync();
}
```

### Implementation

```csharp
public class SpendingApprovalService : ISpendingApprovalService
{
    private readonly AllowanceContext _context;
    private readonly ITransactionService _transactionService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SpendingApprovalService> _logger;

    public SpendingApprovalService(
        AllowanceContext context,
        ITransactionService transactionService,
        INotificationService notificationService,
        ILogger<SpendingApprovalService> logger)
    {
        _context = context;
        _transactionService = transactionService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ApprovalSettingsDto> GetSettingsAsync(Guid childId)
    {
        var settings = await _context.ApprovalSettings
            .Include(s => s.CategoryRules)
                .ThenInclude(r => r.Category)
            .Include(s => s.SpendingLimits)
            .FirstOrDefaultAsync(s => s.ChildId == childId);

        if (settings == null)
        {
            // Create default settings
            settings = new ApprovalSettings
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ApprovalSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return MapToDto(settings);
    }

    public async Task<SpendingCheckResultDto> CheckSpendingAsync(
        Guid childId,
        decimal amount,
        Guid? categoryId)
    {
        var settings = await _context.ApprovalSettings
            .Include(s => s.CategoryRules)
            .Include(s => s.SpendingLimits)
            .FirstOrDefaultAsync(s => s.ChildId == childId);

        var warnings = new List<string>();

        // No settings = no restrictions
        if (settings == null || !settings.IsEnabled)
        {
            return new SpendingCheckResultDto(true, false, null, warnings);
        }

        // Check if paused
        if (settings.IsPaused)
        {
            return new SpendingCheckResultDto(
                false,
                false,
                settings.PauseReason ?? "Spending is currently paused",
                warnings);
        }

        // Check max single purchase
        if (settings.MaxSinglePurchase.HasValue && amount > settings.MaxSinglePurchase.Value)
        {
            return new SpendingCheckResultDto(
                false,
                false,
                $"Amount exceeds maximum purchase limit of {settings.MaxSinglePurchase:C}",
                warnings);
        }

        // Check category restrictions
        if (categoryId.HasValue)
        {
            var categoryRule = settings.CategoryRules
                .FirstOrDefault(r => r.CategoryId == categoryId.Value);

            if (categoryRule != null)
            {
                if (categoryRule.Restriction == CategoryRestriction.Blocked)
                {
                    return new SpendingCheckResultDto(
                        false,
                        false,
                        categoryRule.RestrictionReason ?? "This category is not allowed",
                        warnings);
                }

                if (categoryRule.Restriction == CategoryRestriction.RequiresApproval)
                {
                    return new SpendingCheckResultDto(true, true, null, warnings);
                }

                // Category-specific threshold
                if (categoryRule.CategoryThreshold.HasValue &&
                    amount > categoryRule.CategoryThreshold.Value)
                {
                    return new SpendingCheckResultDto(true, true, null, warnings);
                }
            }
        }

        // Check spending limits
        var limitStatuses = await GetLimitStatusesInternalAsync(childId, settings);
        foreach (var limitStatus in limitStatuses)
        {
            var projectedSpent = limitStatus.SpentAmount + limitStatus.PendingAmount + amount;
            if (projectedSpent > limitStatus.LimitAmount)
            {
                return new SpendingCheckResultDto(
                    false,
                    false,
                    $"This would exceed your {limitStatus.Period.ToString().ToLower()} spending limit",
                    warnings);
            }

            if (limitStatus.PercentUsed > 80)
            {
                warnings.Add($"You've used {limitStatus.PercentUsed:F0}% of your {limitStatus.Period.ToString().ToLower()} limit");
            }
        }

        // Check if requires approval based on threshold
        bool requiresApproval = amount > settings.ApprovalThreshold;

        // Auto-approve under threshold
        if (!requiresApproval && settings.AutoApproveUnderThreshold)
        {
            return new SpendingCheckResultDto(true, false, null, warnings);
        }

        // Auto-approve trusted categories
        if (requiresApproval &&
            settings.AutoApproveTrustedCategories &&
            categoryId.HasValue &&
            settings.TrustedCategoryIds.Contains(categoryId.Value))
        {
            return new SpendingCheckResultDto(true, false, null, warnings);
        }

        return new SpendingCheckResultDto(true, requiresApproval, null, warnings);
    }

    public async Task<SpendingRequestDto> CreateRequestAsync(
        Guid childId,
        CreateSpendingRequestDto dto)
    {
        // Validate spending first
        var check = await CheckSpendingAsync(childId, dto.Amount, dto.CategoryId);
        if (!check.CanSpend)
        {
            throw new InvalidOperationException(check.BlockReason);
        }

        if (!check.RequiresApproval)
        {
            throw new InvalidOperationException("This purchase does not require approval");
        }

        var settings = await _context.ApprovalSettings
            .FirstAsync(s => s.ChildId == childId);

        var request = new SpendingRequest
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            Amount = dto.Amount,
            Description = dto.Description,
            ItemName = dto.ItemName,
            StoreName = dto.StoreName,
            Reason = dto.Reason,
            CategoryId = dto.CategoryId,
            WishListItemId = dto.WishListItemId,
            Status = ApprovalStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(settings.RequestExpirationHours),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SpendingRequests.Add(request);

        // Update pending amount in limit tracker
        await UpdatePendingAmountAsync(childId, dto.Amount);

        await _context.SaveChangesAsync();

        // Notify parents
        var child = await _context.Children
            .Include(c => c.Family)
            .FirstAsync(c => c.Id == childId);

        await _notificationService.NotifyFamilyAsync(
            child.FamilyId,
            NotificationType.ApprovalNeeded,
            $"{child.Name} is requesting approval for {dto.Amount:C}",
            new { RequestId = request.Id, ChildId = childId, Amount = dto.Amount });

        return await GetRequestAsync(request.Id);
    }

    public async Task<SpendingRequestDto> RespondToRequestAsync(
        Guid requestId,
        RespondToRequestDto dto,
        Guid parentId)
    {
        var request = await _context.SpendingRequests
            .Include(r => r.Child)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new KeyNotFoundException("Request not found");

        if (request.Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException("Request is no longer pending");
        }

        request.Status = dto.Approved ? ApprovalStatus.Approved : ApprovalStatus.Denied;
        request.RespondedById = parentId;
        request.RespondedAt = DateTime.UtcNow;
        request.ParentComment = dto.Comment;
        request.IsLearningMoment = dto.IsLearningMoment;
        request.UpdatedAt = DateTime.UtcNow;

        // Update pending amount (remove from pending)
        await UpdatePendingAmountAsync(request.ChildId, -request.Amount);

        if (dto.Approved)
        {
            // Create the transaction
            var transaction = await _transactionService.CreateTransactionAsync(
                new CreateTransactionDto(
                    request.ChildId,
                    request.Amount,
                    TransactionType.Debit,
                    request.Description,
                    request.CategoryId,
                    null,
                    parentId
                ));

            request.TransactionId = transaction.Id;

            // Update spent amount in limit tracker
            await UpdateSpentAmountAsync(request.ChildId, request.Amount);
        }

        await _context.SaveChangesAsync();

        // Notify child
        var parent = await _context.Users.FindAsync(parentId);
        var notificationType = dto.Approved
            ? NotificationType.RequestApproved
            : NotificationType.RequestDenied;

        await _notificationService.NotifyUserAsync(
            request.Child.UserId,
            notificationType,
            dto.Approved
                ? $"Your request for {request.Amount:C} was approved!"
                : $"Your request for {request.Amount:C} was denied",
            new {
                RequestId = request.Id,
                Amount = request.Amount,
                Comment = dto.Comment,
                IsLearningMoment = dto.IsLearningMoment
            });

        return await GetRequestAsync(request.Id);
    }

    public async Task<SpendingRequestSummaryDto> GetPendingRequestsAsync(Guid familyId)
    {
        var childIds = await _context.Children
            .Where(c => c.FamilyId == familyId)
            .Select(c => c.Id)
            .ToListAsync();

        var pendingRequests = await _context.SpendingRequests
            .Include(r => r.Child)
            .Include(r => r.Category)
            .Where(r => childIds.Contains(r.ChildId) && r.Status == ApprovalStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var todayRequests = await _context.SpendingRequests
            .Where(r => childIds.Contains(r.ChildId) && r.RespondedAt >= today)
            .ToListAsync();

        return new SpendingRequestSummaryDto(
            pendingRequests.Count,
            todayRequests.Count(r => r.Status == ApprovalStatus.Approved),
            todayRequests.Count(r => r.Status == ApprovalStatus.Denied),
            pendingRequests.Sum(r => r.Amount),
            pendingRequests.Take(10).Select(MapToDto).ToList()
        );
    }

    public async Task<List<LearningMomentDto>> GetLearningMomentsAsync(Guid childId, int limit = 10)
    {
        return await _context.SpendingRequests
            .Where(r => r.ChildId == childId &&
                        r.IsLearningMoment &&
                        r.ParentComment != null)
            .OrderByDescending(r => r.RespondedAt)
            .Take(limit)
            .Select(r => new LearningMomentDto(
                r.Id,
                r.Description,
                r.Amount,
                r.ParentComment!,
                r.Status == ApprovalStatus.Approved,
                r.RespondedAt!.Value
            ))
            .ToListAsync();
    }

    public async Task ExpireOldRequestsAsync()
    {
        var expiredRequests = await _context.SpendingRequests
            .Where(r => r.Status == ApprovalStatus.Pending &&
                        r.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var request in expiredRequests)
        {
            request.Status = ApprovalStatus.Expired;
            request.UpdatedAt = DateTime.UtcNow;

            // Remove from pending amounts
            await UpdatePendingAmountAsync(request.ChildId, -request.Amount);

            // Notify child
            var child = await _context.Children.FindAsync(request.ChildId);
            if (child != null)
            {
                await _notificationService.NotifyUserAsync(
                    child.UserId,
                    NotificationType.RequestExpired,
                    $"Your request for {request.Amount:C} has expired",
                    new { RequestId = request.Id });
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Expired {Count} spending requests", expiredRequests.Count);
    }

    private async Task UpdatePendingAmountAsync(Guid childId, decimal amount)
    {
        var trackers = await _context.SpendingLimitTrackers
            .Where(t => t.ChildId == childId && t.PeriodEnd > DateTime.UtcNow)
            .ToListAsync();

        foreach (var tracker in trackers)
        {
            tracker.PendingAmount += amount;
            if (tracker.PendingAmount < 0) tracker.PendingAmount = 0;
            tracker.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task UpdateSpentAmountAsync(Guid childId, decimal amount)
    {
        var trackers = await _context.SpendingLimitTrackers
            .Where(t => t.ChildId == childId && t.PeriodEnd > DateTime.UtcNow)
            .ToListAsync();

        foreach (var tracker in trackers)
        {
            tracker.SpentAmount += amount;
            tracker.UpdatedAt = DateTime.UtcNow;
        }
    }

    private SpendingRequestDto MapToDto(SpendingRequest request)
    {
        return new SpendingRequestDto(
            request.Id,
            request.ChildId,
            request.Child?.Name ?? "",
            request.Amount,
            request.Description,
            request.ItemName,
            request.StoreName,
            request.Reason,
            request.CategoryId,
            request.Category?.Name,
            request.Category?.Icon,
            request.WishListItemId,
            request.WishListItem?.Name,
            request.Status,
            request.ExpiresAt,
            request.RespondedBy?.Email,
            request.RespondedAt,
            request.ParentComment,
            request.IsLearningMoment,
            request.CreatedAt
        );
    }

    // Additional methods implementation...
}
```

### Background Job

```csharp
public class SpendingApprovalBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SpendingApprovalBackgroundService> _logger;

    public SpendingApprovalBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SpendingApprovalBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider
                    .GetRequiredService<ISpendingApprovalService>();

                // Expire old requests
                await service.ExpireOldRequestsAsync();

                // Reset period trackers at midnight
                var now = DateTime.UtcNow;
                if (now.Hour == 0 && now.Minute < 5)
                {
                    await service.ResetPeriodTrackersAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in spending approval background service");
            }

            // Run every 5 minutes
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

---

## API Endpoints

### Approval Settings (Parent Only)

```
GET    /api/v1/children/{childId}/approval-settings
       → ApprovalSettingsDto
       Get approval settings for a child

PATCH  /api/v1/children/{childId}/approval-settings
       Body: UpdateApprovalSettingsDto
       → ApprovalSettingsDto
       Update approval settings

POST   /api/v1/children/{childId}/approval-settings/category-rules
       Body: SetCategoryRuleDto
       → ApprovalSettingsDto
       Add or update category rule

DELETE /api/v1/children/{childId}/approval-settings/category-rules/{categoryId}
       → 204 No Content
       Remove category rule

POST   /api/v1/children/{childId}/approval-settings/spending-limits
       Body: SetSpendingLimitDto
       → ApprovalSettingsDto
       Add or update spending limit

DELETE /api/v1/children/{childId}/approval-settings/spending-limits/{period}
       → 204 No Content
       Remove spending limit

POST   /api/v1/children/{childId}/approval-settings/pause
       Body: { "reason": "string" }
       → ApprovalSettingsDto
       Pause all spending

POST   /api/v1/children/{childId}/approval-settings/resume
       → ApprovalSettingsDto
       Resume spending
```

### Spending Status

```
GET    /api/v1/children/{childId}/spending-status
       → ChildSpendingStatusDto
       Get child's spending status and limits

GET    /api/v1/children/{childId}/spending-check
       Query: amount, categoryId
       → SpendingCheckResultDto
       Check if a purchase is allowed

GET    /api/v1/children/{childId}/limit-statuses
       → List<SpendingLimitStatusDto>
       Get current limit statuses
```

### Spending Requests

```
POST   /api/v1/spending-requests
       Body: CreateSpendingRequestDto + childId
       → SpendingRequestDto
       Create a new spending request (Child)

GET    /api/v1/spending-requests/{requestId}
       → SpendingRequestDto
       Get request details

GET    /api/v1/children/{childId}/spending-requests
       Query: status
       → List<SpendingRequestDto>
       Get child's requests

DELETE /api/v1/spending-requests/{requestId}
       → 204 No Content
       Cancel a pending request (Child)

GET    /api/v1/families/{familyId}/spending-requests/pending
       → SpendingRequestSummaryDto
       Get pending requests for family (Parent)

GET    /api/v1/families/{familyId}/spending-requests
       Query: status, limit
       → List<SpendingRequestDto>
       Get all family requests (Parent)

POST   /api/v1/spending-requests/{requestId}/respond
       Body: RespondToRequestDto
       → SpendingRequestDto
       Approve or deny request (Parent)

POST   /api/v1/spending-requests/bulk-respond
       Body: BulkRespondDto
       → List<SpendingRequestDto>
       Bulk approve/deny requests (Parent)
```

### Learning Moments

```
GET    /api/v1/children/{childId}/learning-moments
       Query: limit
       → List<LearningMomentDto>
       Get learning moments for child
```

---

## iOS Implementation

### Models

```swift
enum ApprovalStatus: String, Codable {
    case pending, approved, denied, cancelled, expired
}

enum SpendingLimitPeriod: String, Codable {
    case daily, weekly, monthly
}

enum CategoryRestriction: String, Codable {
    case allowed, requiresApproval, blocked
}

struct ApprovalSettings: Codable, Identifiable {
    let id: UUID
    let childId: UUID
    var isEnabled: Bool
    var isPaused: Bool
    var pauseReason: String?
    var approvalThreshold: Decimal
    var maxSinglePurchase: Decimal?
    var autoApproveUnderThreshold: Bool
    var autoApproveTrustedCategories: Bool
    var trustedCategoryIds: [UUID]
    var requestExpirationHours: Int
    var categoryRules: [CategoryApprovalRule]
    var spendingLimits: [SpendingLimit]
    let updatedAt: Date
}

struct CategoryApprovalRule: Codable, Identifiable {
    let id: UUID
    let categoryId: UUID
    let categoryName: String
    let categoryIcon: String
    var restriction: CategoryRestriction
    var categoryThreshold: Decimal?
    var restrictionReason: String?
}

struct SpendingLimit: Codable, Identifiable {
    let id: UUID
    var period: SpendingLimitPeriod
    var limitAmount: Decimal
    var includesPendingRequests: Bool
}

struct SpendingRequest: Codable, Identifiable {
    let id: UUID
    let childId: UUID
    let childName: String
    let amount: Decimal
    let description: String
    let itemName: String?
    let storeName: String?
    let reason: String?
    let categoryId: UUID?
    let categoryName: String?
    let categoryIcon: String?
    let wishListItemId: UUID?
    let wishListItemName: String?
    var status: ApprovalStatus
    let expiresAt: Date?
    let respondedByName: String?
    let respondedAt: Date?
    let parentComment: String?
    let isLearningMoment: Bool
    let createdAt: Date

    var isExpiringSoon: Bool {
        guard let expires = expiresAt else { return false }
        return expires.timeIntervalSinceNow < 3600 * 6 // 6 hours
    }
}

struct SpendingCheckResult: Codable {
    let canSpend: Bool
    let requiresApproval: Bool
    let blockReason: String?
    let warnings: [String]
}

struct SpendingLimitStatus: Codable {
    let period: SpendingLimitPeriod
    let limitAmount: Decimal
    let spentAmount: Decimal
    let pendingAmount: Decimal
    let remainingAmount: Decimal
    let percentUsed: Double
    let periodStart: Date
    let periodEnd: Date
    let isNearLimit: Bool
    let isAtLimit: Bool
}

struct ChildSpendingStatus: Codable {
    let childId: UUID
    let childName: String
    let approvalRequired: Bool
    let approvalThreshold: Decimal
    let isPaused: Bool
    let pauseReason: String?
    let limitStatuses: [SpendingLimitStatus]
    let pendingRequestCount: Int
    let pendingRequestTotal: Decimal
}

struct LearningMoment: Codable, Identifiable {
    let requestId: UUID
    let itemDescription: String
    let amount: Decimal
    let parentComment: String
    let wasApproved: Bool
    let date: Date

    var id: UUID { requestId }
}
```

### ViewModel

```swift
@Observable
@MainActor
final class SpendingApprovalViewModel {
    // Parent view state
    var approvalSettings: ApprovalSettings?
    var pendingRequests: [SpendingRequest] = []
    var pendingCount: Int = 0
    var pendingTotal: Decimal = 0

    // Child view state
    var spendingStatus: ChildSpendingStatus?
    var myRequests: [SpendingRequest] = []
    var learningMoments: [LearningMoment] = []

    // UI state
    private(set) var isLoading = false
    var errorMessage: String?
    var showingRequestForm = false
    var selectedRequest: SpendingRequest?

    private let apiService: APIServiceProtocol

    init(apiService: APIServiceProtocol = APIService()) {
        self.apiService = apiService
    }

    // MARK: - Parent Actions

    func loadSettings(for childId: UUID) async {
        isLoading = true
        defer { isLoading = false }

        do {
            approvalSettings = try await apiService.get(
                "/children/\(childId)/approval-settings"
            )
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func updateSettings(_ settings: ApprovalSettings) async {
        guard let childId = settings.childId else { return }

        do {
            let dto = UpdateApprovalSettingsDto(
                isEnabled: settings.isEnabled,
                isPaused: settings.isPaused,
                pauseReason: settings.pauseReason,
                approvalThreshold: settings.approvalThreshold,
                maxSinglePurchase: settings.maxSinglePurchase,
                autoApproveUnderThreshold: settings.autoApproveUnderThreshold,
                autoApproveTrustedCategories: settings.autoApproveTrustedCategories,
                trustedCategoryIds: settings.trustedCategoryIds,
                requestExpirationHours: settings.requestExpirationHours
            )

            approvalSettings = try await apiService.patch(
                "/children/\(childId)/approval-settings",
                body: dto
            )
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func loadPendingRequests(for familyId: UUID) async {
        isLoading = true
        defer { isLoading = false }

        do {
            let summary: SpendingRequestSummary = try await apiService.get(
                "/families/\(familyId)/spending-requests/pending"
            )
            pendingRequests = summary.recentPending
            pendingCount = summary.pendingCount
            pendingTotal = summary.pendingTotal
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func respondToRequest(_ request: SpendingRequest, approved: Bool, comment: String?, isLearningMoment: Bool) async {
        do {
            let dto = RespondToRequestDto(
                approved: approved,
                comment: comment,
                isLearningMoment: isLearningMoment
            )

            let updated: SpendingRequest = try await apiService.post(
                "/spending-requests/\(request.id)/respond",
                body: dto
            )

            // Remove from pending
            pendingRequests.removeAll { $0.id == request.id }
            pendingCount -= 1
            pendingTotal -= request.amount

            selectedRequest = nil
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func pauseSpending(for childId: UUID, reason: String) async {
        do {
            approvalSettings = try await apiService.post(
                "/children/\(childId)/approval-settings/pause",
                body: ["reason": reason]
            )
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func resumeSpending(for childId: UUID) async {
        do {
            approvalSettings = try await apiService.post(
                "/children/\(childId)/approval-settings/resume"
            )
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    // MARK: - Child Actions

    func loadSpendingStatus(for childId: UUID) async {
        isLoading = true
        defer { isLoading = false }

        do {
            spendingStatus = try await apiService.get(
                "/children/\(childId)/spending-status"
            )
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func checkSpending(childId: UUID, amount: Decimal, categoryId: UUID?) async -> SpendingCheckResult? {
        do {
            var query = "amount=\(amount)"
            if let catId = categoryId {
                query += "&categoryId=\(catId)"
            }

            return try await apiService.get(
                "/children/\(childId)/spending-check?\(query)"
            )
        } catch {
            errorMessage = error.localizedDescription
            return nil
        }
    }

    func createRequest(_ dto: CreateSpendingRequestDto, for childId: UUID) async -> SpendingRequest? {
        do {
            let request: SpendingRequest = try await apiService.post(
                "/spending-requests",
                body: dto.withChildId(childId)
            )
            myRequests.insert(request, at: 0)
            showingRequestForm = false
            return request
        } catch {
            errorMessage = error.localizedDescription
            return nil
        }
    }

    func cancelRequest(_ request: SpendingRequest) async {
        do {
            try await apiService.delete("/spending-requests/\(request.id)")
            myRequests.removeAll { $0.id == request.id }
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func loadMyRequests(for childId: UUID) async {
        do {
            myRequests = try await apiService.get(
                "/children/\(childId)/spending-requests"
            )
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func loadLearningMoments(for childId: UUID) async {
        do {
            learningMoments = try await apiService.get(
                "/children/\(childId)/learning-moments?limit=10"
            )
        } catch {
            errorMessage = error.localizedDescription
        }
    }
}
```

### Views

```swift
// MARK: - Parent Views

struct ApprovalQueueView: View {
    @State private var viewModel = SpendingApprovalViewModel()
    @Environment(AuthViewModel.self) private var auth

    var body: some View {
        NavigationStack {
            Group {
                if viewModel.isLoading {
                    ProgressView()
                } else if viewModel.pendingRequests.isEmpty {
                    ContentUnavailableView(
                        "No Pending Requests",
                        systemImage: "checkmark.circle",
                        description: Text("All spending requests have been reviewed")
                    )
                } else {
                    List {
                        Section {
                            HStack {
                                VStack(alignment: .leading) {
                                    Text("\(viewModel.pendingCount) Pending")
                                        .font(.headline)
                                    Text("Total: \(viewModel.pendingTotal, format: .currency(code: "USD"))")
                                        .font(.subheadline)
                                        .foregroundStyle(.secondary)
                                }
                                Spacer()
                            }
                            .padding(.vertical, 4)
                        }

                        Section("Requests") {
                            ForEach(viewModel.pendingRequests) { request in
                                SpendingRequestRow(request: request)
                                    .onTapGesture {
                                        viewModel.selectedRequest = request
                                    }
                            }
                        }
                    }
                }
            }
            .navigationTitle("Approval Queue")
            .refreshable {
                if let familyId = auth.familyId {
                    await viewModel.loadPendingRequests(for: familyId)
                }
            }
            .sheet(item: $viewModel.selectedRequest) { request in
                RequestReviewSheet(
                    request: request,
                    onRespond: { approved, comment, isLearning in
                        Task {
                            await viewModel.respondToRequest(
                                request,
                                approved: approved,
                                comment: comment,
                                isLearningMoment: isLearning
                            )
                        }
                    }
                )
            }
            .task {
                if let familyId = auth.familyId {
                    await viewModel.loadPendingRequests(for: familyId)
                }
            }
        }
    }
}

struct SpendingRequestRow: View {
    let request: SpendingRequest

    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text(request.childName)
                    .font(.headline)
                Text(request.description)
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                if request.isExpiringSoon {
                    Label("Expiring soon", systemImage: "clock.badge.exclamationmark")
                        .font(.caption)
                        .foregroundStyle(.orange)
                }
            }

            Spacer()

            Text(request.amount, format: .currency(code: "USD"))
                .font(.title3)
                .fontWeight(.semibold)
        }
        .padding(.vertical, 4)
    }
}

struct RequestReviewSheet: View {
    let request: SpendingRequest
    let onRespond: (Bool, String?, Bool) -> Void

    @State private var comment = ""
    @State private var isLearningMoment = false
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    LabeledContent("From", value: request.childName)
                    LabeledContent("Amount", value: request.amount, format: .currency(code: "USD"))
                    LabeledContent("For", value: request.description)
                    if let store = request.storeName {
                        LabeledContent("Store", value: store)
                    }
                    if let reason = request.reason {
                        VStack(alignment: .leading) {
                            Text("Reason")
                                .font(.caption)
                                .foregroundStyle(.secondary)
                            Text(reason)
                        }
                    }
                }

                Section("Your Response") {
                    TextField("Comment (optional)", text: $comment, axis: .vertical)
                        .lineLimit(3...6)

                    Toggle("Mark as Learning Moment", isOn: $isLearningMoment)
                }

                Section {
                    Button {
                        onRespond(true, comment.isEmpty ? nil : comment, isLearningMoment)
                        dismiss()
                    } label: {
                        Label("Approve", systemImage: "checkmark.circle.fill")
                            .frame(maxWidth: .infinity)
                    }
                    .buttonStyle(.borderedProminent)
                    .tint(.green)

                    Button(role: .destructive) {
                        onRespond(false, comment.isEmpty ? nil : comment, isLearningMoment)
                        dismiss()
                    } label: {
                        Label("Deny", systemImage: "xmark.circle.fill")
                            .frame(maxWidth: .infinity)
                    }
                    .buttonStyle(.borderedProminent)
                }
            }
            .navigationTitle("Review Request")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
            }
        }
        .presentationDetents([.medium, .large])
    }
}

struct ApprovalSettingsView: View {
    let childId: UUID
    @State private var viewModel = SpendingApprovalViewModel()

    var body: some View {
        Form {
            if let settings = viewModel.approvalSettings {
                Section {
                    Toggle("Enable Approval System", isOn: binding(\.isEnabled))
                }

                if settings.isEnabled {
                    Section("Thresholds") {
                        HStack {
                            Text("Require approval above")
                            Spacer()
                            TextField("Amount", value: binding(\.approvalThreshold), format: .currency(code: "USD"))
                                .multilineTextAlignment(.trailing)
                                .keyboardType(.decimalPad)
                                .frame(width: 100)
                        }

                        HStack {
                            Text("Max purchase limit")
                            Spacer()
                            TextField("No limit", value: binding(\.maxSinglePurchase), format: .currency(code: "USD"))
                                .multilineTextAlignment(.trailing)
                                .keyboardType(.decimalPad)
                                .frame(width: 100)
                        }
                    }

                    Section("Auto-Approval") {
                        Toggle("Auto-approve under threshold", isOn: binding(\.autoApproveUnderThreshold))
                        Toggle("Auto-approve trusted categories", isOn: binding(\.autoApproveTrustedCategories))
                    }

                    Section("Spending Limits") {
                        ForEach(settings.spendingLimits) { limit in
                            HStack {
                                Text(limit.period.rawValue.capitalized)
                                Spacer()
                                Text(limit.limitAmount, format: .currency(code: "USD"))
                            }
                        }
                        Button("Add Limit") {
                            // Show add limit sheet
                        }
                    }

                    Section("Category Rules") {
                        ForEach(settings.categoryRules) { rule in
                            HStack {
                                Text(rule.categoryIcon)
                                Text(rule.categoryName)
                                Spacer()
                                Text(rule.restriction.rawValue)
                                    .foregroundStyle(.secondary)
                            }
                        }
                        Button("Add Rule") {
                            // Show add rule sheet
                        }
                    }

                    Section {
                        if settings.isPaused {
                            Button("Resume Spending") {
                                Task {
                                    await viewModel.resumeSpending(for: childId)
                                }
                            }
                            .foregroundStyle(.green)
                        } else {
                            Button("Pause All Spending", role: .destructive) {
                                // Show pause reason sheet
                            }
                        }
                    }
                }
            }
        }
        .navigationTitle("Approval Settings")
        .task {
            await viewModel.loadSettings(for: childId)
        }
    }

    private func binding<T>(_ keyPath: WritableKeyPath<ApprovalSettings, T>) -> Binding<T> {
        Binding(
            get: { viewModel.approvalSettings![keyPath: keyPath] },
            set: { newValue in
                viewModel.approvalSettings![keyPath: keyPath] = newValue
                Task {
                    await viewModel.updateSettings(viewModel.approvalSettings!)
                }
            }
        )
    }
}

// MARK: - Child Views

struct SpendingStatusView: View {
    let childId: UUID
    @State private var viewModel = SpendingApprovalViewModel()

    var body: some View {
        NavigationStack {
            List {
                if let status = viewModel.spendingStatus {
                    if status.isPaused {
                        Section {
                            HStack {
                                Image(systemName: "pause.circle.fill")
                                    .foregroundStyle(.orange)
                                VStack(alignment: .leading) {
                                    Text("Spending Paused")
                                        .font(.headline)
                                    if let reason = status.pauseReason {
                                        Text(reason)
                                            .font(.caption)
                                            .foregroundStyle(.secondary)
                                    }
                                }
                            }
                        }
                    }

                    Section("Spending Limits") {
                        ForEach(status.limitStatuses, id: \.period) { limit in
                            SpendingLimitRow(limit: limit)
                        }
                    }

                    Section("Pending Requests") {
                        if status.pendingRequestCount == 0 {
                            Text("No pending requests")
                                .foregroundStyle(.secondary)
                        } else {
                            HStack {
                                Text("\(status.pendingRequestCount) requests")
                                Spacer()
                                Text(status.pendingRequestTotal, format: .currency(code: "USD"))
                            }
                        }
                    }

                    if status.approvalRequired {
                        Section {
                            Text("Purchases over \(status.approvalThreshold, format: .currency(code: "USD")) require approval")
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        }
                    }
                }
            }
            .navigationTitle("Spending Status")
            .refreshable {
                await viewModel.loadSpendingStatus(for: childId)
            }
            .task {
                await viewModel.loadSpendingStatus(for: childId)
            }
        }
    }
}

struct SpendingLimitRow: View {
    let limit: SpendingLimitStatus

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Text(limit.period.rawValue.capitalized)
                    .font(.headline)
                Spacer()
                Text("\(limit.remainingAmount, format: .currency(code: "USD")) left")
                    .foregroundStyle(limit.isAtLimit ? .red : limit.isNearLimit ? .orange : .secondary)
            }

            ProgressView(value: min(limit.percentUsed / 100, 1.0))
                .tint(limit.isAtLimit ? .red : limit.isNearLimit ? .orange : .blue)

            HStack {
                Text("Spent: \(limit.spentAmount, format: .currency(code: "USD"))")
                if limit.pendingAmount > 0 {
                    Text("+ \(limit.pendingAmount, format: .currency(code: "USD")) pending")
                        .foregroundStyle(.orange)
                }
                Spacer()
                Text("of \(limit.limitAmount, format: .currency(code: "USD"))")
            }
            .font(.caption)
            .foregroundStyle(.secondary)
        }
        .padding(.vertical, 4)
    }
}

struct CreateSpendingRequestView: View {
    let childId: UUID
    @State private var viewModel = SpendingApprovalViewModel()
    @State private var amount: Decimal = 0
    @State private var description = ""
    @State private var itemName = ""
    @State private var storeName = ""
    @State private var reason = ""
    @State private var selectedCategoryId: UUID?
    @State private var checkResult: SpendingCheckResult?
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        NavigationStack {
            Form {
                Section("Purchase Details") {
                    TextField("Amount", value: $amount, format: .currency(code: "USD"))
                        .keyboardType(.decimalPad)
                        .onChange(of: amount) { _, _ in
                            Task { await checkSpending() }
                        }

                    TextField("What is it?", text: $description)
                    TextField("Item name (optional)", text: $itemName)
                    TextField("Store name (optional)", text: $storeName)
                }

                Section("Why do you want it?") {
                    TextField("Tell your parents why...", text: $reason, axis: .vertical)
                        .lineLimit(3...6)
                }

                if let result = checkResult {
                    Section {
                        if !result.canSpend {
                            Label(result.blockReason ?? "Cannot make this purchase", systemImage: "xmark.circle.fill")
                                .foregroundStyle(.red)
                        } else if result.requiresApproval {
                            Label("This will require parent approval", systemImage: "person.badge.clock")
                                .foregroundStyle(.orange)
                        } else {
                            Label("This purchase is allowed", systemImage: "checkmark.circle.fill")
                                .foregroundStyle(.green)
                        }

                        ForEach(result.warnings, id: \.self) { warning in
                            Label(warning, systemImage: "exclamationmark.triangle")
                                .foregroundStyle(.orange)
                                .font(.caption)
                        }
                    }
                }
            }
            .navigationTitle("Request Approval")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Submit") {
                        Task { await submitRequest() }
                    }
                    .disabled(!canSubmit)
                }
            }
        }
    }

    private var canSubmit: Bool {
        amount > 0 &&
        !description.isEmpty &&
        checkResult?.requiresApproval == true
    }

    private func checkSpending() async {
        guard amount > 0 else {
            checkResult = nil
            return
        }
        checkResult = await viewModel.checkSpending(
            childId: childId,
            amount: amount,
            categoryId: selectedCategoryId
        )
    }

    private func submitRequest() async {
        let dto = CreateSpendingRequestDto(
            amount: amount,
            description: description,
            itemName: itemName.isEmpty ? nil : itemName,
            storeName: storeName.isEmpty ? nil : storeName,
            reason: reason.isEmpty ? nil : reason,
            categoryId: selectedCategoryId,
            wishListItemId: nil
        )

        if await viewModel.createRequest(dto, for: childId) != nil {
            dismiss()
        }
    }
}

struct LearningMomentsView: View {
    let childId: UUID
    @State private var viewModel = SpendingApprovalViewModel()

    var body: some View {
        NavigationStack {
            List {
                if viewModel.learningMoments.isEmpty {
                    ContentUnavailableView(
                        "No Learning Moments",
                        systemImage: "lightbulb",
                        description: Text("Parent feedback on your spending requests will appear here")
                    )
                } else {
                    ForEach(viewModel.learningMoments) { moment in
                        VStack(alignment: .leading, spacing: 8) {
                            HStack {
                                Image(systemName: moment.wasApproved ? "checkmark.circle.fill" : "xmark.circle.fill")
                                    .foregroundStyle(moment.wasApproved ? .green : .red)
                                Text(moment.itemDescription)
                                    .font(.headline)
                                Spacer()
                                Text(moment.amount, format: .currency(code: "USD"))
                            }

                            Text(moment.parentComment)
                                .font(.body)
                                .foregroundStyle(.secondary)

                            Text(moment.date, style: .date)
                                .font(.caption)
                                .foregroundStyle(.tertiary)
                        }
                        .padding(.vertical, 4)
                    }
                }
            }
            .navigationTitle("Learning Moments")
            .task {
                await viewModel.loadLearningMoments(for: childId)
            }
        }
    }
}
```

---

## React Implementation

### Types

```typescript
export enum ApprovalStatus {
  Pending = 'pending',
  Approved = 'approved',
  Denied = 'denied',
  Cancelled = 'cancelled',
  Expired = 'expired'
}

export enum SpendingLimitPeriod {
  Daily = 'daily',
  Weekly = 'weekly',
  Monthly = 'monthly'
}

export enum CategoryRestriction {
  Allowed = 'allowed',
  RequiresApproval = 'requiresApproval',
  Blocked = 'blocked'
}

export interface ApprovalSettings {
  id: string;
  childId: string;
  isEnabled: boolean;
  isPaused: boolean;
  pauseReason?: string;
  approvalThreshold: number;
  maxSinglePurchase?: number;
  autoApproveUnderThreshold: boolean;
  autoApproveTrustedCategories: boolean;
  trustedCategoryIds: string[];
  requestExpirationHours: number;
  categoryRules: CategoryApprovalRule[];
  spendingLimits: SpendingLimitConfig[];
  updatedAt: string;
}

export interface CategoryApprovalRule {
  id: string;
  categoryId: string;
  categoryName: string;
  categoryIcon: string;
  restriction: CategoryRestriction;
  categoryThreshold?: number;
  restrictionReason?: string;
}

export interface SpendingLimitConfig {
  id: string;
  period: SpendingLimitPeriod;
  limitAmount: number;
  includesPendingRequests: boolean;
}

export interface SpendingRequest {
  id: string;
  childId: string;
  childName: string;
  amount: number;
  description: string;
  itemName?: string;
  storeName?: string;
  reason?: string;
  categoryId?: string;
  categoryName?: string;
  categoryIcon?: string;
  wishListItemId?: string;
  wishListItemName?: string;
  status: ApprovalStatus;
  expiresAt?: string;
  respondedByName?: string;
  respondedAt?: string;
  parentComment?: string;
  isLearningMoment: boolean;
  createdAt: string;
}

export interface SpendingCheckResult {
  canSpend: boolean;
  requiresApproval: boolean;
  blockReason?: string;
  warnings: string[];
}

export interface SpendingLimitStatus {
  period: SpendingLimitPeriod;
  limitAmount: number;
  spentAmount: number;
  pendingAmount: number;
  remainingAmount: number;
  percentUsed: number;
  periodStart: string;
  periodEnd: string;
  isNearLimit: boolean;
  isAtLimit: boolean;
}

export interface ChildSpendingStatus {
  childId: string;
  childName: string;
  approvalRequired: boolean;
  approvalThreshold: number;
  isPaused: boolean;
  pauseReason?: string;
  limitStatuses: SpendingLimitStatus[];
  pendingRequestCount: number;
  pendingRequestTotal: number;
}

export interface LearningMoment {
  requestId: string;
  itemDescription: string;
  amount: number;
  parentComment: string;
  wasApproved: boolean;
  date: string;
}
```

### Hooks

```typescript
// hooks/useSpendingApproval.ts
import { useState, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';

export function useApprovalSettings(childId: string) {
  return useQuery({
    queryKey: ['approvalSettings', childId],
    queryFn: () => api.get<ApprovalSettings>(`/children/${childId}/approval-settings`),
    enabled: !!childId
  });
}

export function useUpdateApprovalSettings(childId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (settings: Partial<ApprovalSettings>) =>
      api.patch<ApprovalSettings>(`/children/${childId}/approval-settings`, settings),
    onSuccess: (data) => {
      queryClient.setQueryData(['approvalSettings', childId], data);
    }
  });
}

export function usePendingRequests(familyId: string) {
  return useQuery({
    queryKey: ['pendingRequests', familyId],
    queryFn: () => api.get<SpendingRequestSummary>(`/families/${familyId}/spending-requests/pending`),
    enabled: !!familyId,
    refetchInterval: 30000 // Refresh every 30 seconds
  });
}

export function useRespondToRequest() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ requestId, response }: { requestId: string; response: RespondToRequestDto }) =>
      api.post<SpendingRequest>(`/spending-requests/${requestId}/respond`, response),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pendingRequests'] });
      queryClient.invalidateQueries({ queryKey: ['spendingRequests'] });
    }
  });
}

export function useSpendingStatus(childId: string) {
  return useQuery({
    queryKey: ['spendingStatus', childId],
    queryFn: () => api.get<ChildSpendingStatus>(`/children/${childId}/spending-status`),
    enabled: !!childId
  });
}

export function useSpendingCheck(childId: string) {
  const [result, setResult] = useState<SpendingCheckResult | null>(null);

  const checkSpending = useCallback(async (amount: number, categoryId?: string) => {
    if (amount <= 0) {
      setResult(null);
      return null;
    }

    const params = new URLSearchParams({ amount: amount.toString() });
    if (categoryId) params.append('categoryId', categoryId);

    const check = await api.get<SpendingCheckResult>(
      `/children/${childId}/spending-check?${params}`
    );
    setResult(check);
    return check;
  }, [childId]);

  return { result, checkSpending };
}

export function useCreateSpendingRequest(childId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: CreateSpendingRequestDto) =>
      api.post<SpendingRequest>('/spending-requests', { ...dto, childId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['spendingRequests', childId] });
      queryClient.invalidateQueries({ queryKey: ['spendingStatus', childId] });
    }
  });
}

export function useChildRequests(childId: string, status?: ApprovalStatus) {
  return useQuery({
    queryKey: ['spendingRequests', childId, status],
    queryFn: () => {
      const params = status ? `?status=${status}` : '';
      return api.get<SpendingRequest[]>(`/children/${childId}/spending-requests${params}`);
    },
    enabled: !!childId
  });
}

export function useLearningMoments(childId: string) {
  return useQuery({
    queryKey: ['learningMoments', childId],
    queryFn: () => api.get<LearningMoment[]>(`/children/${childId}/learning-moments`),
    enabled: !!childId
  });
}
```

### Components

```tsx
// components/spending/ApprovalQueue.tsx
import React from 'react';
import { usePendingRequests, useRespondToRequest } from '@/hooks/useSpendingApproval';
import { useFamily } from '@/hooks/useFamily';
import { formatCurrency, formatRelativeTime } from '@/lib/utils';
import { RequestReviewModal } from './RequestReviewModal';

export function ApprovalQueue() {
  const { family } = useFamily();
  const { data: summary, isLoading } = usePendingRequests(family?.id ?? '');
  const [selectedRequest, setSelectedRequest] = React.useState<SpendingRequest | null>(null);
  const respondMutation = useRespondToRequest();

  if (isLoading) {
    return <div className="animate-pulse">Loading...</div>;
  }

  if (!summary || summary.pendingCount === 0) {
    return (
      <div className="text-center py-12">
        <div className="text-4xl mb-4">✅</div>
        <h3 className="text-lg font-medium">No Pending Requests</h3>
        <p className="text-gray-500">All spending requests have been reviewed</p>
      </div>
    );
  }

  const handleRespond = async (approved: boolean, comment?: string, isLearningMoment = false) => {
    if (!selectedRequest) return;

    await respondMutation.mutateAsync({
      requestId: selectedRequest.id,
      response: { approved, comment, isLearningMoment }
    });

    setSelectedRequest(null);
  };

  return (
    <div className="space-y-6">
      <div className="bg-blue-50 rounded-lg p-4">
        <div className="flex justify-between items-center">
          <div>
            <span className="text-2xl font-bold">{summary.pendingCount}</span>
            <span className="text-gray-600 ml-2">pending requests</span>
          </div>
          <div className="text-right">
            <div className="text-lg font-semibold">{formatCurrency(summary.pendingTotal)}</div>
            <div className="text-sm text-gray-500">total amount</div>
          </div>
        </div>
      </div>

      <div className="space-y-3">
        {summary.recentPending.map((request) => (
          <div
            key={request.id}
            onClick={() => setSelectedRequest(request)}
            className="bg-white rounded-lg border p-4 cursor-pointer hover:border-blue-300 transition-colors"
          >
            <div className="flex justify-between items-start">
              <div>
                <div className="font-medium">{request.childName}</div>
                <div className="text-gray-600">{request.description}</div>
                {request.expiresAt && isExpiringSoon(request.expiresAt) && (
                  <div className="flex items-center text-orange-600 text-sm mt-1">
                    <ClockIcon className="w-4 h-4 mr-1" />
                    Expiring soon
                  </div>
                )}
              </div>
              <div className="text-xl font-semibold">{formatCurrency(request.amount)}</div>
            </div>
          </div>
        ))}
      </div>

      {selectedRequest && (
        <RequestReviewModal
          request={selectedRequest}
          onClose={() => setSelectedRequest(null)}
          onRespond={handleRespond}
          isLoading={respondMutation.isPending}
        />
      )}
    </div>
  );
}

// components/spending/RequestReviewModal.tsx
export function RequestReviewModal({
  request,
  onClose,
  onRespond,
  isLoading
}: {
  request: SpendingRequest;
  onClose: () => void;
  onRespond: (approved: boolean, comment?: string, isLearningMoment?: boolean) => void;
  isLoading: boolean;
}) {
  const [comment, setComment] = React.useState('');
  const [isLearningMoment, setIsLearningMoment] = React.useState(false);

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
      <div className="bg-white rounded-xl max-w-md w-full p-6">
        <h2 className="text-xl font-bold mb-4">Review Request</h2>

        <div className="space-y-3 mb-6">
          <div className="flex justify-between">
            <span className="text-gray-500">From</span>
            <span className="font-medium">{request.childName}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-500">Amount</span>
            <span className="font-semibold text-lg">{formatCurrency(request.amount)}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-500">For</span>
            <span>{request.description}</span>
          </div>
          {request.storeName && (
            <div className="flex justify-between">
              <span className="text-gray-500">Store</span>
              <span>{request.storeName}</span>
            </div>
          )}
          {request.reason && (
            <div className="pt-2 border-t">
              <div className="text-gray-500 text-sm mb-1">Reason</div>
              <p className="text-gray-700">{request.reason}</p>
            </div>
          )}
        </div>

        <div className="space-y-4 mb-6">
          <div>
            <label className="block text-sm font-medium mb-1">Comment (optional)</label>
            <textarea
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              placeholder="Add feedback for your child..."
              className="w-full border rounded-lg p-3 h-24 resize-none"
            />
          </div>

          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={isLearningMoment}
              onChange={(e) => setIsLearningMoment(e.target.checked)}
              className="rounded"
            />
            <span className="text-sm">Mark as Learning Moment</span>
          </label>
        </div>

        <div className="flex gap-3">
          <button
            onClick={() => onRespond(false, comment || undefined, isLearningMoment)}
            disabled={isLoading}
            className="flex-1 py-3 border-2 border-red-500 text-red-500 rounded-lg font-medium hover:bg-red-50 disabled:opacity-50"
          >
            Deny
          </button>
          <button
            onClick={() => onRespond(true, comment || undefined, isLearningMoment)}
            disabled={isLoading}
            className="flex-1 py-3 bg-green-500 text-white rounded-lg font-medium hover:bg-green-600 disabled:opacity-50"
          >
            Approve
          </button>
        </div>

        <button
          onClick={onClose}
          className="w-full mt-3 py-2 text-gray-500 hover:text-gray-700"
        >
          Cancel
        </button>
      </div>
    </div>
  );
}

// components/spending/SpendingLimitProgress.tsx
export function SpendingLimitProgress({ limit }: { limit: SpendingLimitStatus }) {
  const percentage = Math.min(limit.percentUsed, 100);
  const color = limit.isAtLimit ? 'red' : limit.isNearLimit ? 'orange' : 'blue';

  return (
    <div className="bg-white rounded-lg border p-4">
      <div className="flex justify-between items-center mb-2">
        <span className="font-medium capitalize">{limit.period}</span>
        <span className={`font-semibold ${limit.isAtLimit ? 'text-red-600' : ''}`}>
          {formatCurrency(limit.remainingAmount)} left
        </span>
      </div>

      <div className="h-3 bg-gray-200 rounded-full overflow-hidden">
        <div
          className={`h-full bg-${color}-500 transition-all`}
          style={{ width: `${percentage}%` }}
        />
      </div>

      <div className="flex justify-between text-sm text-gray-500 mt-1">
        <span>
          Spent: {formatCurrency(limit.spentAmount)}
          {limit.pendingAmount > 0 && (
            <span className="text-orange-500"> + {formatCurrency(limit.pendingAmount)} pending</span>
          )}
        </span>
        <span>of {formatCurrency(limit.limitAmount)}</span>
      </div>
    </div>
  );
}

// components/spending/CreateRequestForm.tsx
export function CreateRequestForm({ childId, onSuccess }: { childId: string; onSuccess?: () => void }) {
  const [amount, setAmount] = React.useState('');
  const [description, setDescription] = React.useState('');
  const [itemName, setItemName] = React.useState('');
  const [storeName, setStoreName] = React.useState('');
  const [reason, setReason] = React.useState('');

  const { result: checkResult, checkSpending } = useSpendingCheck(childId);
  const createMutation = useCreateSpendingRequest(childId);

  React.useEffect(() => {
    const numAmount = parseFloat(amount);
    if (!isNaN(numAmount)) {
      checkSpending(numAmount);
    }
  }, [amount, checkSpending]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    await createMutation.mutateAsync({
      amount: parseFloat(amount),
      description,
      itemName: itemName || undefined,
      storeName: storeName || undefined,
      reason: reason || undefined
    });

    onSuccess?.();
  };

  const canSubmit = parseFloat(amount) > 0 &&
    description &&
    checkResult?.requiresApproval;

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div>
        <label className="block text-sm font-medium mb-1">Amount</label>
        <input
          type="number"
          step="0.01"
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
          placeholder="0.00"
          className="w-full border rounded-lg p-3"
        />
      </div>

      <div>
        <label className="block text-sm font-medium mb-1">What is it?</label>
        <input
          type="text"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Describe what you want to buy"
          className="w-full border rounded-lg p-3"
        />
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="block text-sm font-medium mb-1">Item name (optional)</label>
          <input
            type="text"
            value={itemName}
            onChange={(e) => setItemName(e.target.value)}
            className="w-full border rounded-lg p-3"
          />
        </div>
        <div>
          <label className="block text-sm font-medium mb-1">Store (optional)</label>
          <input
            type="text"
            value={storeName}
            onChange={(e) => setStoreName(e.target.value)}
            className="w-full border rounded-lg p-3"
          />
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium mb-1">Why do you want it?</label>
        <textarea
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          placeholder="Tell your parents why..."
          className="w-full border rounded-lg p-3 h-24 resize-none"
        />
      </div>

      {checkResult && (
        <div className={`p-3 rounded-lg ${
          !checkResult.canSpend ? 'bg-red-50 text-red-700' :
          checkResult.requiresApproval ? 'bg-orange-50 text-orange-700' :
          'bg-green-50 text-green-700'
        }`}>
          {!checkResult.canSpend ? (
            <p>❌ {checkResult.blockReason}</p>
          ) : checkResult.requiresApproval ? (
            <p>⏳ This will require parent approval</p>
          ) : (
            <p>✅ This purchase is allowed</p>
          )}
          {checkResult.warnings.map((warning, i) => (
            <p key={i} className="text-sm mt-1">⚠️ {warning}</p>
          ))}
        </div>
      )}

      <button
        type="submit"
        disabled={!canSubmit || createMutation.isPending}
        className="w-full py-3 bg-blue-500 text-white rounded-lg font-medium disabled:opacity-50"
      >
        {createMutation.isPending ? 'Submitting...' : 'Submit Request'}
      </button>
    </form>
  );
}

// components/spending/LearningMoments.tsx
export function LearningMoments({ childId }: { childId: string }) {
  const { data: moments, isLoading } = useLearningMoments(childId);

  if (isLoading) {
    return <div className="animate-pulse">Loading...</div>;
  }

  if (!moments?.length) {
    return (
      <div className="text-center py-12">
        <div className="text-4xl mb-4">💡</div>
        <h3 className="text-lg font-medium">No Learning Moments</h3>
        <p className="text-gray-500">Parent feedback on your spending requests will appear here</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {moments.map((moment) => (
        <div key={moment.requestId} className="bg-white rounded-lg border p-4">
          <div className="flex items-start gap-3">
            <div className={`text-2xl ${moment.wasApproved ? '' : ''}`}>
              {moment.wasApproved ? '✅' : '❌'}
            </div>
            <div className="flex-1">
              <div className="flex justify-between">
                <span className="font-medium">{moment.itemDescription}</span>
                <span className="text-gray-500">{formatCurrency(moment.amount)}</span>
              </div>
              <p className="text-gray-600 mt-2">{moment.parentComment}</p>
              <p className="text-gray-400 text-sm mt-2">
                {new Date(moment.date).toLocaleDateString()}
              </p>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
```

### Pages

```tsx
// pages/spending/approval-queue.tsx
import { ApprovalQueue } from '@/components/spending/ApprovalQueue';
import { RequireParent } from '@/components/auth/RequireParent';

export default function ApprovalQueuePage() {
  return (
    <RequireParent>
      <div className="container mx-auto px-4 py-6">
        <h1 className="text-2xl font-bold mb-6">Approval Queue</h1>
        <ApprovalQueue />
      </div>
    </RequireParent>
  );
}

// pages/spending/settings/[childId].tsx
import { ApprovalSettingsForm } from '@/components/spending/ApprovalSettingsForm';
import { RequireParent } from '@/components/auth/RequireParent';
import { useRouter } from 'next/router';

export default function ApprovalSettingsPage() {
  const router = useRouter();
  const { childId } = router.query;

  if (!childId || typeof childId !== 'string') {
    return null;
  }

  return (
    <RequireParent>
      <div className="container mx-auto px-4 py-6">
        <h1 className="text-2xl font-bold mb-6">Approval Settings</h1>
        <ApprovalSettingsForm childId={childId} />
      </div>
    </RequireParent>
  );
}

// pages/child/spending-status.tsx
import { useAuth } from '@/hooks/useAuth';
import { useSpendingStatus } from '@/hooks/useSpendingApproval';
import { SpendingLimitProgress } from '@/components/spending/SpendingLimitProgress';
import { RequireChild } from '@/components/auth/RequireChild';

export default function SpendingStatusPage() {
  const { user } = useAuth();
  const { data: status, isLoading } = useSpendingStatus(user?.childId ?? '');

  return (
    <RequireChild>
      <div className="container mx-auto px-4 py-6">
        <h1 className="text-2xl font-bold mb-6">My Spending</h1>

        {isLoading ? (
          <div className="animate-pulse">Loading...</div>
        ) : status ? (
          <div className="space-y-6">
            {status.isPaused && (
              <div className="bg-orange-50 border border-orange-200 rounded-lg p-4">
                <div className="flex items-center gap-2 text-orange-700">
                  <span className="text-xl">⏸️</span>
                  <span className="font-medium">Spending Paused</span>
                </div>
                {status.pauseReason && (
                  <p className="text-orange-600 mt-1">{status.pauseReason}</p>
                )}
              </div>
            )}

            {status.limitStatuses.length > 0 && (
              <div>
                <h2 className="text-lg font-semibold mb-3">Spending Limits</h2>
                <div className="space-y-3">
                  {status.limitStatuses.map((limit) => (
                    <SpendingLimitProgress key={limit.period} limit={limit} />
                  ))}
                </div>
              </div>
            )}

            {status.pendingRequestCount > 0 && (
              <div className="bg-blue-50 rounded-lg p-4">
                <div className="font-medium">
                  {status.pendingRequestCount} pending request{status.pendingRequestCount !== 1 ? 's' : ''}
                </div>
                <div className="text-gray-600">
                  Total: {formatCurrency(status.pendingRequestTotal)}
                </div>
              </div>
            )}

            {status.approvalRequired && (
              <p className="text-gray-500 text-sm">
                Purchases over {formatCurrency(status.approvalThreshold)} require approval
              </p>
            )}
          </div>
        ) : null}
      </div>
    </RequireChild>
  );
}
```

---

## Testing Strategy

### Unit Tests (25 tests)

```csharp
// SpendingApprovalServiceTests.cs
public class SpendingApprovalServiceTests
{
    // Settings Tests
    [Fact] public async Task GetSettings_CreatesDefaultIfNotExists()
    [Fact] public async Task UpdateSettings_UpdatesThreshold()
    [Fact] public async Task SetCategoryRule_AddsNewRule()
    [Fact] public async Task SetCategoryRule_UpdatesExistingRule()
    [Fact] public async Task RemoveCategoryRule_DeletesRule()
    [Fact] public async Task SetSpendingLimit_AddsLimit()
    [Fact] public async Task PauseSpending_SetsIsPausedTrue()
    [Fact] public async Task ResumeSpending_SetsIsPausedFalse()

    // Spending Check Tests
    [Fact] public async Task CheckSpending_AllowsWhenDisabled()
    [Fact] public async Task CheckSpending_BlocksWhenPaused()
    [Fact] public async Task CheckSpending_BlocksOverMaxPurchase()
    [Fact] public async Task CheckSpending_BlocksBlockedCategory()
    [Fact] public async Task CheckSpending_RequiresApprovalAboveThreshold()
    [Fact] public async Task CheckSpending_AutoApprovesUnderThreshold()
    [Fact] public async Task CheckSpending_AutoApprovesTrustedCategory()
    [Fact] public async Task CheckSpending_BlocksWhenLimitExceeded()
    [Fact] public async Task CheckSpending_WarnsNearLimit()

    // Request Tests
    [Fact] public async Task CreateRequest_FailsIfNotRequired()
    [Fact] public async Task CreateRequest_CreatesWithPendingStatus()
    [Fact] public async Task CreateRequest_UpdatesPendingTracker()
    [Fact] public async Task CancelRequest_SetsStatusToCancelled()
    [Fact] public async Task RespondToRequest_ApprovesAndCreatesTransaction()
    [Fact] public async Task RespondToRequest_DeniesWithComment()
    [Fact] public async Task ExpireOldRequests_ExpiresOverdue()
    [Fact] public async Task GetLearningMoments_ReturnsMarkedOnly()
}
```

### Controller Tests (10 tests)

```csharp
// SpendingApprovalControllerTests.cs
public class SpendingApprovalControllerTests
{
    [Fact] public async Task GetSettings_RequiresParentRole()
    [Fact] public async Task GetSettings_ReturnsSettingsForChild()
    [Fact] public async Task UpdateSettings_RequiresFamilyMember()
    [Fact] public async Task CreateRequest_RequiresChildRole()
    [Fact] public async Task CreateRequest_ReturnsCreated()
    [Fact] public async Task GetPendingRequests_ReturnsOnlyFamilyRequests()
    [Fact] public async Task RespondToRequest_RequiresParentRole()
    [Fact] public async Task RespondToRequest_UpdatesStatus()
    [Fact] public async Task CancelRequest_RequiresRequestOwner()
    [Fact] public async Task BulkRespond_ProcessesMultipleRequests()
}
```

### Integration Tests (10 tests)

```csharp
// SpendingApprovalIntegrationTests.cs
public class SpendingApprovalIntegrationTests
{
    [Fact] public async Task FullWorkflow_ChildRequestsParentApproves()
    [Fact] public async Task FullWorkflow_ChildRequestsParentDenies()
    [Fact] public async Task FullWorkflow_RequestExpiresAutomatically()
    [Fact] public async Task CategoryRestriction_BlocksSpending()
    [Fact] public async Task SpendingLimit_EnforcedCorrectly()
    [Fact] public async Task PauseSpending_BlocksAllRequests()
    [Fact] public async Task LearningMoment_SavedAndRetrieved()
    [Fact] public async Task ApprovedRequest_CreatesTransaction()
    [Fact] public async Task PendingAmount_TrackedCorrectly()
    [Fact] public async Task MultipleChildren_IndependentSettings()
}
```

---

## Implementation Phases

### Phase 1: Core Models & Settings (2 days)
- Create database models and migrations
- Implement ApprovalSettings CRUD
- Create CategoryApprovalRule management
- Create SpendingLimit management
- Write unit tests for settings

### Phase 2: Spending Check Logic (2 days)
- Implement CheckSpendingAsync logic
- Create SpendingLimitTracker management
- Handle category restrictions
- Add threshold and limit validation
- Write unit tests for spending checks

### Phase 3: Request Workflow (2 days)
- Implement CreateRequestAsync
- Implement RespondToRequestAsync
- Add CancelRequestAsync
- Create notification triggers
- Write request workflow tests

### Phase 4: Background Processing (1 day)
- Create SpendingApprovalBackgroundService
- Implement ExpireOldRequestsAsync
- Implement ResetPeriodTrackersAsync
- Add logging and error handling

### Phase 5: API Layer (2 days)
- Create ApprovalSettingsController
- Create SpendingRequestsController
- Add authorization policies
- Write controller tests
- Add Swagger documentation

### Phase 6: iOS Implementation (3 days)
- Create Swift models
- Implement SpendingApprovalViewModel
- Build parent approval queue views
- Build child spending status views
- Create request form and review modals

### Phase 7: React Implementation (3 days)
- Create TypeScript types
- Implement React hooks
- Build approval queue component
- Build settings management
- Create child spending views

### Phase 8: Testing & Polish (2 days)
- Complete integration tests
- End-to-end testing
- Performance optimization
- Error handling improvements

**Total: 17 days**

---

## Success Criteria

1. **Functional Requirements**
   - Parents can configure per-child approval settings
   - Category restrictions work correctly (blocked, requires approval)
   - Spending limits enforced across periods
   - Children can submit and track requests
   - Parents can approve/deny with learning moments
   - Requests expire automatically

2. **Performance Requirements**
   - Spending check completes in <100ms
   - Request creation completes in <500ms
   - Background job processes expirations efficiently

3. **Security Requirements**
   - Parents can only manage their children's settings
   - Children can only see their own requests
   - Only parents can approve/deny
   - Proper authorization on all endpoints

4. **Quality Requirements**
   - >90% test coverage on service layer
   - All controller endpoints tested
   - Integration tests for critical workflows
   - No regressions in existing functionality

5. **User Experience**
   - Clear feedback on spending check results
   - Real-time notifications for approvals/denials
   - Easy-to-understand limit progress
   - Learning moments visible to children
