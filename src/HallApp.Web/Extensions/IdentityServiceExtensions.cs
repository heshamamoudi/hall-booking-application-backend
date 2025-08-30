using HallApp.Infrastructure.Data;
using HallApp.Core.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.DataProtection;
using HallApp.Web.Hubs;

namespace HallApp.Web.Extensions
{
    public static class IdentityServiceExtensions
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddIdentityCore<AppUser>(opt =>
            {
                // Enhanced password security
                opt.Password.RequiredLength = 8;
                opt.Password.RequireDigit = true;
                opt.Password.RequireLowercase = true;
                opt.Password.RequireUppercase = true;
                opt.Password.RequireNonAlphanumeric = true;
                opt.Password.RequiredUniqueChars = 1;
                
                // Account lockout settings
                opt.Lockout.MaxFailedAccessAttempts = 5;
                opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                opt.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<AppRole>()
            .AddRoleManager<RoleManager<AppRole>>()
            .AddSignInManager<SignInManager<AppUser>>()
            .AddEntityFrameworkStores<DataContext>()
            .AddDefaultTokenProviders();

            // Configure token provider options
            services.Configure<DataProtectionTokenProviderOptions>(opt =>
                opt.TokenLifespan = TimeSpan.FromHours(2));

            services.AddAuthentication(auth =>
            {
                auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Always validate the signing key
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["TokenKey"])),
                    
                    // OWASP recommends validating issuer and audience
                    ValidateIssuer = true,
                    ValidIssuer = config["JWT:Issuer"] ?? "hallbookingapi",
                    ValidateAudience = true,
                    ValidAudience = config["JWT:Audience"] ?? "hallbookingapp",
                    
                    // Strict token lifetime validation
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero, // Remove default 5-minute window
                    RequireExpirationTime = true,
                    
                    // Require signed tokens
                    RequireSignedTokens = true,
                    
                    // Validate token replay (if you store used tokens)
                    ValidateTokenReplay = false, // Enable if you implement token storage
                    
                    // Validate the token is not for future use (prevent pre-issued tokens)
                    ValidateActor = false,
                    
                    // Enhanced security settings (OWASP A02:2021 - Cryptographic Failures)
                    
                    // Set token lifetime tracking
                    LifetimeValidator = (before, expires, token, parameters) =>
                    {
                        if (expires == null) return false;
                        // Add custom validation logic here if needed
                        return expires > DateTime.UtcNow;
                    },
                    
                    // Enable NameClaimType for better integration with ASP.NET Core Identity
                    NameClaimType = "name"
                };
                
                // Use secure token settings
                options.SaveToken = true; // Store the token for later use
                options.RequireHttpsMetadata = true; // Require HTTPS metadata in production

                // This handles receiving the token for SignalR connections
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        // Check if the request is for the SignalR hub
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationsHub"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(opt =>
            {
                opt.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
                opt.AddPolicy("RequireHallManagerRole", policy => policy.RequireRole("Admin", "HallManager"));
                opt.AddPolicy("ModerateOrdersRole", policy => policy.RequireRole("Admin", "Moderator"));
                opt.AddPolicy("CustomerRole", policy => policy.RequireRole("Admin", "Moderator", "Customer"));
            });

            // Register the custom UserIdProvider
            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

            return services;
        }
    }
}
