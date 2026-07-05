# Neon Responder: Night Shift — The Protocol Stack

**Design doc · v0.3** *(v0.2 added Phase 5 balance §8; v0.3 = Hard Split — protocols are level-up-only, the shop is Specials-only)* · *Expansion of GDD RGD Phase 9 (Progression — the roguelite stack).*
Anchored to **GDD v0.4** (`Neon_Responder_GDD`, Notion › Jungle XRKit › Game Design Ideas).
Published: [Notion — The Protocol Stack](https://app.notion.com/p/3935c0654b6b8107a740e1d2658a4244) (child of the GDD).
Status: **design intent only** — Protocols are `[GDD-TARGET · not built]` in code. Nothing here is wired against existing systems.

---

## 0. Purpose & what's new

The GDD already fixes the Protocol **families** (§8, recast in §0.4.f) and seeds ~11 named
protocols. This doc turns that into a **replayability-grade stack**: a fuller family taxonomy
(tone + feat locked per family), a **rarity model**, a **dependency / hidden-tree model**, and a
**first batch of 36 protocols** distributed across all of it.

**Why the stack carries replayability:** per GDD §1, the roguelite stack owns *growth (the ceiling)*
and Momentum decides *how much of that ceiling fires right now*. More distinct build archetypes +
discoverable dependency paths = more reasons to run again. That is the whole job of this doc.

### Decisions & assumptions I made (flag these if wrong)
1. **Kept the GDD's 8 families** verbatim as the spine; added a **tonal codename + locked feat** to each
   so they're "SUPER fitting" on tone *and* mechanic. I did **not** invent new families.
2. **Recast the retired "Takedown" family → "Execution."** v0.4 retired the Takedown *mechanic*; its
   family role (chain range, AoE-on-finish, Overcharge, +Momentum-per-finish) still exists and now
   attaches to the contextual finishing verbs. Naming it "Execution" keeps it verb-agnostic.
3. **Rarity (4 tiers) and dependency-gating are net-new** — the GDD has neither. **Hard Split (v0.3):**
   protocols are acquired **only via in-run level-ups** (pick 1 of 3, slow-mo); the between-encounter
   **Neon Charge shop is reserved for Special Moves + consumables** (see `special-moves-v0.1.md`), and
   later slotable items / power-ups / gear. This diverges from GDD §8 (which sold "bigger Protocols").
4. **All knob values trace to GDD §9 / §0.4.e** where the doc gives one; new knobs are marked *(new knob)*
   and are playtest starting points, not promises — matching the GDD's own convention.
5. **Momentum grants on finishing hits only** (v0.4 locked rule) — every protocol respects this.

---

## 1. Family taxonomy (tone + feat)

Eight families. **Feat** = the mechanical lane it owns (GDD-cited). **Codename/tagline** = the tone skin
that makes it read at a glance in the neon-responder fiction. **Archetype** = the build it anchors.

| Family | Codename | Feat (what it upgrades — GDD cite) | Build archetype |
|---|---|---|---|
| **Auto-Gear** | *"The Grinder"* | Auto-engage rate / arc / pierce / projectiles / chip-damage → **Finish-Ready throughput** (§6.1, §8) | Firehose — drown the belt in Finish-Ready targets |
| **Momentum** | *"The Redline"* | Decay rate, tier cap, multiplier strength, Momentum-per-finish (§6.4, §8) | Flow-state — live at Overdrive, everything screams |
| **Execution** | *"The Last Call"* | Finishing behavior: chain range, AoE-on-finish, Overcharge fill/power (§6.3/6.9, §8 "Takedown" recast) | Chain-slayer — one finish flows into the next forever |
| **Brawler** | *"The Hands"* | The verbs: grab, throw-enemy, kick knockdown, jump-attack, weapon-attack (§0.4.d, §0.4.f) | Wrecking crew — the body is the weapon |
| **Scavenger** | *"The Salvage"* | Prop/weapon pickup range, spawn rate, durability (§0.4.f) | Armory — the street keeps handing you tools |
| **Specials** | *"The Deployables"* | Siren Pulse / Neon Barricade / EMP Line — cooldown, power, Finish-Ready radius (§6.7, §8) | Zone control — manufacture waves of prompts |
| **Defense** | *"The Night Watch"* | Armor, dodge charges, lifesteal-on-finish, i-frames (§6.6, §8) | Immovable — hold the node through anything |
| **Objective** | *"The Dispatch"* | Reboot speed, rescue radius, node damage-resist, Signal interaction (§6.10, §8, §12) | Stabilizer — win the mission, bend the dawn |

> Tone spine (GDD §3): neon-retro synthwave, CRT-adjacent, deadpan-earnest responder vs. absurd threats.
> Codenames should read like gear/ops call-signs ("bring back the dawn", "signal in the static").

---

## 2. Rarity model *(net-new)*

Four tiers. Rarity governs **effect strength**, **draft weight**, and **which channel** surfaces it.

| Tier | Name | What it does | Draft weight | Acquisition (all level-up draft) |
|---|---|---|---|---|
| ◇ **Stock** | common | Flat knob bump (a scaled §9/§0.4.e value) | High | Draft pool |
| ◈ **Tuned** | uncommon | Conditional or compound effect | Medium | Draft pool |
| ✦ **Prototype** | rare | **Behavior change** — rewrites how a verb / finish / meter works | Low | Draft pool, late-run weighted |
| ★ **Blacksite** | keystone | **Run-defining**, usually gated + carries a downside | Very low / **gated-only** | Not in pool until gated → then **guaranteed-offered** next level-up |

**Rules of thumb**
- **Stock** protocols are the backbone of the pick-1-of-3 pool so early level-ups always have safe picks.
- **Prototype+** weight scales up with **Signal tier** (GDD §12) — the nastier the night, the spicier the offers.
- **Blacksite** protocols are mostly **not in the random pool at all** — they only appear once their
  dependency is met (see §3). That's the "hidden" in hidden skill trees.
- **Hard Split:** protocols never appear in the shop — the shop is Specials + consumables only
  (`special-moves-v0.1.md`). Neon Charge starvation (Sirencatchers, §7) therefore pressures your
  *Specials* build, not your protocol draws. Intended tension.

---

## 3. Dependency / hidden-tree model *(net-new)*

A protocol may declare a **prerequisite**. Until it's satisfied, the protocol is **not in any pool** —
invisible until you've built toward it. Four prereq types:

| Prereq type | Meaning | Example |
|---|---|---|
| **Requires X** | A specific protocol is in the stack | *Redline Governor* needs *Afterburner* |
| **N-of-family** | ≥ N protocols from one family | *Overclocked* needs Momentum ×3 |
| **Pair (A+B)** | One from each of two families (a **fusion**) | *Wrecking Finish* needs *Iron Grip* + *Human Wrecking Ball* |
| **Threshold** | A stack-size / character-level gate | Blacksites soft-gated to level ≥ 5 |

**Tree shapes**
- **Linear chain** — I → II → capstone within a family (deepen one lane).
- **Family capstone** — N-of-family unlocks the family's *signature* Blacksite.
- **Cross-family fusion** — a pair from two families unlocks a hybrid Blacksite (the spicy discoveries).
- **Keystone conversion** — a Blacksite that *rewrites a core rule*, gated behind a pair, with a downside.

**Discovery contract:** a gated protocol should telegraph its existence *after* you take one prereq
(e.g. the level-up card shows a locked "⋯ leads somewhere" hint) so build paths feel *discoverable*,
not *guessable*. (New sign — add to GDD §6 sign budget when built.)

---

## 4. Protocol catalog — first batch (36)

Format: **Name** — *rarity* · effect · `dependency` · knobs. † = named in the GDD already (recast noted).

### Auto-Gear — "The Grinder" (5)
- **Split Fire** — ◇ Stock · auto-engage emits +1 projectile. · knob: +1 projectile.
- **Wide Sweep** — ◇ Stock · auto-attack arc 120° → 180°. · knob: arc +60°.
- **Piercing Rounds** — ◈ Tuned · auto-attacks pierce +1 enemy (more chip reaches deep chaff). · knob: pierce +1.
- **Overclocked Coil †** — ◈ Tuned · +50% auto-attack rate, −10% max HP. · *(GDD example, verbatim)*.
- **Saturation Fire** — ✦ Prototype · `N-of-family: Auto-Gear ×2` · auto projectiles pierce **and** chain to a 2nd target — Finish-Ready throughput spikes. *Firehose capstone.*

### Momentum — "The Redline" (5)
- **Quick Ignition** — ◇ Stock · start each encounter at **Warm** instead of Cool. *(new knob)*
- **Afterburner †** — ◈ Tuned · Momentum decays 40% slower. · *(GDD example, verbatim)*.
- **Executioner's Cadence †** — ◈ Tuned · finishing hits **below Hot tier** grant double Momentum (rewards recovery; applies to all verbs). · *(GDD example, v0.4 wording)*.
- **Redline Governor** — ✦ Prototype · `Requires: Afterburner` · raise the **Overdrive** multiplier ×2.5 → ×3.0 (§9). *(new knob)*
- **Overclocked** — ★ Blacksite · `Requires: Redline Governor` **+** `Momentum ×3` · Momentum **never decays while an objective zone is contested**; −20% max HP. *Redline capstone (risk).*

### Execution — "The Last Call" (5) *(recast of retired Takedown family)*
- **Concussive Finish** — ◇ Stock · finishing hits deal a small AoE. · *(GDD §8 "Takedowns hit a small AoE", recast)*.
- **Chain Dispatch †** — ◈ Tuned · finishing a target extends **chain range** to the next Finish-Ready target. · *(GDD example; recast from v0.3 "lengthens next window" — v0.4 has no prompt window)*.
- **Dawn Reserve †** — ◈ Tuned · Overcharge starts each encounter 30% filled. · *(GDD example, verbatim)*.
- **Cascade** — ✦ Prototype · `Requires: Chain Dispatch` · a finish that **chains within 1s** refunds a chunk of Overcharge — chaining literally fills the finisher. *(new knob: +X% Overcharge / chain).*
- **Full Clear** — ★ Blacksite · `Requires: Cascade` **+** `Execution ×2` · the Overcharge finisher (§6.9) also **resets all cooldowns** and grants **Overdrive** on activation. *Chain-slayer capstone.*

### Brawler — "The Hands" (5)
- **Steel Toe** — ◇ Stock · kick knockdown force/radius +; better crowd control (§0.4.d kick). · knob: knockback +30%. *(new knob)*
- **Iron Grip †** — ◈ Tuned · grab +50% duration, no squirm-free. · *(GDD example, verbatim; §0.4.e grab)*.
- **Air Raid †** — ◈ Tuned · jump-attack AoE +40%, −0.5s cooldown. · *(GDD example, verbatim)*.
- **Human Wrecking Ball †** — ✦ Prototype · throw-enemy clip-cap +1 (§0.4.e) + wider throw AoE. · *(GDD example, expanded)*.
- **Wrecking Finish** — ★ Blacksite · `Pair: Iron Grip (Brawler) + Human Wrecking Ball (Brawler→Execution)` · a thrown enemy that clips a Finish-Ready target **instantly finishes it and chains Momentum**. *Flagship grab-and-clear fusion (GDD §0.4.f "Grab-and-clear" pattern, made a capstone).*

### Scavenger — "The Salvage" (4)
- **Long Reach** — ◇ Stock · pickup proximity 0.4m → 0.7m (§0.4.e). · knob: +0.3m.
- **Sticky Fingers †** — ◇ Stock · +2 weapon uses. · *(GDD example, verbatim)*.
- **Street Armory** — ◈ Tuned · props/weapons spawn ~50% more often (§0.4.f). · knob: spawn rate ×1.5. *(new knob)*
- **Signature Iron** — ★ Blacksite · `Requires: Sticky Fingers` **+** `Scavenger ×3` · your current weapon **never depletes**, but you **can't pick up another until you drop it** (commitment). · *(GDD §0.4.f "rare late: weapons don't break", with a commit-cost).*

### Specials — "The Deployables" (4)
- **Capacitor** — ◇ Stock · Special cooldown −25% (base 6s → 4.5s, §9). · knob: cd ×0.75.
- **Wide Field** — ◇ Stock · Special Finish-Ready radius +. · knob: radius +30%. *(new knob)*
- **Priority Override †** — ✦ Prototype · Siren Pulse also advances the objective bar ~15%. · *(GDD example, verbatim)*.
- **Dispatch Protocol** — ★ Blacksite · `Pair: Priority Override (Specials) + any Objective` **+** `Objective ×2` · **every** Special advances the objective, and each Finish-Ready it creates refunds Charge. *Zone-control × stabilizer fusion.*

### Defense — "The Night Watch" (4)
- **Plating** — ◇ Stock · +flat armor (damage reduction). · knob: −2 dmg/hit. *(new knob)*
- **Second Wind** — ◈ Tuned · +1 dodge charge (§6.6). · knob: dodge charges 1 → 2.
- **Vampiric Cadence** — ◈ Tuned · lifesteal on **finishing hits** (recast GDD "lifesteal on Takedown"; finishing-only per v0.4). · knob: heal 2 HP/finish. *(new knob)*
- **Last Responder** — ★ Blacksite · `N-of-family: Defense ×3` · the first lethal hit each encounter is survived at 1 HP + brief invuln + a free jump to **Overdrive**. *Night-watch capstone.*

### Objective — "The Dispatch" (4)
- **Rapid Reboot** — ◇ Stock · Reboot Node speed +25% (§9 base 45–60s). · knob: fill rate ×1.25.
- **Hardline** — ◇ Stock · node damage-resist; harder to be knocked off during a reboot. · knob: −40% knock-off chance. *(new knob)*
- **Crowd Whisperer †** — ✦ Prototype · rescued civilians orbit you as a chaff-blocking shield. · *(GDD example, verbatim; §6.10 Rescue)*.
- **Bring The Dawn** — ★ Blacksite · `N-of-family: Objective ×3` · completing an objective **drops the Signal hard** (good — §12) but **spikes spawn density for ~10s**. *Signal-gambit capstone (risk/reward vs. the dawn).*

**Tally:** 36 protocols — 11 Stock ◇, 12 Tuned ◈, 6 Prototype ✦, 7 Blacksite ★.
Named-in-GDD carried over: 11 (all recast where v0.4 required it).

---

## 5. The hidden trees (dependency map)

Eight gated protocols form the discoverable layer. Two are **fusions** (cross-family), the rest deepen a lane.

```
AUTO-GEAR ─────────────────────────────────────────────
  [Auto-Gear ×2] ──▶ ★ Saturation Fire

MOMENTUM ──────────────────────────────────────────────
  Afterburner ──▶ ✦ Redline Governor ──┐
                                        ├─[+ Momentum ×3]──▶ ★ Overclocked
                                        
EXECUTION ─────────────────────────────────────────────
  Chain Dispatch ──▶ ✦ Cascade ──┐
                                 ├─[+ Execution ×2]──▶ ★ Full Clear

BRAWLER ── Iron Grip ──┐
                       ├── (FUSION) ──▶ ★ Wrecking Finish
BRAWLER ── Human Wrecking Ball ──┘

SPECIALS ── Priority Override ──┐
                                ├── (FUSION, + Objective ×2) ──▶ ★ Dispatch Protocol
OBJECTIVE ── (any) ─────────────┘

DEFENSE ── [Defense ×3] ──▶ ★ Last Responder
OBJECTIVE ── [Objective ×3] ──▶ ★ Bring The Dawn
```

**Read:** ✦ Prototypes are the mid-tree stepping stones (visible in the shop once their `Requires:` is met);
★ Blacksites are the payoffs (surface only when fully gated). The two **fusions** are the marquee
"build discovery" moments — they reward committing across two families, which is exactly the
snowball GDD §8 wants ("stacking within a family = the snowball", now extended across families).

---

## 6. How this rides the GDD loop

- **Auto-Gear / Execution** feed the core loop's throughput and finish layers (§6.1–6.3) — the
  Firehose and Chain-slayer archetypes are two answers to R2 "does it feel idle?"
- **Momentum** protocols directly tune the §9 curve (decay, cap, multiplier) — the highest-variance,
  most tuning-sensitive lane, so it holds the most gated depth.
- **Brawler / Scavenger** are the v0.4 hands-back-to-the-player lanes; *Wrecking Finish* makes the
  flagship **grab-and-clear** pattern (§0.4.f) a build goal.
- **Specials / Defense / Objective** keep the *stabilize-don't-exterminate* pillar (§2) rewarded so a
  build can lean into holding the node and the **Signal** meta, not just kills.

## 7. Open questions / balance notes
1. **MVP subset:** GDD §13/§0.4.g wants only **6–8 protocols incl. ≥1 Brawler + 1 Momentum** for the 48h
   jam. Recommended MVP cut from this batch: *Split Fire, Overclocked Coil, Afterburner, Executioner's
   Cadence, Iron Grip, Human Wrecking Ball, Concussive Finish, Rapid Reboot* — one gated pair
   (*Wrecking Finish*) to prove the hidden-tree mechanic end-to-end.
2. **Draft weights** are qualitative here; they need real numbers once the level-up/shop split is coded.
3. **Blacksite downsides** (−HP, commit-costs, density spikes) need a pass against R8 (whiff death-spiral)
   and R10 (grab as idle-trap) so keystones aren't strictly-worse traps.
4. **Signal-scaled rarity** (§2) is a proposal — confirm the Signal exposes a queryable tier for the
   draft roller.
5. **Meta-unlock layer** (GDD §8 "unlock new Protocols across runs") is out of scope here — this is the
   in-run pool. Flag if you want the cross-run unlock tree designed next.

---

## 8. Parameters & balance (RGD Phase 5)

All values are **starting knobs** (GDD convention) — playtest targets, not promises. They trace to GDD §9 / §0.4.e
where a baseline exists; new values are marked *(new)*.

### 8.1 Power-budget invariants (the guardrails)
These keep the stack from fighting the Momentum multiplier or breaking readability.

1. **Momentum is the only global multiplier.** Protocol damage/rate/gain bonuses are **additive** to their base
   knob; the tier multiplier (×1.0/1.3/1.7/2.5, §9) applies **once** to the summed total. No protocol multiplies
   another protocol. *(Prevents the classic VS-like exponential runaway.)*
2. **Target ceiling:** a fully-stacked build at **Overdrive** should read as ~**6–8×** a fresh build at Cool
   (≈ ×2.5 Momentum × ≈2.5–3× stacked base). Enough that "the build screams" (validation §16), not enough to
   delete the challenge.
3. **Hard caps** (perf + readability, tied to the §9 density budget): auto-attack rate **≤ 6/s**; auto arc **≤ 180°**;
   incoming damage floored at **1** (armor can't zero it); **max-HP floor 50** (all −HP costs are additive and stop here).
4. **Finish-Ready threshold (≤25% HP) is sacrosanct** — no v0.1 protocol moves it. Effects act *around* it, never on it.
5. **Momentum stays finishing-only** — no protocol grants Momentum on chip/knockdown (v0.4 locked rule).
6. **Behavior protocols are unique** (1 copy); **flat-bump protocols stack** to a per-protocol max (below).

### 8.2 Rarity → draft weight
Relative weights for the pick-1-of-3 level-up roll (Hard Split: protocols are draft-only, never shop-bought).
**Signal band** = the §12 dawn-pressure tier (assume 0–3, like Momentum).

<table header-row="true">
<tr><td>Tier</td><td>Base weight</td><td>Signal scaling</td><td>Acquisition</td></tr>
<tr><td>◇ Stock</td><td>100</td><td>none (always-available floor)</td><td>draft pool</td></tr>
<tr><td>◈ Tuned</td><td>50</td><td>×(1 + 0.25·band)</td><td>draft pool</td></tr>
<tr><td>✦ Prototype</td><td>18</td><td>×(1 + 0.5·band) → up to ×2.5</td><td>draft pool, late-run</td></tr>
<tr><td>★ Blacksite</td><td>0 until gated</td><td>gated-only; then guaranteed-offered</td><td>guaranteed-offered next level-up</td></tr>
</table>

- **Anti-brick:** first 3 level-ups of a run guarantee ≥1 Stock/Tuned in each pick-of-3.
- **Duplicate handling:** a re-rolled duplicate becomes a "+1 stack" if the protocol is stackable and below max; else it's swapped for another candidate.
- **Blacksites** never appear until fully gated **and** character level ≥ 5 (backstop against early N-of-family flukes).

### 8.3 Cadence & economy
<table header-row="true">
<tr><td>Knob</td><td>Start</td><td>Basis</td></tr>
<tr><td>Level-ups / encounter</td><td>3–4</td><td>→ ~12–16 protocols/run over 3–5 encounters (§9)</td></tr>
<tr><td>XP curve (cost at level N)</td><td>⌈10 · N^1.35⌉</td><td>*(new)* tune exponent to hit 3–4 levels/encounter at 80–150 hot chaff</td></tr>
<tr><td>XP per kill / Takedown-finish</td><td>1 · ×Momentum</td><td>§8 (gain multiplied by tier)</td></tr>
<tr><td>Neon Charge / encounter</td><td>~60–100</td><td>objectives (bulk) + per-finish (×Momentum); Sirencatchers steal it (§7). **Spent in the Specials shop** (companion doc), not on protocols</td></tr>
<tr><td>Shop reroll</td><td>10, +10 each use (resets per shop)</td><td>*(new)*</td></tr>
<tr><td>Heal (shop) / Special-swap</td><td>25 / 40</td><td>*(new)*</td></tr>
</table>

### 8.4 Per-protocol tuned knobs
Stackable = flat bumps (max copies in parens). Unique = behavior; 1 copy.

<table header-row="true">
<tr><td>Protocol</td><td>Value / stack</td><td>Max</td><td>Note</td></tr>
<tr><td>Split Fire</td><td>+1 projectile</td><td>×3</td><td>capped by 6/s throughput</td></tr>
<tr><td>Wide Sweep</td><td>+30° arc</td><td>×2</td><td>hard cap 180°</td></tr>
<tr><td>Piercing Rounds</td><td>+1 pierce</td><td>×3</td><td></td></tr>
<tr><td>Overclocked Coil</td><td>+50% rate, −10% HP</td><td>unique</td><td>rate obeys 6/s cap; HP floor 50</td></tr>
<tr><td>Saturation Fire</td><td>pierce + chain to 2nd target (100% dmg)</td><td>unique</td><td>gate: Auto-Gear ×2</td></tr>
<tr><td>Quick Ignition</td><td>start at Warm (step 3/9)</td><td>unique</td><td></td></tr>
<tr><td>Afterburner</td><td>decay 2.5s → 4.2s/tier</td><td>unique</td><td>unique to prevent trivial no-decay</td></tr>
<tr><td>Executioner's Cadence</td><td>below-Hot finish = +2 steps (vs +1)</td><td>unique</td><td>the anti-death-spiral tool (R8)</td></tr>
<tr><td>Redline Governor</td><td>Overdrive mult ×2.5 → ×3.0</td><td>unique</td><td>the ONLY ceiling raise; req Afterburner</td></tr>
<tr><td>Overclocked</td><td>no passive decay while node contested; −20% HP</td><td>unique</td><td>whiff still resets to Cool; HP floor 50</td></tr>
<tr><td>Concussive Finish</td><td>finish AoE 0.8m @50% dmg (+0.2m)</td><td>×3</td><td></td></tr>
<tr><td>Chain Dispatch</td><td>chain range 3m +1.5m</td><td>×2</td><td></td></tr>
<tr><td>Dawn Reserve</td><td>Overcharge +30% start (+15%)</td><td>×2</td><td>cap 60% start</td></tr>
<tr><td>Cascade</td><td>+8% Overcharge / chain-within-1s</td><td>unique</td><td>cap +40%/encounter (R4); req Chain Dispatch</td></tr>
<tr><td>Full Clear</td><td>Overcharge finisher also resets cds + grants Overdrive</td><td>unique</td><td>gate: Cascade + Execution ×2</td></tr>
<tr><td>Steel Toe</td><td>+30% kick knockback</td><td>×2</td><td></td></tr>
<tr><td>Iron Grip</td><td>grab 3s → 4.5s, no squirm-free</td><td>unique</td><td>raises grab risk → R10 watch</td></tr>
<tr><td>Air Raid</td><td>jump AoE 1.2m → 1.68m, cd 2.5s → 2.0s</td><td>unique</td><td></td></tr>
<tr><td>Human Wrecking Ball</td><td>throw clip-cap +2 → +3, AoE 0.8m → 1.1m</td><td>unique</td><td></td></tr>
<tr><td>Wrecking Finish</td><td>thrown enemy clipping Finish-Ready = instant finish + chain</td><td>unique</td><td>gate: Iron Grip + Human Wrecking Ball</td></tr>
<tr><td>Long Reach</td><td>pickup 0.4m → 0.7m (+0.15m)</td><td>×2</td><td></td></tr>
<tr><td>Sticky Fingers</td><td>+2 weapon uses</td><td>×3</td><td></td></tr>
<tr><td>Street Armory</td><td>spawn ×1.5 (+0.25×)</td><td>×2</td><td>cap ×2.0</td></tr>
<tr><td>Signature Iron</td><td>weapon never depletes; can't swap till dropped</td><td>unique</td><td>gate: Sticky Fingers + Scavenger ×3</td></tr>
<tr><td>Capacitor</td><td>Special cd −25% (6s→4.5s)</td><td>×2</td><td>floor 3s</td></tr>
<tr><td>Wide Field</td><td>+30% Special Finish-Ready radius</td><td>×2</td><td></td></tr>
<tr><td>Priority Override</td><td>Siren Pulse advances objective +15% (+5%)</td><td>×2</td><td>cap 25%</td></tr>
<tr><td>Dispatch Protocol</td><td>every Special advances objective; Finish-Ready it makes refunds Charge</td><td>unique</td><td>gate: Priority Override + Objective ×2</td></tr>
<tr><td>Plating</td><td>−2 incoming dmg</td><td>×3</td><td>floor incoming at 1</td></tr>
<tr><td>Second Wind</td><td>+1 dodge charge</td><td>×2</td><td>1 → 3 total</td></tr>
<tr><td>Vampiric Cadence</td><td>+2 HP / finish (+1)</td><td>×3</td><td>cap +5</td></tr>
<tr><td>Last Responder</td><td>survive 1st lethal hit/encounter @1 HP + invuln + Overdrive</td><td>unique</td><td>gate: Defense ×3; once/encounter</td></tr>
<tr><td>Rapid Reboot</td><td>+25% reboot speed</td><td>×2</td><td>node time floor ~22s</td></tr>
<tr><td>Hardline</td><td>−40% knock-off, node dmg-resist 20%</td><td>×2</td><td></td></tr>
<tr><td>Crowd Whisperer</td><td>rescued civilians orbit; each blocks 1 hit</td><td>unique</td><td>up to 5 (§9 rescue count)</td></tr>
<tr><td>Bring The Dawn</td><td>objective clear drops Signal hard; +density ~10s</td><td>unique</td><td>gate: Objective ×3</td></tr>
</table>

### 8.5 Stress tests (against GDD §15 risks)
- **R8 — whiff death-spiral.** *Overclocked* removes only *passive* decay and only while a node is contested (not in transit), and **whiff still hard-resets to Cool** — so it rewards holding, not immunity; the −20% HP raises whiff lethality as the counter-cost. *Executioner's Cadence* (double Momentum below Hot) is deliberately **Tuned/cheap** so recovery is always accessible. **Verdict: acceptable; watch the HP floor at Overclocked + Overclocked Coil combined (−30% → 70 HP).**
- **R10 — grab as an idle-trap.** *Iron Grip* lengthens the 0-i-frame commit → strictly *more* dangerous alone; it's only justified once *Wrecking Finish* makes the grab→throw payoff huge. **Mitigation if playtest shows grab unused pre-fusion:** add a small baseline Momentum-on-throw so the Brawler lane isn't dead before its capstone.
- **R4 — finisher trivializes density.** *Full Clear* is strong (cds + Overdrive), but Overcharge fill stays ~25s and *Cascade*'s refund is **capped +40%/encounter**, keeping effective finisher cadence ≥ ~12s. **Verdict: gated behind two Execution picks + a Prototype, so it's a late-run reward, not a default.**
- **Multiplier explosion.** Only Momentum multiplies; *Redline Governor* is the single ceiling raise (×3.0) and is unique. Auto-rate capped 6/s, arc 180°, HP floor 50. **No compounding runaway path exists in v0.1.**

### 8.6 Balance open questions (resolve in playtest)
1. Does the **6–8× Overdrive ceiling** feel like "screaming" or like "trivial"? Adjust base-stack contribution, not the Momentum curve (that's a §9 decision, not a Protocol one).
2. Is **XP exponent 1.35** giving 3–4 level-ups/encounter at target density? First number to instrument.
3. Do **Sirencatchers stealing Charge** starve the shop enough to matter without feeling unfair?
4. Should **Blacksite level-5 backstop** be higher (e.g. 7) so keystones land in encounters 4–5 (§12 curve) rather than mid-run?

---
*Next: hand the **MVP-8 subset** (§6 item 1) to engineering as a Protocol data-schema spec, or re-sync this v0.2 to
the Notion page. This doc feeds the GDD's "base game spec + implementation plan" follow-ups.*
