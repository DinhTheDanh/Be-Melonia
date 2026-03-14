using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.API;

/// <summary>
/// Background service tự động hủy các payment Pending quá hạn (mặc định 15 ngày).
/// Chạy mỗi 24 giờ.
/// </summary>
public class PaymentExpiryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentExpiryBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);
    private const int DefaultDaysThreshold = 15;

    public PaymentExpiryBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<PaymentExpiryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentExpiryBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();

                _logger.LogInformation("Running payment expiry check at {Time}", DateTime.UtcNow);
                var result = await adminService.CancelExpiredPaymentsAsync(DefaultDaysThreshold);

                if (result.Data != null && result.Data.CancelledCount > 0)
                {
                    _logger.LogInformation("Auto-cancelled {Count} expired pending payments", result.Data.CancelledCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PaymentExpiryBackgroundService");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("PaymentExpiryBackgroundService stopped");
    }
}
