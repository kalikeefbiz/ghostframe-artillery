# M1 validation record

Baseline: `826624a176972591ed8eff0871da814d9d46e42a`.
Target editor: Unity 6000.0.60f1 (61dfb374e36f).

Performed outside Unity:
- Parsed package manifest and checked the pinned editor version.
- Checked unique Unity asset GUIDs, .meta coverage, scene/data references and map spawn clearance.
- Compared preserved MovementController, HoldControl, TerrainSystem, BattlefieldDefinition,
  authored Battlefield.asset and ProjectVersion.txt byte-for-byte with the current GitHub baseline.
- Exercised the supplied extraction program in an isolated repository, including source replacement,
  .git/.github preservation, transport removal, commit and local-remote main push.
- Rejected a mismatched archive before repository changes.

Not performed:
- Unity Editor import or C# compilation (Unity is not installed in this environment).
- Unity physics/rendering/Play Mode tests, WebGL compilation or a Unity Cloud deployment.
- iPhone Safari M1 acceptance, including direct-share grey-screen retest.

`CombatChecks.Run` supplies executable C# checks for the cloud/editor environment and is invoked
by BuildSetup.Configure. It covers five-hit defeat, HP bounds, inactive/duplicate cast rejection,
projectile-vs-timer ownership, timeouts, terminal match state, 100 repeated turn handoffs,
reset semantics, asset import/data values, off-class loadout access, unlimited/finite charges,
and analytic AI trajectory solutions in both directions.
These checks are supplied, not represented as already executed.

See M1-ACCEPTANCE.md for the required device tests. No runtime-pass claim is made.
