using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AllowanceTracker.Services;

public class SavingsGoalService : ISavingsGoalService
{
    private readonly AllowanceContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService? _notificationService;
    private readonly IAchievementService? _achievementService;
    private readonly ILogger<SavingsGoalService>? _logger;

    public SavingsGoalService(
        AllowanceContext context,
        ICurrentUserService currentUser,
        INotificationService? notificationService = null,
        IAchievementService? achievementService = null,
        ILogger<SavingsGoalService>? logger = null)
    {
        _context = context;
        _currentUser = currentUser;
        _notificationService = notificationService;
        _achievementService = achievementService;
        _logger = logger;
    }

    #region Goal CRUD

    public async Task<SavingsGoalDto> CreateGoalAsync(CreateSavingsGoalDto dto, Guid userId)
    {
        var child = await _context.Children
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == dto.ChildId)
            ?? throw new InvalidOperationException("Child not found");

        var goal = new SavingsGoal
        {
            Id = Guid.NewGuid(),
            ChildId = dto.ChildId,
            Name = dto.Name,
            Description = dto.Description,
            TargetAmount = dto.TargetAmount,
            CurrentAmount = 0,
            ImageUrl = dto.ImageUrl,
            ProductUrl = dto.ProductUrl,
            Category = dto.Category,
            TargetDate = dto.TargetDate,
            Status = GoalStatus.Active,
            Priority = dto.Priority,
            AutoTransferAmount = dto.AutoTransferAmount,
            AutoTransferType = dto.AutoTransferType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SavingsGoals.Add(goal);

        // Create milestones at 25%, 50%, 75%, 100%
        var milestonePercentages = new[] { 25, 50, 75, 100 };
        foreach (var percent in milestonePercentages)
        {
            var milestone = new GoalMilestone
            {
                Id = Guid.NewGuid(),
                GoalId = goal.Id,
                PercentComplete = percent,
                TargetAmount = dto.TargetAmount * percent / 100,
                IsAchieved = false,
                CelebrationMessage = $"You've reached {percent}% of your goal!"
            };
            _context.GoalMilestones.Add(milestone);
        }

        await _context.SaveChangesAsync();

        // Reload with navigation properties
        goal = await _context.SavingsGoals
            .Include(g => g.Child).ThenInclude(c => c.User)
            .Include(g => g.Milestones)
            .Include(g => g.MatchingRule)
            .Include(g => g.ActiveChallenge)
            .FirstAsync(g => g.Id == goal.Id);

        return SavingsGoalDto.FromGoal(goal);
    }

    public async Task<SavingsGoalDto?> GetGoalByIdAsync(Guid goalId, Guid userId)
    {
        var goal = await _context.SavingsGoals
            .Include(g => g.Child).ThenInclude(c => c.User)
            .Include(g => g.Milestones)
            .Include(g => g.MatchingRule).ThenInclude(r => r!.CreatedByParent)
            .Include(g => g.ActiveChallenge).ThenInclude(c => c!.CreatedByParent)
            .FirstOrDefaultAsync(g => g.Id == goalId);

        return goal != null ? SavingsGoalDto.FromGoal(goal) : null;
    }

    public async Task<List<SavingsGoalDto>> GetChildGoalsAsync(Guid childId, GoalStatus? status, bool includeCompleted, Guid userId)
    {
        var query = _context.SavingsGoals
            .Include(g => g.Child).ThenInclude(c => c.User)
            .Include(g => g.Milestones)
            .Include(g => g.MatchingRule)
            .Include(g => g.ActiveChallenge)
            .Where(g => g.ChildId == childId);

        if (status.HasValue)
        {
            query = query.Where(g => g.Status == status.Value);
        }
        else if (!includeCompleted)
        {
            query = query.Where(g => g.Status == GoalStatus.Active || g.Status == GoalStatus.Paused);
        }

        var goals = await query.OrderBy(g => g.Priority).ThenBy(g => g.CreatedAt).ToListAsync();
        return goals.Select(SavingsGoalDto.FromGoal).ToList();
    }

    public async Task<SavingsGoalDto> UpdateGoalAsync(Guid goalId, UpdateSavingsGoalDto dto, Guid userId)
    {
        var goal = await _context.SavingsGoals
            .Include(g => g.Child).ThenInclude(c => c.User)
            .Include(g => g.Milestones)
            .FirstOrDefaultAsync(g => g.Id == goalId)
            ?? throw new InvalidOperationException("Goal not found");

        if (dto.Name != null) goal.Name = dto.Name;
        if (dto.Description != null) goal.Description = dto.Description;
        if (dto.TargetAmount.HasValue) goal.TargetAmount = dto.TargetAmount.Value;
        if (dto.ImageUrl != null) goal.ImageUrl = dto.ImageUrl;
        if (dto.ProductUrl != null) goal.ProductUrl = dto.ProductUrl;
        if (dto.Category.HasValue) goal.Category = dto.Category.Value;
        if (dto.TargetDate.HasValue) goal.TargetDate = dto.TargetDate.Value;
        if (dto.Priority.HasValue) goal.Priority = dto.Priority.Value;
        if (dto.AutoTransferAmount.HasValue) goal.AutoTransferAmount = dto.AutoTransferAmount.Value;
        if (dto.AutoTransferType.HasValue) goal.AutoTransferType = dto.AutoTransferType.Value;

        goal.UpdatedAt = DateTime.UtcNow;

        // Update milestone targets if target amount changed
        if (dto.TargetAmount.HasValue)
        {
            foreach (var milestone in goal.Milestones)
            {
                milestone.TargetAmount = dto.TargetAmount.Value * milestone.PercentComplete / 100;
            }
        }

        await _context.SaveChangesAsync();
        return SavingsGoalDto.FromGoal(goal);
    }

    public async Task CancelGoalAsync(Guid goalId, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var goal = await _context.SavingsGoals
                .Include(g => g.Child)
                .FirstOrDefaultAsync(g => g.Id == goalId)
                ?? throw new InvalidOperationException("Goal not found");

            // Return funds to child's balance
            goal.Child.CurrentBalance += goal.CurrentAmount;
            goal.Status = GoalStatus.Cancelled;
            goal.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<SavingsGoalDto> PauseGoalAsync(Guid goalId, Guid userId)
    {
        var goal = await _context.SavingsGoals
            .Include(g => g.Child).ThenInclude(c => c.User)
            .Include(g => g.Milestones)
            .FirstOrDefaultAsync(g => g.Id == goalId)
            ?? throw new InvalidOperationException("Goal not found");

        goal.Status = GoalStatus.Paused;
        goal.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return SavingsGoalDto.FromGoal(goal);
    }

    public async Task<SavingsGoalDto> ResumeGoalAsync(Guid goalId, Guid userId)
    {
        var goal = await _context.SavingsGoals
            .Include(g => g.Child).ThenInclude(c => c.User)
            .Include(g => g.Milestones)
            .FirstOrDefaultAsync(g => g.Id == goalId)
            ?? throw new InvalidOperationException("Goal not found");

        goal.Status = GoalStatus.Active;
        goal.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return SavingsGoalDto.FromGoal(goal);
    }

    #endregion

    #region Contributions

    public async Task<GoalProgressEventDto> ContributeAsync(Guid goalId, ContributeToGoalDto dto, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var goal = await _context.SavingsGoals
                .Include(g => g.Child).ThenInclude(c => c.User)
                .Include(g => g.MatchingRule)
                .Include(g => g.Milestones)
                .Include(g => g.ActiveChallenge)
                .FirstOrDefaultAsync(g => g.Id == goalId)
                ?? throw new InvalidOperationException("Goal not found");

            if (goal.Status != GoalStatus.Active)
                throw new InvalidOperationException("Goal is not active");

            if (goal.Child.CurrentBalance < dto.Amount)
                throw new InvalidOperationException("Insufficient balance");

            // 1. Deduct from child's balance
            goal.Child.CurrentBalance -= dto.Amount;

            // 2. Create contribution record
            var contribution = new SavingsContribution
            {
                Id = Guid.NewGuid(),
                GoalId = goalId,
                ChildId = goal.ChildId,
                Amount = dto.Amount,
                Type = ContributionType.ChildDeposit,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId
            };

            goal.CurrentAmount += dto.Amount;
            contribution.GoalBalanceAfter = goal.CurrentAmount;
            _context.SavingsContributions.Add(contribution);

            // 3. Apply parent matching
            decimal matchAmount = 0;
            if (goal.MatchingRule?.IsActive == true)
            {
                matchAmount = CalculateMatchAmount(goal.MatchingRule, dto.Amount);
                if (matchAmount > 0)
                {
                    var matchContribution = new SavingsContribution
                    {
                        Id = Guid.NewGuid(),
                        GoalId = goalId,
                        ChildId = goal.ChildId,
                        Amount = matchAmount,
                        Type = ContributionType.ParentMatch,
                        ParentMatchId = contribution.Id,
                        Description = $"Parent match for ${dto.Amount} deposit",
                        CreatedAt = DateTime.UtcNow
                    };

                    goal.CurrentAmount += matchAmount;
                    matchContribution.GoalBalanceAfter = goal.CurrentAmount;
                    goal.MatchingRule.TotalMatchedAmount += matchAmount;

                    _context.SavingsContributions.Add(matchContribution);
                }
            }

            // 4. Check milestones
            MilestoneDto? milestoneReached = null;
            var progressPercent = (goal.CurrentAmount / goal.TargetAmount) * 100;

            foreach (var milestone in goal.Milestones.Where(m => !m.IsAchieved).OrderBy(m => m.PercentComplete))
            {
                if (progressPercent >= milestone.PercentComplete)
                {
                    milestone.IsAchieved = true;
                    milestone.AchievedAt = DateTime.UtcNow;
                    milestoneReached = MilestoneDto.FromMilestone(milestone);

                    // Award milestone bonus if configured
                    if (milestone.BonusAmount.HasValue && milestone.BonusAmount > 0)
                    {
                        var bonusContribution = new SavingsContribution
                        {
                            Id = Guid.NewGuid(),
                            GoalId = goalId,
                            ChildId = goal.ChildId,
                            Amount = milestone.BonusAmount.Value,
                            Type = ContributionType.ChallengeBonus,
                            Description = $"Bonus for reaching {milestone.PercentComplete}% milestone",
                            CreatedAt = DateTime.UtcNow
                        };
                        goal.CurrentAmount += milestone.BonusAmount.Value;
                        bonusContribution.GoalBalanceAfter = goal.CurrentAmount;
                        _context.SavingsContributions.Add(bonusContribution);
                    }
                }
            }

            // 5. Check challenge completion
            if (goal.ActiveChallenge?.Status == ChallengeStatus.Active &&
                goal.CurrentAmount >= goal.ActiveChallenge.TargetAmount)
            {
                goal.ActiveChallenge.Status = ChallengeStatus.Completed;
                goal.ActiveChallenge.CompletedAt = DateTime.UtcNow;

                // Award challenge bonus
                var challengeBonus = new SavingsContribution
                {
                    Id = Guid.NewGuid(),
                    GoalId = goalId,
                    ChildId = goal.ChildId,
                    Amount = goal.ActiveChallenge.BonusAmount,
                    Type = ContributionType.ChallengeBonus,
                    Description = "Challenge completion bonus!",
                    CreatedAt = DateTime.UtcNow
                };
                goal.CurrentAmount += goal.ActiveChallenge.BonusAmount;
                challengeBonus.GoalBalanceAfter = goal.CurrentAmount;
                _context.SavingsContributions.Add(challengeBonus);
            }

            // 6. Check goal completion
            bool isCompleted = false;
            if (goal.CurrentAmount >= goal.TargetAmount)
            {
                goal.Status = GoalStatus.Completed;
                goal.CompletedAt = DateTime.UtcNow;
                isCompleted = true;
            }

            goal.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new GoalProgressEventDto(
                GoalId: goalId,
                GoalName: goal.Name,
                NewAmount: goal.CurrentAmount,
                TargetAmount: goal.TargetAmount,
                ProgressPercentage: (double)(goal.CurrentAmount / goal.TargetAmount * 100),
                MilestoneReached: milestoneReached,
                IsCompleted: isCompleted,
                MatchAmountAdded: matchAmount > 0 ? matchAmount : null
            );
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private decimal CalculateMatchAmount(ParentMatchingRule rule, decimal depositAmount)
    {
        decimal matchAmount = rule.Type switch
        {
            MatchingType.RatioMatch => depositAmount * rule.MatchRatio,
            MatchingType.PercentageMatch => depositAmount * (rule.MatchRatio / 100),
            _ => 0
        };

        // Apply cap if set
        if (rule.MaxMatchAmount.HasValue)
        {
            var remainingCap = rule.MaxMatchAmount.Value - rule.TotalMatchedAmount;
            matchAmount = Math.Min(matchAmount, remainingCap);
        }

        return Math.Max(0, matchAmount);
    }

    public async Task<ContributionDto> WithdrawAsync(Guid goalId, WithdrawFromGoalDto dto, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var goal = await _context.SavingsGoals
                .Include(g => g.Child)
                .FirstOrDefaultAsync(g => g.Id == goalId)
                ?? throw new InvalidOperationException("Goal not found");

            if (goal.CurrentAmount < dto.Amount)
                throw new InvalidOperationException("Insufficient goal balance");

            // Return to child's balance
            goal.Child.CurrentBalance += dto.Amount;
            goal.CurrentAmount -= dto.Amount;

            var contribution = new SavingsContribution
            {
                Id = Guid.NewGuid(),
                GoalId = goalId,
                ChildId = goal.ChildId,
                Amount = -dto.Amount, // Negative for withdrawal
                Type = ContributionType.Withdrawal,
                GoalBalanceAfter = goal.CurrentAmount,
                Description = dto.Reason,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId
            };

            _context.SavingsContributions.Add(contribution);
            goal.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ContributionDto.FromContribution(contribution);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<ContributionDto>> GetContributionsAsync(Guid goalId, ContributionType? type, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.SavingsContributions
            .Include(c => c.CreatedBy)
            .Where(c => c.GoalId == goalId);

        if (type.HasValue)
            query = query.Where(c => c.Type == type.Value);

        if (startDate.HasValue)
            query = query.Where(c => c.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(c => c.CreatedAt <= endDate.Value);

        var contributions = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        return contributions.Select(ContributionDto.FromContribution).ToList();
    }

    public async Task<SavingsGoalDto> MarkAsPurchasedAsync(Guid goalId, MarkGoalPurchasedDto dto, Guid userId)
    {
        var goal = await _context.SavingsGoals
            .Include(g => g.Child).ThenInclude(c => c.User)
            .Include(g => g.Milestones)
            .FirstOrDefaultAsync(g => g.Id == goalId)
            ?? throw new InvalidOperationException("Goal not found");

        if (goal.Status != GoalStatus.Completed)
            throw new InvalidOperationException("Goal must be completed before marking as purchased");

        goal.Status = GoalStatus.Purchased;
        goal.PurchasedAt = DateTime.UtcNow;
        goal.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return SavingsGoalDto.FromGoal(goal);
    }

    #endregion

    #region Auto-Transfer

    public async Task ProcessAutoTransfersAsync(Guid childId, decimal allowanceAmount)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var child = await _context.Children.FindAsync(childId)
                ?? throw new InvalidOperationException("Child not found");

            var activeGoals = await _context.SavingsGoals
                .Where(g => g.ChildId == childId &&
                           g.Status == GoalStatus.Active &&
                           g.AutoTransferType != AutoTransferType.None)
                .OrderBy(g => g.Priority)
                .ToListAsync();

            foreach (var goal in activeGoals)
            {
                if (child.CurrentBalance <= 0) break;

                decimal transferAmount = goal.AutoTransferType switch
                {
                    AutoTransferType.FixedAmount => goal.AutoTransferAmount,
                    AutoTransferType.Percentage => allowanceAmount * (goal.AutoTransferAmount / 100),
                    _ => 0
                };

                // Don't exceed available balance
                transferAmount = Math.Min(transferAmount, child.CurrentBalance);

                // Don't exceed remaining goal amount
                var remainingGoalAmount = goal.TargetAmount - goal.CurrentAmount;
                transferAmount = Math.Min(transferAmount, remainingGoalAmount);

                if (transferAmount > 0)
                {
                    child.CurrentBalance -= transferAmount;
                    goal.CurrentAmount += transferAmount;

                    var contribution = new SavingsContribution
                    {
                        Id = Guid.NewGuid(),
                        GoalId = goal.Id,
                        ChildId = childId,
                        Amount = transferAmount,
                        Type = ContributionType.AutoTransfer,
                        GoalBalanceAfter = goal.CurrentAmount,
                        Description = "Automatic transfer from allowance",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.SavingsContributions.Add(contribution);

                    goal.UpdatedAt = DateTime.UtcNow;

                    _logger?.LogInformation(
                        "Auto-transferred ${Amount} to goal {GoalId} for child {ChildId}",
                        transferAmount, goal.Id, childId);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger?.LogError(ex, "Failed to process auto-transfers for child {ChildId}", childId);
            throw;
        }
    }

    #endregion

    #region Matching Rules

    public async Task<MatchingRuleDto> CreateMatchingRuleAsync(Guid goalId, CreateMatchingRuleDto dto, Guid parentId)
    {
        var goal = await _context.SavingsGoals
            .Include(g => g.MatchingRule)
            .FirstOrDefaultAsync(g => g.Id == goalId)
            ?? throw new InvalidOperationException("Goal not found");

        if (goal.MatchingRule != null)
            throw new InvalidOperationException("Goal already has a matching rule");

        var parent = await _context.Users.FindAsync(parentId)
            ?? throw new InvalidOperationException("Parent not found");

        var rule = new ParentMatchingRule
        {
            Id = Guid.NewGuid(),
            GoalId = goalId,
            CreatedByParentId = parentId,
            Type = dto.Type,
            MatchRatio = dto.MatchRatio,
            MaxMatchAmount = dto.MaxMatchAmount,
            TotalMatchedAmount = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = dto.ExpiresAt
        };

        _context.ParentMatchingRules.Add(rule);
        await _context.SaveChangesAsync();

        // Reload with navigation
        await _context.Entry(rule).Reference(r => r.CreatedByParent).LoadAsync();

        return MatchingRuleDto.FromRule(rule, goal.Name);
    }

    public async Task<MatchingRuleDto?> GetMatchingRuleAsync(Guid goalId)
    {
        var rule = await _context.ParentMatchingRules
            .Include(r => r.CreatedByParent)
            .Include(r => r.Goal)
            .FirstOrDefaultAsync(r => r.GoalId == goalId);

        return rule != null ? MatchingRuleDto.FromRule(rule, rule.Goal.Name) : null;
    }

    public async Task<MatchingRuleDto> UpdateMatchingRuleAsync(Guid goalId, UpdateMatchingRuleDto dto, Guid parentId)
    {
        var rule = await _context.ParentMatchingRules
            .Include(r => r.CreatedByParent)
            .Include(r => r.Goal)
            .FirstOrDefaultAsync(r => r.GoalId == goalId)
            ?? throw new InvalidOperationException("Matching rule not found");

        if (dto.MatchRatio.HasValue) rule.MatchRatio = dto.MatchRatio.Value;
        if (dto.MaxMatchAmount.HasValue) rule.MaxMatchAmount = dto.MaxMatchAmount.Value;
        if (dto.IsActive.HasValue) rule.IsActive = dto.IsActive.Value;
        if (dto.ExpiresAt.HasValue) rule.ExpiresAt = dto.ExpiresAt.Value;

        await _context.SaveChangesAsync();

        return MatchingRuleDto.FromRule(rule, rule.Goal.Name);
    }

    public async Task RemoveMatchingRuleAsync(Guid goalId, Guid parentId)
    {
        var rule = await _context.ParentMatchingRules
            .FirstOrDefaultAsync(r => r.GoalId == goalId)
            ?? throw new InvalidOperationException("Matching rule not found");

        _context.ParentMatchingRules.Remove(rule);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Challenges

    public async Task<GoalChallengeDto> CreateChallengeAsync(Guid goalId, CreateGoalChallengeDto dto, Guid parentId)
    {
        var goal = await _context.SavingsGoals
            .Include(g => g.ActiveChallenge)
            .FirstOrDefaultAsync(g => g.Id == goalId)
            ?? throw new InvalidOperationException("Goal not found");

        if (goal.ActiveChallenge?.Status == ChallengeStatus.Active)
            throw new InvalidOperationException("Goal already has an active challenge");

        var parent = await _context.Users.FindAsync(parentId)
            ?? throw new InvalidOperationException("Parent not found");

        var challenge = new GoalChallenge
        {
            Id = Guid.NewGuid(),
            GoalId = goalId,
            CreatedByParentId = parentId,
            TargetAmount = dto.TargetAmount,
            StartDate = DateTime.UtcNow,
            EndDate = dto.EndDate,
            BonusAmount = dto.BonusAmount,
            Status = ChallengeStatus.Active,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };

        _context.GoalChallenges.Add(challenge);
        await _context.SaveChangesAsync();

        // Reload with navigation
        await _context.Entry(challenge).Reference(c => c.CreatedByParent).LoadAsync();

        return GoalChallengeDto.FromChallenge(challenge, goal.Name, goal.CurrentAmount);
    }

    public async Task<GoalChallengeDto?> GetActiveChallengeAsync(Guid goalId)
    {
        var goal = await _context.SavingsGoals.FindAsync(goalId);
        if (goal == null) return null;

        var challenge = await _context.GoalChallenges
            .Include(c => c.CreatedByParent)
            .FirstOrDefaultAsync(c => c.GoalId == goalId && c.Status == ChallengeStatus.Active);

        return challenge != null
            ? GoalChallengeDto.FromChallenge(challenge, goal.Name, goal.CurrentAmount)
            : null;
    }

    public async Task CancelChallengeAsync(Guid goalId, Guid parentId)
    {
        var challenge = await _context.GoalChallenges
            .FirstOrDefaultAsync(c => c.GoalId == goalId && c.Status == ChallengeStatus.Active)
            ?? throw new InvalidOperationException("No active challenge found");

        challenge.Status = ChallengeStatus.Cancelled;
        await _context.SaveChangesAsync();
    }

    public async Task<List<GoalChallengeDto>> GetChildChallengesAsync(Guid childId, Guid userId)
    {
        var challenges = await _context.GoalChallenges
            .Include(c => c.Goal)
            .Include(c => c.CreatedByParent)
            .Where(c => c.Goal.ChildId == childId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return challenges.Select(c =>
            GoalChallengeDto.FromChallenge(c, c.Goal.Name, c.Goal.CurrentAmount)
        ).ToList();
    }

    public async Task CheckExpiredChallengesAsync()
    {
        var expiredChallenges = await _context.GoalChallenges
            .Where(c => c.Status == ChallengeStatus.Active && c.EndDate < DateTime.UtcNow)
            .ToListAsync();

        foreach (var challenge in expiredChallenges)
        {
            challenge.Status = ChallengeStatus.Failed;
            _logger?.LogInformation("Challenge {ChallengeId} marked as failed (expired)", challenge.Id);
        }

        await _context.SaveChangesAsync();
    }

    #endregion
}
