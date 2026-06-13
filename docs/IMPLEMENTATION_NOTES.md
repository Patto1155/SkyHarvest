# Implementation Notes — deviations from the MVP plan and why

## Agent validation (before handoff)

Run `bash tools/validate.sh` from the repo root before claiming work is done. Full usage, flags,
troubleshooting, and what is *not* covered: **[docs/VALIDATION.md](VALIDATION.md)**.

The MVP plan (`docs/superpowers/plans/2026-03-14-sky-harvest-mvp.md`) was written assuming interactive
Unity Editor development. This implementation was produced in a headless environment with no Unity
Editor available, optimizing for one goal: **clone the repo, open in Unity 2022.3 LTS, press Play, and
the full game runs with zero manual wiring.** That forced these deviations:

| Plan said | We did | Why |
|---|---|---|
| 3D URP, orthographic camera over 3D models | 2D sprite rendering, built-in render pipeline | Art direction settled on detailed pixel art; 2D sprites are the native fit. URP requires a pipeline asset + quality settings wiring that can't be authored reliably without the editor. |
| Unity Input System package | Legacy `UnityEngine.Input` | Input System needs the "Active Input Handling" player setting flipped + generated C# wrapper class — both editor-side steps. Legacy input works out of the box. |
| TextMeshPro UI | uGUI legacy `Text` | TMP requires "Import TMP Essentials" manual step. |
| Cinemachine | 10-line camera follow script | One less package/manual setup. |
| ScriptableObject `.asset` files for items/crops/recipes/structures/loot | Code-defined `GameDatabase` (plain C# defs) | `.asset` files require hand-authoring YAML with GUID references — fragile without the editor. The data shape mirrors the plan's ScriptableObjects 1:1. |
| Prefabs for player/crops/structures/UI | Everything constructed in code by `Bootstrap` from a single scene | Prefab YAML + meta GUID graphs are too fragile to hand-author. A single hand-written minimal scene (`Assets/Scenes/Main.unity`) holds one `Bootstrap` object. |
| `MainMenu.unity` + `Game.unity` scenes | Single `Main.unity`; main menu is a code-built UI overlay; "New Game"/"Continue" rebuild the world in-place | Avoids EditorBuildSettings/scene-GUID coupling; same player-facing flow. |
| Rigidbody/Physics raycast movement | Transform movement + island-bounds clamp; Physics2D overlap queries for interaction | No physics tuning possible headless; deterministic and sufficient for a grid world. |
| Unity Test Runner | Same NUnit tests, runnable BOTH in Unity Test Runner and via `tools/check.sh` (a .NET 8 harness compiling all game code against UnityEngine stubs) | We needed compile + logic verification without the editor. The stubs live in `tools/clr-harness/` and are not shipped in builds. |

Functional additions beyond the plan (small, spec-aligned):
- `scrap_to_skynet_frame` forge recipe + buildable `skynet` structure, closing the spec's
  "forge better nets → catch better debris" loop at tier 1.
- `InspectorPanel` + `ContextualTooltipUI` (Q to inspect; first-use tooltips) wired from `Bootstrap` — plan UI chunk, code-built like other HUD.
- Audio cues are synthesized procedurally at runtime (no .wav assets can be authored here) by
  `AudioCueSystem` — distinct chimes/thuds per spec §5 player-readable cues.

If the hand-written `Main.unity` ever fails to open: create an empty scene, add an empty GameObject,
attach `Bootstrap` (Assets/Scripts/Core/Bootstrap.cs), press Play. That is the entire wiring.
