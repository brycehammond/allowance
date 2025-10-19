namespace AllowanceTracker.DTOs;

/// <summary>
/// Response returned after successful authentication (login or registration)
/// </summary>
/// <param name="UserId">Unique identifier for the user</param>
/// <param name="Email">User's email address</param>
/// <param name="FirstName">User's first name</param>
/// <param name="LastName">User's last name</param>
/// <param name="Role">User's role (Parent or Child)</param>
/// <param name="FamilyId">ID of the family the user belongs to (null if no family assigned)</param>
/// <param name="FamilyName">Name of the family (null if no family assigned)</param>
/// <param name="Token">JWT bearer token for API authentication</param>
/// <param name="ExpiresAt">Token expiration timestamp (UTC)</param>
public record AuthResponseDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    Guid? FamilyId,
    string? FamilyName,
    string Token,
    DateTime ExpiresAt);
