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
using HallApp.Web.Validators;

// FORCE RAILWAY REBUILD - All SQL Server type fixes applied: commit a79620b
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

// Register FluentValidation validators from Application and Web assemblies
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<HallUpdateWithFilesDtoValidator>();

// Configure consistent validation error responses for [ApiController] auto-validation
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        var response = new
        {
            statusCode = 400,
            message = "One or more validation errors occurred.",
            isSuccess = false,
            errors,
            timestamp = DateTime.UtcNow
        };

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// Configure CORS for SignalR and API calls
var allowedOriginsConfig = builder.Configuration["CORS:AllowedOrigins"]?.Trim() ?? "";
var allowedOrigins = allowedOriginsConfig.Split(',', StringSplitOptions.RemoveEmptyEntries)
    .Select(o => o.Trim())
    .ToArray();

// BUGFIX: Add known production domains if CORS env var is not set
var knownProductionDomains = new[]
{
    "https://keen-lokum-f3d666.netlify.app",  // Current production frontend
    "https://zawaji-app.netlify.app",
    "https://zawaji.netlify.app",
    "https://hall-frontend.netlify.app",
    "http://localhost:4200",  // Local development
    // Add future production domains here
};

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Contains("*"))
        {
            if (builder.Environment.IsDevelopment())
            {
                // Only allow wildcard origins in development
                policy.SetIsOriginAllowed(_ => true)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
            else
            {
                // In production, wildcard with credentials is a security risk
                // Fall back to known production domains
                Console.WriteLine("⚠️  WARNING: Wildcard CORS in production - using known domains fallback");
                policy.WithOrigins(knownProductionDomains)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
        }
        else if (allowedOrigins.Any())
        {
            // Specific origins mode - use configured origins plus localhost for dev
            var origins = new List<string>();

            if (builder.Environment.IsDevelopment())
            {
                origins.AddRange(new[]
                {
                    "http://localhost:4200",
                    "https://localhost:4200",
                    "http://localhost:5235",
                    "http://127.0.0.1:4200",
                    "https://127.0.0.1:4200",
                    "http://127.0.0.1:5235"
                });
            }

            // Add configured origins (from CORS__AllowedOrigins env var)
            origins.AddRange(allowedOrigins);

            policy.WithOrigins(origins.ToArray())
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // No CORS origins configured - use defaults
            // BUGFIX: Include known production domains to prevent HTTP 0 errors
            var defaultOrigins = new List<string>
            {
                "http://localhost:4200",
                "https://localhost:4200"
            };

            // Add production domains in production environment
            if (!builder.Environment.IsDevelopment())
            {
                defaultOrigins.AddRange(knownProductionDomains);
                Console.WriteLine($"⚠️  WARNING: No CORS__AllowedOrigins configured. Using fallback domains: {string.Join(", ", knownProductionDomains)}");
                Console.WriteLine("💡 TIP: Set CORS__AllowedOrigins environment variable for production");
            }

            policy.WithOrigins(defaultOrigins.ToArray())
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Log CORS configuration for debugging
var corsOriginsForLogging = allowedOrigins.Any()
    ? string.Join(", ", allowedOrigins)
    : builder.Environment.IsDevelopment()
        ? "localhost:4200"
        : string.Join(", ", knownProductionDomains);
Console.WriteLine($"🌐 CORS configured for origins: {corsOriginsForLogging}");

var app = builder.Build();

// Configure middleware pipeline using extension
app.ConfigureMiddlewarePipeline();

// Configure endpoints using extension
app.ConfigureEndpoints();

// CRITICAL: Run migrations BEFORE starting the app.
// Data Protection is configured with PersistKeysToDbContext, which requires the
// DataProtectionKeys table to exist. Migrations must complete first.
var logger = app.Services.GetService<ILogger<Program>>();
logger?.LogInformation("Running database migrations before startup...");

try
{
    await app.Services.SetupDatabaseAsync();
    logger?.LogInformation("Database migrations completed successfully");
}
catch (Exception ex)
{
    logger?.LogCritical(ex, "Failed to run database migrations - application cannot start");
    throw; // Fail fast: do not start with a broken database
}

logger?.LogInformation("Starting web server...");
app.Run();
