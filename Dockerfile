# ─── Stage 1: Build ──────────────────────────────────────────────────────────
FROM node:26-alpine AS builder

WORKDIR /build

# Install native build tools required by better-sqlite3 (node-gyp)
RUN apk add --no-cache python3 make g++

# Copy workspace manifests first for better layer caching
COPY package.json package-lock.json tsconfig.base.json ./
COPY packages/server/package.json ./packages/server/

# Install all dependencies including devDeps.
# HUSKY=0 prevents the prepare script trying to set up git hooks (no .git in Docker).
RUN HUSKY=0 npm ci

# Copy all source
COPY packages/ ./packages/

# Build server — shared types are compiled directly into packages/server/dist/
# Prune to production deps only — no lifecycle scripts are triggered by prune
RUN npm --workspace=@defi-dashboard/server run build && \
    npm prune --omit=dev

# ─── Stage 2: Runtime ────────────────────────────────────────────────────────
FROM node:26-alpine AS runtime

RUN apk add --no-cache openssl

WORKDIR /app

# ── Server compiled output (shared types compiled in under dist/shared/) ─────
COPY --from=builder /build/packages/server/dist /app/packages/server/dist

# ── Production node_modules (already pruned in builder stage) ─────────────────
# package.json is needed for npm workspace module resolution at runtime.
COPY --from=builder /build/package.json /app/package.json
COPY --from=builder /build/packages/server/package.json /app/packages/server/package.json
COPY --from=builder /build/node_modules /app/node_modules

# ── Data directory for SQLite DB (volume-mount point) ─────────────────────────
RUN mkdir -p /app/data

# ── Startup script ────────────────────────────────────────────────────────────
COPY docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN chmod +x /usr/local/bin/docker-entrypoint.sh

EXPOSE 443

ENV DB_DIR=/app/data \
    PORT=443 \
    TLS_KEY_PATH=/etc/ssl/defi/server.key \
    TLS_CERT_PATH=/etc/ssl/defi/server.crt \
    NODE_ENV=production

WORKDIR /app/packages/server

ENTRYPOINT ["docker-entrypoint.sh"]
