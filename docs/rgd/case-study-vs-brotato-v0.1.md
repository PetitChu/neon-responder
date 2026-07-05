# Case Study — Skill / Item / Power-up Card Systems: *Vampire Survivors* & *Brotato*

**Research doc · v0.1** · *neutral teardown — no Neon Responder mapping (that's a later pass).*
**Companion:** the full base-game card lists for both games live in [`card-catalog-vs-brotato-v0.1.md`](card-catalog-vs-brotato-v0.1.md).
**Scope:** how each game hands the player power over the course of a run — the card/item economy, its
presentation, its synergy layer, and its meta layer. **Method:** principles-first. Specific numbers are
**illustrative and patch-dependent** (§7) — trust the *systems and principles*, verify any exact value
against the current build before using it to tune.

**Why these two:** they are the two canonical, opposite answers to the same question — *"how do you let a
run get more powerful?"* Vampire Survivors is the **draft** archetype (power arrives as offered cards you
fish for). Brotato is the **shop** archetype (power arrives as goods you deliberately buy). Studying them
side by side isolates which design choices are *genre-fixed* and which are *authored*.

---

## 0. The one-page contrast

| Axis | **Vampire Survivors** | **Brotato** |
|---|---|---|
| Where power comes from | Level-up **draft** (pick 1 of ~3–4 offered cards) | Between-wave **shop** (buy from 4 offered goods) |
| In-run currency | **XP gems** → levels (the pick); **gold** → mostly meta | **Materials** → both shop purchases *and* levels |
| Second economy layer | *Meta* PowerUps (permanent, bought with gold) | *In-run* stat draft (level-up = pick 1 of 4 stats) |
| Player agency over RNG | Reroll / Skip / Banish (unlockable, resource-gated) | Reroll / Lock / Sell (materials-gated, always on) |
| Decision tempo | **Fast** — mid-action, screen paused for a beat | **Slow** — calm shop, full stop between waves |
| Card information density | **Low** — icon, name, level, one line | **High** — icon, tier, price, ± stat deltas, class |
| Synergy model | **Semi-secret evolutions** (discovery) | **Legible set bonuses & stats** (planning) |
| Convergence mechanic | Weapon max + passive → **evolve** (climax) | Matching weapons → **combine up a tier** |
| Build "seed" | Character + **Arcanas** (global rule modifiers) | Character (strong starting archetype modifiers) |
| Meta philosophy | Persistent **power** (creep → accessibility) | Persistent **options + difficulty** (mastery) |
| What it optimizes for | *Snowball spectacle* — become a screen-clearing god | *Build-craft under pressure* — solve each wave's puzzle |

Everything below expands these rows.

---

## 1. Vampire Survivors — teardown

### 1.1 Choice architecture & economy
- **The core transaction is a draft.** Kills drop XP gems; a full XP bar pauses the game and offers a short
  vertical list of cards (usually 3–4). Each card is *one* of: a new weapon, a level on an owned weapon, a
  new passive item, or a level on an owned passive. You hold up to **6 weapon slots + 6 passive slots**;
  weapons cap at **level 8**.
- **The offer is the whole game.** Unlike a shop, you cannot ask for a specific item — you take what the roll
  presents. This makes the *quality of the roll* the central tension, which is why the mitigation tools exist:
  - **Reroll** — re-roll the offered cards. Charges are limited (bought via meta PowerUps).
  - **Skip** — decline the pick entirely for a small reward; used to *not* fill slots you're saving for
    evolution keys, or to thin your item count.
  - **Banish** — permanently remove one item from *this run's* pool. Build-sculpting by subtraction.
- **Luck is a meta-stat that bends the roll**, raising the odds of better cards and better chest contents —
  so RNG itself becomes something you can invest in.
- **Read:** the design deliberately keeps *acquisition* high-variance, then sells you **agency over the
  variance** as unlockable resources. You don't buy power; you buy *control over the fishing*.

### 1.2 Card presentation / UX
- **Minimalist by necessity.** The level-up happens mid-swarm; the player has a second, not a minute. So a
  card carries only: **icon, name, current→next level, one line of effect** ("Fires at the nearest enemy").
  Rarity/importance is signaled by **frame color**, not text.
- **Deep information lives off-card.** Evolution recipes, exact scaling, and synergies are *not* on the card
  face — they live in the Collection menu, tooltips, and player knowledge. The card face is tuned for a
  split-second yes/no, not for study.
- **The jackpot is a separate ritual.** Evolutions and big rewards aren't delivered on a level-up card — they
  arrive via a **treasure chest** that rolls 1 / 3 / 5 items with a slot-machine flourish. Presentation
  itself is the reward beat; the reveal is theatrical on purpose.
- **Principle it demonstrates:** *information density should match the time the player has to read it.* Fast
  decision → sparse card.

### 1.3 Synergy & evolution
- **Evolutions are the marquee synergy.** A weapon at **max level** plus a **specific required passive** owned
  → the weapon transforms into a far stronger evolved form when a post-10-minute chest is opened. Examples
  (illustrative): *Whip + Hollow Heart → Bloody Tear*; *King Bible + Spellbinder → Unholy Vespers*;
  *Magic Wand + Empty Tome → Holy Wand*.
- **Unions** are a variant: two *weapons* combine into a hybrid (e.g. *Peachone + Ebony Wings → Vandalier*).
- **Passives are dual-purpose** — they boost stats *and* act as evolution keys. Every passive pick therefore
  carries two kinds of value, which raises the stakes of an otherwise "boring stat" card.
- **Synergy is semi-secret.** The game barely signals recipes on-screen; discovering (or looking up) an
  evolution is part of the fantasy. This rewards **knowledge as progression** — a returning player is more
  powerful because they *know things*, independent of meta stats.
- **Arcanas** sit above all this: unlockable tarot modifiers you select at run milestones that rewrite global
  rules (duplication, healing-as-damage, retaliation, etc.). They act as a **build seed** that re-colors which
  synergies are even worth chasing this run.

### 1.4 Meta-progression
- **Gold → PowerUps.** Coins collected in-run (and per stage) are spent in the main menu on **permanent stat
  boosts** (Might, Area, Cooldown, Amount, Duration, Speed, Luck, Growth, Greed, Curse, Revival, Magnet, and
  the acquisition tools Reroll / Skip / Banish / Charm).
- **This is persistent *power*.** Meta upgrades make every future run start stronger — an explicit
  **accessibility dial**: struggling players grind PowerUps to lower the wall. Notably, PowerUps can be
  **reset for a full gold refund**, so the dial is reversible for challenge-seekers.
- **Unlocks expand the pool** — characters, weapons, stages, and Arcanas are unlocked by in-run achievements,
  adding *content*, not raw power.
- **Read:** two meta tracks with different jobs — *PowerUps* tune difficulty downward; *unlocks* deepen
  variety. Keeping them separate lets accessibility and replayability be tuned independently.

### 1.5 What VS optimizes for
The whole stack serves **snowball spectacle**: start fragile, fish for cards, hit an evolution, and cross a
threshold where the screen becomes yours. Sparse cards keep the fantasy *fast*; secret evolutions make
mastery feel *earned*; persistent PowerUps make the ramp *forgiving*. The player fantasy is *becoming a god*.

---

## 2. Brotato — teardown

### 2.1 Choice architecture & economy
- **The core transaction is a purchase.** Each wave is a short survival timer; enemies and crates drop
  **Materials**. Between waves the game **fully stops** and opens a **shop** offering **4 items or weapons**.
  You spend Materials to buy.
- **Rich agency, always on** (no unlock gating like VS):
  - **Reroll** — re-roll the 4 offers; cost scales up within a shop visit and resets next wave.
  - **Lock** — pin a slot so a wanted-but-unaffordable item survives to the next shop.
  - **Sell** — dump owned weapons/items back for Materials (enables pivots and tier-combining plays).
- **The economy is a real engine, not a trickle.** The **Harvesting** stat pays out Materials at end of wave,
  so players choose between *spending now* vs *investing in income*. **Luck** raises the odds of higher-tier
  offers. Prices scale with wave and with how much you've already bought.
- **A second, separate draft exists.** Collecting Materials also levels you up, and each level is a **pick 1 of
  4 stat upgrades** (small ± stat changes) — a fast draft layer running *underneath* the deliberate shop.
- **Read:** where VS sells you control over a roll, Brotato gives you a **market** — you see the goods, price
  them against income, and choose. The tension is *portfolio management under a spend limit*, not fishing.

### 2.2 Card / item presentation / UX
- **Dense, because the shop is calm.** With the game fully paused, a shop row can afford heavy information:
  **icon, name, tier (colored border), price, and explicit ± stat deltas** — **green** for gains, **red** for
  costs/downsides — plus a description and, for weapons, class and damage.
- **The delta is the message.** Items show *marginal* change ("+5% Attack Speed", "−5% Ranged Damage"), so the
  player evaluates the *choice at the margin* rather than an absolute — the natural frame for a buy decision.
- **Downsides are first-class and visible.** Many items are **tradeoffs** (a benefit stapled to a cost). The
  red line isn't hidden fine print; it's rendered as prominently as the green — the *tension is the product*.
- **Tier = color = a planning cue** you can read across the whole shop at a glance (white → blue → purple →
  red), telling you where the wave's power ceiling sits right now.
- **Principle it demonstrates:** the *same* principle as VS, inverted — *because the decision is slow, density
  is affordable and desirable.* Reading time and information load are matched.

### 2.3 Synergy & build systems
- **Weapon-class set bonuses** are the legible synergy: weapons belong to classes (Blade, Blunt, Ranged,
  Precise, Elemental, Heavy, Medical, Support, …), and stacking a class grants scaling bonuses at ownership
  thresholds (e.g. 3 / 6 of a class). You can *see and plan* toward these.
- **Stats are the deeper synergy fabric.** The game is profoundly stat-driven (Damage split into
  Melee/Ranged/Elemental, Attack Speed, Crit, Range, Armor, Dodge, Engineering, Luck, Harvesting, Lifesteal…).
  Whole archetypes are stat engines — crit-stacking, dodge-tank, Engineering/turret, lifesteal-melee.
- **Convergence via combine.** Matching weapons merge **up a tier**, turning slot pressure (only 6 weapon
  slots) into an upgrade path — the satisfying "three into one, but stronger" beat, and a reason to *sell*.
- **Character = build seed.** 40+ characters carry strong starting modifiers that pre-commit an archetype
  (bonus attack speed but no armor; everything scales with Engineering; ranged-only; etc.), so the *same shop*
  yields different optimal buys per character.
- **Read:** synergy here is **planned, not discovered.** Sets and stats are shown; the skill is *composition
  under scarcity*, not recipe knowledge.

### 2.4 Meta-progression
- **Meta is options + difficulty, not raw power.** Winning/progressing unlocks **more characters, weapons, and
  items** (deeper pool) and raises the per-character **Danger level (0–5)** — a difficulty ladder with
  modifiers. There is **no permanent stat-purchase** track like VS PowerUps.
- **Consequence:** a new run does **not** start mechanically stronger — you carry **knowledge and unlocked
  options** forward, and prove mastery by clearing higher Danger. The meta rewards *getting better*, not
  *grinding power*.
- **Read:** deliberately the opposite meta stance from VS. It keeps every run honest (no trivializing creep)
  at the cost of VS's accessibility ramp.

### 2.5 What Brotato optimizes for
The stack serves **build-craft under pressure**: each shop is a puzzle — *given this character, these offers,
this income, and next wave's threat, what's the optimal spend?* Dense cards make the puzzle legible; visible
tradeoffs make it tense; difficulty-only meta keeps the puzzle sharp forever. The player fantasy is *solving
the run*, not *becoming unstoppable*.

---

## 3. Head-to-head by lens — the contrasts that teach

### 3.1 Choice architecture — *draft-centric vs shop-centric*
Both gate acquisition behind an offer of ~4, and both sell you tools to fight the RNG — but the **default
posture differs**. VS defaults to *low agency* (take a card) and sells agency back as scarce unlockables
(reroll/skip/banish), which keeps the fishing tense and the fantasy fast. Brotato defaults to *high agency*
(a market with reroll/lock/sell always on) and makes the *pricing* the tension. **Lesson:** agency-over-RNG
is a dial, not a binary — you can meter it (VS) or hand it over and charge for the goods instead (Brotato).

### 3.2 Presentation — *density ∝ reading time*
The single most transferable UX law across both: **card information density should track how long the player
has to read it.** VS decisions happen mid-action → cards are one line, color-coded, deep info off-card.
Brotato decisions happen at a full stop → rows are dense with ± deltas, tiers, prices, and downsides. Same
law, opposite outputs. **Lesson:** decide the *decision tempo* first; the card's information budget follows
from it, not the other way around.

### 3.3 Synergy — *secret/discovery vs legible/planned*
VS hides recipes (evolutions) and rewards **knowledge as progression** — the "aha" is finding the combo.
Brotato shows its sets and stats and rewards **planning under scarcity** — the "aha" is composing the build.
Both produce build depth; they serve different players (explorer vs optimizer) and different session shapes
(long solo snowball vs tight repeated puzzles). **Lesson:** you can put the synergy skill in the player's
*head* (secret) or on the *table* (legible) — pick to match your audience and session length.

### 3.4 Meta — *persistent power vs persistent options*
VS meta makes future runs **stronger** (accessibility, reversible). Brotato meta makes future runs **wider
and harder** (mastery, no creep). This is the cleanest either/or in the whole study. **Lesson:** the meta
layer encodes a philosophy — do you want the game to *get easier as the player invests*, or the *player to
get better while the game stays honest*? Both are valid; they are not compatible in the same track.

---

## 4. Extracted principles (transferable)

1. **Decide decision tempo before card layout.** Fast/mid-action → sparse cards, color-coded, deep info
   off-card (VS). Slow/paused → dense cards with ± deltas and tiers (Brotato). Tempo dictates the info budget.
2. **Show the delta, not the absolute.** "+5 damage / −5% range" frames the pick as a marginal choice — the
   correct mental model for choosing among options (Brotato).
3. **Encode the one decision-critical bit in color.** Rarity/tier and benefit-vs-cost read faster as color
   than as text (both games: frame color, green/red deltas).
4. **Sell agency over RNG as a resource.** Reroll / skip / banish / lock turn "bad luck" into "spend to fix
   it," which is a decision instead of a frustration — and a sink (both games).
5. **Subtraction is a build tool.** Removing noise (VS Banish; Brotato Sell/skip-a-buy) is as powerful as
   adding signal, and lets players *sculpt* a pool toward a plan.
6. **Give components dual purpose.** VS passives are stat boosts *and* evolution keys — every pick carries two
   kinds of value, so no card is purely filler.
7. **Provide a convergence/climax mechanic.** Evolutions (VS) and tier-combining (Brotato) fight slot bloat
   and deliver a power *threshold* the player can aim at — a peak, not just a slope.
8. **Choose secret vs legible synergy deliberately.** Secret recipes reward knowledge and discovery; legible
   sets/stats reward planning. This choice defines who your game is *for*.
9. **A top-level "build seed" multiplies replayability.** Characters (both) and Arcanas (VS) re-color the
   whole offer pool, so the same items support many runs.
10. **Split meta currency from run currency, and pick a meta philosophy.** Keep the in-run economy and the
    cross-run economy distinct; then choose *persistent power* (accessibility, VS) **or** *persistent options +
    difficulty* (mastery, Brotato) — not a muddled mix.
11. **Make tradeoffs first-class, not fine print.** Visible downsides (Brotato red deltas; VS's −HP items) are
    the *product* — they create the interesting decisions. Don't bury them.
12. **Match the reward *ritual* to the reward's weight.** Routine picks are quiet cards; big payoffs get a
    theatrical delivery (VS treasure-chest reveal; Brotato tier-up combine). Ceremony signals significance.

---

## 5. Card anatomy reference (feeds the visual artifact)

**VS level-up card (fast-read, sparse):**
```
┌───────────────────────────┐
│ [icon]  WHIP        Lv 2  │   ← icon + name + current→next level
│ Attacks horizontally,     │   ← ONE line of effect
│ passes through enemies    │
└───────────────────────────┘
  frame color = importance/rarity ·  no price · no synergy text (that's off-card)
```

**Brotato shop row (slow-read, dense):**
```
┌───────────────────────────────────────────────┐
│ [icon]  Bloody Torch      ⬢ Tier III     28 ⛏ │  ← icon + name + tier(color) + price
│  + Life Steal        5                         │  ← green gains (delta)
│  + Burning damage    3                         │
│  − Max HP           10                          │  ← red cost (delta), equally visible
│  "Class: Elemental · Support"                   │  ← class → set-bonus planning cue
└───────────────────────────────────────────────┘
```
The contrast *is* the lesson: same job (offer a choice), opposite information budgets, because one is read in
a second and the other at a full stop.

---

## 6. Open questions / caveats
1. **Numbers are patch-dependent.** Card counts, tier colors, reroll-cost curves, exact evolution recipes,
   set-bonus thresholds, and Danger tuning drift across updates and DLC. Everything here is systems/principles;
   **verify any specific value against the current build before using it to tune.**
2. **This is neutral by request.** No mapping to Neon Responder's Protocol/Special split yet — that's the
   intended next pass (turn these 12 principles into "steal / avoid / already-have" calls against
   `protocol-stack-v0.1.md` and `special-moves-v0.1.md`).
3. **Two more comparables worth a future pass** if this proves useful: *Risk of Rain 2* (item stacking +
   pure-legible pickups, no draft) and *Slay the Spire* (deck-draft with explicit rarity + shop + relics) —
   they cover the design space between these two poles.

---
*Next (on your go): the Neon Responder mapping pass — take §4's principles and §0's contrast table and score
them against the Protocol (draft) and Special (shop) channels.*
