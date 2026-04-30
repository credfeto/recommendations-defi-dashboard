#!/bin/sh
set -e

terminate() {
    echo "Shutting down..."
    kill "$NGINX_PID" "$NODE_PID" 2>/dev/null || true
    wait "$NGINX_PID" "$NODE_PID" 2>/dev/null || true
}

trap terminate INT TERM

# Validate nginx config before starting anything
nginx -t

# Start nginx in the background
nginx -g 'daemon off;' &
NGINX_PID=$!

# Start Fastify server in the background
cd /app/packages/server
node dist/server/server-fastify.js &
NODE_PID=$!

# Exit the container if either process exits
while true; do
    if ! kill -0 "$NGINX_PID" 2>/dev/null; then
        echo "nginx exited; stopping container"
        terminate
        exit 1
    fi

    if ! kill -0 "$NODE_PID" 2>/dev/null; then
        echo "Node server exited; stopping container"
        terminate
        exit 1
    fi

    sleep 1
done
