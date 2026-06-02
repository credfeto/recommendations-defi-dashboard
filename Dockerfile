# ─── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Install native AOT prerequisites: clang (linker) and zlib1g-dev (compression)
RUN apt-get update \
    && apt-get install -y --no-install-recommends clang zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /source
COPY .globalconfig ./
COPY src/ ./
RUN dotnet publish Credfeto.Defi.Server/Credfeto.Defi.Server.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained \
    -o /app/publish

# ─── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble AS runtime
# Add Microsoft package repository; install libmsquic (HTTP/3 QUIC) and openssl (cert generation).
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl \
    && curl -fsSL https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb \
       -o /tmp/mspkg.deb \
    && dpkg -i /tmp/mspkg.deb \
    && rm /tmp/mspkg.deb \
    && apt-get update \
    && apt-get install -y --no-install-recommends libmsquic openssl \
    && apt-get purge -y --auto-remove ca-certificates curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
COPY docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN chmod +x /usr/local/bin/docker-entrypoint.sh \
    && mkdir -p /app/data
EXPOSE 8080
EXPOSE 8081
EXPOSE 8081/udp
ENV Cache__DbDirectory=/app/data
ENTRYPOINT ["docker-entrypoint.sh"]
HEALTHCHECK --interval=5s --timeout=2s --retries=3 --start-period=15s \
  CMD ["/app/Credfeto.Defi.Server", "--health-check", "http://127.0.0.1:8080/ping"]
