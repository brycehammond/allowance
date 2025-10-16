namespace AllowanceTracker.DTOs;

/// <summary>
/// Request model for parent account registration
/// </summary>
/// <param name="Email">Parent's email address (will be used for login)</param>
/// <param name="Password">Account password (minimum 6 characters with at least one digit)</param>
/// <param name="FirstName">Parent's first name</param>
/// <param name="LastName">Parent's last name</param>
/// <param name="FamilyName">Name for the new family (will be created during registration)</param>
public record RegisterParentDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string FamilyName);
