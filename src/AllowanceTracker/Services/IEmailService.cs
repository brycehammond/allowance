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
}
