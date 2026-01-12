using AllowanceTracker.DTOs.Tasks;
using AllowanceTracker.Models;

namespace AllowanceTracker.Services;

/// <summary>
/// Service for managing tasks and task completions
/// </summary>
public interface ITaskService
{
    // Task Management
    Task<TaskDto> CreateTaskAsync(CreateTaskDto dto, Guid createdById);
    Task<TaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskDto dto, Guid userId);
    Task<TaskDto> GetTaskByIdAsync(Guid taskId, Guid userId);
    Task<List<TaskDto>> GetTasksAsync(Guid? childId, ChoreTaskStatus? status, bool? isRecurring, Guid userId);
    Task ArchiveTaskAsync(Guid taskId, Guid userId);

    // Task Completions
    Task<TaskCompletionDto> CompleteTaskAsync(Guid taskId, CompleteTaskDto dto, Guid childId);
    Task<List<TaskCompletionDto>> GetTaskCompletionsAsync(Guid taskId, CompletionStatus? status, DateTime? startDate, DateTime? endDate, Guid userId);
    Task<List<TaskCompletionDto>> GetPendingApprovalsAsync(Guid userId);
    Task<TaskCompletionDto> ReviewCompletionAsync(Guid completionId, ReviewCompletionDto dto, Guid userId);

    // Statistics
    Task<TaskStatisticsDto> GetTaskStatisticsAsync(Guid childId, Guid userId);

    // Recurring Task Generation (Background Job)
    /// <summary>
    /// Generates task instances from recurring task templates.
    /// Called by background job to create today's recurring tasks.
    /// </summary>
    Task<int> GenerateRecurringTasksAsync();
}
