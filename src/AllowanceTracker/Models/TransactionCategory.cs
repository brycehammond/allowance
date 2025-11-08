namespace AllowanceTracker.Models;

/// <summary>
/// Categories for transaction classification
/// </summary>
public enum TransactionCategory
{
    // Income categories (1-9)
    Allowance = 1,
    Chores = 2,
    Gift = 3,
    BonusReward = 4,
    Task = 5,
    OtherIncome = 6,

    // Spending categories (10-29)
    Toys = 10,
    Games = 11,
    Books = 12,
    Clothes = 13,
    Snacks = 14,
    Candy = 15,
    Electronics = 16,
    Entertainment = 17,
    Sports = 18,
    Crafts = 19,
    OtherSpending = 20,

    // Savings & Giving categories (30-39)
    Savings = 30,
    Charity = 31,
    Investment = 32
}
