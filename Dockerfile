# ─── Stage 1: Build everything ─────────────────────────────────────────────────
FROM node:25-alpine AS builder

WORKDIR /build

# Copy workspace manifests first for better layer caching
COPY package.json package-lock.json tsconfig.base.json ./
COPY packages/shared/package.json ./packages/shared/
COPY packages/client/package.json ./packages/client/
COPY packages/server/package.json ./packages/server/

# Install all dependencies including devDeps.
# HUSKY=0 prevents the prepare script trying to set up git hooks (no .git in Docker).
RUN HUSKY=0 npm ci

# Copy all source
COPY packages/ ./packages/

# Build React client → packages/client/build/
RUN npm --workspace=@defi-dashboard/client run build

# Build server (tsc -b compiles shared automatically via project references)
RUN npm --workspace=@defi-dashboard/server run build

# Prune to production deps only — no lifecycle scripts are triggered by prune
RUN npm prune --omit=dev

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
RUN rm -f /etc/nginx/http.d/default.conf

# ── Client static files ────────────────────────────────────────────────────────
COPY --from=builder /build/packages/client/build /app/client

# ── Server compiled output ─────────────────────────────────────────────────────
COPY --from=builder /build/packages/server/dist /app/packages/server/dist
COPY --from=builder /build/packages/shared/dist /app/packages/shared/dist

# ── Production node_modules (already pruned in builder stage) ─────────────────
# package.json files are needed for npm workspace module resolution at runtime
COPY --from=builder /build/package.json /app/package.json
COPY --from=builder /build/packages/server/package.json /app/packages/server/package.json
COPY --from=builder /build/packages/shared/package.json /app/packages/shared/package.json
COPY --from=builder /build/node_modules /app/node_modules
COPY --from=builder /build/packages/server/node_modules /app/packages/server/node_modules

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
