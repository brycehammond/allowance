using Microsoft.AspNetCore.Identity;

namespace AllowanceTracker.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Parent;
    public Guid? FamilyId { get; set; }

    // Navigation properties
    public virtual Family? Family { get; set; }
    public virtual Child? ChildProfile { get; set; }

    // Computed property
    public string FullName => $"{FirstName} {LastName}";
}
