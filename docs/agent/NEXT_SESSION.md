# Next session — task list

Execute in order. Branch off `main` (merge PR #3 first if still open), validate
(`bash tools/check.sh` after every C# change — see the `--no-build` gotcha in START_HERE),
screenshot-verify visual work (WORKFLOW.md), push to a branch + PR.

**Session theme: the cozy/pretty pass.** Patrick wants the game to look pretty and feel
cozy/peaceful. Spec §7 palette: dark earth + warm light accents ("warm island vs cool void").
Haiku's screenshot review (2026-06-13) said the foundation is clean but reads cold/industrial.

## 1. Warm lighting & palette pass (biggest cozy lever)

- Warm point-light feel around structures: forge glow, lantern amber on shelter/workshops —
  cheap 2D approach: additive soft-glow sprites (radial gradient, warm orange, low alpha)
  parented to structures; subtle alpha pulse for flicker.
- Background: replace flat near-black camera color with a subtle vertical gradient (moody
  dusk blue→charcoal) + a distant cloud layer (slow parallax drift) so the island floats in
  a sky, not a void. (Bootstrap builds the camera; add a background quad/canvas behind island.)
- Terrain warmth: nudge tile tinting toward warm earth on fertile cells (IslandRenderer
  applies per-cell sprites — add a slight warm `SpriteRenderer.color` tint variation).

## 2. Avatar & crop readability polish

- Avatar drop shadow (small dark ellipse sprite under player, ~40% alpha) — grounds the
  character, Haiku flagged it as "pasted on".
- Crop growth visibility: stronger per-stage visual difference + gentle sway on mature crops
  (SpriteAnimator already loops; add slight sine x-skew or scale pulse). Ripe crops should
  glow warm per spec ("golden crop glow").
- Check Haiku's claimed faint tile seams at green/grey diagonal boundaries (may be JPEG-y
  artifact — PR #2 supposedly fixed seams; verify at zoom 2 in a screenshot run).

## 3. Audio/ambience cozy layer (if time)

- `AudioCueSystem` exists — verify it has actual clips wired; add gentle ambient loop
  (wind + birds on ClearSkies, rain patter on LightRain) and soft chimes for workshop-done.
  Spec §5: island communicates via audio. Even 2–3 free CC0 loops would transform feel.

## 4. Leftover scope items (small)

- G6 hotbar mismatch (SCOPE_LEDGER): HUD draws 6 slots, only 1–4 wired. Either wire 5–6
  (seed slots?) or render only 4. Decide + do (15 min).
- Human playtest checklist (not automatable, 10 min with Patrick): WASD feel, scroll-zoom
  feel, mouse ghost placement, build-menu arrows, Tab/Esc in every panel combination.
  PlayModeVerify covers everything else (19/19 green 2026-06-13).

## Done-criteria

`tools/validate.sh` green; before/after screenshots (PlayModeScreenshots or PlayModeVerify)
showing the warmth pass; Haiku re-review says tone moved toward cozy; PR opened; this file +
START_HERE refreshed for session 4.
