#!/usr/bin/env bash
set -euo pipefail

echo "[AetherWild] Cloudflare Pages deployment starting..."

if [[ -z "${UNITY_PLAYER_PATH:-}" ]]; then
  echo "ERROR: UNITY_PLAYER_PATH is not set. This script must run as a Unity Build Automation post-build script."
  exit 1
fi

if [[ ! -d "$UNITY_PLAYER_PATH" ]]; then
  echo "ERROR: UNITY_PLAYER_PATH does not point to a directory: $UNITY_PLAYER_PATH"
  exit 1
fi

if [[ -z "${CLOUDFLARE_API_TOKEN:-}" ]]; then
  echo "ERROR: Missing CLOUDFLARE_API_TOKEN environment variable."
  exit 1
fi

if [[ -z "${CLOUDFLARE_ACCOUNT_ID:-}" ]]; then
  echo "ERROR: Missing CLOUDFLARE_ACCOUNT_ID environment variable."
  exit 1
fi

CLOUDFLARE_PAGES_PROJECT="${CLOUDFLARE_PAGES_PROJECT:-aetherwild}"

# Unity Build Automation documents NVM as available on build machines.
# Source the profile so node/npm installed by the image are on PATH.
if [[ -f "$HOME/.profile" ]]; then
  # shellcheck disable=SC1090
  source "$HOME/.profile" || true
fi

echo "[AetherWild] Player path: $UNITY_PLAYER_PATH"
echo "[AetherWild] Pages project: $CLOUDFLARE_PAGES_PROJECT"

# Unity WebGL Brotli/Gzip artifacts need explicit MIME + encoding headers on static hosts.
cat > "$UNITY_PLAYER_PATH/_headers" <<'EOF'
/Build/*.wasm.br
  Content-Type: application/wasm
  Content-Encoding: br

/Build/*.js.br
  Content-Type: application/javascript
  Content-Encoding: br

/Build/*.data.br
  Content-Type: application/octet-stream
  Content-Encoding: br

/Build/*.symbols.json.br
  Content-Type: application/json
  Content-Encoding: br

/Build/*.wasm.gz
  Content-Type: application/wasm
  Content-Encoding: gzip

/Build/*.js.gz
  Content-Type: application/javascript
  Content-Encoding: gzip

/Build/*.data.gz
  Content-Type: application/octet-stream
  Content-Encoding: gzip

/Build/*.symbols.json.gz
  Content-Type: application/json
  Content-Encoding: gzip

/*
  X-Content-Type-Options: nosniff
EOF

if ! command -v node >/dev/null 2>&1 || ! command -v npm >/dev/null 2>&1; then
  echo "ERROR: Node.js/npm are required for Wrangler but are not available on this build image."
  exit 1
fi

echo "[AetherWild] Deploying WebGL output to Cloudflare Pages..."
npx --yes wrangler@latest pages deploy "$UNITY_PLAYER_PATH" \
  --project-name "$CLOUDFLARE_PAGES_PROJECT" \
  --branch main

echo "[AetherWild] Cloudflare Pages deployment complete."
