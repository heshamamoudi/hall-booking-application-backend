using Microsoft.AspNetCore.Authorization;

namespace HallApp.Web.Extensions
{
    public static class EndpointExtensions
    {
        public static void ConfigureEndpoints(this WebApplication app)
        {
            // Development-only endpoints
            if (app.Environment.IsDevelopment())
            {
                app.MapGet("/test", () => "API is working!");
                
                app.MapGet("/debug", async (HttpContext context) => 
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync("<html><body><h1>API Debug Page</h1><p>If you can see this, your API is accessible from browsers.</p></body></html>");
                }).WithMetadata(new AllowAnonymousAttribute());
            }

            // Map controllers and SignalR hub
            app.MapControllers();
            app.MapHub<HallApp.Web.Hubs.NotificationHub>("/notificationsHub")
                .RequireAuthorization(); // Ensure hub requires authentication

            // Handle 404s for API routes
            app.MapGet("api/{**catchAll}", (string catchAll, ILoggerFactory loggerFactory) => 
            {
                var logger = loggerFactory.CreateLogger("API.NotFound");
                logger.LogWarning("Unknown API route: api/{Path}", catchAll);
                
                return Results.NotFound(new { statusCode = 404, message = $"API endpoint 'api/{catchAll}' not found" });
            }).WithDisplayName("API 404 Handler");

            app.MapMethods("api/{**catchAll}", new[] { "POST", "PUT", "DELETE", "PATCH" }, (string catchAll, ILoggerFactory loggerFactory) => 
            {
                var logger = loggerFactory.CreateLogger("API.NotFound");
                logger.LogWarning("Unknown API route: api/{Path} with non-GET method", catchAll);
                
                return Results.NotFound(new { statusCode = 404, message = $"API endpoint 'api/{catchAll}' not found" });
            }).WithDisplayName("API 404 Handler (Non-GET methods)");

            // Fallback for SPA routes
            app.MapFallbackToController("CatchAll", "FallBack");
        }

        public static void ConfigureUrls(this WebApplication app, ILogger logger)
        {
            if (!app.Urls.Any())
            {
                app.Urls.Add("http://localhost:5236");
                app.Urls.Add("http://0.0.0.0:5236");
            }

            logger?.LogInformation("Server listening on: {Urls}", string.Join(", ", app.Urls));
        }
    }
}
