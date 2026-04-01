// System
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

// Microsoft Extensions
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// Data
using SpotOps.Data;

// Features
using SpotOps.Features.Auth;
using SpotOps.Features.Auth.JWT;
// using SpotOps.Features.Events.Detail;
// using SpotOps.Features.Events.List;
// using SpotOps.Features.Events.Reserve;
// using SpotOps.Features.Events.Queue;
// using SpotOps.Features.Events.Selection;
// using SpotOps.Features.Payments;
// using SpotOps.Features.Me.Reservations;
// using SpotOps.Features.Me.Profile;

// Infrastructure
using SpotOps.Infrastructure.PortOne;
using SpotOps.Infrastructure.Redis;
using SpotOps.Infrastructure.Sms;
using SpotOps.Infrastructure.Email;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddProblemDetails();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "SpotOps API",
        Version = "v1"
    });
});

var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "spotops";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "spotops-client";
var jwtSecret = builder.Configuration["JWT_SECRET"];
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException("JWT_SECRET is required.");
if (jwtSecret.Length < 32)
    throw new InvalidOperationException("JWT_SECRET must be at least 32 characters.");

builder.Services.AddJwtFeature(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        if (corsOrigins.Length == 0)
            policy.SetIsOriginAllowed(_ => false);
        else
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

// Service 파일 등록
// builder.Services.AddScoped<ListEventsService>();
// builder.Services.AddScoped<EventDetailService>();
builder.Services.AddAuthFeature();
// builder.Services.AddScoped<ReserveService>();
// builder.Services.AddScoped<SelectionService>();
// builder.Services.AddScoped<PaymentService>();
// builder.Services.AddSingleton<QueueService>();
// builder.Services.AddScoped<MyReservationsService>();
// builder.Services.AddScoped<MyProfileService>();
// builder.Services.AddScoped<PhoneVerificationService>();
builder.Services.AddSingleton<ISmsSender, LoggingSmsSender>();
builder.Services.AddSingleton(sp =>
{
    var c = builder.Configuration;
    return new SmtpOptions
    {
        Host = c["SMTP_HOST"] ?? "localhost",
        Port = int.TryParse(c["SMTP_PORT"], out var p) ? p : 587,
        FromEmail = c["SMTP_FROM_EMAIL"] ?? "noreply@example.com",
        FromName = c["SMTP_FROM_NAME"] ?? "SpotOps",
        Username = c["SMTP_USERNAME"],
        Password = c["SMTP_PASSWORD"],
        EnableSsl = bool.TryParse(c["SMTP_ENABLE_SSL"], out var ssl) && ssl
    };
});
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

builder.Services.AddOptions<PortOneOptions>()
    .Configure<IConfiguration>((o, c) =>
    {
        o.ApiSecret = c["PORTONE:API_SECRET"] ?? "";
        o.StoreId = c["PORTONE:STORE_ID"] ?? "";
        o.WebhookSecret = c["PORTONE:WEBHOOK_SECRET"] ?? "";
        o.ApiBaseUrl = c["PORTONE:API_BASE_URL"] ?? "";
        o.VerifyChannelId = c["PORTONE:VERIFY_CHANNEL_ID"] ?? "";
        o.PaymentChannelId = c["PORTONE:PAYMENT_CHANNEL_ID"] ?? "";
    });

builder.Services.AddHttpClient("PortOne", (sp, client) =>
{
    var opt = sp.GetRequiredService<IOptions<PortOneOptions>>().Value;
    client.BaseAddress = new Uri(opt.ApiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<IPortOnePaymentApi, PortOnePaymentApiClient>();
builder.Services.AddSingleton<IPortOneIvVerifyService, PortOneIvVerifyService>();

// Redis 연결 (singleton으로 등록하여 애플리케이션 전체에서 공유)
// RedisOptions 등록
builder.Services.AddSingleton(_ =>
{
    var c = builder.Configuration;
    return new RedisOptions
    {
        Host = c["REDIS_HOST"] ?? "localhost",
        Port = c.GetValue("REDIS_PORT", 6379),
        Password = c["REDIS_PASSWORD"] ?? "",
        Db = c.GetValue("REDIS_DB", 0),
        KeyPrefix = c["REDIS_KEY_PREFIX"] ?? "spotops",
        DefaultTtlHours = c.GetValue("REDIS_DEFAULT_TTL_HOURS", 24)
    };
});

builder.Services.AddSingleton(sp =>
{
    var redis = sp.GetRequiredService<RedisOptions>();
    return new RedisKeyBuilder(redis.KeyPrefix);
});

// IConnectionMultiplexer 등록 (Options 사용)
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redis = sp.GetRequiredService<RedisOptions>();
    var options = new ConfigurationOptions
    {
        AbortOnConnectFail = false,
        DefaultDatabase = redis.Db,
        ConnectTimeout = 5000
    };
    options.EndPoints.Add(redis.Host, redis.Port);
    if (!string.IsNullOrWhiteSpace(redis.Password))
        options.Password = redis.Password;
    return ConnectionMultiplexer.Connect(options);
});

builder.Services.AddScoped<IDatabase>(sp =>
{
    var mux = sp.GetRequiredService<IConnectionMultiplexer>();
    var redis = sp.GetRequiredService<RedisOptions>();
    return mux.GetDatabase(redis.Db);
});

var host = builder.Configuration["DATABASE_HOST"];
var port = builder.Configuration["DATABASE_PORT"];
var db = builder.Configuration["DATABASE_NAME"];
var user = builder.Configuration["DATABASE_USER"];
var password = builder.Configuration["DATABASE_PASSWORD"];
var connStr = $"Host={host};Port={port};Database={db};Username={user};Password={password}";

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connStr));

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler();

app.UseStatusCodePages();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SpotOps API v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapGet("/api/health", () => Results.Json(new { status = "ok" }))
    .WithTags("Health")
    .AllowAnonymous();

// Endpoint 파일 등록
AuthEndpoints.Map(app);
// ListEndpoint.Map(app);
// DetailEndpoint.Map(app);
// ReserveEndpoint.Map(app);
// QueueEndpoint.Map(app);
// SelectionEndpoint.Map(app);
// PaymentEndpoint.Map(app);
// MyReservationsEndpoint.Map(app);
// MyProfileEndpoint.Map(app);

app.Run();
