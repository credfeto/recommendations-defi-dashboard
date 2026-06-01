#!/bin/sh
set -e

CERT_PATH="/app/server.pfx"

if [ ! -f "$CERT_PATH" ]; then
    openssl req -x509 -nodes -days 3650 \
        -newkey rsa:2048 \
        -keyout /tmp/server.key \
        -out    /tmp/server.crt \
        -subj   "/CN=defi.local" \
        -addext "subjectAltName=DNS:defi.local,DNS:localhost,IP:127.0.0.1" \
        2>/dev/null
    openssl pkcs12 -export \
        -in     /tmp/server.crt \
        -inkey  /tmp/server.key \
        -out    "$CERT_PATH" \
        -passout pass: \
        2>/dev/null
    rm -f /tmp/server.key /tmp/server.crt
fi

exec /app/Credfeto.Defi.Server "$@"
