# Sky Harvest — Pending Design Decisions

**Companion to:** `2026-03-14-sky-harvest-design.md`
**Date:** 2026-03-14
**Status:** Open questions to resolve during or after MVP development

---

## 1. Debris Mechanics

How does scavenging physically work?

**Questions to resolve:**
- Does debris float past the island edge on a visible trajectory? Land on the island? Appear at cliff edges?
- What does the player do to scavenge? Walk to it and interact? Use a tool? Timed grab before it drifts away?
- What's in debris? Need a loot table — at minimum for MVP: scrap, ore, coal, seeds, wood, rope, rare finds
- How frequently does debris arrive? Per real-time minute? Tied to weather? Random intervals?
- Does the Blackstorm aftermath change the loot table quality/quantity?

**Leans:** Debris drifts past island edges, player walks to the edge and interacts before it floats away. Gale winds increase frequency. Loot table weighted by weather type.

---

## 2. Building & Construction Mechanics

How does placing structures work?

**Questions to resolve:**
- Blueprint system? Unlock blueprints, then place a ghost, then supply materials?
- Or instant placement if you have materials in inventory?
- Can structures be relocated, or only demolished and rebuilt?
- What are the resource costs for basic structures (shelter, rain catcher, windbreak, paths)?
- How does "organic placement with smart snapping" actually work? Hidden grid with terrain offsets? Fully free-form with collision checking?
- Tech tree / unlock order for structures?

**Leans:** Blueprint ghost placement (place ghost → supply materials → construction completes). Hidden coarse grid with terrain-aware snapping for practical implementation.

---

## 3. MVP Target Platform

Which platform(s) to build for first?

**Questions to resolve:**
- PC-first, then port? Or simultaneous PC + mobile?
- Browser (WebGL) at MVP or deferred?
- Input abstraction: build for mouse/keyboard first and add touch later, or abstract from day one?

**Leans:** PC-first for MVP. Cleaner development, easier playtesting. Build input abstraction layer early so mobile port isn't a rewrite. Browser deferred to post-MVP.

---

## 4. Resource Timing & Economy

How long do things take? Real-time or game-time?

**Questions to resolve:**
- Do crops grow in real-time (like mobile games) or game-time (like Stardew)?
- What's the target session length? 15-minute mobile bursts or 2-hour PC sessions?
- Starter crop growth time: minutes or hours?
- How many crops can a player reasonably manage before needing automation?
- Workshop processing time: instant, seconds, minutes, or real-time hours?
- Water consumption rate vs rain catcher refill rate?

**Leans:** Game-time for MVP (time only passes while playing). Starter crops: ~2-5 minutes. Staple crops: ~10-20 minutes. Workshop processing: 10-60 seconds depending on recipe. Designed for 30-60 minute sessions that chain into longer ones.

---

## 5. Weather Model (Technical)

How does the weather system actually work under the hood?

**Questions to resolve:**
- Markov chain with weighted transitions? Hidden state machine?
- What determines transition timing? Fixed intervals? Random within ranges?
- How far in advance can a skilled player "read" the weather?
- Does weather differ by island position (elevation, terrain)?
- MVP weather set: all 7 types, or reduced to 4-5 for faster validation?

**Leans:** State machine with weighted random transitions. Weather state changes every 5-10 minutes of game time. Ambient cues (cloud colour, wind direction) telegraph the next state 1-2 minutes before transition.

---

## 6. Isometric Visual Reference

What exactly does "isometric 2.5D" mean for this game?

**Questions to resolve:**
- Camera angle: true isometric (30°) or dimetric?
- Camera rotation: fixed or player-rotatable?
- Rendering: sprite-based or 3D models rendered isometrically?
- Vertical building: how are elevation layers rendered and interacted with?
- Visual reference targets: which existing games look closest to the goal?

**Leans:** Fixed-angle true isometric. 3D models rendered isometrically (Unity makes this straightforward). Camera zoom but no rotation for MVP.

---

## 7. Tutorial & Onboarding

How does a new player learn the game?

**Questions to resolve:**
- Guided tutorial? Contextual hints? Learning by discovery?
- Is there a narrative framing? (Why are you on this island?)
- First 10 minutes experience: scripted or emergent?

**Leans:** Minimal contextual hints (tooltips on first interaction with each system). No heavy tutorial. Discovery-driven with a light narrative hook ("you crashed here, survive").

---

## 8. Plugin-Ready Architecture

When and how to design for UGC extensibility?

**Questions to resolve:**
- Build interface-driven architecture from day one, or refactor post-MVP?
- What systems need to be extensible vs hardcoded?
- Scripting language for UGC: C# runtime compilation? Lua? Visual scripting?

**Leans:** Build clean, decoupled code with event-driven systems (good practice regardless). Defer explicit plugin/scripting architecture to post-MVP. Don't over-engineer for UGC before core loop is validated.
