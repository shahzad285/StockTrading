using StockTrading.Services;
using StockTrading.IServices;
using StockTrading.Data;
using StockTrading.Models;
using StockTrading.Repository.IRepository;
using StockTrading.Repository.Repository;
using StockTrading.Apis.Authentication;
using StockTrading.Common.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("StockTradingWeb", policy =>
    {
        policy
            .WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT token returned by /Account/login. Do not include Bearer."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddStockTradingData(builder.Configuration);
builder.Services.Configure<OtpSettings>(builder.Configuration.GetSection("Auth:Otp"));
builder.Services.Configure<EmailOtpSettings>(builder.Configuration.GetSection("Auth:Otp:Email"));
builder.Services.Configure<BrevoSettings>(builder.Configuration.GetSection("Auth:Otp:Email:Brevo"));
builder.Services.Configure<SendGridSettings>(builder.Configuration.GetSection("Auth:Otp:Email:SendGrid"));
builder.Services.Configure<TwilioSettings>(builder.Configuration.GetSection("Auth:Otp:Mobile:Twilio"));
builder.Services.Configure<StockPollingSettings>(builder.Configuration.GetSection("StockPolling"));
builder.Services.Configure<FundamentalsPollingSettings>(builder.Configuration.GetSection("FundamentalsPolling"));
builder.Services.Configure<MarketScheduleSettings>(builder.Configuration.GetSection("MarketSchedule"));
builder.Services.Configure<TapetideSettings>(builder.Configuration.GetSection("Tapetide"));
builder.Services.Configure<YahooFinanceSettings>(builder.Configuration.GetSection("YahooFinance"));
builder.Services.Configure<NseIndiaSettings>(builder.Configuration.GetSection("NseIndia"));

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecretKey))
{
    throw new InvalidOperationException("Jwt:SecretKey is required.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "StockTrading.Apis",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "StockTrading.Client",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ApplicationRoleNames.SuperAdmin, policy =>
        policy.RequireRole(ApplicationRoleNames.SuperAdmin));

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Add our services
builder.Services.AddSingleton<IAppJwtService, AppJwtService>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
builder.Services.AddScoped<IApplicationRoleRepository, ApplicationRoleRepository>();
builder.Services.AddScoped<IApplicationOtpRepository, ApplicationOtpRepository>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ITradePlanRepository, TradePlanRepository>();
builder.Services.AddScoped<IStockProfileRepository, StockProfileRepository>();
builder.Services.AddScoped<IMarketJobDecisionRepository, MarketJobDecisionRepository>();
builder.Services.AddScoped<IBrokerSessionRepository, BrokerSessionRepository>();
builder.Services.AddScoped<IOtpDeliveryService, OtpDeliveryService>();
builder.Services.AddHttpClient<SendGridEmailOtpSender>(client =>
{
    client.BaseAddress = new Uri("https://api.sendgrid.com/");
});
builder.Services.AddHttpClient<BrevoEmailOtpSender>(client =>
{
    client.BaseAddress = new Uri("https://api.brevo.com/");
});
builder.Services.AddScoped<IEmailOtpSender>(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var activeProvider = config["Auth:Otp:Email:Provider"] ?? "Brevo";

    return activeProvider.Equals("Brevo", StringComparison.OrdinalIgnoreCase)
        ? serviceProvider.GetRequiredService<BrevoEmailOtpSender>()
        : activeProvider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase)
            ? serviceProvider.GetRequiredService<SendGridEmailOtpSender>()
            : throw new InvalidOperationException($"Unsupported email OTP provider '{activeProvider}'.");
});
builder.Services.AddHttpClient<IMobileOtpSender, TwilioMobileOtpSender>(client =>
{
    client.BaseAddress = new Uri("https://api.twilio.com/");
});
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ITradePlanService, TradePlanService>();
builder.Services.AddScoped<IStockFundamentalsService, StockFundamentalsService>();
builder.Services.AddScoped<IMarketScheduleService, MarketScheduleService>();
builder.Services.AddHostedService<StockPricePollingWorker>();
builder.Services.AddHostedService<StockFundamentalsPollingWorker>();
builder.Services.AddScoped<IStringEncryptionService, AesStringEncryptionService>();
builder.Services.AddScoped<IBrokerSessionStore, BrokerSessionStore>();
builder.Services.AddHttpClient<ITapetideFundamentalsService, TapetideFundamentalsService>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TapetideSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
});
builder.Services.AddHttpClient<IYahooFinanceFundamentalsService, YahooFinanceFundamentalsService>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<YahooFinanceSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
    CookieContainer = new CookieContainer(),
    UseCookies = true
});
builder.Services.AddHttpClient<INseIndiaService, NseIndiaService>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<NseIndiaSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
    CookieContainer = new CookieContainer(),
    UseCookies = true
});
builder.Services.AddHttpClient<AngelOneService>();
builder.Services.AddScoped<IBrokerService>(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var activeBroker = config["Broker:Active"] ?? "AngelOne";

    return activeBroker.Equals("AngelOne", StringComparison.OrdinalIgnoreCase)
        ? serviceProvider.GetRequiredService<AngelOneService>()
        : throw new InvalidOperationException($"Unsupported broker '{activeBroker}'.");
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var databaseInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
    await databaseInitializer.InitializeAsync();

    var roleRepository = scope.ServiceProvider.GetRequiredService<IApplicationRoleRepository>();
    await roleRepository.EnsureRolesAsync([ApplicationRoleNames.SuperAdmin, ApplicationRoleNames.User]);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("StockTradingWeb");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
