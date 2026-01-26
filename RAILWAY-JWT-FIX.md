# Railway JWT Configuration Fix

## Problem
Login returns: `"Error generating authentication tokens"`

## Root Cause
The `JWT__SecretKey` environment variable is either:
- Missing
- Set to placeholder value "WILL_BE_REPLACED_BY_ENV_VAR"
- Not being read correctly (wrong format)

## Solution

### 1. Go to Railway Dashboard
1. Navigate to: https://railway.app/dashboard
2. Select your project: "hall-booking-application-backend"
3. Click on your service
4. Go to **"Variables"** tab

### 2. Verify/Add These Variables

**CRITICAL - Check the format exactly:**

```bash
JWT__SecretKey=HallBookingApp2024SecretKey!@#$%^&*()ProductionRailway123456789
JWT__Issuer=HallBookingApp
JWT__Audience=hallbookingapp
JWT__ExpiryInHours=1
```

**⚠️ IMPORTANT NOTES:**
- Double underscore `__` (not single `_`)
- NO spaces around `=`
- Remove quotes if present
- Must be at least 32 characters long

### 3. Verify Database Connection

```bash
ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
```

### 4. Verify ASP.NET Environment

```bash
ASPNETCORE_ENVIRONMENT=Production
```

### 5. Redeploy

After adding variables:
- Railway will automatically redeploy
- Wait for deployment to complete (check logs)
- Test login again

### 6. Quick Verification

Once deployed, check health endpoint:
```bash
curl https://hall-booking-application-backend-production-ce56.up.railway.app/health
```

Should return:
```json
{
  "Status": "Healthy",
  "HasConnectionString": true,
  "DatabaseProvider": "PostgreSQL"
}
```

## Still Getting Errors?

Share Railway deployment logs showing:
- Application startup
- JWT configuration loading
- Any error messages during initialization
