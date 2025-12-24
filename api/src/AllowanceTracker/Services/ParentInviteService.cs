using System.Security.Cryptography;
using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class ParentInviteService : IParentInviteService
{
    private readonly AllowanceContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private const int InviteExpirationDays = 7;

    public ParentInviteService(
        AllowanceContext context,
        UserManager<ApplicationUser> userManager,
        IJwtService jwtService,
        IEmailService emailService)
    {
        _context = context;
        _userManager = userManager;
        _jwtService = jwtService;
        _emailService = emailService;
    }

    public async Task<ParentInviteResponseDto> SendInviteAsync(SendParentInviteDto dto, Guid inviterId, Guid familyId)
    {
        // Check if email already belongs to a member of the same family
        var existingFamilyMember = await _context.Users
            .AnyAsync(u => u.Email == dto.Email && u.FamilyId == familyId);

        if (existingFamilyMember)
        {
            throw new InvalidOperationException("This email is already associated with a member of your family.");
        }

        // Check for existing pending invite
        var existingInvite = await _context.ParentInvites
            .FirstOrDefaultAsync(i => i.InvitedEmail == dto.Email
                && i.FamilyId == familyId
                && i.Status == InviteStatus.Pending
                && i.ExpiresAt > DateTime.UtcNow);

        if (existingInvite != null)
        {
            throw new InvalidOperationException("An active invite already exists for this email.");
        }

        // Check if user with this email already exists (for join request flow)
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        var isExistingUser = existingUser != null;

        // Get inviter and family info for emails
        var inviter = await _context.Users.FindAsync(inviterId);
        var family = await _context.Families.FindAsync(familyId);

        if (inviter == null || family == null)
        {
            throw new InvalidOperationException("Inviter or family not found.");
        }

        // Generate secure token
        var token = GenerateSecureToken();

        // Create the invite
        var invite = new ParentInvite
        {
            Id = Guid.NewGuid(),
            InvitedEmail = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            FamilyId = familyId,
            InvitedById = inviterId,
            Token = token,
            IsExistingUser = isExistingUser,
            ExistingUserId = existingUser?.Id,
            Status = InviteStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(InviteExpirationDays)
        };

        // For new users, create a placeholder account without password
        if (!isExistingUser)
        {
            var placeholderUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = UserRole.Parent,
                FamilyId = familyId,
                EmailConfirmed = false // Mark as not confirmed until they accept invite
            };

            var createResult = await _userManager.CreateAsync(placeholderUser);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create placeholder user: {errors}");
            }
        }

        _context.ParentInvites.Add(invite);
        await _context.SaveChangesAsync();

        // Send appropriate email
        var inviterName = $"{inviter.FirstName} {inviter.LastName}";
        if (isExistingUser)
        {
            await _emailService.SendJoinFamilyRequestEmailAsync(dto.Email, token, inviterName, family.Name);
        }
        else
        {
            await _emailService.SendParentInviteEmailAsync(dto.Email, token, inviterName, family.Name, dto.FirstName);
        }

        var message = isExistingUser
            ? $"A join request has been sent to {dto.Email}. They will need to log in and accept the invitation."
            : $"An invitation has been sent to {dto.Email}. They will receive an email to complete their registration.";

        return new ParentInviteResponseDto(
            invite.Id,
            dto.Email,
            dto.FirstName,
            dto.LastName,
            isExistingUser,
            invite.ExpiresAt,
            message);
    }

    public async Task<ValidateInviteResponseDto> ValidateTokenAsync(string token, string email)
    {
        var invite = await _context.ParentInvites
            .Include(i => i.Family)
            .Include(i => i.InvitedBy)
            .FirstOrDefaultAsync(i => i.Token == token && i.InvitedEmail == email);

        if (invite == null)
        {
            return new ValidateInviteResponseDto(false, false, null, null, null, null, "Invalid invite token.");
        }

        if (invite.Status != InviteStatus.Pending)
        {
            return new ValidateInviteResponseDto(false, false, null, null, null, null,
                invite.Status == InviteStatus.Accepted ? "This invite has already been accepted." : "This invite is no longer valid.");
        }

        if (invite.ExpiresAt <= DateTime.UtcNow)
        {
            invite.Status = InviteStatus.Expired;
            await _context.SaveChangesAsync();
            return new ValidateInviteResponseDto(false, false, null, null, null, null, "This invite has expired.");
        }

        var inviterName = $"{invite.InvitedBy.FirstName} {invite.InvitedBy.LastName}";

        return new ValidateInviteResponseDto(
            true,
            invite.IsExistingUser,
            invite.FirstName,
            invite.LastName,
            invite.Family.Name,
            inviterName,
            null);
    }

    public async Task<AuthResponseDto> AcceptNewUserInviteAsync(AcceptInviteDto dto)
    {
        var invite = await _context.ParentInvites
            .Include(i => i.Family)
            .FirstOrDefaultAsync(i => i.Token == dto.Token && i.InvitedEmail == dto.Email);

        if (invite == null)
        {
            throw new InvalidOperationException("Invalid invite token.");
        }

        if (invite.Status != InviteStatus.Pending)
        {
            throw new InvalidOperationException("This invite is no longer valid.");
        }

        if (invite.ExpiresAt <= DateTime.UtcNow)
        {
            invite.Status = InviteStatus.Expired;
            await _context.SaveChangesAsync();
            throw new InvalidOperationException("This invite has expired.");
        }

        if (invite.IsExistingUser)
        {
            throw new InvalidOperationException("This invite is for an existing user. Please use the join request flow.");
        }

        // Find the placeholder user created during invite
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            throw new InvalidOperationException("User account not found. Please request a new invite.");
        }

        // Set the password
        var passwordResult = await _userManager.AddPasswordAsync(user, dto.Password);
        if (!passwordResult.Succeeded)
        {
            var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to set password: {errors}");
        }

        // Mark email as confirmed
        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);

        // Mark invite as accepted
        invite.Status = InviteStatus.Accepted;
        invite.AcceptedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Generate JWT token for auto-login
        var token = _jwtService.GenerateToken(user);

        return new AuthResponseDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.FamilyId,
            invite.Family.Name,
            token,
            DateTime.UtcNow.AddDays(1));
    }

    public async Task<AcceptJoinResponseDto> AcceptJoinRequestAsync(string token, Guid currentUserId)
    {
        var invite = await _context.ParentInvites
            .Include(i => i.Family)
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invite == null)
        {
            throw new InvalidOperationException("Invalid invite token.");
        }

        if (invite.Status != InviteStatus.Pending)
        {
            throw new InvalidOperationException("This invite is no longer valid.");
        }

        if (invite.ExpiresAt <= DateTime.UtcNow)
        {
            invite.Status = InviteStatus.Expired;
            await _context.SaveChangesAsync();
            throw new InvalidOperationException("This invite has expired.");
        }

        if (!invite.IsExistingUser)
        {
            throw new InvalidOperationException("This invite is for a new user. Please use the registration flow.");
        }

        // Verify the current user matches the invite
        var currentUser = await _context.Users.FindAsync(currentUserId);
        if (currentUser == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (currentUser.Email != invite.InvitedEmail)
        {
            throw new InvalidOperationException("This invite was sent to a different email address.");
        }

        // Update user's family
        currentUser.FamilyId = invite.FamilyId;
        currentUser.Role = UserRole.Parent; // Ensure they have parent role

        // Mark invite as accepted
        invite.Status = InviteStatus.Accepted;
        invite.AcceptedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new AcceptJoinResponseDto(
            invite.FamilyId,
            invite.Family.Name,
            $"You have successfully joined {invite.Family.Name}!");
    }

    public async Task<bool> CancelInviteAsync(Guid inviteId, Guid userId)
    {
        var invite = await _context.ParentInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId);

        if (invite == null)
        {
            return false;
        }

        // Verify the user is from the same family
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.FamilyId != invite.FamilyId)
        {
            return false;
        }

        if (invite.Status != InviteStatus.Pending)
        {
            return false;
        }

        // If this was a new user invite, delete the placeholder user
        if (!invite.IsExistingUser)
        {
            var placeholderUser = await _userManager.FindByEmailAsync(invite.InvitedEmail);
            if (placeholderUser != null && !placeholderUser.EmailConfirmed)
            {
                await _userManager.DeleteAsync(placeholderUser);
            }
        }

        invite.Status = InviteStatus.Cancelled;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<ParentInviteDto>> GetPendingInvitesAsync(Guid familyId)
    {
        var invites = await _context.ParentInvites
            .Include(i => i.InvitedBy)
            .Where(i => i.FamilyId == familyId && i.Status == InviteStatus.Pending && i.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invites.Select(i => new ParentInviteDto(
            i.Id,
            i.InvitedEmail,
            i.FirstName,
            i.LastName,
            i.IsExistingUser,
            i.Status,
            i.ExpiresAt,
            i.CreatedAt,
            $"{i.InvitedBy.FirstName} {i.InvitedBy.LastName}")).ToList();
    }

    public async Task<int> ExpireOldInvitesAsync()
    {
        var expiredInvites = await _context.ParentInvites
            .Where(i => i.Status == InviteStatus.Pending && i.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var invite in expiredInvites)
        {
            invite.Status = InviteStatus.Expired;

            // If this was a new user invite, delete the placeholder user
            if (!invite.IsExistingUser)
            {
                var placeholderUser = await _userManager.FindByEmailAsync(invite.InvitedEmail);
                if (placeholderUser != null && !placeholderUser.EmailConfirmed)
                {
                    await _userManager.DeleteAsync(placeholderUser);
                }
            }
        }

        await _context.SaveChangesAsync();
        return expiredInvites.Count;
    }

    public async Task<ParentInviteResponseDto> ResendInviteAsync(Guid inviteId, Guid userId)
    {
        var invite = await _context.ParentInvites
            .Include(i => i.Family)
            .Include(i => i.InvitedBy)
            .FirstOrDefaultAsync(i => i.Id == inviteId);

        if (invite == null)
        {
            throw new InvalidOperationException("Invite not found.");
        }

        // Verify the user is from the same family
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.FamilyId != invite.FamilyId)
        {
            throw new InvalidOperationException("You don't have permission to resend this invite.");
        }

        // Only allow resending pending or expired invites
        if (invite.Status != InviteStatus.Pending && invite.Status != InviteStatus.Expired)
        {
            throw new InvalidOperationException("This invite cannot be resent.");
        }

        // Generate new token and extend expiration
        invite.Token = GenerateSecureToken();
        invite.ExpiresAt = DateTime.UtcNow.AddDays(InviteExpirationDays);
        invite.Status = InviteStatus.Pending; // Reset to pending if it was expired

        await _context.SaveChangesAsync();

        // Resend the email
        var inviterName = $"{invite.InvitedBy.FirstName} {invite.InvitedBy.LastName}";
        if (invite.IsExistingUser)
        {
            await _emailService.SendJoinFamilyRequestEmailAsync(invite.InvitedEmail, invite.Token, inviterName, invite.Family.Name);
        }
        else
        {
            await _emailService.SendParentInviteEmailAsync(invite.InvitedEmail, invite.Token, inviterName, invite.Family.Name, invite.FirstName);
        }

        var message = invite.IsExistingUser
            ? $"A new join request has been sent to {invite.InvitedEmail}."
            : $"A new invitation has been sent to {invite.InvitedEmail}.";

        return new ParentInviteResponseDto(
            invite.Id,
            invite.InvitedEmail,
            invite.FirstName,
            invite.LastName,
            invite.IsExistingUser,
            invite.ExpiresAt,
            message);
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
