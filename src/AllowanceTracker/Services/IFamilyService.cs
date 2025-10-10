using AllowanceTracker.DTOs;
using AllowanceTracker.Models;

namespace AllowanceTracker.Services;

public interface IFamilyService
{
    Task<List<ChildDto>> GetChildrenAsync();
    Task<ChildDto?> GetChildAsync(Guid childId);
}
