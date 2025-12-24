using System.ComponentModel.DataAnnotations;

namespace AllowanceTracker.DTOs.Allowances;

/// <summary>
/// DTO for adjusting a child's weekly allowance amount
/// </summary>
/// <param name="NewAmount">New weekly allowance amount (must be >= 0)</param>
/// <param name="Reason">Reason for the adjustment (optional)</param>
public record AdjustAllowanceAmountDto(
    [Range(0, 10000, ErrorMessage = "Weekly allowance must be between $0 and $10,000")]
    decimal NewAmount,

    [MaxLength(500, ErrorMessage = "Reason must be 500 characters or less")]
    string? Reason
);
