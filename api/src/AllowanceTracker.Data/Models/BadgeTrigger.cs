namespace AllowanceTracker.Models;

/// <summary>
/// Events that can trigger badge evaluation
/// </summary>
public enum BadgeTrigger
{
    TransactionCreated = 1,
    SavingsDeposit = 2,
    GoalCreated = 3,
    GoalCompleted = 4,
    TaskCompleted = 5,
    TaskApproved = 6,
    AllowanceReceived = 7,
    BalanceChanged = 8,
    StreakUpdated = 9,
    BudgetChecked = 10,
    AccountCreated = 11,
    [Obsolete("Wish list feature removed")]
    WishListPurchased = 12
}
