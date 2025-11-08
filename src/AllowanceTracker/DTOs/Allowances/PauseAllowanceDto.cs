namespace AllowanceTracker.DTOs.Allowances;

/// <summary>
/// DTO for pausing a child's allowance
/// </summary>
/// <param name="Reason">Reason for pausing the allowance (optional)</param>
public record PauseAllowanceDto(
    string? Reason
);
