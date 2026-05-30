# TODO

## Planned

- Get details of stocks created or recommended by reputable stock market investors/retailers such as Jhunjhunwala.
- Plan stock category/type classification using Peter Lynch categories: Slow Grower, Stalwart, Fast Grower, Cyclical, Turnaround, and Asset Play.

## Later

- Add realtime or near-realtime price updates for trade plans.
- Add backend monitoring for trade plans so planned trades can be checked even when the browser is closed.
- Consider SignalR for live frontend updates after backend monitoring exists.
- Improve stock search with a local instrument master or fallback search strategy.
- Track Angel One JWT and refresh token expiry times in broker session storage.
- Research an API/source for identifying Islamically allowed/Shariah-compliant stocks in India.
- Add chunked historical candle fetching if charts need more than the current single SmartAPI candle request can reliably return.
- Fix local GitHub remote/auth before pushing workflow changes from this machine: point `origin` to `https://github.com/shahzad285/StockTrading.git` and use GitHub credentials with workflow permission.

## Done

- Added Indian stock fundamental data support.
- Added fundamentals data fields including market cap, P/E ratio, dividend yield, and other profile metrics.
- Updated fundamentals polling to process incomplete profiles for any stock in the `stocks` table.
- Added stock search API with NSE/BSE only and NSE as default.
- Added stock search to Stock Master and Trade Plan forms.
- Moved duplicate stock search endpoints to common endpoint `GET /Common/StockSearch`.
- Moved shared stock chart endpoint to common endpoint `GET /Common/StockChart`.
- Removed duplicate stock search endpoints from Stock and Trade Plan controllers.
- Updated Stock and Trade Plan UI search calls to use `/Common/StockSearch`.
- Made stock identity fields view-only after selecting a search result.
- Added stock chart popup using Angel One historical candles.
- Confirmed current chart uses SmartAPI historical candles and renders a closing-price line chart.
- Confirmed chart ranges currently use `FIVE_MINUTE` for 1D, `THIRTY_MINUTE` for 1W, and `ONE_DAY` for 1M/6M/1Y.
- Added chart-derived open, high, low, and close below the stock chart.
- Added 52-week high and 52-week low below the stock chart using one-year candles.
- Added chart access from Stock Master list rows.
- Improved stock chart readability and hover details.
- Made chart availability decision for Trade Plan rows.
- Added 1-day app login JWT expiry.
- Removed the Watchlist workflow and kept Stock Master plus Trade Plan as the active stock workflows.

## Current Notes

- Background jobs currently registered: `StockPricePollingWorker` and `StockFundamentalsPollingWorker`.
- `StockPricePollingWorker` refreshes saved Stock Master prices in Angel One quote batches of up to 50 instruments.
- `StockFundamentalsPollingWorker` now selects from the full `stocks` table when profile data is missing or incomplete.
- Trade plans can be created, listed, updated, and deleted, but backend trade-plan price checking/execution is not implemented yet.
- SmartAPI historical candle calls should be treated as limited-result requests; 1-minute candles likely need small date chunks.
