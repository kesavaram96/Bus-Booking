# Build with this from the repository root, e.g.:
#   docker build -f src/BusBooking.API/Dockerfile -t busbooking-api .
# (docker-compose.yml at the repo root already points at this file and context.)

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files copied (and restored) before the rest of the source, so Docker's layer cache is
# reused on every rebuild that only changes source code, not dependencies.
COPY ["src/BusBooking.API/BusBooking.API.csproj", "src/BusBooking.API/"]
COPY ["src/BusBooking.Application/BusBooking.Application.csproj", "src/BusBooking.Application/"]
COPY ["src/BusBooking.Infrastructure/BusBooking.Infrastructure.csproj", "src/BusBooking.Infrastructure/"]
COPY ["src/BusBooking.Domain/BusBooking.Domain.csproj", "src/BusBooking.Domain/"]
RUN dotnet restore "src/BusBooking.API/BusBooking.API.csproj"

COPY src/ src/
RUN dotnet publish "src/BusBooking.API/BusBooking.API.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl for the HEALTHCHECK below — the slim runtime image doesn't ship it by default. Installed
# as root (the image's default user at this point), before dropping to the unprivileged "app"
# user for everything else.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Non-root from here on: the aspnet base image ships a preconfigured, unprivileged "app" user
# (uid/gid 64198) specifically for this — no need to create one by hand.
USER app

COPY --from=build /app/publish .

# .NET 8+ base images already default to 8080; set explicitly so this doesn't silently depend
# on that default if the base image ever changes it. TLS is not terminated here — see
# docker-compose.yml / README for the reverse-proxy-in-front pattern this expects.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Real DB + Redis connectivity check (the same /health endpoint docker-compose's own healthcheck
# and any orchestrator's readiness probe would use) — makes `docker run` alone report health too,
# not just Compose.
HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=5 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "BusBooking.API.dll"]
