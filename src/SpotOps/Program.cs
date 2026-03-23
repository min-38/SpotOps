using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Features.Auth.Login;
using SpotOps.Features.Auth.Register;
using SpotOps.Features.Events.Add;
using SpotOps.Features.Events.Detail;
using SpotOps.Features.Events.List;
using SpotOps.Features.Events.Reserve;
using SpotOps.Features.Events.Queue;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddProblemDetails();
builder.Services.AddAuthorization();
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
builder.Services.AddSingleton<QueueService>();

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

app.Run();
