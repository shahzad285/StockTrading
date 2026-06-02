namespace StockTrading.Common.DTOs;

public class StockListItem
{
    public int StockId { get; set; }
    public required string Symbol { get; set; }
    public string? Name { get; set; }
    public string Exchange { get; set; } = "NSE";
    public string SymbolToken { get; set; } = "";
    public string TradingSymbol { get; set; } = "";
    public int HoldingQuantity { get; set; }
    public string AssetType { get; set; } = "Unknown";
    public string? Theme { get; set; }
    public string? Sector { get; set; }
    public string? Industry { get; set; }
    public string? ClassificationReason { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public string? Description { get; set; }
    public bool UpdatedByNse { get; set; }
    public bool UpdatedByYahoo { get; set; }
    public bool UpdatedByTapetide { get; set; }
    public decimal? DividendYield { get; set; }
    public decimal? GrowthRate { get; set; }
    public decimal? DebtToEquity { get; set; }
    public decimal? PERatio { get; set; }
    public decimal? EarningsPerShare { get; set; }
    public decimal? PriceToBook { get; set; }
    public decimal? TotalRevenue { get; set; }
    public decimal? NetIncome { get; set; }
    public decimal? TotalDebt { get; set; }
    public decimal? TotalCash { get; set; }
    public decimal? CashFlow { get; set; }
    public decimal? MarketCap { get; set; }
    public string StockCategory { get; set; } = "Unknown";
    public string? StockCategoryReason { get; set; }
    public decimal? StockCategoryConfidence { get; set; }
    public DateTime? StockCategoryUpdatedAtUtc { get; set; }
    public DateTime? LastAnalyzedAtUtc { get; set; }
}
