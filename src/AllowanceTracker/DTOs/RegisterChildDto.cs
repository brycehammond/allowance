namespace AllowanceTracker.DTOs;

public record RegisterChildDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    decimal WeeklyAllowance);
