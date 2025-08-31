FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/HallApp.Web/HallApp.Web.csproj", "src/HallApp.Web/"]
COPY ["src/HallApp.Application/HallApp.Application.csproj", "src/HallApp.Application/"]
COPY ["src/HallApp.Infrastructure/HallApp.Infrastructure.csproj", "src/HallApp.Infrastructure/"]
COPY ["src/HallApp.Core/HallApp.Core.csproj", "src/HallApp.Core/"]
RUN dotnet restore "src/HallApp.Web/HallApp.Web.csproj"
COPY . .
WORKDIR "/src/src/HallApp.Web"
RUN dotnet build "HallApp.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HallApp.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HallApp.Web.dll"]
