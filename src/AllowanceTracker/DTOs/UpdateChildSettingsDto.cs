using System.ComponentModel.DataAnnotations;
using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs;

public record UpdateChildSettingsDto(
    [Required][Range(0, 10000)] decimal WeeklyAllowance,
    bool SavingsAccountEnabled = false,
    SavingsTransferType SavingsTransferType = SavingsTransferType.Percentage,
    [Range(0, 100)] decimal? SavingsTransferPercentage = null,
    [Range(0, 10000)] decimal? SavingsTransferAmount = null,
    DayOfWeek? AllowanceDay = null,
    bool? SavingsBalanceVisibleToChild = null);
