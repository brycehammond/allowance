using AllowanceTracker.DTOs.Allowances;

namespace AllowanceTracker.Services;

public interface IAllowanceService
{
    Task PayWeeklyAllowanceAsync(Guid childId);
    Task ProcessAllPendingAllowancesAsync();

    // Pause/Resume operations
    Task PauseAllowanceAsync(Guid childId, string? reason);
    Task ResumeAllowanceAsync(Guid childId);

    // Amount adjustment
    Task AdjustAllowanceAmountAsync(Guid childId, decimal newAmount, string? reason);

    // History
    Task<List<AllowanceAdjustmentDto>> GetAllowanceAdjustmentHistoryAsync(Guid childId);
}
