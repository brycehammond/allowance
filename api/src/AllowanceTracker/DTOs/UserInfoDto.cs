namespace AllowanceTracker.DTOs;

public record UserInfoDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    Guid? FamilyId,
    string? FamilyName);
