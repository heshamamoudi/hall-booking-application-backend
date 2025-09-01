FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# Pass environment variables into the Docker container
ARG ConnectionStrings__DefaultConnection
ARG DATABASE_URL
ARG JWT__SecretKey
ARG JWT__Issuer
ARG JWT__Audience
ARG JWT__ExpiryInHours
ARG ASPNETCORE_ENVIRONMENT
ARG ASPNETCORE_URLS
ARG CORS__AllowedOrigins
ARG LOGGING__LOGLEVEL__DEFAULT
ARG LOGGING__LOGLEVEL__MICROSOFT

ENV ConnectionStrings__DefaultConnection=$ConnectionStrings__DefaultConnection
ENV DATABASE_URL=$DATABASE_URL
ENV JWT__SecretKey=$JWT__SecretKey
ENV JWT__Issuer=$JWT__Issuer
ENV JWT__Audience=$JWT__Audience
ENV JWT__ExpiryInHours=$JWT__ExpiryInHours
ENV ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT
ENV ASPNETCORE_URLS=$ASPNETCORE_URLS
ENV CORS__AllowedOrigins=$CORS__AllowedOrigins
ENV LOGGING__LOGLEVEL__DEFAULT=$LOGGING__LOGLEVEL__DEFAULT
ENV LOGGING__LOGLEVEL__MICROSOFT=$LOGGING__LOGLEVEL__MICROSOFT



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
