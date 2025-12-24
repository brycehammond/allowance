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
    public DbSet<CategoryBudget> CategoryBudgets { get; set; }
    public DbSet<SavingsTransaction> SavingsTransactions { get; set; }
    public DbSet<ChoreTask> Tasks { get; set; }
    public DbSet<TaskCompletion> TaskCompletions { get; set; }
    public DbSet<AllowanceAdjustment> AllowanceAdjustments { get; set; }
    public DbSet<ParentInvite> ParentInvites { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Family configuration
        builder.Entity<Family>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name);

            // Owner relationship - the parent who owns/controls the family
            entity.HasOne(e => e.Owner)
                  .WithMany()
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Invitations relationship
            entity.HasMany(e => e.Invitations)
                  .WithOne(i => i.Family)
                  .HasForeignKey(i => i.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);
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

            // Seed system user for automated/scheduled operations (e.g., weekly allowance)
            entity.HasData(new ApplicationUser
            {
                Id = Constants.SystemUserId,
                FirstName = "Earn",
                LastName = "& Learn",
                Email = "system@allowancetracker.local",
                NormalizedEmail = "SYSTEM@ALLOWANCETRACKER.LOCAL",
                UserName = "system@allowancetracker.local",
                NormalizedUserName = "SYSTEM@ALLOWANCETRACKER.LOCAL",
                EmailConfirmed = true,
                SecurityStamp = "SYSTEM-SECURITY-STAMP",
                ConcurrencyStamp = "SYSTEM-CONCURRENCY-STAMP",
                Role = UserRole.Parent
            });
        });

        // Child configuration
        builder.Entity<Child>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WeeklyAllowance).HasPrecision(10, 2);
            entity.Property(e => e.CurrentBalance).HasPrecision(10, 2);

            // Savings account properties
            entity.Property(e => e.SavingsBalance).HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(e => e.SavingsTransferAmount).HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(e => e.SavingsAccountEnabled).HasDefaultValue(false);
            entity.Property(e => e.SavingsTransferType).HasDefaultValue(SavingsTransferType.None);
            entity.Property(e => e.SavingsTransferPercentage).HasDefaultValue(0);

            // Allowance pause properties
            entity.Property(e => e.AllowancePaused).HasDefaultValue(false);
            entity.Property(e => e.AllowancePausedReason).HasMaxLength(500);

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
            entity.HasIndex(e => e.Category);
        });

        // CategoryBudget configuration
        builder.Entity<CategoryBudget>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Limit).HasPrecision(10, 2);

            entity.HasOne(e => e.Child)
                  .WithMany(c => c.CategoryBudgets)
                  .HasForeignKey(e => e.ChildId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedBy)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            // Unique index: one budget per category per child
            entity.HasIndex(e => new { e.ChildId, e.Category }).IsUnique();
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

        // SavingsTransaction configuration
        builder.Entity<SavingsTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.BalanceAfter).HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);

            entity.HasOne(e => e.Child)
                  .WithMany(c => c.SavingsTransactions)
                  .HasForeignKey(e => e.ChildId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedBy)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ChildId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Type);
        });

        // ChoreTask configuration
        builder.Entity<ChoreTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.RewardAmount).HasPrecision(10, 2).IsRequired();

            entity.HasOne(e => e.Child)
                  .WithMany(c => c.Tasks)
                  .HasForeignKey(e => e.ChildId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedBy)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ChildId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedById);
            entity.HasIndex(e => e.CreatedAt);
        });

        // TaskCompletion configuration
        builder.Entity<TaskCompletion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.PhotoUrl).HasMaxLength(2048);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);

            entity.HasOne(e => e.Task)
                  .WithMany(t => t.Completions)
                  .HasForeignKey(e => e.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Child)
                  .WithMany(c => c.TaskCompletions)
                  .HasForeignKey(e => e.ChildId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ApprovedBy)
                  .WithMany()
                  .HasForeignKey(e => e.ApprovedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Transaction)
                  .WithOne()
                  .HasForeignKey<TaskCompletion>(e => e.TransactionId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.ChildId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CompletedAt);
        });

        // AllowanceAdjustment configuration
        builder.Entity<AllowanceAdjustment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OldAmount).HasPrecision(10, 2);
            entity.Property(e => e.NewAmount).HasPrecision(10, 2);
            entity.Property(e => e.Reason).HasMaxLength(500);

            entity.HasOne(e => e.Child)
                  .WithMany(c => c.AllowanceAdjustments)
                  .HasForeignKey(e => e.ChildId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AdjustedBy)
                  .WithMany()
                  .HasForeignKey(e => e.AdjustedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ChildId);
            entity.HasIndex(e => e.AdjustmentType);
            entity.HasIndex(e => e.CreatedAt);
        });

        // ParentInvite configuration
        builder.Entity<ParentInvite>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvitedEmail).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(256);

            // Family relationship is configured in Family entity

            entity.HasOne(e => e.InvitedBy)
                  .WithMany()
                  .HasForeignKey(e => e.InvitedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ExistingUser)
                  .WithMany()
                  .HasForeignKey(e => e.ExistingUserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.InvitedEmail, e.FamilyId });
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.Status);
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
