using Microsoft.AspNetCore.DataProtection;

namespace HallApp.Web.Extensions
{
    public static class SecurityServiceExtensions
    {
        public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            // Configure HSTS, which prevents clients from using HTTP instead of HTTPS
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            // Configure secure cookie policy
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Strict;
                options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
                options.Secure = CookieSecurePolicy.Always;
            });

            // Configure data protection - simplified for container/cloud deployment
            // Note: In Railway/container environments, keys are ephemeral unless using external storage
            services.AddDataProtection()
                .SetApplicationName("HallBookingApi")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

            return services;
        }

        public static IApplicationBuilder UseSecurityHeadersAndCookies(this IApplicationBuilder app, IWebHostEnvironment environment)
        {
            // NOTE: Do NOT use UseHsts() when behind a reverse proxy like Railway/Heroku/Render.
            // The proxy handles TLS termination and forwards HTTP to the app internally.
            // UseHsts() would cause an infinite redirect loop because the browser sees HSTS,
            // tries HTTPS, but the proxy always sends HTTP to the app.
            // app.UseHsts();

            // Enable cookie policy
            app.UseCookiePolicy();

            // Add secure HTTP headers using NWebsec middleware - OWASP recommendation
            app
            .UseXContentTypeOptions() // Prevent MIME sniffing
            .UseReferrerPolicy(opts => opts.NoReferrer()) // Control information in the Referer header
            .UseXXssProtection(options => options.EnabledWithBlockMode()) // Cross-site scripting protection
            .UseXfo(options => options.Deny()); // X-Frame-Options deny to prevent clickjacking

            // Configure Content Security Policy - same for all environments
            // NOTE: Do NOT use UpgradeInsecureRequests() behind a reverse proxy (Railway/Heroku/Render)
            // as it causes redirect loops similar to HSTS.
            app.UseCsp(opts => opts
                .DefaultSources(s => s.Self())
                .ScriptSources(s => s.Self())
                .StyleSources(s => s.Self())
                .ImageSources(s => s.Self().CustomSources("data:"))
                .FontSources(s => s.Self())
                .ConnectSources(s => s.Self())
                .ObjectSources(s => s.None())
                .FrameSources(s => s.None())
                .FrameAncestors(s => s.None())
                .BaseUris(s => s.Self())
                .FormActions(s => s.Self())
                // No UpgradeInsecureRequests() - Railway handles HTTPS at the edge
            );

            // Add Permissions-Policy header (formerly Feature-Policy)
            app.Use(async (context, next) => {
                // Skip for Swagger
                if (!context.Request.Path.StartsWithSegments("/swagger")) {
                    context.Response.Headers["Permissions-Policy"] = 
                        "camera=(), microphone=(), geolocation=(), payment=()";
                }
                await next();
            });

            return app;
        }
    }
}
