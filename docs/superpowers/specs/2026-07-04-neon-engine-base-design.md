# Neon Responder — Engine Base Design (MVP)

- **Date:** 2026-07-04
- **Status:** Approved (design), pending implementation plan
- **Author:** Sebastien Charron + Claude
- **Branch:** `claude/neon-engine-base`
- **Design ground-truth (GDD):** <https://app.notion.com/p/Neon_Responder_GDD-3935c0654b6b80beaa94f61de4bbf47d> (v0.4)
- **Implementation truth:** the `neon-*` project skills

## 1. Context & motivation

A base refactor has landed on `master`: the project now boots through a VContainer-DI + UnityHFSM
application FSM, with a service roster (`AudioService`, `InputService`, `ScenesService`, `EntitiesService`,
`SpawnerService`), a per-level DI scope (`Level : LifetimeScope`), and a full MonoBehaviour combat-verb FSM
(`UnitStateMachine` + `Player*`/`Enemy*`/`Unit*` states). That is the **plumbing**.

The GDD describes a game none of whose headline systems exist yet: the **engagement + growth loop** —
Auto-Engage, Finish-Ready, Momentum, the contextual finish, roguelite Protocols + economy, the Signal, and
the DOTS swarm density. This spec designs the **base of the engine** that sits between the landed plumbing
and those GDD systems: the shared runtime spine everything plugs into, plus the full MVP vertical slice
(GDD §0.4.g) built on it.

Core loop (GDD v0.4): **auto-engage → Finish-Ready → contextual Act verb → Momentum up → build fires
harder / earns faster → more Finish-Ready.**

## 2. Decisions locked (this brainstorm)

1. **Scope = full MVP vertical slice** (GDD §0.4.g). Designed as one coherent architecture (the systems
   interlock); the implementation plan phases it into build milestones.
2. **Swarm = hybrid.** Chaff + ambient run as DOTS/ECS; player + hero-tier enemies (elites/bruisers/boss)
   + all combat verbs stay MonoBehaviour; a thin bridge joins them.
3. **DOTS combat = greenfield.** No live ECS combat to coordinate with; the swarm + bridge are designed
   fresh against the engine-agnostic combat design.
4. **Finish model = tiered challenges.** Chaff finishes on a single verb (protects R7); tougher targets and
   higher Momentum tiers demand escalating input sequences ("QTE combos"), validated via the existing
   `comboData` engine. Difficulty scales the *challenge*, not the swarm's prompt count.
5. **Verification = EditMode unit tests (pure logic) + scripted play-test gates (runtime-is-ground-truth).**
6. **Density corrected:** ambient reduced from GDD's 200–500 to **~100 pure-vibe props**; hot chaff stays
   80–150. Total live agents ≈ 250.

## 3. Existing foundation (do NOT rebuild)

- **Engine/stack:** Unity 6, C#, 2D (Pixel Perfect), URP 17.3.0. No networking, single-player. VContainer DI,
  UniTask, R3, UnityHFSM (app FSM). **DOTS stack already installed:** `com.unity.entities` 1.4.4, `burst`
  1.8.27, `collections` 2.6.4, `mathematics` 1.3.3 — no new packages needed.
- **Boot:** `ApplicationFSM` runs `InitialState → PlatformState → UnityServicesState → GameServicesState →
  GameState` via `ApplicationLifetimeScope`; health-checked transitions; `EditorBootstrap` forces boot-first
  play-testing.
- **DI rule:** resolve via VContainer only (constructor injection / `[Inject]` fields); **no static `Services`
  accessor** (it was deleted). Register services in an FSM state, never in `ApplicationLifetimeScope`.
- **Entities/Spawner:** `EntitiesService` (`IEntitiesService` + `EntitiesQueries`) is a GameObject registry
  (integer IDs, type buckets), DOTS-shaped. `SpawnerService` does progression/wave spawning from
  `LevelConfigurationAsset` / `EnemyWaveDefinition` / `EnemySpawnEntry`, owned by `Level`.
- **Combat:** custom `UnitStateMachine` (MonoBehaviour, distinct from the app FSM). Verbs exist as states:
  `PlayerAttack` (punch/kick **combo engine** over `comboData`), grab (`PlayerTryGrab`/`PlayerGrabEnemy`/
  `PlayerThrowEnemy`), `PlayerWeaponAttack`, jump (`PlayerJump`/`PlayerJumpAttack`), ground finishers, plus
  shared `Unit*` states. `UnitActions` holds hit helpers (`CheckForHit`/`GetObjectsHit`/`HitBoxActive`) and
  DI accessors. `HealthSystem` (`SubstractHealth`/`AddHealth`, `healthPercentage`, static `onUnitDeath`).
  `EnemyBehaviour` gates AI via `AI_Active`.
- **Known gap to fix:** spawned MonoBehaviour enemies never get `AI_Active` enabled (the "AI_Active spawn
  gap", see `neon-troubleshooting`). Folded into Milestone 1.
- **Legacy (do not touch/extend):** `WaveManager` (`[Obsolete]`), scenes `04_Level2`/`05_Level3`. Only
  `03_Level1` uses the new `Level` + `SpawnerService` path.

## 4. Architecture overview

### 4.1 Three decoupled spine services (Layer 0)

Everything plugs into three shared services so systems never reference each other directly:

- **`IGameplaySignals`** — a typed event bus built on **R3** (already in the stack). One struct per event
  (`EnemyFinished`, `FinishReadyEntered`, `MomentumTierChanged`, `XpGained`, `ObjectiveProgress`,
  `SignalChanged`, …). Publishers don't know their subscribers. This is the decoupling backbone.
- **`IStatSystem` / `StatSheet`** — the load-bearing piece. Every "gets stronger / gets harder" number is a
  `Stat` = `baseValue` + a list of `StatModifier { Op (Add | PctAdd | Mult), value, sourceHandle }`, folded on
  query. `AddModifier(source)` / `RemoveBySource(handle)`. **Momentum tier, each Protocol, and the Signal are
  just modifier sources here** — they never touch each other. Two sheets to start: **player** and
  **run/global**. Pure C#, fully EditMode-testable.
- **`IGameplayClock`** — sole owner of gameplay tick + gameplay-time (scaled by hitstop / level-up slow-mo /
  pause). Systems register into an ordered tick so update order is deterministic (Auto-Engage → Finish-Ready
  eval → selector → Momentum decay). Replaces today's ad-hoc `Level.Update` ticking.

### 4.2 Layer stack (each layer depends only on the spine + layers below)

```
Layer 5  HUD / Signs / Feedback ── Momentum meter, verb-glyph prompt, "+N", objective bar, level-up picker,
                                    hitstop, audio-by-tier   (pure CONSUMER of Signals + Clock)
Layer 4  Run / Meta ─────────────── RunService (encounter FSM) · Objectives (Reboot Node) · Signal (dawn scalar)
Layer 3  Growth ─────────────────── Economy (XP/Charge/Overcharge) · Protocols (1-of-3, shop) · Specials
                                    (Siren Pulse) · Overcharge finisher
Layer 2  Engagement spine ───────── Auto-Engage · Finish-Ready + selector · Finish-resolution (wires EXISTING
                                    verbs) · IFinishChallenge (tiered) · Momentum
Layer 1  Swarm (DOTS) ───────────── ECS chaff/ambient sim + Swarm↔Mono bridge (targeting / resolution / render)
Layer 0  SPINE ──────────────────── IGameplaySignals · IStatSystem · IGameplayClock
─────────────────────────────────────────────────────────────────────────────────────────────────────────
EXISTING FOUNDATION (unchanged): VContainer DI · ApplicationFSM boot · EntitiesService · SpawnerService ·
                                 Level:LifetimeScope · MonoBehaviour combat verbs · HealthSystem · settings
```

### 4.3 Scopes & assemblies

- **New FSM state `GameplayServicesState`** (between `GameServicesState` and `GameState`) registers the
  *run-agnostic* engine services as singletons: spine, Momentum, Economy, Protocol registry, Signal. Honors
  "register in an FSM state, never in `ApplicationLifetimeScope`."
- **Per-run/per-encounter systems** (Auto-Engage, Finish-Ready selector, Objective, swarm bridge) register in
  the **`Level` scope** (a thin `Run` sub-scope is optional), so they tear down between encounters — the way
  `Level` already owns `SpawnerService`.
- **New assembly `BrainlessLabs.Neon.Simulation`** for the DOTS/ECS swarm (references `Unity.Entities`,
  `Burst`, `Collections`, `Mathematics`). Reference direction: **`Neon → Simulation`**; the sim references
  none of our assemblies (pure Burst-friendly leaf). Keeps the existing `Neon`/`Lifecycle`/`Editor` triangle
  intact (runtime never references Editor; `Neon` never references `Lifecycle`).
- **Gameplay meta-FSM (`RunService`) reuses UnityHFSM** but lives in `Neon`, distinct from the app-boot FSM
  in `Lifecycle`. Rationale: boot FSM = process lifecycle; run FSM = gameplay flow. Same library, clean
  assembly separation. (Rejected alternative: extending the app-boot FSM with gameplay states — it would drag
  gameplay into `Lifecycle`.)
- Combat is always described as **player-intent verbs** (engine-agnostic), never MonoBehaviour-vs-ECS.

## 5. System designs

### 5.1 Layer 2 — engagement spine

- **`AutoEngageSystem`** — a **new automated basic attack** (does NOT modify the manual verbs). Each rhythm
  interval (rate from `IStatSystem`) it finds the nearest *hot* enemy in the player's facing arc and applies
  **chip** damage (start 8, tuned to *push toward* Finish-Ready, not kill) via the existing `AttackData` /
  `HealthSystem.SubstractHealth` path — against hero-tier enemies directly and against DOTS chaff via the
  bridge. Upgradable via stats: rate, arc, damage, pierce, projectiles. This is the "setup"; the player's
  manual verbs are the "finish."
- **`FinishReadySystem`** — marks any hot enemy at **≤25% HP (or staggered)** as Finish-Ready. MonoBehaviour
  enemies: a `FinishReadyMarker` component reading `HealthSystem.healthPercentage` (+ glow/icon sign). DOTS
  chaff: a cheap `FinishReadyTag` set in a sim system.
- **`FinishReadySelector`** — picks the **single** highest-priority Finish-Ready target (nearest, then
  most-dangerous), enforcing the R7 one-prompt rule; drives the HUD verb-glyph prompt + verb targeting; emits
  the `+N ready` count.
- **`FinishResolver` + `IFinishChallenge`** — the seam that wires the **existing** verbs into the loop. At the
  verb hit-resolution point, decide: did this verb connect on a Finish-Ready target? If yes → resolve as a
  **finish** and publish `EnemyFinished` → **Momentum +1 step**. Momentum on **finishing hits only** (v0.4
  locked) — chip/knockdown of a merely-hot enemy grants none. `IFinishChallenge` is a per-target, tier-scaled
  challenge: a single verb-press for chaff, an escalating input sequence (2–3+ inputs, windows tightening per
  §9) for elites/bruisers/boss and high-Momentum targets, validated through the existing `comboData` engine.
  Momentum pays out on the *completed* challenge. **No standalone QTE prompt system** (v0.4 retired
  "Takedown"); the challenge is answered with real verbs.
  - *Combat seam (honest note):* this needs a small, **additive** hook into the existing hit path (likely
    inside `UnitActions.CheckForHit` / the verb states / `HealthSystem`). The verbs' behavior is unchanged —
    we only *observe* finishes and *tag* targets. Exact insertion point is confirmed against the real code at
    implementation; no APIs are invented.
- **`IMomentumSystem`** — tiers **Cool → Warm → Hot → Overdrive**; +1 step per finish (3 steps/tier); decay
  −1 tier per 2.5s idle. Registers **one multiplier modifier** into `IStatSystem` feeding *both* damage and
  gain (the "skill × stack" bridge; multipliers ×1.0 / ×1.3 / ×1.7 / ×2.5 per §9). Publishes tier changes.
  Pure C#, EditMode-tested.
- **Whiff cost** — missing a punch/kick/weapon swing → Momentum reset to Cool + 0.5s vulnerability stagger;
  **grab whiffs exempt** (v0.4). Hooks off the same combat seam.

### 5.2 Layer 1 — swarm (DOTS) + bridge

**ECS side (`BrainlessLabs.Neon.Simulation`):**
- Components: `SwarmAgent { tier: Chaff | Ambient }`, `BeltPosition { x, laneIndex }`, `Velocity`, `Health`
  (chaff only), `HotFlag`, `FinishReadyTag`, `EngageIntent`.
- Systems (Burst where possible): `SwarmSpawnSystem` (floods from both belt ends, rate/composition scaled by
  the Signal, density caps enforced), `SwarmSteeringSystem` (funnel-toward-player/objective along 3–5 Z-lanes;
  seek+separation for chaff, cheap wander for ambient), `SwarmChipSystem` (applies auto-engage chip),
  `FinishReadyEvalSystem`, `SwarmDeathSystem` (despawn + emit finish event via bridge). The sim owns the
  **truth**; rendering is a projection.

**Bridge (`SwarmBridge`, in `Neon`, references `Simulation`):** the single seam, four jobs —
1. **Targeting** — `NearestFinishReadyInArc(pos, facing, arc)`, `HotCountInArc(...)` spanning both DOTS chaff
   and Mono hero-tier, returning a unified `TargetRef` (ECS entity **or** `TrackedEntity`/GameObject). The
   engagement spine is world-agnostic.
2. **Resolution** — auto-engage chip and finishing verbs write damage/finish commands into the ECS World via
   `EntityCommandBuffer`; `QueryEntitiesInBox(bounds)` lets a verb hitbox hit swarm agents (they aren't
   physics colliders).
3. **Render sync (R6)** — hot chaff (≤150) sync to a **pooled `SpriteRenderer` proxy set** (need per-agent
   animation + Finish-Ready glow + to read as hittable); ambient (~100) drawn cheaply via
   `Graphics.RenderMeshInstanced` / BatchRendererGroup (vibe only).
4. **Spawn ownership split** — `SpawnerService` keeps spawning **hero-tier MonoBehaviour** units from
   `LevelConfigurationAsset` waves; the DOTS `SwarmSpawnSystem` owns chaff/ambient. `LevelConfigurationAsset`
   gains a companion **swarm-density block** (per-encounter caps/composition) — a data extension, not a
   rewrite.

**`EntitiesService` relationship:** it **stays the MonoBehaviour hero-tier registry** (player, elites,
bruisers, boss). DOTS chaff/ambient do **not** register there (that would defeat the density win). The bridge
is the unifying query layer; `EntitiesQueries` stay hero-tier.

**Highest-risk unknown — 2D sprite rendering of ECS agents.** Unity has no turnkey 2D-sprite ECS renderer
(Entities Graphics is 3D-mesh oriented). The proxy-pool-for-hot + instanced-for-ambient plan is the
recommendation, but the exact ambient instanced path is a **1–2 day spike in Milestone 1**, done in isolation
before anything is built on it. **Fallback if the spike fails:** hot chaff as pooled MonoBehaviour (the
non-DOTS swarm option) — because the engagement spine talks to the *bridge*, this fallback changes only the
swarm internals; nothing above Layer 1 moves.

### 5.3 Layer 3 — growth (roguelite stack)

- **`IEconomySystem`** — three ledgers at three timescales (GDD §8): **XP** (in-encounter → level-ups),
  **Neon Charge** (between-encounter → shop), **Overcharge** (moment-to-moment → finisher). Every *gain* reads
  the current Momentum gain-multiplier from `IStatSystem` — the economy never knows what Momentum is. Pure C#,
  EditMode-tested.
- **`ProtocolDefinitionAsset`** (ScriptableObject, menu `Neon/Protocols/...`) — a data-driven upgrade = a
  bundle of **stat modifiers** + optional tagged hooks (e.g. "Siren Pulse also advances objective ~15%").
  Applying a Protocol calls `IStatSystem.AddModifier(...)` with the Protocol as source handle; stacking within
  a family *is* the snowball, and Momentum multiplies it for free. Families: Auto-gear, Finish, Momentum,
  Specials, Defense, Objective, **Brawler**, **Scavenger** (§8 + §0.4.f).
- **`IProgressionSystem`** — watches XP thresholds → fires a **level-up** (mid-fight slow-mo via
  `IGameplayClock`) → **pick 1 of 3** Protocols; reroll/shop (Neon Charge) between encounters. MVP ships
  **6–8 Protocols incl. ≥1 Brawler + ≥1 Momentum** (§0.4.g).
- **`ISpecialSystem` + Overcharge** — cooldown + Charge-cost actives. MVP = **1 Special (Siren Pulse** —
  radial stun/knockback/reveal that **mass-triggers Finish-Ready**) + the **Overcharge finisher** (meter-gated
  screen-clear that refreshes the field with mass Finish-Ready; tuned to clear chaff, not bosses — R4). Both
  are "manufacture a wave of finish-ready targets" buttons feeding the loop.

Architectural payoff: **Protocols, Momentum, and the Signal are all just modifier sources on `IStatSystem`.**
Adding a Protocol is authoring a ScriptableObject, not writing system code.

### 5.4 Layer 4 — run / objectives / the Signal

- **`RunService` (UnityHFSM gameplay FSM)** — sequences the run: `EncounterIntro → EncounterActive →
  EncounterComplete → Shop → …(3–5×)… → Boss → RunWon / RunLost`, level-up beats interleaved. Win = objective
  complete; lose = player death. **MVP run = one belt arena scene** hosting a *sequence of encounter phases*
  (not multiple scene loads); `RunService` lives in the `Level` scope and per-phase drives hero-tier waves
  (`SpawnerService`), chaff floods (`SwarmSpawnSystem`), and objective activation. Target 10–15 min, 1 boss.
  Ships with a **Boss state that is a stub in MVP** — the run can win on reaching dawn if the boss is cut.
- **`IObjective` + `RebootNodeObjective`** — the win verb. Reboot Node = hold a zone under fire until the bar
  fills (45–60s, §9). Publishes `ObjectiveProgress`; completion advances the Run FSM. Objective speed is a
  stat (Objective Protocol family + "Priority Override" hook modify it). Rescue / Purge-Jammer / Hold-the-Line
  are later `IObjective` impls — the abstraction is MVP, the extra impls aren't.
- **`ISignalSystem` (dawn meta)** — a global 0→dawn scalar. Objectives raise it; failing lowers it. It's
  **another modifier source on the run/global `StatSheet`**, scaling spawn nastiness (read by
  `SwarmSpawnSystem`), map darkness (render), and music aggression (audio). **Win = the Signal hits dawn.**
  Turns "stabilize the city" into a system (§10/§12). Pure C# + events, EditMode-tested for the curve.

### 5.5 Layer 5 — HUD / signs / feedback

**Throughline: this layer is a pure *consumer* of `IGameplaySignals` + `IGameplayClock`** — it reads events
and gameplay-time; it never reaches into systems. Keeps systems headless (unit-testable) and signs swappable.

- **HUD** (extends existing `UIManager` / `UIHUDHealthBar`): Momentum tier meter (heat bands + about-to-decay
  pulse), Overcharge meter, objective bar + giant arrow + zone glow, the **single verb-glyph priority prompt**
  (fist / boot / grab-hand / weapon / throw-arc / jump glyphs, context-swapping — the biggest new
  sign-authoring cost, §0.4.f) + **"+N ready"** counter, **held-state indicator** ("grabbing X" / "holding
  \[weapon]"), and the **level-up 1-of-3 picker**.
- **Signs (before):** global hot-vs-ambient render rule (outline+bloom+saturation vs muted — a material/shader
  convention on the sprite proxies), Finish-Ready glow, jump-attack landing reticle, weapon-durability crack
  tell, distinct grabbed-enemy visual, spawn tells, Signal-driven map darkness.
- **Feedback (after):** hitstop + shake (reuse `CameraShake`) with **per-verb profiles** (throw-enemy = the
  biggest hit in the kit, §0.4.f), tier-up flourish, whiff record-scratch + desaturate, weapon-break snap,
  "NODE RESTORED" callouts, XP/Charge popups, finisher freeze-frame, **audio layering by Momentum tier +
  Signal** (via `AudioService`). All hitstop/slow-mo routes through `IGameplayClock`.

## 6. Density budget (corrected)

| Category | Cap | Representation | Update |
|---|---|---|---|
| Ambient (pure-vibe props) | ~100 | instanced/pooled decorative sprites (DOTS-or-not is a spike call) | low |
| Hot chaff (Glowpunks) | 80–150 | DOTS sim + pooled `SpriteRenderer` proxies | every frame |
| Finish-Ready shown as prompt | **1** (+N counter) | HUD | — |
| Skirmishers / Drones / Bruisers | 8–12 / 2–4 / 2–3 | MonoBehaviour hero-tier (post-MVP) | every frame |
| Elites | 1–2 | MonoBehaviour hero-tier (post-MVP) | every frame |

Total live agents ≈ **250** (was ~650).

## 7. Build milestones (the implementation plan will phase these)

- **M0 — Spine (headless, test-first).** `BrainlessLabs.Neon.Simulation` asmdef (empty ECS bootstrap);
  `IStatSystem`/`StatSheet`/`StatModifier`, `IGameplaySignals` (R3), `IGameplayClock`; `GameplayServicesState`.
  *Gate:* compiles, boots clean, EditMode tests green for stat folding/modifier stacking.
- **M1 — R1 prototype (the core bet; GDD's recommended first target).** *Spike first:* 2D-ECS rendering
  (hot-chaff proxy pool at 150 + ~100 instanced ambient) — gates the rest. Then DOTS swarm + `SwarmBridge`;
  `AutoEngageSystem` + `FinishReadySystem` + selector + `Momentum` + `FinishResolver` wiring the existing
  verbs (single-verb chaff finish) + whiff cost; fix the **AI_Active spawn gap**; minimal HUD (Momentum meter
  + Finish-Ready glow + verb prompt). *Gate (both core bets):* **R1** 150 hot + 100 ambient holds target FPS;
  **R2** hands feel busy, not idle.
- **M2 — Growth.** `IEconomySystem` (Momentum-multiplied XP/Charge/Overcharge), `ProtocolDefinitionAsset` +
  `IProgressionSystem` (level-up 1-of-3 slow-mo picker), 6–8 Protocols (≥1 Brawler, ≥1 Momentum), the tiered
  `IFinishChallenge` (elite/high-Momentum escalating sequences via `comboData`). *Gate:* build snowballs,
  Overdrive "screams" (§16); EditMode tests for economy + protocol stacking.
- **M3 — Run / objective / Signal.** `RunService` (UnityHFSM) sequencing encounter phases; `RebootNodeObjective`;
  `ISignalSystem` feeding spawn/darkness/music; shop beat. *Gate:* a full run lands 10–15 min and ends on the
  dawn beat; objective legible in chaos (§16); EditMode tests for run transitions + Signal curve.
- **M4 — Actives + finishers + feel.** Siren Pulse, Overcharge finisher, per-verb hitstop/shake profiles,
  tier-up flourish, whiff scratch, callouts, audio layering, full HUD polish. *Gate:* the full §16 checklist.
- **Post-MVP (priority order, §0.4.g/§13):** weapon-throw → jump-attack → elite execution + Sirencatcher →
  Jammer/Bruiser → Boss (Blackout Idol) → Rescue objective → 2nd Special → biome swaps.

## 8. Testing strategy

- **EditMode unit tests** (`com.unity.test-framework`, installed) for every pure-logic system: stat folding,
  Momentum tiers/decay, economy math, Signal curve, run-FSM transitions, finish-challenge validation.
  Test-first where cheap.
- **Scripted play-test gates** per milestone via the boot flow (`neon-recipes` Recipe 4) — runtime-is-
  ground-truth checks (density FPS, not-idle feel, legibility). Front-loaded. The **GDD §16 checklist is the
  acceptance set.**

## 9. Risk register

- **R1 density (highest)** — mitigated by *spike-first* in M1 + reduced ~100 ambient.
- **2D-ECS render unknown** — gates M1; fallback = hot chaff as pooled MonoBehaviour (only swarm internals
  change, because the spine talks to the bridge).
- **R2 idle feel** — the M1 gate *is* this test.
- **R7 prompt spam** — single selector + tiered challenge preserves it.
- **R9 state-swap legibility** — held-state HUD + verb glyphs; verify in an early play-test.
- **Combat seam** — the observe-and-tag hook stays minimal and additive; exact insertion confirmed against
  real code at implementation.
- **Scope (R5)** — milestone gating means M1 alone proves the fusion; M2–M4 are independently shippable.

## 10. Non-negotiable constraints honored

- Must DI-bootstrap to run; new services registered in FSM states / `Level` scope, never in
  `ApplicationLifetimeScope`.
- Resolve via VContainer DI only; no static/`Instance` accessors reintroduced.
- Combat design stays engine-agnostic (player-intent verbs).
- Single-player; no networking.
- No `WaveManager`; no rebuilding `04_Level2`/`05_Level3`. New content uses `Level` +
  `LevelConfigurationAsset` + `SpawnerService` (+ the new swarm block).
- Runtime is ground truth — every milestone has a play-test gate.
- Never invent APIs — the combat seam and all existing-type references are confirmed against code before use.
- Assembly direction preserved: `Lifecycle → Neon`, `Editor → Neon`, `Neon → Simulation`; runtime never
  references Editor; `Neon` never references `Lifecycle`; `Simulation` references none of ours.

## 11. Deferred / open questions (resolve in playtest or later specs)

- **Grab i-frames (GDD Open Q7 / R10):** zero i-frames (the deliberate cost) vs. partial armor — revisit once
  Skirmishers/Bruisers are live.
- **Kick → grab lockout (Open Q6):** whether a kick-knockdown locks out grabbing the same target — guard
  against re-introducing the cut combo tree.
- **Jump-attack vs Siren Pulse overlap (Open Q5):** both mass-trigger Finish-Ready — differentiate or cut
  jump-attack; decided if/when jump-attack promotes from nice-to-have.
- **Multi-scene runs** (vs the MVP single-arena-phases model) — a later structural concern.
- **Ambient in-ECS vs decorative pool** — a Milestone-1 spike-time call, not an architecture commitment.
- **Beat-synced QTEs** — parked (GDD §14); keep the finish-challenge timing beat-independent for now.
