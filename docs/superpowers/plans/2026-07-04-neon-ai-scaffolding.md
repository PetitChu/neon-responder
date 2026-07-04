# Neon Responder AI Scaffolding — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a project-side AI context layer for Neon Responder — a repo-root `CLAUDE.md` plus six `neon-*` skills under `.claude/skills/` — following the Squido `project-skill-schematic` format with 100% custom content.

**Architecture:** Six independent `SKILL.md` files (architecture, conventions, combat, game-systems, recipes, troubleshooting) each owning one knowledge domain, plus a `CLAUDE.md` that indexes them and states the non-negotiable rules. Content is written from codebase analysis of master `c27e365` + three memory notes + the Notion GDD (design intent only).

**Tech Stack:** Markdown only (no compiled code). Documents Unity 6 / C# / VContainer / UniTask / UnityHFSM / URP 17.3.0.

**Spec:** `docs/superpowers/specs/2026-07-04-neon-ai-scaffolding-design.md` (read it before starting).

---

## Verification model (read first)

There are no automated tests for markdown. **Every task's "verify" step is a manual checklist the implementer runs before committing:**

- **V1 — Citations resolve:** every file/type/method named in the skill exists in the codebase (grep/read to confirm). No invented APIs.
- **V2 — Frontmatter valid:** `name:` matches the directory (`neon-<x>`); `description:` ends with "Use when …" triggers.
- **V3 — Sections populated:** every required section from this task is present with real content — no `TBD`/`TODO`/empty headers.
- **V4 — Status tags correct:** anything not verified in code is tagged `[GDD-TARGET · not built]`; anything mid-DOTS-migration is `[IN-MIGRATION → DOTS]`; verified code is `[BUILT]` (tag only where built-vs-planned is ambiguous — don't spray tags on obviously-real code).
- **V5 — No framework/GDD duplication:** reference the GDD by section pointer; don't paste large GDD passages.

**Status-tag legend** (use verbatim across all files):
- `[BUILT]` — exists in code on master.
- `[IN-MIGRATION → DOTS]` — exists but actively being reworked toward DOTS/ECS.
- `[GDD-TARGET · not built]` — GDD design intent only; not in code.

**GDD link** (design ground-truth): <https://app.notion.com/p/Neon_Responder_GDD-3935c0654b6b80beaa94f61de4bbf47d>

---

## File structure

| File | Responsibility |
|---|---|
| `CLAUDE.md` (repo root) | Identity, GDD link, non-negotiable rules, assembly/namespace map, skill index, status-tag legend |
| `.claude/skills/neon-architecture/SKILL.md` | How the game is wired: DI, ApplicationFSM boot, scopes, service roster, assemblies |
| `.claude/skills/neon-conventions/SKILL.md` | How code is written: folders, asmdefs, naming, service pattern, settings pattern, prohibited patterns |
| `.claude/skills/neon-combat/SKILL.md` | Engine-agnostic combat design + MonoBehaviour-now/DOTS-target status; verb model; unit FSM |
| `.claude/skills/neon-game-systems/SKILL.md` | Systems catalog (built) + GDD-target systems (not built) |
| `.claude/skills/neon-recipes/SKILL.md` | Step-by-step workflows (add service/state/level, play-test, feature/bugfix flow) |
| `.claude/skills/neon-troubleshooting/SKILL.md` | Known traps: DI bootstrap, AI_Active gap, scene migration, editor-bootstrap flakiness |

**Dependencies:** Tasks 1–6 (skills) are independent and may run in parallel. Task 7 (CLAUDE.md) should run after 1–6 so its skill index matches the final `name`/`description` of each skill. Task 8 is a final cross-file consistency pass.

**Shared pre-read for every task** (facts live here — read before writing):
- `Assets/_neon/Scripts/**` (the subsystem the task covers)
- `Assets/_neon/Scripts/*/*.asmdef` for assembly/namespace facts
- Memory: `neon-responder-combat-architecture`, `neon-responder-gdd`, `neon-responder-run-and-verify` (in `C:\Users\sebch\.claude\projects\G--Brainless-Labs-neon-responder\memory\`)
- The spec (section 6 has the per-skill section outline)

---

## Task 1: `neon-architecture` skill

**Files:**
- Create: `.claude/skills/neon-architecture/SKILL.md`

**Source files to read first:**
- `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/**` (all — states, transitions, core builder/runner)
- `Assets/_neon/Scripts/Lifecycle/LifetimeScope/ApplicationLifetimeScope.cs`
- `Assets/_neon/Scripts/Level/Level.cs`
- `Assets/_neon/Scripts/Entities/{IEntitiesService,EntitiesService,EntitiesQueries}.cs`
- `Assets/_neon/Scripts/Spawner/SpawnerService.cs`
- `Assets/_neon/Scripts/Bootstrap/**`, `Assets/_neon/Scripts/Editor/Bootstrap/EditorBootstrap.cs`
- All three `*.asmdef` files
- Find the centralized `Services` accessor: `grep -rn "Services" Assets/_neon/Scripts/Lifecycle` and confirm its exact form before describing it.

- [ ] **Step 1: Write the frontmatter**

```yaml
---
name: neon-architecture
description: "Neon Responder architecture — VContainer DI, ApplicationFSM (UnityHFSM) boot chain, LifetimeScope hierarchy, service roster, assembly map. Use when designing features, understanding game structure, or making architectural decisions in Neon Responder."
---
```

- [ ] **Step 2: Write the required sections** (per spec §6.1)

Required, in order:
1. **Core identity** table — Engine: Unity 6 · Language: C# · Render: URP 17.3.0 + 2D Pixel Perfect · Networking: none (single-player) · DI: VContainer · Async: UniTask · Reactive: R3 · App FSM: UnityHFSM · Platform: PC/Steam + Unity Services.
2. **Main architectural pattern** — service-oriented over VContainer DI, booted by a lifecycle FSM.
3. **Boot / ApplicationFSM** — `InitialState → PlatformState → UnityServicesState → GameServicesState → GameState`; `GameState` loads `_postBootstrapScene` (`SceneDefinitionAsset`); health-checked transitions (`HealthCheckedServicesTransition`, `IHealthCheckedService`).
4. **DI scopes** — `ApplicationLifetimeScope` (app) → `Level : LifetimeScope` (per level); describe how a Level resolves app services via parent scope.
5. **The `Services` accessor** — describe exactly as found in Step-1 read (singletons removed). If its form differs from "a static accessor", document what's actually there.
6. **Service roster** table — Audio, Input, Scenes, Steam, UnityServices, Entities, Spawner: interface + one-line responsibility each.
7. **Assembly map** — `BrainlessLabs.Neon` (runtime) / `BrainlessLabs.Neon.Lifecycle` (boot/FSM/services, references runtime) / `BrainlessLabs.Neon.Editor` (editor-only). Reference direction rules.
8. **Critical rules** — must DI-bootstrap to run (see neon-troubleshooting + neon-recipes); resolve services via DI/`Services`, never new singletons; no networking assumptions; check status tags before assuming a GDD system exists.

- [ ] **Step 3: Verify** — run V1–V5. Specifically confirm: the boot state order matches the actual state classes; the `Services` accessor description matches code; every asmdef name is exact.

- [ ] **Step 4: Commit**

```bash
git add .claude/skills/neon-architecture/SKILL.md
git commit -m "Add neon-architecture skill"
```

---

## Task 2: `neon-conventions` skill

**Files:**
- Create: `.claude/skills/neon-conventions/SKILL.md`

**Source files to read first:**
- `Assets/_neon/Scripts/Settings/{BaseSettingsAsset,ISettings,ISettingsAsset}.cs`
- A representative service quartet: `Audio/{IAudioService,AudioService,AudioSettings,AudioSettingsAsset}.cs`; `Lifecycle/Services/Platform/{ISteamService,SteamService,NullSteamService}.cs`
- `Assets/_neon/Scripts/Attributes/**` + `Assets/_neon/Scripts/Editor/Drawers/**` (ShowIf, ReadOnly, Help)
- 3–4 files across the runtime to confirm brace style, `_camelCase` fields, `[SerializeField]` usage
- All `*.asmdef` files

- [ ] **Step 1: Frontmatter**

```yaml
---
name: neon-conventions
description: "Neon Responder conventions — folder layout, assembly/namespace naming, the service + Null + settings-asset pattern, naming rules, prohibited patterns. Use when writing code or reviewing for consistency in Neon Responder."
---
```

- [ ] **Step 2: Required sections** (per spec §6.2)

1. **Folder structure** — ASCII tree of `Assets/_neon/` (Scripts subdivided as on disk: Attributes, Audio, Bootstrap, Camera, Editor, Entities, Input, Items, Level, Lifecycle, Objects, Scenes, Settings, Spawner, StateMachine, Tools, UI, Units, WaveManager[legacy]).
2. **Assembly definitions** — the three asmdefs, names exact, reference direction (Lifecycle → runtime; Editor Editor-only).
3. **Namespace map** — `BrainlessLabs.Neon`, `BrainlessLabs.Neon.Lifecycle`, `BrainlessLabs.Neon.Editor`.
4. **Naming table** — element → style → example, filled from real code (private field `_camelCase`, property/method `PascalCase`, etc.). Only list styles you confirmed.
5. **The Service pattern** — `IFoo` + `FooService` + `NullFooService` + `FooSettingsAsset : BaseSettingsAsset`; where each lives; how it registers in DI; how consumers access it.
6. **Settings-asset pattern** — `BaseSettingsAsset`/`ISettings`/`ISettingsAsset`, `[CreateAssetMenu]`, `Assets/Resources/Settings/` location.
7. **`[SerializeField]` + custom attributes** — `ShowIf`, `ReadOnly`, `Help` with a one-line usage each.
8. **Async** — UniTask + CancellationToken expectations (confirm from code usage).
9. **Prohibited patterns** — singletons; `WaveManager`; pressing Play directly in a level scene; inventing APIs. Each with the correct alternative.

- [ ] **Step 3: Verify** — V1–V5. The naming table must only contain confirmed conventions (grep to confirm `_camelCase` before asserting it).

- [ ] **Step 4: Commit**

```bash
git add .claude/skills/neon-conventions/SKILL.md
git commit -m "Add neon-conventions skill"
```

---

## Task 3: `neon-combat` skill

**Files:**
- Create: `.claude/skills/neon-combat/SKILL.md`

**Source files to read first:**
- `Assets/_neon/Scripts/StateMachine/{UnitState,UnitStateMachine}.cs`
- All of `Assets/_neon/Scripts/Units/PlayerStates/**` and `Units/EnemyStates/**`
- `Assets/_neon/Scripts/Units/{HealthSystem,UnitActions,UnitDefinitionAsset,UnitSettings,WeaponAttachment,AttackData,EnemyBehaviour,EnemyManager}.cs`
- Memory `neon-responder-combat-architecture`; GDD sections §0.4 (Combat-Moves Recast) and §6/§9.

- [ ] **Step 1: Frontmatter**

```yaml
---
name: neon-combat
description: "Neon Responder combat — the engine-agnostic verb model (punch/kick/grab/throw/jump/weapon), the custom UnitStateMachine, and MonoBehaviour-now vs DOTS-target status. Use when working on combat, unit states, or the Act-verb design in Neon Responder."
---
```

- [ ] **Step 2: Required sections** (per spec §6.3)

1. **Engine-status banner** — combat *design* is engine-agnostic; today it's MonoBehaviour FSM `[BUILT]`; DOTS/ECS migration is active `[IN-MIGRATION → DOTS]`, bridged by `EntitiesService`. State that design intent must stay engine-agnostic (per memory + GDD).
2. **The verb model / "Act" dispatcher** — list the verbs (punch, kick, grab, throw-enemy, jump-attack, weapon-attack, weapon-throw, pickup); note the single contextual "Act" button + dispatch priority is `[GDD-TARGET · not built]` while the individual verb-states are `[BUILT]`. Pointer to GDD §0.4.c/§0.4.d.
3. **The custom `UnitStateMachine`** — how it works, `UnitState` base, distinct from the lifecycle UnityHFSM (different assembly).
4. **Player states catalog** — table: state class → what it does → GDD verb role. Cover all `PlayerStates/**` incl. shared `Unit*` states.
5. **Enemy states catalog** — the enemy states + `EnemyBehaviour` targeting; note `AI_Active` gate (cross-ref neon-troubleshooting).
6. **Supporting types** — `HealthSystem`, `UnitActions`, `UnitDefinitionAsset`, `WeaponAttachment`, `AttackData`: one line each.
7. **Momentum / Finish-Ready** — `[GDD-TARGET · not built]`; short intent + GDD §0.4.d/§9 pointer. Do not describe as existing code.

- [ ] **Step 3: Verify** — V1–V5. Every state class in the catalogs must exist as a file. The Act-dispatcher and Momentum must be tagged GDD-TARGET.

- [ ] **Step 4: Commit**

```bash
git add .claude/skills/neon-combat/SKILL.md
git commit -m "Add neon-combat skill"
```

---

## Task 4: `neon-game-systems` skill

**Files:**
- Create: `.claude/skills/neon-game-systems/SKILL.md`

**Source files to read first:**
- `Assets/_neon/Scripts/Entities/**`, `Assets/_neon/Scripts/Spawner/**`, `Assets/_neon/Scripts/Level/**`
- `Assets/_neon/Scripts/Scenes/**`
- `Assets/_neon/Scripts/Units/HealthSystem.cs`
- `Assets/_neon/Scripts/Camera/**`, `Assets/_neon/Scripts/Items/**`, `Assets/_neon/Scripts/UI/**`
- Memory `neon-responder-gdd`; GDD §1, §6, §8, §12 for the target systems.

- [ ] **Step 1: Frontmatter**

```yaml
---
name: neon-game-systems
description: "Neon Responder game systems — Entities/Spawner/Level, Health, Scenes, Camera, UI (built), plus the GDD-target systems (auto-engage, Finish-Ready, Momentum, Protocols, Signal) not yet built. Use when working on gameplay systems or checking what exists vs planned in Neon Responder."
---
```

- [ ] **Step 2: Required sections** (per spec §6.4)

1. **System catalog** table — every system + status tag (BUILT vs GDD-TARGET).
2. **Per-system (BUILT)** — for each: key interfaces/classes, data flow, dependencies, tuning ScriptableObjects:
   - `EntitiesService` + `EntitiesQueries` (`IEntitiesService`) — central entity registry + queries (replaced `EnemyManager` tracking).
   - `SpawnerService` + `LevelConfigurationAsset` / `EnemyWaveDefinition` / `EnemySpawnEntry` — progression-based spawning.
   - `Level` controller + per-level scope.
   - `HealthSystem`.
   - Scenes (`ScenesService`, `SceneDefinitionAsset`, `SceneType`).
   - Camera (`CameraFollow`, `CameraShake`, `ParalaxScrolling`); Items/pickups; UI/HUD (`UIManager`, `UIHUDHealthBar`, …).
3. **GDD target systems — NOT built (`[GDD-TARGET · not built]`)** — auto-engage, Finish-Ready, Momentum meter, roguelite Protocols + XP/Neon-Charge/Overcharge economy, the Signal. One-line intent + GDD section pointer each. Explicitly: not in code.

- [ ] **Step 3: Verify** — V1–V5. Built systems cite real files; target systems are all tagged and none imply code exists.

- [ ] **Step 4: Commit**

```bash
git add .claude/skills/neon-game-systems/SKILL.md
git commit -m "Add neon-game-systems skill"
```

---

## Task 5: `neon-recipes` skill

**Files:**
- Create: `.claude/skills/neon-recipes/SKILL.md`

**Source files to read first:**
- A full service quartet (e.g. Audio) + its DI registration site (`GameServicesState.cs` / `ApplicationLifetimeScope.cs`)
- `Assets/_neon/Scripts/StateMachine/**` + one Player state as a template
- `Assets/_neon/Scripts/Spawner/LevelConfigurationAsset.cs`, `Scenes/SceneDefinitionAsset.cs`, `Bootstrap/BootstrapSettingsAsset.cs`
- Memory `neon-responder-run-and-verify` (the play-test flow is authoritative here — reproduce its steps).

- [ ] **Step 1: Frontmatter**

```yaml
---
name: neon-recipes
description: "Neon Responder recipes — step-by-step workflows for adding a service, a unit state, a level, a settings asset, and play-testing a level in-editor. Use when implementing features or following established workflows in Neon Responder."
---
```

- [ ] **Step 2: Required sections** (per spec §6.5) — each an executable numbered workflow with real file paths:

1. **Add a service** — create `IFoo`, `FooService`, `NullFooService`, `FooSettingsAsset`; register in the DI container (cite the exact registration site); access via DI/`Services`.
2. **Add a Player/Enemy state** — subclass `UnitState`, implement enter/tick/exit, wire transitions in the state machine, hook `UnitActions`/animation.
3. **Add a level** — create `LevelConfigurationAsset` (+ `EnemyWaveDefinition`s), a scene with a `Level` scope, a `SceneDefinitionAsset`; wire `_postBootstrapScene` to boot into it.
4. **Play-test a level in-editor** — enable `_enableEditorBootstrap` on `BootstrapSettingsAsset` (`Assets/Resources/Settings/`); boot chain runs; point `_postBootstrapScene` at the target level's `SceneDefinition`; waves are progression-based (nudge player `transform.x` to hit the trigger %). Reproduce the memory note exactly.
5. **Add a ScriptableObject settings asset** — `BaseSettingsAsset` subclass + `[CreateAssetMenu]` + `Resources/Settings/`.
6. **Feature flow / bugfix flow** — requirement → branch (`claude/<desc>`) → implement per neon-conventions → verify at runtime (per user working style: runtime is ground truth) → PR.
7. **Code-review checklist** — conventions compliance, DI correctness, no legacy (`WaveManager`/`04`/`05`), status-tag accuracy.

- [ ] **Step 3: Verify** — V1–V5. The play-test recipe must match `neon-responder-run-and-verify`. The DI registration site must be the real one (confirm by reading it).

- [ ] **Step 4: Commit**

```bash
git add .claude/skills/neon-recipes/SKILL.md
git commit -m "Add neon-recipes skill"
```

---

## Task 6: `neon-troubleshooting` skill

**Files:**
- Create: `.claude/skills/neon-troubleshooting/SKILL.md`

**Source files to read first:**
- Memory `neon-responder-run-and-verify` (the seed content — DI bootstrap, AI_Active gap, scene migration, editor-bootstrap flakiness).
- `Assets/_neon/Scripts/Spawner/SpawnerService.cs` (confirm the `AI_Active` / `SpawnUnit` detail).
- `Assets/_neon/Scripts/Editor/Bootstrap/EditorBootstrap.cs`.
- `Assets/_neon/Scripts/Level/Level.cs` (confirm the LifetimeScope parent relationship).

- [ ] **Step 1: Frontmatter**

```yaml
---
name: neon-troubleshooting
description: "Neon Responder troubleshooting — DI-bootstrap failures, the AI_Active spawn gap, scene-migration status, and editor-bootstrap flakiness. Use when debugging errors or investigating known problems in Neon Responder."
---
```

- [ ] **Step 2: Required sections** (per spec §6.6), each Symptom → Cause → Fix:

1. **DI / bootstrap** — Symptom: Play in a level scene fails to resolve `IEntitiesService`. Cause: `Level : LifetimeScope` has no parent app scope when booted directly. Fix: enable editor bootstrap (cross-ref neon-recipes play-test).
2. **Spawning — the `AI_Active` gap** — Symptom: spawned enemies spot the player but never attack. Cause: `SpawnerService` spawns/injects/registers enemies and they acquire the target, but never sets `EnemyBehaviour.AI_Active = true`. Fix: one-line set in `SpawnerService.SpawnUnit`.
3. **Scene migration status** — only `03_Level1` uses `Level`+`SpawnerService`+`LevelConfigurationAsset`; `04_Level2`/`05_Level3` are legacy `WaveManager`, no DI scope (`[Inject]` never populates → broken). Don't rebuild; GDD abandons them.
4. **Editor-bootstrap flakiness** — Symptom: drops out of Play mode mid-session. Cause: external file changes (git ops / asset writes) trigger a Unity refresh. Fix: avoid external writes during play-test; re-enter Play.
5. **Debugging workflow** — runtime-first: reproduce in-editor before declaring a fix (per user working style).

- [ ] **Step 3: Verify** — V1–V5. Each entry must describe a real problem confirmed against code/memory (not hypothetical). Confirm the `AI_Active` detail by reading `SpawnerService`.

- [ ] **Step 4: Commit**

```bash
git add .claude/skills/neon-troubleshooting/SKILL.md
git commit -m "Add neon-troubleshooting skill"
```

---

## Task 7: `CLAUDE.md` (repo root)

**Files:**
- Create: `CLAUDE.md`

**Depends on:** Tasks 1–6 (reads their final `name`/`description` for the index).

**Source:** the six skill files + spec §7.

- [ ] **Step 1: Write CLAUDE.md** with these sections:

1. **Identity** — one-liner + genre-fusion summary (from `neon-responder-gdd` memory / GDD §1).
2. **Design ground-truth** — GDD link + rule: "GDD = design truth; the `neon-*` skills = implementation truth. Skills reference the GDD, not duplicate it."
3. **Non-negotiable rules** — must DI-bootstrap to run; combat design stays engine-agnostic; no singletons / no `WaveManager` / no direct scene-Play; single-player (no networking); most GDD systems are not built yet — check status tags.
4. **Assembly / namespace map** — the three assemblies + reference direction (quick reference).
5. **Skill index** — a table: skill name → "read when …" (one line each, matching each skill's final description).
6. **Status-tag legend** — `[BUILT]` / `[IN-MIGRATION → DOTS]` / `[GDD-TARGET · not built]`.

- [ ] **Step 2: Verify** — V2/V3/V5 + the skill index names exactly match the six skill directory names and their descriptions.

- [ ] **Step 3: Commit**

```bash
git add CLAUDE.md
git commit -m "Add project CLAUDE.md with rules and skill index"
```

---

## Task 8: Cross-file consistency pass + finish

**Files:** all seven (read-only review + fixes).

- [ ] **Step 1: Consistency review** — read all seven files together and check:
  - Terminology is identical across files (e.g. always "`Level : LifetimeScope`", "`Services` accessor", "Act verb").
  - Status tags used consistently; nothing GDD-only is presented as built anywhere.
  - Cross-references resolve (e.g. neon-combat → neon-troubleshooting `AI_Active`; neon-recipes → neon-troubleshooting bootstrap).
  - Skill `name`/`description` in CLAUDE.md's index match the actual files.
  - No `TBD`/`TODO`/placeholder anywhere.

- [ ] **Step 2: Fix any inconsistencies inline**, then commit any fixes:

```bash
git add -A
git commit -m "Consistency pass across neon AI scaffolding"
```

- [ ] **Step 3: Definition of done** — confirm all present:
  - `CLAUDE.md` at repo root.
  - Six `.claude/skills/neon-*/SKILL.md` files.
  - Every citation resolves to real code; every GDD-only system tagged.
  - All files under version control on `claude/ai-scaffolding`, none under `Assets/` (no Unity disruption).

- [ ] **Step 4: Report** — summarize what was built and hand back the branch for review / PR.

---

## Self-review (author checklist — completed at plan-write time)

- **Spec coverage:** spec §4 layout → File structure + Tasks 1–7; §5 cross-cutting → Verification model + every task's V4/status legend; §6.1–6.6 → Tasks 1–6; §7 CLAUDE.md → Task 7; §8 accuracy → V1/V5 in every task. No gaps.
- **Placeholder scan:** no `TBD`/`TODO`/"implement later"; content-task steps specify exact sources, sections, and checks (the appropriate "complete content" for documentation work).
- **Type consistency:** class/type names (`EntitiesService`, `SpawnerService`, `Level`, `UnitStateMachine`, `BaseSettingsAsset`, `SceneDefinitionAsset`, `LevelConfigurationAsset`, `EnemyBehaviour.AI_Active`) are used identically across all tasks and match the spec.
