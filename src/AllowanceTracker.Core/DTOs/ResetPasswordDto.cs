namespace AllowanceTracker.DTOs;

/// <summary>
/// DTO for resetting password with a reset token
/// </summary>
public record ResetPasswordDto(
    string Email,
    string ResetToken,
    string NewPassword);
