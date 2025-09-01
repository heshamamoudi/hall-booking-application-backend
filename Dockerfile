# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/HallApp.Web/HallApp.Web.csproj", "src/HallApp.Web/"]
COPY ["src/HallApp.Application/HallApp.Application.csproj", "src/HallApp.Application/"]
COPY ["src/HallApp.Infrastructure/HallApp.Infrastructure.csproj", "src/HallApp.Infrastructure/"]
COPY ["src/HallApp.Core/HallApp.Core.csproj", "src/HallApp.Core/"]

RUN dotnet restore "src/HallApp.Web/HallApp.Web.csproj"
COPY . .
WORKDIR "/src/src/HallApp.Web"
RUN dotnet publish "HallApp.Web.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose ports (optional)
EXPOSE 80
EXPOSE 443

# ENTRYPOINT — run the app
ENTRYPOINT ["dotnet", "HallApp.Web.dll"]