# Workflow — exact commands

Repo root: `D:\APATPROJECTS\SkyHarvest`. Unity `2022.3.45f1` at `D:/Unity/Hub/Editor/2022.3.45f1/Editor/Unity.exe`.
`dotnet` = `~/.dotnet/dotnet.exe` (.NET 8). NOT `C:\Program Files\dotnet` (only SDK 3.1, fails).

## THE gotcha: Unity admin dialog

This machine's only account is `Administrator`, so **every GUI Unity launch shows a modal "running as administrator" warning that blocks `-executeMethod`**. Unity checks group membership, not token — de-elevation does NOT help. Two facts:

- **`-batchmode` runs are EXEMPT** (no dialog). So `validate.sh` and the build run unattended fine.
- **Interactive / Play-mode runs need the dialog dismissed.** Run `tools/dismiss-unity-admin-dialog.ps1` alongside the launch — it UIAutomation-finds the dialog and DPI-aware-clicks "I wish to continue at my own risk". (Do NOT invoke it with `-ExecutionPolicy Bypass`; that's blocked. Run the cmdlets inline or via a normal `powershell -File`.)

## Fast logic loop (no Unity, ~10s)

```bash
export PATH="/c/Users/Administrator/.dotnet:$PATH"
bash tools/check.sh          # 73 NUnit tests against Unity stubs
```

## Full validation before handoff (~1 min, batchmode, no dialog)

```bash
bash tools/validate.sh       # check.sh + Unity batch compile + EditMode tests
```

## Build the Windows standalone (batchmode, no dialog, ~30s after import)

```bash
"D:/Unity/Hub/Editor/2022.3.45f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath D:/APATPROJECTS/SkyHarvest \
  -executeMethod BuildScript.BuildWindows \
  -logFile artifacts/build.log
# success line in log: "[BuildScript] BUILD OK -> ...SkyHarvest.exe"
# output: Builds/Windows/SkyHarvest.exe   (smoke-test: launch, confirm alive, kill)
```

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

## Delegate screenshot review to Haiku (token-efficient)

Spawn an Agent with `model: haiku`, point it at the `artifacts/screenshots/*.png`, give it the design intent (dark industrial-survival, distinct terrain types, seamless tiles) and ask for a ranked defect list. Don't burn main-model tokens eyeballing images.

## Git

Branch + PR only (direct `main` push is blocked). `gh` CLI is authed as `Patto1155`. Repo: `Patto1155/SkyHarvest`. User wants incremental pushes.
