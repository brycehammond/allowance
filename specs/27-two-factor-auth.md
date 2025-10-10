# Two-Factor Authentication (2FA) Specification

## Overview

This specification implements Time-based One-Time Password (TOTP) two-factor authentication for enhanced account security. The system uses authenticator apps (Google Authenticator, Microsoft Authenticator, Authy) and provides backup recovery codes for account recovery.

## Goals

1. **TOTP Implementation**: Industry-standard 2FA using authenticator apps
2. **Enrollment Flow**: QR code generation for easy setup
3. **Verification**: TOTP code verification on login
4. **Recovery Codes**: Generate and store backup codes securely
5. **Trusted Devices**: Option to trust devices for 30 days
6. **Enforcement**: Require 2FA for parents (optional for children)
7. **TDD Approach**: 20 comprehensive tests

## Technology Stack

- **Package**: `AspNetCore.Identity.OtpAuthenticator` (NuGet)
- **TOTP**: RFC 6238 standard with 30-second time step
- **QR Code**: `QRCoder` library for QR code generation
- **Storage**: PostgreSQL for recovery codes (hashed)
- **Cookies**: For trusted device tracking
- **Testing**: xUnit, FluentAssertions, Moq

---

## Phase 1: Database Schema

### 1.1 Add 2FA Fields to ApplicationUser

```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    // Existing properties...

    /// <summary>
    /// Is 2FA enabled for this user?
    /// </summary>
    public bool TwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// TOTP secret key (encrypted)
    /// </summary>
    public string? TwoFactorSecretKey { get; set; }

    /// <summary>
    /// When 2FA was enabled
    /// </summary>
    public DateTime? TwoFactorEnabledAt { get; set; }

    /// <summary>
    /// Count of recovery codes remaining
    /// </summary>
    public int RecoveryCodesRemaining { get; set; } = 0;
}
```

### 1.2 RecoveryCode Model

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Backup recovery codes for 2FA account recovery
/// </summary>
public class RecoveryCode
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Hashed recovery code (never store plain text)
    /// </summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>
    /// Has this code been used?
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// When this code was used
    /// </summary>
    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### 1.3 TrustedDevice Model

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Tracks devices trusted for 30 days to skip 2FA
/// </summary>
public class TrustedDevice
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Unique device identifier (cookie-based)
    /// </summary>
    public string DeviceIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Device name/description
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// IP address when device was trusted
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// When trust expires (30 days from creation)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Last time this device was used
    /// </summary>
    public DateTime LastUsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

---

## Phase 2: Service Layer

### 2.1 ITwoFactorService Interface

```csharp
namespace AllowanceTracker.Services;

public interface ITwoFactorService
{
    /// <summary>
    /// Generate TOTP secret and QR code for enrollment
    /// </summary>
    Task<TwoFactorSetupDto> GenerateSetupAsync(Guid userId);

    /// <summary>
    /// Enable 2FA after verifying initial code
    /// </summary>
    Task<EnableTwoFactorResult> EnableTwoFactorAsync(
        Guid userId,
        string secretKey,
        string verificationCode);

    /// <summary>
    /// Disable 2FA (requires password confirmation)
    /// </summary>
    Task DisableTwoFactorAsync(Guid userId, string password);

    /// <summary>
    /// Verify TOTP code during login
    /// </summary>
    Task<bool> VerifyTotpCodeAsync(Guid userId, string code);

    /// <summary>
    /// Generate recovery codes
    /// </summary>
    Task<List<string>> GenerateRecoveryCodesAsync(Guid userId);

    /// <summary>
    /// Verify and use a recovery code
    /// </summary>
    Task<bool> VerifyRecoveryCodeAsync(Guid userId, string code);

    /// <summary>
    /// Get remaining recovery codes count
    /// </summary>
    Task<int> GetRemainingRecoveryCodesCountAsync(Guid userId);

    /// <summary>
    /// Trust current device for 30 days
    /// </summary>
    Task<string> TrustDeviceAsync(
        Guid userId,
        string deviceName,
        string? ipAddress,
        string? userAgent);

    /// <summary>
    /// Check if device is trusted
    /// </summary>
    Task<bool> IsDeviceTrustedAsync(Guid userId, string deviceIdentifier);

    /// <summary>
    /// Get all trusted devices for a user
    /// </summary>
    Task<List<TrustedDeviceDto>> GetTrustedDevicesAsync(Guid userId);

    /// <summary>
    /// Revoke trust for a device
    /// </summary>
    Task RevokeTrustedDeviceAsync(Guid userId, Guid deviceId);

    /// <summary>
    /// Revoke all trusted devices
    /// </summary>
    Task RevokeAllTrustedDevicesAsync(Guid userId);
}
```

### 2.2 DTOs

```csharp
public record TwoFactorSetupDto(
    string SecretKey,
    string QrCodeBase64,
    string ManualEntryKey);

public record EnableTwoFactorResult(
    bool Success,
    List<string> RecoveryCodes,
    string? ErrorMessage = null);

public record TrustedDeviceDto(
    Guid Id,
    string DeviceName,
    string? IpAddress,
    DateTime LastUsedAt,
    DateTime ExpiresAt,
    DateTime CreatedAt);
```

### 2.3 TwoFactorService Implementation

```csharp
using OtpNet;
using QRCoder;
using System.Security.Cryptography;

namespace AllowanceTracker.Services;

public class TwoFactorService : ITwoFactorService
{
    private readonly AllowanceContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwoFactorService> _logger;

    private const int RECOVERY_CODE_COUNT = 10;
    private const int TRUSTED_DEVICE_DAYS = 30;

    public async Task<TwoFactorSetupDto> GenerateSetupAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found");

        // Generate secret key
        var secretKey = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

        // Generate QR code
        var issuer = _configuration["AppName"] ?? "AllowanceTracker";
        var otpAuthUrl = $"otpauth://totp/{issuer}:{user.Email}?secret={secretKey}&issuer={issuer}";

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(otpAuthUrl, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        var qrCodeBase64 = Convert.ToBase64String(qrCodeImage);

        // Format manual entry key (groups of 4)
        var manualKey = string.Join(" ", Enumerable.Range(0, secretKey.Length / 4)
            .Select(i => secretKey.Substring(i * 4, 4)));

        return new TwoFactorSetupDto(secretKey, qrCodeBase64, manualKey);
    }

    public async Task<EnableTwoFactorResult> EnableTwoFactorAsync(
        Guid userId,
        string secretKey,
        string verificationCode)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found");

        // Verify the code before enabling
        var isValid = VerifyTotpCode(secretKey, verificationCode);
        if (!isValid)
        {
            return new EnableTwoFactorResult(
                false,
                new List<string>(),
                "Invalid verification code. Please try again.");
        }

        // Encrypt and store secret key
        var encryptedSecret = EncryptSecretKey(secretKey);
        user.TwoFactorSecretKey = encryptedSecret;
        user.TwoFactorEnabled = true;
        user.TwoFactorEnabledAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        // Generate recovery codes
        var recoveryCodes = await GenerateRecoveryCodesAsync(userId);

        _logger.LogInformation("2FA enabled for user {UserId}", userId);

        return new EnableTwoFactorResult(true, recoveryCodes);
    }

    public async Task DisableTwoFactorAsync(Guid userId, string password)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found");

        // Verify password before disabling 2FA
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
            throw new UnauthorizedAccessException("Invalid password");

        user.TwoFactorEnabled = false;
        user.TwoFactorSecretKey = null;
        user.TwoFactorEnabledAt = null;
        user.RecoveryCodesRemaining = 0;

        await _userManager.UpdateAsync(user);

        // Remove all recovery codes
        var recoveryCodes = await _context.RecoveryCodes
            .Where(rc => rc.UserId == userId)
            .ToListAsync();
        _context.RecoveryCodes.RemoveRange(recoveryCodes);

        // Revoke all trusted devices
        await RevokeAllTrustedDevicesAsync(userId);

        await _context.SaveChangesAsync();

        _logger.LogWarning("2FA disabled for user {UserId}", userId);
    }

    public async Task<bool> VerifyTotpCodeAsync(Guid userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found");

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecretKey))
            return false;

        var secretKey = DecryptSecretKey(user.TwoFactorSecretKey);
        return VerifyTotpCode(secretKey, code);
    }

    private bool VerifyTotpCode(string secretKey, string code)
    {
        var secretKeyBytes = Base32Encoding.ToBytes(secretKey);
        var totp = new Totp(secretKeyBytes, step: 30, totpSize: 6);

        // Verify current code and adjacent time windows (±1 window for clock skew)
        var now = DateTime.UtcNow;
        return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
    }

    public async Task<List<string>> GenerateRecoveryCodesAsync(Guid userId)
    {
        // Remove existing recovery codes
        var existingCodes = await _context.RecoveryCodes
            .Where(rc => rc.UserId == userId && !rc.IsUsed)
            .ToListAsync();
        _context.RecoveryCodes.RemoveRange(existingCodes);

        // Generate new recovery codes
        var recoveryCodes = new List<string>();
        for (int i = 0; i < RECOVERY_CODE_COUNT; i++)
        {
            var code = GenerateRecoveryCode();
            recoveryCodes.Add(code);

            var codeHash = HashRecoveryCode(code);
            _context.RecoveryCodes.Add(new RecoveryCode
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CodeHash = codeHash,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Update count
        var user = await _userManager.FindByIdAsync(userId.ToString());
        user!.RecoveryCodesRemaining = RECOVERY_CODE_COUNT;
        await _userManager.UpdateAsync(user);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated {Count} recovery codes for user {UserId}",
            RECOVERY_CODE_COUNT, userId);

        return recoveryCodes;
    }

    public async Task<bool> VerifyRecoveryCodeAsync(Guid userId, string code)
    {
        var codeHash = HashRecoveryCode(code);

        var recoveryCode = await _context.RecoveryCodes
            .Where(rc => rc.UserId == userId && rc.CodeHash == codeHash && !rc.IsUsed)
            .FirstOrDefaultAsync();

        if (recoveryCode == null)
            return false;

        // Mark as used
        recoveryCode.IsUsed = true;
        recoveryCode.UsedAt = DateTime.UtcNow;

        // Update count
        var user = await _userManager.FindByIdAsync(userId.ToString());
        user!.RecoveryCodesRemaining = Math.Max(0, user.RecoveryCodesRemaining - 1);
        await _userManager.UpdateAsync(user);

        await _context.SaveChangesAsync();

        _logger.LogWarning("Recovery code used for user {UserId}. Remaining: {Count}",
            userId, user.RecoveryCodesRemaining);

        return true;
    }

    public async Task<int> GetRemainingRecoveryCodesCountAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found");

        return user.RecoveryCodesRemaining;
    }

    public async Task<string> TrustDeviceAsync(
        Guid userId,
        string deviceName,
        string? ipAddress,
        string? userAgent)
    {
        var deviceIdentifier = GenerateDeviceIdentifier();

        var trustedDevice = new TrustedDevice
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceIdentifier = deviceIdentifier,
            DeviceName = deviceName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ExpiresAt = DateTime.UtcNow.AddDays(TRUSTED_DEVICE_DAYS),
            LastUsedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.TrustedDevices.Add(trustedDevice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Device {DeviceId} trusted for user {UserId} until {ExpiresAt}",
            trustedDevice.Id, userId, trustedDevice.ExpiresAt);

        return deviceIdentifier;
    }

    public async Task<bool> IsDeviceTrustedAsync(Guid userId, string deviceIdentifier)
    {
        // Clean up expired devices
        await CleanupExpiredDevicesAsync(userId);

        var trustedDevice = await _context.TrustedDevices
            .Where(td =>
                td.UserId == userId &&
                td.DeviceIdentifier == deviceIdentifier &&
                td.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (trustedDevice != null)
        {
            // Update last used
            trustedDevice.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<List<TrustedDeviceDto>> GetTrustedDevicesAsync(Guid userId)
    {
        await CleanupExpiredDevicesAsync(userId);

        var devices = await _context.TrustedDevices
            .Where(td => td.UserId == userId && td.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(td => td.LastUsedAt)
            .ToListAsync();

        return devices.Select(d => new TrustedDeviceDto(
            d.Id,
            d.DeviceName,
            d.IpAddress,
            d.LastUsedAt,
            d.ExpiresAt,
            d.CreatedAt
        )).ToList();
    }

    public async Task RevokeTrustedDeviceAsync(Guid userId, Guid deviceId)
    {
        var device = await _context.TrustedDevices
            .Where(td => td.Id == deviceId && td.UserId == userId)
            .FirstOrDefaultAsync();

        if (device != null)
        {
            _context.TrustedDevices.Remove(device);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Revoked trusted device {DeviceId} for user {UserId}",
                deviceId, userId);
        }
    }

    public async Task RevokeAllTrustedDevicesAsync(Guid userId)
    {
        var devices = await _context.TrustedDevices
            .Where(td => td.UserId == userId)
            .ToListAsync();

        _context.TrustedDevices.RemoveRange(devices);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Revoked all trusted devices for user {UserId}", userId);
    }

    private async Task CleanupExpiredDevicesAsync(Guid userId)
    {
        var expiredDevices = await _context.TrustedDevices
            .Where(td => td.UserId == userId && td.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        if (expiredDevices.Any())
        {
            _context.TrustedDevices.RemoveRange(expiredDevices);
            await _context.SaveChangesAsync();
        }
    }

    private string GenerateRecoveryCode()
    {
        // Generate 8-character alphanumeric code
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var code = new char[8];
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[8];
        rng.GetBytes(bytes);

        for (int i = 0; i < 8; i++)
        {
            code[i] = chars[bytes[i] % chars.Length];
        }

        // Format as XXXX-XXXX
        return $"{new string(code, 0, 4)}-{new string(code, 4, 4)}";
    }

    private string HashRecoveryCode(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(code);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private string GenerateDeviceIdentifier()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private string EncryptSecretKey(string secretKey)
    {
        // TODO: Implement proper encryption using Data Protection API
        // For now, return as-is (in production, encrypt this!)
        return secretKey;
    }

    private string DecryptSecretKey(string encryptedKey)
    {
        // TODO: Implement proper decryption using Data Protection API
        return encryptedKey;
    }
}
```

---

## Phase 3: Test Cases (20 Tests)

```csharp
namespace AllowanceTracker.Tests.Services;

public class TwoFactorServiceTests
{
    // Setup Tests (3)
    [Fact] GenerateSetup_CreatesSecretKeyAndQrCode
    [Fact] GenerateSetup_ReturnsManualEntryKey
    [Fact] GenerateSetup_QrCodeIsValidBase64

    // Enable/Disable Tests (5)
    [Fact] EnableTwoFactor_ValidCode_EnablesSuccessfully
    [Fact] EnableTwoFactor_InvalidCode_ReturnsError
    [Fact] EnableTwoFactor_GeneratesRecoveryCodes
    [Fact] DisableTwoFactor_ValidPassword_DisablesSuccessfully
    [Fact] DisableTwoFactor_InvalidPassword_ThrowsException

    // TOTP Verification Tests (4)
    [Fact] VerifyTotpCode_ValidCode_ReturnsTrue
    [Fact] VerifyTotpCode_InvalidCode_ReturnsFalse
    [Fact] VerifyTotpCode_ExpiredCode_ReturnsFalse
    [Fact] VerifyTotpCode_AllowsClockSkew

    // Recovery Code Tests (4)
    [Fact] GenerateRecoveryCodes_Creates10Codes
    [Fact] VerifyRecoveryCode_ValidCode_ReturnsTrue
    [Fact] VerifyRecoveryCode_UsedCode_ReturnsFalse
    [Fact] VerifyRecoveryCode_DecrementsCount

    // Trusted Device Tests (4)
    [Fact] TrustDevice_CreatesDeviceRecord
    [Fact] IsDeviceTrusted_ValidDevice_ReturnsTrue
    [Fact] IsDeviceTrusted_ExpiredDevice_ReturnsFalse
    [Fact] RevokeAllTrustedDevices_RemovesAllDevices
}
```

---

## Success Metrics

- ✅ All 20 tests passing
- ✅ TOTP codes verified correctly with time window tolerance
- ✅ Recovery codes generated and hashed securely
- ✅ Trusted devices tracked and expired properly
- ✅ QR codes generated and scannable
- ✅ 2FA required for sensitive operations
- ✅ Account recovery flow works smoothly

---

**Total Implementation Time**: 2 weeks
