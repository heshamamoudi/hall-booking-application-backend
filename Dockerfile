# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

WORKDIR /src/src/HallApp.Web
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Accept Railway env vars as ARGs and turn into ENV
ARG ConnectionStrings__DefaultConnection
ENV ConnectionStrings__DefaultConnection=${ConnectionStrings__DefaultConnection}

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HallApp.Web.dll"]