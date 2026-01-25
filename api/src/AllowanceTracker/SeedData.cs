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

        // Check if demo user already exists
        var existingUser = await userManager.FindByEmailAsync("demo@earnandlearn.app");
        if (existingUser != null)
        {
            Console.WriteLine("Demo user already exists. Skipping seed.");
            return;
        }

        Console.WriteLine("Creating demo test data...");

        // Create parent user first (without family)
        var parentUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "demo@earnandlearn.app",
            UserName = "demo@earnandlearn.app",
            FirstName = "Demo",
            LastName = "Parent",
            Role = UserRole.Parent,
            FamilyId = null,  // Will set after family is created
            EmailConfirmed = true
        };

        var parentResult = await userManager.CreateAsync(parentUser, "Demo123!");
        if (!parentResult.Succeeded)
        {
            Console.WriteLine("Failed to create parent user:");
            foreach (var error in parentResult.Errors)
            {
                Console.WriteLine($"  - {error.Description}");
            }
            return;
        }

        Console.WriteLine("Created parent: demo@earnandlearn.app / Demo123!");

        // Create family now that parent exists
        var familyId = Guid.NewGuid();
        var family = new Family
        {
            Id = familyId,
            Name = "Demo Family",
            OwnerId = parentUser.Id,
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        };

        context.Families.Add(family);
        await context.SaveChangesAsync();

        // Update parent's family reference
        parentUser.FamilyId = familyId;
        await userManager.UpdateAsync(parentUser);

        // Get system user for automated transactions
        var systemUserId = Data.Constants.SystemUserId;

        // Create Child 1: Emma (10 years old, $15/week, savings enabled)
        var child1User = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "emma.demo@earnandlearn.app",
            UserName = "emma.demo@earnandlearn.app",
            FirstName = "Emma",
            LastName = "Demo",
            Role = UserRole.Child,
            FamilyId = familyId,
            EmailConfirmed = true
        };

        var child1Result = await userManager.CreateAsync(child1User, "Emma123!");
        if (!child1Result.Succeeded)
        {
            Console.WriteLine("Failed to create Emma user");
            return;
        }

        var child1 = new Child
        {
            Id = Guid.NewGuid(),
            UserId = child1User.Id,
            FamilyId = familyId,
            WeeklyAllowance = 15.00m,
            CurrentBalance = 0,
            AllowanceDay = DayOfWeek.Saturday,
            SavingsAccountEnabled = true,
            SavingsBalance = 25.00m,
            SavingsTransferType = SavingsTransferType.Percentage,
            SavingsTransferPercentage = 20,
            SavingsBalanceVisibleToChild = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        };

        context.Children.Add(child1);
        await context.SaveChangesAsync();

        Console.WriteLine("Created child: Emma ($15/week, savings enabled)");

        // Create Child 2: Jack (8 years old, $10/week)
        var child2User = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "jack.demo@earnandlearn.app",
            UserName = "jack.demo@earnandlearn.app",
            FirstName = "Jack",
            LastName = "Demo",
            Role = UserRole.Child,
            FamilyId = familyId,
            EmailConfirmed = true
        };

        var child2Result = await userManager.CreateAsync(child2User, "Jack123!");
        if (!child2Result.Succeeded)
        {
            Console.WriteLine("Failed to create Jack user");
            return;
        }

        var child2 = new Child
        {
            Id = Guid.NewGuid(),
            UserId = child2User.Id,
            FamilyId = familyId,
            WeeklyAllowance = 10.00m,
            CurrentBalance = 0,
            AllowanceDay = DayOfWeek.Saturday,
            SavingsAccountEnabled = false,
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        };

        context.Children.Add(child2);
        await context.SaveChangesAsync();

        Console.WriteLine("Created child: Jack ($10/week)");

        // Create transactions for Emma (12 weeks of history)
        var emmaBalance = 0m;
        var emmaTransactions = new List<Transaction>();

        // Helper to add transaction
        void AddEmmaTransaction(decimal amount, TransactionType type, TransactionCategory category,
            string description, DateTime date, Guid createdById)
        {
            emmaBalance += type == TransactionType.Credit ? amount : -amount;
            emmaTransactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                ChildId = child1.Id,
                Amount = amount,
                Type = type,
                Category = category,
                Description = description,
                BalanceAfter = emmaBalance,
                CreatedById = createdById,
                CreatedAt = date
            });
        }

        var baseDate = DateTime.UtcNow;

        // Week 1 (12 weeks ago)
        AddEmmaTransaction(15.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-84), systemUserId);
        AddEmmaTransaction(5.99m, TransactionType.Debit, TransactionCategory.Snacks,
            "Ice cream at the mall", baseDate.AddDays(-82), parentUser.Id);

        // Week 2
        AddEmmaTransaction(15.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-77), systemUserId);
        AddEmmaTransaction(12.99m, TransactionType.Debit, TransactionCategory.Books,
            "Harry Potter book", baseDate.AddDays(-74), parentUser.Id);

        // Week 3
        AddEmmaTransaction(15.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-70), systemUserId);
        AddEmmaTransaction(20.00m, TransactionType.Credit, TransactionCategory.Gift,
            "Birthday money from Grandma", baseDate.AddDays(-69), parentUser.Id);
        AddEmmaTransaction(8.50m, TransactionType.Debit, TransactionCategory.Toys,
            "LEGO minifigures", baseDate.AddDays(-66), parentUser.Id);

        // Week 4
        AddEmmaTransaction(15.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-63), systemUserId);

        // Week 5
        AddEmmaTransaction(15.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-56), systemUserId);
        AddEmmaTransaction(4.25m, TransactionType.Debit, TransactionCategory.Candy,
            "Candy from the store", baseDate.AddDays(-54), parentUser.Id);
        AddEmmaTransaction(5.00m, TransactionType.Credit, TransactionCategory.Chores,
            "Helped wash the car", baseDate.AddDays(-51), parentUser.Id);

        // Week 6
        AddEmmaTransaction(15.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-49), systemUserId);
        AddEmmaTransaction(19.99m, TransactionType.Debit, TransactionCategory.Games,
            "Minecraft game", baseDate.AddDays(-46), parentUser.Id);

        // Week 7
        AddEmmaTransaction(15.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-42), systemUserId);
        AddEmmaTransaction(6.50m, TransactionType.Debit, TransactionCategory.Entertainment,
            "Movie snacks", baseDate.AddDays(-36), parentUser.Id);

        // Week 8
        AddEmmaTransaction(15.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-35), systemUserId);
        AddEmmaTransaction(3.75m, TransactionType.Debit, TransactionCategory.Snacks,
            "Smoothie", baseDate.AddDays(-31), parentUser.Id);

        // Week 9
        AddEmmaTransaction(15.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-28), systemUserId);
        AddEmmaTransaction(10.00m, TransactionType.Credit, TransactionCategory.Chores,
            "Mowed the lawn", baseDate.AddDays(-26), parentUser.Id);
        AddEmmaTransaction(15.00m, TransactionType.Debit, TransactionCategory.Crafts,
            "Art supplies", baseDate.AddDays(-23), parentUser.Id);

        // Week 10
        AddEmmaTransaction(15.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-21), systemUserId);
        AddEmmaTransaction(7.99m, TransactionType.Debit, TransactionCategory.Books,
            "Comic book", baseDate.AddDays(-18), parentUser.Id);

        // Week 11
        AddEmmaTransaction(15.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-14), systemUserId);
        AddEmmaTransaction(11.00m, TransactionType.Debit, TransactionCategory.Toys,
            "Stuffed animal", baseDate.AddDays(-10), parentUser.Id);

        // Week 12 (last week)
        AddEmmaTransaction(15.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-7), systemUserId);
        AddEmmaTransaction(5.00m, TransactionType.Debit, TransactionCategory.Charity,
            "Donated to animal shelter", baseDate.AddDays(-5), parentUser.Id);

        // This week
        AddEmmaTransaction(15.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-3), systemUserId);
        AddEmmaTransaction(4.50m, TransactionType.Debit, TransactionCategory.Snacks,
            "Hot chocolate", baseDate.AddDays(-1), parentUser.Id);

        context.Transactions.AddRange(emmaTransactions);

        // Update Emma's balance and last allowance date
        child1.CurrentBalance = emmaBalance;
        child1.LastAllowanceDate = baseDate.AddDays(-3);

        Console.WriteLine($"Created {emmaTransactions.Count} transactions for Emma. Balance: ${emmaBalance:F2}");

        // Create transactions for Jack (9 weeks of history)
        var jackBalance = 0m;
        var jackTransactions = new List<Transaction>();

        void AddJackTransaction(decimal amount, TransactionType type, TransactionCategory category,
            string description, DateTime date, Guid createdById)
        {
            jackBalance += type == TransactionType.Credit ? amount : -amount;
            jackTransactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                ChildId = child2.Id,
                Amount = amount,
                Type = type,
                Category = category,
                Description = description,
                BalanceAfter = jackBalance,
                CreatedById = createdById,
                CreatedAt = date
            });
        }

        // Week 1 (8 weeks ago)
        AddJackTransaction(10.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-56), systemUserId);
        AddJackTransaction(7.99m, TransactionType.Debit, TransactionCategory.Toys,
            "Hot Wheels cars", baseDate.AddDays(-53), parentUser.Id);

        // Week 2
        AddJackTransaction(10.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-49), systemUserId);
        AddJackTransaction(3.50m, TransactionType.Debit, TransactionCategory.Candy,
            "Candy", baseDate.AddDays(-47), parentUser.Id);

        // Week 3
        AddJackTransaction(10.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-42), systemUserId);
        AddJackTransaction(5.00m, TransactionType.Credit, TransactionCategory.Chores,
            "Cleaned his room", baseDate.AddDays(-38), parentUser.Id);
        AddJackTransaction(9.99m, TransactionType.Debit, TransactionCategory.Games,
            "Roblox Robux", baseDate.AddDays(-37), parentUser.Id);

        // Week 4
        AddJackTransaction(10.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-35), systemUserId);

        // Week 5
        AddJackTransaction(10.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-28), systemUserId);
        AddJackTransaction(6.00m, TransactionType.Debit, TransactionCategory.Sports,
            "Basketball cards", baseDate.AddDays(-25), parentUser.Id);

        // Week 6
        AddJackTransaction(10.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-21), systemUserId);
        AddJackTransaction(10.00m, TransactionType.Credit, TransactionCategory.Gift,
            "Tooth fairy", baseDate.AddDays(-20), parentUser.Id);
        AddJackTransaction(8.00m, TransactionType.Debit, TransactionCategory.Snacks,
            "Pizza with friends", baseDate.AddDays(-16), parentUser.Id);

        // Week 7
        AddJackTransaction(10.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-14), systemUserId);
        AddJackTransaction(4.99m, TransactionType.Debit, TransactionCategory.Toys,
            "Nerf darts", baseDate.AddDays(-10), parentUser.Id);

        // Week 8 (last week)
        AddJackTransaction(10.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-7), systemUserId);

        // This week
        AddJackTransaction(10.00m, TransactionType.Credit, TransactionCategory.Allowance,
            "Weekly allowance", baseDate.AddDays(-3), systemUserId);
        AddJackTransaction(3.25m, TransactionType.Debit, TransactionCategory.Candy,
            "Gummy bears", baseDate.AddDays(-2), parentUser.Id);

        context.Transactions.AddRange(jackTransactions);

        // Update Jack's balance and last allowance date
        child2.CurrentBalance = jackBalance;
        child2.LastAllowanceDate = baseDate.AddDays(-3);

        Console.WriteLine($"Created {jackTransactions.Count} transactions for Jack. Balance: ${jackBalance:F2}");


        // Create savings transactions for Emma
        var savingsTransactions = new List<SavingsTransaction>
        {
            new() { Id = Guid.NewGuid(), ChildId = child1.Id, Amount = 10.00m,
                Type = SavingsTransactionType.Deposit, Description = "Initial savings deposit",
                BalanceAfter = 10.00m, CreatedById = parentUser.Id,
                CreatedAt = baseDate.AddMonths(-2) },
            new() { Id = Guid.NewGuid(), ChildId = child1.Id, Amount = 5.00m,
                Type = SavingsTransactionType.Deposit, Description = "Birthday money to savings",
                BalanceAfter = 15.00m, CreatedById = parentUser.Id,
                CreatedAt = baseDate.AddMonths(-1) },
            new() { Id = Guid.NewGuid(), ChildId = child1.Id, Amount = 10.00m,
                Type = SavingsTransactionType.Deposit, Description = "Saved from allowance",
                BalanceAfter = 25.00m, CreatedById = parentUser.Id,
                CreatedAt = baseDate.AddDays(-14) }
        };

        context.SavingsTransactions.AddRange(savingsTransactions);

        Console.WriteLine($"Created {savingsTransactions.Count} savings transactions for Emma");

        await context.SaveChangesAsync();

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("Demo data created successfully!");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine("Demo Accounts:");
        Console.WriteLine("  Parent: demo@earnandlearn.app / Demo123!");
        Console.WriteLine("  Emma:   emma.demo@earnandlearn.app / Emma123!");
        Console.WriteLine("  Jack:   jack.demo@earnandlearn.app / Jack123!");
        Console.WriteLine();
        Console.WriteLine("Children:");
        Console.WriteLine($"  Emma - ${child1.WeeklyAllowance}/week, Balance: ${child1.CurrentBalance:F2}, Savings: ${child1.SavingsBalance:F2}");
        Console.WriteLine($"  Jack - ${child2.WeeklyAllowance}/week, Balance: ${child2.CurrentBalance:F2}");
    }
}
