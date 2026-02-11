# CORS Configuration Guide

## Problem
HTTP status 0 errors occur when the frontend tries to call the backend API but is blocked by CORS policy.

## Solution
The backend automatically allows the following production domains:
- `https://zawaji-app.netlify.app`
- `https://zawaji.netlify.app`
- `https://hall-frontend.netlify.app`

## Railway Environment Variable (Recommended)

For explicit control, set the `CORS__AllowedOrigins` environment variable in Railway:

```bash
CORS__AllowedOrigins=https://zawaji-app.netlify.app,https://your-custom-domain.com
```

**Note:** Use double underscore `__` for nested configuration in environment variables.

## Local Development

Local development automatically allows:
- `http://localhost:4200`
- `http://localhost:3000`
- `http://localhost:5173`
- Plus configured production domains

## Troubleshooting

### HTTP Status 0 Error
**Symptom:** Browser shows "HTTP failure response: 0 Unknown Error"

**Causes:**
1. Frontend domain not in allowed origins list
2. CORS preflight (OPTIONS) request failing
3. Network/SSL issue

**Solution:**
1. Check Railway logs for: `🌐 CORS configured for origins: ...`
2. Verify your frontend domain is listed
3. If missing, add to `CORS__AllowedOrigins` env var or update `knownProductionDomains` in `Program.cs`

### Adding New Production Domain

**Option 1: Environment Variable (Recommended)**
```bash
# In Railway dashboard -> Variables
CORS__AllowedOrigins=https://new-domain.com,https://another-domain.com
```

**Option 2: Code Update**
Edit `Program.cs` line ~60:
```csharp
var knownProductionDomains = new[]
{
    "https://zawaji-app.netlify.app",
    "https://your-new-domain.com",  // Add here
};
```

## Security Notes

- ✅ Wildcard (`*`) origins are **only allowed in development**
- ✅ Production uses explicit domain whitelist
- ✅ Credentials (cookies/auth headers) are supported
- ✅ All HTTP methods and headers are allowed for whitelisted domains

## Logs to Check

On application startup, look for:
```
🌐 CORS configured for origins: https://zawaji-app.netlify.app, https://zawaji.netlify.app
```

If CORS env var is missing:
```
⚠️  WARNING: No CORS__AllowedOrigins configured. Using fallback domains: ...
💡 TIP: Set CORS__AllowedOrigins environment variable for production
```
