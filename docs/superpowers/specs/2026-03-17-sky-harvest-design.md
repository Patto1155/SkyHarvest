# Sky Harvest — Game Design Specification

**Working Title:** Sky Harvest
**Date:** 2026-03-17 (updated from 2026-03-14 draft)
**Status:** Design Complete — ready for implementation planning

---

## 1. Core Identity

A persistent sky island survival-farming game where players start on a tiny floating platform with almost nothing, build a working farm against harsh conditions, process goods through workshops, and eventually earn access to shared hub worlds for trading, PvP, and community.

**Pitch:** "Stardew Valley's farming depth meets Rust's survival stakes on a Skyblock floating island, with shared sky market hubs."

**Platform:** Unity, shallow dimetric 2.5D (~20-25°), mobile-first with PC-quality art
**Dev Setup:** Solo + AI
**Future Vision:** UGC platform with AI-generated game modes in hub zones

**Core Loop:** scavenge → plant → grow → harvest → process → trade/use → expand → repeat

**What makes it addictive:**

- **Chain completion is the primary hook** — workshops humming, outputs feeding inputs, production lines growing longer and more satisfying. The "factory running" dopamine.
- **Staggered harvests** create a baseline heartbeat — always something 2 minutes from ready
- **Weather windows** create urgency spikes — storms punctuate calm farming with "oh shit" moments
- Every crop matters early on (Skyblock scarcity)
- Processing chains add depth without complexity (light crafting)
- Hubs are an earned reward that opens a whole new game
- "My sad little platform became a thriving sky farm" is the identity fantasy

**Session Feel:** Calm efficiency. The player is an engineer admiring a system that works, not a waiter scrambling to keep up. Urgency comes from weather, not from workshops. The baseline mood is meditative tending punctuated by natural tension.

**Session Flexibility:**
- 5-15 min mobile check-ins (tend crops, collect outputs, grab debris)
- 30-60 min core sessions (full farm cycle, workshop chains, weather a storm)
- 1-2 hour deep sessions (major builds, multi-chain processing)
- 10+ hour endgame sessions (future — seasonal events, hub trading runs)

**Tone:** Gritty and beautiful. The sky is gorgeous but not safe. Crops are hard-won. Storms can ruin a harvest. Warm art style, harsh world.

**Player Interaction:** Avatar-based. The player controls a character who walks around their island, approaches objects to interact with them. The same avatar represents the player in hub worlds.

**Key Influences:**

- Skyblock — start from nothing, every resource matters, scarcity-driven progression
- Rust — survival tension, stakes, the world is dangerous
- Stardew Valley — farming depth, crop variety, processing chains, satisfying growth
- Factorio/Satisfactory — the "factory humming" feeling is the primary addiction loop

---

## 2. The Island

### Procedural Generation

Your island is a procedurally generated floating landmass with unique terrain. No two islands are the same. One player might get a flat mesa, another a craggy spire, another a tiered plateau with a natural spring.

### Terrain Types

- **Fertile valleys** — best soil, but flood risk in storms
- **Rocky plateaus** — safe from flooding, good for workshops, poor soil
- **Cliff edges** — dangerous, but debris lands here. Best scavenging spots
- **Natural springs** — rare, huge advantage for irrigation if your island rolls one
- **Wind corridors** — exposed areas where wind funnels between rock formations. Bad for crops, good for windmills

### Building & Construction

- **Blueprint ghost system** — place a translucent ghost → deliver materials → construction completes. Lets you plan layouts before committing resources.
- **No relocation** — demolish and rebuild (lose some materials). Placement decisions are permanent and meaningful.
- **Organic placement** — structures placed freely within the isometric space, snapping to sensible positions via a hidden coarse grid with terrain-aware offsets. Similar to Banished or Northgard.
- **Terrain interaction** — a greenhouse nestled against a rock wall, a mill on a hilltop, terraced crop rows down a slope. Buildings conform to the land.
- **Vertical layering** — build up with scaffolding, walkways, carved-out cliff workshops. Your farm grows UP as well as out.
- **Elevation matters** — lower areas collect water naturally, hilltops get more wind/sun, cliff edges are risky but give debris access.
- **Island silhouette** — your island's shape and structures become your visual identity, visible to other players at hubs.

### Island Expansion

- Reinforce crumbling edges to stop land loss
- Build scaffolding outward to extend your platform
- Bridge to small nearby debris chunks that drift close and lock on
- Rare event: a whole new mini-island collides with yours, dramatically expanding your land (but possibly damaging structures)

### Design Philosophy

Each island is a different puzzle: "How do I make the most of THIS piece of sky rock?" rather than "place optimal tiles in optimal order."

---

## 3. Debris & Scavenging

### Debris Mechanics

Debris crashes onto specific landing zones on your island (cliff edges, plateaus). An audio thud + visual cue alerts the player. Debris sits for 1-2 minutes, then crumbles or blows away. The player walks over and interacts to scavenge.

**Loot table:** scrap, ore, coal, seeds, wood, rope, rare finds. Weighted by weather type — storms bring better materials, calm weather brings common salvage.

### The Skynet

A craftable structure placed on island edges that passively catches debris floating past at higher altitudes — debris the player couldn't otherwise reach.

**Progression:**

| Tier | Name | Materials | Catches |
|------|------|-----------|---------|
| 1 | Basic Skynet | Rope + timber frame | Common drift debris (scrap, wood, rope) |
| 2 | Reinforced Skynet | Forged iron frame + woven fiber | Uncommon debris (ore, seeds, coal) |
| 3 | Magnetic Skynet | Forge parts + rare magnets | Metal-rich debris (rare ore, mechanical parts) |
| 4 | Ethereal Skynet | Alchemist components + crystal | Rare high-altitude debris (legendary seeds, blueprints, unique materials) |

Skynets are checked periodically like a mailbox — fits the calm efficiency vibe. Early game: one basic net on a cliff edge. Late game: an array of specialised nets forming a loot pipeline feeding production chains.

**Key design loop:** Forge better nets → catch better debris → get better materials → forge better nets.

---

## 4. Farming & Workshops

### Farming (70% of gameplay)

#### Soil System

Soil is not equal. Different patches have different quality — rich loam, sandy gravel, rocky clay. You improve soil over time through composting, crop rotation, and amendments. Neglect it and it depletes.

#### Crop Needs

- **Water** — rain catches, irrigation channels carved downhill from springs, manual watering early on. Overwater and roots rot.
- **Sunlight** — some crops need full exposure, others need shade. Tall structures cast shadows that matter.
- **Wind protection** — exposed crops get damaged in storms. Windbreaks, walls, and greenhouse frames protect them but cost resources.
- **Soil nutrients** — monoculture depletes the soil. Rotate crops or compost to keep fertility up.
- **Weather patterns** — not calendar seasons, but the sky has weather cycles. Dry spells, monsoon periods, calm windows. Players learn to plan around them.

#### Crop Tiers

- **Starter crops** — hardy, low yield, fast growing. Barely keep you going. (Sky moss, wind grass, cloud root)
- **Staple crops** — reliable, medium yield, need decent soil. The backbone of your farm. (Storm wheat, iron barley, sun squash)
- **Specialty crops** — slow growing, high value, demanding conditions. Your trade goods. (Void peppers, crystal melon, thunder bloom)
- **Legendary crops** — extremely rare seeds found from debris or hub traders. Need perfect conditions. Massive trade value or unique properties. Status symbols.

#### Crop Timing (game-time)

- Starter crops: 2-5 minutes
- Staple crops: 10-20 minutes
- Specialty crops: 30-60 minutes
- Legendary crops: multiple sessions

#### Animals

- Sky chickens from nests that blow onto your island
- Cloud goats tamed from debris encounters
- Bees kept in hives — pollination boosts crop yield nearby
- Animals need shelter, feed (from your crops), and care. Neglect them and they decline or leave.

### Workshops (30% of gameplay)

Workshops are buildings placed on your island that transform raw farm output into higher-value goods. Processing times are long enough that you're never rushed — load inputs, walk to the next station, tend crops, and by the time you circle back, things are ready.

#### Workshop Types

| Workshop | Function | Processing Time | Notes |
|----------|----------|-----------------|-------|
| Drying Rack | Dry herbs, cure meat, make preserves | 10-30 sec | First workshop. Outdoor, weather-dependent — rain ruins drying batches |
| Stone Mill | Grain to flour, seeds to oil, ore to dust | 15-45 sec | Indoor, reliable |
| Forge | Tools, structural reinforcements, mechanical parts, Skynet upgrades | 30-60 sec | Needs fuel (charcoal or coal from debris) |
| Brewhouse | Tinctures, tonics, fermented drinks | 45-90 sec | Time-based processing |
| Loom | Fiber crops into cloth, rope, sails | 20-40 sec | Used for expansion scaffolding and trade |
| Kitchen | Cooking combines farm + workshop outputs | 30-60 sec | Best trade goods in the game |
| Alchemist's Bench | Rare ingredient combinations | 45-90 sec | Late game. Produces potions, dyes, Skypillar components |

#### Processing Chains

Short but meaningful:

- Wheat → Mill → Flour → Kitchen → Bread (4 steps, solid trade value)
- Thunder bloom → Drying rack → Dried petals → Alchemist → Storm tonic (rare, high value)
- Iron ore (debris) → Forge → Nails → used for building expansion
- Fiber crop → Loom → Cloth → trade or used for greenhouse frames
- Debris scrap → Forge → Skynet frame → catch better debris (self-reinforcing loop)

No conveyor belts. No routing logistics. You carry things between stations or build storage nearby for convenience. Depth comes from **what to grow and what to make**, not how to connect machines.

### Automation (late Phase 2 reward)

Automation is earned, not given. Only worthwhile once your farm is large enough to justify it.

- **Sprinklers** — crafted from forge parts + pipes. Auto-water nearby plots. First taste of automation.
- **Scarecrows / wind totems** — passive crop protection. Saves manual repair after storms.
- **Harvest drones** — small mechanical helpers from rare debris parts. Collect ripe crops, deposit in nearby storage. Expensive to build and maintain.
- **Workshop feeders** — small hoppers attached to workshops that pull from adjacent storage. Mill auto-grinds flour if there's wheat in the chest next to it.
- **Animal feeders** — auto-dispense feed from storage. Animals still need check-ins but won't starve if you're away.

**Key rule:** Automated systems need maintenance — they break down, need parts, consume power from windmills. They save time, not attention.

### Storage

- **Crates** — basic, cheap, small capacity. First storage. Place anywhere.
- **Barrels** — for liquids, fermented goods, oils. Needed next to brewhouse.
- **Silos** — large grain/seed storage. Tall, visible, become part of your island's silhouette.
- **Cold cellar** — dug into rock if terrain allows. Preserves perishables longer.
- **Tool racks / shelves** — wall-mounted storage for workshops.
- **Shipping crate** — special storage for goods marked for trade. This is what you bring to the hub.

Storage placement matters — workshops pull from adjacent storage, so layout decisions have real gameplay impact.

---

## 5. Weather & Survival

### Weather System (core feature)

Weather is not cosmetic — it directly affects gameplay every session. Implemented as a simple state machine with weighted random transitions. Weather state changes every 5-10 minutes of game time.

| Weather | Effect |
|---------|--------|
| Clear skies | Best growing, good for drying racks, but driest — crops need watering |
| Light rain | Ideal. Waters crops, fills rain catchers, no damage |
| Heavy storms | Wind damages exposed crops, rain floods low areas, lightning strikes tall structures |
| Gale winds | Debris arrives more frequently, but unsecured items blow off edges. Crops without windbreaks shredded |
| Fog banks | Low visibility, slow crop growth, but rare cloud creatures appear. Good for taming |
| Sky drought | Extended dry period. Springs slow, catchers empty, soil cracks. Ration water or lose crops |
| The Blackstorm | Rare, dangerous. Near-zero visibility, extreme wind, lightning everywhere. Significant damage possible. But aftermath debris is the rarest and most valuable in the game |

### Player-Readable Cues

**The island tells you what's happening through audio and visuals, not HUD elements.**

- **Weather telegraphing** — ambient cues (cloud colour, wind direction, air pressure changes) telegraph the next weather state 1-2 minutes before transition. Experienced players learn to read the sky.
- **Workshop completion** — chime or clang when done. Each workshop has a distinct sound.
- **Crop readiness** — gentle rustle when ready to harvest. Visual: ripe colour change, slight sway.
- **Debris arrival** — distant thud + visual impact dust. Direction indicates which island edge.
- **Skynet collection** — subtle ping when a net catches something.
- **Danger cues** — crops droop when thirsty, wilt when diseased, structures creak when damaged.

**No HUD pips or notification icons.** The player develops intuition for their island's state through feel. For mobile: subtle haptic feedback for key events (harvest ready, workshop done, debris landed).

### Threats (future features)

Designed but deferred. Architecture should support adding these without reworking core systems:

- **Structural decay** — island edges erode over time. Unreinforced platforms crumble. Land loss if maintenance ignored.
- **Pests** — sky mites eat crops, rust beetles damage metal structures, cloud moths target stored grain. Preventable with companion planting, sealed storage, pest traps.
- **Debris impacts** — incoming debris occasionally damages structures or crops. Usually minor, occasionally devastating.
- **Resource scarcity** — some materials only come from debris or hubs. Full self-sufficiency isn't possible forever.

### Survival Philosophy

**No hunger/thirst bars.** Those are tedious. Survival is about keeping your **farm** alive:

- **Crop health** — are plants watered, protected, rotated, pest-free?
- **Workshop condition** — are machines maintained, fueled, sheltered?
- **Power supply** — windmills and generators power automation and lighting. No power = no sprinklers, no feeders, no drones.
- **Storage security** — are perishables preserved? Is grain sealed? Is your shipping crate stocked?
- **Structural integrity** — is your island holding together? Are platforms reinforced?

The survival pressure is about **your systems failing**, not your character dying. You don't respawn — your farm decays. That's more stressful than a death screen.

---

## 6. Time System

### Hybrid Clock

- **Game-time** for farming and workshops — time only passes while playing. Crops grow, workshops process, weather changes only when the game is active.
- **Slow real-time** for debris and weather state — between sessions, debris accumulates on landing zones and Skynets, and weather may shift. When you return, your island has changed slightly — giving you a reason to check in.
- **No offline crop growth** — crops pause when you're away. This prevents the idle-game trap and keeps farming intentional.

### Session Design

The hybrid clock supports flexible play:

| Session Type | Duration | What Happens |
|-------------|----------|-------------|
| Quick check-in | 5-15 min | Collect Skynet debris, water crops, queue workshops |
| Core session | 30-60 min | Full harvest cycle, process a batch, build something, weather a storm |
| Deep session | 1-2 hours | Major build project, multi-chain processing, island expansion |
| Marathon (future) | 10+ hours | Seasonal events, hub trading runs, endgame progression |

**Workshop timing is calibrated for calm circuits:** load a workshop, walk to the next, tend some crops, circle back. Nothing finishes so fast you must rush, nothing takes so long you idle.

---

## 7. Art Direction

### Visual Identity

Derived from concept art. The look is **dark industrial-survival with warm light punctuating the gloom.**

#### Palette

- **Base:** Dark earth tones — charcoal, rust, timber brown, deep stone grey
- **Warmth:** Forge orange, golden crop glow, lantern amber, ember red
- **Accent:** Occasional cool tones — Skypillar purple/blue (the only "magical" colour), storm grey, fog white
- **Sky:** Moody overcast as default. The island glows against a dark void. Clear skies are bright but never cheerful — always a hint of danger.

#### Material Language

Everything on the island looks **built by hand from salvage:**

- Rough-hewn timber, not clean lumber
- Rusted iron, not polished steel
- Frayed rope, not synthetic cord
- Cracked stone, not smooth masonry
- Hand-stitched cloth, not factory fabric

Nothing is new. Nothing is clean. Everything has been repaired, reused, or improvised.

#### Lighting

- **Warm point lights** are the emotional anchor — forge glow, lanterns, crop shimmer
- **Ambient light** is cool and moody — overcast sky, fog, storm clouds
- **The contrast** between warm island and cool void is what makes it feel like home in a hostile sky
- **Weather affects lighting dramatically** — storms darken everything, clear skies warm the palette, fog desaturates

#### UI Style

- **Painterly card-style** with metallic frames (per icon concepts)
- **Rich textures** — leather, parchment, stamped metal. Not flat/minimal.
- **Icons feel like collectibles** — detailed, warm, tactile
- **HUD is minimal** — just equipped tool, hotbar, time indicator. Everything else is world-space.
- **Touch-first design** — 48px minimum tap targets, thumb-zone navigation, no hover-dependent interactions

### Camera

- **Shallow dimetric angle (~20-25°)** — dramatic cliff faces, imposing buildings, fortress-in-the-sky feel
- **3D models** rendered in the dimetric view — rich lighting, shadows, weather particle effects
- **Fixed camera** — no rotation for MVP. Zoom in/out only.
- **Compensations for compressed ground:** good camera zoom range, clear visual differentiation between crops/structures, strong colour coding for crop states

---

## 8. Tutorial & Onboarding

### Discovery-Driven

- **No tutorial sequence.** No scripted first day. No hand-holding.
- **Contextual tooltips** appear on first interaction with each system — brief, helpful, then gone.
- **The island teaches you.** Rain falls and crops grow. You figure out the connection.
- **Tool tips on hover/inspect** provide details when you need them, not before.
- **Failure is a teacher** — losing crops to a storm teaches weather protection better than any tutorial.

### First 10 Minutes (emergent, not scripted)

The player spawns on a small island with:
- A few basic tools
- Some starter seeds
- One water source
- Debris on the nearest cliff edge

What they do first is up to them. Most will explore, find the debris, plant something, and start building. The game trusts the player.

---

## 9. Player & Interface

### Player Interaction Model

**Avatar-based.** The player controls a character who walks around their island in dimetric view.

- **Movement** — walk around the island using virtual joystick (mobile) or WASD/arrow keys (PC)
- **Interaction** — approach an object and tap/press interact. Context-sensitive: interact with a crop to water/harvest, interact with a workshop to open its crafting menu, interact with debris to scavenge, interact with Skynet to collect
- **Carrying** — the player character has a personal inventory. Items are carried between plots, storage, and workshops manually (until automation is unlocked)
- **Tools** — equippable tools for specific actions (watering can, hoe, hammer, sickle). Tool quality improves through forge upgrades
- **Building** — enter build mode to place blueprint ghosts. Deliver materials to complete construction.

The same avatar represents the player in hub worlds — walking through markets, entering arenas, joining expeditions.

### UI & HUD

**Principle:** Minimal HUD. The island communicates through audio and visual cues, not meters.

**Always visible (HUD):**
- Equipped tool
- Quick-slot inventory bar (hotbar)
- Time of day indicator
- Minimap / island overview toggle

**On-demand (inspect/tap):**
- Crop health, growth stage, water level, soil quality — shown when looking at a specific plot
- Workshop status — current recipe, progress, fuel level — shown when near or interacting
- Storage contents — shown when opened
- Structure condition — shown when inspected

**World-state visuals (no UI needed):**
- Weather — sky colour, cloud movement, wind particles, rain/lightning effects
- Crop health — visual wilting, colour changes, growth stages visible on the model
- Soil quality — visual difference between rich dark soil and depleted cracked ground
- Water levels — visible irrigation channels, puddles, dry/wet soil appearance

---

## 10. Hubs & Multiplayer

### The Skypillar

A craftable portal structure requiring materials from across all production chains — rare crops, forged components, alchemical reagents. Building it is a milestone that proves you've engaged with every system.

When activated, you choose which hub to travel to. Your shipping crate contents come with you as trade inventory. Your farm continues in the background — real-time debris/weather still happens, automation still runs, things can still go wrong.

### Hub Types

#### The Sky Market (trade hub — first hub unlocked)

- Large neutral floating bazaar. Always populated.
- Player-run stalls — set prices, display goods, browse others' shops
- Auction house for rare items — seeds, blueprints, legendary crop harvests
- NPC merchants fill gaps — sell basic supplies, buy surplus at low prices
- Economy is player-driven. If nobody's growing thunder bloom, price goes up naturally.
- **Safe zone** — no PvP, no stealing, no griefing

#### The Proving Grounds (PvP hub)

- Unlocked after first hub visit + crafting a combat token
- Neutral staging area for queuing and challenging

**Arena modes:**

- **1v1 Duels** — wager trade goods or rare items
- **Team Skirmishes** — small group fights on procedural floating platforms
- **Storm Survival** — players dropped on a shared island during a Blackstorm. Last farm standing wins. Building and defending simultaneously.
- **King of the Drift** — fight over a debris field for rare salvage. PvP + scavenging combined.

Better forge output = better equipment. Your farm directly powers your PvP strength. Losses cost what you wagered, not your farm. Your island is always safe.

#### The Expedition Board (co-op hub)

- Optional group content
- Sign up for timed expeditions to **wild islands** — large, dangerous, resource-rich floating landmasses
- 2-4 players, scavenge rare materials, survive harsh conditions, extract before time runs out
- Rewards: materials unavailable elsewhere — rare seeds, unique blueprints, cosmetic items
- Not required for progression — a faster path to rare stuff and a way to play with friends

#### The Commons (social hub — future feature)

- Visit other players' islands, share blueprints, show off farms
- Leaderboards — biggest harvest, most efficient farm, rarest crops, PvP rankings
- Blueprint sharing — upload farm layouts for others to reference
- Community boards — trade requests, expedition recruitment

### Multiplayer Architecture

- **Your island is yours.** Nobody visits without invitation (future). Nobody raids it. Solo experience is sacred.
- **Hubs are shared servers.** Lightweight — players as avatars in a market or staging area, not full simulation.
- **Solo-first design.** If servers go down, you can still farm. Hub features are additive, not required.
- **Hub progression feeds back to island.** Blueprints, rare seeds, and recipes from hubs unlock new things at home. The two loops reinforce each other.

### UGC Platform (future feature)

- Architecture designed to support user-created game modes in hub zones
- Future vision: AI-generated game modes where players describe a mode in natural language and AI generates it
- Core systems built to be composable and extensible for this purpose
- Clean, decoupled code with event-driven systems from day one. Defer explicit plugin/scripting architecture to post-MVP.

---

## 11. Monetization

### Model: Free-to-Play with Seasonal Battle Pass

Base game is completely free. No paywalls on gameplay.

#### What You Sell

- **Season Pass** — cosmetic reward track. Island themes (autumn leaves, volcanic rock, crystal formations), workshop skins, tool appearances, avatar outfits, market stall decorations.
- **Cosmetic Shop** — standalone skins outside the pass. Island banners, weather overlays (cherry blossoms in wind, glowing rain), unique Skypillar designs.
- **Emotes & Flair** — hub-only social expressions, titles, profile badges.

#### What You Never Sell

- Gameplay advantage — no premium seeds, no faster growth, no bonus resources
- Time skips — if crops take 20 minutes, everyone waits 20 minutes
- Storage or island space — expansion is earned, not bought

### Seasons (6-8 week cycles)

Each season brings:

- New cosmetic pass (~30 tiers, free track + premium track)
- Themed weather event (e.g., "The Ember Winds" — warm season with amber storms)
- Limited-time cosmetic crop variants
- Seasonal hub activities — special PvP mode, expedition variant, market festival

### Progression (non-monetized)

- **Island expansion** — gather materials, build outward
- **Crop discovery** — seeds through debris, trading, expeditions
- **Workshop unlocks** — each requires specific milestones
- **Skynet tiers** — forge upgrades unlock better nets
- **Hub access** — Skypillar requires cross-chain crafting
- **Hub reputation** — trade volume, PvP wins, expeditions unlock exclusive blueprints
- **Prestige** (future) — reset island with bonuses, keep cosmetics and knowledge

---

## 12. MVP Scope

### What's In

MVP = Phase 1 + early Phase 2, single player only.

A player can:

- Generate a unique procedural island with varied terrain, elevation, soil types
- Scavenge debris that lands on the island + build a basic Skynet
- Plant, water, tend, and harvest starter and staple crops
- Experience weather that matters — rain waters crops, storms damage them, wind affects exposed areas
- Build structures via blueprint ghost system — shelter, rain catchers, windbreaks, paths
- Place and use first 3 workshops — drying rack, stone mill, forge
- Process crops into basic goods (wheat → flour, herbs → dried herbs)
- Store items in crates and barrels
- Expand island edges with scaffolding
- Save and load their farm
- Hear and see the island's state through audio/visual cues (no HUD notifications)

### What's Not In

- Hubs / multiplayer (Phase 3)
- Automation (late Phase 2)
- Animals (mid Phase 2)
- Advanced workshops (brewhouse, loom, kitchen, alchemist)
- Advanced Skynet tiers (magnetic, ethereal)
- Battle pass / seasons
- PvP, co-op, expeditions
- Threats beyond weather (pests, decay, debris impacts)
- The Blackstorm event
- UGC / AI game modes
- The Commons social hub

### Success Criteria

The MVP works if a player can sit down and lose 2 hours without noticing:

1. **The island feels like a place** — not a grid, not a menu. A little world with character.
2. **Farming has tension** — weather forces real decisions. "Do I plant now or wait for the storm to pass?"
3. **Processing feels like a machine humming** — workshop chains create the "one more thing" loop.
4. **Expansion is visible** — the island after 2 hours looks dramatically different from the start.
5. **Players want to keep going** — the calm efficiency loop pulls into "just let me finish this chain" territory.

### Technical Foundation (build for the future)

Even though the MVP is single player, architect for what's coming:

- **Modular entity system** — crops, workshops, storage, structures, Skynets are all components. Easy to add animals, automation, new building types later.
- **Event-driven weather** — weather broadcasts events that any system can listen to. Today crops respond. Tomorrow structural decay, pests, and power systems can hook in.
- **Audio event system** — workshops, crops, and debris emit audio cues. System must be extensible for new structure types.
- **Inventory/storage abstraction** — generic enough to support shipping crates and hub trading later.
- **Island serialization** — save format that can sync to a server when multiplayer arrives. Hybrid clock state (game-time + real-time markers) persisted.
- **Clean, decoupled architecture** — event-driven, interface-based systems. Foundation for future UGC scripting layer. No explicit plugin API until core loop is validated.

---

## Summary

| Aspect | Decision |
|--------|----------|
| Theme | Floating sky islands, gritty survival farming |
| Perspective | Shallow dimetric 2.5D (~20-25°) |
| Engine | Unity / C# |
| Platform | Mobile-first with PC-quality art, PC added later |
| Core loop | Scavenge → plant → grow → harvest → process → expand |
| Addiction hook | Chain completion (factory humming), staggered harvests, weather urgency |
| Session feel | Calm efficiency — meditative tending with weather-driven tension spikes |
| Session length | Flexible: 5 min check-ins → 10+ hr endgame |
| Time system | Hybrid clock — game-time farming, slow real-time debris/weather |
| Island | Procedural terrain with elevation, organic placement, vertical building |
| Building | Blueprint ghost placement, demolish only (no relocation) |
| Debris | Landing zones + Skynet (passive → tiered upgrades via forge) |
| Farming (70%) | Soil quality, crop needs, weather interaction, crop tiers |
| Workshops (30%) | Processing chains, 7 types, calm-paced timing |
| Automation | Late Phase 2 reward, maintenance-heavy, earned not given |
| Weather | Simple state machine, 7 types, 5-10 min transitions |
| Feedback | Audio + visual cues only — no HUD notifications |
| Survival | Weather-driven, no hunger bars, farm health is what matters |
| Art direction | Dark earth + warm light, salvaged materials, painterly UI |
| Camera | Fixed, zoom only, shallow dimetric with 3D models |
| Tutorial | Discovery-driven, contextual tooltips, no hand-holding |
| Hubs | Earned via Skypillar — market, PvP, co-op, social |
| Multiplayer | Solo island is sacred, hubs are shared, solo-first design |
| Architecture | Clean/decoupled from day one, defer UGC to post-MVP |
| Monetization | F2P + cosmetic battle pass, no pay-to-win |
| MVP | Single player, weather + farming + 3 workshops + basic Skynet |
