# Handover: island edges — void overhang + dynamic cliff/underside faces

**Repo:** `D:\APATPROJECTS\SkyHarvest` · branch `main` (head `ab7401b`) · Unity 2022.3.45f1, headless/code-built by `Bootstrap`.
**Start the game with "New Game"** — the designed two-tier `StarterIsland` is only on the new-game path (Continue/save is still the old procedural island).

The starter island is `Island/StarterIsland.cs`: a fixed **3 wide (gx 0..2) × 4 deep (gy 0..3)**. Front 2×3 = farm (tier 0, player spawns here); back 2×3 = raised forge (tier 1, `Elevation=1` → +0.5 world via `Constants.ElevationWorldStep`). Stair edge `(1,2)↔(1,1)`. Projection in `Core/GridMath.cs`: `wx=(x−y)·0.5`, `wy=(x+y)·−0.25 + elev·0.5`; 64 px/unit; diamonds tessellate centred on each `GridToWorld` point.

Fix these **in order**. Verify after each with `tools/check.sh` (rebuild `tools/clr-harness/Tests/Tests.csproj` first — `--no-build` runs a stale Tests.dll) and a `tools/shot.sh` contact sheet (`artifacts/screenshots/contact_sheet.png`).

---

## 1. (FIRST) Player overhangs/“hovers over” the void on the RIGHT (SE) edge but not the LEFT (SW)

**Symptom:** on the front tier the avatar can walk past the down-right edge and stand out over empty void; the down-left edge correctly blocks. (See image 21 — circled right edge.)

**Root cause to confirm:** walkability is *cell-membership only*. `IslandData.IsWalkable(pos) = IsValidPosition(pos)` and `PlayerController.TryStep` allows a step whenever `WorldToGrid(candidate, CurrentTier)` still rounds to a valid cell. Because `WorldToGrid` rounds to the nearest cell *centre*, the avatar pivot can sit up to ~half a diamond past a cell’s visual edge and still “belong” to that edge cell → overhang. The L/R asymmetry comes from how the rounding + the axis-slide fallback in `TryStep` resolve near the SE edge (and possibly the SE void mapping onto an existing back-tier coordinate). Confirm by logging `fromCell`/`toCell`/`Tier` while walking off each edge.

**Fix direction (pick one, keep it simple + testable):**
- Add a sub-cell containment clamp: reject the candidate unless it lies inside the destination cell’s diamond footprint (|u|+|v| ≤ ~0.85 in cell-local diamond space), so the pivot can’t overhang into void. OR
- Gate on the cell *ahead of the pivot* (the leading edge), not just the pivot’s own cell.

**Files:** `Player/PlayerController.cs` (`TryStep`, `HandleMovement`, `CurrentTier`), `Island/IslandData.cs` (`IsWalkable`, `CanTraverse`, `Tier`), `Core/GridMath.cs`. Add an EditMode test in `Assets/Tests/EditMode/` proving symmetric blocking on all four edges of a tier-0 cell.

---

## 2. Cliff faces are missing on every VOID-facing edge (only the tier boundary has them)

**Symptom:** `IslandRenderer.BuildTierWalls()` draws a face only when a camera-facing neighbour (`+y` SW, `+x` SE) is a **valid lower-tier cell**. Edges facing **void** (no neighbour) are skipped → the island’s outer rim is bare flat diamonds with nothing below (image 19/21, right side; the “?” + up-arrow show the boundary wall also reads as floating because the right half of it has no continuation).

**Fix direction:** extend the per-edge logic so a face is also drawn when the camera-facing neighbour is **absent** (true void) — i.e. for `IsEdge` cells on their exposed `+x`/`+y` sides. Same directional generator (`ProcGfx.IsoTierFace`, SW = `rightSide:false`, SE = `rightSide:true`), but a **taller** skirt (the island underside, see #3), not a one-step tier wall. Make sure both the tier-boundary walls AND the rim undersides render with consistent sorting (currently `vis.SortBase + 1`).

**Files:** `Island/IslandRenderer.cs` (`BuildTierWalls`/`AddTierFace`/`FaceSprite` — currently only checks `myTier > Tier(neighbour)`; add the “neighbour absent” branch), `Core/ProcGfx.cs` (`IsoTierFace`).

---

## 3. Add the floating sky-island underside (visual only) along all border edges

**Goal:** the rim cells should grow a chunky **rocky cliff/underside** hanging below the play surface, so the island reads as a floating chunk of land — matching the repeatedly-referenced concept art (mossy rock sides, gritty painted underside; see `docs/superpowers/concept_art/`). Purely cosmetic — no gameplay cells below.

**Approach for now (placeholder, procedural):** for each `IsEdge` cell, on each void-facing edge from #2, draw a tall skirt (e.g. 1.5–2.5 world units) with a rock palette + downward darkening, optionally narrowing/irregular at the bottom so it doesn’t read as a clean box. This is the same `IsoTierFace` mechanism with a larger `faceHeightPx` and a rock tint; consider a dedicated `ProcGfx.IsoCliffUnderside` if the step-banding stair logic gets in the way.

**Later (preferred):** this is exactly where the **AI-painted backdrop** comes in — `tools/grid_template.py` already renders the grid as a ControlNet/alignment template (`artifacts/grid_template.png`); the painted underside can replace the procedural skirt once generated. Keep the procedural version as fallback.

**Files:** `Island/IslandRenderer.cs`, `Core/ProcGfx.cs`, `Island/IslandData.cs` (`IslandCell.IsEdge`).

---

## Workflow / gotchas (do not relearn the hard way)
- Build freshness = `Builds/Windows/SkyHarvest_Data/Managed/SkyHarvest.dll` mtime — **NOT** `SkyHarvest.exe` (engine bootstrap, rarely changes).
- Adding a **new `.cs`** needs the Unity build/shot run **twice** (first pass imports, compiles too late → `CS0103`); editing existing files is single-pass.
- GUI Unity launches hit an admin modal — `tools/shot.sh`/`verify.sh` already background `tools/dismiss-unity-admin-dialog.ps1`; `-batchmode` is exempt.
- `dotnet` = `~/.dotnet/dotnet.exe` (.NET 8). Fast compile-check: `dotnet build tools/clr-harness/GameCode/GameCode.csproj`.
- Don’t regress the tier gate: `Assets/Tests/EditMode/TierGateTests.cs` (123/123 must stay green).

## Acceptance
- Player cannot stand/overhang over void on **any** edge (symmetric), still climbs the stairs (E facing the stair) after mining.
- Every island edge (tier boundary **and** outer rim) has a cliff face; no bare floating diamond edges.
- Rim cells show a rocky underside hanging below; island reads as a floating chunk.
- `check.sh` green; `shot.sh` contact sheet confirms the look.
