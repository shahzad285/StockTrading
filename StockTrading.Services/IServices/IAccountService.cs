using System.Security.Claims;
using StockTrading.Common.DTOs;
using StockTrading.Common.Enums;
using StockTrading.Models;

namespace StockTrading.IServices;

public interface IAccountService
{
    Task<AccountServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<AccountServiceResult<RequestLoginOtpResponse>> RequestLoginOtpAsync(RequestLoginOtpRequest request, CancellationToken cancellationToken = default);
    Task<AccountServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AccountServiceResult<MeResponse>> MeAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<AccountServiceResult<object>> SmartApiLoginAsync(SmartApiLoginRequest request, CancellationToken cancellationToken = default);
    Task<AccountServiceResult<AccountProfile>> GetProfileAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<AccountServiceResult<AccountBalanceResponse>> GetBalanceAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
}

public sealed record AccountServiceResult<T>(
    AccountServiceResultStatus Status,
    T? Value,
    string? Message = null,
    IReadOnlyList<string>? Errors = null,
    string? Code = null)
{
    public static AccountServiceResult<T> Ok(T value)
    {
        return new AccountServiceResult<T>(AccountServiceResultStatus.Ok, value);
    }

    public static AccountServiceResult<T> BadRequest(string message, IReadOnlyList<string>? errors = null)
    {
        return new AccountServiceResult<T>(AccountServiceResultStatus.BadRequest, default, message, errors);
    }

    public static AccountServiceResult<T> Unauthorized(string message)
    {
        return new AccountServiceResult<T>(AccountServiceResultStatus.Unauthorized, default, message);
    }

    public static AccountServiceResult<T> Forbidden()
    {
        return new AccountServiceResult<T>(AccountServiceResultStatus.Forbidden, default);
    }

    public static AccountServiceResult<T> ServiceUnavailable(string code, string message)
    {
        return new AccountServiceResult<T>(AccountServiceResultStatus.ServiceUnavailable, default, message, Code: code);
    }
}

public sealed record RegisterRequest(string Name, string? Email, string? PhoneNumber, string? Role);
public sealed record RegisterResponse(string Message, string Role);
public sealed record RequestLoginOtpRequest(LoginMethod LoginMethod, string? Email, string? PhoneNumber);
public sealed record RequestLoginOtpResponse(string Message, string? Otp, DateTime ExpiresAtUtc);
public sealed record LoginRequest(LoginMethod LoginMethod, string? Email, string? PhoneNumber, string? Otp, string? GoogleIdToken);
public sealed record LoginResponse(string Message, string Token, IReadOnlyList<string> Roles);
public sealed record MeResponse(int Id, string Name, string? Email, string? PhoneNumber, IReadOnlyList<string> Roles);
public sealed record SmartApiLoginRequest(string? Totp);
