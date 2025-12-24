using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

public enum InviteStatus
{
    Pending,
    Accepted,
    Expired,
    Cancelled
}

public class ParentInvite : IHasCreatedAt
{
    public Guid Id { get; set; }

    /// <summary>
    /// The email of the person being invited
    /// </summary>
    public string InvitedEmail { get; set; } = string.Empty;

    /// <summary>
    /// First name provided by the inviter (used for new user scenario)
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name provided by the inviter (used for new user scenario)
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// The family they are being invited to join
    /// </summary>
    public Guid FamilyId { get; set; }

    /// <summary>
    /// The parent who sent the invite
    /// </summary>
    public Guid InvitedById { get; set; }

    /// <summary>
    /// Secure token for accepting the invite
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Whether this invite is for an existing user (join request) or new user (registration)
    /// </summary>
    public bool IsExistingUser { get; set; }

    /// <summary>
    /// If existing user, reference to their user ID (for join request scenario)
    /// </summary>
    public Guid? ExistingUserId { get; set; }

    /// <summary>
    /// Current status of the invite
    /// </summary>
    public InviteStatus Status { get; set; } = InviteStatus.Pending;

    /// <summary>
    /// When the invite expires (default 7 days from creation)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the invite was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the invite was accepted (null if not yet accepted)
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    // Navigation properties
    public virtual Family Family { get; set; } = null!;
    public virtual ApplicationUser InvitedBy { get; set; } = null!;
    public virtual ApplicationUser? ExistingUser { get; set; }
}
