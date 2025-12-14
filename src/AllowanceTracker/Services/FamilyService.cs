using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class FamilyService : IFamilyService
{
    private readonly AllowanceContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _emailService;

    public FamilyService(AllowanceContext context, ICurrentUserService currentUser, IEmailService emailService)
    {
        _context = context;
        _currentUser = currentUser;
        _emailService = emailService;
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
            .Include(f => f.Owner)
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
            family.OwnerId,
            family.Owner.FullName,
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
            m.Role.ToString(),
            m.Id == family.OwnerId)).ToList();

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
            c.SavingsBalance,
            c.WeeklyAllowance,
            c.LastAllowanceDate,
            c.LastAllowanceDate?.AddDays(7),
            c.AllowanceDay,
            c.SavingsBalanceVisibleToChild)).ToList();

        return new FamilyChildrenDto(family.Id, family.Name, children);
    }

    public async Task<bool> IsOwnerAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user?.FamilyId == null)
        {
            return false;
        }

        var family = await _context.Families.FindAsync(user.FamilyId);
        return family != null && family.OwnerId == userId;
    }

    public async Task RemoveParentAsync(Guid parentId, Guid requestingUserId)
    {
        var requestingUser = await _context.Users.FindAsync(requestingUserId);
        if (requestingUser?.FamilyId == null)
        {
            throw new InvalidOperationException("User not found or not in a family.");
        }

        var family = await _context.Families
            .Include(f => f.Owner)
            .FirstOrDefaultAsync(f => f.Id == requestingUser.FamilyId);

        if (family == null)
        {
            throw new InvalidOperationException("Family not found.");
        }

        // Verify the requesting user is the owner
        if (family.OwnerId != requestingUserId)
        {
            throw new UnauthorizedAccessException("Only the family owner can remove members.");
        }

        // Cannot remove yourself
        if (parentId == requestingUserId)
        {
            throw new InvalidOperationException("You cannot remove yourself. Transfer ownership first if you want to leave.");
        }

        var parentToRemove = await _context.Users.FindAsync(parentId);
        if (parentToRemove == null)
        {
            throw new InvalidOperationException("Parent not found.");
        }

        // Verify they are in the same family
        if (parentToRemove.FamilyId != family.Id)
        {
            throw new InvalidOperationException("Parent is not in your family.");
        }

        // Verify they are a parent, not a child
        if (parentToRemove.Role != UserRole.Parent)
        {
            throw new InvalidOperationException("This endpoint is for removing parents only. Use the children endpoint to manage children.");
        }

        // Remove from family
        parentToRemove.FamilyId = null;
        await _context.SaveChangesAsync();

        // Send notification email
        await _emailService.SendParentRemovedFromFamilyEmailAsync(
            parentToRemove.Email!,
            parentToRemove.FirstName,
            family.Name,
            family.Owner.FullName);
    }

    public async Task<FamilyInfoDto> TransferOwnershipAsync(Guid newOwnerId, Guid currentOwnerId)
    {
        var currentOwner = await _context.Users.FindAsync(currentOwnerId);
        if (currentOwner?.FamilyId == null)
        {
            throw new InvalidOperationException("Current user not found or not in a family.");
        }

        var family = await _context.Families
            .Include(f => f.Members)
            .Include(f => f.Children)
            .FirstOrDefaultAsync(f => f.Id == currentOwner.FamilyId);

        if (family == null)
        {
            throw new InvalidOperationException("Family not found.");
        }

        // Verify the current user is the owner
        if (family.OwnerId != currentOwnerId)
        {
            throw new UnauthorizedAccessException("Only the current owner can transfer ownership.");
        }

        var newOwner = await _context.Users.FindAsync(newOwnerId);
        if (newOwner == null)
        {
            throw new InvalidOperationException("New owner not found.");
        }

        // Verify new owner is in the same family
        if (newOwner.FamilyId != family.Id)
        {
            throw new InvalidOperationException("New owner is not in your family.");
        }

        // Verify new owner is a parent
        if (newOwner.Role != UserRole.Parent)
        {
            throw new InvalidOperationException("Ownership can only be transferred to a parent.");
        }

        // Transfer ownership
        family.OwnerId = newOwnerId;
        await _context.SaveChangesAsync();

        return new FamilyInfoDto(
            family.Id,
            family.Name,
            family.CreatedAt,
            family.OwnerId,
            newOwner.FullName,
            family.Members.Count,
            family.Children.Count);
    }

    public async Task LeaveFamilyAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user?.FamilyId == null)
        {
            throw new InvalidOperationException("User not found or not in a family.");
        }

        var family = await _context.Families.FindAsync(user.FamilyId);
        if (family == null)
        {
            throw new InvalidOperationException("Family not found.");
        }

        // Owner cannot leave - must transfer ownership first
        if (family.OwnerId == userId)
        {
            throw new InvalidOperationException("As the family owner, you must transfer ownership before leaving.");
        }

        // Verify they are a parent (children can't use this endpoint)
        if (user.Role != UserRole.Parent)
        {
            throw new InvalidOperationException("Only parents can leave the family using this endpoint.");
        }

        // Remove from family
        user.FamilyId = null;
        await _context.SaveChangesAsync();
    }
}
