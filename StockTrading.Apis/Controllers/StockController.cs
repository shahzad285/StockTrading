using Microsoft.AspNetCore.Mvc;
using StockTrading.Common.DTOs;
using StockTrading.IServices;

namespace StockTrading.Controllers;

[ApiController]
[Route("[controller]")]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }

    [HttpGet("stocks")]
    public async Task<IActionResult> Stocks([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _stockService.GetStocksAsync(page, pageSize, HttpContext.RequestAborted);
        return Ok(new
        {
            stocks = result.Items,
            result.TotalCount,
            result.Page,
            result.PageSize
        });
    }

    [HttpPost("stocks")]
    public async Task<IActionResult> SaveStock(SaveStockRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Symbol))
        {
            return BadRequest(new { message = "Symbol is required." });
        }

        if (string.IsNullOrWhiteSpace(request.SymbolToken))
        {
            return BadRequest(new { message = "Symbol token is required." });
        }

        if (!Enum.IsDefined(request.Exchange))
        {
            return BadRequest(new { message = "Exchange must be NSE or BSE." });
        }

        var stock = await _stockService.SaveStockAsync(request, HttpContext.RequestAborted);
        return Ok(new { stock });
    }

    [HttpDelete("stocks/{stockId:int}")]
    public async Task<IActionResult> DeleteStock(int stockId)
    {
        var result = await _stockService.DeleteStockAsync(stockId, HttpContext.RequestAborted);
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return BadRequest(new { message = result.Message, dependencies = result.Dependencies });
    }

    [HttpGet("holdings")]
    public async Task<IActionResult> Holdings()
    {
        try
        {
            var holdings = await _stockService.GetHoldingsAsync(HttpContext.RequestAborted);
            return Ok(holdings);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to retrieve stocks", error = ex.Message });
        }
    }

    [HttpGet("prices")]
    public async Task<IActionResult> Prices()
    {
        try
        {
            var prices = await _stockService.GetConfiguredPricesAsync(HttpContext.RequestAborted);
            return Ok(new { prices });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to retrieve stock prices", error = ex.Message });
        }
    }
}
