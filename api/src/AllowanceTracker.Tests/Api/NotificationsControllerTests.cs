using AllowanceTracker.Api.V1;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Api;

public class NotificationsControllerTests
{
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly NotificationsController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public NotificationsControllerTests()
    {
        _mockNotificationService = new Mock<INotificationService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_testUserId);

        _controller = new NotificationsController(
            _mockNotificationService.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task GetNotifications_ReturnsOkWithNotificationList()
    {
        // Arrange
        var notificationList = new NotificationListDto(
            Notifications: new List<NotificationDto>
            {
                new(Guid.NewGuid(), NotificationType.AllowanceDeposit, "Allowance Deposit", "Title", "Body", null, false, null, DateTime.UtcNow, null, null, "1m ago")
            },
            UnreadCount: 1,
            TotalCount: 1,
            HasMore: false
        );

        _mockNotificationService
            .Setup(x => x.GetNotificationsAsync(_testUserId, 1, 20, false, null))
            .ReturnsAsync(notificationList);

        // Act
        var result = await _controller.GetNotifications(1, 20, false, null);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedList = okResult.Value.Should().BeAssignableTo<NotificationListDto>().Subject;
        returnedList.Notifications.Should().HaveCount(1);
        returnedList.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsOkWithCount()
    {
        // Arrange
        _mockNotificationService
            .Setup(x => x.GetUnreadCountAsync(_testUserId))
            .ReturnsAsync(5);

        // Act
        var result = await _controller.GetUnreadCount();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var count = okResult.Value.Should().BeAssignableTo<UnreadCountDto>().Subject;
        count.Count.Should().Be(5);
    }

    [Fact]
    public async Task GetNotification_ReturnsOkWithNotification()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var notification = new NotificationDto(
            notificationId,
            NotificationType.AllowanceDeposit,
            "Allowance Deposit",
            "Title",
            "Body",
            null,
            false,
            null,
            DateTime.UtcNow,
            null,
            null,
            "1m ago"
        );

        _mockNotificationService
            .Setup(x => x.GetNotificationByIdAsync(notificationId, _testUserId))
            .ReturnsAsync(notification);

        // Act
        var result = await _controller.GetNotification(notificationId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedNotification = okResult.Value.Should().BeAssignableTo<NotificationDto>().Subject;
        returnedNotification.Id.Should().Be(notificationId);
    }

    [Fact]
    public async Task GetNotification_ReturnsNotFound_WhenNotificationNotFound()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        _mockNotificationService
            .Setup(x => x.GetNotificationByIdAsync(notificationId, _testUserId))
            .ReturnsAsync((NotificationDto?)null);

        // Act
        var result = await _controller.GetNotification(notificationId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task MarkAsRead_ReturnsOkWithUpdatedNotification()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var notification = new NotificationDto(
            notificationId,
            NotificationType.AllowanceDeposit,
            "Allowance Deposit",
            "Title",
            "Body",
            null,
            true,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(-5),
            null,
            null,
            "5m ago"
        );

        _mockNotificationService
            .Setup(x => x.MarkAsReadAsync(notificationId, _testUserId))
            .ReturnsAsync(notification);

        // Act
        var result = await _controller.MarkAsRead(notificationId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedNotification = okResult.Value.Should().BeAssignableTo<NotificationDto>().Subject;
        returnedNotification.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsRead_ReturnsNotFound_WhenNotificationNotFound()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        _mockNotificationService
            .Setup(x => x.MarkAsReadAsync(notificationId, _testUserId))
            .ReturnsAsync((NotificationDto?)null);

        // Act
        var result = await _controller.MarkAsRead(notificationId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task MarkMultipleAsRead_ReturnsOkWithCount()
    {
        // Arrange
        var dto = new MarkNotificationsReadDto(new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });
        _mockNotificationService
            .Setup(x => x.MarkMultipleAsReadAsync(_testUserId, dto.NotificationIds))
            .ReturnsAsync(2);

        // Act
        var result = await _controller.MarkMultipleAsRead(dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var markResult = okResult.Value.Should().BeAssignableTo<MarkReadResultDto>().Subject;
        markResult.MarkedCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteNotification_ReturnsNoContent()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        _mockNotificationService
            .Setup(x => x.DeleteNotificationAsync(notificationId, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteNotification(notificationId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteNotification_ReturnsNotFound_WhenNotificationNotFound()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        _mockNotificationService
            .Setup(x => x.DeleteNotificationAsync(notificationId, _testUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteNotification(notificationId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteReadNotifications_ReturnsOkWithCount()
    {
        // Arrange
        _mockNotificationService
            .Setup(x => x.DeleteReadNotificationsAsync(_testUserId))
            .ReturnsAsync(10);

        // Act
        var result = await _controller.DeleteReadNotifications();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var deleteResult = okResult.Value.Should().BeAssignableTo<DeleteResultDto>().Subject;
        deleteResult.DeletedCount.Should().Be(10);
    }

    [Fact]
    public async Task GetPreferences_ReturnsOkWithPreferences()
    {
        // Arrange
        var preferences = new NotificationPreferencesDto(
            Preferences: new List<NotificationPreferenceResponseDto>(),
            QuietHoursEnabled: false,
            QuietHoursStart: null,
            QuietHoursEnd: null
        );

        _mockNotificationService
            .Setup(x => x.GetPreferencesAsync(_testUserId))
            .ReturnsAsync(preferences);

        // Act
        var result = await _controller.GetPreferences();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<NotificationPreferencesDto>();
    }

    [Fact]
    public async Task UpdatePreferences_ReturnsOkWithUpdatedPreferences()
    {
        // Arrange
        var updateDto = new UpdateNotificationPreferencesDto(new List<NotificationPreferenceItemDto>());
        var preferences = new NotificationPreferencesDto(
            Preferences: new List<NotificationPreferenceResponseDto>(),
            QuietHoursEnabled: false,
            QuietHoursStart: null,
            QuietHoursEnd: null
        );

        _mockNotificationService
            .Setup(x => x.UpdatePreferencesAsync(_testUserId, updateDto))
            .ReturnsAsync(preferences);

        // Act
        var result = await _controller.UpdatePreferences(updateDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<NotificationPreferencesDto>();
    }

    [Fact]
    public async Task UpdateQuietHours_ReturnsOkWithUpdatedPreferences()
    {
        // Arrange
        var updateDto = new UpdateQuietHoursDto(true, new TimeOnly(22, 0), new TimeOnly(7, 0));
        var preferences = new NotificationPreferencesDto(
            Preferences: new List<NotificationPreferenceResponseDto>(),
            QuietHoursEnabled: true,
            QuietHoursStart: new TimeOnly(22, 0),
            QuietHoursEnd: new TimeOnly(7, 0)
        );

        _mockNotificationService
            .Setup(x => x.UpdateQuietHoursAsync(_testUserId, updateDto))
            .ReturnsAsync(preferences);

        // Act
        var result = await _controller.UpdateQuietHours(updateDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPrefs = okResult.Value.Should().BeAssignableTo<NotificationPreferencesDto>().Subject;
        returnedPrefs.QuietHoursEnabled.Should().BeTrue();
    }
}
