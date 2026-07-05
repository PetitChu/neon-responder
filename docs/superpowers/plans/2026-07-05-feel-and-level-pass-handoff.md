# Feel & Level Pass — Handoff

**Type:** Handoff / pickup brief for the **next workflow** after the engine-base series. Not a plan, not a spec — the orientation doc so a fresh session can start this cold.
**Status:** UNBLOCKED, NOT STARTED. Runs as its **own brainstorm → spec → plan → execute workflow** (not a single plan file).
**Date:** 2026-07-05 · **Author:** Sebastien + Claude
**Supersedes as the entry point:** `docs/superpowers/plans/2026-07-04-feel-and-level-pass-pre-brief.md` (still valid for its scope items; its "gated on level-layout delivery" framing is now **satisfied** — see Inputs).

---

## TL;DR

Sebastien flagged after M1/M2 that the game's **feel "is not super right"** and asked for a durable note so it wouldn't get lost across the M3/M4 plans. This is that work, now a first-class pass: **a purpose-built Level 01, a swarm rework (crowds not lanes), real sprites, and the cosmetic UI/feedback polish the engine-base milestones deliberately deferred.** The level design is done and locked; the swarm/UI/art work is designed-against-it but not built.

**This pass builds the game's *form* on top of the engine-base *function*.** M0–M4 proved the systems work; this makes them read and feel right.

---

## 1. Sequencing — where this sits (read this first)

The engine-base series is **M0 → M1 → M2 → M3 → M4**. Real state as of 2026-07-05:

- **M0–M3: DONE + merged to `master`** (tip `c37b467`). 117/117 EditMode tests. Full run boots → encounters → dawn.
- **M4 (Plan 5): WRITTEN, NOT EXECUTED.** Plan at `docs/superpowers/plans/2026-07-05-neon-engine-base-plan5-m4-feel.md` (12 tasks, actives + systemic feel). Awaiting execution on branch `claude/neon-m4-feel`.

**So this Feel & Level pass is slotted AFTER M4 executes.** Do not start it until M4 is in and gated — several of its items (cosmetic HUD, whiff desaturate) are the deferred *other half* of M4 deliverables and only make sense once M4's functional versions exist.

```
M0 ✓ → M1 ✓ → M2 ✓ → M3 ✓(merged) → M4 (written, execute next) → [THIS: Feel & Level pass]
```

---

## 2. What "done" looks like for this pass (the goal)

Level 01 "First Response" plays as designed: a rain-slick downtown strip that **teaches the core loop** — one concept per intimate corridor, tested in the next wide plaza, escalating to a first-Overdrive payoff. The swarm reads as a **crowd to wade through**, not marching columns. Real sprites replace the `HitEffect.png` placeholders. The QTE/finish prompt, HUD, and feedback are **legible and juicy** in the dawn-density pile.

The experience-goal sentence (from the level doc, locked):
> auto-engage → Finish-Ready → contextual Act verb → Momentum builds → the build fires harder / earns faster → ending on the player's first Overdrive high.

---

## 3. Scope (consolidated — the six pre-brief items + M4's deferrals)

From `2026-07-04-feel-and-level-pass-pre-brief.md`, mapped to current code:

1. **New purpose-built level** — Level 01, designed at `docs/levels/level-01-downtown-strip.md`. Implement via the established path (`Level` + `LevelConfigurationAsset` + `SpawnerService` + swarm block — `neon-recipes` Recipe 3). No legacy scene rebuilds. Replaces the placeholder Level1 config/geometry.
2. **QTE / finish-prompt + feedback UI upgrade** — `UIHUDFinishPrompt` is deliberately minimal today (`KICK 1/2` → "FINISH!" text). Wants real verb glyphs + presentation + juice. **This is the cosmetic half M4 deferred (F1).**
3. **Swap placeholder sprites** — chaff proxies (`SwarmRenderRig._chaffSprite`) + ambient material (`SwarmAmbient.mat`) still use `HitEffect.png`. Needs downtown NPC/thug art. Avatar bible exists (`docs/rgd/avatar-v0.1.md`, Kaito Mori / NR-0047) for the player.
4. **Kill chaff lane spawning/holding** — today `SwarmSpawnSystem` floods 3 fixed lanes and `SwarmSteeringSystem` pins lane-Y. Rip that out. (Layer-1 internal — see §5 isolation note.)
5. **Real chaff crowd grouping** — replace lane-hold with separation/flocking so the swarm reads as a mob. The "seek+separation" the spec always wanted; M1 shipped lane-Y+jitter as a documented stopgap, and every gate since flagged the resulting blob.
6. **Lanes → ambient walkway paths** — ambient agents align on **authored paths that follow the strip's sidewalks/crossings**, doubling as environment storytelling (pedestrians, marketgoers). This is the `SwarmDensityBlock` belt-rect **evolving into level-authored paths** consumed by ambient placement.

**Plus M4's explicitly-deferred "do it once" items** (from Plan 5 deviations 1–2, decision F1):
- Cosmetic HUD polish: verb-glyph art, meter restyling (Momentum/XP/Overcharge/Special bars), the finish-prompt redesign (= item 2).
- Whiff **true fullscreen desaturate** — M4 ships record-scratch SFX + a red uGUI flash; the real desaturate needs render infra (see §5) and belongs here, against the level's look.

> **The "do it once" rule (why these merged):** M4 built *functional* versions against the placeholder Level1; redoing the *form* here against the real level + sprites avoids authoring HUD/feedback art twice. That was the whole reason to split them.

---

## 4. Inputs / authoritative artifacts

| Artifact | What it gives you |
|---|---|
| **`docs/levels/level-01-downtown-strip.md`** | **The level design — LOCKED at v0.1.** Five zones, per-zone widths, pacing curve, the §8 wave/swarm config table, §10 open questions. **The spec for this pass is written against this doc.** This is the gate the pre-brief was waiting on — now delivered. |
| `docs/superpowers/plans/2026-07-04-feel-and-level-pass-pre-brief.md` | Original scope items 1–6 + their code mappings. |
| `docs/superpowers/specs/2026-07-04-neon-engine-base-design.md` | The parent design spec (§5.2 swarm/bridge, §5.5 feedback, §6 density budget). This pass is *within* that spec's world. |
| `docs/superpowers/plans/2026-07-05-...-plan5-m4-feel.md` §"Deviations" | Exactly what M4 deferred here + why (built-in RP, do-it-once). |
| Gate records in plans 2–4 (and M4's, once run) | Carried hands-on feel flags — see §7. |
| `docs/rgd/avatar-v0.1.md` | Player character art bible (Kaito Mori / NR-0047). |
| Memory: `neon-feel-and-level-pass`, `neon-responder-render-pipeline` | Cross-session context; the render-pipeline gotchas. |

---

## 5. Technical ground truth (the gotchas that will bite)

**Verified across M0–M3; do not relearn the hard way.**

- **Built-in Render Pipeline, not URP.** URP is installed but unassigned (`m_CustomRenderPipeline: {fileID: 0}`). Consequences for this pass: no post-processing stack → **whiff desaturate, rain, wet-ground reflections all need built-in-RP-appropriate infra** (fullscreen `Graphics.Blit` material, or add a post stack — a real decision to make in the spec). Instanced draws must use `Graphics.DrawMeshInstanced` + an instancing-safe shader (`Neon/InstancedUnlit`), never `RenderMeshInstanced` or `Sprites/Default` under manual instancing (both fail silently). *(Memory: `neon-responder-render-pipeline`.)*
- **Camera zoom is FIXED.** `CameraFollow` uses `PixelPerfectCamera` — no runtime orthographic-size control. The level's narrow↔wide feel is **faked via walkable-band width + occluders** (level doc §3), authored as scene colliders, *not* code. Dropping PixelPerfect for real zoom is a large art-pipeline effort (veto point, level doc §9).
- **The swarm bridge isolates the rework.** The engagement spine talks to `ISwarmBridge`, not the sim internals (spec §5.2, M1 fork F4). So items 4/5/6 (spawn/steering/ambient-paths) change **only Layer-1 internals** — `SwarmSpawnSystem`, `SwarmSteeringSystem`, `SwarmRenderRig`, `SwarmDensityBlock`, and the sim components. **Nothing above Layer 1 moves.** That's the safety margin for the crowd rework.
- **AI_Active spawn gap.** Spawned enemies need `EnemyBehaviour.AI_Active` set (fixed for waves in M1, but confirm every new wave/elite/mini-boss actually engages — `neon-troubleshooting`, level doc §9).
- **MCP editor screenshots miss `Graphics.Draw*` immediate-mode draws.** Verify instanced/ambient rendering via `UnityStats.instancedBatches` or the Game view, not the MCP screenshot tool. (Bit us in the spike + M3.)
- **Settings assets don't auto-create at runtime.** Use the editor menu **Neon → Settings → Create All Settings Assets** (`SettingsAssetCreator`). Any new settings asset this pass adds must be registered there.
- **Runtime is ground truth.** Boot via Recipe 4 (`BootstrapSettingsAsset` → Post-Bootstrap Scene). Play-test every spawn/wave/AI/feel claim; don't trust code-reading. Null-sprite Filled Images ignore `fillAmount` (M2/M3 HUD lesson).
- **Test baseline:** 117 (M0–M3) → 138 after M4. Keep the suite green; scope runs to `BrainlessLabs.Neon.Tests.EditMode` (third-party DTT tests are pre-broken).

---

## 6. Decisions already locked (don't re-litigate in brainstorm)

From the level design doc §2:
- **Experience goal:** teach the loop cleanly, controlled ramp, ends on first Overdrive.
- **Width & camera:** dynamic per-zone feel via faked width (fixed zoom).
- **Macro structure:** corridor → plaza hybrid; pacing expressed spatially.
- **Theme:** rain-slick downtown strip.
- **5-zone teaching arc:** Service Alley (auto-engage) → Storefront Row (Finish-Ready + Act verb) → The Scaffold (Momentum chain) → Night Market (swarm density) → Neon Crossing (Overdrive payoff). Concept-per-corridor, test-per-plaza.
- **Swarm rework shape:** chaff = crowd/flocking (not lanes); ambient = authored walkway paths (the belt-rect → authored-paths evolution). This *replaces* the earlier "per-wave belt-Y override" idea.

---

## 7. Carried hands-on feel flags (this pass is where they finally resolve)

Threaded through every gate record, waiting on the real level + feel layer:
- **Overdrive "screams"** (M2 open gate) — Zone 5's payoff must validate in a real run.
- **Objective/arrow/glow legibility in the dawn pile** (M3 flag) — the chaff-blob (item 5) is the suspected culprit; the crowd rework should fix the read.
- **Full-run wall-clock 10–15 min** with real fighting (M3 flag; knobs live: `RebootDurationSeconds`, encounter count, wave sizes).
- **Finish-prompt readability** (Zone 2 depends on the item-2 UI upgrade).
- **Momentum→Overdrive naming vs the shipped HUD** (XP bar / Overcharge meter / level-up picker) — reconcile (level doc §10 Q5).

---

## 8. Open questions for the brainstorm (from level doc §10 + this handoff)

1. Exact walkable widths + occluder framing per zone (needs eyes on the pixel-perfect frame at target res).
2. Enemy roster + HP tuning so Finish-Ready windows land where the pacing wants them (`FinishReadyHealthThreshold`). New `UnitDefinitionAsset`s needed: thug / chump / elite / mini-boss (only a generic enemy exists).
3. Zone 4 mid-plaza lull: scripted `Manual` wave gap, or just a density trough?
4. Mini-boss: bespoke unit, or an elite with inflated stats for v1?
5. **Render-infra call:** do rain/reflections/whiff-desaturate justify adding a post stack to the built-in RP, or stay shader/sprite-only? (Biggest technical fork of the pass.)
6. Crowd-behavior approach: full flocking (boids-style separation/cohesion/alignment) vs. cheaper separation-only — perf-budget it against the §6 density (chaff 80–150 + ~100 ambient) and the spike's ~197 FPS headroom.
7. Does the ambient-path authoring want a new data type (spline/waypoint list on the level) — and how does it coexist with the `SwarmDensityBlock`?

---

## 9. First moves for whoever picks this up

1. **Confirm M4 is executed + gated** (this pass depends on M4's functional feedback existing). If not, execute Plan 5 first.
2. **Run the `superpowers:brainstorming` skill** against the level design doc — this is creative/architecture work; brainstorm before spec. Resolve §8's open questions (especially the render-infra fork #5 and the crowd-behavior approach #6) with Sebastien.
3. **Write the spec** against `docs/levels/level-01-downtown-strip.md` (it's the authority for *what the level is*; the spec adds *how it's built*).
4. **Then `superpowers:writing-plans`** — likely more than one plan (swarm rework / level build / art+sprite integration / UI-feedback polish are semi-independent subsystems; the writing-plans skill's scope-check may split them).
5. **Branch off `master`** once M4 is merged.

**Do not implement from this handoff or the pre-brief alone** — they orient; the spec is where design decisions get made.

---

*This pass closes out the "make it feel right" intent behind the whole engine-base effort. After it: post-MVP content per spec §7's priority order (weapon-throw → jump-attack → elite execution + Sirencatcher → Jammer/Bruiser → Boss → Rescue → 2nd Special → biomes).*
