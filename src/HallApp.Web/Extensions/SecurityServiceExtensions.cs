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
            // Apply HSTS header
            app.UseHsts();

            // Enable cookie policy
            app.UseCookiePolicy();

            // Add secure HTTP headers using NWebsec middleware - OWASP recommendation
            app
            .UseXContentTypeOptions() // Prevent MIME sniffing
            .UseReferrerPolicy(opts => opts.NoReferrer()) // Control information in the Referer header
            .UseXXssProtection(options => options.EnabledWithBlockMode()) // Cross-site scripting protection
            .UseXfo(options => options.Deny()); // X-Frame-Options deny to prevent clickjacking

            // Configure Content Security Policy
            if (environment.IsProduction())
            {
                // Production CSP with HTTPS enforcement
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
                    .UpgradeInsecureRequests() // Enforce HTTPS in production
                );
            }
            else
            {
                // Development CSP without HTTPS enforcement
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
                    // No UpgradeInsecureRequests() in development
                );
            }

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
