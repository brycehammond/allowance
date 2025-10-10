using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs;

public record ChildDto(
    Guid Id,
    string FirstName,
    string LastName,
    decimal WeeklyAllowance,
    decimal CurrentBalance,
    DateTime? LastAllowanceDate)
{
    public static ChildDto FromChild(Child child, ApplicationUser user)
    {
        return new ChildDto(
            child.Id,
            user.FirstName,
            user.LastName,
            child.WeeklyAllowance,
            child.CurrentBalance,
            child.LastAllowanceDate);
    }
}
