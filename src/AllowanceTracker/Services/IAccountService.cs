using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.AspNetCore.Identity;

namespace AllowanceTracker.Services;

public interface IAccountService
{
    Task<IdentityResult> RegisterParentAsync(RegisterParentDto dto);
    Task<IdentityResult> RegisterChildAsync(RegisterChildDto dto, Guid familyId);
    Task<SignInResult> LoginAsync(string email, string password, bool rememberMe = false);
    Task LogoutAsync();
    Task<ApplicationUser?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
}
