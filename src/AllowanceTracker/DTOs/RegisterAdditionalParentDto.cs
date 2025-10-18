namespace AllowanceTracker.DTOs;

/// <summary>
/// Request model for adding an additional parent to an existing family
/// </summary>
/// <param name="Email">Parent's email address (will be used for login)</param>
/// <param name="Password">Account password (minimum 6 characters with at least one digit)</param>
/// <param name="FirstName">Parent's first name</param>
/// <param name="LastName">Parent's last name</param>
public record RegisterAdditionalParentDto(
    string Email,
    string Password,
    string FirstName,
    string LastName);
