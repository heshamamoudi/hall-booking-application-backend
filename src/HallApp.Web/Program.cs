using HallApp.Infrastructure;
using HallApp.Application;
using HallApp.Web.Extensions;
using HallApp.Core.Entities;
using HallApp.Infrastructure.Data;
using HallApp.Infrastructure.Data.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using FluentValidation.AspNetCore;
using HallApp.Application.Validators;

var builder = WebApplication.CreateBuilder(args);

// Configure logging to prevent duplicates
builder.Logging.ClearProviders();
builder.Host.AddSecureLogging();

// Configure Kestrel security
builder.ConfigureKestrelSecurity();

// Add services to the container
builder.Services.AddMemoryCache();
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddSecurityServices(builder.Configuration, builder.Environment);
builder.Services.AddCachingServices(builder.Configuration);
builder.Services.AddApiRateLimiting();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<HallApp.Web.Filters.ApiExceptionFilter>();
})
.AddJsonOptions(options =>
{
    // Use camelCase for JSON property names to match frontend expectations
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    // Allow case-insensitive deserialization (frontend sends camelCase, backend DTOs are PascalCase)
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

// Register FluentValidation validators
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

// Configure CORS for SignalR and API calls
var allowedOrigins = builder.Configuration["CORS:AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries)
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Default local development origins
        var origins = new List<string>
        {
            "http://localhost:4200",
            "https://localhost:4200",
            "http://localhost:5235",
            "http://127.0.0.1:4200",
            "https://127.0.0.1:4200",
            "http://127.0.0.1:5235"
        };

        // Add configured origins (from CORS__AllowedOrigins env var)
        origins.AddRange(allowedOrigins);

        policy.WithOrigins(origins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowedToAllowWildcardSubdomains();
    });
});

var port = Environment.GetEnvironmentVariable("PORT");

// Configure Kestrel to listen on Railway's port (Nixpacks deployment)
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}
// Remove hardcoded fallback - let --urls parameter work

var app = builder.Build();

// Configure middleware pipeline using extension
app.ConfigureMiddlewarePipeline();

// Configure endpoints using extension
app.ConfigureEndpoints();

// Configure URLs
if (!app.Urls.Any())
{
    app.Urls.Add("http://localhost:5235");
    app.Urls.Add("http://0.0.0.0:5235");
}

// Log startup information
var logger = app.Services.GetService<ILogger<Program>>();
logger?.LogInformation("Server starting on: {Urls}", string.Join(", ", app.Urls));

// Run database setup in background after server starts (non-blocking for health checks)
app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        try
        {
            await app.Services.SetupDatabaseAsync();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Background database setup failed");
        }
    });
});

app.Run();
