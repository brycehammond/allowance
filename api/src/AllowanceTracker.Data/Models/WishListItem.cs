using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

public class WishListItem : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Url { get; set; }
    public string? Notes { get; set; }
    public bool IsPurchased { get; set; } = false;
    public DateTime? PurchasedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Child Child { get; set; } = null!;

    // Computed property
    public bool CanAfford(decimal balance) => balance >= Price;
}
