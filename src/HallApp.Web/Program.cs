using HallApp.Infrastructure;
using HallApp.Application;
using HallApp.Web.Extensions;
using HallApp.Core.Entities;
using HallApp.Infrastructure.Data;
using HallApp.Infrastructure.Data.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections;

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

// Configure Kestrel to listen on Railway's port
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

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
