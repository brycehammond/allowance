using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class FamilyService : IFamilyService
{
    private readonly AllowanceContext _context;
    private readonly ICurrentUserService _currentUser;

    public FamilyService(AllowanceContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<ChildDto>> GetChildrenAsync()
    {
        var user = await _context.Users.FindAsync(_currentUser.UserId);

        if (user == null || !user.FamilyId.HasValue)
            return new List<ChildDto>();

        var children = await _context.Children
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.FamilyId == user.FamilyId)
            .ToListAsync();

        return children.Select(c => ChildDto.FromChild(c, c.User)).ToList();
    }

    public async Task<ChildDto?> GetChildAsync(Guid childId)
    {
        var child = await _context.Children
            .AsNoTracking()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == childId);

        return child != null ? ChildDto.FromChild(child, child.User) : null;
    }
}
