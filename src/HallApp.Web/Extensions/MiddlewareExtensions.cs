using HallApp.Web.Middleware;
using HallApp.Web.Middleware.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using System.Security.Authentication;

namespace HallApp.Web.Extensions
{
    public static class MiddlewareExtensions
    {
        public static void ConfigureKestrelSecurity(this WebApplicationBuilder builder)
        {
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ConfigureHttpsDefaults(options =>
                {
                    options.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                });
            });
        }

        public static void ConfigureMiddlewarePipeline(this WebApplication app)
        {
            // Forwarded headers - MUST be first for Railway/cloud proxies
            // Railway terminates TLS at the edge and forwards HTTP internally.
            // This ensures the app sees the original scheme (https) and client IP.
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            // Exception handling
            app.UseMiddleware<ExceptionMiddleware>();

            // Enable working security middleware
            app.UseMiddleware<SecurityMonitoringMiddleware>();
            app.UseMiddleware<SecurityHeadersMiddleware>();
            // app.UseMiddleware<AntiCsrfMiddleware>(); // Keep disabled - caused IAntiforgery startup issues
            app.UseMiddleware<InputValidationMiddleware>();
            app.UseMiddleware<ImageUploadMiddleware>();

            // Static files - serve from wwwroot
            app.UseStaticFiles();

            // Ensure uploads directory exists before configuring static files
            var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Serve uploaded files from wwwroot/uploads
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
                RequestPath = "/uploads"
            });

            // Swagger - configurable via SWAGGER__ENABLED env var (defaults to true in Development)
            var swaggerEnabled = app.Configuration["Swagger:Enabled"]
                ?? (app.Environment.IsDevelopment() ? "true" : "false");
            if (string.Equals(swaggerEnabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                app.UseSwaggerDocumentation();
            }

            // Security headers and caching
            app.UseSecurityHeadersAndCookies(app.Environment);
            app.UseCachingMiddleware();

            // Core pipeline
            app.UseRouting();
            app.UseCors();

            // IMPORTANT: Authentication MUST come BEFORE rate limiting
            // so that authenticated users can be identified and exempted from rate limits
            app.UseAuthentication();

            // Rate limiting AFTER authentication - allows skipping rate limits for authenticated users
            // Best practice: Unauthenticated requests are rate limited, authenticated users have unlimited access
            app.UseRateLimiter();  // Built-in ASP.NET Core rate limiter (policy-based)
            app.UseRateLimiting(); // Custom token bucket rate limiter

            app.UseAuthorization();
        }
    }
}
