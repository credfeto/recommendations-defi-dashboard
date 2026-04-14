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

# ─── Stage 2: Build server ─────────────────────────────────────────────────────
FROM node:25-alpine AS server-builder

WORKDIR /build

# Copy workspace manifests first for better layer caching
COPY package.json package-lock.json tsconfig.base.json ./
COPY packages/shared/package.json ./packages/shared/
COPY packages/client/package.json ./packages/client/
COPY packages/server/package.json ./packages/server/

# Install all workspace dependencies (including devDeps for TypeScript compiler)
RUN npm ci --include=dev

# Copy source packages
COPY packages/shared ./packages/shared/
COPY packages/server ./packages/server/

# Build server — TypeScript project references compile shared automatically
RUN npm --workspace=@defi-dashboard/server run build

# ─── Stage 3: Runtime ──────────────────────────────────────────────────────────
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

# ── Server: install production dependencies only ──────────────────────────────
WORKDIR /app

# Copy workspace manifests for production install
COPY package.json package-lock.json ./
COPY packages/shared/package.json ./packages/shared/
COPY packages/client/package.json ./packages/client/
COPY packages/server/package.json ./packages/server/

# Install production dependencies for server only (excludes client deps such as React)
# Remove the prepare script (which calls husky) before installing — husky is a devDep
# and is not present in --omit=dev installs, causing a "command not found" exit 127.
# better-sqlite3 still compiles correctly since install scripts run normally.
RUN npm pkg delete scripts.prepare && npm ci --omit=dev --workspace=@defi-dashboard/server

# ── Copy compiled server and shared output from build stage ───────────────────
COPY --from=server-builder /build/packages/server/dist /app/packages/server/dist
COPY --from=server-builder /build/packages/shared/dist /app/packages/shared/dist

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
