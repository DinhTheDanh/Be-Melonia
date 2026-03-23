using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.API;

/// <summary>
/// Background service publish các bài hát đã đến lịch phát hành.
/// </summary>
public class ReleaseSchedulingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReleaseSchedulingBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private const int BatchSize = 100;

    public ReleaseSchedulingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ReleaseSchedulingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReleaseSchedulingBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var musicService = scope.ServiceProvider.GetRequiredService<IMusicService>();

                var publishedCount = await musicService.PublishDueScheduledSongsAsync(BatchSize);
                if (publishedCount > 0)
                {
                    _logger.LogInformation("Published {Count} scheduled songs", publishedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReleaseSchedulingBackgroundService");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("ReleaseSchedulingBackgroundService stopped");
    }
}
