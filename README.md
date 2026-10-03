# AetherWild
GhostFrame Studios · Unity 6 · mobile-first landscape tactical artillery.

## Status
M0 source foundation only. **Not yet Unity-compiled, cloud-built, or iPhone-validated.**
M1 is gated on a successful M0 mobile pipeline test. Do not begin M2.

This repository is independent of Crownfall Arena; it contains no imported Crownfall code or assets.

## Open and build
- Editor: Unity **6000.0.60f1**, with WebGL Build Support.
- Project root: repository root.
- Scene: `Assets/AetherWild/Scenes/Foundation.unity`.
- In the editor: `AetherWild > Configure M0 build`, open the scene, then Play.
- Batch build: `Unity -batchmode -quit -projectPath <repo> -buildTarget WebGL -executeMethod AetherWild.Editor.BuildSetup.BuildWebGL -logFile <log>`.
- Optional output override: `AETHERWILD_BUILD_PATH`.

See [M0 build and acceptance](Docs/M0-ACCEPTANCE.md) for cloud configuration and required device checks.

## M0 scope
One authored 64-column, half-unit Tilemap battlefield; replaceable mint/orange Mae placeholders;
2D physics; fixed orthographic camera; safe-area HUD; left/right and small-hop touch controls.
Desktop diagnostic controls: A/D or arrow keys, Space.

M0 movement is an unrestricted input diagnostic. Falling beyond the boundary resets the placeholder
and increments a visible counter. M1 will introduce per-turn allowance, defeat, and the combat loop.
No Sigils, damage, AI, terrain destruction, or match rules are claimed to be implemented yet.

## Architecture
- `BattlefieldDefinition`: authored map, spawns, boundary, movement values.
- `TerrainSystem`: one Tilemap for occupancy, rendering and collider generation.
- `MovementController`: Rigidbody2D movement independent of input and art.
- `HoldControl`: multitouch-safe UI pointer state, cleared on exit/focus loss.
- `FoundationScene`: M0 composition and diagnostic presentation; replaceable visual children.
- `BuildSetup`: editor-only build settings and entry points. No WebGL branches in gameplay.

M1 will add data-driven universal Sigil definitions/loadouts and only functional Aether Shot:
Origin / Projectile / Expellant, 20 damage, small persistent terrain impact, unlimited uses.
Any class may equip any Sigil; finite uses never regenerate through cooldowns.
Classes: Embodiment, Conduit, Shaper, Manipulator, Expellant, Specialist.
Resonance architecture must allow >=75% aligned loadout (5/6), without implementing a complex bonus.
