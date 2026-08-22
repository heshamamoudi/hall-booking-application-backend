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

            // Liveness/readiness probe. Unauthenticated and deliberately terse: the
            // platform and Docker poll it every 30s, and the body says nothing beyond
            // "Healthy" / "Unhealthy". Already excluded from rate limiting.
            app.MapHealthChecks("/health").WithMetadata(new AllowAnonymousAttribute());

            // Map controllers and SignalR hubs
            app.MapControllers();
            app.MapHub<HallApp.Web.Hubs.NotificationHub>("/notificationsHub")
                .RequireAuthorization(); // Ensure hub requires authentication
            app.MapHub<HallApp.Web.Hubs.ChatHub>("/chatHub")
                .RequireAuthorization(); // Ensure hub requires authentication

            // Handle 404s for unmatched API routes using MapFallback.
            // IMPORTANT: Do NOT use MapGet/MapMethods with "api/{**catchAll}" patterns here.
            // Catch-all minimal API endpoints compete with MVC controller routes in ASP.NET Core
            // endpoint routing, which can cause legitimate controller endpoints (especially those
            // with [Consumes] constraints like multipart/form-data) to be shadowed by the catch-all.
            // MapFallback has the lowest route priority and only runs when no other endpoint matches.
            app.MapFallback(async (HttpContext context) =>
            {
                var path = context.Request.Path.Value ?? "";
                if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                {
                    var logger = context.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("API.NotFound");
                    logger.LogWarning("Unknown API route: {Method} {Path}", context.Request.Method, path);

                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        statusCode = 404,
                        message = $"API endpoint '{path}' not found"
                    });
                }
                else
                {
                    // Non-API routes: return 404 since frontend is deployed separately
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        statusCode = 404,
                        message = "Frontend is deployed separately. API-only backend."
                    });
                }
            });
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
