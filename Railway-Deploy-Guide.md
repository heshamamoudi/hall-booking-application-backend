# Railway Deployment Guide (FREE)

## 1. Setup Railway Account
1. Go to [railway.app](https://railway.app)
2. Sign up with GitHub (recommended for auto-deploy)

## 2. Deploy Your API
```bash
# Install Railway CLI
npm install -g @railway/cli

# Login to Railway
railway login

# Initialize project
railway init

# Deploy
railway up
```

## 3. Add PostgreSQL Database
```bash
# Add PostgreSQL to your project
railway add postgresql
```

## 4. Set Environment Variables
In Railway dashboard, add these variables (see .env.template):

**Required:**
- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}`
- `JWT__SecretKey=your-32-char-secret-key-here`
- `JWT__Issuer=HallBookingApp`
- `JWT__Audience=hallbookingapp`
- `ASPNETCORE_URLS=http://0.0.0.0:$PORT`

**Optional:**
- `CORS__AllowedOrigins=https://yourdomain.com`

## 5. Database Auto-Detection
The DatabaseProviderFactory automatically detects your database:
- **SQL Server**: For local development (your current setup)
- **PostgreSQL**: For Railway deployment (auto-detected from connection string)

**No code changes needed!** The factory switches providers based on connection string format.

## 6. Your API will be live at:
`https://your-project-name.up.railway.app`

**Free Tier Limits:**
- $5/month in usage credits
- 500 hours/month execution time
- 1GB RAM, 1 vCPU
- Perfect for small to medium APIs
