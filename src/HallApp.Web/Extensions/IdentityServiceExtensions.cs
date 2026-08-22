using HallApp.Infrastructure.Data;
using HallApp.Core.Entities;
using HallApp.Core.Constants;
using HallApp.Web.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
        // Tokens are signed with HMAC-SHA512, which requires a key of at least 512 bits.
        private const int MinimumJwtKeyBytes = 64;

        public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration config)
        {
            // Validated here rather than inside the AddJwtBearer callback: that callback
            // is not invoked until the first request needs the options, so a bad key
            // would otherwise sail through startup and surface as a 500 on every login.
            var jwtSecretKey = config["JWT:SecretKey"];
            if (string.IsNullOrEmpty(jwtSecretKey))
            {
                throw new InvalidOperationException("JWT:SecretKey configuration is required but not found. Check your environment variables.");
            }

            var jwtKeyBytes = Encoding.UTF8.GetByteCount(jwtSecretKey);
            if (jwtKeyBytes < MinimumJwtKeyBytes)
            {
                throw new InvalidOperationException(
                    $"JWT:SecretKey must be at least {MinimumJwtKeyBytes} bytes for HMAC-SHA512 signing, " +
                    $"but the configured value is {jwtKeyBytes}. Set a longer JWT__SecretKey.");
            }

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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
                    
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
                        var path = context.HttpContext.Request.Path;

                        // Check if the request is for any SignalR hub (notificationsHub or chatHub)
                        if (path.StartsWithSegments("/notificationsHub") || path.StartsWithSegments("/chatHub"))
                        {
                            // Get token from query parameter (access_token)
                            var accessToken = context.Request.Query["access_token"].FirstOrDefault();
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                context.Token = accessToken;
                                Console.WriteLine($"🔗 SignalR ({path}): Token received via query parameter (length: {accessToken.Length})");
                            }
                            else
                            {
                                Console.WriteLine($"⚠️ SignalR ({path}): No access_token found in query parameters");
                            }
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(opt =>
            {
                // Legacy policies (preserved for backward compatibility)
                opt.AddPolicy("RequireAdminRole", policy => policy.RequireRole(AppRoles.Admin));
                opt.AddPolicy("RequireHallManagerRole", policy => policy.RequireRole(AppRoles.Admin, AppRoles.HallOrganizationManager, AppRoles.HallManager));
                opt.AddPolicy("ModerateOrdersRole", policy => policy.RequireRole(AppRoles.Admin, AppRoles.Moderator));
                opt.AddPolicy("CustomerRole", policy => policy.RequireRole(AppRoles.Admin, AppRoles.Moderator, AppRoles.Customer));

                // Hall organization-level policies
                opt.AddPolicy("CanManageHallOrganization", policy =>
                    policy.RequireRole(AppRoles.Admin, AppRoles.HallOrganizationManager));

                // Vendor organization-level policies
                opt.AddPolicy("CanManageVendorOrganization", policy =>
                    policy.RequireRole(AppRoles.Admin, AppRoles.VendorOrganizationManager));

                // Generic organization policy (either hall or vendor org manager)
                opt.AddPolicy("CanManageOrganization", policy =>
                    policy.RequireRole(AppRoles.Admin, AppRoles.HallOrganizationManager, AppRoles.VendorOrganizationManager));

                // HallManager can manage assigned halls; HallOrganizationManager and Admin can manage any hall
                opt.AddPolicy("CanManageAssignedHalls", policy =>
                {
                    policy.RequireRole(AppRoles.Admin, AppRoles.HallOrganizationManager, AppRoles.HallManager);
                    policy.AddRequirements(new HallAssignmentRequirement());
                });

                // Vendor management policy
                opt.AddPolicy("CanManageVendors", policy =>
                    policy.RequireRole(AppRoles.Admin, AppRoles.VendorOrganizationManager, AppRoles.VendorManager));
            });

            // Register authorization handler for hall assignment checks
            services.AddScoped<IAuthorizationHandler, HallAssignmentAuthorizationHandler>();

            // Register the custom UserIdProvider
            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

            return services;
        }
    }
}
