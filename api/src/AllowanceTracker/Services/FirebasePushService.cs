using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace AllowanceTracker.Services;

public class FirebasePushService : IFirebasePushService
{
    private readonly IDeviceTokenService _deviceTokenService;
    private readonly ILogger<FirebasePushService> _logger;
    private readonly FirebaseMessaging? _messaging;

    public bool IsAvailable => _messaging != null;

    public FirebasePushService(
        IDeviceTokenService deviceTokenService,
        ILogger<FirebasePushService> logger,
        IConfiguration configuration)
    {
        _deviceTokenService = deviceTokenService;
        _logger = logger;

        // Initialize Firebase Admin SDK if configured
        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                var credentialPath = configuration["Firebase:CredentialPath"];
                var credentialJson = configuration["Firebase:CredentialJson"];

                if (!string.IsNullOrEmpty(credentialPath) && File.Exists(credentialPath))
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(credentialPath)
                    });
                    _logger.LogInformation("Firebase initialized from credential file: {Path}", credentialPath);
                }
                else if (!string.IsNullOrEmpty(credentialJson))
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromJson(credentialJson)
                    });
                    _logger.LogInformation("Firebase initialized from credential JSON");
                }
                else
                {
                    _logger.LogWarning("Firebase credentials not configured. Push notifications will be disabled.");
                    return;
                }
            }

            _messaging = FirebaseMessaging.DefaultInstance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firebase. Push notifications will be disabled.");
        }
    }

    public async Task<bool> SendPushAsync(Guid userId, string title, string body, object? data = null)
    {
        if (!IsAvailable)
        {
            _logger.LogDebug("Firebase not available, skipping push for user {UserId}", userId);
            return false;
        }

        var devices = await _deviceTokenService.GetActiveDevicesAsync(userId);
        if (!devices.Any())
        {
            _logger.LogDebug("No active devices for user {UserId}", userId);
            return false;
        }

        var tokens = devices.Select(d => d.Token).ToList();
        var successCount = await SendToMultipleAsync(tokens, title, body, data);

        return successCount > 0;
    }

    public async Task<bool> SendToDeviceAsync(string fcmToken, string title, string body, object? data = null)
    {
        if (!IsAvailable)
        {
            _logger.LogDebug("Firebase not available, skipping push to device");
            return false;
        }

        try
        {
            var message = new Message
            {
                Token = fcmToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = ConvertToStringDictionary(data),
                // iOS specific configuration
                Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        Badge = 1,
                        Sound = "default"
                    }
                },
                // Android specific configuration
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        Sound = "default",
                        ClickAction = "OPEN_NOTIFICATION",
                        ChannelId = "allowance_notifications"
                    }
                }
            };

            var response = await _messaging!.SendAsync(message);
            _logger.LogInformation("FCM message sent: {MessageId}", response);
            return true;
        }
        catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
        {
            _logger.LogWarning("FCM token is unregistered, deactivating: {Token}", fcmToken[..Math.Min(20, fcmToken.Length)]);
            // Token is invalid, deactivate it
            await _deviceTokenService.DeactivateByTokenAsync(fcmToken, Guid.Empty);
            return false;
        }
        catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
        {
            _logger.LogWarning("FCM invalid argument for token: {Token}, Error: {Error}",
                fcmToken[..Math.Min(20, fcmToken.Length)], ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM message to device");
            return false;
        }
    }

    public async Task<int> SendToMultipleAsync(List<string> fcmTokens, string title, string body, object? data = null)
    {
        if (!IsAvailable || !fcmTokens.Any())
        {
            return 0;
        }

        try
        {
            var message = new MulticastMessage
            {
                Tokens = fcmTokens,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = ConvertToStringDictionary(data),
                Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        Badge = 1,
                        Sound = "default"
                    }
                },
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        Sound = "default",
                        ChannelId = "allowance_notifications"
                    }
                }
            };

            var response = await _messaging!.SendEachForMulticastAsync(message);

            // Handle failed tokens
            if (response.FailureCount > 0)
            {
                for (int i = 0; i < response.Responses.Count; i++)
                {
                    if (!response.Responses[i].IsSuccess)
                    {
                        var error = response.Responses[i].Exception;
                        if (error?.MessagingErrorCode == MessagingErrorCode.Unregistered)
                        {
                            _logger.LogWarning("FCM token unregistered, deactivating: {Token}",
                                fcmTokens[i][..Math.Min(20, fcmTokens[i].Length)]);
                            await _deviceTokenService.DeactivateByTokenAsync(fcmTokens[i], Guid.Empty);
                        }
                        else
                        {
                            _logger.LogWarning("FCM send failed for token: {Error}", error?.Message);
                        }
                    }
                }
            }

            _logger.LogInformation("FCM multicast: {Success} succeeded, {Failed} failed",
                response.SuccessCount, response.FailureCount);

            return response.SuccessCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM multicast message");
            return 0;
        }
    }

    public async Task<bool> SendToTopicAsync(string topic, string title, string body, object? data = null)
    {
        if (!IsAvailable)
        {
            _logger.LogDebug("Firebase not available, skipping topic push");
            return false;
        }

        try
        {
            var message = new Message
            {
                Topic = topic,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = ConvertToStringDictionary(data),
                Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        Sound = "default"
                    }
                },
                Android = new AndroidConfig
                {
                    Priority = Priority.High
                }
            };

            var response = await _messaging!.SendAsync(message);
            _logger.LogInformation("FCM topic message sent to {Topic}: {MessageId}", topic, response);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM topic message to {Topic}", topic);
            return false;
        }
    }

    private static Dictionary<string, string>? ConvertToStringDictionary(object? data)
    {
        if (data == null) return null;

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            return dict?.ToDictionary(
                k => k.Key,
                v => v.Value?.ToString() ?? ""
            );
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// No-op implementation when Firebase is not configured
/// </summary>
public class NoOpFirebasePushService : IFirebasePushService
{
    private readonly ILogger<NoOpFirebasePushService> _logger;

    public bool IsAvailable => false;

    public NoOpFirebasePushService(ILogger<NoOpFirebasePushService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendPushAsync(Guid userId, string title, string body, object? data = null)
    {
        _logger.LogDebug("NoOp: Would send push to user {UserId}: {Title}", userId, title);
        return Task.FromResult(false);
    }

    public Task<bool> SendToDeviceAsync(string fcmToken, string title, string body, object? data = null)
    {
        _logger.LogDebug("NoOp: Would send push to device: {Title}", title);
        return Task.FromResult(false);
    }

    public Task<int> SendToMultipleAsync(List<string> fcmTokens, string title, string body, object? data = null)
    {
        _logger.LogDebug("NoOp: Would send push to {Count} devices: {Title}", fcmTokens.Count, title);
        return Task.FromResult(0);
    }

    public Task<bool> SendToTopicAsync(string topic, string title, string body, object? data = null)
    {
        _logger.LogDebug("NoOp: Would send push to topic {Topic}: {Title}", topic, title);
        return Task.FromResult(false);
    }
}
