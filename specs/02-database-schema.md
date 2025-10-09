# Database Schema - Entity Framework Core (MVP)

## Overview
Simple PostgreSQL database design using Entity Framework Core with code-first migrations. Focus on getting it working quickly with good-enough performance.

## Entity Models

### User (extends IdentityUser)
```csharp
using Microsoft.AspNetCore.Identity;

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

public enum UserRole
{
    Parent = 0,
    Child = 1
}
```

### Family
```csharp
public class Family
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
    public virtual ICollection<Child> Children { get; set; } = new List<Child>();
}
```

### Child
```csharp
public class Child
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
```

### Transaction
```csharp
public class Transaction
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal BalanceAfter { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Child Child { get; set; } = null!;
    public virtual ApplicationUser CreatedBy { get; set; } = null!;
}

public enum TransactionType
{
    Credit = 0,
    Debit = 1
}
```

### WishListItem
```csharp
public class WishListItem
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
```

## DbContext Configuration

### AllowanceContext.cs
```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AllowanceContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AllowanceContext(DbContextOptions<AllowanceContext> options)
        : base(options)
    {
    }

    public DbSet<Family> Families { get; set; }
    public DbSet<Child> Children { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<WishListItem> WishListItems { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Family configuration
        builder.Entity<Family>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name);
        });

        // User configuration
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.HasOne(e => e.Family)
                  .WithMany(f => f.Members)
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Child configuration
        builder.Entity<Child>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WeeklyAllowance).HasPrecision(10, 2);
            entity.Property(e => e.CurrentBalance).HasPrecision(10, 2);

            entity.HasOne(e => e.User)
                  .WithOne(u => u.ChildProfile)
                  .HasForeignKey<Child>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Family)
                  .WithMany(f => f.Children)
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.FamilyId);
        });

        // Transaction configuration
        builder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.BalanceAfter).HasPrecision(10, 2);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);

            entity.HasOne(e => e.Child)
                  .WithMany(c => c.Transactions)
                  .HasForeignKey(e => e.ChildId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedBy)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ChildId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // WishListItem configuration
        builder.Entity<WishListItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.Child)
                  .WithMany(c => c.WishListItems)
                  .HasForeignKey(e => e.ChildId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ChildId);
        });
    }

    // Override SaveChanges to handle CreatedAt automatically
    public override int SaveChanges()
    {
        AddTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void AddTimestamps()
    {
        var entities = ChangeTracker.Entries()
            .Where(x => x.Entity is IHasCreatedAt && x.State == EntityState.Added);

        foreach (var entity in entities)
        {
            ((IHasCreatedAt)entity.Entity).CreatedAt = DateTime.UtcNow;
        }
    }
}

// Interface for entities with CreatedAt
public interface IHasCreatedAt
{
    DateTime CreatedAt { get; set; }
}
```

## Connection String Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=allowance_tracker;Username=postgres;Password=yourpassword"
  }
}
```

### Program.cs Setup
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Entity Framework with PostgreSQL
builder.Services.AddDbContext<AllowanceContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false; // MVP simplification
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<AllowanceContext>();
```

## Migrations

### Initial Migration Commands
```bash
# Install EF Core tools (one time)
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update

# Create migration after model changes
dotnet ef migrations add AddWishListItems

# Generate SQL script (for production)
dotnet ef migrations script
```

## Performance Optimizations (Simple)

### Query Optimization Examples
```csharp
// Use AsNoTracking for read-only queries
public async Task<List<Transaction>> GetRecentTransactions(Guid childId)
{
    return await _context.Transactions
        .AsNoTracking()
        .Where(t => t.ChildId == childId)
        .OrderByDescending(t => t.CreatedAt)
        .Take(20)
        .ToListAsync();
}

// Use Include to avoid N+1 queries
public async Task<Family?> GetFamilyWithChildren(Guid familyId)
{
    return await _context.Families
        .Include(f => f.Children)
            .ThenInclude(c => c.User)
        .FirstOrDefaultAsync(f => f.Id == familyId);
}

// Use compiled queries for hot paths
private static readonly Func<AllowanceContext, Guid, Task<Child?>> GetChildByIdQuery =
    EF.CompileAsyncQuery((AllowanceContext context, Guid childId) =>
        context.Children.FirstOrDefault(c => c.Id == childId));

public Task<Child?> GetChildById(Guid childId)
{
    return GetChildByIdQuery(_context, childId);
}
```

## Data Integrity Rules

### Business Logic in Services (Not Database)
Keep it simple for MVP - enforce rules in service layer:

```csharp
public class TransactionService
{
    public async Task<Transaction> CreateTransaction(CreateTransactionDto dto)
    {
        var child = await _context.Children.FindAsync(dto.ChildId);

        // Check balance for debits
        if (dto.Type == TransactionType.Debit && child.CurrentBalance < dto.Amount)
        {
            throw new InvalidOperationException("Insufficient funds");
        }

        // Update balance
        if (dto.Type == TransactionType.Credit)
            child.CurrentBalance += dto.Amount;
        else
            child.CurrentBalance -= dto.Amount;

        // Create transaction with snapshot
        var transaction = new Transaction
        {
            ChildId = dto.ChildId,
            Amount = dto.Amount,
            Type = dto.Type,
            Description = dto.Description,
            BalanceAfter = child.CurrentBalance,
            CreatedById = _currentUser.Id
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return transaction;
    }
}
```

## Seeding Test Data

### Data/SeedData.cs
```csharp
public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var context = new AllowanceContext(
            serviceProvider.GetRequiredService<DbContextOptions<AllowanceContext>>());

        // Check if already seeded
        if (context.Families.Any())
            return;

        // Create test family
        var family = new Family { Name = "Test Family" };
        context.Families.Add(family);

        // Create parent user (use UserManager for this in real code)
        // Create child users
        // Add some transactions

        await context.SaveChangesAsync();
    }
}
```

## Backup Strategy (Simple)

### PostgreSQL Backup Commands
```bash
# Backup
pg_dump allowance_tracker > backup_$(date +%Y%m%d).sql

# Restore
psql allowance_tracker < backup_20240101.sql
```

## Common Queries

### Get Child with Balance
```csharp
var child = await _context.Children
    .Include(c => c.User)
    .FirstOrDefaultAsync(c => c.Id == childId);
```

### Get This Week's Transactions
```csharp
var startOfWeek = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
var transactions = await _context.Transactions
    .Where(t => t.ChildId == childId && t.CreatedAt >= startOfWeek)
    .OrderByDescending(t => t.CreatedAt)
    .ToListAsync();
```

### Check Affordable Wish List Items
```csharp
var affordableItems = await _context.WishListItems
    .Where(w => w.ChildId == childId &&
                !w.IsPurchased &&
                w.Price <= child.CurrentBalance)
    .ToListAsync();
```