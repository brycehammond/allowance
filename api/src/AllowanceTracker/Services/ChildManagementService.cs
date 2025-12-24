using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
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

    public async Task<Child?> UpdateChildSettingsAsync(Guid childId, UpdateChildSettingsDto dto, Guid requestingUserId)
    {
        var requestingUser = await _context.Users.FindAsync(requestingUserId);
        if (requestingUser == null || requestingUser.Role != UserRole.Parent)
        {
            return null;
        }

        var child = await _context.Children
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == childId);
        if (child == null)
        {
            return null;
        }

        // Check same family
        if (child.FamilyId != requestingUser.FamilyId)
        {
            return null;
        }

        // Update allowance
        child.WeeklyAllowance = dto.WeeklyAllowance;
        child.AllowanceDay = dto.AllowanceDay;

        // Update savings settings
        child.SavingsAccountEnabled = dto.SavingsAccountEnabled;
        child.SavingsTransferType = dto.SavingsAccountEnabled ? dto.SavingsTransferType : SavingsTransferType.None;

        if (dto.SavingsAccountEnabled)
        {
            if (dto.SavingsTransferType == SavingsTransferType.Percentage)
            {
                child.SavingsTransferPercentage = (int)(dto.SavingsTransferPercentage ?? 20);
                child.SavingsTransferAmount = 0;
            }
            else if (dto.SavingsTransferType == SavingsTransferType.FixedAmount)
            {
                child.SavingsTransferAmount = dto.SavingsTransferAmount ?? 0;
                child.SavingsTransferPercentage = 0;
            }
        }
        else
        {
            child.SavingsTransferPercentage = 0;
            child.SavingsTransferAmount = 0;
        }

        // Update savings balance visibility setting (only if provided)
        if (dto.SavingsBalanceVisibleToChild.HasValue)
        {
            child.SavingsBalanceVisibleToChild = dto.SavingsBalanceVisibleToChild.Value;
        }

        // Update allow debt setting (only if provided)
        if (dto.AllowDebt.HasValue)
        {
            child.AllowDebt = dto.AllowDebt.Value;
        }

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
