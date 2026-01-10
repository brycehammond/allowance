using AllowanceTracker.Api.V1;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Api;

public class DevicesControllerTests
{
    private readonly Mock<IDeviceTokenService> _mockDeviceTokenService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly DevicesController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public DevicesControllerTests()
    {
        _mockDeviceTokenService = new Mock<IDeviceTokenService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_testUserId);

        _controller = new DevicesController(
            _mockDeviceTokenService.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task RegisterDevice_ReturnsOkWithDeviceToken()
    {
        // Arrange
        var dto = new RegisterDeviceDto(
            "fcm-token-12345",
            DevicePlatform.iOS,
            "iPhone 15 Pro",
            "1.0.0"
        );

        var deviceToken = new DeviceTokenDto(
            Guid.NewGuid(),
            DevicePlatform.iOS,
            "iPhone 15 Pro",
            true,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        _mockDeviceTokenService
            .Setup(x => x.RegisterDeviceAsync(_testUserId, dto))
            .ReturnsAsync(deviceToken);

        // Act
        var result = await _controller.RegisterDevice(dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDevice = okResult.Value.Should().BeAssignableTo<DeviceTokenDto>().Subject;
        returnedDevice.Platform.Should().Be(DevicePlatform.iOS);
        returnedDevice.DeviceName.Should().Be("iPhone 15 Pro");
    }

    [Fact]
    public async Task GetDevices_ReturnsOkWithDeviceList()
    {
        // Arrange
        var devices = new List<DeviceTokenDto>
        {
            new(Guid.NewGuid(), DevicePlatform.iOS, "iPhone", true, DateTime.UtcNow, DateTime.UtcNow),
            new(Guid.NewGuid(), DevicePlatform.Android, "Android Phone", true, DateTime.UtcNow, DateTime.UtcNow)
        };

        _mockDeviceTokenService
            .Setup(x => x.GetUserDevicesAsync(_testUserId))
            .ReturnsAsync(devices);

        // Act
        var result = await _controller.GetDevices();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDevices = okResult.Value.Should().BeAssignableTo<List<DeviceTokenDto>>().Subject;
        returnedDevices.Should().HaveCount(2);
    }

    [Fact]
    public async Task UnregisterDevice_ReturnsNoContent()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        _mockDeviceTokenService
            .Setup(x => x.DeactivateDeviceAsync(deviceId, _testUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UnregisterDevice(deviceId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UnregisterCurrentDevice_ReturnsNoContent()
    {
        // Arrange
        var token = "fcm-token-to-remove";
        _mockDeviceTokenService
            .Setup(x => x.DeactivateByTokenAsync(token, _testUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UnregisterCurrentDevice(token);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UnregisterCurrentDevice_ReturnsBadRequest_WhenTokenMissing()
    {
        // Act
        var result = await _controller.UnregisterCurrentDevice(null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
