using AllowanceTracker.Data;
using AllowanceTracker.DTOs.Gifting;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Services;

public class GiftServiceTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly GiftService _service;
    private readonly Mock<IGiftLinkService> _mockGiftLinkService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Family _testFamily;
    private readonly ApplicationUser _testParent;
    private readonly Child _testChild;
    private readonly GiftLink _testGiftLink;

    public GiftServiceTests()
    {
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AllowanceContext(options);
        _mockGiftLinkService = new Mock<IGiftLinkService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockEmailService = new Mock<IEmailService>();

        _service = new GiftService(
            _context,
            _mockGiftLinkService.Object,
            _mockNotificationService.Object,
            _mockEmailService.Object);

        // Create test data
        _testFamily = new Family { Id = Guid.NewGuid(), Name = "Test Family" };
        _testParent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "parent@test.com",
            UserName = "parent@test.com",
            FirstName = "Test",
            LastName = "Parent",
            Role = UserRole.Parent,
            FamilyId = _testFamily.Id
        };
        _testFamily.OwnerId = _testParent.Id;

        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            UserName = "child@test.com",
            FirstName = "Timmy",
            LastName = "Test",
            Role = UserRole.Child,
            FamilyId = _testFamily.Id
        };
        _testChild = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            FamilyId = _testFamily.Id,
            CurrentBalance = 50m,
            WeeklyAllowance = 10m
        };

        _testGiftLink = new GiftLink
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            FamilyId = _testFamily.Id,
            CreatedById = _testParent.Id,
            Token = "valid_token",
            Name = "Test Link",
            Visibility = GiftLinkVisibility.Full,
            IsActive = true,
            MinAmount = 5m,
            MaxAmount = 500m
        };

        _context.Families.Add(_testFamily);
        _context.Users.Add(_testParent);
        _context.Users.Add(childUser);
        _context.Children.Add(_testChild);
        _context.GiftLinks.Add(_testGiftLink);
        _context.SaveChanges();

        // Setup mock for valid token
        _mockGiftLinkService
            .Setup(s => s.ValidateTokenAsync("valid_token"))
            .ReturnsAsync(_testGiftLink);

        _mockGiftLinkService
            .Setup(s => s.ValidateTokenAsync("expired_token"))
            .ReturnsAsync((GiftLink?)null);

        _mockGiftLinkService
            .Setup(s => s.IncrementUsageCountAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task GetPortalData_ReturnsChildDisplayInfo()
    {
        // Act
        var result = await _service.GetPortalDataAsync("valid_token");

        // Assert
        result.Should().NotBeNull();
        result.ChildFirstName.Should().Be("Timmy");
        result.MinAmount.Should().Be(5m);
        result.MaxAmount.Should().Be(500m);
    }

    [Fact]
    public async Task GetPortalData_IncludesSavingsGoals_WhenVisibilityAllows()
    {
        // Arrange
        var goal = new SavingsGoal
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            Name = "Bike",
            TargetAmount = 100m,
            CurrentAmount = 25m,
            Status = GoalStatus.Active
        };
        _context.SavingsGoals.Add(goal);
        await _context.SaveChangesAsync();

        _testGiftLink.Visibility = GiftLinkVisibility.WithGoals;

        // Act
        var result = await _service.GetPortalDataAsync("valid_token");

        // Assert
        result.SavingsGoals.Should().NotBeNull();
        result.SavingsGoals.Should().HaveCount(1);
        result.SavingsGoals![0].Name.Should().Be("Bike");
        result.SavingsGoals[0].TargetAmount.Should().Be(100m);
        result.SavingsGoals[0].CurrentAmount.Should().Be(25m);
    }

    [Fact]
    public async Task GetPortalData_ThrowsForExpiredLink()
    {
        // Act
        Func<Task> act = async () => await _service.GetPortalDataAsync("expired_token");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid*");
    }

    [Fact]
    public async Task SubmitGift_CreatesGiftWithPendingStatus()
    {
        // Arrange
        var dto = new SubmitGiftDto(
            "Grandma Betty",
            "grandma@test.com",
            "Grandmother",
            25m,
            GiftOccasion.Birthday,
            null,
            "Happy Birthday!"
        );

        // Act
        var result = await _service.SubmitGiftAsync("valid_token", dto);

        // Assert
        result.Should().NotBeNull();
        result.ChildFirstName.Should().Be("Timmy");
        result.Amount.Should().Be(25m);

        var gift = await _context.Gifts.FirstOrDefaultAsync(g => g.Id == result.GiftId);
        gift.Should().NotBeNull();
        gift!.Status.Should().Be(GiftStatus.Pending);
        gift.GiverName.Should().Be("Grandma Betty");
        gift.GiverEmail.Should().Be("grandma@test.com");
    }

    [Fact]
    public async Task SubmitGift_IncrementsLinkUsageCount()
    {
        // Arrange
        var dto = new SubmitGiftDto("Uncle Bob", null, "Uncle", 50m, GiftOccasion.JustBecause, null, null);

        // Act
        await _service.SubmitGiftAsync("valid_token", dto);

        // Assert
        _mockGiftLinkService.Verify(s => s.IncrementUsageCountAsync(_testGiftLink.Id), Times.Once);
    }

    [Fact]
    public async Task SubmitGift_ValidatesAmountAgainstMinLimit()
    {
        // Arrange
        var dto = new SubmitGiftDto("Cheap Uncle", null, null, 1m, GiftOccasion.JustBecause, null, null);
        _testGiftLink.MinAmount = 5m;

        // Act
        Func<Task> act = async () => await _service.SubmitGiftAsync("valid_token", dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*minimum*");
    }

    [Fact]
    public async Task SubmitGift_ValidatesAmountAgainstMaxLimit()
    {
        // Arrange
        var dto = new SubmitGiftDto("Rich Uncle", null, null, 1000m, GiftOccasion.JustBecause, null, null);
        _testGiftLink.MaxAmount = 500m;

        // Act
        Func<Task> act = async () => await _service.SubmitGiftAsync("valid_token", dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*maximum*");
    }

    [Fact]
    public async Task ApproveGift_CreatesTransactionAndUpdatesBalance()
    {
        // Arrange
        var gift = new Gift
        {
            Id = Guid.NewGuid(),
            GiftLinkId = _testGiftLink.Id,
            ChildId = _testChild.Id,
            GiverName = "Grandpa",
            Amount = 50m,
            Occasion = GiftOccasion.Birthday,
            Status = GiftStatus.Pending
        };
        _context.Gifts.Add(gift);
        await _context.SaveChangesAsync();

        var initialBalance = _testChild.CurrentBalance;
        var dto = new ApproveGiftDto();

        // Act
        var result = await _service.ApproveGiftAsync(gift.Id, dto, _testParent.Id);

        // Assert
        result.Status.Should().Be(GiftStatus.Approved);

        var updatedChild = await _context.Children.FindAsync(_testChild.Id);
        updatedChild!.CurrentBalance.Should().Be(initialBalance + 50m);

        var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == gift.TransactionId);
        transaction.Should().NotBeNull();
        transaction!.Amount.Should().Be(50m);
        transaction.Type.Should().Be(TransactionType.Credit);
    }

    [Fact]
    public async Task ApproveGift_AllocatesToSavingsGoal_WhenSpecified()
    {
        // Arrange
        var goal = new SavingsGoal
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            Name = "Bike",
            TargetAmount = 200m,
            CurrentAmount = 0m,
            Status = GoalStatus.Active
        };
        _context.SavingsGoals.Add(goal);

        var gift = new Gift
        {
            Id = Guid.NewGuid(),
            GiftLinkId = _testGiftLink.Id,
            ChildId = _testChild.Id,
            GiverName = "Grandma",
            Amount = 100m,
            Occasion = GiftOccasion.Birthday,
            Status = GiftStatus.Pending
        };
        _context.Gifts.Add(gift);
        await _context.SaveChangesAsync();

        var dto = new ApproveGiftDto(AllocateToGoalId: goal.Id);

        // Act
        var result = await _service.ApproveGiftAsync(gift.Id, dto, _testParent.Id);

        // Assert
        result.AllocateToGoalId.Should().Be(goal.Id);

        var updatedGoal = await _context.SavingsGoals.FindAsync(goal.Id);
        updatedGoal!.CurrentAmount.Should().Be(100m);
    }

    [Fact]
    public async Task ApproveGift_AllocatesToSavingsPercent_WhenSpecified()
    {
        // Arrange
        _testChild.SavingsAccountEnabled = true;
        var initialSavings = _testChild.SavingsBalance;
        var initialSpending = _testChild.CurrentBalance;

        var gift = new Gift
        {
            Id = Guid.NewGuid(),
            GiftLinkId = _testGiftLink.Id,
            ChildId = _testChild.Id,
            GiverName = "Aunt Jane",
            Amount = 100m,
            Occasion = GiftOccasion.Christmas,
            Status = GiftStatus.Pending
        };
        _context.Gifts.Add(gift);
        await _context.SaveChangesAsync();

        var dto = new ApproveGiftDto(SavingsPercentage: 50);

        // Act
        var result = await _service.ApproveGiftAsync(gift.Id, dto, _testParent.Id);

        // Assert
        result.SavingsPercentage.Should().Be(50);

        var updatedChild = await _context.Children.FindAsync(_testChild.Id);
        // 50% of $100 = $50 to savings, $50 to spending
        updatedChild!.SavingsBalance.Should().Be(initialSavings + 50m);
        updatedChild.CurrentBalance.Should().Be(initialSpending + 50m);
    }

    [Fact]
    public async Task ApproveGift_SendsNotificationToChild()
    {
        // Arrange
        var childUser = await _context.Users.FirstAsync(u => u.Id == _testChild.UserId);

        var gift = new Gift
        {
            Id = Guid.NewGuid(),
            GiftLinkId = _testGiftLink.Id,
            ChildId = _testChild.Id,
            GiverName = "Aunt Sue",
            Amount = 25m,
            Occasion = GiftOccasion.JustBecause,
            Status = GiftStatus.Pending
        };
        _context.Gifts.Add(gift);
        await _context.SaveChangesAsync();

        var dto = new ApproveGiftDto();

        // Act
        await _service.ApproveGiftAsync(gift.Id, dto, _testParent.Id);

        // Assert
        _mockNotificationService.Verify(n => n.SendNotificationAsync(
            childUser.Id,
            NotificationType.GiftReceived,
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("Aunt Sue") && s.Contains("$25")),
            It.IsAny<object>(),
            gift.Id,
            It.IsAny<string>()
        ), Times.Once);
    }

    [Fact]
    public async Task RejectGift_SetsStatusAndReason()
    {
        // Arrange
        var gift = new Gift
        {
            Id = Guid.NewGuid(),
            GiftLinkId = _testGiftLink.Id,
            ChildId = _testChild.Id,
            GiverName = "Suspicious Person",
            Amount = 1000m,
            Occasion = GiftOccasion.Other,
            Status = GiftStatus.Pending
        };
        _context.Gifts.Add(gift);
        await _context.SaveChangesAsync();

        var dto = new RejectGiftDto("Unknown sender");

        // Act
        var result = await _service.RejectGiftAsync(gift.Id, dto, _testParent.Id);

        // Assert
        result.Status.Should().Be(GiftStatus.Rejected);
        result.RejectionReason.Should().Be("Unknown sender");
    }

    [Fact]
    public async Task ExpireOldPendingGifts_ExpiresGiftsOlderThanDays()
    {
        // Arrange
        var oldGift = new Gift
        {
            Id = Guid.NewGuid(),
            GiftLinkId = _testGiftLink.Id,
            ChildId = _testChild.Id,
            GiverName = "Old Relative",
            Amount = 50m,
            Occasion = GiftOccasion.JustBecause,
            Status = GiftStatus.Pending
        };
        var recentGift = new Gift
        {
            Id = Guid.NewGuid(),
            GiftLinkId = _testGiftLink.Id,
            ChildId = _testChild.Id,
            GiverName = "Recent Relative",
            Amount = 25m,
            Occasion = GiftOccasion.JustBecause,
            Status = GiftStatus.Pending
        };
        _context.Gifts.AddRange(oldGift, recentGift);
        await _context.SaveChangesAsync();

        // Update CreatedAt directly after save (bypasses IHasCreatedAt override)
        oldGift.CreatedAt = DateTime.UtcNow.AddDays(-35);
        recentGift.CreatedAt = DateTime.UtcNow.AddDays(-5);
        await _context.SaveChangesAsync();

        // Act
        var expiredCount = await _service.ExpireOldPendingGiftsAsync(30);

        // Assert
        expiredCount.Should().Be(1);

        var expiredGift = await _context.Gifts.FindAsync(oldGift.Id);
        expiredGift!.Status.Should().Be(GiftStatus.Expired);

        var stillPendingGift = await _context.Gifts.FindAsync(recentGift.Id);
        stillPendingGift!.Status.Should().Be(GiftStatus.Pending);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
