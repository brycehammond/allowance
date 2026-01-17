namespace AllowanceTracker.Services;

/// <summary>
/// No-op email service for local development when Azure Communication Services is not configured
/// </summary>
public class NoOpEmailService : IEmailService
{
    private readonly ILogger<NoOpEmailService> _logger;

    public NoOpEmailService(ILogger<NoOpEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetToken, string userName)
    {
        _logger.LogWarning(
            "NoOpEmailService: Would have sent password reset email to {Email} for user {UserName}. Token: {Token}",
            email, userName, resetToken);
        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string to, string subject, string htmlContent, string plainTextContent)
    {
        _logger.LogWarning(
            "NoOpEmailService: Would have sent email to {To} with subject '{Subject}'",
            to, subject);
        return Task.CompletedTask;
    }

    public Task SendParentInviteEmailAsync(string email, string token, string inviterName, string familyName, string recipientFirstName)
    {
        _logger.LogWarning(
            "NoOpEmailService: Would have sent parent invite email to {Email} from {Inviter} for family {Family}. Token: {Token}",
            email, inviterName, familyName, token);
        return Task.CompletedTask;
    }

    public Task SendJoinFamilyRequestEmailAsync(string email, string token, string inviterName, string familyName)
    {
        _logger.LogWarning(
            "NoOpEmailService: Would have sent join family request email to {Email} from {Inviter} for family {Family}. Token: {Token}",
            email, inviterName, familyName, token);
        return Task.CompletedTask;
    }

    public Task SendParentRemovedFromFamilyEmailAsync(string email, string parentFirstName, string familyName, string ownerName)
    {
        _logger.LogWarning(
            "NoOpEmailService: Would have sent removal notification email to {Email} ({Name}) from family {Family} by {Owner}",
            email, parentFirstName, familyName, ownerName);
        return Task.CompletedTask;
    }

    public Task SendGiftConfirmationEmailAsync(string email, string giverName, string childName, decimal amount)
    {
        _logger.LogWarning(
            "NoOpEmailService: Would have sent gift confirmation email to {Email} from {Giver} for {Child} amount {Amount:C}",
            email, giverName, childName, amount);
        return Task.CompletedTask;
    }

    public Task SendThankYouNoteEmailAsync(string email, string giverName, string childName, string message, string? imageUrl)
    {
        _logger.LogWarning(
            "NoOpEmailService: Would have sent thank you note email to {Email} from {Child} to {Giver}",
            email, childName, giverName);
        return Task.CompletedTask;
    }
}
