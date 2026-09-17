#!/bin/sh
set -eu

PUBLIC_HOST="${1:-${MINETUNNEL_PUBLIC_HOST:-}}"
BINARY_SOURCE="/home/ubuntu/minetunnel-relay-src/minetunnel-relay-linux-amd64"
SERVICE_SOURCE="/home/ubuntu/minetunnel-relay.service"

if [ -z "$PUBLIC_HOST" ]; then
  printf 'Uso: sudo sh install-relay.sh IP_OU_DOMINIO_PUBLICO\n' >&2
  exit 1
fi

install -m 0755 "$BINARY_SOURCE" /usr/local/bin/minetunnel-relay

secret=""
if [ -f /etc/minetunnel-relay ]; then
  secret="$(sed -n 's/^MINETUNNEL_SECRET=//p' /etc/minetunnel-relay | head -n 1)"
fi
if [ -z "$secret" ]; then
  secret="$(openssl rand -hex 24)"
fi
config_tmp="$(mktemp)"
trap 'rm -f "$config_tmp"' EXIT
printf 'MINETUNNEL_PUBLIC_HOST=%s\nMINETUNNEL_SECRET=%s\n' \
  "$PUBLIC_HOST" "$secret" > "$config_tmp"
install -m 0600 -o root -g root "$config_tmp" /etc/minetunnel-relay

install -m 0644 "$SERVICE_SOURCE" /etc/systemd/system/minetunnel-relay.service
systemctl daemon-reload
systemctl enable --now minetunnel-relay

printf 'Instalacao concluida. Guarde este segredo em local privado:\n%s\n' "$secret"
