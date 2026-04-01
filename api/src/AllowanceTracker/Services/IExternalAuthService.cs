using AllowanceTracker.DTOs;
using AllowanceTracker.Models;

namespace AllowanceTracker.Services;

/// <summary>
/// Information extracted from a validated external provider ID token
/// </summary>
public record ExternalTokenInfo(
    string ProviderKey,
    string Email,
    string? FirstName,
    string? LastName);

/// <summary>
/// Validates ID tokens from external authentication providers
/// </summary>
public interface IExternalTokenValidator
{
    Task<ExternalTokenInfo?> ValidateGoogleTokenAsync(string idToken);
    Task<ExternalTokenInfo?> ValidateAppleTokenAsync(string idToken);
}

/// <summary>
/// Result of an external authentication attempt
/// </summary>
public record ExternalLoginResult
{
    public bool Succeeded { get; init; }
    public ApplicationUser? User { get; init; }
    public bool IsNewUser { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static ExternalLoginResult Success(ApplicationUser user, bool isNewUser = false)
        => new() { Succeeded = true, User = user, IsNewUser = isNewUser };

    public static ExternalLoginResult Failure(string errorCode, string errorMessage)
        => new() { Succeeded = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
}

/// <summary>
/// Handles authentication via external providers (Google, Apple)
/// </summary>
public interface IExternalAuthService
{
    Task<ExternalLoginResult> ExternalLoginAsync(ExternalLoginDto dto);
}
