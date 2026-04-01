using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class ExternalAuthService : IExternalAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AllowanceContext _context;
    private readonly IExternalTokenValidator _tokenValidator;

    private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Google", "Apple"
    };

    public ExternalAuthService(
        UserManager<ApplicationUser> userManager,
        AllowanceContext context,
        IExternalTokenValidator tokenValidator)
    {
        _userManager = userManager;
        _context = context;
        _tokenValidator = tokenValidator;
    }

    public async Task<ExternalLoginResult> ExternalLoginAsync(ExternalLoginDto dto)
    {
        if (!SupportedProviders.Contains(dto.Provider))
        {
            return ExternalLoginResult.Failure("UNSUPPORTED_PROVIDER",
                $"Provider '{dto.Provider}' is not supported. Supported providers: Google, Apple.");
        }

        // Validate the ID token with the appropriate provider
        var tokenInfo = dto.Provider.Equals("Google", StringComparison.OrdinalIgnoreCase)
            ? await _tokenValidator.ValidateGoogleTokenAsync(dto.IdToken)
            : await _tokenValidator.ValidateAppleTokenAsync(dto.IdToken);

        if (tokenInfo == null)
        {
            return ExternalLoginResult.Failure("INVALID_TOKEN",
                "The provided ID token is invalid or expired.");
        }

        // Normalize provider name for storage
        var provider = dto.Provider.Equals("Google", StringComparison.OrdinalIgnoreCase) ? "Google" : "Apple";

        // 1. Check if this external login already exists
        var existingUser = await _userManager.FindByLoginAsync(provider, tokenInfo.ProviderKey);
        if (existingUser != null)
        {
            return ExternalLoginResult.Success(existingUser);
        }

        // 2. Check if a user with this email already exists — link the account
        var userByEmail = await _userManager.FindByEmailAsync(tokenInfo.Email);
        if (userByEmail != null)
        {
            var addLoginResult = await _userManager.AddLoginAsync(userByEmail,
                new UserLoginInfo(provider, tokenInfo.ProviderKey, provider));

            if (!addLoginResult.Succeeded)
            {
                return ExternalLoginResult.Failure("LINK_FAILED",
                    "Failed to link external login to existing account.");
            }

            return ExternalLoginResult.Success(userByEmail);
        }

        // 3. New user — require family name
        if (string.IsNullOrWhiteSpace(dto.FamilyName))
        {
            return ExternalLoginResult.Failure("FAMILY_NAME_REQUIRED",
                "A family name is required to create a new account.");
        }

        // Resolve name: prefer DTO values (important for Apple where token has no name),
        // fall back to token values, then defaults
        var firstName = !string.IsNullOrWhiteSpace(dto.FirstName) ? dto.FirstName
            : !string.IsNullOrWhiteSpace(tokenInfo.FirstName) ? tokenInfo.FirstName
            : "User";
        var lastName = !string.IsNullOrWhiteSpace(dto.LastName) ? dto.LastName
            : !string.IsNullOrWhiteSpace(tokenInfo.LastName) ? tokenInfo.LastName
            : "";

        return await CreateNewUserAsync(provider, tokenInfo.ProviderKey, tokenInfo.Email,
            firstName, lastName, dto.FamilyName);
    }

    private async Task<ExternalLoginResult> CreateNewUserAsync(
        string provider, string providerKey, string email,
        string firstName, string lastName, string familyName)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Create family
            var family = new Family
            {
                Id = Guid.NewGuid(),
                Name = familyName,
                CreatedAt = DateTime.UtcNow
            };
            _context.Families.Add(family);
            await _context.SaveChangesAsync();

            // Create user without password
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = email,
                UserName = email,
                FirstName = firstName,
                LastName = lastName,
                Role = UserRole.Parent,
                FamilyId = family.Id,
                EmailConfirmed = true // Social providers verify email
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync();
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return ExternalLoginResult.Failure("USER_CREATION_FAILED", errors);
            }

            // Link external login
            var addLoginResult = await _userManager.AddLoginAsync(user,
                new UserLoginInfo(provider, providerKey, provider));

            if (!addLoginResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return ExternalLoginResult.Failure("USER_CREATION_FAILED",
                    "Failed to link external login to new account.");
            }

            await transaction.CommitAsync();
            return ExternalLoginResult.Success(user, isNewUser: true);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
