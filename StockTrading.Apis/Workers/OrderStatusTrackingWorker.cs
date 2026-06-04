using StockTrading.IServices;

namespace StockTrading.Workers;

public sealed class OrderStatusTrackingWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<OrderStatusTrackingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                if (!MarketWorkerSchedule.IsWithinMarketWindow(configuration))
                {
                    continue;
                }

                var service = scope.ServiceProvider.GetRequiredService<IOrderStatusTrackingService>();
                await service.TrackOpenOrdersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to track order statuses.");
            }
        }
    }
}
