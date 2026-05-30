using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StockTrading.Common.DTOs;
using StockTrading.Common.Enums;
using StockTrading.Common.Settings;
using StockTrading.IServices;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Services;

public sealed class AccountService(
    IConfiguration configuration,
    IBrokerService brokerService,
    IAppJwtService jwtService,
    IApplicationUserRepository userRepository,
    IApplicationRoleRepository roleRepository,
    IApplicationOtpRepository otpRepository,
    IOtpDeliveryService otpDeliveryService,
    IOptions<OtpSettings> otpSettings) : IAccountService
{
    private readonly OtpSettings _otpSettings = otpSettings.Value;

    public async Task<AccountServiceResult<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateUser(request);
        if (validationErrors.Count > 0)
        {
            return AccountServiceResult<RegisterResponse>.BadRequest("Registration failed", validationErrors);
        }

        var hasUsers = await userRepository.AnyAsync(cancellationToken);
        if (hasUsers && !user.IsInRole(ApplicationRoleNames.SuperAdmin))
        {
            return AccountServiceResult<RegisterResponse>.Forbidden();
        }

        var isConfiguredSuperAdmin = IsConfiguredSuperAdminEmail(request.Email);
        if (!hasUsers && !isConfiguredSuperAdmin)
        {
            return AccountServiceResult<RegisterResponse>.Unauthorized("Only the configured superadmin can create the first user.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var existingEmailUser = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (existingEmailUser != null)
            {
                return AccountServiceResult<RegisterResponse>.BadRequest("Registration failed", ["Email already exists"]);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var existingPhoneUser = await userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
            if (existingPhoneUser != null)
            {
                return AccountServiceResult<RegisterResponse>.BadRequest("Registration failed", ["Phone number already exists"]);
            }
        }

        var roleName = isConfiguredSuperAdmin
            ? ApplicationRoleNames.SuperAdmin
            : ApplicationRoleNames.User;

        var applicationUser = new ApplicationUser
        {
            Name = request.Name.Trim(),
            Email = request.Email?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim()
        };

        await roleRepository.EnsureRolesAsync([ApplicationRoleNames.SuperAdmin, ApplicationRoleNames.User], cancellationToken);
        await userRepository.AddAsync(applicationUser, cancellationToken);
        await roleRepository.AddUserToRoleAsync(applicationUser.Id, roleName, cancellationToken);

        return AccountServiceResult<RegisterResponse>.Ok(new RegisterResponse("Registration successful", roleName));
    }

    public async Task<AccountServiceResult<RequestLoginOtpResponse>> RequestLoginOtpAsync(
        RequestLoginOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserForOtpLoginAsync(request.LoginMethod, request.Email, request.PhoneNumber, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return AccountServiceResult<RequestLoginOtpResponse>.Unauthorized("Invalid login details");
        }

        var otp = GenerateOtp();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(Math.Max(1, _otpSettings.ExpiryMinutes));
        var deliveryResult = await otpDeliveryService.SendLoginOtpAsync(
            user,
            request.LoginMethod,
            otp,
            expiresAtUtc,
            cancellationToken);

        if (!deliveryResult.IsSuccess)
        {
            return AccountServiceResult<RequestLoginOtpResponse>.ServiceUnavailable(
                "OTP_DELIVERY_FAILED",
                deliveryResult.ErrorMessage ?? "Failed to send OTP.");
        }

        await otpRepository.CreateAsync(user.Id, HashOtp(otp), expiresAtUtc, cancellationToken);

        return AccountServiceResult<RequestLoginOtpResponse>.Ok(new RequestLoginOtpResponse(
            "OTP sent",
            _otpSettings.ExposeOtpInResponse ? otp : null,
            expiresAtUtc));
    }

    public async Task<AccountServiceResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.LoginMethod == LoginMethod.GoogleOAuth)
        {
            return AccountServiceResult<LoginResponse>.BadRequest("Google OAuth login is not wired yet.");
        }

        if (string.IsNullOrWhiteSpace(request.Otp))
        {
            return AccountServiceResult<LoginResponse>.BadRequest("Login failed", ["OTP is required"]);
        }

        var user = await GetUserForOtpLoginAsync(request.LoginMethod, request.Email, request.PhoneNumber, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return AccountServiceResult<LoginResponse>.Unauthorized("Invalid login details or OTP");
        }

        var nowUtc = DateTime.UtcNow;
        var otpId = await otpRepository.GetValidOtpIdAsync(user.Id, HashOtp(request.Otp), nowUtc, cancellationToken);
        if (otpId == null)
        {
            return AccountServiceResult<LoginResponse>.Unauthorized("Invalid mobile number or OTP");
        }

        await otpRepository.MarkConsumedAsync(otpId.Value, nowUtc, cancellationToken);

        var roles = GetEffectiveRoles(user);
        var token = jwtService.CreateToken(user, roles);

        return AccountServiceResult<LoginResponse>.Ok(new LoginResponse("Login successful", token, roles));
    }

    public async Task<AccountServiceResult<MeResponse>> MeAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userId, out var parsedUserId))
        {
            return AccountServiceResult<MeResponse>.Unauthorized("User not found");
        }

        var applicationUser = await userRepository.GetByIdAsync(parsedUserId, cancellationToken);
        if (applicationUser == null)
        {
            return AccountServiceResult<MeResponse>.Unauthorized("User not found");
        }

        var roles = GetEffectiveRoles(applicationUser);
        return AccountServiceResult<MeResponse>.Ok(new MeResponse(
            applicationUser.Id,
            applicationUser.Name,
            applicationUser.Email,
            applicationUser.PhoneNumber,
            roles));
    }

    public async Task<AccountServiceResult<object>> SmartApiLoginAsync(
        SmartApiLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var isConnected = await brokerService.LoginAsync(request.Totp);
        return isConnected
            ? AccountServiceResult<object>.Ok(new { message = "Broker login successful" })
            : AccountServiceResult<object>.BadRequest("Broker login failed");
    }

    public async Task<AccountServiceResult<AccountProfile>> GetProfileAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await brokerService.GetProfileAsync();
            if (profile != null)
            {
                return AccountServiceResult<AccountProfile>.Ok(profile);
            }

            var message = user.IsInRole(ApplicationRoleNames.SuperAdmin)
                ? "Broker session expired. Please login to SmartAPI again using TOTP."
                : "Broker session expired. Please contact admin.";

            return AccountServiceResult<AccountProfile>.ServiceUnavailable("BROKER_AUTH_FAILED", message);
        }
        catch (Exception ex)
        {
            return AccountServiceResult<AccountProfile>.BadRequest("Failed to retrieve profile", [ex.Message]);
        }
    }

    public async Task<AccountServiceResult<AccountBalanceResponse>> GetBalanceAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var balance = await brokerService.GetAccountBalanceAsync();
            if (balance != null)
            {
                return AccountServiceResult<AccountBalanceResponse>.Ok(balance);
            }

            var message = user.IsInRole(ApplicationRoleNames.SuperAdmin)
                ? "Broker session expired. Please login to SmartAPI again using TOTP."
                : "Broker session expired. Please contact admin.";

            return AccountServiceResult<AccountBalanceResponse>.ServiceUnavailable("BROKER_AUTH_FAILED", message);
        }
        catch (Exception ex)
        {
            return AccountServiceResult<AccountBalanceResponse>.BadRequest("Failed to retrieve account balance", [ex.Message]);
        }
    }

    private static List<string> ValidateUser(RegisterRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Name is required");
        }

        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            errors.Add("Email or phone number is required");
        }

        return errors;
    }

    private async Task<ApplicationUser?> GetUserForOtpLoginAsync(
        LoginMethod loginMethod,
        string? email,
        string? phoneNumber,
        CancellationToken cancellationToken)
    {
        if (loginMethod == LoginMethod.EmailOtp)
        {
            return string.IsNullOrWhiteSpace(email)
                ? null
                : await userRepository.GetByEmailAsync(email, cancellationToken);
        }

        if (loginMethod == LoginMethod.PhoneOtp)
        {
            return string.IsNullOrWhiteSpace(phoneNumber)
                ? null
                : await userRepository.GetByPhoneNumberAsync(phoneNumber, cancellationToken);
        }

        return null;
    }

    private bool IsConfiguredSuperAdminEmail(string? email)
    {
        var superAdminEmail = configuration["Auth:SuperAdminEmail"];
        return !string.IsNullOrWhiteSpace(email)
            && !string.IsNullOrWhiteSpace(superAdminEmail)
            && string.Equals(email.Trim(), superAdminEmail.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyList<string> GetEffectiveRoles(ApplicationUser user)
    {
        return IsConfiguredSuperAdminEmail(user.Email)
            ? [ApplicationRoleNames.SuperAdmin]
            : [ApplicationRoleNames.User];
    }

    private static string GenerateOtp()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    }

    private static string HashOtp(string otp)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(otp.Trim()));
        return Convert.ToHexString(bytes);
    }
}
