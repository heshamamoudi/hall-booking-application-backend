using HallApp.Web.Middleware;
using HallApp.Web.Middleware.RateLimiting;
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
            // Exception handling first
            app.UseMiddleware<ExceptionMiddleware>();

            // Enable working security middleware
            app.UseMiddleware<SecurityMonitoringMiddleware>();
            app.UseMiddleware<SecurityHeadersMiddleware>();
            // app.UseMiddleware<AntiCsrfMiddleware>(); // Keep disabled - caused IAntiforgery startup issues
            app.UseMiddleware<InputValidationMiddleware>();
            app.UseMiddleware<ImageUploadMiddleware>();

            // Static files - serve from wwwroot
            app.UseStaticFiles();

            // Serve uploaded files from wwwroot/uploads
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads")),
                RequestPath = "/uploads"
            });

            // Swagger in development
            if (app.Environment.IsDevelopment())
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
