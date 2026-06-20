# Next session — task list

Execute in order. Branch off `main`. Validate: `bash tools/check.sh` after C# changes;
`bash tools/verify.sh` before handoff. Fast visual loop: `tools/shot.sh` + `visual.json`.

**Session 8 (stair cutout + corridor climb + dev debug) is DONE — commit `11e9e51`.**
Session 8b (automation pass) landed the harness fixes, spring irrigation MVP, visual/audio
nudges, and doc refresh below.

## 0. Human playtest (needs Patrick — ~5 min)

- Stair climb: walk lower tier → carve (E) → climb to forge tier → step onto upper neighbours.
- Save/continue on starter island: stairs carved + tilled tiles persist.
- F3 debug overlay: both tiers visible, magenta corridor matches art.
- F8 stair layout editor (`--dev`): nudge cutout if art still misaligned.
- Cursor + tile glow at real resolution; ambient volume (tuned softer in session 8b).

## 1. Optional polish (code or JSON)

- `visual.json` — further cozy nudges after `shot.sh` review.
- Real CC0 audio samples for ambient/SFX (still procedural sine waves; spec §5).
- Debris/Skynet/expansion dedicated audio cues (follow-on from session 5).
- Feel tuning: scroll-zoom, ghost placement, build-menu arrow nav.

## 2. Backlog (post-MVP per spec §12)

- Skynet tiers 2–4, advanced workshops, automation, animals, hubs/multiplayer.
- Sunlight/shade simulation (G11, deferred).
- Irrigation channels carved downhill (G9 partial — passive spring drip done; channels not built).
- Expansion cost scaling with island size.

## Done-criteria

`tools/validate.sh` green; before/after via `tools/shot.sh`; PR if batching; refresh this file when scope shifts.
Testing loops: **`docs/agent/TESTING.md`**.
