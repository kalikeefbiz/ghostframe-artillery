#!/usr/bin/env bash
set -euo pipefail

SHARE_ID="1ucLm-lT0ZRLgROXQNCZvqv4CH1xo6W7PDsbow1O4nw"
SHARE_API="https://build-automation.services.api.unity.com/v2/shares/$SHARE_ID"
OUT_DIR="cloudflare-placeholder"
TMP_DIR="$(mktemp -d)"

cleanup() {
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

echo "[AetherWild] Fetching public metadata for Unity Build #13..."
curl -fsSL --retry 3 "$SHARE_API" -o "$TMP_DIR/share.json"

ARTIFACT_URL="$(
  node - "$TMP_DIR/share.json" <<'NODE'
const fs = require("fs");
const p = process.argv[2];
const data = JSON.parse(fs.readFileSync(p, "utf8"));
const primary = (data.links?.artifacts || []).find(a => a.primary);
const href = primary?.files?.[0]?.href || data.links?.download_primary?.href;
if (!href) process.exit(2);
process.stdout.write(href);
NODE
)"

if [[ -z "$ARTIFACT_URL" ]]; then
  echo "ERROR: Unity share metadata did not contain a primary artifact URL."
  exit 1
fi

echo "[AetherWild] Downloading existing Build #13 artifact..."
curl -fL --retry 3 --retry-all-errors "$ARTIFACT_URL" -o "$TMP_DIR/build.zip"

echo "[AetherWild] Extracting WebGL artifact..."
mkdir -p "$TMP_DIR/extracted"
unzip -q "$TMP_DIR/build.zip" -d "$TMP_DIR/extracted"

INDEX_PATH="$(find "$TMP_DIR/extracted" -type f -name index.html -print -quit)"
if [[ -z "$INDEX_PATH" ]]; then
  echo "ERROR: index.html was not found in the Unity artifact."
  exit 1
fi

WEB_ROOT="$(dirname "$INDEX_PATH")"
echo "[AetherWild] WebGL root: $WEB_ROOT"

rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR"
cp -a "$WEB_ROOT"/. "$OUT_DIR"/

# Ensure the previous diagnostic proxy cannot survive into the static deployment.
rm -f "$OUT_DIR/_worker.js"

cat > "$OUT_DIR/_headers" <<'EOF'
/Build/*.wasm
  Content-Type: application/wasm

/Build/*.js
  Content-Type: application/javascript

/Build/*.data
  Content-Type: application/octet-stream

/Build/*.json
  Content-Type: application/json

/*
  X-Content-Type-Options: nosniff
EOF

echo "[AetherWild] Materialized Build #13 into $OUT_DIR"
du -ah "$OUT_DIR" | sort -h | tail -n 20
