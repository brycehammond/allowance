using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace AllowanceTracker.Services;

/// <summary>
/// Email service implementation using SendGrid
/// </summary>
public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _resetPasswordUrl;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(
        ISendGridClient sendGridClient,
        IConfiguration configuration,
        ILogger<SendGridEmailService> logger)
    {
        _sendGridClient = sendGridClient;
        _logger = logger;
        _fromEmail = configuration["SendGrid:FromEmail"] ?? "noreply@allowancetracker.com";
        _fromName = configuration["SendGrid:FromName"] ?? "Allowance Tracker";
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
            var from = new EmailAddress(_fromEmail, _fromName);
            var toAddress = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, plainTextContent, htmlContent);

            var response = await _sendGridClient.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid returned error. Status: {StatusCode}, Body: {Body}",
                    response.StatusCode, body);
                throw new Exception($"Failed to send email: {response.StatusCode}");
            }

            _logger.LogInformation("Email sent successfully to {Email}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", to);
            throw;
        }
    }
}
