# Level 01 — "First Response" · Rain-Slick Downtown Strip
## Layout & architecture design (v0.1)

**Status:** DESIGN — layout/structure **locked at v0.1**. This is the level-design deliverable, not a
build order. Numbers are first-pass, to be tuned in-editor (runtime is ground truth).

**Purpose:** This is the *"new level purpose-built for the gameplay"* design that the **Feel & Level
pass pre-brief** (`docs/superpowers/plans/2026-07-04-feel-and-level-pass-pre-brief.md`, item 1) is
**gated on**. The Feel & Level spec is written *against this document*. It also gives the swarm rework
(pre-brief items 4/5/6) a concrete level to target.

**Game:** Neon Responder — Night Shift · PC/Steam 2D side-scroll beat-'em-up · single-player · **NOT VR**.
**Grounded against:** branch `claude/neon-m2-growth` — the engagement + growth loop is *live* here
(`Level.RegisterEngagementSystems` wires `AutoEngageSystem`, `FinishReadySystem`, `FinishReadySelector`,
`FinishResolver`, `ProtocolEffectsSystem`, `SwarmBridge`). Config field names cited are real.
**Author:** Sebastien Charron · **Date:** 2026-07-04

---

## 1. Experience goal (locked)

The first level's single job is to make the **core loop legible**:

> auto-engage → **Finish-Ready** → contextual **Act verb** → **Momentum** builds → the build fires
> harder / earns faster → ending on the player's **first Overdrive high**.

It teaches **one concept per intimate corridor**, then **tests that concept in the next wide plaza**,
escalating density the whole way. The player should leave Level 1 understanding *why* the street keeps
getting wider — because width is the pressure, and the loop is how you survive it.

## 2. The locked design forks

| Fork | Decision |
|---|---|
| Experience goal | **Teach the loop cleanly** (controlled ramp, ends on first Overdrive) |
| Width & camera | **Dynamic per-zone** — corridors read zoomed-in, plazas read zoomed-out |
| Macro structure | **Corridor → plaza hybrid** — pacing expressed *spatially* |
| Theme | **Rain-slick downtown strip** — neon signage, wet asphalt, storefronts, night market, a big crossing |

## 3. Core spatial principle — fixed zoom, *faked* width

`CameraFollow` runs on a **`PixelPerfectCamera`**: framing is locked to `refResolution` + `assetsPPU`,
there is **no runtime orthographic-size control**, and changing zoom on a pixel-perfect camera either
snaps between integer scales or breaks crisp pixels. **So the camera zoom never changes.**

Instead, the narrow↔wide feel comes from **how much of the fixed frame is walkable** — the standard
pixel-brawler trick:

- **Corridors** — tight walkable band (~2.5 u) + heavy **foreground/background occluders** (brick walls,
  dumpsters, scaffolding, parked cars, overhangs) that eat the top and bottom of the frame → *intimate,
  zoomed-in*.
- **Plazas** — walkable band opens (~6–7 u), occluders pull back to the frame edges, the swarm fills the
  depth → *wide, open, zoomed-out*.

Walkable width per zone is authored purely as **scene floor/boundary colliders** — no code, fully
per-zone. Camera arena locks reuse the existing per-wave `HasCameraBound` / `CameraBoundProgression`.

## 4. The street — five zones

Progression 0→1 mapped to world X with `Level._levelLength ≈ 96` (up from the current placeholder 50),
`_levelStartX = 0`. (Earlier sketch names Cold Open / First Blood / … are re-themed below.)

| # | Zone (themed) | Prog | World X | Type | Walkable | Teaches |
|---|---|---|---|---|---|---|
| 1 | **Service Alley** | 0.00–0.10 | 0–10 | corridor ~2.5 u | move + **auto-engage** |
| 2 | **Storefront Row** | 0.10–0.28 | 10–27 | pocket plaza ~4 u | **Finish-Ready + Act verb** |
| 3 | **The Scaffold** | 0.28–0.42 | 27–40 | corridor ~2.5 u | **Momentum chain** (Hot escalation) |
| 4 | **Night Market** | 0.42–0.72 | 40–69 | wide plaza ~6–7 u | **swarm density** inside the loop |
| 5 | **Neon Crossing** | 0.72–1.00 | 69–96 | widest plaza ~7 u | **Overdrive** payoff |

### Zone 1 — Service Alley *(corridor · teach: auto-engage)*
A wet back-alley off the strip: brick, drips, a single buzzing sign. Occluders tight top and bottom;
walkable strip ~2.5 u. **2–3 lone thugs**, well spaced. The player learns that walking right makes the
character **auto-engage and basic-attack** — they only steer and position. Lowest stakes in the game.
Swarm near-silent (ambient only). **No camera lock** — free scroll teaches forward motion.

### Zone 2 — Storefront Row *(pocket plaza · teach: Finish-Ready + Act verb)*
First step onto the lit strip — shopfronts, awnings, a little breathing room (~4 u). The **camera locks**
into the first real arena. Enemies are paced so they enter the **Finish-Ready** window **one at a time**,
so the player can't miss the tell and the **`UIHUDFinishPrompt`** verb cue (`KICK 1/2` → "FINISH!"). This
is the heart of the game introduced in isolation. Low chaff.
> **Dependency:** this teach is only as good as the finish prompt UI, which is deliberately minimal today
> (pre-brief item 2). If Feel & Level improves the prompt, this zone benefits directly.

### Zone 3 — The Scaffold *(corridor · teach: Momentum chain)*
Back to tight: a covered construction arcade between two buildings, single-file, scaffolding and plastic
sheeting as occluders (~2.5 u). A **line of enemies to finish in quick succession** → **Momentum climbs**
and the finish challenge **escalates ("Hot")** — the M2 *tiered hero finish / verb-sequence* behavior.
The corridor's single file makes the chain obvious: finish, the next steps up, finish. First taste of
"fire harder / earn faster." Short `CooldownBetweenSpawns` keeps the chain alive.

### Zone 4 — Night Market *(wide plaza · teach: swarm under the loop)*
The reveal, and the game's thesis statement. The strip opens into a **rain-slick night-market
intersection** — food stalls, steam, string lights, a milling crowd. Walkable opens to ~6–7 u; occluders
retreat; the **active chaff swarm fills the depth as a crowd** (see §6 — *not* lanes). Everything now
combines: auto-engage carves the chaff, Finish-Ready pops on tougher enemies mixed into the crowd,
Momentum sustains through sheer volume. A deliberate **mid-plaza lull** (breather) precedes the push to
the far side. **Camera locks** for the arena.

### Zone 5 — Neon Crossing *(widest plaza · payoff: Overdrive)*
The widest, most open space: a **major intersection under a giant neon billboard**, rain sheeting through
the light. Density peaks and an **elite / mini-boss anchor** holds the far side. Sustained finishing tips
Momentum into **Overdrive** — the power-fantasy crescendo the whole level has been building toward. The
existing **`SlowMotionOnLastKill`** punctuates the final finish. `EndLevelWhenAllWavesCompleted` → the
LevelCompleted menu.
> **Verify-in-play:** Overdrive's feedback ("the scream") is the *open hands-on gate* on this branch
> (M2 gate record). Zone 5's payoff must be validated in a real run, not trusted on paper.

## 5. Pacing / density curve

A **rising sawtooth**. Corridors are the breathers *and* the classroom (low intensity, but each with a
higher floor than the last); plazas spike. The player quickly internalizes **"wide street = brace."**
Width becomes the difficulty tell, so the level teaches its own pacing without a tutorial pop-up.

```
intensity  ▁▁      ▃▃            ▂▂                 ▆▆                        ██
           Alley   Storefront    Scaffold           Night Market            Neon Crossing
           (teach) (test verb)   (test momentum)    (test swarm)            (payoff: Overdrive)
```

## 6. Swarm behavior this level requires

This layout **depends on** the swarm rework the Feel & Level pre-brief already scopes (items 4/5/6):

- **Chaff = a crowd, never lanes.** Today `SwarmSpawnSystem` floods 3 fixed lanes and `SwarmSteeringSystem`
  holds lane-Y — that must go (item 4). In the plazas, chaff needs **separation/flocking** so the crowd
  reads as a mob to wade through, not marching columns (item 5).
- **Ambient = life on authored walkway paths.** The background crowd (the ~100 ambient cap) should walk
  **paths that follow the strip's sidewalks and crossings** — the `SwarmDensityBlock` belt rect *evolving
  into level-authored paths consumed by ambient placement* (item 6). On a downtown strip this doubles as
  environment storytelling: pedestrians, marketgoers, commuters flowing past the fight.
- This **replaces** my earlier "per-wave belt-Y override" ask — the authored-path evolution is the better,
  already-planned home for per-zone swarm geometry.

## 7. Theme & environment — rain-slick downtown strip

| Zone | Set dressing | Parallax / lighting |
|---|---|---|
| Service Alley | brick, dumpsters, fire escape, one flickering sign | dark, tight; single key light |
| Storefront Row | shopfronts, awnings, neon window signs | mid-bright; sign glow on wet ground |
| The Scaffold | construction hoarding, plastic sheeting, work lamps | caged shadows, strobing work lights |
| Night Market | food stalls, string lights, steam, produce | warm pools of light, busiest parallax |
| Neon Crossing | intersection, traffic signals, giant billboard | brightest; rain sheets, big reflections |

- **Parallax** uses the existing `ParalaxScrolling` (background layers move by `ParallaxScale`). Downtown
  wants ≥3 layers: far skyline, mid buildings/signage, near street clutter.
- **Wet-ground reflections** and **rain** are the signature look — carried by art/shaders, independent of
  the belt logic. Note the project runs **Built-in RP** (not URP), so plan reflections/rain accordingly.
- **Real sprites required** — chaff proxies and the ambient material still use `HitEffect.png` placeholders
  (pre-brief item 3). Downtown NPC + thug art is a prerequisite for this level to *read*.
- `Surface` markers under each zone drive footstep SFX (wet-concrete variants would sell the rain).

## 8. Config mapping (buildable — first-pass)

Lands on the current `Level1` config path (replacing the single-wave placeholder in
`Assets/_neon/Level/Level1.asset`) plus scene geometry. `Level._levelLength ≈ 96`,
`PlayerSpawnProgression 0.0`, `PlayerSpawnDirection RIGHT`.

| Wave | Zone | `WaveTriggerType` | Trigger | Entries | `MaxActive` | Cooldown | `SpawnYRange` | `HasCameraBound` |
|---|---|---|---|---|---|---|---|---|
| A | 1 Service Alley | `ProgressionPercent` | 0.02 | 3× thug | 2 | 1.0 | {-1, 1} | no |
| B | 2 Storefront Row | `PreviousWaveCompleted` | — | 5× (chump-first) | 3 | 1.0 | {-2, 2} | 0.28 |
| C | 3 The Scaffold | `ProgressionPercent` | 0.30 | 6× thug | 3 | **0.5** (chain) | {-1, 1} | — |
| D | 4 Night Market | `ProgressionPercent` | 0.45 | crowd + 1 elite | 5 | 0.8 | {-3, 3} | — |
| E | 4 Night Market | `ProgressionPercent` | 0.58 | bigger crowd | 6 | 0.8 | {-3, 3} | 0.72 |
| F | 4 Night Market | `Manual` | (breather→push) | reinforcements | 4 | 0.8 | {-3, 3} | — |
| G | 5 Neon Crossing | `ProgressionPercent` | 0.80 | escalating crowd | 6 | 0.7 | {-3, 3} | — |
| H | 5 Neon Crossing | `PreviousWaveCompleted` | — | crowd + **mini-boss** | 6 | 0.7 | {-3, 3} | 1.00 |

**Swarm block** (`SwarmDensityBlock`): `EnableSwarm true`; ramp `ChaffCap` across the level (~20 in Alley →
~120 in Night Market → ~150 at Neon Crossing) once the crowd-behavior rework (§6) lands; `AmbientCap ~100`
on authored walkway paths. `SlowMotionOnLastKill true`; `EndLevelWhenAllWavesCompleted true`.

> Enemy `UnitDefinitionAsset`s (thug / chump / elite / mini-boss) referenced above need authoring; only a
> generic enemy exists on the placeholder today.

## 9. Feasibility & runtime-verify flags

- ✅ **Corridor/plaza widths, arena locks, density ramp, one-concept pacing** — all supported by existing
  tech (`CameraFollow` view area + per-wave camera bounds, `SwarmDensityBlock`, the four `WaveTriggerType`s).
- ⚠️ **Fixed zoom** — resolved via faked width (§3). Veto point: real dynamic zoom means dropping
  `PixelPerfectCamera` and re-tuning the art pipeline — a much larger effort.
- 🔧 **Swarm crowd behavior + ambient walkway paths** (§6) — the level's hard dependency; can't ship on
  today's lane chaff. Aligns with pre-brief items 4/5/6.
- 🔎 **Overdrive legibility** (Zone 5) — open hands-on gate; validate in a real run.
- 🔎 **Finish prompt readability** (Zone 2) — depends on `UIHUDFinishPrompt`, minimal today (item 2).
- ⚠️ **`AI_Active` spawn gap** — spawned enemies have a known activation caveat (`neon-troubleshooting`);
  confirm every wave's enemies actually engage when play-testing (Recipe 4), especially the elite/mini-boss.

## 10. Open questions / tune in-editor

1. Exact walkable widths and occluder framing per zone — needs eyes on the pixel-perfect frame at target
   resolution.
2. Enemy roster & HP tuning so Finish-Ready windows land where the pacing wants them
   (`FinishReadyHealthThreshold` from `EngagementSettings`).
3. Whether Zone 4's mid-plaza lull is a scripted `Manual` wave gap or just a density trough.
4. Mini-boss: bespoke unit, or an elite with inflated stats for v1?
5. Does the design's "Momentum → Overdrive" language map 1:1 to the shipped growth HUD (XP bar / Overcharge
   meter / level-up picker), or do names need reconciling?

## 11. Slotting / next steps

- This doc **unblocks** the Feel & Level pass (its own brainstorm → spec → plan), which is slotted **after
  the engine-base series (M3–M4)**. **Do not implement yet** — the swarm rework it depends on is part of
  that workflow.
- When it's time: implementation follows the established path (`Level` + `LevelConfigurationAsset` +
  `SpawnerService` + swarm block — `neon-recipes` Recipe 3), no legacy scene rebuilds.
- Immediate optional follow-ups: a dedicated **theme/mood pass** (lighting, rain, reflections, sign design),
  or drafting the concrete `Level1.asset` wave config from §8 for review.
