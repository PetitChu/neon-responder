# Neon Responder: Night Shift — The Avatar ("NR-0047")

**Design doc · v0.1** · *Companion to [Character Sheet](character-sheet-v0.1.md) + [Special Moves](special-moves-v0.1.md) + [Protocol Stack](protocol-stack-v0.1.md).*
Anchored to **GDD v0.4** (`Neon_Responder_GDD`). Status: **design intent + first working sprite accepted**
(`Assets/_generated/Sprites/Test_Sprite.png`, see §6).
This doc is the **protagonist bible**: lore, persona, voice, visual guide, and a Unity-AI **sprite tech spec**.
It feeds the "sprite swap" item in the Feel & Level pass and the Character Sheet's "who am I this run" portrait.

---

## 0. Purpose & decisions

The GDD gives the *world* and the *loop* but never names the man swinging the blade. §2–§3 speak **in his voice**
("the block gets brighter because I showed up") without ever saying who he is. This doc fixes that: one protagonist,
built to serve the game's #1 pillar — **readable chaos** — by being **the single brightest, most legible thing on a
dark screen**, and to carry the GDD's **Kung Fury tone** as its deadpan center.

### The one-line identity
> **A masterless swordsman took the only job left in a city going dark — night-shift grid responder — and turned
> "answer every call" into his new code. His blade grinds the horde into sparks, his hands finish them, and block
> by block he keeps the last lights on until dawn. He is not on a mercy mission or a revenge mission. He's on a
> *shift* — and the neon apocalypse is just what's standing between him and clocking out.**

### Decisions & assumptions (flag if wrong)
1. **Role fusion = "Grid Ronin" (decided):** a *ronin* (warrior whose lord is gone) and a *first responder* (warrior
   whose only master is the call) are **the same person** — one who serves people directly because there's no one
   left to serve. This single idea resolves every requirement: why he's alone ("the last one awake"), why he has a
   code (sincere heroism), why he *fixes* rather than conquers (the win-metric), and why he **glows** (a responder's
   hi-vis strips *become* a samurai's honor-light).
2. **Tone = parodic Kung Fury Samurai (decided, retuned during design from an earlier "sincere hero" read):** committed **deadpan camp** — total sincerity in
   service of maximum absurdity + radical excess. He never winks. The world is a synthwave fever dream; he treats
   saving it like filing a work order. **The comedy engine is cosmic, merciless violence treated as a mundane annoyance.**
3. **Mercy correction — flag:** "Stabilize, don't exterminate" (GDD pillar 1) is a **systems/win-metric** rule
   (objectives win, not kill-count; the horde is infinite so you *can't* exterminate it) — **NOT a character trait.**
   His *method* is no-mercy, stylish obliteration. The horde is **traffic**; he walks through it. This keeps the GDD
   pillar intact while making him a wrecking machine.
4. **Samurai form = signature blade + improvises (decided):** one iconic neon blade (reads as his auto-engage "gear"
   grinding chaff) **plus** picked-up street props (§6 combat verbs). Blade = who he is; props = what the street gives him.
5. **Male protagonist** (assumed from brief). Single fixed character — the GDD cuts "multiple characters" (§13).
6. **Name = Kaito Mori** (confirmed for now). **Callsign = "NR-0047" (decided):** pure bureaucracy,
   no myth-name — the city's deadliest swordsman is a payroll number, and dispatch reads god-tier carnage as routine
   ticket-closing. The legend lives in what he *does*, not what he's called. "NR" doubles as *Neon Responder*.
7. **Sprite height = 128px** (decided) — rich enough for "HD pixel-art," multiple-of-8, sets the whole pixel budget.

---

## 1. Lore & world-role

- **The world:** a sprawling neon megacity runs on a fragile power **Grid**. The **Blackout Idol** and its
  signal-parasites are draining the city into darkness node by node — **Jammer Drones** dim the streets, the horde
  surges wherever the light dies. Emergency services collapsed or fled. The **Signal** bar (GDD §12) is the city's
  dwindling will-to-dawn.
- **His past (kept jam-vague on purpose):** a swordsman bound to an order — a corp-clan, a dojo-guild, pick your
  poison — that **went dark first** in the blackout. Left without a master and without a war, he did the only
  honorable thing a blade can do when there's no lord: **he started answering the calls himself.** He clipped a
  dead responder's dispatch-beacon to his obi and clocked in.
- **The night shift = the run:** the whole run is **one long night**. Every encounter is **a call**. The Signal is
  the clock. **Win = dawn breaks = end of shift = he finally gets to rest.** His entire want is small and human —
  he wants to go home — and *that* is the sincere spine under the camp.
- **The mundane frame (the parody premise):** an ancient legendary warrior is doing **gig-economy overtime** saving
  the apocalypse. "Neon Responder: **Night Shift**" — the joke is that the end of the world is just his *shift*.

---

## 2. Persona, tone & voice

**The register: deadpan.** The Kung Fury trick is *not* winking — it's playing ludicrous things 100% straight.
He under-talks, he's mildly annoyed by the apocalypse, and he treats god-tier carnage as normal overtime. The absurd
neon monsters are funny *because he is the straight man*, not the clown.

- **To the horde:** contempt-as-traffic, never hatred. *"Grid's down on 5th. Guess I'm walking through all of you, then."*
- **Mid-massacre (flat):** *"This is why I don't take day shifts."*
- **On the odds:** *"Ten thousand of you. One of me."* … *"Unfair. For you."*
- **Level-up pick:** *"Overtime better be neon."*
- **To the boss (Blackout Idol):** *"You're my last call. Then I'm going home."*
- **On a stabilized node:** *"...one down. Dawn's at six. You're all done by five."*
- **Asked the blade's name:** *"It has a name."* (turns the scabbard: **COMPANY PROPERTY**)

**Dispatch / UI voice (the world talking *to* him)** stays the GDD's self-aware copy — "*signal in the static*",
"*bring back the dawn*", "**NR-0047, YOU'RE LATE.**" The gag: the city addresses a mythic swordsman by employee ID,
like a late field tech.

**Whiff / low-Momentum voice:** losing his flow reads as *losing his cool* — a beat of un-composed irritation, never
despair. The stakes are pride and the clock, not angst.

---

## 3. Character description (the collision)

The design *is* the joke: **full traditional samurai silhouette + unmistakable modern first-responder gear.** A ronin
wearing a hi-vis vest. Play the collision straight and it lands as both cool and funny.

- **Build & bearing:** lean, economical, unhurried. Moves like someone who has done this every night for a decade.
- **Samurai layer:** long **haori/coat** over lean segmented **armor**; asymmetric kimono-wrap collar; **obi/sash**;
  topknot or shaved-side undercut; a half-face **respirator mask** (night-shift smog + keeps him a myth, not a face).
- **Responder layer:** a **hi-vis harness** with reflective banding *rendered as neon light-lines*; a **dispatch
  beacon** clipped to the obi (blinking); a shoulder **mon** (family crest) that doubles as his **unit insignia —
  NR-0047**. The responder's kit and the samurai's kit are **the same objects**.
- **The signature blade — "COMPANY PROPERTY":** a neon-edged **grid-blade** that is *also his tool* — he reboots nodes
  and cuts the horde with the same instrument. The street swears the legendary blade has a secret name; it does — it's
  the inventory stamp on the scabbard: **PROPERTY OF GRID MAINTENANCE DIV.** Worn at the hip; drawn iai-style.
- **Absurd-mundane prop (optional flavor):** a laminated badge / a convenience-store coffee — one small mundane
  object to sell the "it's a job" gag against the mythic figure.

---

## 4. Visual guide / art direction

The entire look is one idea: **darkness everywhere, and he is the light source.**

- **Neon/Dark palette:** near-black / deep-indigo base (coat, armor, night) so neon *pops*. Muted, desaturated
  everything-else. He is the highest-contrast object in frame at all times (GDD "hot vs ambient" render rule, §10 —
  he is the brightest *hot* entity).
- **Self-illumination:** he casts his own light. "The block gets brighter because I showed up" is **literally true** —
  his glow should visibly lift the local scene.
- **Momentum = his body (the RGD money-shot):** his neon linework **heats up by Momentum tier**, so his core
  mechanic is readable *on him*, no HUD glance needed:

  | Momentum tier | Linework state | Feel |
  |---|---|---|
  | **Cool** | dim cyan, thin lines | idling, composed |
  | **Warm** | brighter aqua/teal, lines thicken | warmed up |
  | **Hot** | hot-magenta, edges bloom, faint trails | cooking |
  | **Overdrive** | white-hot strobe, full motion-trails, screen-lift | a strobing neon god (GDD "screaming build") |

  *Constraint:* match these to the **HUD Momentum band colors** (M2 growth HUD) so body and meter agree. Tunable.
- **Eastern influence:** asymmetric wrap, sash, the **mon** crest, iai draw poses, a restrained line economy (a few
  bold shapes, not clutter) — reads as ukiyo-e-meets-synthwave, not costume-shop "ninja."
- **Whiff read:** on a whiff, the glow **guts to cyan** and stutters for the vulnerability window — a visible
  "lost my cool" beat.

---

## 5. Mechanical embodiment (he *is* the RGD pillars)

Not just flavor — every system has a character read, so the fiction and the mechanics reinforce each other.

| System (GDD) | How the avatar expresses it |
|---|---|
| **Auto-Engage** (§6.1) | the blade **grinds chaff** on its own rhythm — his gear does the softening, his hands do the finish |
| **The Act verbs** (v0.4) | weapon-attack = **iai draw-cut**; grab→throw = **judo/aikido**; jump-attack = **hop-and-slam**; props = a ronin using whatever the road provides |
| **Finish-Ready** (§6.2) | an enemy overloads/glitches; he **resolves** it — no ceremony, it's traffic |
| **Momentum** (§6.4) | his **heat/flow** — the linework ramp in §4; a whiff = losing his cool |
| **Overcharge** (§6.9) | a signature **screen-clear flourish** — the one moment he's allowed to look *maximally* cool |
| **Stabilize / Signal** (§6.10, §12) | the **shift & the clock** — objectives are the job; dawn is clocking out |

---

## 6. Sprite tech spec (Unity-AI generation-ready)

**Target:** single character, **128px tall** at idle bounding box, side-view (belt brawler), **canonical facing =
right** (mirror for left), transparent background, high-definition pixel-art.

**Current working sprite (accepted "for now", 2026-07-05):** `Assets/_generated/Sprites/Test_Sprite.png` — 512×512
Unity-AI output of the seed below. On-spec: topknot, respirator, dark-indigo robes, shoulder mon, neon-edged blade,
facing right. Accepted deviations: harness reads **safety-green vest** (sharper "ronin in a work vest" gag than the
cyan light-lines — keep); 512×512 canvas (scale to 128px in-game height via import PPU, not native-128). Treat as the
**Cool** base reference until a final animation set is authored.

**Style rules:**
- Crisp pixels, **no anti-aliased blur**; hard cel shading; bold readable silhouette that survives at belt scale
  against 80–150 hot enemies.
- **Limited-but-rich palette:** ~16–24 base colors (dark chassis + skin/cloth) **plus** the neon accent ramp.
  Base stays dark; **all "pop" comes from emissive neon**, never from bright base fills.
- **Consistent self-lit key:** light reads as coming *from him* (rim/emissive), not an external sun.

**Momentum glow = a separate emissive overlay layer (production trick):** author the **base sprite** dark/unlit, and a
**single glow-mask** for the linework. Tint that one mask per tier (cyan→aqua→magenta→white) instead of redrawing the
character 4×. In **Built-in RP** (this project's pipeline), drive it as an **additive child sprite** + bloom so it
works without URP emission. One mask, four tints, done.

**Animation set (map 1:1 to the combat verbs; frame counts are starting knobs):**

| Clip | Frames | Notes |
|---|---|---|
| Idle | 4–6 | subtle breath + beacon blink + linework flicker |
| Walk (belt) | 6–8 | X-move; lane shift is a small Z-hop |
| Dash / Dodge | 3–4 | i-frame roll (GDD Dodge); doubles as whiff-recovery |
| Punch | 3–4 | default finish (v0.4 dispatch-5) |
| Kick | 3–4 | knockdown-favored |
| Grab | 2–3 + hold | enter + carry loop (no i-frames — the cost) |
| Throw-enemy | 4–5 | **biggest hit in the kit** — sell it |
| Weapon-attack | 3–4 | iai draw-cut; swappable weapon overlay |
| Weapon-throw | 3–4 | ranged finisher; weapon spent |
| Jump + Jump-attack | 4 + 3 | vertical hop-and-slam, landing AoE |
| Hurt / Stagger (whiff) | 2–3 | glow guts to cyan |
| Finish flourish | 3–5 | the chain-pop signature pose |
| Overcharge | 6–10 | the showcase screen-clear |
| Level-up pose | 2–3 | slow-mo pick beat ("...this'll do.") |
| Clock-out / Victory | 4–6 | dawn beat — he finally relaxes |
| KO / Down | 3–4 | fail state |

**Generation prompt seed (paste-ready starting point):**
> *"High-definition pixel-art sprite, side view, full body, 128px tall, transparent background. A lean masterless
> samurai (ronin) in a dark indigo haori over segmented tech-armor, half-face respirator, topknot; wearing a modern
> hi-vis first-responder harness whose reflective strips glow as thin cyan neon light-lines; a glowing shoulder crest;
> a neon-edged katana at the hip; a small blinking dispatch beacon on the sash. Near-black palette, self-illuminated,
> crisp pixels, hard cel shading, no blur. Deadpan, cool, slightly tired posture. Neon-noir cyberpunk with Japanese
> Edo influence."*
> **Avoid:** realistic 3D render, soft anti-aliasing, chibi proportions, bright non-neon fills, gun as primary weapon,
> generic "ninja."

Author the **Cool** base first; derive the other three heat states by re-tinting the glow mask (do not regenerate the body).

---

## 7. RGD & card threading (touch-points)

Where the avatar plugs into the existing design surface. (This doc = source of truth; edits to siblings are a light
voice pass, not a redesign.)

1. **This doc (`avatar-v0.1.md`)** — the bible. New, done.
2. **[Character Sheet](character-sheet-v0.1.md)** — the "who am I this run" screen is literally *him*: add a
   **portrait + name/callsign** to the Vitals panel; the **Gear** slots map to his kit (Chassis = armor, Frame =
   his heat, Auto-Weapon = **his blade**).
3. **[Special Moves](special-moves-v0.1.md)** + **[Protocol Stack](protocol-stack-v0.1.md)** — optional flavor pass so
   families read as **facets of him**: Momentum·**Redline** = his heat, Defense·**Night Watch** = his vigil,
   Execution·**Last Call** = his no-mercy finish code, Scavenger·**Salvage** = the ronin improviser.
4. **GDD (Notion)** — his identity currently lives implicitly in §2–§3; cross-link this bible, and (on your explicit
   go, since it's outward-facing) push a short **Character** section to the GDD page.

---

## 8. Scope — MVP vs full game

- **MVP (48h jam):** one **playable 128px sprite set** covering the MVP verbs (idle, walk, dodge, punch, grab,
  throw-enemy, kick, one weapon-attack, hurt, finish, overcharge) + the **4 Momentum heat tints** via the glow mask.
  Lore/persona/voice are **flavor polish** — a handful of the §2 barks + the dispatch UI voice is plenty for the slice.
- **Full game:** the complete animation set (§6), boss-specific barks, the mundane-prop gag, alt costumes / neon paint
  (the GDD's cut-without-regret cosmetic meta), and the Character-Sheet portrait integration.

---

## 9. Open questions

1. **Callsign — resolved: "NR-0047."** Pure-bureaucracy tag, no myth-name (the two-layer street-legend gag is
   dropped). Dispatch addresses him by employee ID; the "NR" reads as *Neon Responder*.
2. **Given name — resolved: Kaito Mori** (confirmed for now; revisit only if the voice pass surfaces a better fit).
3. **Blade name — resolved: "COMPANY PROPERTY."** The legend's true name is the inventory stamp on the scabbard
   (**PROPERTY OF GRID MAINTENANCE DIV.**) — maximum deadpan, extends the NR-0047 payroll gag. Bark: *"It has a name."*
4. **Momentum heat ramp:** cyan→aqua→magenta→white (proposed) — confirm it matches the **HUD Momentum band colors**
   from the M2 growth HUD, or re-tint to match the meter.
5. **Face reveal:** respirator always on (stays a myth) vs. a dawn/clock-out unmask beat? Affects the victory pose.
6. **How mundane is the frame?** Light background gag (a beacon + a coffee) vs. a running bit (dispatch nags him about
   overtime, timesheets). Decides how much UI copy leans into the "it's a job" joke.

---
*Next: on your go — (a) generate the Cool base sprite from the §6 seed and derive the heat tints, (b) do the light
voice pass on the Character Sheet + families. The Character-Sheet portrait is the first place he becomes visible
in-build.*
