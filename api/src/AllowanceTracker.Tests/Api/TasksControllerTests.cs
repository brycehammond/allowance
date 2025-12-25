using AllowanceTracker.Api.V1;
using AllowanceTracker.DTOs.Tasks;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Api;

public class TasksControllerTests
{
    private readonly Mock<ITaskService> _mockTaskService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly TasksController _controller;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public TasksControllerTests()
    {
        _mockTaskService = new Mock<ITaskService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _mockCurrentUserService
            .Setup(x => x.UserId)
            .Returns(_currentUserId);

        _controller = new TasksController(_mockTaskService.Object, _mockCurrentUserService.Object);
    }

    #region GetTasks Tests

    [Fact]
    public async Task GetTasks_ReturnsOkWithTasks()
    {
        // Arrange
        var tasks = new List<TaskDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Child 1", "Clean room", null, 5m,
                ChoreTaskStatus.Active, false, null, "One-time", DateTime.UtcNow,
                Guid.NewGuid(), "Parent", 0, 0, null),
            new(Guid.NewGuid(), Guid.NewGuid(), "Child 2", "Take out trash", null, 2m,
                ChoreTaskStatus.Active, true, RecurrenceType.Weekly, "Weekly on Monday",
                DateTime.UtcNow, Guid.NewGuid(), "Parent", 3, 1, DateTime.UtcNow)
        };

        _mockTaskService
            .Setup(x => x.GetTasksAsync(null, null, null, _currentUserId))
            .ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetTasks(null, null, null);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTasks = okResult.Value.Should().BeAssignableTo<List<TaskDto>>().Subject;
        returnedTasks.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTasks_WithFilters_ReturnsFilteredTasks()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var tasks = new List<TaskDto>
        {
            new(Guid.NewGuid(), childId, "Child 1", "Clean room", null, 5m,
                ChoreTaskStatus.Active, false, null, "One-time", DateTime.UtcNow,
                Guid.NewGuid(), "Parent", 0, 0, null)
        };

        _mockTaskService
            .Setup(x => x.GetTasksAsync(childId, ChoreTaskStatus.Active, true, _currentUserId))
            .ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetTasks(childId, ChoreTaskStatus.Active, true);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTasks = okResult.Value.Should().BeAssignableTo<List<TaskDto>>().Subject;
        returnedTasks.Should().HaveCount(1);
    }

    #endregion

    #region GetTaskById Tests

    [Fact]
    public async Task GetTaskById_ReturnsOkWithTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new TaskDto(
            taskId, Guid.NewGuid(), "Child 1", "Clean room", "Make bed and vacuum", 5m,
            ChoreTaskStatus.Active, false, null, "One-time", DateTime.UtcNow,
            Guid.NewGuid(), "Parent", 0, 0, null);

        _mockTaskService
            .Setup(x => x.GetTaskByIdAsync(taskId, _currentUserId))
            .ReturnsAsync(task);

        // Act
        var result = await _controller.GetTaskById(taskId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTask = okResult.Value.Should().BeAssignableTo<TaskDto>().Subject;
        returnedTask.Title.Should().Be("Clean room");
    }

    [Fact]
    public async Task GetTaskById_ReturnsNotFound_WhenTaskNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _mockTaskService
            .Setup(x => x.GetTaskByIdAsync(taskId, _currentUserId))
            .ThrowsAsync(new InvalidOperationException("Task not found"));

        // Act
        var result = await _controller.GetTaskById(taskId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateTask Tests

    [Fact]
    public async Task CreateTask_ReturnsCreatedWithTask()
    {
        // Arrange
        var dto = new CreateTaskDto(
            Guid.NewGuid(), "Clean room", "Make bed", 5m, false, null, null, null);

        var createdTask = new TaskDto(
            Guid.NewGuid(), dto.ChildId, "Child 1", dto.Title, dto.Description, dto.RewardAmount,
            ChoreTaskStatus.Active, false, null, "One-time", DateTime.UtcNow,
            _currentUserId, "Parent", 0, 0, null);

        _mockTaskService
            .Setup(x => x.CreateTaskAsync(dto, _currentUserId))
            .ReturnsAsync(createdTask);

        // Act
        var result = await _controller.CreateTask(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedTask = createdResult.Value.Should().BeAssignableTo<TaskDto>().Subject;
        returnedTask.Title.Should().Be("Clean room");
    }

    [Fact]
    public async Task CreateTask_ReturnsBadRequest_WhenInvalidChild()
    {
        // Arrange
        var dto = new CreateTaskDto(
            Guid.NewGuid(), "Clean room", null, 5m, false, null, null, null);

        _mockTaskService
            .Setup(x => x.CreateTaskAsync(dto, _currentUserId))
            .ThrowsAsync(new InvalidOperationException("Child not found"));

        // Act
        var result = await _controller.CreateTask(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region UpdateTask Tests

    [Fact]
    public async Task UpdateTask_ReturnsOkWithUpdatedTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var dto = new UpdateTaskDto("Updated title", "New description", 10m, false, null, null, null);

        var updatedTask = new TaskDto(
            taskId, Guid.NewGuid(), "Child 1", dto.Title, dto.Description, dto.RewardAmount,
            ChoreTaskStatus.Active, false, null, "One-time", DateTime.UtcNow,
            _currentUserId, "Parent", 0, 0, null);

        _mockTaskService
            .Setup(x => x.UpdateTaskAsync(taskId, dto, _currentUserId))
            .ReturnsAsync(updatedTask);

        // Act
        var result = await _controller.UpdateTask(taskId, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTask = okResult.Value.Should().BeAssignableTo<TaskDto>().Subject;
        returnedTask.Title.Should().Be("Updated title");
    }

    [Fact]
    public async Task UpdateTask_ReturnsNotFound_WhenTaskNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var dto = new UpdateTaskDto("Title", null, 5m, false, null, null, null);

        _mockTaskService
            .Setup(x => x.UpdateTaskAsync(taskId, dto, _currentUserId))
            .ThrowsAsync(new InvalidOperationException("Task not found"));

        // Act
        var result = await _controller.UpdateTask(taskId, dto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region ArchiveTask Tests

    [Fact]
    public async Task ArchiveTask_ReturnsNoContent()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _mockTaskService
            .Setup(x => x.ArchiveTaskAsync(taskId, _currentUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ArchiveTask(taskId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ArchiveTask_ReturnsNotFound_WhenTaskNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _mockTaskService
            .Setup(x => x.ArchiveTaskAsync(taskId, _currentUserId))
            .ThrowsAsync(new InvalidOperationException("Task not found"));

        // Act
        var result = await _controller.ArchiveTask(taskId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CompleteTask Tests

    [Fact]
    public async Task CompleteTask_ReturnsCreatedWithCompletion()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var dto = new CompleteTaskDto("All done!", null);

        var completion = new TaskCompletionDto(
            Guid.NewGuid(), taskId, "Clean room", 5m, Guid.NewGuid(), "Child 1",
            DateTime.UtcNow, dto.Notes, null, CompletionStatus.PendingApproval,
            null, null, null, null, null);

        _mockTaskService
            .Setup(x => x.CompleteTaskAsync(taskId, dto, _currentUserId))
            .ReturnsAsync(completion);

        // Act
        var result = await _controller.CompleteTask(taskId, dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedCompletion = createdResult.Value.Should().BeAssignableTo<TaskCompletionDto>().Subject;
        returnedCompletion.Status.Should().Be(CompletionStatus.PendingApproval);
    }

    [Fact]
    public async Task CompleteTask_ReturnsBadRequest_WhenTaskArchived()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var dto = new CompleteTaskDto(null, null);

        _mockTaskService
            .Setup(x => x.CompleteTaskAsync(taskId, dto, _currentUserId))
            .ThrowsAsync(new InvalidOperationException("Cannot complete archived task"));

        // Act
        var result = await _controller.CompleteTask(taskId, dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetTaskCompletions Tests

    [Fact]
    public async Task GetTaskCompletions_ReturnsOkWithCompletions()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var completions = new List<TaskCompletionDto>
        {
            new(Guid.NewGuid(), taskId, "Clean room", 5m, Guid.NewGuid(), "Child 1",
                DateTime.UtcNow, null, null, CompletionStatus.Approved,
                Guid.NewGuid(), "Parent", DateTime.UtcNow, null, Guid.NewGuid()),
            new(Guid.NewGuid(), taskId, "Clean room", 5m, Guid.NewGuid(), "Child 1",
                DateTime.UtcNow, null, null, CompletionStatus.PendingApproval,
                null, null, null, null, null)
        };

        _mockTaskService
            .Setup(x => x.GetTaskCompletionsAsync(taskId, null, null, null, _currentUserId))
            .ReturnsAsync(completions);

        // Act
        var result = await _controller.GetTaskCompletions(taskId, null, null, null);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCompletions = okResult.Value.Should().BeAssignableTo<List<TaskCompletionDto>>().Subject;
        returnedCompletions.Should().HaveCount(2);
    }

    #endregion

    #region GetPendingApprovals Tests

    [Fact]
    public async Task GetPendingApprovals_ReturnsOkWithPendingCompletions()
    {
        // Arrange
        var completions = new List<TaskCompletionDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Clean room", 5m, Guid.NewGuid(), "Child 1",
                DateTime.UtcNow, null, null, CompletionStatus.PendingApproval,
                null, null, null, null, null),
            new(Guid.NewGuid(), Guid.NewGuid(), "Take out trash", 2m, Guid.NewGuid(), "Child 2",
                DateTime.UtcNow, null, null, CompletionStatus.PendingApproval,
                null, null, null, null, null)
        };

        _mockTaskService
            .Setup(x => x.GetPendingApprovalsAsync(_currentUserId))
            .ReturnsAsync(completions);

        // Act
        var result = await _controller.GetPendingApprovals();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCompletions = okResult.Value.Should().BeAssignableTo<List<TaskCompletionDto>>().Subject;
        returnedCompletions.Should().HaveCount(2);
        returnedCompletions.Should().OnlyContain(c => c.Status == CompletionStatus.PendingApproval);
    }

    #endregion

    #region ReviewCompletion Tests

    [Fact]
    public async Task ReviewCompletion_Approve_ReturnsOkWithApprovedCompletion()
    {
        // Arrange
        var completionId = Guid.NewGuid();
        var dto = new ReviewCompletionDto(true, null);

        var approvedCompletion = new TaskCompletionDto(
            completionId, Guid.NewGuid(), "Clean room", 5m, Guid.NewGuid(), "Child 1",
            DateTime.UtcNow, null, null, CompletionStatus.Approved,
            _currentUserId, "Parent", DateTime.UtcNow, null, Guid.NewGuid());

        _mockTaskService
            .Setup(x => x.ReviewCompletionAsync(completionId, dto, _currentUserId))
            .ReturnsAsync(approvedCompletion);

        // Act
        var result = await _controller.ReviewCompletion(completionId, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCompletion = okResult.Value.Should().BeAssignableTo<TaskCompletionDto>().Subject;
        returnedCompletion.Status.Should().Be(CompletionStatus.Approved);
        returnedCompletion.TransactionId.Should().NotBeNull();
    }

    [Fact]
    public async Task ReviewCompletion_Reject_ReturnsOkWithRejectedCompletion()
    {
        // Arrange
        var completionId = Guid.NewGuid();
        var dto = new ReviewCompletionDto(false, "Not done properly");

        var rejectedCompletion = new TaskCompletionDto(
            completionId, Guid.NewGuid(), "Clean room", 5m, Guid.NewGuid(), "Child 1",
            DateTime.UtcNow, null, null, CompletionStatus.Rejected,
            null, null, null, "Not done properly", null);

        _mockTaskService
            .Setup(x => x.ReviewCompletionAsync(completionId, dto, _currentUserId))
            .ReturnsAsync(rejectedCompletion);

        // Act
        var result = await _controller.ReviewCompletion(completionId, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCompletion = okResult.Value.Should().BeAssignableTo<TaskCompletionDto>().Subject;
        returnedCompletion.Status.Should().Be(CompletionStatus.Rejected);
        returnedCompletion.RejectionReason.Should().Be("Not done properly");
    }

    [Fact]
    public async Task ReviewCompletion_ReturnsNotFound_WhenCompletionNotFound()
    {
        // Arrange
        var completionId = Guid.NewGuid();
        var dto = new ReviewCompletionDto(true, null);

        _mockTaskService
            .Setup(x => x.ReviewCompletionAsync(completionId, dto, _currentUserId))
            .ThrowsAsync(new InvalidOperationException("Completion not found"));

        // Act
        var result = await _controller.ReviewCompletion(completionId, dto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion
}
