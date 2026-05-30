using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Common.Settings;
using StockTrading.IServices;

namespace StockTrading.Services;

public sealed class StockFundamentalsPollingWorker(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<FundamentalsPollingSettings> options,
    ILogger<StockFundamentalsPollingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = options.CurrentValue;
            var interval = TimeSpan.FromMinutes(Math.Max(1, settings.IntervalMinutes));

            if (settings.Enabled)
            {
                await RefreshFundamentalsAsync(settings, stoppingToken);
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RefreshFundamentalsAsync(
        FundamentalsPollingSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var marketScheduleService = scope.ServiceProvider.GetRequiredService<IMarketScheduleService>();
            var decision = await marketScheduleService.DecideAsync("StockFundamentalsPolling", cancellationToken: cancellationToken);
            if (decision.JobsEnabled)
            {
                logger.LogInformation(
                    "Stock fundamentals polling skipped. Reason: Market is open for price-sensitive jobs.");
                return;
            }

            var stockFundamentalsService = scope.ServiceProvider.GetRequiredService<IStockFundamentalsService>();
            var updatedCount = await stockFundamentalsService.RefreshMissingProfilesAsync(
                settings.MaxStocksPerRun,
                cancellationToken);

            logger.LogInformation("Stock fundamentals polling updated {Count} profiles.", updatedCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Stock fundamentals polling failed.");
        }
    }
}
