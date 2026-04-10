# ─── Stage 1: Build client ─────────────────────────────────────────────────────
FROM node:25-alpine AS client-builder

WORKDIR /build

# Copy workspace manifests first for better layer caching
COPY package.json package-lock.json tsconfig.base.json ./
COPY packages/shared/package.json ./packages/shared/
COPY packages/client/package.json ./packages/client/
COPY packages/server/package.json ./packages/server/

# Install all workspace dependencies
RUN npm ci --include=dev

# Copy source
COPY packages/ ./packages/

# Build React client → packages/client/build/
RUN npm --workspace=@defi-dashboard/client run build

# ─── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM node:25-alpine AS runtime

# Install nginx and openssl (for self-signed cert generation)
RUN apk add --no-cache nginx openssl

WORKDIR /app

# ── TLS certificate ────────────────────────────────────────────────────────────
RUN mkdir -p /etc/nginx/certs && \
    openssl req -x509 -nodes -days 3650 \
        -newkey rsa:2048 \
        -keyout /etc/nginx/certs/defi.key \
        -out    /etc/nginx/certs/defi.crt \
        -subj   "/CN=defi.local" \
        -addext "subjectAltName=DNS:defi.local,DNS:localhost,IP:127.0.0.1"

# ── nginx configuration ────────────────────────────────────────────────────────
COPY nginx.conf /etc/nginx/http.d/defi.conf
# Remove any default nginx config
RUN rm -f /etc/nginx/http.d/default.conf

# ── Client static files ────────────────────────────────────────────────────────
COPY --from=client-builder /build/packages/client/build /app/client

# ── Server: install workspace deps for production ─────────────────────────────
WORKDIR /app

# Copy workspace manifests and base tsconfig (needed for ts-node path resolution)
COPY package.json package-lock.json tsconfig.base.json /app/
COPY packages/shared/package.json /app/packages/shared/
COPY packages/client/package.json /app/packages/client/
COPY packages/server/package.json /app/packages/server/

# Install all deps (ts-node + tsconfig-paths are devDeps needed at runtime)
RUN npm ci --include=dev

# Copy server and shared source
COPY packages/server /app/packages/server/
COPY packages/shared /app/packages/shared/

# ── Data directory for SQLite DB (volume-mount point) ─────────────────────────
RUN mkdir -p /app/data

# ── Startup script ─────────────────────────────────────────────────────────────
COPY docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN chmod +x /usr/local/bin/docker-entrypoint.sh

EXPOSE 443

ENV DB_DIR=/app/data \
    PORT=5000 \
    NODE_ENV=production

WORKDIR /app/packages/server

ENTRYPOINT ["docker-entrypoint.sh"]
