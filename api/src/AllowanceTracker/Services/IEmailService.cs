namespace AllowanceTracker.Services;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a password reset email with a reset token
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="resetToken">Password reset token</param>
    /// <param name="userName">User's name for personalization</param>
    Task SendPasswordResetEmailAsync(string email, string resetToken, string userName);

    /// <summary>
    /// Send a generic email
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlContent">HTML email body</param>
    /// <param name="plainTextContent">Plain text email body</param>
    Task SendEmailAsync(string to, string subject, string htmlContent, string plainTextContent);

    /// <summary>
    /// Send a parent invite email to a new user
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="token">Invite token</param>
    /// <param name="inviterName">Name of the person sending the invite</param>
    /// <param name="familyName">Name of the family being invited to</param>
    /// <param name="recipientFirstName">First name of the recipient</param>
    Task SendParentInviteEmailAsync(string email, string token, string inviterName, string familyName, string recipientFirstName);

    /// <summary>
    /// Send a join family request email to an existing user
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="token">Invite token</param>
    /// <param name="inviterName">Name of the person sending the invite</param>
    /// <param name="familyName">Name of the family being invited to</param>
    Task SendJoinFamilyRequestEmailAsync(string email, string token, string inviterName, string familyName);

    /// <summary>
    /// Send notification email when a parent is removed from a family
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="parentFirstName">First name of the removed parent</param>
    /// <param name="familyName">Name of the family they were removed from</param>
    /// <param name="ownerName">Name of the family owner who removed them</param>
    Task SendParentRemovedFromFamilyEmailAsync(string email, string parentFirstName, string familyName, string ownerName);

    /// <summary>
    /// Send confirmation email to gift giver after submitting a gift
    /// </summary>
    /// <param name="email">Giver's email address</param>
    /// <param name="giverName">Giver's name</param>
    /// <param name="childName">Child's first name</param>
    /// <param name="amount">Gift amount</param>
    Task SendGiftConfirmationEmailAsync(string email, string giverName, string childName, decimal amount);

    /// <summary>
    /// Send thank you note email from child to gift giver
    /// </summary>
    /// <param name="email">Giver's email address</param>
    /// <param name="giverName">Giver's name</param>
    /// <param name="childName">Child's first name</param>
    /// <param name="message">Thank you message</param>
    /// <param name="imageUrl">Optional image URL</param>
    Task SendThankYouNoteEmailAsync(string email, string giverName, string childName, string message, string? imageUrl);
}
