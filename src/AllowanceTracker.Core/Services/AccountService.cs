using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AllowanceContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailService _emailService;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AllowanceContext context,
        IHttpContextAccessor httpContextAccessor,
        IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
    }

    public async Task<IdentityResult> RegisterParentAsync(RegisterParentDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Create family
            var family = new Family
            {
                Id = Guid.NewGuid(),
                Name = dto.FamilyName,
                CreatedAt = DateTime.UtcNow
            };
            _context.Families.Add(family);
            await _context.SaveChangesAsync();

            // Create parent user
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                UserName = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = UserRole.Parent,
                FamilyId = family.Id
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (result.Succeeded)
            {
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }

            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IdentityResult> RegisterAdditionalParentAsync(RegisterAdditionalParentDto dto, Guid familyId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Verify family exists
            var family = await _context.Families.FindAsync(familyId);
            if (family == null)
                return IdentityResult.Failed(new IdentityError { Description = "Family not found" });

            // Create additional parent user
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                UserName = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = UserRole.Parent,
                FamilyId = familyId
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (result.Succeeded)
            {
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }

            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IdentityResult> RegisterChildAsync(RegisterChildDto dto, Guid familyId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Verify family exists
            var family = await _context.Families.FindAsync(familyId);
            if (family == null)
                return IdentityResult.Failed(new IdentityError { Description = "Family not found" });

            // Create child user
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                UserName = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = UserRole.Child,
                FamilyId = familyId
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (result.Succeeded)
            {
                // Create child profile
                var childProfile = new Child
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    FamilyId = familyId,
                    WeeklyAllowance = dto.WeeklyAllowance,
                    CurrentBalance = dto.InitialBalance,
                    AllowanceDay = dto.AllowanceDay,
                    SavingsAccountEnabled = dto.SavingsAccountEnabled,
                    SavingsTransferType = dto.SavingsTransferType,
                    SavingsTransferPercentage = dto.SavingsTransferPercentage.HasValue ? (int)dto.SavingsTransferPercentage.Value : 0,
                    SavingsTransferAmount = dto.SavingsTransferAmount ?? 0,
                    SavingsBalance = dto.InitialSavingsBalance,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Children.Add(childProfile);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }

            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<SignInResult> LoginAsync(string email, string password, bool rememberMe = false)
    {
        return await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User == null)
            return null;

        return await _userManager.GetUserAsync(httpContext.User);
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var user = await GetCurrentUserAsync();
        return user != null;
    }

    public async Task<IdentityResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        return await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return false; // Don't reveal that user doesn't exist for security

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var userName = $"{user.FirstName} {user.LastName}";

        await _emailService.SendPasswordResetEmailAsync(email, resetToken, userName);
        return true;
    }

    public async Task<IdentityResult> ResetPasswordAsync(string email, string resetToken, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        return await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
    }
}
