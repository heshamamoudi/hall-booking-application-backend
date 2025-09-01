# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file and project files
COPY ["src/HallApp.Web/HallApp.Web.csproj", "src/HallApp.Web/"]
COPY ["src/HallApp.Application/HallApp.Application.csproj", "src/HallApp.Application/"]
COPY ["src/HallApp.Infrastructure/HallApp.Infrastructure.csproj", "src/HallApp.Infrastructure/"]
COPY ["src/HallApp.Core/HallApp.Core.csproj", "src/HallApp.Core/"]

# Restore dependencies
RUN dotnet restore "src/HallApp.Web/HallApp.Web.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/HallApp.Web"
RUN dotnet build "HallApp.Web.csproj" -c Release -o /app/build

# Publish
RUN dotnet publish "HallApp.Web.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Set environment variables for Railway
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Expose standard port (Railway will override with $PORT at runtime)
EXPOSE 80

COPY --from=build /app/publish .

# Use exec form and let Railway inject env vars at runtime
ENTRYPOINT ["dotnet", "HallApp.Web.dll"]