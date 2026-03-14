using Microsoft.AspNetCore.SignalR;
using MUSIC.STREAMING.WEBSITE.API.Hubs;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.API.Services;

public class NotificationHubService : INotificationHubService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationHubService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToUserAsync(Guid userId, string method, object data)
    {
        await _hubContext.Clients.Group($"user_{userId}").SendAsync(method, data);
    }
}
