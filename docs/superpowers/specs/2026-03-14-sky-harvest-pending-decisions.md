# Sky Harvest — Pending Design Decisions

**Companion to:** `2026-03-17-sky-harvest-design.md`
**Date:** 2026-03-14 (created) / 2026-03-17 (all resolved)
**Status:** All decisions resolved

---

## 1. Debris Mechanics — RESOLVED

**Decision:** Landing zones. Debris crashes onto specific island spots (cliff edges, plateaus), sits for 1-2 min with audio/visual alert, player walks over to scavenge. Plus the **Skynet** — a passive craftable structure on island edges that catches orbital debris. Progresses from basic (rope + timber) to ethereal (alchemist components) through forge upgrades.

---

## 2. Building & Construction Mechanics — RESOLVED

**Decision:** Blueprint ghost system. Place translucent ghost → deliver materials → construction completes. No relocation — demolish and rebuild (lose some materials). Hidden coarse grid with terrain-aware snapping.

---

## 3. MVP Target Platform — RESOLVED

**Decision:** Mobile-first with PC-quality art. Build for the tighter constraint (mobile touch, 48px targets, thumb-zone UI) with the higher quality bar (3D models, rich lighting from concept art). PC added later as an easy win — more screen real estate, keyboard shortcuts, higher render settings.

---

## 4. Resource Timing & Economy — RESOLVED

**Decision:** Hybrid clock. Game-time for farming/workshops (pauses when offline). Slow real-time for debris accumulation and weather shifts between sessions. Flexible session design: 5-15 min check-ins, 30-60 min core sessions, 1-2 hr deep sessions, 10+ hr endgame (future). Workshop timing calibrated for calm walking circuits, not rushed scrambling.

---

## 5. Weather Model (Technical) — RESOLVED

**Decision:** Simple state machine with weighted random transitions. All 7 weather types (Clear, Light Rain, Heavy Storm, Gale, Fog, Drought, Blackstorm). State changes every 5-10 minutes of game time. Ambient cues telegraph next state 1-2 min ahead. Evolve toward seasonal drift and player-influenced weather post-MVP.

---

## 6. Isometric Visual Reference — RESOLVED

**Decision:** Shallow dimetric (~20-25°) with 3D models rendered isometrically. Fixed camera, zoom only, no rotation for MVP. Dramatic cliff faces, imposing buildings. Compensate for compressed ground with good zoom range and strong visual differentiation.

---

## 7. Tutorial & Onboarding — RESOLVED

**Decision:** Discovery-driven. No tutorial sequence. Contextual tooltips on first interaction with each system. The island teaches you through cause and effect. Failure is a teacher.

---

## 8. Plugin-Ready Architecture — RESOLVED

**Decision:** Clean, decoupled code with event-driven systems from day one. Interface-based architecture as good practice. Defer explicit plugin API and UGC scripting to post-MVP. Don't over-engineer for modding before the core loop is validated.
