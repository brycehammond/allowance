using System.ComponentModel.DataAnnotations;

namespace AllowanceTracker.DTOs;

public record UpdateAllowanceDto(
    [Required][Range(0, 10000)] decimal WeeklyAllowance);
