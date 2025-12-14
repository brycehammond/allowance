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
    private readonly string _acceptInviteUrl;
    private readonly string _acceptJoinUrl;
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
        _acceptInviteUrl = configuration["App:AcceptInviteUrl"] ?? "http://localhost:5173/accept-invite";
        _acceptJoinUrl = configuration["App:AcceptJoinUrl"] ?? "http://localhost:5173/accept-join";
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

    public async Task SendParentInviteEmailAsync(string email, string token, string inviterName, string familyName, string recipientFirstName)
    {
        var inviteUrl = $"{_acceptInviteUrl}?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";

        var htmlContent = $@"
            <html>
            <body>
                <h2>You're Invited to Join {familyName} on Earn & Learn!</h2>
                <p>Hi {recipientFirstName},</p>
                <p>{inviterName} has invited you to join their family on Earn & Learn, the allowance tracking app that helps families teach kids about money.</p>
                <p>Click the button below to complete your registration and set your password:</p>
                <p><a href=""{inviteUrl}"" style=""display: inline-block; padding: 12px 24px; background-color: #2da370; color: white; text-decoration: none; border-radius: 6px;"">Accept Invitation</a></p>
                <p>Or copy and paste this link into your browser:</p>
                <p>{inviteUrl}</p>
                <p>This invitation will expire in 7 days.</p>
                <p>If you didn't expect this invitation, you can safely ignore this email.</p>
                <br/>
                <p>Thanks,<br/>The Earn & Learn Team</p>
            </body>
            </html>";

        var plainTextContent = $@"
            You're Invited to Join {familyName} on Earn & Learn!

            Hi {recipientFirstName},

            {inviterName} has invited you to join their family on Earn & Learn, the allowance tracking app that helps families teach kids about money.

            Click the link below to complete your registration and set your password:

            {inviteUrl}

            This invitation will expire in 7 days.

            If you didn't expect this invitation, you can safely ignore this email.

            Thanks,
            The Earn & Learn Team";

        await SendEmailAsync(email, $"You're invited to join {familyName} on Earn & Learn", htmlContent, plainTextContent);
    }

    public async Task SendJoinFamilyRequestEmailAsync(string email, string token, string inviterName, string familyName)
    {
        var joinUrl = $"{_acceptJoinUrl}?token={Uri.EscapeDataString(token)}";

        var htmlContent = $@"
            <html>
            <body>
                <h2>{inviterName} Invited You to Join {familyName}</h2>
                <p>Hi there,</p>
                <p>{inviterName} has invited you to join the {familyName} family on Earn & Learn.</p>
                <p>Since you already have an account, you can accept this invitation by logging in and clicking the link below:</p>
                <p><a href=""{joinUrl}"" style=""display: inline-block; padding: 12px 24px; background-color: #2da370; color: white; text-decoration: none; border-radius: 6px;"">Accept Invitation</a></p>
                <p>Or copy and paste this link into your browser:</p>
                <p>{joinUrl}</p>
                <p><strong>Note:</strong> Accepting this invitation will move you to the {familyName} family.</p>
                <p>This invitation will expire in 7 days.</p>
                <p>If you didn't expect this invitation, you can safely ignore this email.</p>
                <br/>
                <p>Thanks,<br/>The Earn & Learn Team</p>
            </body>
            </html>";

        var plainTextContent = $@"
            {inviterName} Invited You to Join {familyName}

            Hi there,

            {inviterName} has invited you to join the {familyName} family on Earn & Learn.

            Since you already have an account, you can accept this invitation by logging in and clicking the link below:

            {joinUrl}

            Note: Accepting this invitation will move you to the {familyName} family.

            This invitation will expire in 7 days.

            If you didn't expect this invitation, you can safely ignore this email.

            Thanks,
            The Earn & Learn Team";

        await SendEmailAsync(email, $"{inviterName} invited you to join {familyName}", htmlContent, plainTextContent);
    }
}
