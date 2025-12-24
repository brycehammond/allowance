using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs.Tasks;

/// <summary>
/// Response DTO for a task completion
/// </summary>
public record TaskCompletionDto(
    Guid Id,
    Guid TaskId,
    string TaskTitle,
    decimal RewardAmount,
    Guid ChildId,
    string ChildName,
    DateTime CompletedAt,
    string? Notes,
    string? PhotoUrl,
    CompletionStatus Status,
    Guid? ApprovedById,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    string? RejectionReason,
    Guid? TransactionId
);
