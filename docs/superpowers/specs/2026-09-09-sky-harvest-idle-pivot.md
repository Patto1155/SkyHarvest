# Sky Harvest v2 — Idle Sky-Farm Pivot (Android, portrait)

**Date:** 2026-09-09
**Status:** Design agreed with Patrick; Phase 1 implementation in progress
**Supersedes:** `2026-03-17-sky-harvest-design.md` §6 (time system), §11 (no time-skips), and the
"no offline growth" thesis. Everything else in the old spec still applies unless contradicted here.

---

## 1. What changed and why

The March spec was an active-session farming game: crops paused when you left, weather punished you,
automation was a late reward. v2 keeps the world, the art direction, the real crop system (soil,
water, wind, crop tiers) and the island, but re-shapes the loop around **idle / offline progression**
so it works as a phone game you check in on.

Influences added: *Idle Obelisk Miner*, *Adventure Capitalist* (lanes, multipliers, offline earnings,
time-warp monetisation) and *SkyFactory 2* (sieve opening, storage networks, resource crops you
industrialise).

### Pillars

1. **Never punish absence.** Nothing is lost while you're away. Weather is an active-session
   opportunity (rain = growth boost, gale = debris frenzy), never an offline threat.
2. **Everything runs until a buffer saturates.** Offline, the island simulates until storage is
   full, the water network is dry, or the battery is dead. Every upgrade in the game extends one of
   those three buffers.
3. **Automation is the game, not the reward.** Each crop need (water, wind, harvest/replant, soil,
   power) gets an automation layer, unlocked in the order the player feels the pain.
4. **Real crops first, essence crops last.** Wheat/herbs/moss stay as they are. "Iron grows on
   plants" (SkyFactory/Mystical-Agriculture essence crops) is the late unlock behind the whole
   industrial chain.
5. **Portrait, one thumb, 90-second sessions.** Tap the tile, not the avatar. The avatar stays as a
   charming auto-walking "manager" that visibly walks the routes you've automated.

## 2. Progression ladder

| Tier | Unlock | What it adds |
|---|---|---|
| 0 | start | 3×4 two-tier starter island, hand tools, a few seeds, first debris |
| 1 | first hour | Rain catcher → **Water Tank** (bigger offline water buffer); **Sprinkler** (3×3 auto-water) |
| 2 | first storm | Windbreak → **Wind Totem** (3×3 protection, active-session only) |
| 3 | ~2h | **Tender's Post** — 3×3 auto-harvest into adjacent crate + auto-replant from seeds in it. Turns "a plot" into "a farm module". |
| 4 | mid | **Composter** — harvests feed it, it tops up soil nutrients in range. Closes the soil loop. |
| 5 | mid | **Windmill** (+ bonus on WindCorridor) and **Battery**. Tender's Posts draw power; battery size = offline runtime. |
| 6 | late | Brewhouse → Alchemist's Bench → **infused soil** → essence crops (iron root, coal moss). Each essence tier requires the previous industrial tier's output. |
| P | prestige | **The Blackstorm** — player-triggered reset: island breaks along chunk seams, you drift to a new procedurally rolled island keeping legendary seeds + blueprints. |

## 3. Offline model (the core of v2)

- Save stores `LastSeenUnixTime`. On Continue, elapsed real seconds are clamped to
  `OfflineCapSeconds` (4h base; upgrades/IAP extend) and the island is advanced in 60s steps by
  the pure-C# `IslandSim` (`Assets/Scripts/Sim/`).
- Offline environment is neutral: no rain, no wind, sun 0.75. Water comes only from the shared
  **WaterNetwork** (tanks + springs) via sprinklers. Unwatered crops simply pause.
- Each step: windmills → power, springs → water, sprinklers water plots, crops tick, Tender's
  Posts harvest/replant, composters top up soil, workshops finish their current batch.
- The run produces an `OfflineReport` (harvested items, replants, batches finished, first moment
  storage filled / water ran dry / power died). It drives the **Welcome Back** panel — the
  dopamine hit that opens every session.
- Skynets keep their existing real-time accrual (`SkynetAccrual`); this generalises that idea to
  the whole island.

### Buffers = monetisation surface

| Buffer | Free | Extended by |
|---|---|---|
| Offline cap | 4h | upgrades, IAP "long haul" |
| Storage | crate/barrel slots | more/bigger storage, silo |
| Water | 50 base + tanks | tanks, spring chunks |
| Power | 20 base + batteries | batteries |

Monetisation (later phase, all optional): time-warps (2h/8h), rewarded ad to double the
Welcome-Back yield, offline-cap extension, cosmetics/battle pass. Never sell yield multipliers.

## 4. Automation devices (Phase 1 data)

| id | cost | effect |
|---|---|---|
| `water_tank` | 4 wood, 3 scrap, 2 nails | +200 water capacity |
| `sprinkler` | 3 scrap, 2 nails, 1 rope | keeps 3×3 soil ≥ 40 water from the network; no power |
| `wind_totem` | 4 wood, 1 rope | 3×3 crops take no wind damage |
| `tenders_post` | 6 wood, 4 nails, 2 rope, 1 scrap | 3×3 auto-harvest → adjacent storage, auto-replant from its seeds; 0.1 power/s |
| `composter` | 4 wood, 2 stone | stores 1 compost per harvest in range; applies +20 nutrients to plots below 70 |
| `windmill` | 6 wood, 4 nails, 2 rope | +0.2 power/s (×2 on WindCorridor) |
| `battery` | 4 scrap, 4 nails | +100 power capacity |

Adjacency for "adjacent storage" is Chebyshev distance ≤ 1 (the 8 neighbours).

## 5. Island & expansion (unchanged intent, portrait framing)

Dimetric iso volumes, cliff faces, underside shadow, void below with clouds drifting under. Portrait:
island centre-screen, pinch zoom, vertical growth (terraces/tiers) reads better than horizontal.
Expansion paths: scaffold outward (exists), drift chunks with terrain rolls (new), terracing (tiers
exist), rare island collision (new). Blackstorm prestige breaks the island along chunk seams.

## 6. Phases

- **Phase 1 (this PR):** `Sim` core + tests, automation structures + data, save v2 with
  `LastSeenUnixTime`, offline catch-up on Continue, Welcome Back panel, active-session automation
  ticking, portrait/Android player settings, harness restored.
- **Phase 2:** tap-to-move / tap-to-interact, portrait HUD relayout, avatar auto-routes, time-warp
  hooks, push notification when storage caps.
- **Phase 3:** drift chunks, brewhouse/alchemist, infused soil + essence crops, windmill art, silo.
- **Phase 4:** Blackstorm prestige, monetisation SDKs, battle pass.

## 7. Out of scope (still)

Hubs/multiplayer, PvP, expeditions, UGC — unchanged from the March spec.
