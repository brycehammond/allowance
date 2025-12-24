using AllowanceTracker.Models;
using System.ComponentModel.DataAnnotations;

namespace AllowanceTracker.DTOs.Tasks;

/// <summary>
/// Request to update an existing task
/// </summary>
public record UpdateTaskDto(
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string Title,

    [StringLength(500)]
    string? Description,

    [Required]
    [Range(0.01, 1000.00)]
    decimal RewardAmount,

    bool IsRecurring,

    RecurrenceType? RecurrenceType,

    DayOfWeek? RecurrenceDay,

    [Range(1, 28)]
    int? RecurrenceDayOfMonth
);
