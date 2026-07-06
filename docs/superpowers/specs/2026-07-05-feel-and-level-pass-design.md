# Feel & Level Pass — Design Spec

**Type:** Design spec (the authority for *how this pass is built*). Produced by the `superpowers:brainstorming`
workflow against the handoff + level design.
**Status:** DESIGN — approved to plan. Next step: `superpowers:writing-plans` (expected to emit the six plans below).
**Date:** 2026-07-05 · **Author:** Sebastien Charron + Claude
**Sits after:** M0–M4 (engine-base series), all merged to `master` (tip `621f361`, 138/138 EditMode tests).

**Reads with:**
- `docs/superpowers/plans/2026-07-05-feel-and-level-pass-handoff.md` — the orientation brief this spec answers.
- `docs/levels/level-01-downtown-strip.md` — the level design (authority for *what the level is*). **This spec
  supersedes that doc's camera fork — see §2 and §11.**
- `docs/rgd/protocol-stack-v0.1.md` — protocol family taxonomy (used by the finisher UI).
- `docs/rgd/avatar-v0.1.md` — player art bible (Kaito Mori / NR-0047).

---

## 1. Goal

Turn the proven engine-base *function* (M0–M4) into *form*: Level 01 "First Response" plays as designed, the
swarm reads as a crowd, real character art replaces placeholders, and the feedback/HUD is legible and juicy — all
on a camera that is **pulled back far enough to read**. The experience-goal sentence (from the level doc, locked):

> auto-engage → **Finish-Ready** → contextual **Act verb** → **Momentum** builds → the build fires harder / earns
> faster → ending on the player's **first Overdrive high**.

**Design principle that emerged in brainstorm (applies throughout the UI):** *every loud word on screen maps to a
real system; nothing repeats for free.*

---

## 2. Locked decisions

| # | Decision | Rationale |
|---|---|---|
| 1 | **One unified spec → staged plans** | Cross-cutting forks (camera scale, render, crowd) resolved once; `writing-plans` carves implementation. |
| 2 | **Camera: replace `PixelPerfectCamera` with a Cinemachine orthographic rig**, zoomed-out baseline | Non-negotiable: avatar + enemies are too big; the player must see more of the world. This is foundational — **it supersedes the level doc §2/§3/§9 "fixed zoom / faked width" fork.** |
| 3 | **Render: URP with the 2D Renderer** — PPv2/BiRP was a wrong turn, corrected by **Plan B.b** | End-state wants 2D lights, wet-ground/rain render features, and an SRP for the ECS-graphics track. Plan B built bloom/grade/vignette + whiff-desaturate on PPv2/built-in RP (executed), then **Plan B.b migrates to URP 2D** and ports post to URP Volumes at parity (the whiff *architecture* ports ~1:1; the PPv2 *stack* is removed). |
| 4 | **Chaff crowd = seek + separation + light cohesion** (no alignment) | Reads as a converging mob, not marching columns; alignment is what reintroduces column reads. |
| 5 | **Ambient = scene-authored polyline paths + gizmos**, baked into the sim | Matches the level's other spatial data (all scene-authored); no new package; path-follow stays unit-testable. |
| 6 | **Art = characters real (AI-gen per bible); environment functional-blockout** | PPv2 carries mood; environment art beautified in a later pass. |
| 7 | **Zone 5 anchor = elite + inflated stats** (v1) | Ships the Overdrive payoff without a bespoke unit or new behavior/activation risk. |
| 8 | **Finish prompt = square draining frame + verb glyph + square pips + docked `+N` → cinematic banner finisher** | See §8. Verb-headline + protocol-family flair kills the "always FINISH" repetition. |

---

## 3. Plan decomposition

```
[ 0. Camera & scale  ∥  A. Swarm rework  ∥  B. Render foundation ] → B.b. URP 2D migration → C. Level 01 build → [ D. Character art ∥ E. UI / feedback ]
```

Plans 0, A, B are mutually independent (parallelizable). C depends on all three. **A.a layers on A** (the hero-follower
crowd — a design addition made after A was written; kept as a separate plan at the author's request). D and E follow C;
E also touches Plan 0 (Impulse). `writing-plans` owns final task breakdown and may split further.

| Plan | One-line scope | Depends on |
|---|---|---|
| **0 · Camera & scale** | Cinemachine ortho rig, zoomed-out baseline, per-zone vcams, `Confiner2D`, Impulse shake | — (first) |
| **A · Swarm rework** | Kill lanes; seek+separation+cohesion; ambient walkway paths; per-zone density curve | — |
| **A.a · Hero-follower crowd** | Layers on A: all chaff become hero-squad followers (per-hero cap; orphan→player on hero death; re-adopt into freed slots); `SwarmBridge` pushes hero positions/caps into the sim | A |
| **B · Render foundation** | PPv2 install + volume/profile; whiff = grade; perf gate (executed on BiRP, then superseded) | — |
| **B.b · URP 2D migration** | BiRP→URP 2D Renderer; port post to URP Volumes; remove PPv2; lighting foundation. Corrects Plan B's pipeline; **must precede Plan C** | B |
| **C · Level 01 build** | Waves A–H; scene geometry; enemy roster; Zone 4 lull; `AI_Active` verify | 0, A, B |
| **D · Character art** | AI-gen chaff/ambient/roster/player, scaled to the new baseline | 0, A, C |
| **E · UI / feedback** | Finish prompt; HUD restyle; naming reconciliation; `CameraShake`→Impulse | B (touches 0) |

---

## 4. Plan 0 — Camera & scale *(foundational, first)*

**Problem (ground truth from `Assets/_neon/Scripts/Camera/CameraFollow.cs`):** the visible world height is
`refResolutionY / assetsPPU` on a `PixelPerfectCamera`. Zoom *is* controllable (via reference resolution / PPU) but
`PixelPerfectCamera` cannot smoothly *animate* zoom at runtime — which the per-zone design wants.

**Decision:** drop `PixelPerfectCamera`; adopt a **Cinemachine 3.x** rig (`com.unity.cinemachine`, Unity 6.3.5 →
`CinemachineCamera` + `CinemachineBrain`) on an orthographic main camera.

**Scope:**
- **Zoomed-out baseline** — a base vcam lens orthographic size tuned so the avatar + enemies read small enough to see
  the field. Exact size tuned in-editor at target resolution.
- **Per-zone zoom (real)** — a vcam per zone (or per corridor/plaza archetype) with its own lens size; zone trigger
  volumes raise the active vcam's priority and the Brain **blends** the zoom. Corridors zoom in, plazas zoom out —
  genuinely showing more, not occluder-faked.
- **Bounds** — `CinemachineConfiner2D` replaces the hand-written `Left/Right/Top/Bottom` + `levelBound` clamps in
  `CameraFollow`. The per-wave arena locks (`EnemyWaveDefinition.HasCameraBound` / `CameraBoundProgression`) map onto
  confiner-shape swaps or vcam priority changes triggered per wave.
- **Follow** — single-player (one `Player` tag), so the legacy multi-target `targets[]` centering collapses to a plain
  Follow; no `TargetGroup` needed.
- **Shake** — migrate `CameraShake.cs` to `CinemachineImpulseSource` / `Listener`; rewire M4 `FeedbackSystem` shake to
  raise impulses (see Plan E).
- **Crispness** — we accept managing sprite crispness ourselves (point filtering + sane PPU) instead of the
  pixel-perfect guarantee. Lower-stakes because PPv2 bloom/grade already pulls the look off strict pixel-purism.

**Ripples:** occluders in Level 01 become **set-dressing / mood only** (the vcams do the width); character-art scale
(Plan D) is authored against the new baseline; the level doc's faked-width framing is superseded.

**Verify:** in-editor at target resolution — avatar/enemy on-screen size reads right; per-zone blends are smooth;
confiner holds the arena; shake still lands on hits.

---

## 5. Plan A — Swarm rework *(Layer-1 internal; `ISwarmBridge` and everything above it untouched)*

Current state (ground truth):
- `SwarmSpawnSystem` (`Assets/_neon/Scripts/Simulation/SwarmSpawnSystem.cs`) floods chaff from belt ends into
  `LANE_COUNT = 3` fixed lanes (per-chaff `LaneIndex`); seeds `AmbientCap` scattered agents once.
- `SwarmSteeringSystem` (`.../SwarmSteeringSystem.cs`) seeks the player, holds lane-Y with deterministic jitter
  `(entity.Index % 7 - 3) * 0.15f`, stops at `STOP_RADIUS = 0.9f`, velocity-lerps at `STEER_LERP = 0.08f`. Ambient
  bounce-wanders off belt bounds. **No separation/flocking exists.**
- `SwarmRenderRig` (`Assets/_neon/Scripts/Swarm/SwarmRenderRig.cs`): chaff = pooled `SpriteRenderer` proxies with
  `_chaffSprite`, tinted `_hotColor (1,0.35,0.65)` / `_finishReadyColor (1,0.85,0.2)`; ambient =
  `Graphics.DrawMeshInstanced` with `_ambientMaterial` (`Neon/InstancedUnlit` shader, `_ambientSize 0.8`, Z=1).
- `SwarmDensityBlock` (nested on `LevelConfigurationAsset`): `EnableSwarm`, `ChaffCap`, `AmbientCap`,
  `ChaffSpawnRatePerSecond`, `ChaffMoveSpeed`, `BeltYMin`, `BeltYMax` → assembled by `SwarmConfig.From(config, level)`.

**Changes:**
1. **Kill lanes** — remove `LANE_COUNT` / `LaneIndex` from spawn; remove lane-Y hold + the `entity.Index` jitter from
   steering.
2. **Crowd behavior** — chaff steering = seek-player **+ separation + light cohesion** (no alignment). Neighbor
   lookups via a uniform-grid **spatial hash** so it stays cheap at ChaffCap 150. Keep arrival behavior around the
   player (a `STOP_RADIUS`-style ring).
3. **Ambient walkway paths** — new `WalkwayPath` authoring MonoBehaviour (scene gizmos, editable waypoints) → baked at
   level start into a runtime point-list the sim consumes. Ambient agents advance along an assigned path with a
   per-agent lateral offset + speed, replacing bounce-wander. The `SwarmDensityBlock` belt rect is retained for chaff
   spawn bounds; ambient placement moves onto paths.
4. **Per-zone density** — add a progression-driven `AnimationCurve` to `SwarmDensityBlock` for `ChaffCap` (and
   `AmbientCap`) over level progression 0→1 (target ~20 → ~120 → ~150), so the §5 "rising sawtooth" is data-authored
   with no per-wave plumbing. `SwarmConfig.From` samples the curve at current progression.
5. **Sprite-swap plumbing** — expose `_chaffSprite` + `_ambientMaterial` cleanly for Plan D to assign real art.

**Tests (EditMode):** separation keeps a minimum inter-agent spacing under density; agents converge on the player;
path-follow advances an agent along a constructed path; density curve returns expected caps at sampled progressions.

### 5a. Chaff-as-followers — hero squads (Plan A.a, layered on A)

A design addition made after Plan A was written (kept as a separate plan at the author's request). It **re-targets**
Plan A's crowd: instead of all chaff seeking the player, **every chaff is a follower of a hero enemy** (the
MonoBehaviour roster units — thug/elite/mini-boss). Model:
- **Per-hero follower cap** — `UnitDefinitionAsset.MaxFollowers` (new field), optional per-wave override. Assignment =
  **nearest hero with a free slot** (for fresh spawns and orphan re-adoption alike).
- **Replacement** — a hero refills a freed slot while the wave budget / `ChaffCap` ceiling allows, **adopting an
  existing orphan before spawning a fresh chaff**.
- **Orphans** — a hero's death does **not** despawn its followers; they become orphans that **seek the player** until
  re-adopted into any hero's freed slot.
- **Density** — `ChaffCap` (Plan A's progression curve) becomes the **ceiling**; actual population is hero-demand-driven
  (filled slots + orphans ≤ ceiling), not a flat flood.
- **Bridge** — `SwarmBridge.Tick` pushes a hero table (id = `TrackedEntity.Id`, position, cap from `MaxFollowers`) into
  the sim each tick, exactly as it already pushes `PlayerPosition`. A hero leaving `IEntitiesService` drops its id →
  its followers auto-orphan. The `ISwarmBridge` interface + all consumers stay untouched.
- **Reuse** — Plan A's `SwarmSteering` math is unchanged; only the *target* differs (assigned-hero position, or the
  player when orphaned).
- **Verification** — the chaff-side ships with placeholder heroes for a smoke test; Plan C's real roster does the full
  follower run.

---

## 6. Plan B — Render foundation *(PPv2 — executed, then superseded by Plan B.b)*

> **Pipeline correction:** Plan B was built on PPv2 + built-in RP, which was the wrong foundation for a 2D
> neon game (no 2D lights, no render features, dead-end stack). **Plan B.b** (`…-plan-bb-urp.md`) migrates to
> URP 2D and ports the post below to the URP Volume system at parity — the whiff architecture (`WhiffFx` curve +
> tests, `WhiffPostFx` weight-pulse, `FeedbackSystem` seam) ports ~1:1; the PPv2 package/profiles/components are
> removed. The bloom/grade/vignette/whiff *intent* below stands; only the stack changes.


- Install `com.unity.postprocessing`. Add a `PostProcessLayer` to the (Cinemachine) main camera + a global
  `PostProcessVolume` / profile: **bloom** (neon signage + Finish-Ready glow), **color grading**, **vignette**.
- **Whiff** = animate the grade's saturation toward grey on the existing whiff signal (replaces M4's red uGUI flash;
  the record-scratch SFX stays). The HUD is Screen-Space-Overlay and composites *after* post — bloom will **not** blow
  out HUD glyphs or the finish prompt; only world-space feedback is affected.
- **Perf gate:** budget-check the stack against the 150-chaff scene (spike headroom ~197 FPS). Done = no material
  regression at the density budget.

---

## 7. Plan C — Level 01 build

Authored against `docs/levels/level-01-downtown-strip.md` §4–§8. Lands on `Assets/_neon/Level/Level1.asset` +
`03_Level1` scene geometry. `Level._levelLength` 50 → **96**; `PlayerSpawnProgression 0.0`; `PlayerSpawnDirection RIGHT`.

**Framing (post-camera-rework):** per-zone feel is delivered by **Cinemachine vcams** (corridor vcams = tighter lens,
plaza vcams = wider), triggered by zone volumes. **Occluders are set-dressing / mood, not the width mechanism.**
Walkable-band colliders still define the playable space per zone. Per-wave arena locks map to confiner/vcam swaps.

**Waves (first-pass, from level-doc §8, using real `EnemyWaveDefinition` fields):**

| Wave | Zone | `TriggerType` | Trigger | Entries | `MaxActiveEnemies` | `CooldownBetweenSpawns` | `SpawnYRange` | Camera |
|---|---|---|---|---|---|---|---|---|
| A | 1 Service Alley | `ProgressionPercent` | 0.02 | 3× thug | 2 | 1.0 | {-1,1} | free |
| B | 2 Storefront Row | `PreviousWaveCompleted` | — | 5× (chump-first) | 3 | 1.0 | {-2,2} | lock 0.28 |
| C | 3 The Scaffold | `ProgressionPercent` | 0.30 | 6× thug | 3 | **0.5** (chain) | {-1,1} | — |
| D | 4 Night Market | `ProgressionPercent` | 0.45 | crowd + 1 elite | 5 | 0.8 | {-3,3} | — |
| E | 4 Night Market | `ProgressionPercent` | 0.58 | bigger crowd | 6 | 0.8 | {-3,3} | lock 0.72 |
| F | 4 Night Market | `Manual` | breather→push | reinforcements | 4 | 0.8 | {-3,3} | — |
| G | 5 Neon Crossing | `ProgressionPercent` | 0.80 | escalating crowd | 6 | 0.7 | {-3,3} | — |
| H | 5 Neon Crossing | `PreviousWaveCompleted` | — | crowd + **mini-boss** | 6 | 0.7 | {-3,3} | lock 1.00 |

(`TriggerType.ProgressionPercent` uses `TriggerProgressionPercent`; camera locks use `HasCameraBound` +
`CameraBoundProgression`, reconciled to Cinemachine per Plan 0.)

**Zone 4 lull:** the breather is a scripted **`Manual`-trigger gap** (wave F), not just a density trough — authored
control reads more deliberately.

**Enemy roster:** author 4 `UnitDefinitionAsset`s (only `UnitDefinition_NmeOne` + the player exist today) —
**thug / chump / elite / mini-boss**. Mini-boss = the elite def with inflated `_maxHealth` + scale. Each roster def
also sets **`MaxFollowers`** (the hero-follower cap, Plan A.a); Plan C runs the full follower-squad verification
(chaff group around heroes; orphans seek the player on hero death and re-adopt into freed slots). First-pass
`_maxHealth` set relative to `FinishReadyHealthThreshold` (`EngagementSettings`) so Finish-Ready windows land where
the pacing wants; **tuned in-editor** (runtime is ground truth).

**`AI_Active`:** confirm every wave's enemies engage on spawn in play-test (Recipe 4), especially elite/mini-boss
(known activation caveat — `neon-troubleshooting`).

**Density block:** `EnableSwarm true`; ChaffCap curve ~20→120→150 (Plan A); `AmbientCap ~100` on authored paths.
Plus the level-end / last-kill flags the level doc §8 names (`SlowMotionOnLastKill`, `EndLevelWhenAllWavesCompleted`)
— **confirm their exact field names + owning config at build time** (not verified in recon).

---

## 8. Plan E — UI / feedback (finish prompt is the centrepiece)

### 8.1 Finish prompt (rework `UIHUDFinishPrompt`)
Today `UIHUDFinishPrompt` (`Assets/_neon/Scripts/UI/UIHUDFinishPrompt.cs`) is text-only: `verbLabel` + `countLabel`,
following the target on a Screen-Space-Overlay canvas, driven by `FinishReadyPromptChanged` +
`FinishChallengeChanged`. New design:

**Base state (finish-ready / window live):**
- A **square draining frame** whose border depletes as the finish window closes (the timer).
- The **verb glyph** (which Act verb to press) centered.
- **Square pips** below the glyph = the verb sequence (filled = done, outline = remaining).
- **`+N`** docked *inside* the frame's top-right corner (colored chip) = other enemies also finish-ready.
- When finish-ready but the timed window hasn't started, the frame sits idle/full — the ring never implies pressure
  that isn't there.

**Finisher (sequence complete):** resolves into a **cinematic chevron banner**:
- **Headline = the Act verb that landed it** (`PUNCH` / `KICK` / `GRAB` / `THROW` / `JUMP` / `WEAPON`) — dynamic per
  finish, so it never reads a wall of "FINISH".
- **Flair line = protocol family codename** (see §8.3).
- **Tier** carried by **colour escalation** (amber → pink → gold) plus **punctuation callouts** that take the headline
  slot only at the escalation beats.

**Loud-word rule (each maps to a real, distinct system):**

| Word | System | Built? |
|---|---|---|
| the verb (`KICK`…) | combat Act verb | yes |
| `HOT`, `OVERDRIVE` | Momentum tiers | yes |
| `OVERCHARGE` | Overcharge finisher (M4 `OverchargeFinisher`) | yes |
| `SIREN PULSE` | Special (M4 `SpecialSystem`) | yes |

Verb slams fire on every finish; the tier/system callouts fire as their own punctuation beat (tier-up transition /
finisher / special) so the loud words stay rare and keep impact.

### 8.2 HUD restyle
Restyle the Momentum / XP / Overcharge / Special meters into the same square/neon tier language as the finish prompt
(shared colours, flat-neon treatment). Cosmetic — the functional M4 versions stay.

### 8.3 Protocol-family flair *(forward-compatible, not wired now)*
The finisher flair line shows the **protocol family** driving the finish (family codenames, `protocol-stack-v0.1.md`
§1: Auto-Gear *The Grinder*, Momentum *The Redline*, Execution *The Last Call*, Brawler *The Hands*, Scavenger *The
Salvage*, Specials *The Deployables*, Defense *The Night Watch*, Objective *The Dispatch*).

**Ground-truth constraint:** Protocols are `[GDD-TARGET · not built]` (CLAUDE.md rule 4 — do not wire against them).
So this pass ships the slot with a **static default string `THE LAST CALL`** (Execution — the finisher's home family;
not wired to anything). When Protocols land, the slot becomes build-reactive (a Momentum build reads *The Redline*,
etc.). No fake system in between — just a designed slot with a sensible constant.

### 8.4 Naming reconciliation
Keep four **distinct** systems, no renames:
- **Momentum** = the tier ladder (Cool → Warm → Hot → **Overdrive**); source of the `HOT` / `OVERDRIVE` slam words.
- **Overcharge** = the meter-gated screen-clear finisher (M4 `OverchargeFinisher`); slam word `OVERCHARGE`.
- **Special** = Siren Pulse etc. (M4 `SpecialSystem`); slam word `SIREN PULSE`.
- **XP** = level/growth (level-up picker; Protocols later).

The spec-implementer **verifies the shipped field/type names** and aligns player-facing text to this vocabulary.

### 8.5 Feedback (camera migration)
Migrate `CameraShake` → `CinemachineImpulseSource`; rewire M4 `FeedbackSystem` per-verb hitstop/shake to raise
impulses. Whiff = PPv2 grade (Plan B).

---

## 9. Plan D — Character art

AI-generated in-editor (the `GeneratedAssets/` pipeline) per the bibles:
- Chaff + ambient NPC set (downtown pedestrians / thugs), the 4 enemy roster sprites, and the player
  (Kaito Mori / NR-0047, 128px + glow-mask heat ramp — `docs/rgd/avatar-v0.1.md`).
- **Scaled to the Plan-0 zoomed-out baseline** so on-screen size reads correctly.
- Assigned into the Plan-A fields (`_chaffSprite`, `_ambientMaterial`) and the roster `UnitDefinitionAsset._prefab`.
- Environment stays functional blockout (a later mood pass beautifies).

---

## 10. Cross-cutting ground truth (do not relearn the hard way)

- **DI bootstrap required** — boot via Recipe 4 (`BootstrapSettingsAsset` → post-bootstrap scene); Play-in-scene breaks DI.
- **Resolve via VContainer only**; register gameplay services in an FSM state — no singletons/`Services` locator.
- **Runtime is ground truth** — play-test every spawn/wave/AI/feel/zoom claim; don't trust code-reading.
- **New settings assets** must be registered in **Neon → Settings → Create All Settings Assets** (`SettingsAssetCreator`).
- **Instanced/ambient rendering** verified via `UnityStats.instancedBatches` or Game view — **not** MCP screenshots
  (they miss `Graphics.Draw*` immediate-mode draws).
- **Null-sprite Filled Images ignore `fillAmount`** (M2/M3 HUD lesson) — assign sprites when wiring meters.
- **New dependencies this pass:** `com.unity.cinemachine` (Plan 0), `com.unity.postprocessing` (Plan B).

---

## 11. Supersessions

This spec **supersedes** the level design doc's camera fork:
- `docs/levels/level-01-downtown-strip.md` §2 ("Width & camera: dynamic per-zone via faked width, fixed zoom"), §3
  ("fixed zoom, *faked* width"), and §9 ("Fixed zoom" veto).
- Replaced by: **real dynamic zoom via the Cinemachine rig (§4)**; occluders demote to set-dressing.

Follow-up: update the level doc to reflect the camera change (or annotate it as superseded by this spec). All other
level-doc content (zones, pacing, waves, theme) stands.

---

## 12. Testing & verification

- **EditMode suite green** (138 baseline; scope `BrainlessLabs.Neon.Tests.EditMode` — DTT third-party tests are
  pre-broken). New sim tests per Plan A (§5).
- **Runtime gates (Recipe 4)** — the carried feel-flags resolve here:
  - Camera baseline reads (avatar/enemies not too big); per-zone zoom blends smoothly.
  - Overdrive "scream" validates in a real Zone 5 run.
  - Dawn-pile legibility (finish prompt + glow reads through the crowd).
  - Finish-prompt readability (Zone 2 teach).
  - Full-run wall-clock 10–15 min with real fighting.

---

## 13. Risks & open tuning

- **Camera crispness** — dropping pixel-perfect may shimmer pixel art in motion; mitigate with point filtering + sane
  PPU; PPv2 grade/bloom softens the concern. Verify in motion at target resolution.
- **PPv2 perf** at 150 chaff (Plan B gate).
- **`AI_Active` gap** on new roster/elite/mini-boss (Plan C verify).
- **In-editor tuning** (runtime ground truth): baseline + per-zone ortho sizes, walkable widths, enemy `_maxHealth`
  vs `FinishReadyHealthThreshold`, density curve shape, separation/cohesion weights.

---

*Next: `superpowers:writing-plans` — expected to produce plans 0 / A / B / C / D / E (its scope-check may split
further). Branch off `master`.*
