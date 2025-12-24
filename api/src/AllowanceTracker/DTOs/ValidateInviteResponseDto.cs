namespace AllowanceTracker.DTOs;

/// <summary>
/// Response for validating an invite token
/// </summary>
public record ValidateInviteResponseDto(
    bool IsValid,
    bool IsExistingUser,
    string? FirstName,
    string? LastName,
    string? FamilyName,
    string? InviterName,
    string? ErrorMessage);
