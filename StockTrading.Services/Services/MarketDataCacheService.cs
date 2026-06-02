using System.Collections.Concurrent;
using StockTrading.Common.Configuration;
using StockTrading.Common.DTOs;
using StockTrading.IServices;

namespace StockTrading.Services;

public sealed class MarketDataCacheService(MarketDataCacheOptions options) : IMarketDataCacheService
{
    private static readonly TimeZoneInfo IndiaTimeZone = GetIndiaTimeZone();
    private readonly ConcurrentDictionary<string, CacheEntry<StockPrice>> priceCache = new();
    private readonly ConcurrentDictionary<string, CacheEntry<object>> balanceCache = new();

    public async Task<List<StockPrice>> GetPricesAsync(
        IEnumerable<StockListItem> stocks,
        Func<IEnumerable<StockListItem>, Task<List<StockPrice>>> fetchPricesAsync,
        CancellationToken cancellationToken = default)
    {
        var stockList = stocks
            .Where(stock => !string.IsNullOrWhiteSpace(stock.SymbolToken))
            .GroupBy(stock => GetPriceCacheKey(stock.Exchange, stock.SymbolToken))
            .Select(group => group.First())
            .ToArray();

        var prices = new List<StockPrice>();
        var missingStocks = new List<StockListItem>();

        foreach (var stock in stockList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cacheKey = GetPriceCacheKey(stock.Exchange, stock.SymbolToken);
            if (priceCache.TryGetValue(cacheKey, out var cachedPrice) &&
                cachedPrice.ExpiresAtUtc > DateTime.UtcNow)
            {
                prices.Add(cachedPrice.Value);
                continue;
            }

            missingStocks.Add(stock);
        }

        if (missingStocks.Count > 0)
        {
            var fetchedPrices = await fetchPricesAsync(missingStocks);
            foreach (var price in fetchedPrices)
            {
                var ttl = price.IsFetched ? GetPriceTtl() : TimeSpan.FromSeconds(5);
                priceCache[GetPriceCacheKey(price.Exchange, price.SymbolToken)] = new CacheEntry<StockPrice>(
                    price,
                    DateTime.UtcNow.Add(ttl));
                prices.Add(price);
            }
        }

        return prices;
    }

    public async Task<T?> GetAccountBalanceAsync<T>(
        Func<Task<T?>> fetchBalanceAsync,
        CancellationToken cancellationToken = default)
    {
        const string cacheKey = "broker-account-balance:active";
        if (balanceCache.TryGetValue(cacheKey, out var cachedBalance) &&
            cachedBalance.ExpiresAtUtc > DateTime.UtcNow)
        {
            return cachedBalance.Value is T typedBalance ? typedBalance : default;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var balance = await fetchBalanceAsync();
        if (balance != null)
        {
            balanceCache[cacheKey] = new CacheEntry<object>(
                balance,
                DateTime.UtcNow.Add(GetBalanceTtl()));
        }
        else
        {
            balanceCache[cacheKey] = new CacheEntry<object>(
                new NullBalanceMarker(),
                DateTime.UtcNow.Add(TimeSpan.FromSeconds(5)));
        }

        return balance;
    }

    private static string GetPriceCacheKey(string exchange, string symbolToken)
    {
        return $"stock-price:{exchange.Trim().ToUpperInvariant()}:{symbolToken.Trim()}";
    }

    private TimeSpan GetPriceTtl()
    {
        return IsTradingHours()
            ? TimeSpan.FromSeconds(Math.Max(1, options.TradingHoursPriceTtlSeconds))
            : TimeSpan.FromMinutes(Math.Max(1, options.AfterHoursPriceTtlMinutes));
    }

    private TimeSpan GetBalanceTtl()
    {
        return IsTradingHours()
            ? TimeSpan.FromSeconds(Math.Max(1, options.TradingHoursBalanceTtlSeconds))
            : TimeSpan.FromMinutes(Math.Max(1, options.AfterHoursBalanceTtlMinutes));
    }

    private static bool IsTradingHours()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndiaTimeZone);
        if (now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        var currentTime = TimeOnly.FromDateTime(now);
        return currentTime >= new TimeOnly(9, 15) &&
               currentTime <= new TimeOnly(15, 30);
    }

    private static TimeZoneInfo GetIndiaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        }
    }

    private sealed record CacheEntry<T>(T Value, DateTime ExpiresAtUtc);

    private sealed class NullBalanceMarker
    {
    }
}
