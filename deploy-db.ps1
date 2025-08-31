# Database Deployment Script
# Run this after setting up your production database

# Set production connection string
$ConnectionString = "Server=your-server;Database=HallAppDb;User Id=your-user;Password=your-password;Encrypt=true;"

# Navigate to the project directory
cd "src/HallApp.Infrastructure"

# Apply migrations to production database
dotnet ef database update --connection $ConnectionString --project HallApp.Infrastructure.csproj --startup-project ../HallApp.Web/HallApp.Web.csproj

# Seed initial data (if needed)
Write-Host "Database deployment completed successfully!"
