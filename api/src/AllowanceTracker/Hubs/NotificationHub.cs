using AllowanceTracker.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AllowanceTracker.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly AllowanceContext _context;

    public NotificationHub(AllowanceContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
            // Join user-specific group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Also join family group if user belongs to a family
            var familyId = await GetUserFamilyIdAsync(Guid.Parse(userId));
            if (familyId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"family_{familyId}");
            }
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Groups are automatically cleaned up when connection is closed
        await base.OnDisconnectedAsync(exception);
    }

    private async Task<Guid?> GetUserFamilyIdAsync(Guid userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.FamilyId;
    }
}
