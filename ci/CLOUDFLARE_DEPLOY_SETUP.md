# AetherWild WebGL auto-deploy setup

The repository includes `ci/deploy-webgl-cloudflare.sh`.

Unity Build Automation should run it as the **Post-Build Script** for the WebGL build target.

## Required Unity Build Automation environment variables

- `CLOUDFLARE_API_TOKEN`
- `CLOUDFLARE_ACCOUNT_ID`
- `CLOUDFLARE_PAGES_PROJECT` (recommended value: `aetherwild`)

## Unity Build Automation configuration

1. Build Automation -> Configurations
2. Edit the WebGL build target
3. Advanced Settings
4. Post-Build Script Path: `ci/deploy-webgl-cloudflare.sh`
5. Add the three environment variables above
6. Apply / Save Changes

The post-build script uses `UNITY_PLAYER_PATH`, writes Unity WebGL Brotli/Gzip MIME headers into `_headers`, and deploys the finished player with Wrangler.

Do not commit Cloudflare credentials into GitHub.
