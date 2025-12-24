using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs;

public record FamilyChildrenDto(
    Guid FamilyId,
    string FamilyName,
    List<ChildDetailDto> Children);

public record ChildDetailDto(
    Guid ChildId,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    decimal CurrentBalance,
    decimal SavingsBalance,
    decimal WeeklyAllowance,
    DateTime? LastAllowanceDate,
    DateTime? NextAllowanceDate,
    DayOfWeek? AllowanceDay,
    bool SavingsAccountEnabled,
    SavingsTransferType SavingsTransferType,
    decimal? SavingsTransferPercentage,
    decimal? SavingsTransferAmount,
    bool SavingsBalanceVisibleToChild,
    bool AllowDebt);
