# AetherWild M2 source delivery

Baseline: GitHub main 1f40ac1d22750bf119796ed2b95f3c585c562267.
Editor remains Unity 6000.0.60f1 (61dfb374e36f). This is an extension of the existing M1 project.

## Status
M2 source implementation is packaged for cloud compilation and device testing.
It is NOT an iPhone-validated release. No Unity editor or physical iPhone was available in the
execution environment. A C# syntax parse is not a Unity compilation or runtime test.
Do not mark any unexecuted acceptance item passed.

## Upload
Upload AetherWild-M2-source.zip unchanged to the GitHub repository root.
Place unpack-aetherwild-m2.yml in .github/workflows/.
The workflow triggers on either file's upload to main, or run it manually from Actions.
Its checksum is paired to this exact ZIP. A changed ZIP needs a matching updated workflow.
It validates and stages the entire archive before replacing the source roots, preserves .git and
.github, removes the transport ZIP, commits and pushes main without force.
Existing source is recoverable through Git history. Branch protection or Actions permissions
can prevent the push. Existing older workflows remain unchanged; use the M2 workflow only.

Keep the existing Cloud Build target, Foundation scene and AetherWild.Editor.BuildSetup.PreExport.
Both CombatChecks.Run and SliceChecks.Run execute before export. These checks must pass in Unity.

## Implemented scope
- Original WildsDepth1.jpeg retained byte-for-byte. No image generation, repainting or conversion.
- One authored 48-unit battlefield with rear low ground, flat shelves, basins and raised center.
- A 0.25-unit grid generates merged full/diagonal collision tiles. No decorative art colliders.
- The renderer samples the original art; cut cells reveal dark excavated cavities and added
  terrain uses rock sampled from that same texture. A thin surface guide indicates actual collision.
- The center is 1.75 units above the spawn shelves, blocking low launch paths.
- Persistent circular excavation, destructible Wall construction, valid-surface Step placement.
- Six visible Sigil buttons, selection, finite charges, shield HP, move allowance and larger FIRE.
- Menu, combat, results, rematch; full terrain, positions/velocity, HP, shield, charges, aim,
  turn timer, projectile and AI-state reset.
- Shared universal Sigil data. Mae remains Conduit. Starter resonance is correctly OFF (1/6).
- 75% resonance threshold, with a 5% bonus only to a matching Sigil's configured primary property.
- Simple utility AI selects among six Sigils, conserves last charges, defends at low HP,
  evaluates support/height/cover, and makes a short safe move when range is poor.

## Exact starter tuning
| Sigil | Damage | Terrain / other effect | Uses |
| --- | ---: | --- | ---: |
| Aether Bolt | 20 direct | 0.35-unit crater radius | Unlimited |
| Lil' Bomb | 30 direct, linear radial falloff | 2.5 splash radius; 1.7 crater; light knockback | 3 |
| Fault | 10 direct | 3.2-unit crater radius | 2 |
| Wall | 0 | 1.5 wide x 2.5 high; 7-unit placement range | 2 |
| Summoner's Step | 0 | 7-unit supported-position range | 2 |
| Brace | 0 | 25 Shield HP | 2 |

Brace refreshes Shield HP to at least 25; it does not stack to 50. Shield persists until consumed.
Damage consumes shield before health, including overflow. Explosions may damage their caster.
Resonance rounds final damage/shield values to whole numbers; terrain and distance use floats.
Wall's bonus scales its dimensions, not damage. Fault's bonus scales terrain radius, not damage.
Off-class Sigils are unrestricted and receive neither a bonus nor a penalty.

Movement allowance is 5 horizontal world units per turn; a hop spends 0.8 allowance.
Knockback is a short impulse added to the existing movement motor. Exhaustion blocks movement.
Aim remains drag-direction/drag-distance; release never fires. FIRE confirms every Sigil.
Wall/Step use tap/drag target position with green/red validity feedback. Invalid casts spend no charge.
The preview remains only the first 0.56 seconds of a projectile path.
After an effect, a short settling interval resolves falling/knockback before the next turn.
Falling below the deep floor or outside map bounds now defeats the Summoner, rather than the
M1 diagnostic boundary reset. This makes excavation and positioning consequential.

## Required iPhone acceptance (all pending)
1. Cloud WebGL build succeeds; menu and original art load; twin suns remain visible.
2. Both rear low grounds, flat shelves, ramps and center are traversable with left/right/hop.
3. Collision surface guide and art feel naturally aligned; center blocks low shots.
4. Bolt deals 20 once; Bomb splashes, knocks back and creates a moderate crater.
5. Fault deals 10 on direct hit and makes a visibly larger crater. Repeated impacts deepen trenches.
6. Wall requires support and clear space, blocks movement/projectiles, supports landing and breaks later.
7. Step rejects out-of-range/out-of-bounds/occupied/unsupported targets; valid destination is standing.
8. Brace gives 25 shield. Verify 30 incoming damage produces shield 0, HP 95.
9. Uses decrease once per valid cast; empty Sigils are disabled; Bolt stays unlimited.
10. All six buttons remain visible, legible and touchable. FIRE never overlaps the Sigil bar.
11. Existing two-finger movement/hop, drag aim, preview, separate FIRE and timer remain stable.
12. AI uses attack, defense and repositioning when useful; invalid attempts consume no resources.
13. Finish full matches in both directions, including a fall defeat.
14. Rematch repeatedly restores the original terrain, removes walls and resets every resource/state.
15. Rotate landscape, switch apps, return and repeat matches without stuck input or duplicate impacts.
16. Test embedded Unity Cloud viewer and direct Safari share separately.

## Known limitations
- Unity C# compilation, shader compilation, collision merging, UI layout and all physical-device tests
  are unexecuted here. Mobile terrain-rebuild performance and ramp traversal need particular attention.
- Original artwork is a single flattened image. Excavated holes use a dark cavity fill rather than
  invented background scenery. The original file itself is unchanged.
- The simplified collision intentionally does not follow every decorative ridge. The raised center
  and Wall additions are shown with source-rock texture; validate their alignment in the build.
- Grid excavation can produce diagonal edges, ledges and floating terrain. No structural collapse.
- AI uses approximate utility and an analytic shot with small error, not a terrain-solving search.
- A basic camera fit expands for deep trenches; high vertical shots may still leave the view briefly.

## Safari diagnostic result
NOT CONFIRMED FIXED. M1's Disabled compression, Decompression Fallback, default template,
WebGL 2/OpenGLES3 and non-threaded setting remain. The new art explicitly imports uncompressed,
without mipmaps or NPOT rescaling, avoiding mobile-specific compressed texture formats.
No hosted Safari test or host-header change was performed. Rebuild and test a fresh share.
No further milestone is included.
