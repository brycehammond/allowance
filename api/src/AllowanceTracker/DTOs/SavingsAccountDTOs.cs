using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs;

public record EnableSavingsAccountRequest(
    Guid ChildId,
    SavingsTransferType TransferType,
    decimal Amount); // Fixed amount OR percentage (0-100)

public record UpdateSavingsConfigRequest(
    Guid ChildId,
    SavingsTransferType TransferType,
    decimal Amount);

public record DepositToSavingsRequest(
    Guid ChildId,
    decimal Amount,
    string Description);

public record WithdrawFromSavingsRequest(
    Guid ChildId,
    decimal Amount,
    string Description);

public record SavingsAccountSummary(
    Guid ChildId,
    bool IsEnabled,
    decimal CurrentBalance,
    SavingsTransferType TransferType,
    decimal TransferAmount,
    int TransferPercentage,
    int TotalTransactions,
    decimal TotalDeposited,
    decimal TotalWithdrawn,
    DateTime? LastTransactionDate,
    string ConfigDescription); // Human-readable: "Saves $5.00 per allowance" or "Saves 20% per allowance"

public record SavingsTransactionDto(
    Guid Id,
    Guid ChildId,
    string Type,
    decimal Amount,
    string Description,
    decimal BalanceAfter,
    DateTime CreatedAt,
    Guid CreatedById,
    string CreatedByName);
