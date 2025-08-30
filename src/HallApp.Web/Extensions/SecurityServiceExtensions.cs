using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using System.Security.Cryptography;
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

            // Configure data protection with enhanced security settings (cross-platform)
            var dataProtectionPath = Path.Combine(Directory.GetCurrentDirectory(), "DataProtection-Keys");
            if (!Directory.Exists(dataProtectionPath))
            {
                Directory.CreateDirectory(dataProtectionPath);
            }

            var dataProtectionBuilder = services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath)) // Store keys in a specific location
                .SetApplicationName("HallBookingApi") // Isolate by application name
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90)) // Automatic key rotation every 90 days
                // Use authenticated encryption with validation (AES-256-CBC with HMACSHA256)
                .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
                {
                    EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                    ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
                });
                
            // Configure XML encryption for the keys
            // For development and cross-platform compatibility, generate and use a certificate
            try
            {
                // Check for existing certificate or generate a new one
                var certificatePath = Path.Combine(dataProtectionPath, "key-protection.pfx");
                X509Certificate2 certificate;
                
                if (File.Exists(certificatePath))
                {
                    // Use existing certificate (in production, use a password from secure storage)
                    certificate = new X509Certificate2(certificatePath, "", X509KeyStorageFlags.MachineKeySet);
                }
                else
                {
                    // Generate a self-signed certificate for development
                    // In production, use a properly issued certificate
                    using var algorithm = RSA.Create(2048);
                    var subject = new X500DistinguishedName("CN=HallBookingApi Data Protection");
                    var request = new CertificateRequest(
                        subject, 
                        algorithm, 
                        HashAlgorithmName.SHA256, 
                        RSASignaturePadding.Pkcs1
                    );
                    
                    // Set certificate validity period
                    var notBefore = DateTimeOffset.UtcNow;
                    var notAfter = notBefore.AddYears(5); // 5-year validity
                    
                    // Create self-signed certificate
                    certificate = request.CreateSelfSigned(notBefore, notAfter);
                    
                    // Save the certificate (in production, use a strong password)
                    File.WriteAllBytes(certificatePath, certificate.Export(X509ContentType.Pfx));
                }
                
                // Use the certificate to protect the XML keys
                dataProtectionBuilder.ProtectKeysWithCertificate(certificate);
            }
            catch (Exception ex)
            {
                // Log the error but continue (fallback to unencrypted for now)
                // In production, this should be handled more carefully
                Console.Error.WriteLine($"Failed to configure XML encryption: {ex.Message}");
            }

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
