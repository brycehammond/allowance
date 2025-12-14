using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

public class Family : IHasCreatedAt
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The account owner who has full control over the family
    /// </summary>
    public Guid OwnerId { get; set; }

    // Navigation properties
    public virtual ApplicationUser Owner { get; set; } = null!;
    public virtual ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
    public virtual ICollection<Child> Children { get; set; } = new List<Child>();
    public virtual ICollection<ParentInvite> Invitations { get; set; } = new List<ParentInvite>();
}
