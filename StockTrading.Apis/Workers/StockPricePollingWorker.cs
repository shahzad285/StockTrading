using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Common.Settings;
using StockTrading.IServices;

namespace StockTrading.Workers;

public sealed class StockPricePollingWorker(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<StockPollingSettings> options,
    ILogger<StockPricePollingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = options.CurrentValue;
            var interval = TimeSpan.FromSeconds(Math.Max(1, settings.IntervalSeconds));

            if (settings.Enabled)
            {
                await RefreshPricesAsync(stoppingToken);
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RefreshPricesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var marketScheduleService = scope.ServiceProvider.GetRequiredService<IMarketScheduleService>();
            var decision = await marketScheduleService.DecideAsync("StockPricePolling", cancellationToken: cancellationToken);
            if (!decision.JobsEnabled)
            {
                logger.LogInformation(
                    "Stock price polling skipped. Reason: {Reason}",
                    decision.DecisionReason);
                return;
            }

            var brokerSessionStore = scope.ServiceProvider.GetRequiredService<IBrokerSessionStore>();
            var brokerSession = await brokerSessionStore.GetAsync("AngelOne", cancellationToken: cancellationToken);
            if (brokerSession == null ||
                string.IsNullOrWhiteSpace(brokerSession.AccessToken) ||
                string.IsNullOrWhiteSpace(brokerSession.RefreshToken))
            {
                logger.LogDebug("Stock price polling skipped because Angel One broker session is not available.");
                return;
            }

            var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
            var prices = await stockService.RefreshConfiguredPricesAsync(cancellationToken);

            logger.LogInformation("Stock price polling refreshed {Count} prices.", prices.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Stock price polling failed.");
        }
    }
}
