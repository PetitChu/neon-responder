# Neon Responder: Night Shift — Special Moves

**Design doc · v0.1** · *Companion to the [Protocol Stack](protocol-stack-v0.1.md); expands GDD §6.7 (Manual Specials) + §8 (economy).*
Anchored to **GDD v0.4** (`Neon_Responder_GDD`). Status: **design intent only** — Specials are
`[GDD-TARGET · not built]`, further from built than Protocols: only **1 Special** is even in MVP scope (§13).

---

## 0. Purpose & decisions

Specials are the player's **deliberate, on-demand** tools — the "manufacture a wave of Finish-Ready" button
(§6.7; §11 "special-made wave") — in contrast to auto-engage's *passive* throughput and the contextual verbs'
*moment-to-moment* finishing.

### Decisions & assumptions (flag if wrong)
1. **Channel split (your call):** **Specials are bought/upgraded in the inter-level Neon Charge shop; Protocols
   come from in-run level-ups.** Clean separation of the two growth systems.
2. **Consequence — flag:** this **supersedes Protocol doc §8.3** (which had Tuned/Prototype protocols also
   purchasable in the shop) and **diverges from GDD §8** (shop sold "bigger Protocols"). Net effect: protocols
   become level-up-only (more draft-RNG), the shop becomes Specials-centric (deterministic). *If you want the
   shop to retain some protocol determinism, say so — see §6 open Q1.*
3. **Specials use a curated roster + ranks, not the protocol rarity system.** Few specials, 2 equip slots,
   deliberate purchases → tiers-by-cost + upgrade ranks fit better than random rarity.
4. **Protocol families reshape specials ("the family affects the special-type"):** beyond the Specials family's
   numeric buffs, investing in a family imprints a **Special Overlay** that changes how your specials *behave* — §4.
   This is the cross-system replayability hook.
5. **Momentum stays finishing-only:** a special never grants Momentum directly; the finishes it *enables* do (v0.4 rule).

---

## 1. What a Special is
- **Slots:** 2 equipped — Special 1 & Special 2 (§5; Special 2 is nice-to-have per MVP). Keyboard **A / S**,
  gamepad **LB / LT** (§0.4.c).
- **Activation cost:** cooldown **+** Neon Charge per use (§6.7; §9 base cd 6s / Charge 20). Placed or aimed by type.
- **Core role:** manufacture Finish-Ready waves + control/utility. They complement, never replace, auto-engage and the verbs.
- **Distinct from Overcharge:** Overcharge (§6.9) is the meter-gated screen-clear; Specials are cheaper, repeatable,
  cooldown-based. Keep them different in feel.

---

## 2. The Special roster (7 across 5 types)

**Type** = the special's shape/behavior (the "special-type" the overlays in §4 hook into).
Format: **Name** *(type)* · effect · cd / Charge · Finish-Ready role. † = named in GDD.

| Special | Type | Effect | cd / Charge | Finish-Ready role |
|---|---|---|---|---|
| **Siren Pulse †** | Pulse | radial stun + knockback + brief light-reveal | 6s / 20 | **mass-triggers Finish-Ready** in radius — flagship wave-maker; **the MVP special** |
| **Neon Barricade †** | Barrier | placed light-wall blocks horde inflow on a lane/side for a duration | 10s / 25 | none directly — buys positioning / node defense |
| **EMP Line †** | Line | linear discharge down a lane: stagger + disable (shuts Jammer Drones, §7) | 8s / 20 | staggered enemies → Finish-Ready along the line |
| **Overload Coil** | Field | lingering electric zone: chip-DoT that pushes enemies in it toward the Finish-Ready threshold | 12s / 30 | sustained Finish-Ready generator on a chokepoint |
| **Dispatch Beacon** | Deployable | drop an autonomous drone that auto-fires like a 2nd auto-engage source (duration) | 14s / 35 | hands-free Finish-Ready pressure |
| **Flashbang Halo** | Pulse | fast, cheap blind/stun + reveal; **no** Finish-Ready trigger | 5s / 15 | none — defensive panic/reset (pairs with R8 whiff-recovery) |
| **Rescue Flare** | Field | mark + pull civilians toward you + brief chaff-repel (§6.10 Rescue aid) | 12s / 25 | none — objective-focused |

*(new)* = Overload Coil, Dispatch Beacon, Flashbang Halo, Rescue Flare. **MVP = Siren Pulse only** (§13/§0.4.g).

---

## 3. The inter-level shop (Specials economy)
- **Where:** between encounters (§8 shop beat). Currency: **Neon Charge** (~60–100/encounter; Sirencatchers steal it, §7).
- **Buy = unlock.** Owning a special lets you equip it; **equipping/swapping owned specials is free** between
  encounters (the GDD "special swap", §8).
- **Ranks R1→R3** deepen a special — the shop analog to protocol stacking.

| Shop item | Charge cost |
|---|---|
| Unlock a Special (Pulse / Line tier) | 40 |
| Unlock a Special (Field / Deployable tier) | 55 |
| Special rank R2 / R3 | 30 / 60 |
| Heal | 25 |
| Reroll shop stock | 10, +10/use (resets per shop) |

Rank effects (tuned §5): **R2** −20% cd, **R3** +radius/power (or +duration for deployables). An unlock (~40–55) ≈
half an encounter's Charge → a real spend-vs-heal-vs-rank decision.

---

## 4. Protocol → Special interaction

### 4a. Specials family ("The Deployables") — numeric buffs *(from Protocol doc)*
*Capacitor* (−cd), *Wide Field* (+Finish-Ready radius), *Priority Override* (Siren Pulse → objective),
*Dispatch Protocol* (every Special → objective + Charge refund). These stack onto all equipped specials.
Not redefined here — see Protocol doc §4/§8.4.

### 4b. Family Overlays — "the family affects the special-type"
Investing **≥3 protocols in a family** imprints a **Special Overlay**: a passive rider that changes what ALL your
specials *do*, re-typing them toward that family's fantasy. **Cap: your specials carry overlays from at most your
2 highest-count families** (readability contract, pillars 2–3).

| Family (≥3 held) | Special Overlay — how it changes the special-type |
|---|---|
| **Auto-Gear · Grinder** | Specials also emit a burst of auto-fire projectiles on cast (adds chip + Finish-Ready) — every special gains a "fire" component |
| **Momentum · Redline** | Special radius/power scales with current Momentum tier; at Overdrive, −30% cd — specials get hungry for heat |
| **Execution · Last Call** | Enemies a special pushes to Finish-Ready get **one free auto-chained finish** — specials become mini-Overcharges |
| **Brawler · Hands** | Specials knock down + set up grabs; Barrier/Line specials fling clipped enemies (special gains a "throw" rider) |
| **Scavenger · Salvage** | Specials may drop a weapon/prop on cast; kills in the special's radius refund Charge |
| **Specials · Deployables** | (direct numeric buffs — §4a) |
| **Defense · Night Watch** | Casting a special grants a brief shield/armor — specials double as defensive beats |
| **Objective · Dispatch** | Specials advance the objective bar while active in the node zone (Priority Override, generalized) |

**Result:** the **same 3 specials feel different per build** — a Redline build's Siren Pulse scales with Momentum;
an Execution build's Siren Pulse chains free finishes; a Brawler build's EMP Line throws bodies. That's the
cross-system replayability multiplier the request is after.

---

## 5. Parameters & balance (Phase 5-lite)
- **Charge economy sets the pace:** unlock ~40–55, so specials come online over 1–2 encounters (§12 shop beat).
- **cd / Charge** per special: §2 table. **Rank** R2 −20% cd; R3 +30% radius/power or +50% duration. *(new knobs)*
- **Guardrails (consistent with Protocol doc §8.1):**
  - Specials manufacture Finish-Ready **on cooldown + Charge only** — can't spam-trivialize density (R4-adjacent).
  - The special itself grants **no Momentum**; the finishes it enables do (finishing-only rule).
  - **Overlays cap at 2 families** for readability + power control.
  - Overlay riders are **additive**, except the Momentum overlay (scales with tier) — the single deliberate
    Momentum-multiplier exception, matching §8.1's "only Momentum multiplies."
- **Anti-redundancy:** Flashbang Halo (control, no Finish-Ready) vs Siren Pulse (wave-maker) must stay distinct —
  if they converge in playtest, cut/merge (echoes the GDD jump-attack-vs-Siren-Pulse open Q5).

---

## 6. MVP subset & open questions
**MVP (§13/§0.4.g):** **Siren Pulse only** — no shop, no ranks, no overlays. The whole shop + overlay layer is a
**full-game system**, further out than the Protocol stack. Prove Siren Pulse's "special-made wave" (§11) first.

**Open questions:**
1. **Shop determinism (the fork):** with protocols now level-up-only, do you want the shop to retain *some* protocol
   agency (a "targeted" protocol offer, or a protocol reroll token) — or is a hard split (Specials=shop,
   Protocols=draft-only) the intent? **This decides whether I edit Protocol doc §8.3.**
2. **Overlay threshold:** ≥3-per-family (chosen) vs a scaling ≥2/≥4 (weak/strong)?
3. **Overlay cap:** 2 families (chosen) vs 1 (cleaner identity) vs uncapped (busier)?
4. **Special 2 slot:** MVP-cut per §5 — confirm the shop assumes 2 slots for the full game.
5. **Rank vs breadth:** should Charge favor deep-ranking one special or owning many? (tune unlock vs R2/R3 costs.)

---
*Next: publish to Notion alongside the Protocol Stack (on your go — outward-facing), and/or reconcile
Protocol doc §8.3 to the channel split once Q1 is decided.*
