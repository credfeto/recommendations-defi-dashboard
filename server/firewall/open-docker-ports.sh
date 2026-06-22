#!/bin/bash

set -euo pipefail

die() {
    if [ -t 2 ]; then
        printf '\n\033[31m✗\033[0m %s\n' "$*" >&2
    else
        printf '\n✗ %s\n' "$*" >&2
    fi
    exit 1
}

success() {
    if [ -t 1 ]; then
        printf '\n\033[32m✓\033[0m %s\n' "$*"
    else
        printf '\n✓ %s\n' "$*"
    fi
}

info() {
    if [ -t 1 ]; then
        printf '\n\033[32m→\033[0m %s\n' "$*"
    else
        printf '\n→ %s\n' "$*"
    fi
}

# Returns true (0) when running inside a Claude Code Bash-tool session.
# Claude Code sets CLAUDECODE=1 in every shell it spawns via the Bash tool;
# that value is inherited by subprocesses (e.g. git hooks).
# Source: https://docs.anthropic.com/en/docs/claude-code/settings#environment-variables
is_ai_agent() {
    [ "${CLAUDECODE:-}" = "1" ]
}

IPV4_PRIVATE_RANGES=(
    "10.0.0.0/8"
    "172.16.0.0/12"
    "192.168.0.0/16"
)

IPV6_PRIVATE_RANGES=(
    "fc00::/7"
    "fe80::/10"
)

allow_ipv4() {
    local subnet="$1"
    local port="$2"
    local protocol="$3"
    firewall-cmd --permanent \
        --add-rich-rule="rule family='ipv4' source address='${subnet}' port port='${port}' protocol='${protocol}' accept"
}

allow_ipv6() {
    local subnet="$1"
    local port="$2"
    local protocol="$3"
    firewall-cmd --permanent \
        --add-rich-rule="rule family='ipv6' source address='${subnet}' port port='${port}' protocol='${protocol}' accept"
}

open_port_for_private_networks() {
    local port="$1"
    local protocol="${2:-tcp}"

    for subnet in "${IPV4_PRIVATE_RANGES[@]}"; do
        allow_ipv4 "${subnet}" "${port}" "${protocol}"
    done

    for subnet in "${IPV6_PRIVATE_RANGES[@]}"; do
        allow_ipv6 "${subnet}" "${port}" "${protocol}"
    done
}

if [ "$(id -u)" -ne 0 ]; then
    die "This script must be run as root"
fi

info "Opening port 443/tcp for private networks..."
open_port_for_private_networks 443 tcp

info "Opening port 443/udp for private networks..."
open_port_for_private_networks 443 udp

info "Opening port 8080/tcp for private networks..."
open_port_for_private_networks 8080 tcp

info "Opening port 8081/tcp for private networks..."
open_port_for_private_networks 8081 tcp

info "Opening port 8081/udp for private networks..."
open_port_for_private_networks 8081 udp

info "Reloading firewall..."
firewall-cmd --reload

success "Docker firewall ports opened successfully"
