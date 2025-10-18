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
/// <param name="LastAllowanceDate">Date when the child last received their allowance (null if never received)</param>
/// <param name="AllowanceDay">Optional day of week for scheduled allowance payments (null for rolling 7-day window)</param>
public record ChildDto(
    Guid Id,
    string FirstName,
    string LastName,
    decimal WeeklyAllowance,
    decimal CurrentBalance,
    DateTime? LastAllowanceDate,
    DayOfWeek? AllowanceDay)
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
            child.LastAllowanceDate,
            child.AllowanceDay);
    }
}
