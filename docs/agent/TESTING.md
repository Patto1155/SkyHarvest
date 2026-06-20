# Testing & dev mode — agent guide

Pick the **cheapest** loop that can answer your question. Full details for each tier below.

## Which loop when? (token-efficient)

| Question | Use | Time | Needs Unity GUI? |
|----------|-----|------|------------------|
| Did my C# compile? Logic still correct? | `bash tools/check.sh` | ~10s | No |
| Real Unity compiler + EditMode tests? | `bash tools/validate.sh` | ~1–6 min | Batch only |
| Did I break a gameplay system end-to-end? | `bash tools/verify.sh` | ~6 min | Yes (+ dialog dismisser) |
| Does it **look** right (cozy pass, tiles, glow)? | edit `visual.json` → `bash tools/shot.sh` | ~8 min | Yes |
| Stair art / cutout alignment? | `bash tools/stair-shot.sh` | ~5 min | Yes |
| Walk around, feel input, dev overlays? | `bash tools/run.sh -- --dev` | build ~30s + manual | No (standalone `.exe`) |
| Iterating C# while playing? | `bash tools/dev-watch.sh` | auto-rebuild ~30s | No |

**Default after C# changes:** `check.sh` → (if touching UI/Unity-only APIs) `validate.sh` → (if gameplay wiring) `verify.sh`.

**Do not** launch a 6–8 min Unity GUI run to answer something `check.sh` or a new EditMode test could prove.

---

## Tier 1 — Fast logic (`tools/check.sh`)

Compiles all game code + tests against **Unity stubs** (.NET 8 CLR harness). No editor.

```bash
bash tools/check.sh
```

- Tests live in `Assets/Tests/EditMode/` (NUnit).
- **134+ tests** currently; one known pre-existing edge pick in `GridMathTests`.
- `check.sh` runs `dotnet test --no-build` — after **adding a new test file**, rebuild once:
  ```bash
  ~/.dotnet/dotnet.exe build tools/clr-harness/Tests/Tests.csproj
  ```

### CLR harness gotchas

- Uses `~/.dotnet/dotnet.exe` (.NET 8). PATH `dotnet` may be 3.1 → `MSB3644`.
- **`Assets/Scripts/Dev/**`** and **`StairCutoutEditor.cs`** are **excluded** from the harness compile (GL/GUI/LoadImage). Stubs in `tools/clr-harness/GameCode/HarnessShims.cs` satisfy `Bootstrap` references under `SKYHARVEST_HEADLESS`.
- Missing Unity API in a new script? Add to `tools/clr-harness/UnityStubs/Stubs.*.cs`.

### Adding a test (preferred for new logic)

1. Add `Assets/Tests/EditMode/YourFeatureTests.cs` (pure logic, no Play mode).
2. `bash tools/check.sh`
3. Examples: `TierGateTests`, `StairWalkTests`, `SpringIrrigationTests`, `GridMathTests`.

Publish/subscribe and static helpers are easy to test here. Rendering and legacy `Input.*` are not.

---

## Tier 2 — Full validation (`tools/validate.sh`)

Pre-handoff gate: check.sh + Unity batch compile + Unity EditMode test runner.

```bash
bash tools/validate.sh
# flags: --skip-check | --skip-unity-compile | --skip-unity-tests
```

Logs: `artifacts/unity-compile.log`, `artifacts/unity-editmode.log`.

**Two-pass gotcha:** first Unity run after **adding a new `.cs`** imports but compiles too late → `CS0103`. Run the **same command again**.

See also `docs/VALIDATION.md`.

---

## Tier 3 — Live feature harness (`tools/verify.sh`)

Drives real game systems in **Play mode** via `Assets/Editor/PlayModeVerify.cs` — calls the same methods Bootstrap hotkeys use (not raw keyboard injection).

```bash
bash tools/verify.sh
```

- Output: `artifacts/verify/verify_report.md` + PNGs under `artifacts/verify/`.
- Expect **~6 min**; uses GUI Unity + `tools/dismiss-unity-admin-dialog.ps1`.
- Extend by adding steps in `PlayModeVerify.BuildSteps()` / new `Step*` methods.

Use when: inventory/build/farming/debris/save wiring — anything that needs MonoBehaviours alive.

---

## Tier 4 — Visual / screenshot loops

### Cozy / island look (JSON only, no C# recompile)

```bash
# edit Assets/StreamingAssets/visual.json
bash tools/shot.sh
# → artifacts/screenshots/contact_sheet.png (one stitched image)
```

Runs `PlayModeContactSheet.Run` — wide + detail framings, forge + ripe crops pre-placed.

### Generic timed frames

`PlayModeScreenshots.Run` — `artifacts/screenshots/frame_*.png`. Documented in `WORKFLOW.md`.

### Stair cutout framing

```bash
bash tools/stair-shot.sh
# → artifacts/screenshots/stair_verify.png
```

`PlayModeStairShot.Run` — dev session, carved stairs, camera on boundary.

**Review tip:** delegate PNG review to a cheap vision subagent; don't burn main-context tokens on pixels unless necessary.

---

## Tier 5 — Play the build (human or agent-launched)

### One-shot run

```bash
bash tools/build.sh
bash tools/run.sh -- --dev          # rebuild if scripts/sprites newer than DLL
bash tools/run.sh --no-build -- --dev
```

- Exe: `Builds/Windows/SkyHarvest.exe`
- **Freshness = `SkyHarvest_Data/Managed/SkyHarvest.dll` mtime**, not `.exe`.
- Standalone has **no** Unity admin dialog.

### Auto-rebuild on save

```bash
bash tools/dev-watch.sh
```

Polls `Assets/Scripts` + sprite folders; rebuilds + relaunches with `--dev` (~30s per change). Not hot-reload.

---

## Dev mode (`--dev`)

Pass **`--dev`** on the standalone exe (or `--stair-edit` for layout editor only).

### What `--dev` enables

| Flag / key | Effect |
|------------|--------|
| `--dev` | Skip main menu → `StarterIsland` New Game with **stairs pre-carved**, player at `(1,3)` |
| **F3** | Toggle dev debug panel (`Dev/DevDebugPanel.cs`) |
| **F8** | Toggle stair cutout **layout mode** (blocks WASD/hotbar while active) |
| `--stair-edit` | F8 editor only, without full dev session shortcuts |

Dev debug panel toggles (F3 UI): diamond walk bounds, terrain category fill, player/cursor walk probes, magenta stair corridor, show both tiers.

Implementation: `Bootstrap.StartDevSession()`, `DevDebugPanel`, `DevDebugOverlay`, `StairCutoutEditor`.

### Stair layout editor (F8)

- **IJKL** — nudge cutout · **right-drag** — move · **U/O** scale · **T/G** width · **Y/H** height
- **F6/F7** — base height · **F5** — save to `StreamingAssets/stair_cutout_layout.json` (+ project mirror)
- **F9** — reload layout

### Agent workflow for movement / hitbox bugs

1. `bash tools/run.sh -- --dev` (or `dev-watch.sh`)
2. **F3** — confirm green crosshair inside cyan/orange diamonds; red X = outside walk volume
3. Magenta band = `StairWalkMath` corridor; should cover stair path to upper tier
4. Fix logic in `PlayerController` / `StairWalkMath` / `GridMath` → `check.sh` with `StairWalkTests`

---

## Editor scripts reference

| Script | Launch | Purpose |
|--------|--------|---------|
| `BuildScript.BuildWindows` | `tools/build.sh` | Windows standalone (batchmode) |
| `PlayModeVerify.Run` | `tools/verify.sh` | Feature matrix + report |
| `PlayModeContactSheet.Run` | `tools/shot.sh` | Cozy contact sheet |
| `PlayModeStairShot.Run` | `tools/stair-shot.sh` | Single stair screenshot |
| `PlayModeScreenshots.Run` | manual Unity `-executeMethod` | Timed frame captures |

All under `Assets/Editor/`. Batchmode builds **skip** the admin dialog; GUI runs need the dismisser (see `WORKFLOW.md`).

---

## What still needs a human

Documented in `NEXT_SESSION.md` §0 — cannot automate with legacy Input:

- Movement **feel**, scroll-zoom, ghost placement UX
- Save/Continue emotional check ("does my farm feel restored?")
- Audio loudness at real speakers
- Subjective cursor/glow readability

Add **EditMode tests** or **PlayModeVerify steps** when a human check repeats — that's how the suite stays token-efficient over time.

---

## Related docs

- `WORKFLOW.md` — exact paths, Unity version, admin-dialog gotcha, sprite pipeline
- `VALIDATION.md` — validate.sh flags and troubleshooting
- `MAP.md` — where Bootstrap, island, farming, dev scripts live
- `NEXT_SESSION.md` — current manual playtest checklist
