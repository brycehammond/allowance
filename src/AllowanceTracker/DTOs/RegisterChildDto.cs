using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs;

public record RegisterChildDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    decimal WeeklyAllowance,
    bool SavingsAccountEnabled = false,
    SavingsTransferType SavingsTransferType = SavingsTransferType.Percentage,
    decimal? SavingsTransferPercentage = null,
    decimal? SavingsTransferAmount = null,
    decimal InitialBalance = 0,
    decimal InitialSavingsBalance = 0);
