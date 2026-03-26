// System
using System.Text.Json;
using System.Text.Json.Serialization;

// Microsoft Extensions
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;

// Data
using SpotOps.Data;

// Features
using SpotOps.Features.Auth.Login;
using SpotOps.Features.Auth.Register;
using SpotOps.Features.Events.Add;
using SpotOps.Features.Events.Detail;
using SpotOps.Features.Events.List;
using SpotOps.Features.Events.Reserve;
using SpotOps.Features.Events.Queue;
using SpotOps.Features.Events.Selection;
using SpotOps.Features.Payments;

// Infrastructure
using SpotOps.Infrastructure.PortOne;
using SpotOps.Infrastructure.Redis;
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
builder.Services.AddRazorPages(options =>
{
    options.RootDirectory = "/Features";
});
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.PageViewLocationFormats.Add("/Features/Shared/{0}.cshtml");
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SpotOps.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            var returnUrl = Uri.EscapeDataString(context.Request.PathBase + context.Request.Path + context.Request.QueryString);
            context.Response.Redirect($"/auth/login?returnUrl={returnUrl}");
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect("/events");
            return Task.CompletedTask;
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
builder.Services.AddScoped<ListEventsService>();
builder.Services.AddScoped<EventDetailService>();
builder.Services.AddScoped<AddEventService>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<RegisterService>();
builder.Services.AddScoped<ReserveService>();
builder.Services.AddScoped<SelectionService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddSingleton<QueueService>();

builder.Services.AddOptions<PortOneOptions>()
    .Configure<IConfiguration>((o, c) =>
    {
        o.ApiSecret = c["PortOne:ApiSecret"] ?? c["PORTONE_SECRET"] ?? "";
        o.StoreId = c["PortOne:StoreId"] ?? c["PORTONE_STORE_ID"] ?? c["PORTONE_MID"] ?? "";
        o.WebhookSecret = c["PortOne:WebhookSecret"] ?? c["PORTONE_WEBHOOK_SECRET"] ?? "";
        o.ApiBaseUrl = c["PortOne:ApiBaseUrl"] ?? c["PORTONE_API_BASE_URL"] ?? "https://api.portone.io";
    });

builder.Services.AddHttpClient("PortOne", (sp, client) =>
{
    var opt = sp.GetRequiredService<IOptions<PortOneOptions>>().Value;
    client.BaseAddress = new Uri(opt.ApiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<IPortOnePaymentApi, PortOnePaymentApiClient>();

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
app.UseStaticFiles();
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

app.MapGet("/events/list", () => Results.Redirect("/events", permanent: false))
    .AllowAnonymous();

app.MapGet("/organizer/create-event", () => Results.Redirect("/events/add", permanent: false))
    .AllowAnonymous();

app.MapRazorPages();

app.MapGet("/api/health", () => Results.Json(new { status = "ok" }))
    .WithTags("Health")
    .AllowAnonymous();

// Endpoint 파일 등록
LoginEndpoint.Map(app);
RegisterEndpoint.Map(app);
ListEndpoint.Map(app);
DetailEndpoint.Map(app);
AddEventEndpoint.Map(app);
ReserveEndpoint.Map(app);
QueueEndpoint.Map(app);
SelectionEndpoint.Map(app);
PaymentEndpoint.Map(app);

app.Run();
