namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

public interface INotificationHubService
{
    Task SendToUserAsync(Guid userId, string method, object data);
}
