using System.ComponentModel.DataAnnotations;

namespace AllowanceTracker.Models;

/// <summary>
/// Represents a child's completion of a task, pending parent approval
/// </summary>
public class TaskCompletion
{
    public Guid Id { get; set; }

    // Relationships
    public Guid TaskId { get; set; }
    public virtual ChoreTask Task { get; set; } = null!;

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    // Completion Details
    public DateTime CompletedAt { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(2048)]
    public string? PhotoUrl { get; set; }

    // Approval
    public CompletionStatus Status { get; set; } = CompletionStatus.PendingApproval;

    public Guid? ApprovedById { get; set; }
    public virtual ApplicationUser? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [StringLength(500)]
    public string? RejectionReason { get; set; }

    // Payment
    public Guid? TransactionId { get; set; }
    public virtual Transaction? Transaction { get; set; }
}

/// <summary>
/// Status of task completion approval workflow
/// </summary>
public enum CompletionStatus
{
    PendingApproval,  // Child completed, waiting for parent
    Approved,         // Parent approved, payment created
    Rejected          // Parent rejected
}
