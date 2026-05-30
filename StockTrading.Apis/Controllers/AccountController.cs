using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockTrading.Common.Enums;
using StockTrading.IServices;
using StockTrading.Models;

namespace StockTrading.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _accountService.RegisterAsync(request, User, HttpContext.RequestAborted);
        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("login/request-otp")]
    public async Task<IActionResult> RequestLoginOtp(RequestLoginOtpRequest request)
    {
        var result = await _accountService.RequestLoginOtpAsync(request, HttpContext.RequestAborted);
        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _accountService.LoginAsync(request, HttpContext.RequestAborted);
        return ToActionResult(result);
    }

    [Authorize(Roles = ApplicationRoleNames.SuperAdmin)]
    [HttpPost("smartapi/login")]
    public async Task<IActionResult> SmartApiLogin(SmartApiLoginRequest request)
    {
        var result = await _accountService.SmartApiLoginAsync(request, HttpContext.RequestAborted);
        return ToActionResult(result);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var result = await _accountService.GetProfileAsync(User, HttpContext.RequestAborted);
        return ToActionResult(result, value => new { profile = value });
    }

    [HttpGet("balance")]
    public async Task<IActionResult> Balance()
    {
        var result = await _accountService.GetBalanceAsync(User, HttpContext.RequestAborted);
        return ToActionResult(result, value => new { balance = value });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var result = await _accountService.MeAsync(User, HttpContext.RequestAborted);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(AccountServiceResult<T> result, Func<T, object>? okValue = null)
    {
        return result.Status switch
        {
            AccountServiceResultStatus.Ok => Ok(okValue == null ? result.Value : okValue(result.Value!)),
            AccountServiceResultStatus.BadRequest => BadRequest(new { message = result.Message, errors = result.Errors }),
            AccountServiceResultStatus.Unauthorized => Unauthorized(new { message = result.Message }),
            AccountServiceResultStatus.Forbidden => Forbid(),
            AccountServiceResultStatus.ServiceUnavailable => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { code = result.Code, message = result.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
