namespace AllowanceTracker.DTOs;

public record AuthResponseDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    Guid? FamilyId,
    string? FamilyName,
    string Token,
    DateTime ExpiresAt);
