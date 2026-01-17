using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

/// <summary>
/// A thank you note from a child to a gift giver
/// </summary>
public class ThankYouNote : IHasCreatedAt
{
    public Guid Id { get; set; }

    /// <summary>
    /// The gift this thank you note is for
    /// </summary>
    public Guid GiftId { get; set; }

    /// <summary>
    /// The child who wrote the note
    /// </summary>
    public Guid ChildId { get; set; }

    /// <summary>
    /// The thank you message content
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional image URL (e.g., a drawing or photo)
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Whether the note has been sent to the giver
    /// </summary>
    public bool IsSent { get; set; } = false;

    /// <summary>
    /// When the note was sent
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// When the note was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the note was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual Gift Gift { get; set; } = null!;
    public virtual Child Child { get; set; } = null!;
}
