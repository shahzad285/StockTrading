namespace StockTrading.Models;

public class TradePlan
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public decimal BuyPrice { get; set; }
    public decimal SellPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal? MaxBudget { get; set; }
    public string Status { get; set; } = TradePlanStatuses.Active;
    public bool IsActive { get; set; } = true;
    public bool RepeatEnabled { get; set; } = true;
    public int BuyTriggerCount { get; set; }
    public int SellTriggerCount { get; set; }
    public DateTime? LastBuyTriggeredAtUtc { get; set; }
    public DateTime? LastSellTriggeredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public string Symbol { get; set; } = "";
    public string? Name { get; set; }
    public string Exchange { get; set; } = "NSE";
    public string SymbolToken { get; set; } = "";
    public string TradingSymbol { get; set; } = "";
}

public static class TradePlanStatuses
{
    public const string Active = "Active";
    public const string Paused = "Paused";
    public const string Cancelled = "Cancelled";
}
