# Next session — task list

> **Session 8 (2026-09-09) pivoted the game to an Android idle sky-farm** — see START_HERE.md and
> `docs/superpowers/specs/2026-09-09-sky-harvest-idle-pivot.md`. Phases 1 and 2 are done on
> `claude/laughing-mayer-3s61u8` (PR #10): offline sim, 7 automation devices + sprites, save v2,
> Welcome Back panel, tap-to-move, portrait HUD, real-time pacing, editor compile-checking.
> **The list immediately below is the priority now; everything after it is older and secondary.**
>
> ## Do this first — verify in Unity
> Nothing in the pivot has been seen running; there is no Unity editor in the agent environment.
> 1. `tools/verify.sh` → read `artifacts/verify/verify_report.md`. Four steps are new
>    (tap-to-move, tender's post, sprinkler, offline catch-up) and have never executed.
> 2. Play it in portrait (Game view 720×1280): does tapping a tile walk the avatar and act? Is the
>    hotbar reachable with a thumb? Do the 7 device sprites read at actual size?
> 3. The full idle loop by hand: New Game → till + water plots → build `crate` (seeds inside) +
>    `tenders_post` next to them → Save+Quit → wait 5 min → Continue → Welcome Back should list
>    harvests and replants. With no tender's post it should still say "N crops ripened".
>
> ## Phase 3 — the rest of the idle game (spec §2 ladder, §6)
> 1. **Balance pass.** Nothing is tuned: `OfflineCapSeconds` (4h), device costs, sprinkler flow,
>    `TendersPost.PowerPerSecond`, windmill generation. Play an hour, then tune. The Skynet and
>    crop timers are the two dials that decide whether a 90-second check-in feels worth it.
> 2. **Early-game idle income.** First offline income currently needs the forge (nails). Rain
>    catchers now trickle water offline, but the sprinkler still needs nails. Consider the spec's
>    Tier-0 **sieve** (tap dirt → pebbles/seeds) as the pre-forge loop, or a nail-free starter
>    sprinkler. This is the single biggest open design question for retention.
> 3. **Storage-full notification** (Android local notification when the offline buffer caps).
> 4. **Time-warp / rewarded-double hooks** on the Welcome Back panel — pure logic first
>    (`OfflineReport` × multiplier), SDK later. Never sell yield multipliers (spec §3).
> 5. **Drift chunks** — small islands that drift past and can be anchored, each bringing a terrain
>    roll (this is how a player without a spring eventually gets one). Spec §5.
> 6. **Essence crops** (spec §2 tier 6): brewhouse → alchemist's bench → infused soil → iron root.
>    The "iron grows on plants" payoff, deliberately gated behind the whole industrial chain.
> 7. **Blackstorm prestige** (spec §2): island breaks along chunk seams, reroll terrain, keep
>    legendary seeds and blueprints.
>
> ## Known rough edges left behind
> - `TapController.NearestReachable` runs a BFS per candidate cell — O(cells²) on a tap that lands
>   somewhere unreachable. Fine at 12–50 cells, needs a single flood-fill if islands get big.
> - Panels are still laid out in absolute offsets inside a centred rect; only the HUD chrome is
>   edge-anchored. A tall narrow panel style would use the portrait screen much better.
> - `AutomationSystem` rebuilds the whole `IslandSim` on any place/demolish/plant/harvest. Cheap
>   now, but it is a full `FindObjectsOfType<CropPlot>` scan each time.
> - The keybind overlay (H) still documents the desktop scheme as primary.

Execute in order. Branch off `main` — note `feat/bugfixes-and-session-prs` (commit `6c98a7e`)
is sitting uncommitted-to-main and **not yet merged**; merge or rebase onto it first, don't
redo its work. Validate (`bash tools/check.sh` after every C# change — see the `--no-build`
gotcha in START_HERE, **and** the .NET 8 SDK gotcha below), visual-verify via the fast loop
(`bash tools/shot.sh` + `visual.json` — see WORKFLOW.md), `tools/verify.sh` before handoff, PR.

**Session 7 (playtest bugfixes + PR1/2/4/6/7/9/10) is DONE — see START_HERE.md.** Patrick
explicitly rejected PR3 (tool hints), PR5 (debris toast), PR8 (crop water alert) — don't
re-propose these without him asking.

## 0. ~~Fix `tools/check.sh`~~ ✅ DONE (session 8)

The root cause was not the SDK: `tools/clr-harness/**/*.csproj` matched the `*.csproj` gitignore
rule and had never been committed, so the projects `check.sh` builds did not exist in a fresh
clone. They are now tracked via a gitignore exception, `check.sh` builds the Tests and EditorCode
projects too, and on Linux `apt-get install -y dotnet-sdk-8.0` is the only setup needed.

### Original (stale) note, kept for context

- The sandboxed shell used in session 7 didn't have `dotnet` on PATH by default
  (`C:\Program Files\dotnet\dotnet.exe` exists but isn't found without an explicit PATH add).
  Even with it added, the installed SDK is **3.1.201** but `tools/clr-harness/UnityStubs/
  UnityStubs.csproj` targets **net8.0** → `MSB3644: reference assemblies for .NETFramework
  Version=v8.0 were not found`. Either install a .NET 8 SDK, or retarget the stub project to
  whatever's actually installed. Until fixed, fall back to `tools/verify.sh` (live Unity
  Play-mode harness) for validation — slower (~6 min) but it did catch a real bug this
  session (see START_HERE session-7 "Bug caught by the verify harness").

## 1. ~~Fix `PlayModeVerify.cs` for the two-tier starter island~~ ✅ DONE (verified session 8)

Both gaps are closed: `StarterIsland` does emit `CliffEdge` cells at the two front corners
(so `StepSkynet` finds one), and `StepTillSow` moves the seed into a hotbar slot and selects it
before sowing. `PlayModeVerify` is now compile-checked by `tools/check.sh`. Original notes:

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
