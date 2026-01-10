using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class DeviceTokenService : IDeviceTokenService
{
    private readonly AllowanceContext _context;
    private const int MaxDevicesPerUser = 5;

    public DeviceTokenService(AllowanceContext context)
    {
        _context = context;
    }

    public async Task<DeviceTokenDto> RegisterDeviceAsync(Guid userId, RegisterDeviceDto dto)
    {
        // Check if this token already exists for this user
        var existingDevice = await _context.DeviceTokens
            .FirstOrDefaultAsync(d => d.Token == dto.Token && d.UserId == userId);

        if (existingDevice != null)
        {
            // Update existing device
            existingDevice.DeviceName = dto.DeviceName;
            existingDevice.AppVersion = dto.AppVersion;
            existingDevice.IsActive = true;
            existingDevice.DeactivatedAt = null;
            existingDevice.LastUsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToDto(existingDevice);
        }

        // Deactivate old tokens for the same platform (single device per platform policy)
        var oldDevices = await _context.DeviceTokens
            .Where(d => d.UserId == userId && d.Platform == dto.Platform && d.IsActive)
            .ToListAsync();

        foreach (var oldDevice in oldDevices)
        {
            oldDevice.IsActive = false;
            oldDevice.DeactivatedAt = DateTime.UtcNow;
        }

        // Enforce max devices per user
        var activeDeviceCount = await _context.DeviceTokens
            .Where(d => d.UserId == userId && d.IsActive)
            .CountAsync();

        if (activeDeviceCount >= MaxDevicesPerUser)
        {
            // Deactivate the oldest active device
            var oldestDevice = await _context.DeviceTokens
                .Where(d => d.UserId == userId && d.IsActive)
                .OrderBy(d => d.LastUsedAt ?? d.CreatedAt)
                .FirstAsync();

            oldestDevice.IsActive = false;
            oldestDevice.DeactivatedAt = DateTime.UtcNow;
        }

        // Create new device token
        var newDevice = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = dto.Token,
            Platform = dto.Platform,
            DeviceName = dto.DeviceName,
            AppVersion = dto.AppVersion,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow
        };

        _context.DeviceTokens.Add(newDevice);
        await _context.SaveChangesAsync();

        return MapToDto(newDevice);
    }

    public async Task<List<DeviceTokenDto>> GetUserDevicesAsync(Guid userId)
    {
        var devices = await _context.DeviceTokens
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.LastUsedAt ?? d.CreatedAt)
            .ToListAsync();

        return devices.Select(MapToDto).ToList();
    }

    public async Task DeactivateDeviceAsync(Guid deviceId, Guid userId)
    {
        var device = await _context.DeviceTokens
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId);

        if (device == null)
        {
            return;
        }

        device.IsActive = false;
        device.DeactivatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeactivateByTokenAsync(string token, Guid userId)
    {
        var device = await _context.DeviceTokens
            .FirstOrDefaultAsync(d => d.Token == token && (userId == Guid.Empty || d.UserId == userId));

        if (device == null)
        {
            return;
        }

        device.IsActive = false;
        device.DeactivatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<List<DeviceToken>> GetActiveDevicesAsync(Guid userId)
    {
        return await _context.DeviceTokens
            .Where(d => d.UserId == userId && d.IsActive)
            .ToListAsync();
    }

    public async Task UpdateLastUsedAsync(string token)
    {
        var device = await _context.DeviceTokens
            .FirstOrDefaultAsync(d => d.Token == token);

        if (device != null)
        {
            device.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task CleanupStaleTokensAsync(int daysInactive = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysInactive);

        // Remove inactive tokens older than cutoff
        var staleInactiveTokens = await _context.DeviceTokens
            .Where(d => !d.IsActive && d.DeactivatedAt < cutoffDate)
            .ToListAsync();

        // Remove active tokens that haven't been used since cutoff
        var staleActiveTokens = await _context.DeviceTokens
            .Where(d => d.IsActive &&
                        (d.LastUsedAt == null ? d.CreatedAt : d.LastUsedAt.Value) < cutoffDate)
            .ToListAsync();

        _context.DeviceTokens.RemoveRange(staleInactiveTokens);
        _context.DeviceTokens.RemoveRange(staleActiveTokens);

        await _context.SaveChangesAsync();
    }

    private static DeviceTokenDto MapToDto(DeviceToken device)
    {
        return new DeviceTokenDto(
            Id: device.Id,
            Platform: device.Platform,
            DeviceName: device.DeviceName,
            IsActive: device.IsActive,
            CreatedAt: device.CreatedAt,
            LastUsedAt: device.LastUsedAt
        );
    }
}
