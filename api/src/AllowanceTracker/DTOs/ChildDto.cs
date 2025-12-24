using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs;

/// <summary>
/// Child profile information including allowance and balance details
/// </summary>
/// <param name="Id">Unique identifier for the child</param>
/// <param name="FirstName">Child's first name</param>
/// <param name="LastName">Child's last name</param>
/// <param name="WeeklyAllowance">Amount of allowance the child receives each week</param>
/// <param name="CurrentBalance">Child's current balance available to spend</param>
/// <param name="SavingsBalance">Child's current savings balance</param>
/// <param name="LastAllowanceDate">Date when the child last received their allowance (null if never received)</param>
/// <param name="AllowanceDay">Optional day of week for scheduled allowance payments (null for rolling 7-day window)</param>
/// <param name="SavingsAccountEnabled">Whether the savings account feature is enabled for this child</param>
/// <param name="SavingsTransferType">Type of automatic savings transfer (None, Percentage, or FixedAmount)</param>
/// <param name="SavingsTransferPercentage">Percentage of allowance to transfer to savings (if using Percentage type)</param>
/// <param name="SavingsTransferAmount">Fixed dollar amount to transfer to savings (if using FixedAmount type)</param>
/// <param name="SavingsBalanceVisibleToChild">Whether the child can see their savings balance</param>
/// <param name="AllowDebt">Whether the child's spending balance can go negative</param>
public record ChildDto(
    Guid Id,
    string FirstName,
    string LastName,
    decimal WeeklyAllowance,
    decimal CurrentBalance,
    decimal SavingsBalance,
    DateTime? LastAllowanceDate,
    DayOfWeek? AllowanceDay,
    bool SavingsAccountEnabled,
    SavingsTransferType SavingsTransferType,
    decimal? SavingsTransferPercentage,
    decimal? SavingsTransferAmount,
    bool SavingsBalanceVisibleToChild,
    bool AllowDebt)
{
    /// <summary>
    /// Creates a ChildDto from a Child entity and associated ApplicationUser
    /// </summary>
    public static ChildDto FromChild(Child child, ApplicationUser user)
    {
        return new ChildDto(
            child.Id,
            user.FirstName,
            user.LastName,
            child.WeeklyAllowance,
            child.CurrentBalance,
            child.SavingsBalance,
            child.LastAllowanceDate,
            child.AllowanceDay,
            child.SavingsAccountEnabled,
            child.SavingsTransferType,
            child.SavingsTransferPercentage,
            child.SavingsTransferAmount,
            child.SavingsBalanceVisibleToChild,
            child.AllowDebt);
    }
}
