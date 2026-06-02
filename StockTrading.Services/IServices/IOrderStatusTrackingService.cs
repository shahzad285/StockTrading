namespace StockTrading.IServices;

public interface IOrderStatusTrackingService
{
    Task TrackOpenOrdersAsync(CancellationToken cancellationToken = default);
}
