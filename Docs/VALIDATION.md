# M2 validation record

Baseline: 1f40ac1d22750bf119796ed2b95f3c585c562267.
Target Unity 6000.0.60f1 (61dfb374e36f).

Performed:
- C# syntax parsing of all 19 C# files: no parse errors. This does not check Unity API linking.
- JSON/Unity GUID/reference checks, original-art SHA256, preserved M1 files and source cache exclusions.
- Six-Sigil parameters, affinities, charges and authored profile checks.
- Paired workflow extraction, root replacement, .git/.github preservation, transport removal,
  extraction commit and local-remote main push in an isolated temporary Git repository.
- Mismatched ZIP rejection before repository changes.

Not performed:
- Unity Editor import, C# compilation or shader compilation.
- M2 physics/collider performance, visual alignment or gameplay execution.
- WebGL build, Unity Cloud deployment, physical iPhone acceptance or Safari share retest.

Unity was unavailable. Installing an additional compiler was blocked by environment permissions.
The supplied CombatChecks and SliceChecks run before cloud export but are not claimed executed here.
The original source JPEG hash is in M2-SOURCE-EVIDENCE.json.

M2-ACCEPTANCE.md contains the required runtime checks. M2 is not marked acceptance-passed.
