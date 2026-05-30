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
    IOptionsMonitor<MarketScheduleSettings> marketScheduleOptions,
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
            if (IsMarketHoursActive())
            {
                logger.LogInformation(
                    "Stock fundamentals polling skipped because stock market hours are active.");
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

    private bool IsMarketHoursActive()
    {
        var settings = marketScheduleOptions.CurrentValue;
        var timeZone = GetTimeZone(settings.TimeZoneId);
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

        if (nowLocal.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        var localTime = TimeOnly.FromDateTime(nowLocal);
        var openTime = ParseTime(settings.OpenTime, new TimeOnly(9, 0));
        var closeTime = ParseTime(settings.CloseTime, new TimeOnly(15, 30));

        return localTime >= openTime && localTime < closeTime;
    }

    private static TimeZoneInfo GetTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        }
    }

    private static TimeOnly ParseTime(string value, TimeOnly fallback)
    {
        return TimeOnly.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }
}
