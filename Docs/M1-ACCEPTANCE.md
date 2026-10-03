# AetherWild M1 delivery and acceptance

## Baseline
Extended GitHub main `826624a176972591ed8eff0871da814d9d46e42a`.
Unity version remains **6000.0.60f1**, revision **61dfb374e36f**.
M0 pipeline, scene and touch controls were validated on iPhone by the user.
M1 has not yet been compiled by Unity or tested on a physical iPhone in this delivery environment.

## Manual upload from iPhone
1. Upload `AetherWild-M1-source.zip` unchanged to the repository root.
2. Add the provided workflow exactly at `.github/workflows/unpack-aetherwild-m1.yml`.
3. The workflow runs on either upload to main, or use Actions > Unpack AetherWild M1 Source > Run workflow.
4. Wait for the extraction commit. The job summary reports its exact commit hash.
5. Build the resulting main commit with the existing Unity Cloud WebGL target.

ZIP layout: `Assets/`, `Packages/`, `ProjectSettings/`, `Docs/`, `Tools/`, `README.md`, `.gitignore`
at archive root, without a wrapper directory. Existing `.git` and `.github` are preserved.
The checksum-paired workflow validates the archive before replacing these source roots,
removes the transport ZIP only after a successful copy, then commits/pushes to main.
It never force-pushes. Branch protection or disabled Actions write permissions may block the push.
Enable Actions write permission if GitHub reports that restriction.

The repository currently contains an older `unpack-aetherwild-m0.yml` whose contents are Markdown,
not an executable YAML workflow. It is preserved by request; use the new M1 workflow only.
The separately spelled `unnpack-aetherwild-m0.yml` is also left untouched.

## Build configuration
Keep the current target and scene: `Assets/AetherWild/Scenes/Foundation.unity`.
Keep pre-export: `AetherWild.Editor.BuildSetup.PreExport`.
Core/data assertions automatically run during pre-export and build preprocessing.
Manual editor entry: AetherWild > Run M1 checks.
Standalone batch: `Unity -batchmode -quit -projectPath <project> -executeMethod AetherWild.Editor.CombatChecks.Run -logFile <log>`.
WebGL batch: `Unity -batchmode -quit -projectPath <project> -buildTarget WebGL -executeMethod AetherWild.Editor.BuildSetup.BuildWebGL -logFile <log>`.

## Controls and behavior
- Existing LEFT / RIGHT / HOP positions and MovementController stay unchanged.
- Drag anywhere in the central battlefield area in the desired launch direction.
- Longer drag gives more power (25% to 100%). Release retains the aim; it never fires.
- FIRE commits one cast and immediately closes input until that projectile resolves and AI acts.
- Preview covers only the first 0.56 seconds; it is not a landing solution.
- Every direct enemy hit deals 20 HP. Terrain impact is visual/collision only in M1.
- A miss resolves on terrain, outside the projectile bounds, or after ten simulated seconds.
- A turn times out after 30 active seconds and passes control. Timeout cannot interrupt a projectile.
- Rotation to portrait/background focus suspends turn timer/input. A shot already in flight may finish.
- Boundary reset is retained from validated M0. M1 victory/defeat uses HP, not boundary elimination.
- Mae is Conduit. Both teams use the same definition/loadout; off-class Aether Bolt is allowed.
- REMATCH resets HP, positions/velocity, loadout uses, turn number/timer, aim and in-flight state.
- Input from a held control is cleared at turn transitions; lift and press again on the next turn.

## iPhone acceptance (pending)
Record build number, extraction commit, share URL, device and iOS version.

1. Build loads in landscape; M0 controls still work, including simultaneous direction plus hop.
2. Player starts, 30-second timer counts down, and AI cannot cast in player phase.
3. Drag changes angle/power and partial preview; release does not fire.
4. Tap FIRE rapidly: exactly one bolt appears; movement/hop/aim are blocked during resolution.
5. Direct enemy hit changes 100 -> 80 HP once. Terrain/undershoot/overshoot each resolve the turn.
6. AI fires after its short delay; player can lose 20 HP. Control returns to player repeatedly.
7. Let the player timer expire, both normally and after a finger-down gesture; handoff remains valid.
8. Five direct hits produce VICTORY; five AI hits produce DEFEAT. No further turns occur.
9. REMATCH restores 100/100 HP, starting positions, player turn and 30 seconds without reloading.
10. Repeat multiple matches, focus changes, landscape rotations, finger exits, and two-finger movement/hop.
11. Check both Unity Cloud embedded viewer and direct Safari share separately.

## Known limitations and verification status
- Local source/asset integrity and extraction workflow checks are performed; see VALIDATION.md.
- Unity Editor/runtime is unavailable here. C# build assertions are supplied but not claimed executed.
- Physical iPhone acceptance remains pending. M1 is a source implementation awaiting cloud/device validation.
- High-powered near-vertical shots may briefly leave the fixed camera view before descending.
- AI uses one analytic high arc with small angle/power error, and does not reason about terrain or move.
- No terrain edits, shields, other Sigils, resonance bonus, progression, networking or M2 work.

## Safari grey-screen diagnostic
Status: **not confirmed fixed**.
- Change Gzip to Disabled compression in the build hook and project settings.
- Decompression Fallback remains enabled, but has no compressed payload to decode in this diagnostic build.
- Explicit WebGL 2 / OpenGLES3 graphics API; preserve the default template and non-threaded project setting.
- Generic texture subtarget. Current generated textures are uncompressed RGBA, so there are no imported
  DXT/ASTC textures to change. No texture assets were converted.
- Larger download is the expected tradeoff. Rebuild and retest the direct share; cached old builds do not test this change.
- This does not establish a root cause: share-page embedding, headers, memory or another browser error may remain.

Unity references reviewed:
https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/webgl/building-distribution/deploying
https://docs.unity3d.com/ja/6000.0/ScriptReference/PlayerSettings.WebGL.html
https://docs.unity3d.com/jp/current/ScriptReference/WebGLTextureSubtarget.html
