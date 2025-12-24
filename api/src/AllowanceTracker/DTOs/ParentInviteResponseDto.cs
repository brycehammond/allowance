namespace AllowanceTracker.DTOs;

/// <summary>
/// Response after successfully sending a parent invite
/// </summary>
public record ParentInviteResponseDto(
    Guid InviteId,
    string Email,
    string FirstName,
    string LastName,
    bool IsExistingUser,
    DateTime ExpiresAt,
    string Message);
