using Microsoft.AspNetCore.Mvc;
using StockTrading.IServices;
using StockTrading.Models;

namespace StockTrading.Controllers;

[ApiController]
[Route("[controller]")]
public class TradePlanController : ControllerBase
{
    private readonly ITradePlanService _tradePlanService;

    public TradePlanController(ITradePlanService tradePlanService)
    {
        _tradePlanService = tradePlanService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _tradePlanService.GetAsync(page, pageSize, HttpContext.RequestAborted);
        return Ok(new
        {
            tradePlans = result.Items,
            result.TotalCount,
            result.Page,
            result.PageSize
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save(TradePlan tradePlan)
    {
        try
        {
            var savedTradePlan = await _tradePlanService.SaveAsync(tradePlan, HttpContext.RequestAborted);
            return Ok(new { tradePlan = savedTradePlan });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _tradePlanService.DeleteAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }
}
