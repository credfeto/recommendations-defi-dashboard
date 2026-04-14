#!/bin/sh
set -e

# Start nginx in the background
nginx -g 'daemon off;' &

# Start Fastify server using pre-compiled JavaScript
cd /app/packages/server
exec node dist/server/server-fastify.js
