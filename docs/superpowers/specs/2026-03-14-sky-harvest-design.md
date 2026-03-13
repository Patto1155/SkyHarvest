# Sky Harvest — Game Design Specification

**Working Title:** Sky Harvest
**Date:** 2026-03-14
**Status:** Draft

---

## 1. Core Identity

A persistent sky island survival-farming game where players start on a tiny floating platform with almost nothing, build a working farm against harsh conditions, process goods through workshops, and eventually earn access to shared hub worlds for trading, PvP, and community.

**Pitch:** "Stardew Valley's farming depth meets Rust's survival stakes on a Skyblock floating island, with shared sky market hubs."

**Platform:** Unity, isometric 2.5D, cross-platform (PC, mobile, browser)
**Dev Setup:** Solo + AI
**Future Vision:** UGC platform with AI-generated game modes in hub zones

**Core Loop:** scavenge → plant → grow → harvest → process → trade/use → expand → repeat

**What makes it addictive:**

- Every crop matters early on (Skyblock scarcity)
- Weather and hazards create real tension (Rust stakes)
- Processing chains add depth without complexity (light crafting)
- Hubs are an earned reward that opens a whole new game
- "My sad little platform became a thriving sky farm" is the identity fantasy

**Tone:** Gritty and beautiful. The sky is gorgeous but not safe. Crops are hard-won. Storms can ruin a harvest. Warm art style, harsh world.

**Player Interaction:** Avatar-based. The player controls a character who walks around their island, approaches objects to interact with them. The avatar also represents the player in hub worlds. Similar to Stardew Valley / Harvest Moon but in isometric view.

**Key Influences:**

- Skyblock — start from nothing, every resource matters, scarcity-driven progression
- Rust — survival tension, stakes, the world is dangerous
- Stardew Valley — farming depth, crop variety, processing chains, satisfying growth
- Factorio/Mindustry — light automation as a late-game reward (not the core identity)

---

## 2. The Island

### Procedural Generation

Your island is a procedurally generated floating landmass with unique terrain. No two islands are the same. One player might get a flat mesa, another a craggy spire, another a tiered plateau with a natural spring.

### Terrain Types

- **Fertile valleys** — best soil, but flood risk in storms
- **Rocky plateaus** — safe from flooding, good for workshops, poor soil
- **Cliff edges** — dangerous, but debris snags here first. Best scavenging spots
- **Natural springs** — rare, huge advantage for irrigation if your island rolls one
- **Wind corridors** — exposed areas where wind funnels between rock formations. Bad for crops, good for windmills

### Building & Placement

- **Organic placement** — structures placed freely within the isometric space, snapping to sensible positions but not locked to a rigid grid. Similar to Banished or Northgard.
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

## 3. Farming & Workshops

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

#### Animals

- Sky chickens from nests that blow onto your island
- Cloud goats tamed from debris encounters
- Bees kept in hives — pollination boosts crop yield nearby
- Animals need shelter, feed (from your crops), and care. Neglect them and they decline or leave.

### Workshops (30% of gameplay)

Workshops are buildings placed on your island that transform raw farm output into higher-value goods.

#### Workshop Types

| Workshop | Function | Notes |
|----------|----------|-------|
| Drying Rack | Dry herbs, cure meat, make preserves | First workshop. Outdoor, weather-dependent — rain ruins drying batches |
| Stone Mill | Grain to flour, seeds to oil, ore to dust | Indoor, reliable |
| Forge | Tools, structural reinforcements, mechanical parts | Needs fuel (charcoal or coal from debris) |
| Brewhouse | Tinctures, tonics, fermented drinks | Time-based processing, some recipes take real-time hours |
| Loom | Fiber crops into cloth, rope, sails | Used for expansion scaffolding and trade |
| Kitchen | Cooking combines farm + workshop outputs | Best trade goods in the game |
| Alchemist's Bench | Rare ingredient combinations | Late game. Produces potions, dyes, Skypillar components |

#### Processing Chains

Short but meaningful:

- Wheat → Mill → Flour → Kitchen → Bread (4 steps, solid trade value)
- Thunder bloom → Drying rack → Dried petals → Alchemist → Storm tonic (rare, high value)
- Iron ore (debris) → Forge → Nails → used for building expansion
- Fiber crop → Loom → Cloth → trade or used for greenhouse frames

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

## 4. Weather & Survival

### Weather System (core feature)

Weather is not cosmetic — it directly affects gameplay every session.

| Weather | Effect |
|---------|--------|
| Clear skies | Best growing, good for drying racks, but driest — crops need watering |
| Light rain | Ideal. Waters crops, fills rain catchers, no damage |
| Heavy storms | Wind damages exposed crops, rain floods low areas, lightning strikes tall structures |
| Gale winds | Debris flies in faster (good scavenging) but unsecured items blow off edges. Crops without windbreaks shredded |
| Fog banks | Low visibility, slow crop growth, but rare cloud creatures appear. Good for taming |
| Sky drought | Extended dry period. Springs slow, catchers empty, soil cracks. Ration water or lose crops |
| The Blackstorm | Rare, dangerous. Near-zero visibility, extreme wind, lightning everywhere. Significant damage possible. But aftermath debris is the rarest and most valuable in the game |

**Weather has patterns, not schedules.** Players learn to read the sky — cloud colour, wind direction, air pressure shown through ambient cues, not UI meters. Experienced players anticipate what's coming.

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

## 5. Player & Interface

### Player Interaction Model

**Avatar-based.** The player controls a character who walks around their island in isometric view.

- **Movement** — walk around the island using keyboard (WASD/arrow keys) or tap-to-move on mobile
- **Interaction** — approach an object and press interact (or tap on mobile). Context-sensitive: interact with a crop to water/harvest, interact with a workshop to open its crafting menu, interact with debris to scavenge
- **Carrying** — the player character has a personal inventory. Items are carried between plots, storage, and workshops manually (until automation is unlocked)
- **Tools** — equippable tools for specific actions (watering can, hoe, hammer, sickle). Tool quality improves through forge upgrades
- **Building** — enter build mode to place structures. Details TBD (see Pending Decisions doc)

The same avatar represents the player in hub worlds — walking through markets, entering arenas, joining expeditions.

### UI & HUD

**Principle:** UI meters for gameplay mechanics. Weather stays ambient (visual/audio cues only).

**Always visible (HUD):**
- Equipped tool
- Quick-slot inventory bar (hotbar)
- Time of day indicator
- Minimap / island overview toggle

**On-demand (inspect/hover):**
- Crop health, growth stage, water level, soil quality — shown when looking at a specific plot
- Workshop status — current recipe, progress, fuel level — shown when near or interacting
- Storage contents — shown when opened
- Structure condition — shown when inspected

**World-state visuals (no UI needed):**
- Weather — sky colour, cloud movement, wind particles, rain/lightning effects
- Crop health — visual wilting, colour changes, growth stages visible on the sprite
- Soil quality — visual difference between rich dark soil and depleted cracked ground
- Water levels — visible irrigation channels, puddles, dry/wet soil appearance

---

## 6. Hubs & Multiplayer

### The Skypillar

A craftable portal structure requiring materials from across all production chains — rare crops, forged components, alchemical reagents. Building it is a milestone that proves you've engaged with every system.

When activated, you choose which hub to travel to. Your shipping crate contents come with you as trade inventory. Your farm continues in the background — weather still happens, automation still runs, things can still go wrong.

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
- Plugin-ready architecture with systems communicating through interfaces

---

## 7. Monetization

### Model: Free-to-Play with Seasonal Battle Pass

Base game is completely free. No paywalls on gameplay.

#### What You Sell

- **Season Pass** — cosmetic reward track. Island themes (autumn leaves, volcanic rock, crystal formations), workshop skins, tool appearances, avatar outfits, market stall decorations.
- **Cosmetic Shop** — standalone skins outside the pass. Island banners, weather overlays (cherry blossoms in wind, glowing rain), unique Skypillar designs.
- **Emotes & Flair** — hub-only social expressions, titles, profile badges.

#### What You Never Sell

- Gameplay advantage — no premium seeds, no faster growth, no bonus resources
- Time skips — if crops take 2 hours, everyone waits 2 hours
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
- **Hub access** — Skypillar requires cross-chain crafting
- **Hub reputation** — trade volume, PvP wins, expeditions unlock exclusive blueprints
- **Prestige** (future) — reset island with bonuses, keep cosmetics and knowledge

---

## 8. MVP Scope

### What's In

MVP = Phase 1 + early Phase 2, single player only.

A player can:

- Generate a unique procedural island with varied terrain, elevation, soil types
- Scavenge debris that drifts past on timers
- Plant, water, tend, and harvest starter and staple crops
- Experience weather that matters — rain waters crops, storms damage them, wind affects exposed areas
- Build basic structures — shelter, rain catchers, windbreaks, paths
- Place and use first 3 workshops — drying rack, stone mill, forge
- Process crops into basic goods (wheat → flour, herbs → dried herbs)
- Store items in crates and barrels
- Expand island edges with scaffolding
- Save and load their farm

### What's Not In

- Hubs / multiplayer (Phase 3)
- Automation (late Phase 2)
- Animals (mid Phase 2)
- Advanced workshops (brewhouse, loom, kitchen, alchemist)
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
3. **Processing feels rewarding** — turning raw crops into goods gives a clear sense of progress.
4. **Expansion is visible** — the island after 2 hours looks dramatically different from the start.
5. **Players want to keep going** — the loop pulls into "one more harvest" territory.

### Technical Foundation (build for the future)

Even though the MVP is single player, architect for what's coming:

- **Modular entity system** — crops, workshops, storage, structures are all components. Easy to add animals, automation, new building types later.
- **Event-driven weather** — weather broadcasts events that any system can listen to. Today crops respond. Tomorrow structural decay, pests, and power systems can hook in.
- **Inventory/storage abstraction** — generic enough to support shipping crates and hub trading later.
- **Island serialization** — save format that can sync to a server when multiplayer arrives.
- **Plugin-ready architecture** — systems communicate through interfaces. Foundation for UGC scripting layer.

---

## Summary

| Aspect | Decision |
|--------|----------|
| Theme | Floating sky islands, gritty survival farming |
| Perspective | Isometric 2.5D |
| Engine | Unity / C# |
| Platform | Cross-platform (PC, mobile, browser) |
| Core loop | Scavenge → plant → grow → harvest → process → expand |
| Island | Procedural terrain with elevation, organic placement, vertical building |
| Farming (70%) | Soil quality, crop needs, weather interaction, crop tiers |
| Workshops (30%) | Processing chains, 7 workshop types, short meaningful recipes |
| Automation | Late Phase 2 reward, maintenance-heavy, earned not given |
| Survival | Weather-driven, no hunger bars, farm health is what matters |
| Hubs | Earned via Skypillar — market, PvP arena, expedition board, social commons |
| Multiplayer | Solo island is sacred, hubs are shared, solo-first design |
| Monetization | F2P + cosmetic battle pass, no pay-to-win |
| MVP | Phase 1 + early Phase 2, single player, weather + farming + 3 workshops |
