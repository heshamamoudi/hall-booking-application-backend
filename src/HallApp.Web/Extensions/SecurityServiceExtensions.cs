using Microsoft.AspNetCore.DataProtection;
using HallApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

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

            // BUGFIX: Configure data protection to persist keys in database
            // Prevents key loss on container restart and ensures consistency across multiple instances
            var dataProtectionBuilder = services.AddDataProtection()
                .SetApplicationName("HallBookingApi")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

            // In production, persist keys to database and configure encryption
            if (!environment.IsDevelopment())
            {
                // Keys will be stored in AspNetCore.DataProtection.EntityFrameworkCore table
                dataProtectionBuilder.PersistKeysToDbContext<DataContext>();

                // SECURITY: Configure at-rest encryption for data protection keys
                // Option 1: Certificate-based encryption (recommended for production)
                var certThumbprint = configuration["DataProtection:CertificateThumbprint"];
                var certPassword = configuration["DataProtection:CertificatePassword"];
                var certPath = configuration["DataProtection:CertificatePath"];

                if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
                {
                    // Load certificate from file (e.g., mounted secret in Railway)
                    var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                        certPath,
                        certPassword
                    );
                    dataProtectionBuilder.ProtectKeysWithCertificate(cert);
                    Console.WriteLine("🔐 Data Protection: Using certificate-based encryption");
                }
                else if (!string.IsNullOrEmpty(certThumbprint))
                {
                    // Load certificate from store (Windows environments)
                    dataProtectionBuilder.ProtectKeysWithCertificate(certThumbprint);
                    Console.WriteLine("🔐 Data Protection: Using certificate from store");
                }
                else
                {
                    // Option 2: Rely on database encryption at rest
                    // PostgreSQL/Railway encrypts data at rest by default
                    Console.WriteLine("⚠️ Data Protection: No certificate configured - relying on database encryption at rest");
                    Console.WriteLine("   To add encryption, set DataProtection:CertificatePath in Railway environment variables");
                    Console.WriteLine("   Keys are protected by PostgreSQL's built-in encryption and TLS in transit");
                }

                Console.WriteLine("🔐 Data Protection: Keys will be persisted to database");
            }
            else
            {
                Console.WriteLine("🔐 Data Protection: Using default file system (development only)");
            }

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
