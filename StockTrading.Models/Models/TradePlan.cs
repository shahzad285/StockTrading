namespace StockTrading.Models;

public class TradePlan
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public decimal BuyPrice { get; set; }
    public decimal SellPrice { get; set; }
    public int MaxStocksAllowed { get; set; }
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
