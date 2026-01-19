using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.AspNetCore.Identity;

namespace AllowanceTracker.Services;

public interface IAccountService
{
    Task<IdentityResult> RegisterParentAsync(RegisterParentDto dto);
    Task<IdentityResult> RegisterAdditionalParentAsync(RegisterAdditionalParentDto dto, Guid familyId);
    Task<IdentityResult> RegisterChildAsync(RegisterChildDto dto, Guid familyId);
    Task<SignInResult> LoginAsync(string email, string password, bool rememberMe = false);
    Task LogoutAsync();
    Task<ApplicationUser?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<IdentityResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<bool> ForgotPasswordAsync(string email);
    Task<IdentityResult> ResetPasswordAsync(string email, string resetToken, string newPassword);
    Task<IdentityResult> DeleteAccountAsync(Guid userId);
    Task<IdentityResult> DeleteAccountByEmailAsync(string email);
}
