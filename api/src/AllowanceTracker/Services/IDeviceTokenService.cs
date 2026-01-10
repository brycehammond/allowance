using AllowanceTracker.DTOs;
using AllowanceTracker.Models;

namespace AllowanceTracker.Services;

public interface IDeviceTokenService
{
    Task<DeviceTokenDto> RegisterDeviceAsync(Guid userId, RegisterDeviceDto dto);
    Task<List<DeviceTokenDto>> GetUserDevicesAsync(Guid userId);
    Task DeactivateDeviceAsync(Guid deviceId, Guid userId);
    Task DeactivateByTokenAsync(string token, Guid userId);
    Task<List<DeviceToken>> GetActiveDevicesAsync(Guid userId);
    Task UpdateLastUsedAsync(string token);
    Task CleanupStaleTokensAsync(int daysInactive = 90);
}
