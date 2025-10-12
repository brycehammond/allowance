using AllowanceTracker.Data;
using AllowanceTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AllowanceContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Ensure database is created
        await context.Database.MigrateAsync();

        // Check if we already have data
        if (await context.Users.AnyAsync())
        {
            return; // Database has been seeded
        }

        // Create test family
        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = "Test Family",
            CreatedAt = DateTime.UtcNow
        };

        context.Families.Add(family);
        await context.SaveChangesAsync();

        // Create test parent user
        var parentUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "parent@test.com",
            UserName = "parent@test.com",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Parent,
            FamilyId = family.Id,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(parentUser, "Test123!");

        if (result.Succeeded)
        {
            Console.WriteLine("Test user created successfully!");
            Console.WriteLine("Email: parent@test.com");
            Console.WriteLine("Password: Test123!");
        }
        else
        {
            Console.WriteLine("Failed to create test user:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  - {error.Description}");
            }
        }
    }
}
