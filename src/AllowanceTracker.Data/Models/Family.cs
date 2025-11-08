using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

public class Family : IHasCreatedAt
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
    public virtual ICollection<Child> Children { get; set; } = new List<Child>();
}
