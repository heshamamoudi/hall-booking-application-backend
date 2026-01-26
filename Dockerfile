FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY HallApp.sln .
COPY src/HallApp.Web/*.csproj src/HallApp.Web/
COPY src/HallApp.Application/*.csproj src/HallApp.Application/
COPY src/HallApp.Core/*.csproj src/HallApp.Core/
COPY src/HallApp.Infrastructure/*.csproj src/HallApp.Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish src/HallApp.Web -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "HallApp.Web.dll"]
