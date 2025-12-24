namespace AllowanceTracker.Services;

public interface IAllowanceService
{
    Task PayWeeklyAllowanceAsync(Guid childId);
    Task ProcessAllPendingAllowancesAsync();
}
