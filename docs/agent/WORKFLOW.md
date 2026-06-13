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

## Delegate screenshot review to Haiku (token-efficient)

Spawn an Agent with `model: haiku`, point it at the `artifacts/screenshots/*.png`, give it the design intent (dark industrial-survival, distinct terrain types, seamless tiles) and ask for a ranked defect list. Don't burn main-model tokens eyeballing images.

## Git

Branch + PR only (direct `main` push is blocked). `gh` CLI is authed as `Patto1155`. Repo: `Patto1155/SkyHarvest`. User wants incremental pushes.
