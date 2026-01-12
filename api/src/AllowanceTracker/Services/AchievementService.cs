using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AllowanceTracker.Services;

/// <summary>
/// Service for managing achievement badges and rewards
/// </summary>
public class AchievementService : IAchievementService
{
    private readonly AllowanceContext _context;

    public AchievementService(AllowanceContext context)
    {
        _context = context;
    }

    #region Badge Queries

    public async Task<List<BadgeDto>> GetAllBadgesAsync(BadgeCategory? category = null, bool includeSecret = false)
    {
        var query = _context.Badges.AsNoTracking().Where(b => b.IsActive);

        if (category.HasValue)
        {
            query = query.Where(b => b.Category == category.Value);
        }

        if (!includeSecret)
        {
            query = query.Where(b => !b.IsSecret);
        }

        var badges = await query.OrderBy(b => b.SortOrder).ThenBy(b => b.Name).ToListAsync();

        return badges.Select(b => ToBadgeDto(b)).ToList();
    }

    public async Task<List<ChildBadgeDto>> GetChildBadgesAsync(Guid childId, BadgeCategory? category = null, bool newOnly = false)
    {
        var query = _context.ChildBadges
            .AsNoTracking()
            .Include(cb => cb.Badge)
            .Where(cb => cb.ChildId == childId);

        if (category.HasValue)
        {
            query = query.Where(cb => cb.Badge.Category == category.Value);
        }

        if (newOnly)
        {
            query = query.Where(cb => cb.IsNew);
        }

        var childBadges = await query.OrderByDescending(cb => cb.EarnedAt).ToListAsync();

        return childBadges.Select(cb => ToChildBadgeDto(cb)).ToList();
    }

    public async Task<List<BadgeProgressDto>> GetBadgeProgressAsync(Guid childId)
    {
        var progressRecords = await _context.BadgeProgressRecords
            .AsNoTracking()
            .Include(p => p.Badge)
            .Where(p => p.ChildId == childId && p.CurrentProgress < p.TargetProgress)
            .OrderByDescending(p => (double)p.CurrentProgress / p.TargetProgress)
            .ToListAsync();

        return progressRecords.Select(p => ToBadgeProgressDto(p)).ToList();
    }

    public async Task<AchievementSummaryDto> GetAchievementSummaryAsync(Guid childId)
    {
        var child = await _context.Children.FindAsync(childId)
            ?? throw new InvalidOperationException("Child not found");

        var totalBadges = await _context.Badges.CountAsync(b => b.IsActive);
        var earnedBadges = await _context.ChildBadges.CountAsync(cb => cb.ChildId == childId);

        var recentBadges = await _context.ChildBadges
            .AsNoTracking()
            .Include(cb => cb.Badge)
            .Where(cb => cb.ChildId == childId)
            .OrderByDescending(cb => cb.EarnedAt)
            .Take(5)
            .ToListAsync();

        var inProgressBadges = await GetBadgeProgressAsync(childId);

        var badgesByCategory = await _context.ChildBadges
            .AsNoTracking()
            .Include(cb => cb.Badge)
            .Where(cb => cb.ChildId == childId)
            .GroupBy(cb => cb.Badge.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category.ToString(), x => x.Count);

        return new AchievementSummaryDto(
            TotalBadges: totalBadges,
            EarnedBadges: earnedBadges,
            TotalPoints: child.TotalPoints,
            AvailablePoints: child.AvailablePoints,
            RecentBadges: recentBadges.Select(cb => ToChildBadgeDto(cb)).ToList(),
            InProgressBadges: inProgressBadges.Take(5).ToList(),
            BadgesByCategory: badgesByCategory
        );
    }

    #endregion

    #region Badge Mutations

    public async Task<ChildBadgeDto> ToggleBadgeDisplayAsync(Guid childId, Guid badgeId, bool isDisplayed)
    {
        var childBadge = await _context.ChildBadges
            .Include(cb => cb.Badge)
            .FirstOrDefaultAsync(cb => cb.ChildId == childId && cb.BadgeId == badgeId)
            ?? throw new InvalidOperationException("Child badge not found");

        childBadge.IsDisplayed = isDisplayed;
        await _context.SaveChangesAsync();

        return ToChildBadgeDto(childBadge);
    }

    public async Task MarkBadgesSeenAsync(Guid childId, List<Guid> badgeIds)
    {
        var childBadges = await _context.ChildBadges
            .Where(cb => cb.ChildId == childId && badgeIds.Contains(cb.BadgeId))
            .ToListAsync();

        foreach (var childBadge in childBadges)
        {
            childBadge.IsNew = false;
        }

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Badge Unlocking

    public async Task<ChildBadgeDto?> TryUnlockBadgeAsync(Guid childId, string badgeCode, string? context = null)
    {
        var badge = await _context.Badges.FirstOrDefaultAsync(b => b.Code == badgeCode && b.IsActive);
        if (badge == null)
        {
            return null;
        }

        // Check if already earned
        var alreadyEarned = await _context.ChildBadges
            .AnyAsync(cb => cb.ChildId == childId && cb.BadgeId == badge.Id);
        if (alreadyEarned)
        {
            return null;
        }

        // Create the child badge
        var childBadge = new ChildBadge
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            BadgeId = badge.Id,
            EarnedAt = DateTime.UtcNow,
            IsDisplayed = true,
            IsNew = true,
            EarnedContext = context
        };
        _context.ChildBadges.Add(childBadge);

        // Award points to child
        var child = await _context.Children.FindAsync(childId)
            ?? throw new InvalidOperationException("Child not found");

        child.TotalPoints += badge.PointsValue;
        child.AvailablePoints += badge.PointsValue;

        await _context.SaveChangesAsync();

        // Load the badge for the DTO
        childBadge.Badge = badge;
        return ToChildBadgeDto(childBadge);
    }

    public async Task CheckAndUnlockBadgesAsync(Guid childId, BadgeTrigger trigger, object? triggerData = null)
    {
        // Get badges that respond to this trigger and haven't been earned
        var earnedBadgeIds = await _context.ChildBadges
            .Where(cb => cb.ChildId == childId)
            .Select(cb => cb.BadgeId)
            .ToListAsync();

        var badges = await _context.Badges
            .Where(b => b.IsActive && !earnedBadgeIds.Contains(b.Id))
            .ToListAsync();

        foreach (var badge in badges)
        {
            // Check if this badge responds to the trigger
            var config = JsonSerializer.Deserialize<BadgeCriteriaConfig>(badge.CriteriaConfig) ?? new BadgeCriteriaConfig();
            if (config.Triggers != null && !config.Triggers.Contains(trigger))
            {
                continue;
            }

            // Evaluate the badge criteria (simplified - full evaluation would be in BadgeCriteriaEvaluator)
            var shouldUnlock = await EvaluateBadgeCriteriaAsync(badge, childId, triggerData);
            if (shouldUnlock)
            {
                var contextJson = triggerData != null ? JsonSerializer.Serialize(triggerData) : null;
                await TryUnlockBadgeAsync(childId, badge.Code, contextJson);
            }
        }
    }

    private async Task<bool> EvaluateBadgeCriteriaAsync(Badge badge, Guid childId, object? triggerData)
    {
        var config = JsonSerializer.Deserialize<BadgeCriteriaConfig>(badge.CriteriaConfig) ?? new BadgeCriteriaConfig();

        return badge.CriteriaType switch
        {
            BadgeCriteriaType.SingleAction => true, // If we're checking, the action occurred
            BadgeCriteriaType.CountThreshold => await EvaluateCountThresholdAsync(config, childId),
            BadgeCriteriaType.AmountThreshold => await EvaluateAmountThresholdAsync(config, childId),
            BadgeCriteriaType.StreakCount => await EvaluateStreakCountAsync(config, childId),
            BadgeCriteriaType.PercentageTarget => await EvaluatePercentageTargetAsync(config, childId, triggerData),
            BadgeCriteriaType.GoalCompletion => await EvaluateGoalCompletionAsync(config, childId),
            BadgeCriteriaType.TimeBasedAction => EvaluateTimeBasedAction(config, triggerData),
            BadgeCriteriaType.Compound => await EvaluateCompoundAsync(config, childId, triggerData),
            _ => false
        };
    }

    private async Task<bool> EvaluateCountThresholdAsync(BadgeCriteriaConfig config, Guid childId)
    {
        if (!config.CountTarget.HasValue)
            return false;

        var count = config.MeasureField switch
        {
            "transaction_count" => await _context.Transactions.CountAsync(t => t.ChildId == childId),
            "task_count" => await _context.TaskCompletions.CountAsync(tc => tc.ChildId == childId),
            _ => 0
        };

        return count >= config.CountTarget.Value;
    }

    private async Task<bool> EvaluateAmountThresholdAsync(BadgeCriteriaConfig config, Guid childId)
    {
        if (!config.AmountTarget.HasValue)
            return false;

        var child = await _context.Children.FindAsync(childId);
        if (child == null)
            return false;

        var amount = config.MeasureField switch
        {
            "current_balance" => child.CurrentBalance,
            "savings_balance" => child.SavingsBalance,
            "total_saved" => await _context.SavingsTransactions
                .Where(st => st.ChildId == childId && st.Type == SavingsTransactionType.Deposit)
                .SumAsync(st => st.Amount),
            _ => child.CurrentBalance
        };

        return amount >= config.AmountTarget.Value;
    }

    private async Task<bool> EvaluateStreakCountAsync(BadgeCriteriaConfig config, Guid childId)
    {
        if (!config.StreakTarget.HasValue)
            return false;

        var child = await _context.Children.FindAsync(childId);
        return child?.SavingStreak >= config.StreakTarget.Value;
    }

    private async Task<bool> EvaluatePercentageTargetAsync(BadgeCriteriaConfig config, Guid childId, object? triggerData)
    {
        if (!config.PercentageTarget.HasValue)
            return false;

        var child = await _context.Children.FindAsync(childId);
        if (child == null || child.WeeklyAllowance <= 0)
            return false;

        // Calculate percentage based on MeasureField
        decimal percentage = 0;

        if (config.MeasureField == "savings_rate" || config.MeasureField == null)
        {
            // Calculate what percentage of allowance was saved
            // If triggerData contains the savings amount, use that
            if (triggerData != null)
            {
                try
                {
                    var json = JsonSerializer.Serialize(triggerData);
                    var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (data != null && data.TryGetValue("Amount", out var amountEl) && amountEl.TryGetDecimal(out var amount))
                    {
                        percentage = (amount / child.WeeklyAllowance) * 100;
                    }
                }
                catch
                {
                    // Fall back to checking savings balance as percentage of total earned
                    var totalEarned = await _context.Transactions
                        .Where(t => t.ChildId == childId && t.Type == TransactionType.Credit)
                        .SumAsync(t => t.Amount);
                    if (totalEarned > 0)
                    {
                        percentage = (child.SavingsBalance / totalEarned) * 100;
                    }
                }
            }
            else
            {
                // Check lifetime savings rate
                var totalEarned = await _context.Transactions
                    .Where(t => t.ChildId == childId && t.Type == TransactionType.Credit)
                    .SumAsync(t => t.Amount);
                if (totalEarned > 0)
                {
                    var totalSaved = await _context.SavingsTransactions
                        .Where(st => st.ChildId == childId && st.Type == SavingsTransactionType.Deposit)
                        .SumAsync(st => st.Amount);
                    percentage = (totalSaved / totalEarned) * 100;
                }
            }
        }

        return percentage >= config.PercentageTarget.Value;
    }

    private async Task<bool> EvaluateGoalCompletionAsync(BadgeCriteriaConfig config, Guid childId)
    {
        var targetGoals = config.GoalTarget ?? config.CountTarget ?? 1;

        var completedGoals = await _context.SavingsGoals
            .CountAsync(g => g.ChildId == childId && g.Status == GoalStatus.Completed);

        return completedGoals >= targetGoals;
    }

    private bool EvaluateTimeBasedAction(BadgeCriteriaConfig config, object? triggerData)
    {
        if (string.IsNullOrEmpty(config.TimeCondition))
            return false;

        var now = DateTime.UtcNow;

        return config.TimeCondition switch
        {
            // Same day as allowance - check if action happened on same day as allowance
            "same_day_as_allowance" => true, // If triggered during allowance, it's same day

            // Weekend saver - action on Saturday or Sunday
            "weekend" => now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday,

            // Early bird - action before 9 AM
            "early_bird" => now.Hour < 9,

            // Night owl - action after 9 PM
            "night_owl" => now.Hour >= 21,

            // Start of month - first 3 days
            "start_of_month" => now.Day <= 3,

            // End of month - last 3 days
            "end_of_month" => now.Day >= DateTime.DaysInMonth(now.Year, now.Month) - 2,

            // Consistent saver - this would typically be tracked via streak
            "consistent" => true,

            _ => false
        };
    }

    private async Task<bool> EvaluateCompoundAsync(BadgeCriteriaConfig config, Guid childId, object? triggerData)
    {
        if (config.SubCriteria == null || config.SubCriteria.Count == 0)
            return false;

        // All sub-criteria must be met
        foreach (var subConfig in config.SubCriteria)
        {
            // Create a temporary badge with the sub-criteria to evaluate
            var subBadge = new Badge
            {
                CriteriaType = DetermineCriteriaType(subConfig),
                CriteriaConfig = JsonSerializer.Serialize(subConfig)
            };

            var subResult = await EvaluateBadgeCriteriaAsync(subBadge, childId, triggerData);
            if (!subResult)
                return false;
        }

        return true;
    }

    private static BadgeCriteriaType DetermineCriteriaType(BadgeCriteriaConfig config)
    {
        // Determine criteria type based on which fields are populated
        if (config.SubCriteria != null && config.SubCriteria.Count > 0)
            return BadgeCriteriaType.Compound;
        if (!string.IsNullOrEmpty(config.TimeCondition))
            return BadgeCriteriaType.TimeBasedAction;
        if (config.GoalTarget.HasValue)
            return BadgeCriteriaType.GoalCompletion;
        if (config.PercentageTarget.HasValue)
            return BadgeCriteriaType.PercentageTarget;
        if (config.StreakTarget.HasValue)
            return BadgeCriteriaType.StreakCount;
        if (config.AmountTarget.HasValue)
            return BadgeCriteriaType.AmountThreshold;
        if (config.CountTarget.HasValue)
            return BadgeCriteriaType.CountThreshold;
        if (!string.IsNullOrEmpty(config.ActionType))
            return BadgeCriteriaType.SingleAction;

        return BadgeCriteriaType.SingleAction;
    }

    #endregion

    #region Points

    public async Task<ChildPointsDto> GetChildPointsAsync(Guid childId)
    {
        var child = await _context.Children.FindAsync(childId)
            ?? throw new InvalidOperationException("Child not found");

        var badgesEarned = await _context.ChildBadges.CountAsync(cb => cb.ChildId == childId);
        var rewardsUnlocked = await _context.ChildRewards.CountAsync(cr => cr.ChildId == childId);
        var spentPoints = child.TotalPoints - child.AvailablePoints;

        return new ChildPointsDto(
            TotalPoints: child.TotalPoints,
            AvailablePoints: child.AvailablePoints,
            SpentPoints: spentPoints,
            BadgesEarned: badgesEarned,
            RewardsUnlocked: rewardsUnlocked
        );
    }

    #endregion

    #region Rewards

    public async Task<List<RewardDto>> GetAvailableRewardsAsync(RewardType? type = null, Guid? childId = null)
    {
        var query = _context.Rewards.AsNoTracking().Where(r => r.IsActive);

        if (type.HasValue)
        {
            query = query.Where(r => r.Type == type.Value);
        }

        var rewards = await query.OrderBy(r => r.SortOrder).ThenBy(r => r.PointsCost).ToListAsync();

        int availablePoints = 0;
        HashSet<Guid> unlockedRewardIds = new();

        if (childId.HasValue)
        {
            var child = await _context.Children.FindAsync(childId.Value);
            availablePoints = child?.AvailablePoints ?? 0;

            unlockedRewardIds = (await _context.ChildRewards
                .Where(cr => cr.ChildId == childId.Value)
                .Select(cr => cr.RewardId)
                .ToListAsync())
                .ToHashSet();
        }

        return rewards.Select(r => ToRewardDto(r, unlockedRewardIds.Contains(r.Id), false, availablePoints >= r.PointsCost)).ToList();
    }

    public async Task<List<RewardDto>> GetChildRewardsAsync(Guid childId)
    {
        var childRewards = await _context.ChildRewards
            .AsNoTracking()
            .Include(cr => cr.Reward)
            .Where(cr => cr.ChildId == childId)
            .OrderByDescending(cr => cr.UnlockedAt)
            .ToListAsync();

        var child = await _context.Children.FindAsync(childId);

        return childRewards.Select(cr => ToRewardDto(cr.Reward, true, cr.IsEquipped, true)).ToList();
    }

    public async Task<RewardDto> UnlockRewardAsync(Guid childId, Guid rewardId)
    {
        var child = await _context.Children.FindAsync(childId)
            ?? throw new InvalidOperationException("Child not found");

        var reward = await _context.Rewards.FindAsync(rewardId)
            ?? throw new InvalidOperationException("Reward not found");

        // Check if already unlocked
        var alreadyUnlocked = await _context.ChildRewards
            .AnyAsync(cr => cr.ChildId == childId && cr.RewardId == rewardId);
        if (alreadyUnlocked)
        {
            throw new InvalidOperationException("Reward already unlocked");
        }

        // Check if enough points
        if (child.AvailablePoints < reward.PointsCost)
        {
            throw new InvalidOperationException("Child has insufficient points");
        }

        // Deduct points
        child.AvailablePoints -= reward.PointsCost;

        // Create child reward
        var childReward = new ChildReward
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            RewardId = rewardId,
            UnlockedAt = DateTime.UtcNow,
            IsEquipped = false
        };
        _context.ChildRewards.Add(childReward);

        await _context.SaveChangesAsync();

        return ToRewardDto(reward, true, false, true);
    }

    public async Task<RewardDto> EquipRewardAsync(Guid childId, Guid rewardId)
    {
        var childReward = await _context.ChildRewards
            .Include(cr => cr.Reward)
            .FirstOrDefaultAsync(cr => cr.ChildId == childId && cr.RewardId == rewardId)
            ?? throw new InvalidOperationException("Child reward not found");

        var reward = childReward.Reward;

        // Unequip any previously equipped reward of the same type
        var previouslyEquipped = await _context.ChildRewards
            .Include(cr => cr.Reward)
            .Where(cr => cr.ChildId == childId && cr.Reward.Type == reward.Type && cr.IsEquipped)
            .ToListAsync();

        foreach (var prev in previouslyEquipped)
        {
            prev.IsEquipped = false;
        }

        // Equip the new reward
        childReward.IsEquipped = true;

        // Update child's equipped value
        var child = await _context.Children.FindAsync(childId)!;
        switch (reward.Type)
        {
            case RewardType.Avatar:
                child!.EquippedAvatarUrl = reward.Value;
                break;
            case RewardType.Theme:
                child!.EquippedTheme = reward.Value;
                break;
            case RewardType.Title:
                child!.EquippedTitle = reward.Value;
                break;
        }

        await _context.SaveChangesAsync();

        return ToRewardDto(reward, true, true, true);
    }

    public async Task UnequipRewardAsync(Guid childId, Guid rewardId)
    {
        var childReward = await _context.ChildRewards
            .Include(cr => cr.Reward)
            .FirstOrDefaultAsync(cr => cr.ChildId == childId && cr.RewardId == rewardId)
            ?? throw new InvalidOperationException("Child reward not found");

        childReward.IsEquipped = false;

        // Clear the equipped value on the child
        var child = await _context.Children.FindAsync(childId)!;
        switch (childReward.Reward.Type)
        {
            case RewardType.Avatar:
                child!.EquippedAvatarUrl = null;
                break;
            case RewardType.Theme:
                child!.EquippedTheme = null;
                break;
            case RewardType.Title:
                child!.EquippedTitle = null;
                break;
        }

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Progress Tracking

    public async Task UpdateProgressAsync(Guid childId, string badgeCode, int increment = 1)
    {
        var badge = await _context.Badges.FirstOrDefaultAsync(b => b.Code == badgeCode);
        if (badge == null) return;

        var progress = await _context.BadgeProgressRecords
            .FirstOrDefaultAsync(p => p.ChildId == childId && p.BadgeId == badge.Id);

        if (progress == null)
        {
            // Create new progress record
            var config = JsonSerializer.Deserialize<BadgeCriteriaConfig>(badge.CriteriaConfig) ?? new BadgeCriteriaConfig();
            var target = config.CountTarget ?? config.GoalTarget ?? 10;

            progress = new BadgeProgress
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                BadgeId = badge.Id,
                CurrentProgress = 0,
                TargetProgress = target
            };
            _context.BadgeProgressRecords.Add(progress);
        }

        progress.CurrentProgress = Math.Min(progress.CurrentProgress + increment, progress.TargetProgress);
        progress.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task SetProgressAsync(Guid childId, string badgeCode, int value)
    {
        var badge = await _context.Badges.FirstOrDefaultAsync(b => b.Code == badgeCode);
        if (badge == null) return;

        var progress = await _context.BadgeProgressRecords
            .FirstOrDefaultAsync(p => p.ChildId == childId && p.BadgeId == badge.Id);

        if (progress == null)
        {
            // Create new progress record
            var config = JsonSerializer.Deserialize<BadgeCriteriaConfig>(badge.CriteriaConfig) ?? new BadgeCriteriaConfig();
            var target = config.CountTarget ?? (config.AmountTarget.HasValue ? (int)config.AmountTarget.Value : 10);

            progress = new BadgeProgress
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                BadgeId = badge.Id,
                CurrentProgress = 0,
                TargetProgress = target
            };
            _context.BadgeProgressRecords.Add(progress);
        }

        progress.CurrentProgress = Math.Min(value, progress.TargetProgress);
        progress.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    #endregion

    #region DTO Mapping

    private static BadgeDto ToBadgeDto(Badge badge, bool isEarned = false, DateTime? earnedAt = null, bool isDisplayed = true, int? currentProgress = null, int? targetProgress = null)
    {
        double? progressPercentage = null;
        if (currentProgress.HasValue && targetProgress.HasValue && targetProgress.Value > 0)
        {
            progressPercentage = (double)currentProgress.Value / targetProgress.Value * 100;
        }

        return new BadgeDto(
            Id: badge.Id,
            Code: badge.Code,
            Name: badge.Name,
            Description: badge.Description,
            IconUrl: badge.IconUrl,
            Category: badge.Category,
            CategoryName: badge.Category.ToString(),
            Rarity: badge.Rarity,
            RarityName: badge.Rarity.ToString(),
            PointsValue: badge.PointsValue,
            IsSecret: badge.IsSecret,
            IsEarned: isEarned,
            EarnedAt: earnedAt,
            IsDisplayed: isDisplayed,
            CurrentProgress: currentProgress,
            TargetProgress: targetProgress,
            ProgressPercentage: progressPercentage
        );
    }

    private static ChildBadgeDto ToChildBadgeDto(ChildBadge childBadge)
    {
        return new ChildBadgeDto(
            Id: childBadge.Id,
            BadgeId: childBadge.BadgeId,
            BadgeName: childBadge.Badge.Name,
            BadgeDescription: childBadge.Badge.Description,
            IconUrl: childBadge.Badge.IconUrl,
            Category: childBadge.Badge.Category,
            CategoryName: childBadge.Badge.Category.ToString(),
            Rarity: childBadge.Badge.Rarity,
            RarityName: childBadge.Badge.Rarity.ToString(),
            PointsValue: childBadge.Badge.PointsValue,
            EarnedAt: childBadge.EarnedAt,
            IsDisplayed: childBadge.IsDisplayed,
            IsNew: childBadge.IsNew,
            EarnedContext: childBadge.EarnedContext
        );
    }

    private static BadgeProgressDto ToBadgeProgressDto(BadgeProgress progress)
    {
        var percentage = progress.TargetProgress > 0
            ? (double)progress.CurrentProgress / progress.TargetProgress * 100
            : 0;

        var progressText = $"{progress.CurrentProgress}/{progress.TargetProgress}";

        return new BadgeProgressDto(
            BadgeId: progress.BadgeId,
            BadgeName: progress.Badge.Name,
            Description: progress.Badge.Description,
            IconUrl: progress.Badge.IconUrl,
            Category: progress.Badge.Category,
            CategoryName: progress.Badge.Category.ToString(),
            Rarity: progress.Badge.Rarity,
            RarityName: progress.Badge.Rarity.ToString(),
            PointsValue: progress.Badge.PointsValue,
            CurrentProgress: progress.CurrentProgress,
            TargetProgress: progress.TargetProgress,
            ProgressPercentage: percentage,
            ProgressText: progressText
        );
    }

    private static RewardDto ToRewardDto(Reward reward, bool isUnlocked, bool isEquipped, bool canAfford)
    {
        return new RewardDto(
            Id: reward.Id,
            Name: reward.Name,
            Description: reward.Description,
            Type: reward.Type,
            TypeName: reward.Type.ToString(),
            Value: reward.Value,
            PreviewUrl: reward.PreviewUrl,
            PointsCost: reward.PointsCost,
            IsUnlocked: isUnlocked,
            IsEquipped: isEquipped,
            CanAfford: canAfford
        );
    }

    #endregion
}
