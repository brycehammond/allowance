namespace AllowanceTracker.DTOs;

public record FamilyMemberDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsOwner);
