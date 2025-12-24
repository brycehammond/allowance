using System.ComponentModel.DataAnnotations;

namespace AllowanceTracker.DTOs.Tasks;

/// <summary>
/// Request to approve or reject a task completion
/// </summary>
public record ReviewCompletionDto(
    [Required]
    bool IsApproved,

    [StringLength(500)]
    string? RejectionReason
);
