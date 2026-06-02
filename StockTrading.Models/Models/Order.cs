using StockTrading.Common.Enums;

namespace StockTrading.Models;

public class Order
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public int? TradePlanId { get; set; }
    public string BrokerOrderId { get; set; } = "";
    public string TradingSymbol { get; set; } = "";
    public string Exchange { get; set; } = "";
    public string SymbolToken { get; set; } = "";
    public string TransactionType { get; set; } = "";
    public string OrderType { get; set; } = "";
    public string ProductType { get; set; } = "";
    public string Duration { get; set; } = "";
    public OrderSource Source { get; set; } = OrderSource.Unknown;
    public OrderStatus Status { get; set; } = OrderStatus.Unknown;
    public string RejectionReason { get; set; } = "";
    public int Quantity { get; set; }
    public int FilledShares { get; set; }
    public int UnfilledShares { get; set; }
    public int CancelledShares { get; set; }
    public decimal Price { get; set; }
    public decimal TriggerPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public string UpdateTime { get; set; } = "";
    public string ExchangeTime { get; set; } = "";
    public string ParentBrokerOrderId { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<OrderHistory> History { get; set; } = new();
}
