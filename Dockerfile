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

# Copy everything else and build.
#
# `COPY . .` is why .dockerignore in this repository is load-bearing rather than
# tidiness: whatever is left in the context lands in this stage. Every spelling
# of .env is excluded there; .env.template, which carries names and no values,
# is the only one that travels.
COPY . .

# SatelliteResourceLanguages=en drops the framework's translated exception
# strings. Measured: thirteen language folders - cs, de, es, fr, it, ja, ko, pl,
# pt-BR, ru, tr, zh-Hans, zh-Hant - totalling 7.3 MB, for messages that go to a
# log nobody reads in any of those languages.
RUN dotnet publish src/HallApp.Web -c Release -o /app/publish --no-restore \
    -p:SatelliteResourceLanguages=en

# Drop the RID-specific assemblies for platforms this image will never be.
# Windows only: `unix` and `browser` stay, because one of them is what actually
# loads. Measured: runtimes/ was 7.7 MB, of which the win* folders were 6.4 MB.
RUN set -eu; \
    find /app/publish/runtimes -mindepth 1 -maxdepth 1 -name 'win*' -exec rm -rf {} +; \
    echo "kept runtimes: $(ls /app/publish/runtimes)"

# Split the publish into what changes on every commit and what does not.
#
# The binding constraint on the registry is not storage, it is TRANSFER: 1 GB a
# month for a private package on GitHub Free. A pull moves only the layers that
# changed, so the size that matters per deploy is the size of the layer a commit
# rewrites - not the size of the image.
#
# /out/deps - third-party assemblies and their RID assets. Copied FIRST, so it
#             is the earlier and therefore stable layer.
# /out/app  - HallApp.* , this solution's own output, plus wwwroot. The only
#             thing a code commit rewrites.
#
# The split only pays if the deps layer is bit-IDENTICAL between builds: one
# byte of difference changes its digest and the registry re-uploads the lot for
# a change to nothing. `dotnet publish` stamps each copied dependency with the
# time of the build, so the mtimes are pinned to the epoch here.
#
# BuildKit is understood to normalise timestamps as it snapshots a layer, which
# would make this line redundant rather than wrong. It is kept because it costs
# nothing, it holds under a builder that does not normalise, and the failure it
# guards against is invisible: the image would be correct and the bill would
# simply be many times larger.
RUN set -eu; \
    mkdir -p /out/deps /out/app; \
    cd /app/publish; \
    find . -mindepth 1 -maxdepth 1 -name 'HallApp.*' -exec mv {} /out/app/ \; ; \
    if [ -d wwwroot ]; then mv wwwroot /out/app/; fi; \
    find . -mindepth 1 -maxdepth 1 -exec mv {} /out/deps/ \; ; \
    find /out/deps -exec touch -h -d @0 {} + ; \
    echo "deps $(du -sh /out/deps | cut -f1), app $(du -sh /out/app | cut -f1)"

# The upload mount point, made in the stage that still has a shell. See below.
RUN mkdir -p /mnt/uploads

# ---------------------------------------------------------------------------
# Runtime image.
#
# Ubuntu chiselled: the same glibc and the same .NET runtime as the Debian image
# this replaces, with everything that is not the runtime removed. No shell, no
# apt, no curl, no wget. 420 MB becomes 291 MB, and the twelve unfixable gosu
# findings in 12-open-actions.md go with it, because there is no gosu either.
#
# -extra, because this application needs ICU and tzdata: AuthController.cs calls
# TimeZoneInfo.FindSystemTimeZoneById("Georgia Standard Time") - a Windows
# timezone id, which .NET can only translate to an IANA one through ICU. On a
# plain chiselled base DOTNET_SYSTEM_GLOBALIZATION_INVARIANT is true and that
# call throws TimeZoneNotFoundException, on a path nothing here exercises. ICU
# costs 14.6 MB compressed. Finding that fault in production costs more.
#
# NOT -composite-extra, which is 17 MB smaller and was tried first. It does not
# work for this application, and the failure is total rather than subtle:
#
#     Process terminated. MVID mismatch between loaded assembly
#     'Microsoft.Extensions.Hosting.Abstractions' and an assembly with the same
#     simple name embedded in the native image 'full-composite.r2r.dll'
#
# A composite image compiles the framework assemblies into one ReadyToRun blob,
# and refuses to load an app-local copy of anything already inside it. This
# publish carries its own Microsoft.Extensions.Hosting.Abstractions.dll, so the
# container exits at startup, every time. Measured on 2026-08-27, not inferred.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled-extra@sha256:9c681b562bfe4e55c806ba768543b3f9711ba85afc23923548d198dc30b8e898
WORKDIR /app

# Three layers, rarely-changing ones first. See the split in the build stage: a
# code-only commit rewrites only /out/app and leaves the others byte-identical,
# so a deploy pulls the application assemblies rather than the whole image.
COPY --from=build --chown=1654:1654 /out/deps .
COPY --from=build --chown=1654:1654 /src/Data ./Data
COPY --from=build --chown=1654:1654 /out/app .

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
#
# Copied from the build stage rather than created with `mkdir`, because a
# chiselled image has no shell and therefore no RUN. COPY --chown does the same
# job at the same point in the build.
#
# The uid does not change with this base - verified, not assumed: `docker
# inspect` reports APP_UID=1654 and USER 1654 on both mcr.../aspnet:8.0 and
# aspnet:8.0-jammy-chiseled*. (The 64198 uid belongs to the .NET 9+ / Noble
# chiselled images, not to these.) The existing selfhost_zawaji_api_uploads
# volume is owned by 1654 and stays usable; nobody has to chown a live volume.
COPY --from=build --chown=1654:1654 /mnt/uploads /data/uploads
VOLUME ["/data/uploads"]
ENV Uploads__Path=/data/uploads

# /health runs a database probe, so this reports ready rather than merely
# running. start-period covers migrations on first boot.
#
# There is no curl in this image and no way to install one, so the probe is the
# application binary itself: --healthcheck is handled at the top of Program.cs,
# before any host is built, and returns an exit code. Exec form, so no /bin/sh
# is involved - because there is not one.
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD ["dotnet", "/app/HallApp.Web.dll", "--healthcheck"]

# Run as a non-root user. 1654 is APP_UID on this base, as it was on the last.
USER 1654

ENTRYPOINT ["dotnet", "HallApp.Web.dll"]
