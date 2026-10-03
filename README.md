# AetherWild — M2 vertical-slice source
GhostFrame Studios · Unity 6000.0.60f1 (61dfb374e36f).

Extends validated M1 main `1f40ac1d22750bf119796ed2b95f3c585c562267`.
M2 implementation is packaged, but Unity compilation and iPhone acceptance are pending.

One map: **The Wilds — Depth 1**, using the supplied original JPEG unchanged.
One Summoner: **Mae**, Conduit, player vs AI.

Six universal Sigils: Aether Bolt, Lil' Bomb, Fault, Wall, Summoner's Step, Brace.
Persistent terrain destruction/construction, shield-first damage, finite uses, limited movement,
75% class-resonance threshold, utility AI, menu/results and full rematch reset.

Use the existing scene `Assets/AetherWild/Scenes/Foundation.unity`, build target and pre-export hook.
Read [M2 acceptance](Docs/M2-ACCEPTANCE.md) before uploading and testing.
[Validation record](Docs/VALIDATION.md) distinguishes checks performed here from pending Unity/device tests.

The ZIP has project files at its root. The paired workflow preserves .git and .github and performs
the extraction commit in your repository. No direct GitHub push was attempted.
M0/M1 documents are historical; M2-ACCEPTANCE.md is the current delivery reference.
