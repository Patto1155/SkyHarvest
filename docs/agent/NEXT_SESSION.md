# Next session — task list

Execute in order. Branch off `main` (merge the cozy-pass PR first if still open), validate
(`bash tools/check.sh` after every C# change — see the `--no-build` gotcha in START_HERE),
visual-verify via the fast loop (`bash tools/shot.sh` + `visual.json` — see WORKFLOW.md), PR.

**Session 3 (cozy/warm pass) is DONE.** Sky gradient + clouds, warm earth tint, forge/shelter
glow, golden ripe-crop glow + sway, avatar shadow, compact ~4×3 starter island — all driven by
`Assets/StreamingAssets/visual.json` and verifiable in one screenshot via `tools/shot.sh`.

## 1. Keep tuning the look (cheap now — JSON only, no recompile)

- `visual.json` is the dial board. Open `screenshots/cozy_pass_after.png` for the current state.
  Likely next nudges: clouds still subtle (raise `cloudAlpha`/`cloudCount`); the forge glow can
  feel blobby (tune `glowRadius`/`glowAlpha`); fertile tiles a touch flat (try a warmer
  `warmEarthTint` or higher `earthTintStrength`). Edit → `bash tools/shot.sh` → look.
- **Depth is the remaining art gap:** the island is a flat diamond — the concept art's chunky
  rocky CLIFF SIDES need a side/elevation sprite per edge cell (not just top tiles). Bigger job;
  needs art. Consider a simple dark "rock skirt" sprite under edge cells as a cheap approximation.

## 2. Audio/ambience cozy layer

- `AudioCueSystem` exists — verify it has actual clips wired; add a gentle ambient loop (wind +
  birds on ClearSkies, rain patter on LightRain) + soft chimes for workshop-done / crop-ripe.
  Spec §5: the island communicates via audio. 2–3 free CC0 loops would transform the feel.

## 3. The debris → skynet → expansion loop (gameplay) — ✅ DONE (session 5)

- ~~Spec §3: debris lands on cliff edges; craftable **Skynet** passively catches debris ("checked
  like a mailbox"); debris + scaffolding EXPAND the island outward.~~ **CLOSED on `feat/debris-loop`.**
  Audit found the chain was already wired end-to-end; fixed two real defects: (1) Skynet offline
  accrual was dead (`InitializeOfflineAccrual` had no real caller — only the verify harness) → wired
  into save-restore, math extracted to pure tested `SkynetAccrual`; (2) `IslandExpandedEvent`
  double-fired → removed the `BuildModeController` duplicate. Proven 20/20 in Play mode
  (`tools/verify.sh`) + 11 new unit tests. See START_HERE session-5 notes.
- **Follow-on backlog (not built):** debris/skynet/expansion have no audio cue of their own beyond
  the scavenge tone; expansion has no "scaffold cost scales with island size" balancing; Skynet
  tiers 2–4 are deferred by design (spec §12). Depth/cliff-side art (task 1) still the big visual gap.

## 4. Leftover scope items (small)

- ~~G6 hotbar mismatch~~ ✅ DONE (session 4): unified Stardew-style hotbar (4 tools + 6 items,
  number keys 1-9/0, selected-slot highlight, stack counts). Also fixed latent G12 — bare-ground
  tilling was never wired to runtime; the player can now till the faced cell with the Hoe.
- Human playtest checklist (10 min with Patrick): WASD feel, scroll-zoom, ghost placement,
  build-menu arrows, Tab/Esc in every panel combo, **new: number-key hotbar select + hold-seed
  plant + hoe-till-bare-ground**. PlayModeVerify covers the rest (19/19).

## Done-criteria

`tools/validate.sh` green; before/after via `tools/shot.sh`; PR opened; this file + START_HERE
refreshed.
