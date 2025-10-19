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

    public async Task<FamilyInfoDto?> GetFamilyInfoAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user?.FamilyId == null)
        {
            return null;
        }

        var family = await _context.Families
            .AsNoTracking()
            .Include(f => f.Members)
            .Include(f => f.Children)
            .FirstOrDefaultAsync(f => f.Id == user.FamilyId);

        if (family == null)
        {
            return null;
        }

        return new FamilyInfoDto(
            family.Id,
            family.Name,
            family.CreatedAt,
            family.Members.Count,
            family.Children.Count);
    }

    public async Task<FamilyMembersDto?> GetFamilyMembersAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user?.FamilyId == null)
        {
            return null;
        }

        var family = await _context.Families
            .AsNoTracking()
            .Include(f => f.Members)
            .FirstOrDefaultAsync(f => f.Id == user.FamilyId);

        if (family == null)
        {
            return null;
        }

        var members = family.Members.Select(m => new FamilyMemberDto(
            m.Id,
            m.Email!,
            m.FirstName,
            m.LastName,
            m.Role.ToString())).ToList();

        return new FamilyMembersDto(family.Id, family.Name, members);
    }

    public async Task<FamilyChildrenDto?> GetFamilyChildrenAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user?.FamilyId == null)
        {
            return null;
        }

        var family = await _context.Families
            .AsNoTracking()
            .Include(f => f.Children)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(f => f.Id == user.FamilyId);

        if (family == null)
        {
            return null;
        }

        var children = family.Children.Select(c => new ChildDetailDto(
            c.Id,
            c.UserId,
            c.User.FirstName,
            c.User.LastName,
            c.User.Email!,
            c.CurrentBalance,
            c.WeeklyAllowance,
            c.LastAllowanceDate,
            c.LastAllowanceDate?.AddDays(7),
            c.AllowanceDay)).ToList();

        return new FamilyChildrenDto(family.Id, family.Name, children);
    }
}
