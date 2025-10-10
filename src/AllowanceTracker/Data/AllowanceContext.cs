using AllowanceTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Data;

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
