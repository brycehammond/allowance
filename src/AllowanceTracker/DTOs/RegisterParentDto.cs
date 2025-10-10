namespace AllowanceTracker.DTOs;

public record RegisterParentDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string FamilyName);
