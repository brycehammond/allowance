using AllowanceTracker.DTOs;
using AllowanceTracker.Models;

namespace AllowanceTracker.Services;

public interface IFamilyService
{
    Task<List<ChildDto>> GetChildrenAsync();
    Task<ChildDto?> GetChildAsync(Guid childId);

    /// <summary>
    /// Get family information by user ID
    /// </summary>
    Task<FamilyInfoDto?> GetFamilyInfoAsync(Guid userId);

    /// <summary>
    /// Get all family members by user ID
    /// </summary>
    Task<FamilyMembersDto?> GetFamilyMembersAsync(Guid userId);

    /// <summary>
    /// Get all children in family by user ID
    /// </summary>
    Task<FamilyChildrenDto?> GetFamilyChildrenAsync(Guid userId);
}
