#!/bin/sh
set -e

mkdir -p /etc/ssl/defi

openssl req -x509 -nodes -days 3650 \
    -newkey rsa:2048 \
    -keyout /etc/ssl/defi/server.key \
    -out    /etc/ssl/defi/server.crt \
    -subj   "/CN=defi.local" \
    -addext "subjectAltName=DNS:defi.local,DNS:localhost,IP:127.0.0.1" \
    2>/dev/null

exec node dist/server/server-fastify.js
