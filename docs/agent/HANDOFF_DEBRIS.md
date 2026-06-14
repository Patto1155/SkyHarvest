# Handoff — debris → skynet → island-expansion loop

**Task:** NEXT_SESSION.md #3 — close the gameplay payoff loop the compact ~4×3 starter
island was built for. Patrick is keen on this one.

## Start here

- **Repo:** `D:\APATPROJECTS\SkyHarvest` (GitHub `Patto1155/SkyHarvest`, `gh` authed as Patto1155).
- Main is synced at the PR #5 merge. Working tree should be clean.
- **First action: branch off `main`** (e.g. `feat/debris-loop`). Direct `main` push is blocked —
  PR + user merge-click only.
- **Read first (all short):** `START_HERE.md` → `WORKFLOW.md` → `NEXT_SESSION.md` → `MAP.md`,
  plus `SCOPE_LEDGER.md`. Full feature spec is **§3 of
  `docs/superpowers/specs/2026-03-17-sky-harvest-design.md`**.

## What to build

Debris drifts in and lands on cliff edges → player scavenges it → builds/feeds a craftable
**Skynet** that passively catches drifting debris ("checked like a mailbox") → debris +
scaffolding **expand the island outward**. The code partly exists but is stubbed/unwired —
the job is to **audit wired-vs-dead and connect it end-to-end** so it's visible and playable:
debris visibly lands → scavenge → build/feed a skynet → scaffold a new cell.

## Where the code lives (audit each for wired-vs-stub)

- `Assets/Scripts/Island/IslandExpansion.cs` (`Expand`) + renderer `RenderNewCells`
- `Assets/Scripts/.../DebrisSpawner.cs` + `DebrisObject` (debris spawn/land/scavenge)
- `Assets/Scripts/.../Skynet.cs` — real-time catch buffer w/ `LastCollectedUnixTime`
- Loot tables in `Assets/Scripts/Data/GameDatabase.cs`
- Save fields in `Assets/Scripts/SaveLoad/WorldSaveData.cs` + `SaveManager.cs`

**Known trap (bit us twice already):** several "implemented" systems had **zero runtime callers**
(e.g. `FarmingActions.TryTill`, `BuildMenuUI.Open`). `DebrisSpawner.SetIsland` *is* called in
`Bootstrap.StartNewGame`, but **grep for callers of each debris/skynet/expansion method** and
verify the spawn→land→scavenge→build→expand chain actually fires in Play mode before assuming it works.

## Workflow & gotchas (critical)

- Fast logic check (~10s): `export PATH="/c/Users/Administrator/.dotnet:$PATH"; bash tools/check.sh`
- Full pre-handoff: `bash tools/validate.sh` (check + Unity batch compile + EditMode tests)
- **Adding a NEW `.cs` file → run the Unity command TWICE** (1st imports, 2nd compiles, else
  `CS0103`/`CS0246`). Editing existing files = single pass.
- **After adding/changing tests, rebuild the Tests project**
  (`dotnet build tools/clr-harness/Tests/Tests.csproj`) or `check.sh`'s `--no-build` reports a stale count.
- Visual check: `bash tools/shot.sh` → Read `artifacts/screenshots/contact_sheet.png` (one image;
  auto-dismisses the Unity admin modal). GUI Unity launches always hit the admin dialog —
  `tools/dismiss-unity-admin-dialog.ps1` handles it; `-batchmode` is exempt.
- Prefer pure, testable models (like `HotbarModel` from the last session) for new logic so it's
  covered by NUnit rather than only Play-mode.

## Done-criteria

`tools/validate.sh` green · before/after via `tools/shot.sh` · new NUnit coverage for new logic ·
PR opened · docs (`SCOPE_LEDGER`/`START_HERE`/`NEXT_SESSION`) refreshed. Full session history is in
the agent memory file `project_skyharvest.md`.
