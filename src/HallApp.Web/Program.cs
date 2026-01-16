using HallApp.Infrastructure;
using HallApp.Application;
using HallApp.Web.Extensions;
using HallApp.Core.Entities;
using HallApp.Infrastructure.Data;
using HallApp.Infrastructure.Data.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections;
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
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddSecurityServices(builder.Configuration, builder.Environment);
builder.Services.AddCachingServices(builder.Configuration);
builder.Services.AddApiRateLimiting();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<HallApp.Web.Filters.ApiExceptionFilter>();
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
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200",
                "http://localhost:5235",
                "http://127.0.0.1:4200",
                 "https://127.0.0.1:4200",
                "http://127.0.0.1:5235"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowedToAllowWildcardSubdomains();
    });
});
// Debug environment variables for Railway deployment
var env = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
var config = builder.Configuration.GetConnectionString("DefaultConnection");
var port = Environment.GetEnvironmentVariable("PORT");
var jwtSecret = Environment.GetEnvironmentVariable("JWT__SecretKey");
var jwtSecretConfig = builder.Configuration["JWT:SecretKey"];

Console.WriteLine($"🔍 ENV ConnectionString: {env}");
Console.WriteLine($"🔍 CONFIG ConnectionString: {config}");
Console.WriteLine($"🔍 PORT: {port}");
Console.WriteLine($"🔍 JWT Secret ENV: {(string.IsNullOrEmpty(jwtSecret) ? "NULL/MISSING" : "PRESENT")}");
Console.WriteLine($"🔍 JWT Secret CONFIG: {(string.IsNullOrEmpty(jwtSecretConfig) ? "NULL/MISSING" : "PRESENT")}");

// List all environment variables starting with JWT
Console.WriteLine("🔍 All JWT Environment Variables:");
foreach (DictionaryEntry envVar in Environment.GetEnvironmentVariables())
{
    var key = envVar.Key.ToString();
    if (key.StartsWith("JWT"))
    {
        Console.WriteLine($"   {key} = {envVar.Value}");
    }
}

// List all configuration keys related to JWT
Console.WriteLine("🔍 Configuration JWT Values:");
Console.WriteLine($"   JWT:SecretKey = {(string.IsNullOrEmpty(builder.Configuration["JWT:SecretKey"]) ? "NULL/MISSING" : "PRESENT")}");
Console.WriteLine($"   JWT:Issuer = {builder.Configuration["JWT:Issuer"]}");
Console.WriteLine($"   JWT:Audience = {builder.Configuration["JWT:Audience"]}");
Console.WriteLine($"   JWT:ExpiryInHours = {builder.Configuration["JWT:ExpiryInHours"]}");

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

// Database setup using extension
await app.Services.SetupDatabaseAsync();

// Configure URLs
if (!app.Urls.Any())
{
    app.Urls.Add("http://localhost:5235");
    app.Urls.Add("http://0.0.0.0:5235");
}

// Log startup information

var logger = app.Services.GetService<ILogger<Program>>();
logger?.LogInformation("Server starting on: {Urls}", string.Join(", ", app.Urls));

app.Run();
