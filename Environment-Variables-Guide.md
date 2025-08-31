# Environment Variables → appsettings.json Mapping

## How .NET Core Binds Environment Variables

Environment variables **override** appsettings.json values using double underscore (`__`) syntax:

```bash
# Railway Environment Variable → appsettings.json Structure
ConnectionStrings__DefaultConnection  →  "ConnectionStrings": { "DefaultConnection": "value" }
JWT__SecretKey                       →  "JWT": { "SecretKey": "value" }
JWT__Issuer                          →  "JWT": { "Issuer": "value" }
JWT__Audience                        →  "JWT": { "Audience": "value" }
CORS__AllowedOrigins                 →  "CORS": { "AllowedOrigins": ["value"] }
```

## Required Railway Environment Variables

```bash
# Database (Railway auto-populates)
ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}

# JWT Configuration  
JWT__SecretKey=your-production-secret-key-here-min-32-chars
JWT__Issuer=HallBookingApp
JWT__Audience=hallbookingapp

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:$PORT

# Optional CORS
CORS__AllowedOrigins=https://yourdomain.com
```

## Your Current appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "overridden by env var"
  },
  "JWT": {
    "SecretKey": "overridden by env var",
    "Issuer": "overridden by env var",
    "Audience": "overridden by env var",
    "ExpiryInHours": 1
  }
}
```

## Database Provider Auto-Detection

- **Local Development**: SQL Server (your existing connection string)
- **Railway Deployment**: PostgreSQL (auto-detected from Railway's `postgresql://` URL)

No code changes needed - the DatabaseProviderFactory handles this automatically!
