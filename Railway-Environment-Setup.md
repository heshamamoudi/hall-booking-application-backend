# Railway Environment Variables Setup for Docker Deployment

## Required Environment Variables in Railway Dashboard

Add these **exact** environment variables in your Railway project settings:

### Database Connection
```bash
ConnectionStrings__DefaultConnection = ${{Postgres.DATABASE_URL}}
```

### JWT Configuration
```bash
JWT__SecretKey = HallBookingApp2024!SecretKey@Production#Railway$Deploy%
JWT__Issuer = HallBookingApp
JWT__Audience = hallbookingapp
JWT__ExpiryInHours = 1
```

### ASP.NET Core Configuration
```bash
ASPNETCORE_ENVIRONMENT = Production
# Remove ASPNETCORE_URLS - let Docker handle port binding
```

### CORS Configuration (Update with your actual domains)
```bash
CORS__AllowedOrigins = https://your-actual-frontend-domain.com
```

## How to Add Variables in Railway:

1. Go to your Railway project dashboard
2. Click on your service
3. Go to "Variables" tab
4. Click "New Variable"
5. Add each variable with **exact** name and value above

## Verification:

After deployment, check the health endpoint:
```
GET https://your-app.up.railway.app/health
```

Should return:
```json
{
  "Status": "Healthy",
  "HasConnectionString": true,
  "DatabaseProvider": "PostgreSQL"
}
```

## Troubleshooting:

If environment variables are not being picked up:
1. Verify variable names match exactly (case-sensitive)
2. Check Railway logs for the debug output starting with 🔍
3. Ensure PostgreSQL service is added to your Railway project
4. Verify `${{Postgres.DATABASE_URL}}` is resolving correctly
