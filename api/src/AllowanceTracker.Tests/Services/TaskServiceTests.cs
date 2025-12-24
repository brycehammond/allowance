using AllowanceTracker.Data;
using AllowanceTracker.DTOs.Tasks;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace AllowanceTracker.Tests.Services;

public class TaskServiceTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly ITaskService _taskService;
    private readonly Guid _familyId;
    private readonly Guid _parentId;
    private readonly Guid _childUserId;
    private readonly Guid _childId;

    public TaskServiceTests()
    {
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AllowanceContext(options);
        _taskService = new TaskService(_context);

        // Setup test data
        _familyId = Guid.NewGuid();
        _parentId = Guid.NewGuid();
        _childUserId = Guid.NewGuid();
        _childId = Guid.NewGuid();

        var family = new Family
        {
            Id = _familyId,
            Name = "Test Family"
        };

        var parent = new ApplicationUser
        {
            Id = _parentId,
            UserName = "parent@test.com",
            Email = "parent@test.com",
            FirstName = "Parent",
            LastName = "User",
            Role = UserRole.Parent,
            FamilyId = _familyId
        };

        var childUser = new ApplicationUser
        {
            Id = _childUserId,
            UserName = "child@test.com",
            Email = "child@test.com",
            FirstName = "Child",
            LastName = "User",
            Role = UserRole.Child,
            FamilyId = _familyId
        };

        var child = new Child
        {
            Id = _childId,
            UserId = _childUserId,
            FamilyId = _familyId,
            CurrentBalance = 50.00m,
            WeeklyAllowance = 10.00m,
            AllowanceDay = DayOfWeek.Friday
        };

        _context.Families.Add(family);
        _context.Users.AddRange(parent, childUser);
        _context.Children.Add(child);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CreateTaskAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task CreateTaskAsync_ValidDto_CreatesTask()
    {
        // Arrange
        var dto = new CreateTaskDto(
            ChildId: _childId,
            Title: "Clean your room",
            Description: "Make bed, vacuum, organize desk",
            RewardAmount: 5.00m,
            IsRecurring: false,
            RecurrenceType: null,
            RecurrenceDay: null,
            RecurrenceDayOfMonth: null
        );

        // Act
        var result = await _taskService.CreateTaskAsync(dto, _parentId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Clean your room");
        result.ChildId.Should().Be(_childId);
        result.RewardAmount.Should().Be(5.00m);
        result.Status.Should().Be(ChoreTaskStatus.Active);
        result.CreatedById.Should().Be(_parentId);
        result.TotalCompletions.Should().Be(0);
        result.PendingApprovals.Should().Be(0);

        var taskInDb = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == result.Id);
        taskInDb.Should().NotBeNull();
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTaskAsync_RecurringTask_SetsRecurrenceProperties()
    {
        // Arrange
        var dto = new CreateTaskDto(
            ChildId: _childId,
            Title: "Take out trash",
            Description: null,
            RewardAmount: 2.00m,
            IsRecurring: true,
            RecurrenceType: RecurrenceType.Weekly,
            RecurrenceDay: DayOfWeek.Monday,
            RecurrenceDayOfMonth: null
        );

        // Act
        var result = await _taskService.CreateTaskAsync(dto, _parentId);

        // Assert
        result.IsRecurring.Should().BeTrue();
        result.RecurrenceType.Should().Be(RecurrenceType.Weekly);
        result.RecurrenceDisplay.Should().Be("Weekly on Monday");
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTaskAsync_InvalidChildId_ThrowsException()
    {
        // Arrange
        var dto = new CreateTaskDto(
            ChildId: Guid.NewGuid(), // Non-existent child
            Title: "Test Task",
            Description: null,
            RewardAmount: 5.00m,
            IsRecurring: false,
            RecurrenceType: null,
            RecurrenceDay: null,
            RecurrenceDayOfMonth: null
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _taskService.CreateTaskAsync(dto, _parentId)
        );
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTaskAsync_DifferentFamily_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var otherFamilyId = Guid.NewGuid();
        var otherParentId = Guid.NewGuid();

        var otherFamily = new Family { Id = otherFamilyId, Name = "Other Family" };
        var otherParent = new ApplicationUser
        {
            Id = otherParentId,
            UserName = "other@test.com",
            Email = "other@test.com",
            FirstName = "Other",
            LastName = "Parent",
            Role = UserRole.Parent,
            FamilyId = otherFamilyId
        };

        _context.Families.Add(otherFamily);
        _context.Users.Add(otherParent);
        await _context.SaveChangesAsync();

        var dto = new CreateTaskDto(
            ChildId: _childId, // Child from different family
            Title: "Test Task",
            Description: null,
            RewardAmount: 5.00m,
            IsRecurring: false,
            RecurrenceType: null,
            RecurrenceDay: null,
            RecurrenceDayOfMonth: null
        );

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _taskService.CreateTaskAsync(dto, otherParentId)
        );
    }

    #endregion

    #region CompleteTaskAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task CompleteTaskAsync_ValidTask_CreatesCompletion()
    {
        // Arrange
        var task = new ChoreTask
        {
            Id = Guid.NewGuid(),
            ChildId = _childId,
            Title = "Test Task",
            RewardAmount = 5.00m,
            Status = ChoreTaskStatus.Active,
            CreatedById = _parentId,
            CreatedAt = DateTime.UtcNow,
            IsRecurring = false
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var dto = new CompleteTaskDto(
            Notes: "All done!",
            PhotoUrl: null
        );

        // Act
        var result = await _taskService.CompleteTaskAsync(task.Id, dto, _childUserId);

        // Assert
        result.Should().NotBeNull();
        result.TaskId.Should().Be(task.Id);
        result.ChildId.Should().Be(_childId);
        result.Status.Should().Be(CompletionStatus.PendingApproval);
        result.Notes.Should().Be("All done!");
        result.RewardAmount.Should().Be(5.00m);
    }

    [Fact]
    public async System.Threading.Tasks.Task CompleteTaskAsync_ArchivedTask_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new ChoreTask
        {
            Id = Guid.NewGuid(),
            ChildId = _childId,
            Title = "Archived Task",
            RewardAmount = 5.00m,
            Status = ChoreTaskStatus.Archived,
            ArchivedAt = DateTime.UtcNow,
            CreatedById = _parentId,
            CreatedAt = DateTime.UtcNow,
            IsRecurring = false
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var dto = new CompleteTaskDto(Notes: null, PhotoUrl: null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _taskService.CompleteTaskAsync(task.Id, dto, _childUserId)
        );
    }

    [Fact]
    public async System.Threading.Tasks.Task CompleteTaskAsync_WrongChild_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var otherChildUserId = Guid.NewGuid();
        var otherChildId = Guid.NewGuid();

        var otherChildUser = new ApplicationUser
        {
            Id = otherChildUserId,
            UserName = "other-child@test.com",
            Email = "other-child@test.com",
            FirstName = "Other",
            LastName = "Child",
            Role = UserRole.Child,
            FamilyId = _familyId
        };

        var otherChild = new Child
        {
            Id = otherChildId,
            UserId = otherChildUserId,
            FamilyId = _familyId,
            CurrentBalance = 25.00m,
            WeeklyAllowance = 10.00m,
            AllowanceDay = DayOfWeek.Friday
        };

        _context.Users.Add(otherChildUser);
        _context.Children.Add(otherChild);
        await _context.SaveChangesAsync();

        var task = new ChoreTask
        {
            Id = Guid.NewGuid(),
            ChildId = _childId, // Assigned to different child
            Title = "Test Task",
            RewardAmount = 5.00m,
            Status = ChoreTaskStatus.Active,
            CreatedById = _parentId,
            CreatedAt = DateTime.UtcNow,
            IsRecurring = false
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var dto = new CompleteTaskDto(Notes: null, PhotoUrl: null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _taskService.CompleteTaskAsync(task.Id, dto, otherChildUserId)
        );
    }

    #endregion

    #region ReviewCompletionAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task ReviewCompletionAsync_Approve_CreatesTransactionAndUpdatesBalance()
    {
        // Arrange
        var task = new ChoreTask
        {
            Id = Guid.NewGuid(),
            ChildId = _childId,
            Title = "Test Task",
            RewardAmount = 10.00m,
            Status = ChoreTaskStatus.Active,
            CreatedById = _parentId,
            CreatedAt = DateTime.UtcNow,
            IsRecurring = false
        };

        var completion = new TaskCompletion
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            ChildId = _childId,
            CompletedAt = DateTime.UtcNow,
            Status = CompletionStatus.PendingApproval
        };

        _context.Tasks.Add(task);
        _context.TaskCompletions.Add(completion);
        await _context.SaveChangesAsync();

        var dto = new ReviewCompletionDto(
            IsApproved: true,
            RejectionReason: null
        );

        var initialBalance = _context.Children.First(c => c.Id == _childId).CurrentBalance;

        // Act
        var result = await _taskService.ReviewCompletionAsync(completion.Id, dto, _parentId);

        // Assert
        result.Status.Should().Be(CompletionStatus.Approved);
        result.ApprovedById.Should().Be(_parentId);
        result.ApprovedAt.Should().NotBeNull();
        result.TransactionId.Should().NotBeNull();

        var child = await _context.Children.FirstAsync(c => c.Id == _childId);
        child.CurrentBalance.Should().Be(initialBalance + 10.00m);

        var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == result.TransactionId);
        transaction.Should().NotBeNull();
        transaction!.Amount.Should().Be(10.00m);
        transaction.Type.Should().Be(TransactionType.Credit);
        transaction.Category.Should().Be(TransactionCategory.Task);
    }

    [Fact]
    public async System.Threading.Tasks.Task ReviewCompletionAsync_Reject_DoesNotCreateTransaction()
    {
        // Arrange
        var task = new ChoreTask
        {
            Id = Guid.NewGuid(),
            ChildId = _childId,
            Title = "Test Task",
            RewardAmount = 10.00m,
            Status = ChoreTaskStatus.Active,
            CreatedById = _parentId,
            CreatedAt = DateTime.UtcNow,
            IsRecurring = false
        };

        var completion = new TaskCompletion
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            ChildId = _childId,
            CompletedAt = DateTime.UtcNow,
            Status = CompletionStatus.PendingApproval
        };

        _context.Tasks.Add(task);
        _context.TaskCompletions.Add(completion);
        await _context.SaveChangesAsync();

        var dto = new ReviewCompletionDto(
            IsApproved: false,
            RejectionReason: "Didn't clean thoroughly enough"
        );

        var initialBalance = _context.Children.First(c => c.Id == _childId).CurrentBalance;

        // Act
        var result = await _taskService.ReviewCompletionAsync(completion.Id, dto, _parentId);

        // Assert
        result.Status.Should().Be(CompletionStatus.Rejected);
        result.RejectionReason.Should().Be("Didn't clean thoroughly enough");
        result.TransactionId.Should().BeNull();

        var child = await _context.Children.FirstAsync(c => c.Id == _childId);
        child.CurrentBalance.Should().Be(initialBalance); // Balance unchanged
    }

    [Fact]
    public async System.Threading.Tasks.Task ReviewCompletionAsync_AlreadyReviewed_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new ChoreTask
        {
            Id = Guid.NewGuid(),
            ChildId = _childId,
            Title = "Test Task",
            RewardAmount = 10.00m,
            Status = ChoreTaskStatus.Active,
            CreatedById = _parentId,
            CreatedAt = DateTime.UtcNow,
            IsRecurring = false
        };

        var completion = new TaskCompletion
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            ChildId = _childId,
            CompletedAt = DateTime.UtcNow,
            Status = CompletionStatus.Approved, // Already approved
            ApprovedById = _parentId,
            ApprovedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        _context.TaskCompletions.Add(completion);
        await _context.SaveChangesAsync();

        var dto = new ReviewCompletionDto(IsApproved: true, RejectionReason: null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _taskService.ReviewCompletionAsync(completion.Id, dto, _parentId)
        );
    }

    #endregion

    #region GetPendingApprovalsAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task GetPendingApprovalsAsync_ReturnsPendingCompletions()
    {
        // Arrange
        var task = new ChoreTask
        {
            Id = Guid.NewGuid(),
            ChildId = _childId,
            Title = "Test Task",
            RewardAmount = 10.00m,
            Status = ChoreTaskStatus.Active,
            CreatedById = _parentId,
            CreatedAt = DateTime.UtcNow,
            IsRecurring = false
        };

        var completion1 = new TaskCompletion
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            ChildId = _childId,
            CompletedAt = DateTime.UtcNow.AddHours(-2),
            Status = CompletionStatus.PendingApproval
        };

        var completion2 = new TaskCompletion
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            ChildId = _childId,
            CompletedAt = DateTime.UtcNow.AddHours(-1),
            Status = CompletionStatus.Approved,
            ApprovedById = _parentId,
            ApprovedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        _context.TaskCompletions.AddRange(completion1, completion2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetPendingApprovalsAsync(_parentId);

        // Assert
        result.Should().HaveCount(1);
        result.First().Status.Should().Be(CompletionStatus.PendingApproval);
    }

    #endregion

    #region ArchiveTaskAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task ArchiveTaskAsync_ActiveTask_ArchivesTask()
    {
        // Arrange
        var task = new ChoreTask
        {
            Id = Guid.NewGuid(),
            ChildId = _childId,
            Title = "Test Task",
            RewardAmount = 10.00m,
            Status = ChoreTaskStatus.Active,
            CreatedById = _parentId,
            CreatedAt = DateTime.UtcNow,
            IsRecurring = false
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        await _taskService.ArchiveTaskAsync(task.Id, _parentId);

        // Assert
        var archivedTask = await _context.Tasks.FirstAsync(t => t.Id == task.Id);
        archivedTask.Status.Should().Be(ChoreTaskStatus.Archived);
        archivedTask.ArchivedAt.Should().NotBeNull();
    }

    #endregion

    #region GetTaskStatisticsAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task GetTaskStatisticsAsync_ReturnsAccurateStatistics()
    {
        // Arrange
        var task1 = new ChoreTask
        {
            Id = Guid.NewGuid(),
            ChildId = _childId,
            Title = "Task 1",
            RewardAmount = 10.00m,
            Status = ChoreTaskStatus.Active,
            CreatedById = _parentId,
            CreatedAt = DateTime.UtcNow,
            IsRecurring = false
        };

        var task2 = new ChoreTask
        {
            Id = Guid.NewGuid(),
            ChildId = _childId,
            Title = "Task 2",
            RewardAmount = 5.00m,
            Status = ChoreTaskStatus.Archived,
            ArchivedAt = DateTime.UtcNow,
            CreatedById = _parentId,
            CreatedAt = DateTime.UtcNow,
            IsRecurring = false
        };

        var completion1 = new TaskCompletion
        {
            Id = Guid.NewGuid(),
            TaskId = task1.Id,
            ChildId = _childId,
            CompletedAt = DateTime.UtcNow,
            Status = CompletionStatus.Approved,
            ApprovedById = _parentId,
            ApprovedAt = DateTime.UtcNow
        };

        var completion2 = new TaskCompletion
        {
            Id = Guid.NewGuid(),
            TaskId = task1.Id,
            ChildId = _childId,
            CompletedAt = DateTime.UtcNow,
            Status = CompletionStatus.PendingApproval
        };

        _context.Tasks.AddRange(task1, task2);
        _context.TaskCompletions.AddRange(completion1, completion2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetTaskStatisticsAsync(_childId, _parentId);

        // Assert
        result.TotalTasks.Should().Be(2);
        result.ActiveTasks.Should().Be(1);
        result.ArchivedTasks.Should().Be(1);
        result.TotalCompletions.Should().Be(2);
        result.PendingApprovals.Should().Be(1);
        result.TotalEarned.Should().Be(10.00m);
        result.PendingEarnings.Should().Be(10.00m);
    }

    #endregion
}
