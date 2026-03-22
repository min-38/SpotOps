using Microsoft.EntityFrameworkCore;
using SpotOps.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// DB 연결
var host = builder.Configuration["DATABASE_HOST"];
var port = builder.Configuration["DATABASE_PORT"];
var db = builder.Configuration["DATABASE_NAME"];
var user = builder.Configuration["DATABASE_USER"];
var password = builder.Configuration["DATABASE_PASSWORD"];
var connStr = $"Host={host};Port={port};Database={db};Username={user};Password={password}";

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connStr));

// 인증
builder.Services.AddAuthentication("Cookies")
    .AddCookie(opt => {
        opt.LoginPath = "/login";
        opt.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
