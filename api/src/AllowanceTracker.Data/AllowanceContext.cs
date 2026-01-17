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
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }
    public DbSet<DeviceToken> DeviceTokens { get; set; }
    public DbSet<Badge> Badges { get; set; }
    public DbSet<ChildBadge> ChildBadges { get; set; }
    public DbSet<BadgeProgress> BadgeProgressRecords { get; set; }
    public DbSet<Reward> Rewards { get; set; }
    public DbSet<ChildReward> ChildRewards { get; set; }
    public DbSet<SavingsGoal> SavingsGoals { get; set; }
    public DbSet<SavingsContribution> SavingsContributions { get; set; }
    public DbSet<ParentMatchingRule> ParentMatchingRules { get; set; }
    public DbSet<GoalMilestone> GoalMilestones { get; set; }
    public DbSet<GoalChallenge> GoalChallenges { get; set; }
    public DbSet<GiftLink> GiftLinks { get; set; }
    public DbSet<Gift> Gifts { get; set; }
    public DbSet<ThankYouNote> ThankYouNotes { get; set; }

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

            // Achievement system properties
            entity.Property(e => e.TotalPoints).HasDefaultValue(0);
            entity.Property(e => e.AvailablePoints).HasDefaultValue(0);
            entity.Property(e => e.EquippedAvatarUrl).HasMaxLength(500);
            entity.Property(e => e.EquippedTheme).HasMaxLength(100);
            entity.Property(e => e.EquippedTitle).HasMaxLength(100);
            entity.Property(e => e.SavingStreak).HasDefaultValue(0);

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

        // Notification configuration
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Body).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Data).HasMaxLength(4000);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.Property(e => e.RelatedEntityType).HasMaxLength(50);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Type);
        });

        // NotificationPreference configuration
        builder.Entity<NotificationPreference>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.NotificationType }).IsUnique();
        });

        // DeviceToken configuration
        builder.Entity<DeviceToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.Property(e => e.DeviceName).HasMaxLength(100);
            entity.Property(e => e.AppVersion).HasMaxLength(20);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.IsActive });
        });

        // Badge configuration
        builder.Entity<Badge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.IconUrl).HasMaxLength(500);
            entity.Property(e => e.CriteriaConfig).HasMaxLength(2000);

            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Rarity);
            entity.HasIndex(e => e.IsActive);
        });

        // ChildBadge configuration
        builder.Entity<ChildBadge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EarnedContext).HasMaxLength(2000);

            entity.HasOne(e => e.Child)
                  .WithMany(c => c.Badges)
                  .HasForeignKey(e => e.ChildId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Badge)
                  .WithMany(b => b.ChildBadges)
                  .HasForeignKey(e => e.BadgeId)
                  .OnDelete(DeleteBehavior.Cascade);

            // One badge per child
            entity.HasIndex(e => new { e.ChildId, e.BadgeId }).IsUnique();
            entity.HasIndex(e => e.EarnedAt);
            entity.HasIndex(e => e.IsNew);
        });

        // BadgeProgress configuration
        builder.Entity<BadgeProgress>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Child)
                  .WithMany(c => c.BadgeProgress)
                  .HasForeignKey(e => e.ChildId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Badge)
                  .WithMany(b => b.BadgeProgress)
                  .HasForeignKey(e => e.BadgeId)
                  .OnDelete(DeleteBehavior.Cascade);

            // One progress record per badge per child
            entity.HasIndex(e => new { e.ChildId, e.BadgeId }).IsUnique();
        });

        // Reward configuration
        builder.Entity<Reward>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PreviewUrl).HasMaxLength(500);

            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.PointsCost);
        });

        // ChildReward configuration
        builder.Entity<ChildReward>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Child)
                  .WithMany(c => c.Rewards)
                  .HasForeignKey(e => e.ChildId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Reward)
                  .WithMany(r => r.ChildRewards)
                  .HasForeignKey(e => e.RewardId)
                  .OnDelete(DeleteBehavior.Cascade);

            // One reward per child (can't unlock same reward twice)
            entity.HasIndex(e => new { e.ChildId, e.RewardId }).IsUnique();
            entity.HasIndex(e => e.IsEquipped);
        });

        // SavingsGoal configuration
        builder.Entity<SavingsGoal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.TargetAmount).HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.CurrentAmount).HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(e => e.ImageUrl).HasMaxLength(2048);
            entity.Property(e => e.ProductUrl).HasMaxLength(2048);
            entity.Property(e => e.AutoTransferAmount).HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(e => e.Priority).HasDefaultValue(1);

            entity.HasOne(e => e.Child)
                  .WithMany(c => c.SavingsGoals)
                  .HasForeignKey(e => e.ChildId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ChildId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.ChildId, e.Status });

            // Ignore computed properties
            entity.Ignore(e => e.RemainingAmount);
            entity.Ignore(e => e.ProgressPercentage);
            entity.Ignore(e => e.DaysRemaining);
        });

        // SavingsContribution configuration
        builder.Entity<SavingsContribution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.GoalBalanceAfter).HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.Goal)
                  .WithMany(g => g.Contributions)
                  .HasForeignKey(e => e.GoalId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Child)
                  .WithMany(c => c.SavingsContributions)
                  .HasForeignKey(e => e.ChildId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedBy)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.GoalId);
            entity.HasIndex(e => e.ChildId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.CreatedAt);
        });

        // ParentMatchingRule configuration
        builder.Entity<ParentMatchingRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MatchRatio).HasPrecision(10, 4).IsRequired();
            entity.Property(e => e.MaxMatchAmount).HasPrecision(10, 2);
            entity.Property(e => e.TotalMatchedAmount).HasPrecision(10, 2).HasDefaultValue(0m);

            entity.HasOne(e => e.Goal)
                  .WithOne(g => g.MatchingRule)
                  .HasForeignKey<ParentMatchingRule>(e => e.GoalId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByParent)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByParentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.GoalId).IsUnique();
            entity.HasIndex(e => e.IsActive);

            // Ignore computed property
            entity.Ignore(e => e.RemainingMatchAmount);
        });

        // GoalMilestone configuration
        builder.Entity<GoalMilestone>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TargetAmount).HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.CelebrationMessage).HasMaxLength(500);
            entity.Property(e => e.BonusAmount).HasPrecision(10, 2);

            entity.HasOne(e => e.Goal)
                  .WithMany(g => g.Milestones)
                  .HasForeignKey(e => e.GoalId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.GoalId);
            entity.HasIndex(e => new { e.GoalId, e.PercentComplete }).IsUnique();
        });

        // GoalChallenge configuration
        builder.Entity<GoalChallenge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TargetAmount).HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.BonusAmount).HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.Goal)
                  .WithOne(g => g.ActiveChallenge)
                  .HasForeignKey<GoalChallenge>(e => e.GoalId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByParent)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByParentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.GoalId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.EndDate);

            // Ignore computed properties
            entity.Ignore(e => e.DaysRemaining);
            entity.Ignore(e => e.IsExpired);
        });

        // GiftLink configuration
        builder.Entity<GiftLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.MinAmount).HasPrecision(10, 2);
            entity.Property(e => e.MaxAmount).HasPrecision(10, 2);

            entity.HasOne(e => e.Child)
                  .WithMany(c => c.GiftLinks)
                  .HasForeignKey(e => e.ChildId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Family)
                  .WithMany()
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedBy)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.ChildId);
            entity.HasIndex(e => e.FamilyId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ExpiresAt);
        });

        // Gift configuration
        builder.Entity<Gift>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GiverName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.GiverEmail).HasMaxLength(256);
            entity.Property(e => e.GiverRelationship).HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.CustomOccasion).HasMaxLength(100);
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);

            entity.HasOne(e => e.GiftLink)
                  .WithMany(gl => gl.Gifts)
                  .HasForeignKey(e => e.GiftLinkId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Child)
                  .WithMany(c => c.Gifts)
                  .HasForeignKey(e => e.ChildId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ProcessedBy)
                  .WithMany()
                  .HasForeignKey(e => e.ProcessedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AllocateToGoal)
                  .WithMany()
                  .HasForeignKey(e => e.AllocateToGoalId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Transaction)
                  .WithOne()
                  .HasForeignKey<Gift>(e => e.TransactionId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.GiftLinkId);
            entity.HasIndex(e => e.ChildId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        // ThankYouNote configuration
        builder.Entity<ThankYouNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.ImageUrl).HasMaxLength(2048);

            entity.HasOne(e => e.Gift)
                  .WithOne(g => g.ThankYouNote)
                  .HasForeignKey<ThankYouNote>(e => e.GiftId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Child)
                  .WithMany(c => c.ThankYouNotes)
                  .HasForeignKey(e => e.ChildId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.GiftId).IsUnique();
            entity.HasIndex(e => e.ChildId);
            entity.HasIndex(e => e.IsSent);
        });

        // Seed Badge data
        var badges = AllowanceTracker.Data.SeedData.BadgeSeedData.GetBadges();
        builder.Entity<Badge>().HasData(badges);

        // Seed Reward data
        var rewards = AllowanceTracker.Data.SeedData.BadgeSeedData.GetRewards();
        builder.Entity<Reward>().HasData(rewards);
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
