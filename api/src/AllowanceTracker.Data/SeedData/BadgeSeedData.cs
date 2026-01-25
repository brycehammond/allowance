using AllowanceTracker.Models;
using System.Text.Json;

namespace AllowanceTracker.Data.SeedData;

/// <summary>
/// Badge criteria configuration for seed data
/// </summary>
public class SeedBadgeCriteriaConfig
{
    public string? ActionType { get; set; }
    public int? CountTarget { get; set; }
    public decimal? AmountTarget { get; set; }
    public int? StreakTarget { get; set; }
    public int? PercentageTarget { get; set; }
    public int? GoalTarget { get; set; }
    public string? TimeCondition { get; set; }
    public string? MeasureField { get; set; }
    public List<BadgeTrigger>? Triggers { get; set; }
}

/// <summary>
/// Seed data for achievement badges
/// </summary>
public static class BadgeSeedData
{
    // Static seed date to avoid PendingModelChangesWarning in EF Core
    private static readonly DateTime SeedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static List<Badge> GetBadges()
    {
        var badges = new List<Badge>();
        int sortOrder = 0;

        // Saving Badges
        badges.AddRange(new[]
        {
            CreateBadge("FIRST_SAVER", "First Saver", "Made your first deposit to savings",
                BadgeCategory.Saving, BadgeRarity.Common, 10, BadgeCriteriaType.SingleAction,
                new SeedBadgeCriteriaConfig { ActionType = "first_savings_deposit", Triggers = new() { BadgeTrigger.SavingsDeposit } },
                sortOrder++),

            CreateBadge("PENNY_PINCHER", "Penny Pincher", "Saved $10 total",
                BadgeCategory.Saving, BadgeRarity.Common, 15, BadgeCriteriaType.AmountThreshold,
                new SeedBadgeCriteriaConfig { AmountTarget = 10, MeasureField = "total_saved", Triggers = new() { BadgeTrigger.SavingsDeposit } },
                sortOrder++),

            CreateBadge("MONEY_STACKER", "Money Stacker", "Saved $50 total",
                BadgeCategory.Saving, BadgeRarity.Uncommon, 25, BadgeCriteriaType.AmountThreshold,
                new SeedBadgeCriteriaConfig { AmountTarget = 50, MeasureField = "total_saved", Triggers = new() { BadgeTrigger.SavingsDeposit } },
                sortOrder++),

            CreateBadge("SAVINGS_STAR", "Savings Star", "Saved $100 total",
                BadgeCategory.Saving, BadgeRarity.Rare, 50, BadgeCriteriaType.AmountThreshold,
                new SeedBadgeCriteriaConfig { AmountTarget = 100, MeasureField = "total_saved", Triggers = new() { BadgeTrigger.SavingsDeposit } },
                sortOrder++),

            CreateBadge("SAVINGS_CHAMPION", "Savings Champion", "Saved $500 total",
                BadgeCategory.Saving, BadgeRarity.Epic, 100, BadgeCriteriaType.AmountThreshold,
                new SeedBadgeCriteriaConfig { AmountTarget = 500, MeasureField = "total_saved", Triggers = new() { BadgeTrigger.SavingsDeposit } },
                sortOrder++),

            CreateBadge("EARLY_BIRD", "Early Bird", "Saved on the same day as allowance",
                BadgeCategory.Saving, BadgeRarity.Uncommon, 20, BadgeCriteriaType.TimeBasedAction,
                new SeedBadgeCriteriaConfig { TimeCondition = "same_day_as_allowance", Triggers = new() { BadgeTrigger.SavingsDeposit } },
                sortOrder++),

            CreateBadge("SUPER_SAVER", "Super Saver", "Saved 50% of allowance in a month",
                BadgeCategory.Saving, BadgeRarity.Rare, 40, BadgeCriteriaType.PercentageTarget,
                new SeedBadgeCriteriaConfig { PercentageTarget = 50, MeasureField = "monthly_savings_rate", Triggers = new() { BadgeTrigger.StreakUpdated } },
                sortOrder++),

            CreateBadge("FRUGAL_MASTER", "Frugal Master", "Saved 75% of allowance in a month",
                BadgeCategory.Saving, BadgeRarity.Epic, 75, BadgeCriteriaType.PercentageTarget,
                new SeedBadgeCriteriaConfig { PercentageTarget = 75, MeasureField = "monthly_savings_rate", Triggers = new() { BadgeTrigger.StreakUpdated } },
                sortOrder++),
        });

        // Goals Badges
        badges.AddRange(new[]
        {
            CreateBadge("GOAL_SETTER", "Goal Setter", "Created your first savings goal",
                BadgeCategory.Goals, BadgeRarity.Common, 10, BadgeCriteriaType.SingleAction,
                new SeedBadgeCriteriaConfig { ActionType = "first_goal_created", Triggers = new() { BadgeTrigger.GoalCreated } },
                sortOrder++),

            CreateBadge("GOAL_CRUSHER", "Goal Crusher", "Completed your first savings goal",
                BadgeCategory.Goals, BadgeRarity.Common, 20, BadgeCriteriaType.GoalCompletion,
                new SeedBadgeCriteriaConfig { GoalTarget = 1, Triggers = new() { BadgeTrigger.GoalCompleted } },
                sortOrder++),

            CreateBadge("DREAM_ACHIEVER", "Dream Achiever", "Completed 5 savings goals",
                BadgeCategory.Goals, BadgeRarity.Rare, 50, BadgeCriteriaType.GoalCompletion,
                new SeedBadgeCriteriaConfig { GoalTarget = 5, Triggers = new() { BadgeTrigger.GoalCompleted } },
                sortOrder++),

            CreateBadge("GOAL_MACHINE", "Goal Machine", "Completed 10 savings goals",
                BadgeCategory.Goals, BadgeRarity.Epic, 100, BadgeCriteriaType.GoalCompletion,
                new SeedBadgeCriteriaConfig { GoalTarget = 10, Triggers = new() { BadgeTrigger.GoalCompleted } },
                sortOrder++),

            // WISHLIST_WINNER badge removed - wish list feature has been consolidated into savings goals
        });

        // Chores Badges
        badges.AddRange(new[]
        {
            CreateBadge("HELPER", "Helper", "Completed your first task",
                BadgeCategory.Chores, BadgeRarity.Common, 10, BadgeCriteriaType.SingleAction,
                new SeedBadgeCriteriaConfig { ActionType = "first_task_completed", Triggers = new() { BadgeTrigger.TaskCompleted } },
                sortOrder++),

            CreateBadge("HARD_WORKER", "Hard Worker", "Completed 10 tasks",
                BadgeCategory.Chores, BadgeRarity.Common, 20, BadgeCriteriaType.CountThreshold,
                new SeedBadgeCriteriaConfig { CountTarget = 10, MeasureField = "task_count", Triggers = new() { BadgeTrigger.TaskApproved } },
                sortOrder++),

            CreateBadge("CHORE_CHAMPION", "Chore Champion", "Completed 50 tasks",
                BadgeCategory.Chores, BadgeRarity.Rare, 50, BadgeCriteriaType.CountThreshold,
                new SeedBadgeCriteriaConfig { CountTarget = 50, MeasureField = "task_count", Triggers = new() { BadgeTrigger.TaskApproved } },
                sortOrder++),

            CreateBadge("TASK_MASTER", "Task Master", "Completed 100 tasks",
                BadgeCategory.Chores, BadgeRarity.Epic, 100, BadgeCriteriaType.CountThreshold,
                new SeedBadgeCriteriaConfig { CountTarget = 100, MeasureField = "task_count", Triggers = new() { BadgeTrigger.TaskApproved } },
                sortOrder++),

            CreateBadge("PERFECT_RECORD", "Perfect Record", "Had 10 tasks approved in a row",
                BadgeCategory.Chores, BadgeRarity.Rare, 40, BadgeCriteriaType.StreakCount,
                new SeedBadgeCriteriaConfig { StreakTarget = 10, MeasureField = "approved_task_streak", Triggers = new() { BadgeTrigger.TaskApproved } },
                sortOrder++),
        });

        // Streaks Badges
        badges.AddRange(new[]
        {
            CreateBadge("STREAK_STARTER", "Streak Starter", "Saved for 2 weeks in a row",
                BadgeCategory.Streaks, BadgeRarity.Common, 15, BadgeCriteriaType.StreakCount,
                new SeedBadgeCriteriaConfig { StreakTarget = 2, MeasureField = "saving_streak", Triggers = new() { BadgeTrigger.StreakUpdated } },
                sortOrder++),

            CreateBadge("CONSISTENCY_KING", "Consistency King", "Saved for 4 weeks in a row",
                BadgeCategory.Streaks, BadgeRarity.Uncommon, 30, BadgeCriteriaType.StreakCount,
                new SeedBadgeCriteriaConfig { StreakTarget = 4, MeasureField = "saving_streak", Triggers = new() { BadgeTrigger.StreakUpdated } },
                sortOrder++),

            CreateBadge("STREAK_MASTER", "Streak Master", "Saved for 10 weeks in a row",
                BadgeCategory.Streaks, BadgeRarity.Rare, 60, BadgeCriteriaType.StreakCount,
                new SeedBadgeCriteriaConfig { StreakTarget = 10, MeasureField = "saving_streak", Triggers = new() { BadgeTrigger.StreakUpdated } },
                sortOrder++),

            CreateBadge("UNSTOPPABLE", "Unstoppable", "Saved for 26 weeks in a row",
                BadgeCategory.Streaks, BadgeRarity.Epic, 100, BadgeCriteriaType.StreakCount,
                new SeedBadgeCriteriaConfig { StreakTarget = 26, MeasureField = "saving_streak", Triggers = new() { BadgeTrigger.StreakUpdated } },
                sortOrder++),

            CreateBadge("LEGENDARY_STREAK", "Legendary Streak", "Saved for 52 weeks in a row",
                BadgeCategory.Streaks, BadgeRarity.Legendary, 200, BadgeCriteriaType.StreakCount,
                new SeedBadgeCriteriaConfig { StreakTarget = 52, MeasureField = "saving_streak", Triggers = new() { BadgeTrigger.StreakUpdated } },
                sortOrder++),
        });

        // Milestones Badges
        badges.AddRange(new[]
        {
            CreateBadge("FIRST_PURCHASE", "First Purchase", "Made your first transaction",
                BadgeCategory.Milestones, BadgeRarity.Common, 5, BadgeCriteriaType.SingleAction,
                new SeedBadgeCriteriaConfig { ActionType = "first_transaction", Triggers = new() { BadgeTrigger.TransactionCreated } },
                sortOrder++),

            CreateBadge("DOUBLE_DIGITS", "Double Digits", "Reached $10 balance",
                BadgeCategory.Milestones, BadgeRarity.Common, 10, BadgeCriteriaType.AmountThreshold,
                new SeedBadgeCriteriaConfig { AmountTarget = 10, MeasureField = "current_balance", Triggers = new() { BadgeTrigger.BalanceChanged } },
                sortOrder++),

            CreateBadge("FIFTY_CLUB", "Fifty Club", "Reached $50 balance",
                BadgeCategory.Milestones, BadgeRarity.Uncommon, 25, BadgeCriteriaType.AmountThreshold,
                new SeedBadgeCriteriaConfig { AmountTarget = 50, MeasureField = "current_balance", Triggers = new() { BadgeTrigger.BalanceChanged } },
                sortOrder++),

            CreateBadge("CENTURY_CLUB", "Century Club", "Reached $100 balance",
                BadgeCategory.Milestones, BadgeRarity.Rare, 50, BadgeCriteriaType.AmountThreshold,
                new SeedBadgeCriteriaConfig { AmountTarget = 100, MeasureField = "current_balance", Triggers = new() { BadgeTrigger.BalanceChanged } },
                sortOrder++),

            CreateBadge("HIGH_ROLLER", "High Roller", "Reached $500 balance",
                BadgeCategory.Milestones, BadgeRarity.Epic, 100, BadgeCriteriaType.AmountThreshold,
                new SeedBadgeCriteriaConfig { AmountTarget = 500, MeasureField = "current_balance", Triggers = new() { BadgeTrigger.BalanceChanged } },
                sortOrder++),
        });

        // Spending Badges
        badges.AddRange(new[]
        {
            CreateBadge("BUDGET_AWARE", "Budget Aware", "Stayed under budget for a week",
                BadgeCategory.Spending, BadgeRarity.Common, 15, BadgeCriteriaType.StreakCount,
                new SeedBadgeCriteriaConfig { StreakTarget = 1, MeasureField = "budget_streak", Triggers = new() { BadgeTrigger.BudgetChecked } },
                sortOrder++),

            CreateBadge("BUDGET_BOSS", "Budget Boss", "Stayed under budget for 4 weeks",
                BadgeCategory.Spending, BadgeRarity.Rare, 50, BadgeCriteriaType.StreakCount,
                new SeedBadgeCriteriaConfig { StreakTarget = 4, MeasureField = "budget_streak", Triggers = new() { BadgeTrigger.BudgetChecked } },
                sortOrder++),

            CreateBadge("SMART_SPENDER", "Smart Spender", "Tracked 50 transactions",
                BadgeCategory.Spending, BadgeRarity.Uncommon, 25, BadgeCriteriaType.CountThreshold,
                new SeedBadgeCriteriaConfig { CountTarget = 50, MeasureField = "transaction_count", Triggers = new() { BadgeTrigger.TransactionCreated } },
                sortOrder++),

            CreateBadge("TRANSACTION_TRACKER", "Transaction Tracker", "Tracked 200 transactions",
                BadgeCategory.Spending, BadgeRarity.Rare, 50, BadgeCriteriaType.CountThreshold,
                new SeedBadgeCriteriaConfig { CountTarget = 200, MeasureField = "transaction_count", Triggers = new() { BadgeTrigger.TransactionCreated } },
                sortOrder++),
        });

        // Special Badges
        badges.AddRange(new[]
        {
            CreateBadge("WELCOME", "Welcome", "Joined the app",
                BadgeCategory.Special, BadgeRarity.Common, 5, BadgeCriteriaType.SingleAction,
                new SeedBadgeCriteriaConfig { ActionType = "account_created", Triggers = new() { BadgeTrigger.AccountCreated } },
                sortOrder++),

            CreateBadge("BIRTHDAY_BONUS", "Birthday Bonus", "Received a gift on your birthday",
                BadgeCategory.Special, BadgeRarity.Uncommon, 25, BadgeCriteriaType.SingleAction,
                new SeedBadgeCriteriaConfig { ActionType = "birthday_gift", Triggers = new() { BadgeTrigger.TransactionCreated } },
                sortOrder++, isSecret: true),

            CreateBadge("GENEROUS_HEART", "Generous Heart", "Gave money to a sibling",
                BadgeCategory.Special, BadgeRarity.Rare, 40, BadgeCriteriaType.SingleAction,
                new SeedBadgeCriteriaConfig { ActionType = "sibling_transfer", Triggers = new() { BadgeTrigger.TransactionCreated } },
                sortOrder++),

            CreateBadge("FAMILY_FIRST", "Family First", "Part of a family savings goal",
                BadgeCategory.Special, BadgeRarity.Rare, 40, BadgeCriteriaType.SingleAction,
                new SeedBadgeCriteriaConfig { ActionType = "family_goal_participant", Triggers = new() { BadgeTrigger.GoalCreated } },
                sortOrder++),
        });

        return badges;
    }

    public static List<Reward> GetRewards()
    {
        var rewards = new List<Reward>();
        int sortOrder = 0;

        // Avatar Rewards
        rewards.AddRange(new[]
        {
            CreateReward("Cool Cat", "A stylish cat avatar", RewardType.Avatar,
                "avatars/cool-cat.png", "/previews/cool-cat.png", 25, sortOrder++),
            CreateReward("Super Star", "A shining star avatar", RewardType.Avatar,
                "avatars/super-star.png", "/previews/super-star.png", 50, sortOrder++),
            CreateReward("Money Dragon", "A dragon guarding treasure", RewardType.Avatar,
                "avatars/money-dragon.png", "/previews/money-dragon.png", 100, sortOrder++),
            CreateReward("Piggy Pro", "A professional piggy bank", RewardType.Avatar,
                "avatars/piggy-pro.png", "/previews/piggy-pro.png", 75, sortOrder++),
            CreateReward("Coin Collector", "A coin collector character", RewardType.Avatar,
                "avatars/coin-collector.png", "/previews/coin-collector.png", 150, sortOrder++),
        });

        // Theme Rewards
        rewards.AddRange(new[]
        {
            CreateReward("Ocean Blue", "A calming ocean theme", RewardType.Theme,
                "theme-ocean", "/previews/theme-ocean.png", 50, sortOrder++),
            CreateReward("Forest Green", "A refreshing forest theme", RewardType.Theme,
                "theme-forest", "/previews/theme-forest.png", 50, sortOrder++),
            CreateReward("Sunset Orange", "A warm sunset theme", RewardType.Theme,
                "theme-sunset", "/previews/theme-sunset.png", 75, sortOrder++),
            CreateReward("Galaxy Purple", "A cosmic galaxy theme", RewardType.Theme,
                "theme-galaxy", "/previews/theme-galaxy.png", 100, sortOrder++),
            CreateReward("Golden Luxury", "A premium gold theme", RewardType.Theme,
                "theme-gold", "/previews/theme-gold.png", 200, sortOrder++),
        });

        // Title Rewards
        rewards.AddRange(new[]
        {
            CreateReward("Saver", "The 'Saver' title", RewardType.Title,
                "Saver", null, 25, sortOrder++),
            CreateReward("Budget Master", "The 'Budget Master' title", RewardType.Title,
                "Budget Master", null, 50, sortOrder++),
            CreateReward("Money Expert", "The 'Money Expert' title", RewardType.Title,
                "Money Expert", null, 100, sortOrder++),
            CreateReward("Financial Wizard", "The 'Financial Wizard' title", RewardType.Title,
                "Financial Wizard", null, 150, sortOrder++),
            CreateReward("Legendary Investor", "The 'Legendary Investor' title", RewardType.Title,
                "Legendary Investor", null, 300, sortOrder++),
        });

        // Profile Frame Rewards
        rewards.AddRange(new[]
        {
            CreateReward("Bronze Frame", "A bronze profile frame", RewardType.ProfileFrame,
                "frame-bronze", "/previews/frame-bronze.png", 30, sortOrder++),
            CreateReward("Silver Frame", "A silver profile frame", RewardType.ProfileFrame,
                "frame-silver", "/previews/frame-silver.png", 60, sortOrder++),
            CreateReward("Gold Frame", "A gold profile frame", RewardType.ProfileFrame,
                "frame-gold", "/previews/frame-gold.png", 100, sortOrder++),
            CreateReward("Diamond Frame", "A diamond profile frame", RewardType.ProfileFrame,
                "frame-diamond", "/previews/frame-diamond.png", 200, sortOrder++),
        });

        return rewards;
    }

    private static Badge CreateBadge(
        string code,
        string name,
        string description,
        BadgeCategory category,
        BadgeRarity rarity,
        int pointsValue,
        BadgeCriteriaType criteriaType,
        SeedBadgeCriteriaConfig criteriaConfig,
        int sortOrder,
        bool isSecret = false)
    {
        return new Badge
        {
            Id = GenerateGuidFromCode(code),
            Code = code,
            Name = name,
            Description = description,
            IconUrl = $"/badges/{code.ToLower().Replace("_", "-")}.png",
            Category = category,
            Rarity = rarity,
            PointsValue = pointsValue,
            CriteriaType = criteriaType,
            CriteriaConfig = JsonSerializer.Serialize(criteriaConfig),
            IsSecret = isSecret,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = SeedDate
        };
    }

    private static Reward CreateReward(
        string name,
        string description,
        RewardType type,
        string value,
        string? previewUrl,
        int pointsCost,
        int sortOrder)
    {
        return new Reward
        {
            Id = GenerateGuidFromCode($"REWARD_{name.ToUpper().Replace(" ", "_")}"),
            Name = name,
            Description = description,
            Type = type,
            Value = value,
            PreviewUrl = previewUrl,
            PointsCost = pointsCost,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = SeedDate
        };
    }

    /// <summary>
    /// Generate a deterministic GUID from a string code for consistent seeding
    /// </summary>
    private static Guid GenerateGuidFromCode(string code)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(code));
        return new Guid(hash);
    }
}
