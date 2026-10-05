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

echo "[AetherWild] Environment validation complete."

# Wrangler 4 requires Node 22+. Unity's Windows image currently exposes an older
# system Node, so use Unity Build Automation's NVM installation explicitly.
NVM_SCRIPT=""
if [[ -n "${NVM_DIR:-}" && -f "${NVM_DIR}/nvm.sh" ]]; then
  NVM_SCRIPT="${NVM_DIR}/nvm.sh"
elif [[ -f "$HOME/.nvm/nvm.sh" ]]; then
  NVM_SCRIPT="$HOME/.nvm/nvm.sh"
fi

if [[ -z "$NVM_SCRIPT" ]]; then
  echo "ERROR: NVM is required to select Node 22 for Wrangler, but nvm.sh was not found."
  exit 1
fi

echo "[AetherWild] Loading NVM..."
set +u
# shellcheck disable=SC1090
source "$NVM_SCRIPT"
nvm install 22
nvm use 22
set -u

echo "[AetherWild] node: $(node --version)"
echo "[AetherWild] npm: $(npm --version)"
echo "[AetherWild] npx: $(command -v npx)"

PLAYER_PATH="$UNITY_PLAYER_PATH"
# Unity Build Automation runs these hooks as bash even on Windows builders.
# UNITY_PLAYER_PATH is a Cygwin-style path on Windows; Wrangler ultimately runs
# under Node, so pass it a native Windows path there.
if [[ "${BUILDER_OS:-}" == "WINDOWS" ]]; then
  if ! command -v cygpath >/dev/null 2>&1; then
    echo "ERROR: Windows builder detected but cygpath is unavailable."
    exit 1
  fi
  PLAYER_PATH="$(cygpath -wa "$UNITY_PLAYER_PATH")"
fi

echo "[AetherWild] Builder OS: ${BUILDER_OS:-unknown}"
echo "[AetherWild] Player path: $PLAYER_PATH"
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

if ! command -v npx >/dev/null 2>&1; then
  echo "ERROR: npx is required for Wrangler but is not available after selecting Node 22."
  exit 1
fi

echo "[AetherWild] Deploying WebGL output to Cloudflare Pages..."
npx --yes wrangler@latest pages deploy "$PLAYER_PATH" \
  --project-name "$CLOUDFLARE_PAGES_PROJECT" \
  --branch main

echo "[AetherWild] Cloudflare Pages deployment complete."
