# Notifications System - Comprehensive In-App and Email Notifications

## Overview
Full-featured notification system providing real-time in-app notifications and email alerts for key events. Parents and children receive timely updates about transactions, allowances, goals, and achievements through customizable notification preferences.

## Core Philosophy: Test-First Development
**Every feature starts with a failing test**. Follow strict TDD methodology for all notification functionality.

## Technology Stack

### Core Dependencies
```xml
<ItemGroup>
  <!-- Email Services -->
  <PackageReference Include="SendGrid" Version="9.28.1" />
  <PackageReference Include="MailKit" Version="4.3.0" />

  <!-- Real-time Notifications -->
  <!-- SignalR is built into Blazor Server -->

  <!-- Templating -->
  <PackageReference Include="RazorLight" Version="2.3.0" />
</ItemGroup>
```

## Database Schema

### Notification Model
```csharp
public class Notification
{
    public Guid Id { get; set; }
    public Guid RecipientId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }

    // Navigation properties
    public virtual ApplicationUser Recipient { get; set; } = null!;
}

public enum NotificationType
{
    TransactionCreated = 0,
    AllowancePaid = 1,
    GoalReached = 2,
    GoalProgress = 3,
    LowBalance = 4,
    ApprovalRequest = 5,
    ApprovalGranted = 6,
    ApprovalDenied = 7,
    AchievementUnlocked = 8,
    WeeklyDigest = 9,
    SystemAlert = 10
}

public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}
```

### NotificationPreferences Model
```csharp
public class NotificationPreferences
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // In-App Notifications
    public bool InAppEnabled { get; set; } = true;
    public bool InAppTransactions { get; set; } = true;
    public bool InAppAllowance { get; set; } = true;
    public bool InAppGoals { get; set; } = true;
    public bool InAppAchievements { get; set; } = true;

    // Email Notifications
    public bool EmailEnabled { get; set; } = true;
    public bool EmailTransactions { get; set; } = true;
    public bool EmailAllowance { get; set; } = true;
    public bool EmailGoals { get; set; } = true;
    public bool EmailAchievements { get; set; } = false;
    public bool EmailWeeklyDigest { get; set; } = true;

    // Digest Settings
    public DayOfWeek WeeklyDigestDay { get; set; } = DayOfWeek.Sunday;
    public int WeeklyDigestHour { get; set; } = 9; // 9 AM

    // Thresholds
    public decimal? LowBalanceThreshold { get; set; }
    public bool LowBalanceNotifications { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
}
```

### DbContext Configuration
```csharp
// Add to AllowanceContext.cs

public DbSet<Notification> Notifications { get; set; }
public DbSet<NotificationPreferences> NotificationPreferences { get; set; }

protected override void OnModelCreating(ModelBuilder builder)
{
    // ... existing configuration ...

    // Notification configuration
    builder.Entity<Notification>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
        entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
        entity.Property(e => e.ActionUrl).HasMaxLength(500);
        entity.Property(e => e.RelatedEntityType).HasMaxLength(100);

        entity.HasOne(e => e.Recipient)
              .WithMany()
              .HasForeignKey(e => e.RecipientId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(e => e.RecipientId);
        entity.HasIndex(e => e.IsRead);
        entity.HasIndex(e => e.CreatedAt);
        entity.HasIndex(e => new { e.RecipientId, e.IsRead });
    });

    // NotificationPreferences configuration
    builder.Entity<NotificationPreferences>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
              .WithOne()
              .HasForeignKey<NotificationPreferences>(e => e.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(e => e.UserId).IsUnique();
    });
}
```

## Service Interfaces

### INotificationService Interface
```csharp
public interface INotificationService
{
    // Create Notifications
    Task<Notification> CreateNotificationAsync(CreateNotificationDto dto);
    Task CreateTransactionNotificationAsync(Transaction transaction);
    Task CreateAllowancePaidNotificationAsync(Child child, Transaction transaction);
    Task CreateGoalReachedNotificationAsync(Child child, WishListItem goal);
    Task CreateLowBalanceNotificationAsync(Child child);
    Task CreateAchievementNotificationAsync(ApplicationUser user, string achievementName);

    // Read Notifications
    Task<List<Notification>> GetUnreadNotificationsAsync(Guid userId);
    Task<List<Notification>> GetAllNotificationsAsync(Guid userId, int limit = 50);
    Task<int> GetUnreadCountAsync(Guid userId);

    // Mark as Read
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId);

    // Delete Notifications
    Task DeleteNotificationAsync(Guid notificationId);
    Task DeleteOldNotificationsAsync(int daysOld = 30);
}
```

### IEmailNotificationService Interface
```csharp
public interface IEmailNotificationService
{
    Task SendTransactionEmailAsync(Transaction transaction, ApplicationUser recipient);
    Task SendAllowancePaidEmailAsync(Child child, Transaction transaction);
    Task SendGoalReachedEmailAsync(Child child, WishListItem goal);
    Task SendWeeklyDigestEmailAsync(Guid userId);
    Task SendLowBalanceEmailAsync(Child child);
    Task<bool> IsEmailConfiguredAsync();
}
```

### INotificationPreferencesService Interface
```csharp
public interface INotificationPreferencesService
{
    Task<NotificationPreferences> GetPreferencesAsync(Guid userId);
    Task<NotificationPreferences> UpdatePreferencesAsync(Guid userId, UpdatePreferencesDto dto);
    Task<NotificationPreferences> CreateDefaultPreferencesAsync(Guid userId);
    Task<bool> ShouldSendNotificationAsync(Guid userId, NotificationType type, NotificationChannel channel);
}

public enum NotificationChannel
{
    InApp,
    Email
}
```

## Service Implementation

### NotificationService Implementation
```csharp
public class NotificationService : INotificationService
{
    private readonly AllowanceContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly INotificationPreferencesService _preferencesService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AllowanceContext context,
        IHubContext<NotificationHub> hubContext,
        INotificationPreferencesService preferencesService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _preferencesService = preferencesService;
        _logger = logger;
    }

    public async Task<Notification> CreateNotificationAsync(CreateNotificationDto dto)
    {
        // Check if user wants this type of notification
        if (!await _preferencesService.ShouldSendNotificationAsync(
            dto.RecipientId,
            dto.Type,
            NotificationChannel.InApp))
        {
            _logger.LogDebug("Notification skipped due to user preferences: {Type} for {UserId}",
                dto.Type, dto.RecipientId);
            return null!;
        }

        var notification = new Notification
        {
            RecipientId = dto.RecipientId,
            Type = dto.Type,
            Title = dto.Title,
            Message = dto.Message,
            ActionUrl = dto.ActionUrl,
            RelatedEntityId = dto.RelatedEntityId,
            RelatedEntityType = dto.RelatedEntityType,
            Priority = dto.Priority,
            ExpiresAt = dto.ExpiresAt
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification via SignalR
        await _hubContext.Clients.User(dto.RecipientId.ToString())
            .SendAsync("ReceiveNotification", MapToDto(notification));

        _logger.LogInformation("Notification created: {NotificationId} for user {UserId}",
            notification.Id, dto.RecipientId);

        return notification;
    }

    public async Task CreateTransactionNotificationAsync(Transaction transaction)
    {
        var child = await _context.Children
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == transaction.ChildId);

        if (child == null) return;

        var title = transaction.Type == TransactionType.Credit
            ? "Money Added"
            : "Money Spent";

        var message = $"{transaction.Amount:C} {(transaction.Type == TransactionType.Credit ? "added to" : "spent from")} your account. {transaction.Description}";

        await CreateNotificationAsync(new CreateNotificationDto(
            child.UserId,
            NotificationType.TransactionCreated,
            title,
            message,
            $"/transactions/{transaction.Id}",
            transaction.Id,
            "Transaction",
            NotificationPriority.Normal,
            null
        ));

        // Also notify parents
        var parents = await _context.Users
            .Where(u => u.FamilyId == child.FamilyId && u.Role == UserRole.Parent)
            .ToListAsync();

        foreach (var parent in parents)
        {
            await CreateNotificationAsync(new CreateNotificationDto(
                parent.Id,
                NotificationType.TransactionCreated,
                $"{child.User.FirstName} - {title}",
                $"{child.User.FirstName}: {message}",
                $"/children/{child.Id}/transactions",
                transaction.Id,
                "Transaction",
                NotificationPriority.Low,
                null
            ));
        }
    }

    public async Task CreateAllowancePaidNotificationAsync(Child child, Transaction transaction)
    {
        await CreateNotificationAsync(new CreateNotificationDto(
            child.UserId,
            NotificationType.AllowancePaid,
            "Allowance Paid!",
            $"Your weekly allowance of {transaction.Amount:C} has been added to your account.",
            $"/dashboard",
            transaction.Id,
            "Transaction",
            NotificationPriority.High,
            null
        ));
    }

    public async Task CreateGoalReachedNotificationAsync(Child child, WishListItem goal)
    {
        await CreateNotificationAsync(new CreateNotificationDto(
            child.UserId,
            NotificationType.GoalReached,
            "Goal Reached!",
            $"Congratulations! You have enough money saved for {goal.Name}!",
            $"/wishlist/{goal.Id}",
            goal.Id,
            "WishListItem",
            NotificationPriority.High,
            null
        ));

        // Notify parents
        var parents = await _context.Users
            .Where(u => u.FamilyId == child.FamilyId && u.Role == UserRole.Parent)
            .ToListAsync();

        foreach (var parent in parents)
        {
            await CreateNotificationAsync(new CreateNotificationDto(
                parent.Id,
                NotificationType.GoalReached,
                $"{child.User.FirstName} Reached a Goal!",
                $"{child.User.FirstName} has saved enough for {goal.Name} ({goal.Price:C})!",
                $"/children/{child.Id}/wishlist",
                goal.Id,
                "WishListItem",
                NotificationPriority.Normal,
                null
            ));
        }
    }

    public async Task CreateLowBalanceNotificationAsync(Child child)
    {
        var preferences = await _preferencesService.GetPreferencesAsync(child.UserId);

        if (!preferences.LowBalanceNotifications || !preferences.LowBalanceThreshold.HasValue)
            return;

        if (child.CurrentBalance <= preferences.LowBalanceThreshold.Value)
        {
            await CreateNotificationAsync(new CreateNotificationDto(
                child.UserId,
                NotificationType.LowBalance,
                "Low Balance Alert",
                $"Your balance is {child.CurrentBalance:C}. You might want to save more before making purchases.",
                "/dashboard",
                null,
                null,
                NotificationPriority.Normal,
                DateTime.UtcNow.AddDays(7)
            ));
        }
    }

    public async Task CreateAchievementNotificationAsync(ApplicationUser user, string achievementName)
    {
        await CreateNotificationAsync(new CreateNotificationDto(
            user.Id,
            NotificationType.AchievementUnlocked,
            "Achievement Unlocked!",
            $"You've earned the '{achievementName}' achievement!",
            "/achievements",
            null,
            "Achievement",
            NotificationPriority.High,
            null
        ));
    }

    public async Task<List<Notification>> GetUnreadNotificationsAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Notification>> GetAllNotificationsAsync(Guid userId, int limit = 50)
    {
        return await _context.Notifications
            .Where(n => n.RecipientId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.RecipientId == userId && !n.IsRead);
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Notify client to update unread count
            await _hubContext.Clients.User(notification.RecipientId.ToString())
                .SendAsync("NotificationRead", notificationId);
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        if (notifications.Any())
        {
            await _context.SaveChangesAsync();

            await _hubContext.Clients.User(userId.ToString())
                .SendAsync("AllNotificationsRead");
        }
    }

    public async Task DeleteNotificationAsync(Guid notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteOldNotificationsAsync(int daysOld = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

        var oldNotifications = await _context.Notifications
            .Where(n => n.CreatedAt < cutoffDate && n.IsRead)
            .ToListAsync();

        _context.Notifications.RemoveRange(oldNotifications);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted {Count} old notifications", oldNotifications.Count);
    }

    private NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.ActionUrl,
            notification.Priority,
            notification.IsRead,
            notification.CreatedAt
        );
    }
}
```

### EmailNotificationService Implementation
```csharp
public class EmailNotificationService : IEmailNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly INotificationPreferencesService _preferencesService;
    private readonly AllowanceContext _context;
    private readonly ILogger<EmailNotificationService> _logger;

    public async Task SendTransactionEmailAsync(Transaction transaction, ApplicationUser recipient)
    {
        if (!await _preferencesService.ShouldSendNotificationAsync(
            recipient.Id,
            NotificationType.TransactionCreated,
            NotificationChannel.Email))
        {
            return;
        }

        var child = await _context.Children
            .Include(c => c.User)
            .FirstAsync(c => c.Id == transaction.ChildId);

        var subject = $"Transaction: {transaction.Amount:C} {transaction.Type}";
        var body = $@"
            <html>
            <body>
                <h2>Transaction Notification</h2>
                <p><strong>Child:</strong> {child.User.FullName}</p>
                <p><strong>Type:</strong> {transaction.Type}</p>
                <p><strong>Amount:</strong> {transaction.Amount:C}</p>
                <p><strong>Description:</strong> {transaction.Description}</p>
                <p><strong>New Balance:</strong> {transaction.BalanceAfter:C}</p>
                <p><strong>Date:</strong> {transaction.CreatedAt:MMMM d, yyyy h:mm tt}</p>
            </body>
            </html>
        ";

        await SendEmailAsync(recipient.Email!, subject, body);
    }

    public async Task SendAllowancePaidEmailAsync(Child child, Transaction transaction)
    {
        if (!await _preferencesService.ShouldSendNotificationAsync(
            child.UserId,
            NotificationType.AllowancePaid,
            NotificationChannel.Email))
        {
            return;
        }

        var subject = $"Your Allowance of {transaction.Amount:C} Has Been Paid!";
        var body = $@"
            <html>
            <body>
                <h2>Allowance Paid!</h2>
                <p>Hi {child.User.FirstName},</p>
                <p>Great news! Your weekly allowance of <strong>{transaction.Amount:C}</strong> has been added to your account.</p>
                <p><strong>Your new balance:</strong> {transaction.BalanceAfter:C}</p>
                <p>Keep up the great work!</p>
            </body>
            </html>
        ";

        await SendEmailAsync(child.User.Email!, subject, body);
    }

    public async Task SendGoalReachedEmailAsync(Child child, WishListItem goal)
    {
        if (!await _preferencesService.ShouldSendNotificationAsync(
            child.UserId,
            NotificationType.GoalReached,
            NotificationChannel.Email))
        {
            return;
        }

        var subject = $"You Reached Your Goal: {goal.Name}!";
        var body = $@"
            <html>
            <body>
                <h2>Congratulations, {child.User.FirstName}!</h2>
                <p>You've saved enough money for <strong>{goal.Name}</strong>!</p>
                <p><strong>Goal Price:</strong> {goal.Price:C}</p>
                <p><strong>Your Balance:</strong> {child.CurrentBalance:C}</p>
                <p>Great job on reaching your savings goal! Talk to your parents about purchasing your item.</p>
            </body>
            </html>
        ";

        await SendEmailAsync(child.User.Email!, subject, body);
    }

    public async Task SendWeeklyDigestEmailAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.ChildProfile)
            .ThenInclude(c => c!.Transactions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return;

        var preferences = await _preferencesService.GetPreferencesAsync(userId);
        if (!preferences.EmailWeeklyDigest) return;

        var startDate = DateTime.UtcNow.AddDays(-7);
        var transactions = user.ChildProfile?.Transactions
            .Where(t => t.CreatedAt >= startDate)
            .OrderByDescending(t => t.CreatedAt)
            .ToList() ?? new List<Transaction>();

        var subject = $"Your Weekly Allowance Summary - {startDate:MMM d} to {DateTime.UtcNow:MMM d}";
        var body = $@"
            <html>
            <body>
                <h2>Weekly Summary for {user.FirstName}</h2>
                <p><strong>Current Balance:</strong> {user.ChildProfile?.CurrentBalance:C}</p>
                <p><strong>Transactions this week:</strong> {transactions.Count}</p>
                <h3>Recent Activity</h3>
                <ul>
                    {string.Join("", transactions.Take(10).Select(t =>
                        $"<li>{t.CreatedAt:MMM dd}: {t.Description} - {t.Amount:C} ({t.Type})</li>"))}
                </ul>
                {(transactions.Count > 10 ? "<p><em>...and more. Log in to see all transactions.</em></p>" : "")}
            </body>
            </html>
        ";

        await SendEmailAsync(user.Email!, subject, body);
    }

    public async Task SendLowBalanceEmailAsync(Child child)
    {
        var preferences = await _preferencesService.GetPreferencesAsync(child.UserId);
        if (!preferences.LowBalanceNotifications) return;

        var subject = "Low Balance Alert";
        var body = $@"
            <html>
            <body>
                <h2>Low Balance Alert</h2>
                <p>Hi {child.User.FirstName},</p>
                <p>Your current balance is <strong>{child.CurrentBalance:C}</strong>.</p>
                <p>You might want to save more before making your next purchase!</p>
            </body>
            </html>
        ";

        await SendEmailAsync(child.User.Email!, subject, body);
    }

    public Task<bool> IsEmailConfiguredAsync()
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        return Task.FromResult(!string.IsNullOrEmpty(apiKey));
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        var client = new SendGridClient(apiKey);

        var msg = new SendGridMessage();
        msg.SetFrom(new EmailAddress(
            _configuration["SendGrid:FromEmail"],
            _configuration["SendGrid:FromName"]));
        msg.AddTo(new EmailAddress(toEmail));
        msg.SetSubject(subject);
        msg.AddContent(MimeType.Html, htmlBody);

        var response = await client.SendEmailAsync(msg);

        if (response.StatusCode != System.Net.HttpStatusCode.OK &&
            response.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            _logger.LogError("Failed to send email to {Email}: {StatusCode}", toEmail, response.StatusCode);
            throw new InvalidOperationException($"Failed to send email: {response.StatusCode}");
        }

        _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
    }
}
```

### NotificationPreferencesService Implementation
```csharp
public class NotificationPreferencesService : INotificationPreferencesService
{
    private readonly AllowanceContext _context;

    public async Task<NotificationPreferences> GetPreferencesAsync(Guid userId)
    {
        var preferences = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            preferences = await CreateDefaultPreferencesAsync(userId);
        }

        return preferences;
    }

    public async Task<NotificationPreferences> UpdatePreferencesAsync(Guid userId, UpdatePreferencesDto dto)
    {
        var preferences = await GetPreferencesAsync(userId);

        preferences.InAppEnabled = dto.InAppEnabled;
        preferences.InAppTransactions = dto.InAppTransactions;
        preferences.InAppAllowance = dto.InAppAllowance;
        preferences.InAppGoals = dto.InAppGoals;
        preferences.InAppAchievements = dto.InAppAchievements;

        preferences.EmailEnabled = dto.EmailEnabled;
        preferences.EmailTransactions = dto.EmailTransactions;
        preferences.EmailAllowance = dto.EmailAllowance;
        preferences.EmailGoals = dto.EmailGoals;
        preferences.EmailAchievements = dto.EmailAchievements;
        preferences.EmailWeeklyDigest = dto.EmailWeeklyDigest;

        preferences.LowBalanceThreshold = dto.LowBalanceThreshold;
        preferences.LowBalanceNotifications = dto.LowBalanceNotifications;

        preferences.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return preferences;
    }

    public async Task<NotificationPreferences> CreateDefaultPreferencesAsync(Guid userId)
    {
        var preferences = new NotificationPreferences
        {
            UserId = userId,
            InAppEnabled = true,
            InAppTransactions = true,
            InAppAllowance = true,
            InAppGoals = true,
            InAppAchievements = true,
            EmailEnabled = true,
            EmailTransactions = true,
            EmailAllowance = true,
            EmailGoals = true,
            EmailAchievements = false,
            EmailWeeklyDigest = true,
            WeeklyDigestDay = DayOfWeek.Sunday,
            WeeklyDigestHour = 9
        };

        _context.NotificationPreferences.Add(preferences);
        await _context.SaveChangesAsync();

        return preferences;
    }

    public async Task<bool> ShouldSendNotificationAsync(Guid userId, NotificationType type, NotificationChannel channel)
    {
        var preferences = await GetPreferencesAsync(userId);

        if (channel == NotificationChannel.InApp && !preferences.InAppEnabled)
            return false;

        if (channel == NotificationChannel.Email && !preferences.EmailEnabled)
            return false;

        return type switch
        {
            NotificationType.TransactionCreated => channel == NotificationChannel.InApp
                ? preferences.InAppTransactions
                : preferences.EmailTransactions,

            NotificationType.AllowancePaid => channel == NotificationChannel.InApp
                ? preferences.InAppAllowance
                : preferences.EmailAllowance,

            NotificationType.GoalReached or NotificationType.GoalProgress => channel == NotificationChannel.InApp
                ? preferences.InAppGoals
                : preferences.EmailGoals,

            NotificationType.AchievementUnlocked => channel == NotificationChannel.InApp
                ? preferences.InAppAchievements
                : preferences.EmailAchievements,

            NotificationType.WeeklyDigest => channel == NotificationChannel.Email && preferences.EmailWeeklyDigest,

            _ => true
        };
    }
}
```

## DTOs

```csharp
public record CreateNotificationDto(
    Guid RecipientId,
    NotificationType Type,
    string Title,
    string Message,
    string? ActionUrl,
    Guid? RelatedEntityId,
    string? RelatedEntityType,
    NotificationPriority Priority,
    DateTime? ExpiresAt
);

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    string? ActionUrl,
    NotificationPriority Priority,
    bool IsRead,
    DateTime CreatedAt
);

public record UpdatePreferencesDto(
    bool InAppEnabled,
    bool InAppTransactions,
    bool InAppAllowance,
    bool InAppGoals,
    bool InAppAchievements,
    bool EmailEnabled,
    bool EmailTransactions,
    bool EmailAllowance,
    bool EmailGoals,
    bool EmailAchievements,
    bool EmailWeeklyDigest,
    decimal? LowBalanceThreshold,
    bool LowBalanceNotifications
);

public record NotificationPreferencesDto(
    Guid UserId,
    bool InAppEnabled,
    bool InAppTransactions,
    bool InAppAllowance,
    bool InAppGoals,
    bool InAppAchievements,
    bool EmailEnabled,
    bool EmailTransactions,
    bool EmailAllowance,
    bool EmailGoals,
    bool EmailAchievements,
    bool EmailWeeklyDigest,
    DayOfWeek WeeklyDigestDay,
    int WeeklyDigestHour,
    decimal? LowBalanceThreshold,
    bool LowBalanceNotifications
);
```

## API Controllers

### NotificationsController
```csharp
[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;

    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetNotifications([FromQuery] bool unreadOnly = false)
    {
        var notifications = unreadOnly
            ? await _notificationService.GetUnreadNotificationsAsync(_currentUser.UserId)
            : await _notificationService.GetAllNotificationsAsync(_currentUser.UserId);

        var dtos = notifications.Select(n => new NotificationDto(
            n.Id,
            n.Type,
            n.Title,
            n.Message,
            n.ActionUrl,
            n.Priority,
            n.IsRead,
            n.CreatedAt
        )).ToList();

        return Ok(dtos);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var count = await _notificationService.GetUnreadCountAsync(_currentUser.UserId);
        return Ok(count);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await _notificationService.MarkAsReadAsync(id);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notificationService.MarkAllAsReadAsync(_currentUser.UserId);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        await _notificationService.DeleteNotificationAsync(id);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/notification-preferences")]
[Authorize]
public class NotificationPreferencesController : ControllerBase
{
    private readonly INotificationPreferencesService _preferencesService;
    private readonly ICurrentUserService _currentUser;

    [HttpGet]
    public async Task<ActionResult<NotificationPreferencesDto>> GetPreferences()
    {
        var prefs = await _preferencesService.GetPreferencesAsync(_currentUser.UserId);

        var dto = new NotificationPreferencesDto(
            prefs.UserId,
            prefs.InAppEnabled,
            prefs.InAppTransactions,
            prefs.InAppAllowance,
            prefs.InAppGoals,
            prefs.InAppAchievements,
            prefs.EmailEnabled,
            prefs.EmailTransactions,
            prefs.EmailAllowance,
            prefs.EmailGoals,
            prefs.EmailAchievements,
            prefs.EmailWeeklyDigest,
            prefs.WeeklyDigestDay,
            prefs.WeeklyDigestHour,
            prefs.LowBalanceThreshold,
            prefs.LowBalanceNotifications
        );

        return Ok(dto);
    }

    [HttpPut]
    public async Task<ActionResult<NotificationPreferencesDto>> UpdatePreferences([FromBody] UpdatePreferencesDto dto)
    {
        var prefs = await _preferencesService.UpdatePreferencesAsync(_currentUser.UserId, dto);

        var responseDto = new NotificationPreferencesDto(
            prefs.UserId,
            prefs.InAppEnabled,
            prefs.InAppTransactions,
            prefs.InAppAllowance,
            prefs.InAppGoals,
            prefs.InAppAchievements,
            prefs.EmailEnabled,
            prefs.EmailTransactions,
            prefs.EmailAllowance,
            prefs.EmailGoals,
            prefs.EmailAchievements,
            prefs.EmailWeeklyDigest,
            prefs.WeeklyDigestDay,
            prefs.WeeklyDigestHour,
            prefs.LowBalanceThreshold,
            prefs.LowBalanceNotifications
        );

        return Ok(responseDto);
    }
}
```

## SignalR Hub

### NotificationHub
```csharp
[Authorize]
public class NotificationHub : Hub
{
    private readonly ICurrentUserService _currentUser;

    public NotificationHub(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public override async Task OnConnectedAsync()
    {
        // Add user to their personal notification group
        await Groups.AddToGroupAsync(Context.ConnectionId, Context.UserIdentifier!);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, Context.UserIdentifier!);
        await base.OnDisconnectedAsync(exception);
    }
}
```

## Blazor Components

### NotificationBell.razor
```razor
@inject INotificationService NotificationService
@inject NavigationManager Navigation
@implements IAsyncDisposable

<div class="notification-bell">
    <button class="btn btn-link position-relative" @onclick="ToggleNotifications">
        <i class="bi bi-bell"></i>
        @if (unreadCount > 0)
        {
            <span class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger">
                @(unreadCount > 9 ? "9+" : unreadCount.ToString())
            </span>
        }
    </button>

    @if (showNotifications)
    {
        <div class="notification-dropdown">
            <div class="notification-header">
                <h5>Notifications</h5>
                @if (unreadCount > 0)
                {
                    <button class="btn btn-sm btn-link" @onclick="MarkAllAsRead">Mark all read</button>
                }
            </div>
            <div class="notification-list">
                @if (notifications == null)
                {
                    <div class="text-center p-3">
                        <div class="spinner-border spinner-border-sm"></div>
                    </div>
                }
                else if (!notifications.Any())
                {
                    <div class="text-center text-muted p-3">
                        No notifications
                    </div>
                }
                else
                {
                    @foreach (var notification in notifications)
                    {
                        <div class="notification-item @(!notification.IsRead ? "unread" : "")"
                             @onclick="() => HandleNotificationClick(notification)">
                            <div class="notification-icon">
                                <i class="@GetNotificationIcon(notification.Type)"></i>
                            </div>
                            <div class="notification-content">
                                <h6>@notification.Title</h6>
                                <p>@notification.Message</p>
                                <small>@FormatTime(notification.CreatedAt)</small>
                            </div>
                        </div>
                    }
                }
            </div>
            <div class="notification-footer">
                <a href="/notifications">View all notifications</a>
            </div>
        </div>
    }
</div>

@code {
    private HubConnection? hubConnection;
    private List<NotificationDto>? notifications;
    private int unreadCount = 0;
    private bool showNotifications = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadNotifications();
        await ConnectToHub();
    }

    private async Task ConnectToHub()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/notificationHub"))
            .WithAutomaticReconnect()
            .Build();

        hubConnection.On<NotificationDto>("ReceiveNotification", async (notification) =>
        {
            notifications?.Insert(0, notification);
            unreadCount++;
            await InvokeAsync(StateHasChanged);
        });

        hubConnection.On<Guid>("NotificationRead", async (notificationId) =>
        {
            var notification = notifications?.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                unreadCount--;
                await InvokeAsync(StateHasChanged);
            }
        });

        hubConnection.On("AllNotificationsRead", async () =>
        {
            unreadCount = 0;
            await InvokeAsync(StateHasChanged);
        });

        await hubConnection.StartAsync();
    }

    private async Task LoadNotifications()
    {
        notifications = await NotificationService.GetAllNotificationsAsync(Guid.Empty, 10);
        unreadCount = await NotificationService.GetUnreadCountAsync(Guid.Empty);
    }

    private void ToggleNotifications()
    {
        showNotifications = !showNotifications;
    }

    private async Task MarkAllAsRead()
    {
        await NotificationService.MarkAllAsReadAsync(Guid.Empty);
        unreadCount = 0;
        if (notifications != null)
        {
            foreach (var n in notifications)
            {
                // Update local state
            }
        }
    }

    private async Task HandleNotificationClick(NotificationDto notification)
    {
        if (!notification.IsRead)
        {
            await NotificationService.MarkAsReadAsync(notification.Id);
            unreadCount--;
        }

        if (!string.IsNullOrEmpty(notification.ActionUrl))
        {
            Navigation.NavigateTo(notification.ActionUrl);
        }

        showNotifications = false;
    }

    private string GetNotificationIcon(NotificationType type)
    {
        return type switch
        {
            NotificationType.TransactionCreated => "bi bi-cash-coin",
            NotificationType.AllowancePaid => "bi bi-wallet2",
            NotificationType.GoalReached => "bi bi-trophy",
            NotificationType.LowBalance => "bi bi-exclamation-triangle",
            NotificationType.AchievementUnlocked => "bi bi-award",
            _ => "bi bi-info-circle"
        };
    }

    private string FormatTime(DateTime date)
    {
        var timespan = DateTime.UtcNow - date;
        if (timespan.TotalMinutes < 1) return "Just now";
        if (timespan.TotalHours < 1) return $"{(int)timespan.TotalMinutes}m ago";
        if (timespan.TotalDays < 1) return $"{(int)timespan.TotalHours}h ago";
        if (timespan.TotalDays < 7) return $"{(int)timespan.TotalDays}d ago";
        return date.ToString("MMM dd");
    }

    public async ValueTask DisposeAsync()
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }
    }
}
```

### NotificationPreferences.razor
```razor
@page "/settings/notifications"
@attribute [Authorize]
@inject INotificationPreferencesService PreferencesService

<h2>Notification Preferences</h2>

<EditForm Model="@preferences" OnValidSubmit="@SavePreferences">
    <div class="card mb-3">
        <div class="card-header">
            <h4>In-App Notifications</h4>
        </div>
        <div class="card-body">
            <div class="form-check form-switch mb-3">
                <InputCheckbox class="form-check-input" @bind-Value="preferences.InAppEnabled" />
                <label class="form-check-label">Enable in-app notifications</label>
            </div>

            @if (preferences.InAppEnabled)
            {
                <div class="ms-4">
                    <div class="form-check mb-2">
                        <InputCheckbox class="form-check-input" @bind-Value="preferences.InAppTransactions" />
                        <label class="form-check-label">Transaction notifications</label>
                    </div>
                    <div class="form-check mb-2">
                        <InputCheckbox class="form-check-input" @bind-Value="preferences.InAppAllowance" />
                        <label class="form-check-label">Allowance payment notifications</label>
                    </div>
                    <div class="form-check mb-2">
                        <InputCheckbox class="form-check-input" @bind-Value="preferences.InAppGoals" />
                        <label class="form-check-label">Goal achievement notifications</label>
                    </div>
                    <div class="form-check mb-2">
                        <InputCheckbox class="form-check-input" @bind-Value="preferences.InAppAchievements" />
                        <label class="form-check-label">Achievement notifications</label>
                    </div>
                </div>
            }
        </div>
    </div>

    <div class="card mb-3">
        <div class="card-header">
            <h4>Email Notifications</h4>
        </div>
        <div class="card-body">
            <div class="form-check form-switch mb-3">
                <InputCheckbox class="form-check-input" @bind-Value="preferences.EmailEnabled" />
                <label class="form-check-label">Enable email notifications</label>
            </div>

            @if (preferences.EmailEnabled)
            {
                <div class="ms-4">
                    <div class="form-check mb-2">
                        <InputCheckbox class="form-check-input" @bind-Value="preferences.EmailTransactions" />
                        <label class="form-check-label">Transaction emails</label>
                    </div>
                    <div class="form-check mb-2">
                        <InputCheckbox class="form-check-input" @bind-Value="preferences.EmailAllowance" />
                        <label class="form-check-label">Allowance payment emails</label>
                    </div>
                    <div class="form-check mb-2">
                        <InputCheckbox class="form-check-input" @bind-Value="preferences.EmailGoals" />
                        <label class="form-check-label">Goal achievement emails</label>
                    </div>
                    <div class="form-check mb-2">
                        <InputCheckbox class="form-check-input" @bind-Value="preferences.EmailWeeklyDigest" />
                        <label class="form-check-label">Weekly summary email</label>
                    </div>
                </div>
            }
        </div>
    </div>

    <div class="card mb-3">
        <div class="card-header">
            <h4>Low Balance Alert</h4>
        </div>
        <div class="card-body">
            <div class="form-check form-switch mb-3">
                <InputCheckbox class="form-check-input" @bind-Value="preferences.LowBalanceNotifications" />
                <label class="form-check-label">Enable low balance alerts</label>
            </div>

            @if (preferences.LowBalanceNotifications)
            {
                <div class="mb-3">
                    <label class="form-label">Alert when balance falls below:</label>
                    <InputNumber class="form-control" @bind-Value="preferences.LowBalanceThreshold" />
                </div>
            }
        </div>
    </div>

    <button type="submit" class="btn btn-primary" disabled="@isSaving">
        @(isSaving ? "Saving..." : "Save Preferences")
    </button>
</EditForm>

@code {
    private UpdatePreferencesDto preferences = new(true, true, true, true, true, true, true, true, true, false, true, null, false);
    private bool isSaving = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadPreferences();
    }

    private async Task LoadPreferences()
    {
        // Load from service
    }

    private async Task SavePreferences()
    {
        isSaving = true;
        try
        {
            await PreferencesService.UpdatePreferencesAsync(Guid.Empty, preferences);
        }
        finally
        {
            isSaving = false;
        }
    }
}
```

## Background Job for Weekly Digests

### WeeklyDigestJob
```csharp
public class WeeklyDigestJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeeklyDigestJob> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AllowanceContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();

                // Find users who should receive digest today
                var preferences = await context.NotificationPreferences
                    .Include(p => p.User)
                    .Where(p => p.EmailWeeklyDigest &&
                               p.EmailEnabled &&
                               p.WeeklyDigestDay == now.DayOfWeek &&
                               p.WeeklyDigestHour == now.Hour)
                    .ToListAsync(stoppingToken);

                foreach (var pref in preferences)
                {
                    try
                    {
                        await emailService.SendWeeklyDigestEmailAsync(pref.UserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send weekly digest to user {UserId}", pref.UserId);
                    }
                }

                _logger.LogInformation("Processed {Count} weekly digest emails", preferences.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in weekly digest job");
            }

            // Check every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

## Test Cases (30 Tests Total)

### NotificationService Tests
```csharp
public class NotificationServiceTests
{
    [Fact]
    public async Task CreateNotification_CreatesAndBroadcasts()
    {
        // Arrange
        var service = CreateService();
        var dto = new CreateNotificationDto(
            Guid.NewGuid(),
            NotificationType.TransactionCreated,
            "Test",
            "Test message",
            null,
            null,
            null,
            NotificationPriority.Normal,
            null
        );

        // Act
        var notification = await service.CreateNotificationAsync(dto);

        // Assert
        notification.Should().NotBeNull();
        notification.Title.Should().Be("Test");
    }

    [Fact]
    public async Task CreateTransactionNotification_NotifiesChildAndParents()
    {
        // Test implementation
    }

    [Fact]
    public async Task CreateAllowancePaidNotification_SetsHighPriority()
    {
        // Test implementation
    }

    [Fact]
    public async Task CreateGoalReachedNotification_NotifiesAllPartyMembers()
    {
        // Test implementation
    }

    [Fact]
    public async Task CreateLowBalanceNotification_RespectsThreshold()
    {
        // Test implementation
    }

    [Fact]
    public async Task GetUnreadNotifications_ReturnsOnlyUnread()
    {
        // Test implementation
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsCorrectCount()
    {
        // Test implementation
    }

    [Fact]
    public async Task MarkAsRead_UpdatesNotification()
    {
        // Test implementation
    }

    [Fact]
    public async Task MarkAllAsRead_UpdatesMultipleNotifications()
    {
        // Test implementation
    }

    [Fact]
    public async Task DeleteNotification_RemovesFromDatabase()
    {
        // Test implementation
    }

    [Fact]
    public async Task DeleteOldNotifications_RemovesExpiredOnly()
    {
        // Test implementation
    }

    [Fact]
    public async Task CreateNotification_RespectsUserPreferences()
    {
        // Test implementation
    }
}

public class EmailNotificationServiceTests
{
    [Fact]
    public async Task SendTransactionEmail_SendsSuccessfully()
    {
        // Test implementation
    }

    [Fact]
    public async Task SendAllowancePaidEmail_UsesTemplate()
    {
        // Test implementation
    }

    [Fact]
    public async Task SendGoalReachedEmail_IncludesGoalDetails()
    {
        // Test implementation
    }

    [Fact]
    public async Task SendWeeklyDigestEmail_IncludesTransactions()
    {
        // Test implementation
    }

    [Fact]
    public async Task SendLowBalanceEmail_OnlyWhenEnabled()
    {
        // Test implementation
    }

    [Fact]
    public async Task SendEmail_RespectsUserPreferences()
    {
        // Test implementation
    }
}

public class NotificationPreferencesServiceTests
{
    [Fact]
    public async Task GetPreferences_ReturnsExistingOrCreatesDefault()
    {
        // Test implementation
    }

    [Fact]
    public async Task UpdatePreferences_SavesChanges()
    {
        // Test implementation
    }

    [Fact]
    public async Task CreateDefaultPreferences_SetsCorrectDefaults()
    {
        // Test implementation
    }

    [Fact]
    public async Task ShouldSendNotification_RespectsInAppSettings()
    {
        // Test implementation
    }

    [Fact]
    public async Task ShouldSendNotification_RespectsEmailSettings()
    {
        // Test implementation
    }

    [Fact]
    public async Task ShouldSendNotification_ChecksGlobalEnable()
    {
        // Test implementation
    }
}

public class NotificationsControllerTests
{
    [Fact]
    public async Task GetNotifications_ReturnsUserNotifications()
    {
        // Test implementation
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsCorrectCount()
    {
        // Test implementation
    }

    [Fact]
    public async Task MarkAsRead_UpdatesNotification()
    {
        // Test implementation
    }

    [Fact]
    public async Task MarkAllAsRead_UpdatesAll()
    {
        // Test implementation
    }

    [Fact]
    public async Task DeleteNotification_RemovesNotification()
    {
        // Test implementation
    }
}
```

## Success Metrics

### Performance Targets
- Notification creation: < 100ms
- SignalR broadcast: < 50ms
- Email delivery: < 3 seconds
- Unread count query: < 50ms

### Quality Metrics
- 30 tests passing (100% critical path coverage)
- Real-time notifications delivered within 1 second
- Email delivery rate > 95%
- Zero notification data loss

## Configuration

### appsettings.json
```json
{
  "SendGrid": {
    "ApiKey": "your-sendgrid-api-key",
    "FromEmail": "noreply@allowancetracker.com",
    "FromName": "Allowance Tracker"
  },
  "Notifications": {
    "RetentionDays": 30,
    "MaxNotificationsPerUser": 100
  }
}
```

### Program.cs Registration
```csharp
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddScoped<INotificationPreferencesService, NotificationPreferencesService>();
builder.Services.AddHostedService<WeeklyDigestJob>();
```
