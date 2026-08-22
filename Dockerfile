FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution, project files and lockfiles. The lockfiles have to come across
# with the .csproj files or --locked-mode has nothing to check against.
COPY HallApp.sln Directory.Build.props ./
COPY src/HallApp.Web/*.csproj src/HallApp.Web/packages.lock.json src/HallApp.Web/
COPY src/HallApp.Application/*.csproj src/HallApp.Application/packages.lock.json src/HallApp.Application/
COPY src/HallApp.Core/*.csproj src/HallApp.Core/packages.lock.json src/HallApp.Core/
COPY src/HallApp.Infrastructure/*.csproj src/HallApp.Infrastructure/packages.lock.json src/HallApp.Infrastructure/

# --locked-mode: restore exactly what the lockfiles record. A .csproj that has
# drifted from its lockfile fails the build instead of quietly resolving a
# different version.
RUN dotnet restore src/HallApp.Web/HallApp.Web.csproj --locked-mode

# Copy everything else and build
COPY . .
RUN dotnet publish src/HallApp.Web -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# curl is here for HEALTHCHECK below. Installed before dropping to the app user,
# since apt needs root.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

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

# /health runs a database probe, so this reports ready rather than merely
# running. start-period covers migrations on first boot.
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl -fsS http://127.0.0.1:8080/health || exit 1

# Run as a non-root user. APP_UID (1654) is defined by the aspnet base image.
USER $APP_UID

ENTRYPOINT ["dotnet", "HallApp.Web.dll"]
