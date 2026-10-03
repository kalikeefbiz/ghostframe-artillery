# AetherWild — M1 combat foundation
GhostFrame Studios · Unity **6000.0.60f1** · landscape tactical artillery.

Extends the user's validated M0 at main `826624a176972591ed8eff0871da814d9d46e42a`.
M1 source is prepared; Unity compilation and iPhone runtime acceptance are pending.

Open `Assets/AetherWild/Scenes/Foundation.unity`. Run `AetherWild > Run M1 checks`, then Play.
Use the existing Cloud Build project, branch and pre-export method; do not create a new project.

Implemented: player/AI turns, 30-second timer, retained M0 movement/hop, drag aiming,
partial preview, separate FIRE, swept ballistic projectile collision, 100 HP / 20 damage,
AI shot error, victory/defeat and page-reload-free REMATCH. Only Aether Bolt is functional.

Both placeholders use Mae's shared Conduit definition, a loadout and an independent SigilDefinition.
Finite-resource fields support later Sigils but do not regenerate. Resonance fields are metadata only:
future eligibility requires >=75% class-aligned equipped Sigils (5/6 or 6/6), with no class restrictions.
School enum XO represents X'O. No other Sigil or resonance behavior is implemented.

M0 MovementController, HoldControl, TerrainSystem, authored battlefield and camera configuration
are preserved. FoundationScene only wires combat/input gates/HUD into the existing scene.
Boundary reset remains in place per the M1 request; terrain deformation is excluded.

See [M1 acceptance and upload](Docs/M1-ACCEPTANCE.md) and [validation results](Docs/VALIDATION.md).
The archived M0 acceptance document describes the earlier delivery state, not this milestone's status.
