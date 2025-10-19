using AllowanceTracker.DTOs;
using AllowanceTracker.Models;

namespace AllowanceTracker.Services;

public interface ISavingsAccountService
{
    // Configuration
    Task EnableSavingsAccountAsync(Guid childId, SavingsTransferType transferType, decimal amount);
    Task DisableSavingsAccountAsync(Guid childId);
    Task UpdateSavingsConfigAsync(Guid childId, SavingsTransferType transferType, decimal amount);

    // Manual Transactions
    Task<SavingsTransaction> DepositToSavingsAsync(Guid childId, decimal amount, string description, Guid userId);
    Task<SavingsTransaction> WithdrawFromSavingsAsync(Guid childId, decimal amount, string description, Guid userId);

    // Automatic Transfer (called by AllowanceService)
    Task ProcessAutomaticTransferAsync(Guid childId, Guid allowanceTransactionId, decimal allowanceAmount);

    // Query
    Task<decimal> GetSavingsBalanceAsync(Guid childId);
    Task<List<SavingsTransaction>> GetSavingsHistoryAsync(Guid childId, int limit = 50);
    Task<SavingsAccountSummary> GetSummaryAsync(Guid childId);

    // Validation & Calculation
    decimal CalculateTransferAmount(decimal allowanceAmount, SavingsTransferType type, decimal configValue);
    bool ValidateSavingsConfig(SavingsTransferType type, decimal amount);
}
