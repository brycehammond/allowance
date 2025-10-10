using AllowanceTracker.Models;

namespace AllowanceTracker.Services;

public interface IChildManagementService
{
    /// <summary>
    /// Get child by ID with authorization check (parent in same family or the child themselves)
    /// </summary>
    Task<Child?> GetChildAsync(Guid childId, Guid requestingUserId);

    /// <summary>
    /// Update child's weekly allowance (parent only)
    /// </summary>
    Task<Child?> UpdateChildAllowanceAsync(Guid childId, decimal weeklyAllowance, Guid requestingUserId);

    /// <summary>
    /// Delete child from family (parent only)
    /// </summary>
    Task<bool> DeleteChildAsync(Guid childId, Guid requestingUserId);

    /// <summary>
    /// Check if user can access child data
    /// </summary>
    Task<bool> CanUserAccessChildAsync(Guid userId, Guid childId);
}
