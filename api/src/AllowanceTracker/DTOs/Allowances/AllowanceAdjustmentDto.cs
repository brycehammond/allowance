using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs.Allowances;

/// <summary>
/// DTO for allowance adjustment history record
/// </summary>
public record AllowanceAdjustmentDto(
    Guid Id,
    Guid ChildId,
    AllowanceAdjustmentType AdjustmentType,
    decimal? OldAmount,
    decimal? NewAmount,
    string? Reason,
    Guid AdjustedById,
    string AdjustedByName,
    DateTime AdjustedAt
);
