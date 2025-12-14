using AllowanceTracker.DTOs;

namespace AllowanceTracker.Services;

/// <summary>
/// Service for managing parent invitations
/// </summary>
public interface IParentInviteService
{
    /// <summary>
    /// Send an invite to a co-parent
    /// </summary>
    /// <param name="dto">Invite details</param>
    /// <param name="inviterId">ID of the parent sending the invite</param>
    /// <param name="familyId">ID of the family to invite to</param>
    /// <returns>Invite response with details</returns>
    Task<ParentInviteResponseDto> SendInviteAsync(SendParentInviteDto dto, Guid inviterId, Guid familyId);

    /// <summary>
    /// Validate an invite token
    /// </summary>
    /// <param name="token">Invite token</param>
    /// <param name="email">Email address</param>
    /// <returns>Validation result with invite details if valid</returns>
    Task<ValidateInviteResponseDto> ValidateTokenAsync(string token, string email);

    /// <summary>
    /// Accept an invite and set password (for new users)
    /// </summary>
    /// <param name="dto">Accept invite details with password</param>
    /// <returns>Auth response with JWT token for auto-login</returns>
    Task<AuthResponseDto> AcceptNewUserInviteAsync(AcceptInviteDto dto);

    /// <summary>
    /// Accept a family join request (for existing users)
    /// </summary>
    /// <param name="token">Invite token</param>
    /// <param name="currentUserId">ID of the current logged-in user</param>
    /// <returns>Join response with family details</returns>
    Task<AcceptJoinResponseDto> AcceptJoinRequestAsync(string token, Guid currentUserId);

    /// <summary>
    /// Cancel a pending invite
    /// </summary>
    /// <param name="inviteId">ID of the invite to cancel</param>
    /// <param name="userId">ID of the user requesting cancellation</param>
    /// <returns>True if cancelled successfully</returns>
    Task<bool> CancelInviteAsync(Guid inviteId, Guid userId);

    /// <summary>
    /// Get all pending invites for a family
    /// </summary>
    /// <param name="familyId">Family ID</param>
    /// <returns>List of pending invites</returns>
    Task<List<ParentInviteDto>> GetPendingInvitesAsync(Guid familyId);

    /// <summary>
    /// Mark expired invites as expired (for background job)
    /// </summary>
    /// <returns>Number of invites that were expired</returns>
    Task<int> ExpireOldInvitesAsync();

    /// <summary>
    /// Resend an existing invite with a new token and extended expiration
    /// </summary>
    /// <param name="inviteId">ID of the invite to resend</param>
    /// <param name="userId">ID of the user requesting the resend</param>
    /// <returns>Updated invite response</returns>
    Task<ParentInviteResponseDto> ResendInviteAsync(Guid inviteId, Guid userId);
}
