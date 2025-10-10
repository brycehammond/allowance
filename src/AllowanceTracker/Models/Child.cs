using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

public class Child : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FamilyId { get; set; }
    public decimal WeeklyAllowance { get; set; } = 0;
    public decimal CurrentBalance { get; set; } = 0;
    public DateTime? LastAllowanceDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Family Family { get; set; } = null!;
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<WishListItem> WishListItems { get; set; } = new List<WishListItem>();
}
