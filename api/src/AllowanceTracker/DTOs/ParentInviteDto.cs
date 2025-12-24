using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs;

/// <summary>
/// DTO for listing pending invites
/// </summary>
public record ParentInviteDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsExistingUser,
    InviteStatus Status,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    string InvitedByName);
