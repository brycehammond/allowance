using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Services;

public class NotificationServiceTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly NotificationService _service;
    private readonly ApplicationUser _testUser;
    private readonly Family _testFamily;
    private readonly Mock<IFirebasePushService> _mockFirebasePushService;
    private readonly Mock<ISignalRNotificationService> _mockSignalRNotificationService;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AllowanceContext(options);
        _mockFirebasePushService = new Mock<IFirebasePushService>();
        _mockSignalRNotificationService = new Mock<ISignalRNotificationService>();
        var mockLogger = new Mock<ILogger<NotificationService>>();

        _service = new NotificationService(
            _context,
            _mockFirebasePushService.Object,
            _mockSignalRNotificationService.Object,
            mockLogger.Object);

        // Create test family
        _testFamily = new Family { Id = Guid.NewGuid(), Name = "Test Family" };

        // Create test user
        _testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Parent,
            FamilyId = _testFamily.Id
        };

        _testFamily.OwnerId = _testUser.Id;

        _context.Families.Add(_testFamily);
        _context.Users.Add(_testUser);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateNotificationAsync_CreatesNewNotification()
    {
        // Arrange
        var dto = new CreateNotificationInternalDto(
            UserId: _testUser.Id,
            Type: NotificationType.AllowanceDeposit,
            Title: "Allowance Received!",
            Body: "Your weekly allowance of $10 has been deposited."
        );

        // Act
        var result = await _service.CreateNotificationAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(NotificationType.AllowanceDeposit);
        result.Title.Should().Be("Allowance Received!");
        result.Body.Should().Be("Your weekly allowance of $10 has been deposited.");
        result.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task CreateNotificationAsync_IncludesRelatedEntityInfo()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var dto = new CreateNotificationInternalDto(
            UserId: _testUser.Id,
            Type: NotificationType.TransactionCreated,
            Title: "Transaction Created",
            Body: "New spending of $25.00",
            RelatedEntityId: transactionId,
            RelatedEntityType: "Transaction"
        );

        // Act
        var result = await _service.CreateNotificationAsync(dto);

        // Assert
        result.RelatedEntityId.Should().Be(transactionId);
        result.RelatedEntityType.Should().Be("Transaction");
    }

    [Fact]
    public async Task GetNotificationsAsync_ReturnsPaginatedList()
    {
        // Arrange
        await CreateNotifications(25);

        // Act
        var result = await _service.GetNotificationsAsync(_testUser.Id, page: 1, pageSize: 10, unreadOnly: false, type: null);

        // Assert
        result.Notifications.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task GetNotificationsAsync_ReturnsUnreadOnly_WhenFiltered()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Type = NotificationType.AllowanceDeposit,
                Title = $"Notification {i}",
                Body = "Test body",
                IsRead = i < 2,  // First 2 are read
                Channel = NotificationChannel.InApp,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            _context.Notifications.Add(notification);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetNotificationsAsync(_testUser.Id, page: 1, pageSize: 10, unreadOnly: true, type: null);

        // Assert
        result.Notifications.Should().HaveCount(3);  // 3 unread
        result.Notifications.Should().AllSatisfy(n => n.IsRead.Should().BeFalse());
    }

    [Fact]
    public async Task GetNotificationsAsync_FiltersbyType()
    {
        // Arrange
        var notification1 = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Type = NotificationType.AllowanceDeposit,
            Title = "Allowance",
            Body = "Test",
            Channel = NotificationChannel.InApp
        };
        var notification2 = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Type = NotificationType.TaskApproved,
            Title = "Task",
            Body = "Test",
            Channel = NotificationChannel.InApp
        };
        _context.Notifications.AddRange(notification1, notification2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetNotificationsAsync(_testUser.Id, page: 1, pageSize: 10, unreadOnly: false, type: NotificationType.AllowanceDeposit);

        // Assert
        result.Notifications.Should().HaveCount(1);
        result.Notifications[0].Type.Should().Be(NotificationType.AllowanceDeposit);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Type = NotificationType.AllowanceDeposit,
                Title = $"Notification {i}",
                Body = "Test body",
                IsRead = i < 2,  // First 2 are read
                Channel = NotificationChannel.InApp
            };
            _context.Notifications.Add(notification);
        }
        await _context.SaveChangesAsync();

        // Act
        var count = await _service.GetUnreadCountAsync(_testUser.Id);

        // Assert
        count.Should().Be(3);  // 3 unread
    }

    [Fact]
    public async Task MarkAsReadAsync_UpdatesNotification()
    {
        // Arrange
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Type = NotificationType.AllowanceDeposit,
            Title = "Test",
            Body = "Test body",
            IsRead = false,
            Channel = NotificationChannel.InApp
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.MarkAsReadAsync(notification.Id, _testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result!.IsRead.Should().BeTrue();
        result.ReadAt.Should().NotBeNull();
        result.ReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task MarkAsReadAsync_ReturnsNull_WhenNotificationNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.MarkAsReadAsync(nonExistentId, _testUser.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MarkAsReadAsync_ReturnsNull_WhenNotificationBelongsToOtherUser()
    {
        // Arrange
        var otherUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "other@example.com",
            UserName = "other@example.com",
            FirstName = "Other",
            LastName = "User",
            Role = UserRole.Parent
        };
        _context.Users.Add(otherUser);

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = otherUser.Id,
            Type = NotificationType.AllowanceDeposit,
            Title = "Test",
            Body = "Test body",
            IsRead = false,
            Channel = NotificationChannel.InApp
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.MarkAsReadAsync(notification.Id, _testUser.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MarkMultipleAsReadAsync_MarksSpecificNotifications()
    {
        // Arrange
        var notification1 = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Type = NotificationType.AllowanceDeposit,
            Title = "Test 1",
            Body = "Test body",
            IsRead = false,
            Channel = NotificationChannel.InApp
        };
        var notification2 = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Type = NotificationType.TaskApproved,
            Title = "Test 2",
            Body = "Test body",
            IsRead = false,
            Channel = NotificationChannel.InApp
        };
        var notification3 = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Type = NotificationType.GoalCompleted,
            Title = "Test 3",
            Body = "Test body",
            IsRead = false,
            Channel = NotificationChannel.InApp
        };
        _context.Notifications.AddRange(notification1, notification2, notification3);
        await _context.SaveChangesAsync();

        // Act
        var markedCount = await _service.MarkMultipleAsReadAsync(_testUser.Id, new List<Guid> { notification1.Id, notification2.Id });

        // Assert
        markedCount.Should().Be(2);

        var updated1 = await _context.Notifications.FindAsync(notification1.Id);
        var updated2 = await _context.Notifications.FindAsync(notification2.Id);
        var updated3 = await _context.Notifications.FindAsync(notification3.Id);

        updated1!.IsRead.Should().BeTrue();
        updated2!.IsRead.Should().BeTrue();
        updated3!.IsRead.Should().BeFalse();  // Not included in list
    }

    [Fact]
    public async Task MarkMultipleAsReadAsync_MarksAllWhenIdsIsNull()
    {
        // Arrange
        for (int i = 0; i < 3; i++)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Type = NotificationType.AllowanceDeposit,
                Title = $"Test {i}",
                Body = "Test body",
                IsRead = false,
                Channel = NotificationChannel.InApp
            };
            _context.Notifications.Add(notification);
        }
        await _context.SaveChangesAsync();

        // Act
        var markedCount = await _service.MarkMultipleAsReadAsync(_testUser.Id, null);

        // Assert
        markedCount.Should().Be(3);

        var allNotifications = await _context.Notifications
            .Where(n => n.UserId == _testUser.Id)
            .ToListAsync();
        allNotifications.Should().AllSatisfy(n => n.IsRead.Should().BeTrue());
    }

    [Fact]
    public async Task DeleteNotificationAsync_RemovesNotification()
    {
        // Arrange
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Type = NotificationType.AllowanceDeposit,
            Title = "Test",
            Body = "Test body",
            Channel = NotificationChannel.InApp
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteNotificationAsync(notification.Id, _testUser.Id);

        // Assert
        result.Should().BeTrue();
        var deleted = await _context.Notifications.FindAsync(notification.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteNotificationAsync_ReturnsFalse_WhenNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteNotificationAsync(nonExistentId, _testUser.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteReadNotificationsAsync_DeletesOnlyReadNotifications()
    {
        // Arrange
        var readNotification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Type = NotificationType.AllowanceDeposit,
            Title = "Read",
            Body = "Test body",
            IsRead = true,
            Channel = NotificationChannel.InApp
        };
        var unreadNotification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Type = NotificationType.TaskApproved,
            Title = "Unread",
            Body = "Test body",
            IsRead = false,
            Channel = NotificationChannel.InApp
        };
        _context.Notifications.AddRange(readNotification, unreadNotification);
        await _context.SaveChangesAsync();

        // Act
        var deletedCount = await _service.DeleteReadNotificationsAsync(_testUser.Id);

        // Assert
        deletedCount.Should().Be(1);

        var remaining = await _context.Notifications
            .Where(n => n.UserId == _testUser.Id)
            .ToListAsync();
        remaining.Should().HaveCount(1);
        remaining[0].Title.Should().Be("Unread");
    }

    [Fact]
    public async Task GetNotificationByIdAsync_ReturnsNotification()
    {
        // Arrange
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Type = NotificationType.AllowanceDeposit,
            Title = "Test",
            Body = "Test body",
            Channel = NotificationChannel.InApp
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetNotificationByIdAsync(notification.Id, _testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(notification.Id);
        result.Title.Should().Be("Test");
    }

    [Fact]
    public async Task GetNotificationByIdAsync_ReturnsNull_WhenBelongsToOtherUser()
    {
        // Arrange
        var otherUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "other@example.com",
            UserName = "other@example.com",
            FirstName = "Other",
            LastName = "User",
            Role = UserRole.Parent
        };
        _context.Users.Add(otherUser);

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = otherUser.Id,
            Type = NotificationType.AllowanceDeposit,
            Title = "Test",
            Body = "Test body",
            Channel = NotificationChannel.InApp
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetNotificationByIdAsync(notification.Id, _testUser.Id);

        // Assert
        result.Should().BeNull();
    }

    private async Task CreateNotifications(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Type = NotificationType.AllowanceDeposit,
                Title = $"Notification {i}",
                Body = "Test body",
                Channel = NotificationChannel.InApp,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            _context.Notifications.Add(notification);
        }
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
