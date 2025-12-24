using System.ComponentModel.DataAnnotations;

namespace AllowanceTracker.DTOs.Tasks;

/// <summary>
/// Request to mark a task as completed by a child
/// </summary>
public record CompleteTaskDto(
    [StringLength(500)]
    string? Notes,

    [StringLength(2048)]
    string? PhotoUrl
);
