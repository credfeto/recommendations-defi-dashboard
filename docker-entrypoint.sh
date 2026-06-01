#!/bin/sh
set -e

TLS_KEY="${TLS_KEY_PATH:-/etc/ssl/defi/server.key}"
TLS_CERT="${TLS_CERT_PATH:-/etc/ssl/defi/server.crt}"

if [ ! -f "$TLS_KEY" ] || [ ! -f "$TLS_CERT" ]; then
    mkdir -p "$(dirname "$TLS_KEY")"
    openssl req -x509 -nodes -days 3650 \
        -newkey rsa:2048 \
        -keyout "$TLS_KEY" \
        -out    "$TLS_CERT" \
        -subj   "/CN=defi.local" \
        -addext "subjectAltName=DNS:defi.local,DNS:localhost,IP:127.0.0.1" \
        2>/dev/null
fi

exec node dist/server/server-fastify.js
