namespace StockTrading.IServices;

public interface ITradePlanExecutionService
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
