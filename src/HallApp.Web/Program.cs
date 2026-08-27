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

/* ------------------------------------------------------------- healthcheck --
 *
 * The runtime image is chiselled: no shell, no curl, no wget, and no package
 * manager to install one with. That is the point of it - the image went from
 * 420 MB to 254 MB and there is nothing left in it for a foothold to run - but
 * it also means the old `curl -fsS http://127.0.0.1:8080/health` HEALTHCHECK
 * has nothing to execute with. The probe therefore has to be this binary.
 *
 * This branch must stay ABOVE everything that builds a host. Without it the
 * flag is ignored and every probe starts a SECOND copy of the application:
 * it runs the migrations again, fails to bind a port the first copy already
 * holds, and aborts - so Docker reports the container unhealthy while the app
 * is serving perfectly.
 *
 * It matters more than it looks: the platform's agent treats a container with
 * no health state at all as merely `running`, so an app that quietly loses its
 * healthcheck also quietly loses its deploy gate and its rollback trigger.
 */
if (args.Contains("--healthcheck"))
{
    // Over loopback, so this answers "is this container serving HTTP right now"
    // rather than the much weaker "does a process exist". /health runs the
    // DbContext probe, so a database it cannot reach reports unhealthy.
    var probePort = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") is { Length: > 0 } pp
        ? pp.Split(';')[0]
        : "8080";
    using var probe = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
    try
    {
        var reply = await probe.GetAsync($"http://127.0.0.1:{probePort}/health");
        return reply.IsSuccessStatusCode ? 0 : 1;
    }
    catch
    {
        return 1;   // not answering yet, or not answering any more
    }
}

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

// Health checks. The database probe is what makes this a readiness signal rather
// than "the process started" - it is the difference between a deploy reporting
// success when the container is up and reporting success when it can serve.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DataContext>("database");

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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Contains("*"))
        {
            if (builder.Environment.IsDevelopment())
            {
                // Allow wildcard in development only
                policy.SetIsOriginAllowed(_ => true)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
            else
            {
                // Production: wildcard is insecure, require explicit origins
                throw new InvalidOperationException(
                    "SECURITY: Wildcard CORS (*) is not allowed in production. " +
                    "Set CORS__AllowedOrigins environment variable with specific domains."
                );
            }
        }
        else if (allowedOrigins.Any())
        {
            // Use configured origins from environment variable
            var origins = new List<string>();

            // In development, add localhost for convenience
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

            origins.AddRange(allowedOrigins);

            Console.WriteLine($"CORS configured with {allowedOrigins.Length} domain(s): {string.Join(", ", allowedOrigins)}");

            policy.WithOrigins(origins.ToArray())
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // No CORS configuration provided
            if (builder.Environment.IsDevelopment())
            {
                // Development: Allow localhost only
                Console.WriteLine("WARNING: No CORS__AllowedOrigins set - using localhost for development");
                policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
            else
            {
                // Production: FAIL LOUDLY - do not start without proper CORS configuration
                throw new InvalidOperationException(
                    "CRITICAL: CORS__AllowedOrigins environment variable is not set in production! " +
                    "The application cannot start without proper CORS configuration. " +
                    "Set CORS__AllowedOrigins=https://your-frontend-domain.com in the deployment environment."
                );
            }
        }
    });
});

// Log CORS configuration
var corsStatus = allowedOrigins.Any()
    ? $"CORS: {string.Join(", ", allowedOrigins)}"
    : builder.Environment.IsDevelopment()
        ? "CORS: Development mode (localhost only)"
        : "CORS: NOT CONFIGURED";
Console.WriteLine(corsStatus);

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

// The --healthcheck branch at the top of this file returns an exit code, which
// makes this program's entry point return int - so every path has to.
return 0;
