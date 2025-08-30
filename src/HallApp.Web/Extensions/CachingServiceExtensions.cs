using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Extensions
{
    public static class CachingServiceExtensions
    {
        public static IServiceCollection AddCachingServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add response caching with secure defaults
            services.AddResponseCaching(options =>
            {
                // Don't cache responses with authorization headers
                options.UseCaseSensitivePaths = true;
                options.MaximumBodySize = 64 * 1024; // 64KB max cached response size
            });

            // Add distributed cache for general caching needs
            services.AddDistributedMemoryCache();

            // Configure cache profiles to apply to controllers
            services.Configure<MvcOptions>(options =>
            {
                // Define cache profiles that can be applied to controllers/actions
                options.CacheProfiles.Add("StaticFiles", new CacheProfile
                {
                    Duration = 86400, // 24 hours for static content
                    Location = ResponseCacheLocation.Any // Allow caching by proxies and browsers
                });
                
                options.CacheProfiles.Add("Public", new CacheProfile
                {
                    Duration = 60, // 1 minute for public API data
                    Location = ResponseCacheLocation.Any,
                    VaryByQueryKeys = new[] { "*" } // Vary by all query parameters
                });
                
                options.CacheProfiles.Add("Private", new CacheProfile
                {
                    Duration = 60,
                    Location = ResponseCacheLocation.Client, // Only allow browser caching
                    NoStore = false // Allow storing
                });
                
                options.CacheProfiles.Add("NoStore", new CacheProfile
                {
                    NoStore = true, // Never store these responses
                    Location = ResponseCacheLocation.None
                });
            });

            return services;
        }

        public static IApplicationBuilder UseCachingMiddleware(this IApplicationBuilder app)
        {
            // Add response caching middleware
            app.UseResponseCaching();

            // Add cache control middleware to ensure proper Cache-Control headers
            app.Use(async (context, next) =>
            {
                // Don't cache authenticated responses by default
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    context.Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
                    context.Response.Headers["Pragma"] = "no-cache";
                }
                
                await next();
            });

            return app;
        }
    }
}
