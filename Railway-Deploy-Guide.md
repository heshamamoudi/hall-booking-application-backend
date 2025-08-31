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

## 4. Update Connection String
Update your DataContext to use PostgreSQL instead of SQL Server:

```csharp
// In DataContext.cs, replace UseSqlServer with:
optionsBuilder.UseNpgsql(connectionString);
```

## 5. Environment Variables
Set these in Railway dashboard:
- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}`

## 6. Your API will be live at:
`https://your-project-name.up.railway.app`

**Free Tier Limits:**
- $5/month in usage credits
- 500 hours/month execution time
- 1GB RAM, 1 vCPU
- Perfect for small to medium APIs
