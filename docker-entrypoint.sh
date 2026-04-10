#!/bin/sh
set -e

# Start nginx in the background
nginx -g 'daemon off;' &

# Start Fastify server using ts-node (handles @shared path aliases via tsconfig-paths)
cd /app/packages/server
exec node \
  -r tsconfig-paths/register \
  -r ts-node/register \
  src/server/server-fastify.ts
