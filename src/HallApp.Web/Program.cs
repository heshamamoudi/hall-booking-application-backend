using HallApp.Infrastructure;
using HallApp.Application;
using HallApp.Web.Extensions;
using HallApp.Core.Entities;
using HallApp.Infrastructure.Data;
using HallApp.Infrastructure.Data.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure logging to prevent duplicates
builder.Logging.ClearProviders();
builder.Host.AddSecureLogging();

// Configure Kestrel security
builder.ConfigureKestrelSecurity();

// Add services to the container
builder.Services.AddMemoryCache();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddSecurityServices(builder.Configuration, builder.Environment);
builder.Services.AddCachingServices(builder.Configuration);
builder.Services.AddApiRateLimiting();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddSignalR();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
var env = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
var config = builder.Configuration.GetConnectionString("DefaultConnection");

Console.WriteLine($"ENV: {env}");
Console.WriteLine($"CONFIG: {config}");

var app = builder.Build();

// Configure middleware pipeline using extension
app.ConfigureMiddlewarePipeline();

// Configure endpoints using extension
app.ConfigureEndpoints();

// Database setup using extension
await app.Services.SetupDatabaseAsync();

// Configure URLs
//if (!app.Urls.Any())
//{
//    app.Urls.Add("http://localhost:5236");
//    app.Urls.Add("http://0.0.0.0:5236");
//}

// Log startup information

var logger = app.Services.GetService<ILogger<Program>>();
logger?.LogInformation("Server starting on: {Urls}", string.Join(", ", app.Urls));

app.Run();
