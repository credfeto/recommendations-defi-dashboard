#!/bin/sh
set -e

CERT_PATH="${CERT_PATH:-/app/data/server.pfx}"
CERT_DIR="$(dirname "$CERT_PATH")"

if [ ! -f "$CERT_PATH" ]; then
    mkdir -p "$CERT_DIR"
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
