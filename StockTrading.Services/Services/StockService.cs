using StockTrading.Common.DTOs;
using StockTrading.Common.Enums;
using StockTrading.IServices;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Services;

public sealed class StockService(
    IBrokerService brokerService,
    IMarketDataCacheService marketDataCacheService,
    IStockRepository stockRepository,
    ITradePlanRepository tradePlanRepository,
    IMarketScheduleService marketScheduleService) : IStockService
{
    public Task<PagedResult<StockListItem>> GetStocksAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return stockRepository.GetPageAsync(page, pageSize, cancellationToken);
    }

    public Task<Stock> SaveStockAsync(SaveStockRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedRequest = Normalize(request);
        return stockRepository.UpsertAsync(normalizedRequest, cancellationToken);
    }

    public async Task<StockServiceDeleteResult> DeleteStockAsync(
        int stockId,
        CancellationToken cancellationToken = default)
    {
        var stock = await stockRepository.GetByIdAsync(stockId, cancellationToken);
        if (stock == null)
        {
            return new StockServiceDeleteResult(false, "Stock not found.");
        }

        var deleteCheck = await stockRepository.GetDeleteCheckAsync(stockId, cancellationToken);
        if (deleteCheck.HasDependencies)
        {
            return new StockServiceDeleteResult(
                false,
                "Stock cannot be removed because it is used by other records.",
                deleteCheck.GetMessages());
        }

        await stockRepository.DeleteAsync(stockId, cancellationToken);
        return new StockServiceDeleteResult(true, "Stock removed.");
    }

    public Task<HoldingsResponse> GetHoldingsAsync(CancellationToken cancellationToken = default)
    {
        return brokerService.GetHoldingsAsync();
    }

    public Task<List<StockSearchResult>> SearchStocksAsync(
        string query,
        StockExchange exchange = StockExchange.NSE,
        CancellationToken cancellationToken = default)
    {
        return brokerService.SearchStocksAsync(query, exchange);
    }

    public Task<List<StockCandle>> GetCandlesAsync(
        string symbolToken,
        StockExchange exchange = StockExchange.NSE,
        StockChartInterval interval = StockChartInterval.ONE_DAY,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        return brokerService.GetCandlesAsync(symbolToken, exchange, interval, from, to);
    }

    public async Task<List<StockPrice>> GetConfiguredPricesAsync(CancellationToken cancellationToken = default)
    {
        var stocks = await stockRepository.GetAllAsync(cancellationToken);
        return await marketDataCacheService.GetPricesAsync(
            stocks,
            stocks => brokerService.GetPricesAsync(stocks),
            cancellationToken);
    }

    public async Task<List<StockPrice>> GetPricesAsync(
        IEnumerable<StockListItem> stocks,
        CancellationToken cancellationToken = default)
    {
        var stockList = stocks.ToArray();
        var decision = await marketScheduleService.DecideAsync("PriceApi", cancellationToken: cancellationToken);
        if (!decision.JobsEnabled)
        {
            return GetMarketClosedPrices(stockList, decision);
        }

        return await marketDataCacheService.GetPricesAsync(
            stockList,
            stocks => brokerService.GetPricesAsync(stocks),
            cancellationToken);
    }

    public async Task<List<StockPrice>> RefreshConfiguredPricesAsync(CancellationToken cancellationToken = default)
    {
        var tradePlans = await tradePlanRepository.GetAllAsync(cancellationToken);
        var stocks = tradePlans
            .GroupBy(tradePlan => tradePlan.StockId)
            .Select(group => group.First())
            .Select(tradePlan => new StockListItem
            {
                StockId = tradePlan.StockId,
                Symbol = tradePlan.Symbol,
                Name = tradePlan.Name,
                Exchange = tradePlan.Exchange,
                SymbolToken = tradePlan.SymbolToken,
                TradingSymbol = tradePlan.TradingSymbol
            })
            .ToArray();

        var decision = await marketScheduleService.DecideAsync("StockPricePolling", cancellationToken: cancellationToken);
        if (!decision.JobsEnabled)
        {
            return GetMarketClosedPrices(stocks, decision);
        }

        return await brokerService.GetPricesAsync(stocks);
    }

    private static List<StockPrice> GetMarketClosedPrices(
        IEnumerable<StockListItem> stocks,
        MarketJobDecisionResult decision)
    {
        return stocks.Select(stock => new StockPrice
        {
            Symbol = stock.Symbol,
            TradingSymbol = stock.TradingSymbol,
            Exchange = string.IsNullOrWhiteSpace(stock.Exchange) ? decision.Exchange : stock.Exchange,
            SymbolToken = stock.SymbolToken,
            IsFetched = false,
            Message = decision.DecisionReason
        }).ToList();
    }

    private static SaveStockRequest Normalize(SaveStockRequest request)
    {
        return new SaveStockRequest
        {
            StockId = request.StockId,
            Symbol = request.Symbol.Trim().ToUpperInvariant(),
            Name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim(),
            Exchange = request.Exchange,
            SymbolToken = request.SymbolToken.Trim(),
            TradingSymbol = string.IsNullOrWhiteSpace(request.TradingSymbol)
                ? request.Symbol.Trim().ToUpperInvariant()
                : request.TradingSymbol.Trim().ToUpperInvariant()
        };
    }
}
