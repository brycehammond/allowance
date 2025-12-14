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

    /// <summary>
    /// Check if a user is the owner of their family
    /// </summary>
    Task<bool> IsOwnerAsync(Guid userId);

    /// <summary>
    /// Remove a co-parent from the family (owner only)
    /// </summary>
    /// <param name="parentId">ID of the parent to remove</param>
    /// <param name="requestingUserId">ID of the user making the request (must be owner)</param>
    Task RemoveParentAsync(Guid parentId, Guid requestingUserId);

    /// <summary>
    /// Transfer family ownership to another parent (owner only)
    /// </summary>
    /// <param name="newOwnerId">ID of the new owner</param>
    /// <param name="currentOwnerId">ID of the current owner making the request</param>
    /// <returns>Updated family info</returns>
    Task<FamilyInfoDto> TransferOwnershipAsync(Guid newOwnerId, Guid currentOwnerId);

    /// <summary>
    /// Leave the family (non-owners only)
    /// </summary>
    /// <param name="userId">ID of the user leaving</param>
    Task LeaveFamilyAsync(Guid userId);
}
