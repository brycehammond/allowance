using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs.Tasks;

/// <summary>
/// Response DTO for a task
/// </summary>
public record TaskDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    string Title,
    string? Description,
    decimal RewardAmount,
    ChoreTaskStatus Status,
    bool IsRecurring,
    RecurrenceType? RecurrenceType,
    string RecurrenceDisplay,
    DateTime CreatedAt,
    Guid CreatedById,
    string CreatedByName,
    int TotalCompletions,
    int PendingApprovals,
    DateTime? LastCompletedAt
);
