# Next session — task list

Execute in order. Branch off `main` — note `feat/bugfixes-and-session-prs` (commit `6c98a7e`)
is sitting uncommitted-to-main and **not yet merged**; merge or rebase onto it first, don't
redo its work. Validate (`bash tools/check.sh` after every C# change — see the `--no-build`
gotcha in START_HERE, **and** the .NET 8 SDK gotcha below), visual-verify via the fast loop
(`bash tools/shot.sh` + `visual.json` — see WORKFLOW.md), `tools/verify.sh` before handoff, PR.

**Session 7 (playtest bugfixes + PR1/2/4/6/7/9/10) is DONE — see START_HERE.md.** Patrick
explicitly rejected PR3 (tool hints), PR5 (debris toast), PR8 (crop water alert) — don't
re-propose these without him asking.

## 0. Fix `tools/check.sh` (.NET 8 SDK gotcha — blocks fast unit-test validation)

- The sandboxed shell used in session 7 didn't have `dotnet` on PATH by default
  (`C:\Program Files\dotnet\dotnet.exe` exists but isn't found without an explicit PATH add).
  Even with it added, the installed SDK is **3.1.201** but `tools/clr-harness/UnityStubs/
  UnityStubs.csproj` targets **net8.0** → `MSB3644: reference assemblies for .NETFramework
  Version=v8.0 were not found`. Either install a .NET 8 SDK, or retarget the stub project to
  whatever's actually installed. Until fixed, fall back to `tools/verify.sh` (live Unity
  Play-mode harness) for validation — slower (~6 min) but it did catch a real bug this
  session (see START_HERE session-7 "Bug caught by the verify harness").

## 1. Fix `PlayModeVerify.cs` for the two-tier starter island (test-harness gaps, not gameplay bugs)

- `StepSkynet` filters for `c.Terrain == TerrainType.CliffEdge`, which doesn't exist anywhere
  on `StarterIsland` (only `FertileValley`/`RockyPlateau`) → always fails with "no free
  cliff-edge cell on this island". Either add a `CliffEdge` cell to `StarterIsland.Build`, or
  relax the predicate.
- `StepTillSow` calls the no-seed-arg `FarmingActions.TrySow(plot, player)`, which only sows
  if the seed item is the **currently selected hotbar slot** — but `StepTools` (which runs
  earlier) leaves slot 0 (Hoe) selected and nothing ever selects the wheat-seed slot. Add a
  `hotbar.SelectSlot(<seed slot>)` call, or switch to the explicit
  `TrySow(plot, player, "wheat_seed")` overload. This cascades into Grow/Harvest also failing.
- Full detail + exact line numbers: SCOPE_LEDGER.md "New known test-harness gaps".

## 2. Human playtest of session 7's fixes (5 min, can't be automated)

- Confirm the N/S facing fix actually feels right — walk toward the forge, face the stair,
  press E to carve, walk up. Confirm tilling works past the old "2-3 tile" ceiling.
- Confirm Continue restores the starter island correctly: New Game → carve stairs → till a
  few tiles → Save+Quit → Continue → stairs still carved, tiles still tilled, player on the
  correct tier. This is PR2's whole point and `PlayModeVerify` doesn't cover save/continue.
- Confirm the new cursor + tile glow read well at actual screen resolution (subjective —
  Patrick asked for this explicitly, ProcGfx sprites were only checked via code-read + build).
- Confirm the ambient audio actually plays from the very first frame of New Game (PR7) and
  doesn't sound harsh/loud — values were tuned by ear in a prior session, not re-validated.

## 4. Keep tuning the look (cheap now — JSON only, no recompile)

- `visual.json` is the dial board. Open `screenshots/cozy_pass_after.png` for the current state.
  Likely next nudges: clouds still subtle (raise `cloudAlpha`/`cloudCount`); the forge glow can
  feel blobby (tune `glowRadius`/`glowAlpha`); fertile tiles a touch flat (try a warmer
  `warmEarthTint` or higher `earthTintStrength`). Edit → `bash tools/shot.sh` → look.
- ~~Depth is the remaining art gap~~ ✅ DONE: session 6 added directional cliff faces
  (`IslandRenderer.BuildTierWalls`/`AddTierFace`/`AddRimFace`); session 7 added the underside
  shadow disc (PR6). If it still doesn't read as "floating" at actual resolution, the next
  lever is the rim-face colors/`RimFaceH` in `IslandRenderer.cs`, not a structural change.

## 5. Audio/ambience cozy layer

- `AudioCueSystem` exists and is wired (procedural per-weather drone + SFX tones for every
  major action; session 7's PR7 made sure the drone starts on the very first frame). What's
  still **procedural sine waves, not real samples** — if the cozy feel needs actual wind/bird/
  rain CC0 audio clips instead of synthesized tones, that's the remaining gap here. Spec §5.

## 6. The debris → skynet → expansion loop (gameplay) — ✅ DONE (session 5)

- ~~Spec §3: debris lands on cliff edges; craftable **Skynet** passively catches debris ("checked
  like a mailbox"); debris + scaffolding EXPAND the island outward.~~ **CLOSED on `feat/debris-loop`.**
  Audit found the chain was already wired end-to-end; fixed two real defects: (1) Skynet offline
  accrual was dead (`InitializeOfflineAccrual` had no real caller — only the verify harness) → wired
  into save-restore, math extracted to pure tested `SkynetAccrual`; (2) `IslandExpandedEvent`
  double-fired → removed the `BuildModeController` duplicate. Proven 20/20 in Play mode
  (`tools/verify.sh`) + 11 new unit tests. See START_HERE session-5 notes.
- **Follow-on backlog (not built):** debris/skynet/expansion have no audio cue of their own beyond
  the scavenge tone; expansion has no "scaffold cost scales with island size" balancing; Skynet
  tiers 2–4 are deferred by design (spec §12).

## 7. Leftover scope items (small)

- ~~G6 hotbar mismatch~~ ✅ DONE (session 4): unified Stardew-style hotbar (4 tools + 6 items,
  number keys 1-9/0, selected-slot highlight, stack counts). Also fixed latent G12 — bare-ground
  tilling was never wired to runtime; the player can now till the faced cell with the Hoe.
- ~~G8 minimap stub~~ ✅ DONE (session 7): real top-down dot map + player marker.
- Human playtest checklist, folded into item 2 above for session 7's specific changes; still
  outstanding from earlier sessions: scroll-zoom feel, ghost placement, build-menu arrow nav.

## Done-criteria

`tools/validate.sh` green; before/after via `tools/shot.sh`; PR opened; this file + START_HERE
refreshed.
