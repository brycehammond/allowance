using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AllowanceContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AllowanceContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
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
                    CurrentBalance = 0,
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
}
