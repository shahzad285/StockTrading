namespace StockTrading.Common.Configuration;

public sealed class MarketDataCacheOptions
{
    public int TradingHoursPriceTtlSeconds { get; set; } = 15;
    public int TradingHoursBalanceTtlSeconds { get; set; } = 30;
    public int AfterHoursPriceTtlMinutes { get; set; } = 120;
    public int AfterHoursBalanceTtlMinutes { get; set; } = 30;
}
