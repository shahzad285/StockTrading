namespace StockTrading.Workers;

internal static class MarketWorkerSchedule
{
    public static bool IsWithinMarketWindow(IConfiguration configuration)
    {
        var timeZoneId = configuration["MarketSchedule:TimeZoneId"] ?? "India Standard Time";
        var openTimeText = configuration["MarketSchedule:OpenTime"] ?? "09:15";
        var closeTimeText = configuration["MarketSchedule:CloseTime"] ?? "15:30";

        var timeZone = GetTimeZone(timeZoneId);
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        if (now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        var openTime = TimeOnly.TryParse(openTimeText, out var configuredOpenTime)
            ? configuredOpenTime
            : new TimeOnly(9, 15);
        var closeTime = TimeOnly.TryParse(closeTimeText, out var configuredCloseTime)
            ? configuredCloseTime
            : new TimeOnly(15, 30);
        var currentTime = TimeOnly.FromDateTime(now);

        return currentTime >= openTime && currentTime <= closeTime;
    }

    private static TimeZoneInfo GetTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        }
    }
}
