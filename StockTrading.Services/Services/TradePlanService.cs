using StockTrading.IServices;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Services;

public sealed class TradePlanService(ITradePlanRepository tradePlanRepository) : ITradePlanService
{
    public Task<IReadOnlyList<TradePlan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return tradePlanRepository.GetAllAsync(cancellationToken);
    }

    public Task<TradePlan> SaveAsync(TradePlan tradePlan, CancellationToken cancellationToken = default)
    {
        return tradePlanRepository.SaveAsync(Normalize(tradePlan), cancellationToken);
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return tradePlanRepository.DeleteAsync(id, cancellationToken);
    }

    private static TradePlan Normalize(TradePlan tradePlan)
    {
        if (string.IsNullOrWhiteSpace(tradePlan.Symbol))
        {
            throw new ArgumentException("Symbol is required.");
        }

        if (string.IsNullOrWhiteSpace(tradePlan.SymbolToken))
        {
            throw new ArgumentException("Symbol token is required.");
        }

        if (tradePlan.BuyPrice <= 0)
        {
            throw new ArgumentException("Buy price must be greater than zero.");
        }

        if (tradePlan.SellPrice <= 0)
        {
            throw new ArgumentException("Sell price must be greater than zero.");
        }

        if (tradePlan.Quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.");
        }

        return new TradePlan
        {
            Id = tradePlan.Id,
            StockId = tradePlan.StockId,
            BuyPrice = tradePlan.BuyPrice,
            SellPrice = tradePlan.SellPrice,
            Quantity = tradePlan.Quantity,
            MaxBudget = tradePlan.MaxBudget,
            Status = string.IsNullOrWhiteSpace(tradePlan.Status)
                ? TradePlanStatuses.Active
                : tradePlan.Status.Trim(),
            IsActive = tradePlan.IsActive,
            RepeatEnabled = tradePlan.RepeatEnabled,
            Symbol = tradePlan.Symbol.Trim().ToUpperInvariant(),
            Name = GetStockName(tradePlan.Name, tradePlan.Symbol, tradePlan.TradingSymbol),
            Exchange = string.IsNullOrWhiteSpace(tradePlan.Exchange)
                ? "NSE"
                : tradePlan.Exchange.Trim().ToUpperInvariant(),
            SymbolToken = tradePlan.SymbolToken.Trim(),
            TradingSymbol = string.IsNullOrWhiteSpace(tradePlan.TradingSymbol)
                ? tradePlan.Symbol.Trim().ToUpperInvariant()
                : tradePlan.TradingSymbol.Trim().ToUpperInvariant()
        };
    }

    private static string GetStockName(string? name, string symbol, string tradingSymbol)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name.Trim();
        }

        var fallback = string.IsNullOrWhiteSpace(symbol) ? tradingSymbol : symbol;
        return GetDisplayName(fallback);
    }

    private static string GetDisplayName(string value)
    {
        var name = value.Trim().ToUpperInvariant();
        foreach (var suffix in new[] { "-EQ", "-BE" })
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return name[..^suffix.Length];
            }
        }

        return name;
    }
}
