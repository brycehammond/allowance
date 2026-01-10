using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AllowanceTracker.Tests.Services;

public class DeviceTokenServiceTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly DeviceTokenService _service;
    private readonly ApplicationUser _testUser;

    public DeviceTokenServiceTests()
    {
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AllowanceContext(options);
        _service = new DeviceTokenService(_context);

        // Create test user
        _testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Parent
        };

        _context.Users.Add(_testUser);
        _context.SaveChanges();
    }

    [Fact]
    public async Task RegisterDeviceAsync_CreatesNewDeviceToken()
    {
        // Arrange
        var dto = new RegisterDeviceDto(
            Token: "fcm-token-12345",
            Platform: DevicePlatform.iOS,
            DeviceName: "iPhone 15 Pro",
            AppVersion: "1.0.0"
        );

        // Act
        var result = await _service.RegisterDeviceAsync(_testUser.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be(DevicePlatform.iOS);
        result.DeviceName.Should().Be("iPhone 15 Pro");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterDeviceAsync_UpdatesExistingToken()
    {
        // Arrange
        var existingDevice = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Token = "old-fcm-token",
            Platform = DevicePlatform.iOS,
            DeviceName = "Old iPhone",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        _context.DeviceTokens.Add(existingDevice);
        await _context.SaveChangesAsync();

        var dto = new RegisterDeviceDto(
            Token: "old-fcm-token",  // Same token
            Platform: DevicePlatform.iOS,
            DeviceName: "New iPhone",  // Updated device name
            AppVersion: "2.0.0"
        );

        // Act
        var result = await _service.RegisterDeviceAsync(_testUser.Id, dto);

        // Assert
        result.Id.Should().Be(existingDevice.Id);  // Same device record
        result.DeviceName.Should().Be("New iPhone");

        var totalDevices = await _context.DeviceTokens.CountAsync();
        totalDevices.Should().Be(1);  // Should not create duplicate
    }

    [Fact]
    public async Task RegisterDeviceAsync_DeactivatesOldTokensForSamePlatform()
    {
        // Arrange
        var oldDevice = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Token = "old-token",
            Platform = DevicePlatform.iOS,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        _context.DeviceTokens.Add(oldDevice);
        await _context.SaveChangesAsync();

        var dto = new RegisterDeviceDto(
            Token: "new-token",
            Platform: DevicePlatform.iOS,
            DeviceName: "New iPhone",
            AppVersion: "1.0.0"
        );

        // Act
        var result = await _service.RegisterDeviceAsync(_testUser.Id, dto);

        // Assert
        var oldDeviceUpdated = await _context.DeviceTokens.FindAsync(oldDevice.Id);
        oldDeviceUpdated!.IsActive.Should().BeFalse();
        oldDeviceUpdated.DeactivatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterDeviceAsync_LimitsMaxDevicesPerUser()
    {
        // Arrange - Create 5 existing devices
        for (int i = 0; i < 5; i++)
        {
            var device = new DeviceToken
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Token = $"token-{i}",
                Platform = DevicePlatform.iOS,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i * 10)
            };
            _context.DeviceTokens.Add(device);
        }
        await _context.SaveChangesAsync();

        var dto = new RegisterDeviceDto(
            Token: "new-token",
            Platform: DevicePlatform.iOS,
            DeviceName: "Newest iPhone",
            AppVersion: "1.0.0"
        );

        // Act
        var result = await _service.RegisterDeviceAsync(_testUser.Id, dto);

        // Assert - Should have max 5 active devices
        var activeDevices = await _context.DeviceTokens
            .Where(d => d.UserId == _testUser.Id && d.IsActive)
            .CountAsync();
        activeDevices.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetUserDevicesAsync_ReturnsUserDevices()
    {
        // Arrange
        var device1 = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Token = "token-1",
            Platform = DevicePlatform.iOS,
            DeviceName = "iPhone",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var device2 = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Token = "token-2",
            Platform = DevicePlatform.Android,
            DeviceName = "Android Phone",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.DeviceTokens.AddRange(device1, device2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserDevicesAsync(_testUser.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(d => d.Platform == DevicePlatform.iOS);
        result.Should().Contain(d => d.Platform == DevicePlatform.Android);
    }

    [Fact]
    public async Task DeactivateDeviceAsync_DeactivatesDevice()
    {
        // Arrange
        var device = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Token = "token",
            Platform = DevicePlatform.iOS,
            IsActive = true
        };
        _context.DeviceTokens.Add(device);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeactivateDeviceAsync(device.Id, _testUser.Id);

        // Assert
        var updated = await _context.DeviceTokens.FindAsync(device.Id);
        updated!.IsActive.Should().BeFalse();
        updated.DeactivatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeactivateDeviceAsync_DoesNothing_WhenDeviceBelongsToOtherUser()
    {
        // Arrange
        var otherUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "other@example.com",
            UserName = "other@example.com"
        };
        _context.Users.Add(otherUser);

        var device = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = otherUser.Id,
            Token = "token",
            Platform = DevicePlatform.iOS,
            IsActive = true
        };
        _context.DeviceTokens.Add(device);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeactivateDeviceAsync(device.Id, _testUser.Id);

        // Assert - Device should still be active
        var unchanged = await _context.DeviceTokens.FindAsync(device.Id);
        unchanged!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateByTokenAsync_DeactivatesDeviceByToken()
    {
        // Arrange
        var device = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Token = "fcm-token-to-deactivate",
            Platform = DevicePlatform.iOS,
            IsActive = true
        };
        _context.DeviceTokens.Add(device);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeactivateByTokenAsync("fcm-token-to-deactivate", _testUser.Id);

        // Assert
        var updated = await _context.DeviceTokens.FindAsync(device.Id);
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveDevicesAsync_ReturnsOnlyActiveDevices()
    {
        // Arrange
        var activeDevice = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Token = "active-token",
            Platform = DevicePlatform.iOS,
            IsActive = true
        };
        var inactiveDevice = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Token = "inactive-token",
            Platform = DevicePlatform.Android,
            IsActive = false
        };
        _context.DeviceTokens.AddRange(activeDevice, inactiveDevice);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActiveDevicesAsync(_testUser.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].Token.Should().Be("active-token");
    }

    [Fact]
    public async Task UpdateLastUsedAsync_UpdatesTimestamp()
    {
        // Arrange
        var device = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Token = "token-to-update",
            Platform = DevicePlatform.iOS,
            IsActive = true,
            LastUsedAt = DateTime.UtcNow.AddDays(-7)
        };
        _context.DeviceTokens.Add(device);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateLastUsedAsync("token-to-update");

        // Assert
        var updated = await _context.DeviceTokens.FindAsync(device.Id);
        updated!.LastUsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CleanupStaleTokensAsync_RemovesInactiveOldTokens()
    {
        // Arrange
        var recentActive = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Token = "recent-active",
            Platform = DevicePlatform.iOS,
            IsActive = true,
            LastUsedAt = DateTime.UtcNow.AddDays(-10)
        };
        var oldActive = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Token = "old-active",
            Platform = DevicePlatform.iOS,
            IsActive = true,
            LastUsedAt = DateTime.UtcNow.AddDays(-100),
            CreatedAt = DateTime.UtcNow.AddDays(-100)
        };
        var oldInactive = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Token = "old-inactive",
            Platform = DevicePlatform.Android,
            IsActive = false,
            DeactivatedAt = DateTime.UtcNow.AddDays(-100),
            CreatedAt = DateTime.UtcNow.AddDays(-100)
        };
        _context.DeviceTokens.AddRange(recentActive, oldActive, oldInactive);
        await _context.SaveChangesAsync();

        // Act
        await _service.CleanupStaleTokensAsync(90);

        // Assert
        var remaining = await _context.DeviceTokens.ToListAsync();
        remaining.Should().HaveCount(1);  // Only recent active should remain
        remaining[0].Token.Should().Be("recent-active");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
