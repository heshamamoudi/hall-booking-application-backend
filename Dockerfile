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
COPY --from=build /src/Data ./Data

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# The platform mounts the root filesystem read-only with /tmp as the only
# writable path, so scratch space has to live under /tmp.
ENV TMPDIR=/tmp \
    DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp/.net

# Uploaded images have to outlive the container, and /tmp is a tmpfs that does
# not. A volume solves both halves: mounts stay writable under a read-only root
# filesystem, and the contents survive restarts. Ownership is set here because
# Docker seeds a fresh volume from the image directory, permissions included --
# so the app user owns it from the first boot.
RUN mkdir -p /data/uploads && chown -R $APP_UID:$APP_UID /data
VOLUME ["/data/uploads"]
ENV Uploads__Path=/data/uploads

# Run as a non-root user. APP_UID (1654) is defined by the aspnet base image.
USER $APP_UID

ENTRYPOINT ["dotnet", "HallApp.Web.dll"]
