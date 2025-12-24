using AllowanceTracker.Data;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Services;

public class AllowanceServiceTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<ITransactionService> _mockTransactionService;
    private readonly IAllowanceService _allowanceService;
    private readonly Guid _currentUserId;

    public AllowanceServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AllowanceContext(options);

        // Setup mock current user
        _currentUserId = Guid.NewGuid();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockCurrentUser.Setup(x => x.UserId).Returns(_currentUserId);

        // Setup mock transaction service
        _mockTransactionService = new Mock<ITransactionService>();

        _allowanceService = new AllowanceService(
            _context,
            _mockCurrentUser.Object,
            _mockTransactionService.Object);
    }

    [Fact]
    public async Task PayAllowance_CreatesTransactionAndUpdatesDate()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 15.00m, balance: 10.00m);

        // Act
        await _allowanceService.PayWeeklyAllowanceAsync(child.Id);

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.LastAllowanceDate.Should().NotBeNull();
        updatedChild.LastAllowanceDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify transaction service was called with correct parameters
        _mockTransactionService.Verify(
            x => x.CreateTransactionAsync(It.Is<DTOs.CreateTransactionDto>(dto =>
                dto.ChildId == child.Id &&
                dto.Amount == 15.00m &&
                dto.Type == TransactionType.Credit &&
                dto.Description.Contains("Weekly Allowance"))),
            Times.Once);
    }

    [Fact]
    public async Task PayAllowance_PreventsDoublePay_SameWeek()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 15.00m);
        await _allowanceService.PayWeeklyAllowanceAsync(child.Id);

        // Act
        var act = () => _allowanceService.PayWeeklyAllowanceAsync(child.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Allowance already paid this week");
    }

    [Fact]
    public async Task PayAllowance_WithNonExistentChild_ThrowsException()
    {
        // Arrange
        var nonExistentChildId = Guid.NewGuid();

        // Act
        var act = () => _allowanceService.PayWeeklyAllowanceAsync(nonExistentChildId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Child not found");
    }

    [Fact]
    public async Task PayAllowance_WithZeroAllowance_ThrowsException()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 0m);

        // Act
        var act = () => _allowanceService.PayWeeklyAllowanceAsync(child.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Child has no weekly allowance configured");
    }

    [Fact]
    public async Task PayAllowance_FirstTime_Succeeds()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 20.00m);
        child.LastAllowanceDate.Should().BeNull();

        // Act
        await _allowanceService.PayWeeklyAllowanceAsync(child.Id);

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.LastAllowanceDate.Should().NotBeNull();

        _mockTransactionService.Verify(
            x => x.CreateTransactionAsync(It.IsAny<DTOs.CreateTransactionDto>()),
            Times.Once);
    }

    [Fact]
    public async Task PayAllowance_AfterOneWeek_Succeeds()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 15.00m);
        child.LastAllowanceDate = DateTime.UtcNow.AddDays(-8); // 8 days ago
        await _context.SaveChangesAsync();

        // Act
        await _allowanceService.PayWeeklyAllowanceAsync(child.Id);

        // Assert - Should succeed
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.LastAllowanceDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task PayAllowance_WithinSameWeek_Fails()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 15.00m);
        child.LastAllowanceDate = DateTime.UtcNow.AddDays(-3); // 3 days ago
        await _context.SaveChangesAsync();

        // Act
        var act = () => _allowanceService.PayWeeklyAllowanceAsync(child.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Allowance already paid this week");
    }

    [Fact]
    public async Task ProcessAllPendingAllowances_ProcessesEligibleChildren()
    {
        // Arrange
        var child1 = await CreateTestChild(weeklyAllowance: 10.00m, firstName: "Child1");
        child1.LastAllowanceDate = DateTime.UtcNow.AddDays(-8); // Eligible

        var child2 = await CreateTestChild(weeklyAllowance: 15.00m, firstName: "Child2");
        child2.LastAllowanceDate = null; // First time, eligible

        var child3 = await CreateTestChild(weeklyAllowance: 20.00m, firstName: "Child3");
        child3.LastAllowanceDate = DateTime.UtcNow.AddDays(-3); // Not eligible

        await _context.SaveChangesAsync();

        // Act
        await _allowanceService.ProcessAllPendingAllowancesAsync();

        // Assert - Should process 2 children (child1 and child2)
        _mockTransactionService.Verify(
            x => x.CreateTransactionAsync(It.IsAny<DTOs.CreateTransactionDto>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessAllPendingAllowances_SkipsChildrenWithZeroAllowance()
    {
        // Arrange
        var child1 = await CreateTestChild(weeklyAllowance: 0m);
        var child2 = await CreateTestChild(weeklyAllowance: 10.00m);
        child2.LastAllowanceDate = DateTime.UtcNow.AddDays(-8);

        await _context.SaveChangesAsync();

        // Act
        await _allowanceService.ProcessAllPendingAllowancesAsync();

        // Assert - Should only process child2
        _mockTransactionService.Verify(
            x => x.CreateTransactionAsync(It.IsAny<DTOs.CreateTransactionDto>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAllPendingAllowances_ContinuesOnError()
    {
        // Arrange
        var child1 = await CreateTestChild(weeklyAllowance: 10.00m, firstName: "Child1");
        child1.LastAllowanceDate = DateTime.UtcNow.AddDays(-8);

        var child2 = await CreateTestChild(weeklyAllowance: 15.00m, firstName: "Child2");
        child2.LastAllowanceDate = DateTime.UtcNow.AddDays(-8);

        await _context.SaveChangesAsync();

        // Setup mock to fail on first child but succeed on second
        var callCount = 0;
        _mockTransactionService
            .Setup(x => x.CreateTransactionAsync(It.IsAny<DTOs.CreateTransactionDto>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new Exception("Simulated error");
                return Task.FromResult(new Transaction());
            });

        // Act
        await _allowanceService.ProcessAllPendingAllowancesAsync();

        // Assert - Should attempt both despite first failure
        _mockTransactionService.Verify(
            x => x.CreateTransactionAsync(It.IsAny<DTOs.CreateTransactionDto>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task PayAllowance_WithAllowanceDay_OnCorrectDay_Succeeds()
    {
        // Arrange - Create child with AllowanceDay set to today's day of week
        var child = await CreateTestChild(weeklyAllowance: 15.00m);
        child.AllowanceDay = DateTime.UtcNow.DayOfWeek;
        child.LastAllowanceDate = DateTime.UtcNow.AddDays(-8); // Last paid 8 days ago
        await _context.SaveChangesAsync();

        // Act
        await _allowanceService.PayWeeklyAllowanceAsync(child.Id);

        // Assert - Should succeed because today matches AllowanceDay
        _mockTransactionService.Verify(
            x => x.CreateTransactionAsync(It.IsAny<DTOs.CreateTransactionDto>()),
            Times.Once);
    }

    [Fact]
    public async Task PayAllowance_WithAllowanceDay_OnWrongDay_Fails()
    {
        // Arrange - Create child with AllowanceDay set to different day
        var child = await CreateTestChild(weeklyAllowance: 15.00m);
        child.AllowanceDay = (DayOfWeek)(((int)DateTime.UtcNow.DayOfWeek + 1) % 7); // Different day
        child.LastAllowanceDate = DateTime.UtcNow.AddDays(-8); // Last paid 8 days ago
        await _context.SaveChangesAsync();

        // Act
        var act = () => _allowanceService.PayWeeklyAllowanceAsync(child.Id);

        // Assert - Should fail because today doesn't match AllowanceDay
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not the scheduled allowance day*");
    }

    [Fact]
    public async Task PayAllowance_WithAllowanceDay_FirstTime_OnCorrectDay_Succeeds()
    {
        // Arrange - First allowance payment on correct day
        var child = await CreateTestChild(weeklyAllowance: 20.00m);
        child.AllowanceDay = DateTime.UtcNow.DayOfWeek;
        child.LastAllowanceDate = null; // Never paid before
        await _context.SaveChangesAsync();

        // Act
        await _allowanceService.PayWeeklyAllowanceAsync(child.Id);

        // Assert
        _mockTransactionService.Verify(
            x => x.CreateTransactionAsync(It.IsAny<DTOs.CreateTransactionDto>()),
            Times.Once);
    }

    [Fact]
    public async Task PayAllowance_WithNullAllowanceDay_UsesRollingWindow()
    {
        // Arrange - Child without AllowanceDay set (null)
        var child = await CreateTestChild(weeklyAllowance: 15.00m);
        child.AllowanceDay = null;
        child.LastAllowanceDate = DateTime.UtcNow.AddDays(-8); // Last paid 8 days ago
        await _context.SaveChangesAsync();

        // Act
        await _allowanceService.PayWeeklyAllowanceAsync(child.Id);

        // Assert - Should succeed with 7-day rolling window logic
        _mockTransactionService.Verify(
            x => x.CreateTransactionAsync(It.IsAny<DTOs.CreateTransactionDto>()),
            Times.Once);
    }

    [Fact]
    public async Task PayAllowance_WithAllowanceDay_SameWeek_SameDay_Fails()
    {
        // Arrange - Already paid today
        var child = await CreateTestChild(weeklyAllowance: 15.00m);
        child.AllowanceDay = DateTime.UtcNow.DayOfWeek;
        child.LastAllowanceDate = DateTime.UtcNow.AddHours(-2); // Paid 2 hours ago today
        await _context.SaveChangesAsync();

        // Act
        var act = () => _allowanceService.PayWeeklyAllowanceAsync(child.Id);

        // Assert - Should fail because already paid this week
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Allowance already paid this week");
    }

    [Fact]
    public async Task ProcessAllPendingAllowances_RespectsAllowanceDay()
    {
        // Arrange
        var today = DateTime.UtcNow.DayOfWeek;
        var tomorrow = (DayOfWeek)(((int)today + 1) % 7);

        // Child 1: AllowanceDay is today, eligible
        var child1 = await CreateTestChild(weeklyAllowance: 10.00m, firstName: "Child1");
        child1.AllowanceDay = today;
        child1.LastAllowanceDate = DateTime.UtcNow.AddDays(-8);

        // Child 2: AllowanceDay is tomorrow, not eligible
        var child2 = await CreateTestChild(weeklyAllowance: 15.00m, firstName: "Child2");
        child2.AllowanceDay = tomorrow;
        child2.LastAllowanceDate = DateTime.UtcNow.AddDays(-8);

        // Child 3: No AllowanceDay, eligible (rolling window)
        var child3 = await CreateTestChild(weeklyAllowance: 20.00m, firstName: "Child3");
        child3.AllowanceDay = null;
        child3.LastAllowanceDate = DateTime.UtcNow.AddDays(-8);

        await _context.SaveChangesAsync();

        // Act
        await _allowanceService.ProcessAllPendingAllowancesAsync();

        // Assert - Should process child1 and child3, but not child2
        _mockTransactionService.Verify(
            x => x.CreateTransactionAsync(It.IsAny<DTOs.CreateTransactionDto>()),
            Times.Exactly(2));
    }

    // NEW: Allowance Pause/Resume Tests
    [Fact]
    public async Task PauseAllowance_WithValidChild_SetsAllowancePausedToTrue()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 15.00m);
        child.AllowancePaused.Should().BeFalse(); // Default state

        // Act
        await _allowanceService.PauseAllowanceAsync(child.Id, "Going on vacation");

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.AllowancePaused.Should().BeTrue();
        updatedChild.AllowancePausedReason.Should().Be("Going on vacation");
    }

    [Fact]
    public async Task PauseAllowance_WithNoReason_SetsAllowancePausedWithNullReason()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 15.00m);

        // Act
        await _allowanceService.PauseAllowanceAsync(child.Id, null);

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.AllowancePaused.Should().BeTrue();
        updatedChild.AllowancePausedReason.Should().BeNull();
    }

    [Fact]
    public async Task PauseAllowance_WhenAlreadyPaused_UpdatesReason()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 15.00m);
        child.AllowancePaused = true;
        child.AllowancePausedReason = "Original reason";
        await _context.SaveChangesAsync();

        // Act
        await _allowanceService.PauseAllowanceAsync(child.Id, "New reason");

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.AllowancePausedReason.Should().Be("New reason");
    }

    [Fact]
    public async Task ResumeAllowance_WithPausedChild_SetsAllowancePausedToFalse()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 15.00m);
        child.AllowancePaused = true;
        child.AllowancePausedReason = "Temporary pause";
        await _context.SaveChangesAsync();

        // Act
        await _allowanceService.ResumeAllowanceAsync(child.Id);

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.AllowancePaused.Should().BeFalse();
        updatedChild.AllowancePausedReason.Should().BeNull();
    }

    [Fact]
    public async Task ResumeAllowance_WhenNotPaused_DoesNothing()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 15.00m);
        child.AllowancePaused = false;
        await _context.SaveChangesAsync();

        // Act
        await _allowanceService.ResumeAllowanceAsync(child.Id);

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.AllowancePaused.Should().BeFalse();
    }

    [Fact]
    public async Task PayAllowance_WhenPaused_ThrowsException()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 15.00m);
        child.AllowancePaused = true;
        child.AllowancePausedReason = "Vacation";
        await _context.SaveChangesAsync();

        // Act
        var act = () => _allowanceService.PayWeeklyAllowanceAsync(child.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Allowance is currently paused*");
    }

    [Fact]
    public async Task ProcessAllPendingAllowances_SkipsPausedChildren()
    {
        // Arrange
        var child1 = await CreateTestChild(weeklyAllowance: 10.00m, firstName: "Child1");
        child1.LastAllowanceDate = DateTime.UtcNow.AddDays(-8); // Eligible
        child1.AllowancePaused = false;

        var child2 = await CreateTestChild(weeklyAllowance: 15.00m, firstName: "Child2");
        child2.LastAllowanceDate = DateTime.UtcNow.AddDays(-8); // Eligible but paused
        child2.AllowancePaused = true;

        await _context.SaveChangesAsync();

        // Act
        await _allowanceService.ProcessAllPendingAllowancesAsync();

        // Assert - Should only process child1 (not paused)
        _mockTransactionService.Verify(
            x => x.CreateTransactionAsync(It.IsAny<DTOs.CreateTransactionDto>()),
            Times.Once);
    }

    [Fact]
    public async Task AdjustAllowanceAmount_WithValidAmount_UpdatesWeeklyAllowance()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 10.00m);

        // Act
        await _allowanceService.AdjustAllowanceAmountAsync(child.Id, 20.00m, "Performance improvement");

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.WeeklyAllowance.Should().Be(20.00m);
    }

    [Fact]
    public async Task AdjustAllowanceAmount_WithZeroAmount_SetsAllowanceToZero()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 10.00m);

        // Act
        await _allowanceService.AdjustAllowanceAmountAsync(child.Id, 0m, "Suspended allowance");

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.WeeklyAllowance.Should().Be(0m);
    }

    [Fact]
    public async Task AdjustAllowanceAmount_WithNegativeAmount_ThrowsException()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 10.00m);

        // Act
        var act = () => _allowanceService.AdjustAllowanceAmountAsync(child.Id, -5.00m, "Invalid");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot be negative*");
    }

    [Fact]
    public async Task PauseAllowance_CreatesAdjustmentHistoryRecord()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 10.00m);

        // Act
        await _allowanceService.PauseAllowanceAsync(child.Id, "Vacation");

        // Assert - Check directly in context
        var adjustments = await _context.AllowanceAdjustments
            .Where(a => a.ChildId == child.Id)
            .ToListAsync();

        adjustments.Should().HaveCount(1);
        adjustments[0].AdjustmentType.Should().Be(AllowanceAdjustmentType.Paused);
        adjustments[0].Reason.Should().Be("Vacation");
    }

    [Fact]
    public async Task GetAllowanceAdjustmentHistory_ReturnsAllAdjustments()
    {
        // Arrange
        var child = await CreateTestChild(weeklyAllowance: 10.00m);

        // Make several adjustments
        await _allowanceService.PauseAllowanceAsync(child.Id, "Vacation");
        _context.ChangeTracker.Clear(); // Clear tracking to ensure fresh query

        await _allowanceService.ResumeAllowanceAsync(child.Id);
        _context.ChangeTracker.Clear(); // Clear tracking to ensure fresh query

        await _allowanceService.AdjustAllowanceAmountAsync(child.Id, 15.00m, "Raise");
        _context.ChangeTracker.Clear(); // Clear tracking to ensure fresh query

        // Act
        var history = await _allowanceService.GetAllowanceAdjustmentHistoryAsync(child.Id);

        // Assert
        history.Should().HaveCount(3);
        history[0].AdjustmentType.Should().Be(AllowanceAdjustmentType.Paused);
        history[1].AdjustmentType.Should().Be(AllowanceAdjustmentType.Resumed);
        history[2].AdjustmentType.Should().Be(AllowanceAdjustmentType.AmountChanged);
    }

    private async Task<Child> CreateTestChild(
        decimal balance = 0m,
        decimal weeklyAllowance = 10m,
        string firstName = "Test")
    {
        var family = new Family { Name = "Test Family" };
        _context.Families.Add(family);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{firstName.ToLower()}@example.com",
            Email = $"{firstName.ToLower()}@example.com",
            FirstName = firstName,
            LastName = "Child",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        _context.Users.Add(user);

        var child = new Child
        {
            UserId = user.Id,
            FamilyId = family.Id,
            CurrentBalance = balance,
            WeeklyAllowance = weeklyAllowance
        };
        _context.Children.Add(child);

        await _context.SaveChangesAsync();
        return child;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
