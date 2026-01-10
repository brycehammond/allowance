using AllowanceTracker.DTOs;
using AllowanceTracker.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AllowanceTracker.Services;

public class SignalRNotificationService : ISignalRNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToUserAsync(Guid userId, NotificationDto notification)
    {
        await _hubContext.Clients
            .Group($"user_{userId}")
            .SendAsync("ReceiveNotification", notification);
    }

    public async Task SendToFamilyAsync(Guid familyId, NotificationDto notification)
    {
        await _hubContext.Clients
            .Group($"family_{familyId}")
            .SendAsync("ReceiveNotification", notification);
    }

    public async Task SendUnreadCountAsync(Guid userId, int count)
    {
        await _hubContext.Clients
            .Group($"user_{userId}")
            .SendAsync("UnreadCountChanged", count);
    }

    public async Task SendBalanceUpdatedAsync(Guid familyId, Guid childId, decimal newBalance)
    {
        await _hubContext.Clients
            .Group($"family_{familyId}")
            .SendAsync("BalanceUpdated", childId, newBalance);
    }
}
