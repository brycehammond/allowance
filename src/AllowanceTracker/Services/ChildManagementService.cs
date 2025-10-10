using AllowanceTracker.Data;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class ChildManagementService : IChildManagementService
{
    private readonly AllowanceContext _context;

    public ChildManagementService(AllowanceContext context)
    {
        _context = context;
    }

    public async Task<Child?> GetChildAsync(Guid childId, Guid requestingUserId)
    {
        var child = await _context.Children
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == childId);

        if (child == null)
        {
            return null;
        }

        // Check authorization
        var canAccess = await CanUserAccessChildAsync(requestingUserId, childId);
        if (!canAccess)
        {
            return null;
        }

        return child;
    }

    public async Task<Child?> UpdateChildAllowanceAsync(Guid childId, decimal weeklyAllowance, Guid requestingUserId)
    {
        var requestingUser = await _context.Users.FindAsync(requestingUserId);
        if (requestingUser == null || requestingUser.Role != UserRole.Parent)
        {
            return null;
        }

        var child = await _context.Children.FindAsync(childId);
        if (child == null)
        {
            return null;
        }

        // Check same family
        if (child.FamilyId != requestingUser.FamilyId)
        {
            return null;
        }

        child.WeeklyAllowance = weeklyAllowance;
        await _context.SaveChangesAsync();

        return child;
    }

    public async Task<bool> DeleteChildAsync(Guid childId, Guid requestingUserId)
    {
        var requestingUser = await _context.Users.FindAsync(requestingUserId);
        if (requestingUser == null || requestingUser.Role != UserRole.Parent)
        {
            return false;
        }

        var child = await _context.Children
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == childId);

        if (child == null)
        {
            return false;
        }

        // Check same family
        if (child.FamilyId != requestingUser.FamilyId)
        {
            return false;
        }

        // Remove child profile and user
        _context.Children.Remove(child);
        _context.Users.Remove(child.User);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CanUserAccessChildAsync(Guid userId, Guid childId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        var child = await _context.Children.FindAsync(childId);
        if (child == null)
        {
            return false;
        }

        // Parent can access children in same family
        if (user.Role == UserRole.Parent)
        {
            return child.FamilyId == user.FamilyId;
        }

        // Child can only access their own data
        if (user.Role == UserRole.Child)
        {
            return child.UserId == user.Id;
        }

        return false;
    }
}
