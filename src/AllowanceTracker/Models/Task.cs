using System.ComponentModel.DataAnnotations;

namespace AllowanceTracker.Models;

/// <summary>
/// Represents a chore or task that can be assigned to a child with a monetary reward
/// </summary>
public class ChoreTask
{
    public Guid Id { get; set; }

    // Relationships
    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    public Guid CreatedById { get; set; }
    public virtual ApplicationUser CreatedBy { get; set; } = null!;

    // Task Details
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0.01, 1000.00)]
    public decimal RewardAmount { get; set; }

    // Status
    public ChoreTaskStatus Status { get; set; } = ChoreTaskStatus.Active;

    // Recurrence
    public bool IsRecurring { get; set; }
    public RecurrenceType? RecurrenceType { get; set; }
    public DayOfWeek? RecurrenceDay { get; set; } // For weekly tasks

    [Range(1, 28)]
    public int? RecurrenceDayOfMonth { get; set; } // For monthly tasks (1-28 to avoid month-end issues)

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }

    // Navigation
    public virtual ICollection<TaskCompletion> Completions { get; set; } = new List<TaskCompletion>();

    /// <summary>
    /// Display string for recurrence schedule
    /// </summary>
    public string RecurrenceDisplay
    {
        get
        {
            if (!IsRecurring || RecurrenceType == null)
                return "One-time";

            return RecurrenceType switch
            {
                Models.RecurrenceType.Daily => "Daily",
                Models.RecurrenceType.Weekly => RecurrenceDay.HasValue
                    ? $"Weekly on {RecurrenceDay.Value}"
                    : "Weekly",
                Models.RecurrenceType.Monthly => RecurrenceDayOfMonth.HasValue
                    ? $"Monthly on day {RecurrenceDayOfMonth.Value}"
                    : "Monthly",
                _ => "Unknown"
            };
        }
    }
}

/// <summary>
/// Task status indicating if it can be completed
/// </summary>
public enum ChoreTaskStatus
{
    Active,      // Can be completed
    Archived     // No longer active, soft deleted
}

/// <summary>
/// Recurrence pattern for recurring tasks
/// </summary>
public enum RecurrenceType
{
    Daily,
    Weekly,
    Monthly
}
