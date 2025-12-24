namespace AllowanceTracker.DTOs;

/// <summary>
/// DTO for changing password when user is authenticated
/// </summary>
public record ChangePasswordDto(
    string CurrentPassword,
    string NewPassword);
