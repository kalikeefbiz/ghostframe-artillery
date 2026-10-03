# M0 pipeline gate

## Unity Build Automation configuration
Use a separate AetherWild Unity Cloud project and this repository only.

| Field | Value |
| --- | --- |
| Repository | https://github.com/kalikeefbiz/ghostframe-artillery |
| Branch | main |
| Project subfolder | Empty (repository root) |
| Unity version | 6000.0.60f1 |
| Target | WebGL |
| Scene | Assets/AetherWild/Scenes/Foundation.unity |
| Pre-export method | AetherWild.Editor.BuildSetup.PreExport |

The pre-export method configures the scene and player settings. A build preprocessing callback
also applies the settings. Do not use `BuildWebGL` as the pre-export hook: that method initiates
a local batch build itself. Gzip decompression fallback is enabled for remote testing.
Keep Unity's default WebGL template until actual hosted-device testing identifies a concrete issue.
Unity import should generate remaining default ProjectSettings and the package lock; commit those
resolved files after the first successful editor import. No Unity-generated files are claimed here.

## Required evidence
Record exact Git commit, build number, Unity version, share URL, iPhone model, iOS/Safari version,
and test result. Build success alone does not establish M0 acceptance.

1. Cloud build succeeds with no compilation errors.
2. Published Unity Cloud share loads on iPhone Safari in landscape.
3. Entire authored map and both placeholders are visible; no pink materials or missing terrain.
4. Mint player moves left/right using held touch controls; release stops horizontal movement.
5. Holding a direction and tapping HOP works together; landing has solid terrain collision.
6. Hop cannot repeat in midair; raised and lower terrain sections behave consistently.
7. Dragging off a control, rotating, switching apps, and returning do not leave input stuck.
8. HUD is readable and usable around the notch and Safari chrome in both landscape orientations.
9. Running off the edge reaches the boundary, resets the placeholder, and increments the counter.
10. Repeat movement/hop for several minutes without reloading; inspect available console logs.

## Current result
- Repository source and asset-reference checks: performed before initial M0 commit.
- Unity editor import / C# compilation: NOT RUN; no Unity editor in execution workspace.
- Build Automation: NOT RUN; Unity Cloud session requires sign-in.
- Hosted WebGL: NOT RUN.
- Physical iPhone Safari: NOT RUN.
- M0 exit condition: NOT MET.
- M1: NOT STARTED, intentionally gated on M0 acceptance.

After the gate passes, preserve validated movement/input/terrain components and extend them for M1.
