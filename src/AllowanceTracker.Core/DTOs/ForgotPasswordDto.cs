namespace AllowanceTracker.DTOs;

/// <summary>
/// DTO for requesting a password reset email
/// </summary>
public record ForgotPasswordDto(
    string Email);
