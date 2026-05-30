namespace StockTrading.Common.DTOs;

public sealed class StockDeleteCheck
{
    public int TradePlanCount { get; set; }
    public int TradePlanRunCount { get; set; }
    public int OrderCount { get; set; }

    public bool HasDependencies =>
        TradePlanCount > 0 ||
        TradePlanRunCount > 0 ||
        OrderCount > 0;

    public IReadOnlyList<string> GetMessages()
    {
        var messages = new List<string>();
        AddMessage(messages, TradePlanCount, "trade plan", "trade plans");
        AddMessage(messages, TradePlanRunCount, "trade plan run", "trade plan runs");
        AddMessage(messages, OrderCount, "order", "orders");
        return messages;
    }

    private static void AddMessage(List<string> messages, int count, string singular, string plural)
    {
        if (count > 0)
        {
            messages.Add($"{count} {(count == 1 ? singular : plural)}");
        }
    }
}
