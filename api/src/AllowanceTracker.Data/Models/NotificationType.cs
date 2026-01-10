namespace AllowanceTracker.Models;

public enum NotificationType
{
    // Balance & Transactions
    BalanceAlert = 1,
    LowBalanceWarning = 2,
    TransactionCreated = 3,

    // Allowance
    AllowanceDeposit = 10,
    AllowancePaused = 11,
    AllowanceResumed = 12,

    // Goals & Savings
    GoalProgress = 20,
    GoalMilestone = 21,
    GoalCompleted = 22,
    ParentMatchAdded = 23,

    // Tasks
    TaskAssigned = 30,
    TaskReminder = 31,
    TaskCompleted = 32,
    ApprovalRequired = 33,
    TaskApproved = 34,
    TaskRejected = 35,
    TaskCompletionPendingApproval = 36,

    // Budget
    BudgetWarning = 40,
    BudgetExceeded = 41,

    // Achievements
    AchievementUnlocked = 50,
    StreakUpdate = 51,

    // Family
    FamilyInvite = 60,
    ChildAdded = 61,
    GiftReceived = 62,
    FamilyUpdate = 63,

    // System
    WeeklySummary = 70,
    MonthlySummary = 71,
    SystemAnnouncement = 99
}
