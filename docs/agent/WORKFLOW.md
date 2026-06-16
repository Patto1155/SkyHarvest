# Workflow — exact commands

Repo root: `D:\APATPROJECTS\SkyHarvest`. Unity `2022.3.45f1` at `D:/Unity/Hub/Editor/2022.3.45f1/Editor/Unity.exe`.
`dotnet` = `~/.dotnet/dotnet.exe` (.NET 8 SDK 8.0.x, installed). The machine ALSO has
`C:\Program Files\dotnet` on PATH but that's only **SDK 3.1**, which fails the net8.0 stub
projects with `MSB3644`. `tools/check.sh` now **auto-locates `~/.dotnet/dotnet.exe`**, so it
works with no PATH tweak — if a past note says ".NET 8 isn't installed", that was a PATH
mistake, not a missing SDK. For manual `dotnet` calls, use `~/.dotnet/dotnet.exe` explicitly.

## THE gotcha: Unity admin dialog

This machine's only account is `Administrator`, so **every GUI Unity launch shows a modal "running as administrator" warning that blocks `-executeMethod`**. Unity checks group membership, not token — de-elevation does NOT help. Two facts:

- **`-batchmode` runs are EXEMPT** (no dialog). So `validate.sh` and the build run unattended fine.
- **Interactive / Play-mode runs need the dialog dismissed.** Run `tools/dismiss-unity-admin-dialog.ps1` alongside the launch — it UIAutomation-finds the dialog and DPI-aware-clicks "I wish to continue at my own risk". (Do NOT invoke it with `-ExecutionPolicy Bypass`; that's blocked. Run the cmdlets inline or via a normal `powershell -File`.)

## Fast logic loop (no Unity, ~10s)

```bash
bash tools/check.sh          # NUnit tests against Unity stubs (123 currently)
```

`check.sh` self-locates the .NET 8 SDK — no `export PATH` needed. If it reports
`error CS...` for a NEW UI script, the stub may be missing a Unity member: add it to
`tools/clr-harness/UnityStubs/Stubs.*.cs` (e.g. `PointerEventData.button`,
`RectTransformUtility`). Keeping the stubs complete is what lets the fast loop catch
compile errors in new UI code before a 6-min Unity run.

## Full validation before handoff (~1 min, batchmode, no dialog)

```bash
bash tools/validate.sh       # check.sh + Unity batch compile + EditMode tests
```

## Build the Windows standalone (batchmode, no dialog, ~30s after import)

```bash
bash tools/build.sh
# or manually:
"D:/Unity/Hub/Editor/2022.3.45f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath D:/APATPROJECTS/SkyHarvest \
  -executeMethod BuildScript.BuildWindows \
  -logFile artifacts/build.log
# success line in log: "[BuildScript] BUILD OK -> ...SkyHarvest.exe"
# output: Builds/Windows/SkyHarvest.exe
```

**Build freshness:** check `Builds/Windows/SkyHarvest_Data/Managed/SkyHarvest.dll` mtime —
not `SkyHarvest.exe` (the engine bootstrap rarely changes).

## Run the Windows build (play the game)

After a successful build:

```bash
bash tools/run.sh
# or manually (from repo root):
./Builds/Windows/SkyHarvest.exe
```

`run.sh` rebuilds first if `SkyHarvest.dll` is older than any `Assets/Scripts/**/*.cs`
file (use `bash tools/run.sh --no-build` to skip). The game opens **windowed 1280×720**.
Click **New Game** on the main menu to start.

**Controls (quick ref):** WASD move · E interact · Tab inventory · B build menu ·
1–9/0 hotbar · mouse drag items between hotbar and inventory when Tab is open ·
Esc cancel / pause.

No Unity admin dialog for the standalone `.exe` — that only affects editor launches.


## Visual verification (Play-mode screenshots) — NEEDS the dialog dismisser

GUI editor required (screenshots need a GPU context). Two concurrent steps:

1. Kill any running Unity + remove `Temp/UnityLockfile` first.
2. Launch: `Unity.exe -projectPath D:/APATPROJECTS/SkyHarvest -executeMethod PlayModeScreenshots.Run -logFile artifacts/playmode.log`
3. Concurrently run the dismisser watcher (`tools/dismiss-unity-admin-dialog.ps1`, ~150s timeout) to clear the modal.
4. Output: `artifacts/screenshots/frame_00{30,90,300,600}.png`. Unity self-exits when done.

`PlayModeScreenshots.cs` disables domain reload (or entering Play wipes the capture loop) and auto-clicks "New Game". Edit `CaptureFrames` / add input simulation there to test specific interactions.

## Fast cozy/visual iteration loop (NEW — use this for any look work)

Two tools make visual tuning fast and token-cheap:

1. **`Assets/StreamingAssets/visual.json`** — every warmth knob (sky colours, island
   radius, forge/lantern glow, golden-crop glow, warm earth tint, avatar shadow) is
   DATA, loaded by `VisualConfig` (`Assets/Scripts/Core/VisualConfig.cs`). Colours are
   `#RRGGBB`. **Editing this file needs NO C# recompile** — that's the whole point.
2. **`bash tools/shot.sh`** — launches the GUI editor, runs `PlayModeContactSheet.Run`
   (wide establishing + cozy detail framings stitched into ONE png), auto-dismisses the
   admin modal, and prints ONLY `PASS: <path>` or `FAIL`. The harness instant-builds a
   forge + a few ripe crops so the glow/golden-crop work is visible (a fresh New Game has
   neither). Output: `artifacts/screenshots/contact_sheet.png` — Read that one image.

Loop: edit `visual.json` → `bash tools/shot.sh` → Read the contact sheet → adjust. Run
it with a generous tool timeout (~8 min); it's a cold Unity boot each time.

⚠ **Two-pass gotcha for NEW `.cs` files:** the first Unity run after ADDING a script
(batchmode OR `shot.sh`) imports it but compiles too late → `CS0103 'X' does not exist`.
Just run the SAME command again; the second run compiles clean. Editing an EXISTING file
is single-pass. (Same root cause as the `validate.sh` first-run import.)

## Sprite art pipeline (Python/PIL — `tools/spritegen/`)

All in-game sprites are **procedurally drawn pixel art** (NOT AI-generated), written to
`Assets/Resources/Sprites/**` and loaded at runtime by `SpriteLoader` at PPU 64, pivot
bottom-centre. Per-module generators: `terrain/player/crops/structures/items/ui/fx/bg`.

Regenerate everything: `python -m tools.spritegen.generate_all`
Regenerate one module (fast): `python -c "from tools.spritegen import structures; structures.generate()"`
Render + view ONE sprite while iterating (don't regen the whole set):
```bash
python -c "from tools.spritegen import structures, core; \
  core.save_strip(structures.forge(),'structures/forge.png',128,128)"
# then Read Assets/Resources/Sprites/structures/forge.png (zoom via PIL .resize NEAREST)
```

**Isometric volume primitives (`tools/spritegen/core.py`)** — structures are drawn as 2:1
dimetric VOLUMES sitting on the 64×32 tile diamond, NOT flat front elevations. Use:
- `iso_box(c, cx, base_y, hw, hh, height, top, left, right)` — a prism (crate, posts, anvil).
- `iso_frustum(c, cx, base_y, hw_b, hh_b, hw_t, hh_t, height, top, left, right)` — a tapered
  box; `hw_t<hw_b` narrows (furnace/tower), `hw_t>hw_b` flares (funnel/barrel belly).
- `iso_top(c, cx, base_y, hw, hh, height, fn)` — a single diamond face (platform, lid, water).
- `fill_poly(c, [pts], fn)` — arbitrary convex iso face (roofs, A-frames, nets).

Face fns take **`(x, y, v)`** (screen x/y + v=0 at base→1 at top) or a flat colour. Light is
upper-LEFT: top brightest, left face mid, right face shadowed. Ready-made face painters:
`iso_stone_faces(ramp)`, `iso_plank_faces(ramp)`, `iso_metal_faces(ramp)` in `structures.py`.
`WARMSTONE_R` = the weathered warm furnace-stone ramp. The forge is the reference example
(tapered frustum body + voussoir arch via `_in_open`/`_in_outer` + centred hooded chimney).
`path` stays a flat ground overlay (correct — it lies on the tile, no volume).

## Delegate screenshot review to Haiku (token-efficient)

Spawn an Agent with `model: haiku`, point it at the `artifacts/screenshots/*.png`, give it the design intent (dark industrial-survival, distinct terrain types, seamless tiles) and ask for a ranked defect list. Don't burn main-model tokens eyeballing images.

## Git

Branch + PR only (direct `main` push is blocked). `gh` CLI is authed as `Patto1155`. Repo: `Patto1155/SkyHarvest`. User wants incremental pushes.
