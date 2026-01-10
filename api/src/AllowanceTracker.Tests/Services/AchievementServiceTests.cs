using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace AllowanceTracker.Tests.Services;

public class AchievementServiceTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly AchievementService _service;
    private readonly Child _testChild;
    private readonly Family _testFamily;
    private readonly ApplicationUser _testUser;

    public AchievementServiceTests()
    {
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AllowanceContext(options);
        _service = new AchievementService(_context);

        // Create test data
        _testFamily = new Family { Id = Guid.NewGuid(), Name = "Test Family" };
        _testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            UserName = "child@test.com",
            FirstName = "Test",
            LastName = "Child",
            Role = UserRole.Child,
            FamilyId = _testFamily.Id
        };
        _testChild = new Child
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            FamilyId = _testFamily.Id,
            CurrentBalance = 100m,
            WeeklyAllowance = 10m,
            TotalPoints = 0,
            AvailablePoints = 0
        };

        _context.Families.Add(_testFamily);
        _context.Users.Add(_testUser);
        _context.Children.Add(_testChild);
        _context.SaveChanges();
    }

    #region Badge Query Tests

    [Fact]
    public async Task GetAllBadgesAsync_ReturnsActiveBadges()
    {
        // Arrange
        var badge1 = CreateBadge("FIRST_SAVER", "First Saver", BadgeCategory.Saving, isActive: true);
        var badge2 = CreateBadge("RETIRED_BADGE", "Retired Badge", BadgeCategory.Saving, isActive: false);
        _context.Badges.AddRange(badge1, badge2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllBadgesAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Code.Should().Be("FIRST_SAVER");
    }

    [Fact]
    public async Task GetAllBadgesAsync_FiltersByCategory()
    {
        // Arrange
        var savingBadge = CreateBadge("SAVING_BADGE", "Saving Badge", BadgeCategory.Saving);
        var goalBadge = CreateBadge("GOAL_BADGE", "Goal Badge", BadgeCategory.Goals);
        _context.Badges.AddRange(savingBadge, goalBadge);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllBadgesAsync(category: BadgeCategory.Saving);

        // Assert
        result.Should().HaveCount(1);
        result.First().Code.Should().Be("SAVING_BADGE");
    }

    [Fact]
    public async Task GetAllBadgesAsync_ExcludesSecretBadges_ByDefault()
    {
        // Arrange
        var publicBadge = CreateBadge("PUBLIC_BADGE", "Public Badge", BadgeCategory.Milestones, isSecret: false);
        var secretBadge = CreateBadge("SECRET_BADGE", "Secret Badge", BadgeCategory.Special, isSecret: true);
        _context.Badges.AddRange(publicBadge, secretBadge);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllBadgesAsync(includeSecret: false);

        // Assert
        result.Should().HaveCount(1);
        result.First().Code.Should().Be("PUBLIC_BADGE");
    }

    [Fact]
    public async Task GetAllBadgesAsync_IncludesSecretBadges_WhenRequested()
    {
        // Arrange
        var publicBadge = CreateBadge("PUBLIC_BADGE", "Public Badge", BadgeCategory.Milestones);
        var secretBadge = CreateBadge("SECRET_BADGE", "Secret Badge", BadgeCategory.Special, isSecret: true);
        _context.Badges.AddRange(publicBadge, secretBadge);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllBadgesAsync(includeSecret: true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetChildBadgesAsync_ReturnsEarnedBadges()
    {
        // Arrange
        var badge = CreateBadge("TEST_BADGE", "Test Badge", BadgeCategory.Saving);
        _context.Badges.Add(badge);
        await _context.SaveChangesAsync();

        var childBadge = new ChildBadge
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            BadgeId = badge.Id,
            EarnedAt = DateTime.UtcNow,
            IsDisplayed = true,
            IsNew = false
        };
        _context.ChildBadges.Add(childBadge);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetChildBadgesAsync(_testChild.Id);

        // Assert
        result.Should().HaveCount(1);
        result.First().BadgeName.Should().Be("Test Badge");
    }

    [Fact]
    public async Task GetChildBadgesAsync_FiltersByCategory()
    {
        // Arrange
        var savingBadge = CreateBadge("SAVING_BADGE", "Saving Badge", BadgeCategory.Saving);
        var goalBadge = CreateBadge("GOAL_BADGE", "Goal Badge", BadgeCategory.Goals);
        _context.Badges.AddRange(savingBadge, goalBadge);
        await _context.SaveChangesAsync();

        _context.ChildBadges.AddRange(
            new ChildBadge { Id = Guid.NewGuid(), ChildId = _testChild.Id, BadgeId = savingBadge.Id, EarnedAt = DateTime.UtcNow },
            new ChildBadge { Id = Guid.NewGuid(), ChildId = _testChild.Id, BadgeId = goalBadge.Id, EarnedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetChildBadgesAsync(_testChild.Id, category: BadgeCategory.Goals);

        // Assert
        result.Should().HaveCount(1);
        result.First().BadgeName.Should().Be("Goal Badge");
    }

    [Fact]
    public async Task GetChildBadgesAsync_FiltersNewBadgesOnly()
    {
        // Arrange
        var badge1 = CreateBadge("NEW_BADGE", "New Badge", BadgeCategory.Saving);
        var badge2 = CreateBadge("SEEN_BADGE", "Seen Badge", BadgeCategory.Saving);
        _context.Badges.AddRange(badge1, badge2);
        await _context.SaveChangesAsync();

        _context.ChildBadges.AddRange(
            new ChildBadge { Id = Guid.NewGuid(), ChildId = _testChild.Id, BadgeId = badge1.Id, EarnedAt = DateTime.UtcNow, IsNew = true },
            new ChildBadge { Id = Guid.NewGuid(), ChildId = _testChild.Id, BadgeId = badge2.Id, EarnedAt = DateTime.UtcNow, IsNew = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetChildBadgesAsync(_testChild.Id, newOnly: true);

        // Assert
        result.Should().HaveCount(1);
        result.First().BadgeName.Should().Be("New Badge");
    }

    [Fact]
    public async Task GetBadgeProgressAsync_ReturnsInProgressBadges()
    {
        // Arrange
        var badge = CreateBadge("PROGRESS_BADGE", "Progress Badge", BadgeCategory.Chores, pointsValue: 50);
        badge.CriteriaType = BadgeCriteriaType.CountThreshold;
        badge.CriteriaConfig = JsonSerializer.Serialize(new BadgeCriteriaConfig { CountTarget = 10 });
        _context.Badges.Add(badge);
        await _context.SaveChangesAsync();

        var progress = new BadgeProgress
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            BadgeId = badge.Id,
            CurrentProgress = 7,
            TargetProgress = 10
        };
        _context.BadgeProgressRecords.Add(progress);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetBadgeProgressAsync(_testChild.Id);

        // Assert
        result.Should().HaveCount(1);
        var progressDto = result.First();
        progressDto.BadgeName.Should().Be("Progress Badge");
        progressDto.CurrentProgress.Should().Be(7);
        progressDto.TargetProgress.Should().Be(10);
        progressDto.ProgressPercentage.Should().Be(70);
    }

    [Fact]
    public async Task GetAchievementSummaryAsync_ReturnsCompleteSummary()
    {
        // Arrange
        var badge1 = CreateBadge("EARNED_BADGE", "Earned Badge", BadgeCategory.Saving, pointsValue: 25);
        var badge2 = CreateBadge("PROGRESS_BADGE", "Progress Badge", BadgeCategory.Goals, pointsValue: 50);
        _context.Badges.AddRange(badge1, badge2);
        await _context.SaveChangesAsync();

        _context.ChildBadges.Add(new ChildBadge
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            BadgeId = badge1.Id,
            EarnedAt = DateTime.UtcNow,
            IsNew = true
        });

        _context.BadgeProgressRecords.Add(new BadgeProgress
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            BadgeId = badge2.Id,
            CurrentProgress = 3,
            TargetProgress = 5
        });

        _testChild.TotalPoints = 25;
        _testChild.AvailablePoints = 25;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAchievementSummaryAsync(_testChild.Id);

        // Assert
        result.TotalBadges.Should().Be(2);
        result.EarnedBadges.Should().Be(1);
        result.TotalPoints.Should().Be(25);
        result.AvailablePoints.Should().Be(25);
        result.RecentBadges.Should().HaveCount(1);
        result.InProgressBadges.Should().HaveCount(1);
    }

    #endregion

    #region Badge Unlock Tests

    [Fact]
    public async Task TryUnlockBadgeAsync_UnlocksBadgeAndAwardsPoints()
    {
        // Arrange
        var badge = CreateBadge("TEST_BADGE", "Test Badge", BadgeCategory.Saving, pointsValue: 50);
        _context.Badges.Add(badge);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.TryUnlockBadgeAsync(_testChild.Id, "TEST_BADGE");

        // Assert
        result.Should().NotBeNull();
        result!.BadgeName.Should().Be("Test Badge");
        result.PointsValue.Should().Be(50);
        result.IsNew.Should().BeTrue();

        // Verify child points were updated
        var child = await _context.Children.FindAsync(_testChild.Id);
        child!.TotalPoints.Should().Be(50);
        child.AvailablePoints.Should().Be(50);
    }

    [Fact]
    public async Task TryUnlockBadgeAsync_ReturnsNull_WhenBadgeNotFound()
    {
        // Act
        var result = await _service.TryUnlockBadgeAsync(_testChild.Id, "NONEXISTENT_BADGE");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TryUnlockBadgeAsync_ReturnsNull_WhenAlreadyEarned()
    {
        // Arrange
        var badge = CreateBadge("TEST_BADGE", "Test Badge", BadgeCategory.Saving, pointsValue: 50);
        _context.Badges.Add(badge);

        _context.ChildBadges.Add(new ChildBadge
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            BadgeId = badge.Id,
            EarnedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.TryUnlockBadgeAsync(_testChild.Id, "TEST_BADGE");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TryUnlockBadgeAsync_StoresEarnedContext()
    {
        // Arrange
        var badge = CreateBadge("TEST_BADGE", "Test Badge", BadgeCategory.Saving);
        _context.Badges.Add(badge);
        await _context.SaveChangesAsync();

        var contextJson = JsonSerializer.Serialize(new { TransactionId = Guid.NewGuid() });

        // Act
        var result = await _service.TryUnlockBadgeAsync(_testChild.Id, "TEST_BADGE", contextJson);

        // Assert
        result.Should().NotBeNull();
        result!.EarnedContext.Should().Be(contextJson);
    }

    #endregion

    #region Badge Display Tests

    [Fact]
    public async Task ToggleBadgeDisplayAsync_UpdatesDisplayStatus()
    {
        // Arrange
        var badge = CreateBadge("TEST_BADGE", "Test Badge", BadgeCategory.Saving);
        _context.Badges.Add(badge);

        var childBadge = new ChildBadge
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            BadgeId = badge.Id,
            EarnedAt = DateTime.UtcNow,
            IsDisplayed = true
        };
        _context.ChildBadges.Add(childBadge);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ToggleBadgeDisplayAsync(_testChild.Id, badge.Id, false);

        // Assert
        result.Should().NotBeNull();
        result.IsDisplayed.Should().BeFalse();

        var updated = await _context.ChildBadges.FindAsync(childBadge.Id);
        updated!.IsDisplayed.Should().BeFalse();
    }

    [Fact]
    public async Task MarkBadgesSeenAsync_UpdatesIsNewFlag()
    {
        // Arrange
        var badge = CreateBadge("TEST_BADGE", "Test Badge", BadgeCategory.Saving);
        _context.Badges.Add(badge);

        var childBadge = new ChildBadge
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            BadgeId = badge.Id,
            EarnedAt = DateTime.UtcNow,
            IsNew = true
        };
        _context.ChildBadges.Add(childBadge);
        await _context.SaveChangesAsync();

        // Act
        await _service.MarkBadgesSeenAsync(_testChild.Id, new List<Guid> { badge.Id });

        // Assert
        var updated = await _context.ChildBadges.FindAsync(childBadge.Id);
        updated!.IsNew.Should().BeFalse();
    }

    #endregion

    #region Points Tests

    [Fact]
    public async Task GetChildPointsAsync_ReturnsPointsSummary()
    {
        // Arrange
        var badge = CreateBadge("TEST_BADGE", "Test Badge", BadgeCategory.Saving, pointsValue: 100);
        _context.Badges.Add(badge);

        _context.ChildBadges.Add(new ChildBadge
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            BadgeId = badge.Id,
            EarnedAt = DateTime.UtcNow
        });

        var reward = new Reward
        {
            Id = Guid.NewGuid(),
            Name = "Test Reward",
            Description = "Test",
            Type = RewardType.Avatar,
            Value = "avatar1",
            PointsCost = 30
        };
        _context.Rewards.Add(reward);

        _context.ChildRewards.Add(new ChildReward
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            RewardId = reward.Id,
            UnlockedAt = DateTime.UtcNow
        });

        _testChild.TotalPoints = 100;
        _testChild.AvailablePoints = 70;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetChildPointsAsync(_testChild.Id);

        // Assert
        result.TotalPoints.Should().Be(100);
        result.AvailablePoints.Should().Be(70);
        result.SpentPoints.Should().Be(30);
        result.BadgesEarned.Should().Be(1);
        result.RewardsUnlocked.Should().Be(1);
    }

    #endregion

    #region Reward Tests

    [Fact]
    public async Task GetAvailableRewardsAsync_ReturnsActiveRewards()
    {
        // Arrange
        var activeReward = new Reward
        {
            Id = Guid.NewGuid(),
            Name = "Active Reward",
            Description = "Test",
            Type = RewardType.Avatar,
            Value = "avatar1",
            PointsCost = 50,
            IsActive = true
        };
        var inactiveReward = new Reward
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Reward",
            Description = "Test",
            Type = RewardType.Avatar,
            Value = "avatar2",
            PointsCost = 50,
            IsActive = false
        };
        _context.Rewards.AddRange(activeReward, inactiveReward);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailableRewardsAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Active Reward");
    }

    [Fact]
    public async Task GetAvailableRewardsAsync_FiltersByType()
    {
        // Arrange
        var avatarReward = new Reward
        {
            Id = Guid.NewGuid(),
            Name = "Avatar Reward",
            Description = "Test",
            Type = RewardType.Avatar,
            Value = "avatar1",
            PointsCost = 50,
            IsActive = true
        };
        var themeReward = new Reward
        {
            Id = Guid.NewGuid(),
            Name = "Theme Reward",
            Description = "Test",
            Type = RewardType.Theme,
            Value = "theme1",
            PointsCost = 50,
            IsActive = true
        };
        _context.Rewards.AddRange(avatarReward, themeReward);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailableRewardsAsync(type: RewardType.Theme);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Theme Reward");
    }

    [Fact]
    public async Task GetAvailableRewardsAsync_CalculatesCanAfford()
    {
        // Arrange
        _testChild.AvailablePoints = 75;
        await _context.SaveChangesAsync();

        var affordableReward = new Reward
        {
            Id = Guid.NewGuid(),
            Name = "Affordable",
            Description = "Test",
            Type = RewardType.Avatar,
            Value = "avatar1",
            PointsCost = 50,
            IsActive = true
        };
        var expensiveReward = new Reward
        {
            Id = Guid.NewGuid(),
            Name = "Expensive",
            Description = "Test",
            Type = RewardType.Avatar,
            Value = "avatar2",
            PointsCost = 100,
            IsActive = true
        };
        _context.Rewards.AddRange(affordableReward, expensiveReward);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailableRewardsAsync(childId: _testChild.Id);

        // Assert
        result.First(r => r.Name == "Affordable").CanAfford.Should().BeTrue();
        result.First(r => r.Name == "Expensive").CanAfford.Should().BeFalse();
    }

    [Fact]
    public async Task UnlockRewardAsync_DeductsPointsAndCreatesChildReward()
    {
        // Arrange
        _testChild.AvailablePoints = 100;
        await _context.SaveChangesAsync();

        var reward = new Reward
        {
            Id = Guid.NewGuid(),
            Name = "Test Reward",
            Description = "Test",
            Type = RewardType.Avatar,
            Value = "avatar1",
            PointsCost = 50,
            IsActive = true
        };
        _context.Rewards.Add(reward);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UnlockRewardAsync(_testChild.Id, reward.Id);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Reward");
        result.IsUnlocked.Should().BeTrue();

        var child = await _context.Children.FindAsync(_testChild.Id);
        child!.AvailablePoints.Should().Be(50);

        var childReward = await _context.ChildRewards.FirstOrDefaultAsync(cr => cr.ChildId == _testChild.Id && cr.RewardId == reward.Id);
        childReward.Should().NotBeNull();
    }

    [Fact]
    public async Task UnlockRewardAsync_ThrowsWhenInsufficientPoints()
    {
        // Arrange
        _testChild.AvailablePoints = 25;
        await _context.SaveChangesAsync();

        var reward = new Reward
        {
            Id = Guid.NewGuid(),
            Name = "Expensive Reward",
            Description = "Test",
            Type = RewardType.Avatar,
            Value = "avatar1",
            PointsCost = 50,
            IsActive = true
        };
        _context.Rewards.Add(reward);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.UnlockRewardAsync(_testChild.Id, reward.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*insufficient points*");
    }

    [Fact]
    public async Task UnlockRewardAsync_ThrowsWhenAlreadyUnlocked()
    {
        // Arrange
        _testChild.AvailablePoints = 100;
        await _context.SaveChangesAsync();

        var reward = new Reward
        {
            Id = Guid.NewGuid(),
            Name = "Test Reward",
            Description = "Test",
            Type = RewardType.Avatar,
            Value = "avatar1",
            PointsCost = 50,
            IsActive = true
        };
        _context.Rewards.Add(reward);

        _context.ChildRewards.Add(new ChildReward
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            RewardId = reward.Id,
            UnlockedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.UnlockRewardAsync(_testChild.Id, reward.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already unlocked*");
    }

    [Fact]
    public async Task EquipRewardAsync_SetsEquippedFlag()
    {
        // Arrange
        var reward = new Reward
        {
            Id = Guid.NewGuid(),
            Name = "Test Avatar",
            Description = "Test",
            Type = RewardType.Avatar,
            Value = "avatar1",
            PointsCost = 50,
            IsActive = true
        };
        _context.Rewards.Add(reward);

        var childReward = new ChildReward
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            RewardId = reward.Id,
            UnlockedAt = DateTime.UtcNow,
            IsEquipped = false
        };
        _context.ChildRewards.Add(childReward);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.EquipRewardAsync(_testChild.Id, reward.Id);

        // Assert
        result.IsEquipped.Should().BeTrue();

        var child = await _context.Children.FindAsync(_testChild.Id);
        child!.EquippedAvatarUrl.Should().Be("avatar1");
    }

    [Fact]
    public async Task EquipRewardAsync_UnequipsPreviousOfSameType()
    {
        // Arrange
        var reward1 = new Reward
        {
            Id = Guid.NewGuid(),
            Name = "Avatar 1",
            Description = "Test",
            Type = RewardType.Avatar,
            Value = "avatar1",
            PointsCost = 50
        };
        var reward2 = new Reward
        {
            Id = Guid.NewGuid(),
            Name = "Avatar 2",
            Description = "Test",
            Type = RewardType.Avatar,
            Value = "avatar2",
            PointsCost = 50
        };
        _context.Rewards.AddRange(reward1, reward2);

        var childReward1 = new ChildReward
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            RewardId = reward1.Id,
            UnlockedAt = DateTime.UtcNow,
            IsEquipped = true
        };
        var childReward2 = new ChildReward
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            RewardId = reward2.Id,
            UnlockedAt = DateTime.UtcNow,
            IsEquipped = false
        };
        _context.ChildRewards.AddRange(childReward1, childReward2);
        _testChild.EquippedAvatarUrl = "avatar1";
        await _context.SaveChangesAsync();

        // Act
        await _service.EquipRewardAsync(_testChild.Id, reward2.Id);

        // Assert
        var updated1 = await _context.ChildRewards.FindAsync(childReward1.Id);
        var updated2 = await _context.ChildRewards.FindAsync(childReward2.Id);
        updated1!.IsEquipped.Should().BeFalse();
        updated2!.IsEquipped.Should().BeTrue();

        var child = await _context.Children.FindAsync(_testChild.Id);
        child!.EquippedAvatarUrl.Should().Be("avatar2");
    }

    [Fact]
    public async Task UnequipRewardAsync_ClearsEquippedFlag()
    {
        // Arrange
        var reward = new Reward
        {
            Id = Guid.NewGuid(),
            Name = "Test Avatar",
            Description = "Test",
            Type = RewardType.Avatar,
            Value = "avatar1",
            PointsCost = 50
        };
        _context.Rewards.Add(reward);

        var childReward = new ChildReward
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            RewardId = reward.Id,
            UnlockedAt = DateTime.UtcNow,
            IsEquipped = true
        };
        _context.ChildRewards.Add(childReward);
        _testChild.EquippedAvatarUrl = "avatar1";
        await _context.SaveChangesAsync();

        // Act
        await _service.UnequipRewardAsync(_testChild.Id, reward.Id);

        // Assert
        var updated = await _context.ChildRewards.FindAsync(childReward.Id);
        updated!.IsEquipped.Should().BeFalse();

        var child = await _context.Children.FindAsync(_testChild.Id);
        child!.EquippedAvatarUrl.Should().BeNull();
    }

    #endregion

    #region Progress Tracking Tests

    [Fact]
    public async Task UpdateProgressAsync_IncrementsProgress()
    {
        // Arrange
        var badge = CreateBadge("PROGRESS_BADGE", "Progress Badge", BadgeCategory.Chores);
        badge.CriteriaConfig = JsonSerializer.Serialize(new BadgeCriteriaConfig { CountTarget = 10 });
        _context.Badges.Add(badge);

        var progress = new BadgeProgress
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            BadgeId = badge.Id,
            CurrentProgress = 5,
            TargetProgress = 10
        };
        _context.BadgeProgressRecords.Add(progress);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateProgressAsync(_testChild.Id, "PROGRESS_BADGE", 1);

        // Assert
        var updated = await _context.BadgeProgressRecords.FindAsync(progress.Id);
        updated!.CurrentProgress.Should().Be(6);
    }

    [Fact]
    public async Task UpdateProgressAsync_DoesNotExceedTarget()
    {
        // Arrange
        var badge = CreateBadge("PROGRESS_BADGE", "Progress Badge", BadgeCategory.Chores);
        badge.CriteriaConfig = JsonSerializer.Serialize(new BadgeCriteriaConfig { CountTarget = 10 });
        _context.Badges.Add(badge);

        var progress = new BadgeProgress
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            BadgeId = badge.Id,
            CurrentProgress = 9,
            TargetProgress = 10
        };
        _context.BadgeProgressRecords.Add(progress);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateProgressAsync(_testChild.Id, "PROGRESS_BADGE", 5);

        // Assert
        var updated = await _context.BadgeProgressRecords.FindAsync(progress.Id);
        updated!.CurrentProgress.Should().Be(10);
    }

    [Fact]
    public async Task SetProgressAsync_SetsExactValue()
    {
        // Arrange
        var badge = CreateBadge("PROGRESS_BADGE", "Progress Badge", BadgeCategory.Milestones);
        badge.CriteriaConfig = JsonSerializer.Serialize(new BadgeCriteriaConfig { AmountTarget = 100 });
        _context.Badges.Add(badge);

        var progress = new BadgeProgress
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            BadgeId = badge.Id,
            CurrentProgress = 50,
            TargetProgress = 100
        };
        _context.BadgeProgressRecords.Add(progress);
        await _context.SaveChangesAsync();

        // Act
        await _service.SetProgressAsync(_testChild.Id, "PROGRESS_BADGE", 75);

        // Assert
        var updated = await _context.BadgeProgressRecords.FindAsync(progress.Id);
        updated!.CurrentProgress.Should().Be(75);
    }

    [Fact]
    public async Task UpdateProgressAsync_CreatesProgressRecord_IfNotExists()
    {
        // Arrange
        var badge = CreateBadge("NEW_PROGRESS_BADGE", "New Progress Badge", BadgeCategory.Chores);
        badge.CriteriaType = BadgeCriteriaType.CountThreshold;
        badge.CriteriaConfig = JsonSerializer.Serialize(new BadgeCriteriaConfig { CountTarget = 10 });
        _context.Badges.Add(badge);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateProgressAsync(_testChild.Id, "NEW_PROGRESS_BADGE", 1);

        // Assert
        var progress = await _context.BadgeProgressRecords
            .FirstOrDefaultAsync(p => p.ChildId == _testChild.Id && p.Badge.Code == "NEW_PROGRESS_BADGE");
        progress.Should().NotBeNull();
        progress!.CurrentProgress.Should().Be(1);
        progress.TargetProgress.Should().Be(10);
    }

    #endregion

    #region Helper Methods

    private Badge CreateBadge(
        string code,
        string name,
        BadgeCategory category,
        BadgeRarity rarity = BadgeRarity.Common,
        int pointsValue = 10,
        bool isActive = true,
        bool isSecret = false)
    {
        return new Badge
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = $"Description for {name}",
            IconUrl = $"/badges/{code.ToLower()}.png",
            Category = category,
            Rarity = rarity,
            PointsValue = pointsValue,
            CriteriaType = BadgeCriteriaType.SingleAction,
            CriteriaConfig = "{}",
            IsActive = isActive,
            IsSecret = isSecret,
            SortOrder = 0
        };
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
