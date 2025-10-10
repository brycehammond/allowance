using AllowanceTracker.Data;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.SignalR;

namespace AllowanceTracker.Hubs;

public class FamilyHub : Hub
{
    private readonly ICurrentUserService _currentUser;
    private readonly AllowanceContext _context;

    public FamilyHub(ICurrentUserService currentUser, AllowanceContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var user = await _context.Users.FindAsync(_currentUser.UserId);

        if (user?.FamilyId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"family-{user.FamilyId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var user = await _context.Users.FindAsync(_currentUser.UserId);

        if (user?.FamilyId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"family-{user.FamilyId}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
