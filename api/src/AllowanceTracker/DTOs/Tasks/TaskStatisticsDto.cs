namespace AllowanceTracker.DTOs.Tasks;

/// <summary>
/// Response DTO for task statistics for a child
/// </summary>
public record TaskStatisticsDto(
    int TotalTasks,
    int ActiveTasks,
    int ArchivedTasks,
    int TotalCompletions,
    int PendingApprovals,
    decimal TotalEarned,
    decimal PendingEarnings,
    double CompletionRate
);
