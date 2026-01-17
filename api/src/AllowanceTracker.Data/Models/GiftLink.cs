using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

/// <summary>
/// A shareable link that allows external family members to send gifts to a child
/// </summary>
public class GiftLink : IHasCreatedAt
{
    public Guid Id { get; set; }

    /// <summary>
    /// The child this gift link is for
    /// </summary>
    public Guid ChildId { get; set; }

    /// <summary>
    /// The family this gift link belongs to
    /// </summary>
    public Guid FamilyId { get; set; }

    /// <summary>
    /// The parent who created this link
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    /// Secure URL-safe token for accessing the gift portal
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the link (e.g., "Birthday 2024", "Grandparents Link")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description or notes about the link
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Controls what information is visible to gift givers
    /// </summary>
    public GiftLinkVisibility Visibility { get; set; } = GiftLinkVisibility.Minimal;

    /// <summary>
    /// Whether the link is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the link expires (null = never expires)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Maximum number of times this link can be used (null = unlimited)
    /// </summary>
    public int? MaxUses { get; set; }

    /// <summary>
    /// Number of times this link has been used
    /// </summary>
    public int UseCount { get; set; } = 0;

    /// <summary>
    /// Minimum gift amount allowed (null = no minimum)
    /// </summary>
    public decimal? MinAmount { get; set; }

    /// <summary>
    /// Maximum gift amount allowed (null = no maximum)
    /// </summary>
    public decimal? MaxAmount { get; set; }

    /// <summary>
    /// Default occasion for gifts through this link (can be overridden by giver)
    /// </summary>
    public GiftOccasion? DefaultOccasion { get; set; }

    /// <summary>
    /// When the link was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the link was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual Child Child { get; set; } = null!;
    public virtual Family Family { get; set; } = null!;
    public virtual ApplicationUser CreatedBy { get; set; } = null!;
    public virtual ICollection<Gift> Gifts { get; set; } = new List<Gift>();
}
