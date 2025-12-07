using Azure;
using Azure.Communication.Email;

namespace AllowanceTracker.Services;

/// <summary>
/// Email service implementation using Azure Communication Services
/// </summary>
public class AzureEmailService : IEmailService
{
    private readonly EmailClient _emailClient;
    private readonly string _fromEmail;
    private readonly string _resetPasswordUrl;
    private readonly ILogger<AzureEmailService> _logger;

    public AzureEmailService(
        EmailClient emailClient,
        IConfiguration configuration,
        ILogger<AzureEmailService> logger)
    {
        _emailClient = emailClient;
        _logger = logger;
        _fromEmail = configuration["AzureEmail:FromEmail"] ?? "noreply@allowancetracker.com";
        _resetPasswordUrl = configuration["App:ResetPasswordUrl"] ?? "http://localhost:5173/reset-password";
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken, string userName)
    {
        var resetUrl = $"{_resetPasswordUrl}?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(email)}";

        var htmlContent = $@"
            <html>
            <body>
                <h2>Password Reset Request</h2>
                <p>Hi {userName},</p>
                <p>We received a request to reset your password. Click the link below to reset it:</p>
                <p><a href=""{resetUrl}"">Reset Password</a></p>
                <p>If you didn't request this, you can safely ignore this email.</p>
                <p>This link will expire in 24 hours.</p>
                <br/>
                <p>Thanks,<br/>The Allowance Tracker Team</p>
            </body>
            </html>";

        var plainTextContent = $@"
            Password Reset Request

            Hi {userName},

            We received a request to reset your password. Copy and paste the link below to reset it:

            {resetUrl}

            If you didn't request this, you can safely ignore this email.

            This link will expire in 24 hours.

            Thanks,
            The Allowance Tracker Team";

        await SendEmailAsync(email, "Reset Your Password", htmlContent, plainTextContent);
    }

    public async Task SendEmailAsync(string to, string subject, string htmlContent, string plainTextContent)
    {
        try
        {
            var emailMessage = new EmailMessage(
                senderAddress: _fromEmail,
                recipientAddress: to,
                content: new EmailContent(subject)
                {
                    Html = htmlContent,
                    PlainText = plainTextContent
                });

            var operation = await _emailClient.SendAsync(WaitUntil.Started, emailMessage);

            _logger.LogInformation("Email sent successfully to {Email}, OperationId: {OperationId}",
                to, operation.Id);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Communication Services error sending email to {Email}. Status: {Status}",
                to, ex.Status);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", to);
            throw;
        }
    }
}
