using AllowanceTracker.DTOs.Tasks;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ICurrentUserService _currentUserService;

    public TasksController(ITaskService taskService, ICurrentUserService currentUserService)
    {
        _taskService = taskService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get all tasks (filtered by family)
    /// </summary>
    /// <param name="childId">Optional: Filter by specific child</param>
    /// <param name="status">Optional: Filter by status (Active, Archived)</param>
    /// <param name="isRecurring">Optional: Filter recurring tasks</param>
    [HttpGet]
    public async Task<ActionResult<List<TaskDto>>> GetTasks(
        [FromQuery] Guid? childId,
        [FromQuery] ChoreTaskStatus? status,
        [FromQuery] bool? isRecurring)
    {
        var userId = _currentUserService.UserId;
        var tasks = await _taskService.GetTasksAsync(childId, status, isRecurring, userId);
        return Ok(tasks);
    }

    /// <summary>
    /// Get task by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TaskDto>> GetTaskById(Guid id)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var task = await _taskService.GetTaskByIdAsync(id, userId);
            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Create new task (Parents only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskDto dto)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var task = await _taskService.CreateTaskAsync(dto, userId);
            return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Update task (Parents only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<TaskDto>> UpdateTask(Guid id, [FromBody] UpdateTaskDto dto)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var task = await _taskService.UpdateTaskAsync(id, dto, userId);
            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Archive task (soft delete) (Parents only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult> ArchiveTask(Guid id)
    {
        try
        {
            var userId = _currentUserService.UserId;
            await _taskService.ArchiveTaskAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Mark task as complete (Children only - assigned child)
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<ActionResult<TaskCompletionDto>> CompleteTask(Guid id, [FromBody] CompleteTaskDto dto)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var completion = await _taskService.CompleteTaskAsync(id, dto, userId);
            return CreatedAtAction(nameof(GetTaskCompletions), new { taskId = id }, completion);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Get completions for a task
    /// </summary>
    [HttpGet("{taskId}/completions")]
    public async Task<ActionResult<List<TaskCompletionDto>>> GetTaskCompletions(
        Guid taskId,
        [FromQuery] CompletionStatus? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var completions = await _taskService.GetTaskCompletionsAsync(taskId, status, startDate, endDate, userId);
            return Ok(completions);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Get all pending approvals for current user's family (Parents only)
    /// </summary>
    [HttpGet("completions/pending")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<List<TaskCompletionDto>>> GetPendingApprovals()
    {
        try
        {
            var userId = _currentUserService.UserId;
            var completions = await _taskService.GetPendingApprovalsAsync(userId);
            return Ok(completions);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Approve or reject a task completion (Parents only)
    /// </summary>
    [HttpPut("completions/{id}/review")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<TaskCompletionDto>> ReviewCompletion(Guid id, [FromBody] ReviewCompletionDto dto)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var completion = await _taskService.ReviewCompletionAsync(id, dto, userId);
            return Ok(completion);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Get task statistics for a child
    /// </summary>
    [HttpGet("children/{childId}/statistics")]
    public async Task<ActionResult<TaskStatisticsDto>> GetTaskStatistics(Guid childId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var statistics = await _taskService.GetTaskStatisticsAsync(childId, userId);
            return Ok(statistics);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }
}
