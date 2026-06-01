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
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/data
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV Cache__DbDirectory=/app/data
ENTRYPOINT ["/app/Credfeto.Defi.Server"]
HEALTHCHECK --interval=5s --timeout=2s --retries=3 --start-period=5s \
  CMD ["/app/Credfeto.Defi.Server", "--health-check", "http://127.0.0.1:8080/ping"]
