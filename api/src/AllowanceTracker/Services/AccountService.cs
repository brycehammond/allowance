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
            // Create parent user first (to get the user Id for OwnerId)
            var userId = Guid.NewGuid();
            var familyId = Guid.NewGuid();

            // Create family with owner set to the new user
            var family = new Family
            {
                Id = familyId,
                Name = dto.FamilyName,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            // Create parent user
            var user = new ApplicationUser
            {
                Id = userId,
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
                // Add family after user is created to satisfy FK constraint
                _context.Families.Add(family);
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

    public async Task<IdentityResult> DeleteAccountAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        return await DeleteUserAndRelatedDataAsync(user);
    }

    public async Task<IdentityResult> DeleteAccountByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        return await DeleteUserAndRelatedDataAsync(user);
    }

    private async Task<IdentityResult> DeleteUserAndRelatedDataAsync(ApplicationUser user)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // If user is a child, delete child profile first
            if (user.Role == UserRole.Child)
            {
                var childProfile = await _context.Children
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (childProfile != null)
                {
                    // Delete all related child data (transactions, wish list items, etc.)
                    // Most should cascade, but be explicit for safety
                    var transactions = await _context.Transactions
                        .Where(t => t.ChildId == childProfile.Id)
                        .ToListAsync();
                    _context.Transactions.RemoveRange(transactions);

                    var wishListItems = await _context.WishListItems
                        .Where(w => w.ChildId == childProfile.Id)
                        .ToListAsync();
                    _context.WishListItems.RemoveRange(wishListItems);

                    var savingsTransactions = await _context.SavingsTransactions
                        .Where(s => s.ChildId == childProfile.Id)
                        .ToListAsync();
                    _context.SavingsTransactions.RemoveRange(savingsTransactions);

                    _context.Children.Remove(childProfile);
                    await _context.SaveChangesAsync();
                }
            }

            // If user is a parent and owns the family, delete the entire family
            if (user.Role == UserRole.Parent && user.FamilyId.HasValue)
            {
                var family = await _context.Families
                    .FirstOrDefaultAsync(f => f.Id == user.FamilyId.Value);

                if (family != null && family.OwnerId == user.Id)
                {
                    // Delete all family members (children and other parents)
                    var familyMembers = await _context.Users
                        .Where(u => u.FamilyId == family.Id && u.Id != user.Id)
                        .ToListAsync();

                    foreach (var member in familyMembers)
                    {
                        // Recursively delete each member
                        await DeleteUserAndRelatedDataAsync(member);
                    }

                    // Delete the family
                    _context.Families.Remove(family);
                    await _context.SaveChangesAsync();
                }
            }

            // Delete the user
            var result = await _userManager.DeleteAsync(user);

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
}
