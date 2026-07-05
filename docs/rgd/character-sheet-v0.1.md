# Neon Responder: Night Shift — The Character Sheet

**Design doc · v0.1** · *Companion to [Protocol Stack](protocol-stack-v0.1.md) + [Special Moves](special-moves-v0.1.md).*
Anchored to **GDD v0.4** (`Neon_Responder_GDD`). Status: **design intent only** — the Character Sheet UI and
the **Gear meta layer** are **full-game, post-MVP**. The GDD cut meta-progression for the 48h jam (§8, §13);
Hybrid re-opens it *only for Gear* (a contained decision, made this pass).

---

## 0. Purpose & decisions

The Character Sheet is the **legibility surface for the entire build** — the one screen that answers *"who am I
this run?"* It's on-brand for the game's #1 pillar, **readable chaos** (§2, §10): the growth systems are otherwise
scattered (Protocols at level-up, Specials at the shop, Overlays computed invisibly), and this makes them one picture.

### The core insight — two modes
- **INSPECT (read-only):** the **Protocol stack** — you *drafted* these, you can't unequip them. Families, rarity,
  stack counts, active **Special Overlays**, and **hidden-tree progress** (unlocked / one-prereq-away hints).
- **MANAGE (equip / rank / spend):** your **loadout** — Specials, Gear, Items, Powerups.

**Protocols are inspected; everything else is managed.** That single split is what keeps the sheet from becoming a soup.

### Decisions & assumptions (flag if wrong)
1. **Scope = Hybrid (decided):** **Gear is persistent** (an owned collection, slotted **pre-run**); **Specials,
   Items, Powerups, Protocols are in-run** (reset each run). Gear is the *only* meta thread.
2. **GDD flag:** Gear persistence re-opens the **meta-progression** the GDD deferred (§8/§13). Contained to Gear;
   flagged, not silent.
3. **Growth axes stay distinct by timescale + interaction + role** (§1) so they don't blur into each other or into Protocols.
4. **Guardrail continuity:** all cross-axis bonuses are **additive** (only Momentum multiplies — Protocol doc §8.1);
   Gear sets *baselines*, Protocols/Specials add on top, and the same hard caps (auto-rate ≤ 6/s, etc.) apply to the sum.

---

## 1. The five growth axes (master reference)

| Axis | Timescale | Persistence | Interaction | Distinct role |
|---|---|---|---|---|
| **Protocols** | in-run | resets each run | **inspect** (drafted, accumulate) | the *emergent, RNG* build — families + hidden trees |
| **Specials** | in-run | resets each run | equip ×2, rank (shop) | *active abilities* (buttoned) |
| **Items** | in-run | resets each run | slot a few (drops / shop) | *passive rule-benders* (relics) — chosen, limited |
| **Powerups** | in-run | consumed | trigger / auto (shop / drops) | *one-shot / timed* boosts you spend |
| **Gear** | **META** | **persistent across runs** | equip **pre-run** from an owned collection | your *base chassis* — the baseline you bring in |

**The organizing logic:** shop/loadout = **deterministic agency**; draft = **RNG**. Gear/Items are the *chosen*
counterweight to the *earned* Protocol stack — the same symmetry that justified the Hard Split. **Overlap to police:**
"Item (passive modifier)" vs "Protocol (passive stack)" — Items are *chosen from a collection / limited slots*;
Protocols are *drafted by playing / permanent stack*. Keep that line sharp.

---

## 2. Sheet layout

| Panel | Mode | Contents |
|---|---|---|
| **Vitals** | inspect | Derived character: HP, armor, dodge charges, auto-rate/arc, Momentum cap/decay, current multiplier — the **summed** build. Makes the §8.1 power ceiling visible. |
| **Protocol Stack** | inspect | Families → protocols (rarity icons, stack counts); active **Overlays**; **hidden-tree map** with unlock hints (§3 of Protocol doc) |
| **Loadout** | manage | **Special ×2** · **Gear** (fixed slots) · **Item** slots · **Powerup** pouch |
| **Run status** | inspect | Signal (dawn) bar, encounter progress |

### Access points (respect the real-time combat — no mid-fight fiddling)
- **Pre-run:** MANAGE — set your **Gear** loadout from the collection. (The sheet doubles as the run's home screen.)
- **Shop beat (between encounters):** MANAGE — buy/equip/rank **Specials, Items, Powerups**; spend Neon Charge.
- **Pause (anytime):** **INSPECT only** — read your build; no equipping mid-encounter.
- **Level-up:** a **compact glance** of the sheet to inform the pick-1-of-3 (not the full manage view).

---

## 3. Gear — the persistent layer (new)

Gear is the **hardware you bring in**; it sets the *baseline* the run's Protocols/Specials then build on. Three slots,
each mapped to one core loop stat so the sheet reads cleanly:

| Gear slot | Sets the baseline for | Ties to family |
|---|---|---|
| **Auto-Weapon** | auto-engage rate / arc / pierce / projectiles / chip-damage | Auto-Gear ("Grinder") |
| **Chassis** | max HP / armor / dodge charges | Defense ("Night Watch") |
| **Frame** | Momentum start-tier / decay, Overcharge fill | Momentum ("Redline") / Execution |

- **Gear sets the base knob; Protocols add on top** (additive per §8.1) — e.g. a high-rate Auto-Weapon *plus*
  *Overclocked Coil* stack, both under the 6/s cap. No new multiplier.
- **Archetype seeding (design lever, open Q2):** a Gear piece may grant a **starting signature Protocol/Special**
  that biases a build (e.g. a "Redline Frame" starts you at Warm and pre-slots *Afterburner*). This is how different
  loadouts feel like different characters on run 1, before any drafting.
- **Meta-unlock (open Q1):** new Auto-Weapons / Chassis / Frames unlock into the collection across runs (via run
  milestones or a meta currency — TBD). This is the **meta replayability engine**, complementing the *in-run* RNG
  replayability of Protocols: pick a Gear lean pre-run, then draft into or against it.

---

## 4. Items & Powerups (in-run)

- **Items (relics)** — a few slots (proposed **3**), found as drops or shop-bought, **reset each run**. Passive
  rule-benders that enable builds: e.g. *"+Charge on Sirencatcher kill"*, *"overkill damage → Overcharge"*.
  Distinct from Protocols: **chosen + limited + swappable** vs. drafted + permanent stack. (Own future doc.)
- **Powerups** — a small **pouch** (proposed 2–3 charges), bought/dropped, **consumed on use**: instant heal, brief
  invuln, instant Overdrive, a timed fire-rate spike. Distinct from Specials: **spent one-shots** vs. repeatable cooldown abilities.

---

## 5. Cross-system consistency
- Reaffirms Protocol doc **§8.1**: only **Momentum** multiplies; Gear/Item/Powerup bonuses are **additive**; the
  Momentum-family Special Overlay remains the single deliberate multiplier exception.
- Gear baselines feed the **same hard caps** (auto-rate ≤ 6/s, arc ≤ 180°, HP floor 50) — the Vitals panel shows the
  post-cap summed result so the ceiling is honest.
- **Special Overlays** (≥3-in-a-family) are computed from the *drafted* Protocols, not Gear — but Gear that *seeds*
  a family (Q2) can accelerate reaching an overlay threshold. Nice, intended synergy.

---

## 6. UI / implementation note (light)
The Character Sheet is a **new `UIMenu`** and fits the existing **`UIManager` named-menu switcher** pattern
(`Scripts/UI/`, `[BUILT]`) — `ShowMenu("CharacterSheet")` from pause / shop / pre-run. It reads (does not own)
the growth state, so it's a **view over** the Protocol/Special/Gear services when those exist. `[GDD-TARGET · not built]`.

---

## 7. Scope & open questions
**MVP (§13):** none of this is in the jam build. At most, a **minimal inspect-only pause overlay** listing drafted
protocols. The full Character Sheet + Gear meta is a **full-game system**, the furthest-out of the three docs.

**Open questions:**
1. **Gear meta-currency:** achievement-unlock vs. an earned currency (e.g. "Grid Credits" per run)?
2. **Archetype seeding:** does Gear grant a starting signature Protocol/Special, or only stat baselines?
3. **Gear slot count:** 3 (Auto-Weapon / Chassis / Frame) — right, or add a 4th (Boots / utility)?
4. **Item slots + source:** how many, and drops / shop / both?
5. **MVP inspect overlay:** worth the jam cost, or cut entirely?
6. **Sheet-open in real-time:** confirm INSPECT-on-pause doesn't dent the "no menu-fiddling" flow (read-only should be safe).

---
*Next: publish to Notion alongside the other two (on your go), or push into any of the open questions — the Gear
meta-currency (Q1) is the one that unlocks a real meta-progression design.*
