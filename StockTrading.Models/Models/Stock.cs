namespace StockTrading.Models;

public class Stock
{
    public int Id { get; set; }
    public string Symbol { get; set; } = "";
    public string Exchange { get; set; } = "NSE";
    public string SymbolToken { get; set; } = "";
    public string TradingSymbol { get; set; } = "";
    public string? Name { get; set; }
    public int HoldingQuantity { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
