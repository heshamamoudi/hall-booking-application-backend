# Railway Deployment Guide - Data Protection & Security Setup

## Overview
This guide covers the deployment of the Hall Booking API to Railway with proper Data Protection configuration, including certificate-based encryption for production security.

---

## Step 1: Automatic Database Migration on Deployment

### ✅ Already Configured
The application automatically applies pending migrations on startup. Verify in `Program.cs`:

```csharp
// Auto-migrate database on startup (production-safe)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DataContext>();
    context.Database.Migrate();
}
```

### New Migration Included
- **Migration:** `20260211210624_AddDataProtectionKeys`
- **Creates:** `DataProtectionKeys` table for persistent key storage
- **Status:** ✅ Ready to deploy

### Deployment Process
1. Push code to GitHub (✅ Already done: commit `180a708`)
2. Railway detects changes and triggers build
3. On container startup, migrations auto-apply
4. Application starts with persistent key storage

---

## Step 2: Certificate-Based Encryption (Optional but Recommended)

### Option A: Generate Self-Signed Certificate (Quick Start)

#### 1. Generate Certificate Locally
```bash
# Generate a self-signed certificate (valid for 10 years)
openssl req -x509 -newkey rsa:4096 -keyout dataprotection.key -out dataprotection.crt \
  -days 3650 -nodes -subj "/CN=HallBookingDataProtection"

# Combine into PFX format
openssl pkcs12 -export -out dataprotection.pfx -inkey dataprotection.key \
  -in dataprotection.crt -password pass:YourStrongPassword123!
```

#### 2. Base64 Encode for Railway
```bash
# Encode certificate to base64 for environment variable
base64 -i dataprotection.pfx -o dataprotection.pfx.base64

# On macOS, use:
base64 -i dataprotection.pfx > dataprotection.pfx.base64
```

#### 3. Configure Railway Environment Variables
Add these in Railway Dashboard → Variables:

```env
DataProtection__CertificatePassword=YourStrongPassword123!
DataProtection__CertificateBase64=<paste-base64-content-here>
```

#### 4. Update SecurityServiceExtensions.cs
Add base64 certificate loading:

```csharp
// In SecurityServiceExtensions.cs, add before certPath check:
var certBase64 = configuration["DataProtection:CertificateBase64"];
if (!string.IsNullOrEmpty(certBase64))
{
    var certBytes = Convert.FromBase64String(certBase64);
    var cert = new X509Certificate2(certBytes, certPassword);
    dataProtectionBuilder.ProtectKeysWithCertificate(cert);
    Console.WriteLine("🔐 Data Protection: Using certificate from environment variable");
}
```

### Option B: Use Railway Volume (Persistent File Storage)

#### 1. Create Railway Volume
```bash
# In Railway Dashboard:
# 1. Go to your service
# 2. Click "Variables" tab
# 3. Add a Volume mount point: /app/secrets
```

#### 2. Upload Certificate via Railway CLI
```bash
# Install Railway CLI
npm i -g @railway/cli

# Login and link project
railway login
railway link

# Copy certificate to volume (after first deploy creates volume)
railway run bash
# Inside container:
mkdir -p /app/secrets
# Exit and use Railway dashboard to upload file
```

#### 3. Configure Environment Variables
```env
DataProtection__CertificatePath=/app/secrets/dataprotection.pfx
DataProtection__CertificatePassword=YourStrongPassword123!
```

### Option C: Production CA-Signed Certificate (Enterprise)

For production with proper CA-signed certificates:

1. Obtain certificate from a Certificate Authority (Let's Encrypt, DigiCert, etc.)
2. Convert to PFX format if needed
3. Follow Option A (base64) or Option B (volume) for deployment

---

## Step 3: Verify Deployment & Security Status

### 1. Check Railway Deployment Logs

After deployment, verify these log messages:

```log
✅ Expected Success Logs:
🔐 Data Protection: Keys will be persisted to database
🔐 Data Protection: Using certificate-based encryption
   OR
🔐 Data Protection: Using certificate from environment variable
   OR
⚠️ Data Protection: No certificate configured - relying on database encryption at rest
```

### 2. Verify Database Tables

Connect to PostgreSQL and verify:

```sql
-- Check DataProtectionKeys table exists
\dt DataProtectionKeys

-- Should show table with columns: Id, FriendlyName, Xml
SELECT * FROM "DataProtectionKeys" LIMIT 1;
```

### 3. Test Key Persistence

1. Deploy application to Railway
2. Login to app and create auth token
3. Restart Railway service (Railway Dashboard → Deployments → Restart)
4. Verify auth token still works (keys persisted!)

### 4. Security Checklist

- ✅ Keys stored in PostgreSQL database (not ephemeral filesystem)
- ✅ Keys survive container restarts
- ✅ Keys encrypted at rest (PostgreSQL default encryption)
- ✅ TLS encryption in transit (Railway → PostgreSQL)
- ✅ 90-day automatic key rotation configured
- ⚠️ Certificate encryption (optional, recommended for production)

---

## Security Levels Comparison

| Level | Configuration | Security | Complexity |
|-------|--------------|----------|-----------|
| **Minimum** | Database persistence only | Good | ✅ Simple (current) |
| **Standard** | DB + Self-signed cert | Better | ⚡ Moderate |
| **Production** | DB + CA-signed cert | Best | 🔒 Advanced |

### Current Status: ✅ Minimum (Production-Ready)
- Keys persist in PostgreSQL
- Protected by database encryption at rest
- TLS encryption for all connections
- **Recommendation:** Sufficient for most applications

### Upgrade to Standard (Recommended for Sensitive Data)
Follow **Step 2, Option A** to add certificate encryption.

---

## Troubleshooting

### Issue: Keys Still Lost on Restart
**Symptom:** Users forced to re-login after deployment
**Solution:**
1. Check logs for: `🔐 Data Protection: Keys will be persisted to database`
2. Verify DataProtectionKeys table exists in PostgreSQL
3. Check Railway environment has DATABASE_URL configured

### Issue: Certificate Loading Fails
**Symptom:** Warning about unencrypted keys
**Solution:**
1. Verify base64 encoding is correct (no newlines/spaces)
2. Check password matches in environment variable
3. Review logs for specific error messages

### Issue: Migration Fails
**Symptom:** Application won't start after deployment
**Solution:**
1. Check Railway logs for migration errors
2. Verify PostgreSQL connection string is correct
3. Ensure database user has CREATE TABLE permissions

---

## Environment Variables Reference

### Required (Already Set)
```env
DATABASE_URL=postgresql://user:pass@host:5432/dbname
ASPNETCORE_ENVIRONMENT=Production
```

### Data Protection (Optional)
```env
# Option 1: Certificate from base64
DataProtection__CertificateBase64=<base64-encoded-pfx>
DataProtection__CertificatePassword=<password>

# Option 2: Certificate from file path
DataProtection__CertificatePath=/app/secrets/cert.pfx
DataProtection__CertificatePassword=<password>

# Option 3: Certificate from Windows store (not applicable for Railway)
DataProtection__CertificateThumbprint=<thumbprint>
```

---

## Railway Deployment Command Summary

```bash
# 1. Verify local build
cd HallAppBackend
dotnet build
dotnet ef database update  # Test migration locally

# 2. Commit and push (already done)
git add .
git commit -m "Deploy with data protection fixes"
git push origin main

# 3. Railway auto-deploys from GitHub
# Monitor: https://railway.app/dashboard

# 4. Check deployment logs
railway logs

# 5. Verify health endpoint
curl https://your-app.railway.app/health
```

---

## Next Steps After Deployment

1. **Monitor First Deployment**
   - Watch Railway logs for successful migration
   - Verify "Data Protection: Keys will be persisted" message
   - Test user login/auth functionality

2. **Consider Certificate Upgrade** (Within 30 days)
   - Generate certificate using Option A
   - Add to Railway environment variables
   - Redeploy to enable encryption layer

3. **Document Certificate Rotation** (Within 90 days)
   - Calendar reminder for certificate renewal
   - Update DataProtection__CertificateBase64 when renewing
   - Keys auto-rotate every 90 days

4. **Security Audit**
   - Review Railway security settings
   - Enable Railway's built-in security features
   - Set up monitoring/alerting for auth failures

---

## Additional Resources

- [ASP.NET Core Data Protection](https://docs.microsoft.com/aspnet/core/security/data-protection/)
- [Railway Volumes Documentation](https://docs.railway.app/deploy/volumes)
- [PostgreSQL Encryption](https://www.postgresql.org/docs/current/encryption-options.html)
- [Certificate Management Best Practices](https://docs.microsoft.com/security/certificates)

---

## Support

For issues with this deployment:
1. Check Railway logs first
2. Review this guide's Troubleshooting section
3. Verify all environment variables are set correctly
4. Test locally with PostgreSQL before deploying

**Last Updated:** 2026-02-11
**Version:** 1.0.0
**Migration Version:** 20260211210624_AddDataProtectionKeys
