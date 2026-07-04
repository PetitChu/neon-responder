# Neon Responder — AI Scaffolding Design

- **Date:** 2026-07-04
- **Status:** Approved (design), pending implementation
- **Author:** Sebastien Charron + Claude
- **Branch:** `claude/ai-scaffolding`

## 1. Context & motivation

Squido projects are bootstrapped for AI-assisted development by `/squido-engineering:squido-setup`, which
generates project-side skills (`.claude/skills/{game}-*/SKILL.md`) plus a `CLAUDE.md`, following the
`project-skill-schematic` template (6 skill types: architecture, conventions, networking, game-systems,
recipes, troubleshooting).

Neon Responder is a **fundamentally different project** from the Squido VR/Quest catalog, so the standard
setup (which assumes Jungle XRKit, Meta Quest, Photon Fusion multiplayer) does not fit. We want the **same
format/shape** as Squido's project-side output, but **100% custom content** for this game.

### What Neon Responder is

**Neon Responder: Night Shift** — a side-scrolling neon beat-'em-up, a hard fusion of four genres:
beat-'em-up (feel) + idle-survivor "VS" density (threat) + roguelite stack (growth) + contextual-verb
combat (engagement). Unity 6 + a DOTS/ECS trajectory, **PC standalone** (keyboard + gamepad), **single-player**.
Design ground-truth is the GDD in Notion:
<https://app.notion.com/p/Neon_Responder_GDD-3935c0654b6b80beaa94f61de4bbf47d>

Core loop (GDD): **auto-engage → Finish-Ready → contextual "Act" verb → Momentum up → build fires harder / earns faster → more Finish-Ready.**

### Current codebase reality (master `c27e365`)

- **Engine:** Unity 6, C#, 2D (Pixel Perfect), URP 17.3.0. **No networking.**
- **DI:** VContainer. **Async:** UniTask. **Reactive:** R3. **App FSM:** UnityHFSM. **Scene refs:** Eflatun.SceneReference.
- **Assemblies:** `BrainlessLabs.Neon` (runtime), `BrainlessLabs.Neon.Lifecycle` (boot/FSM/services; references runtime), `BrainlessLabs.Neon.Editor`. Namespaces match asmdef names.
- **Boot:** `ApplicationFSM` (UnityHFSM) runs `InitialState → PlatformState (Steam) → UnityServicesState → GameServicesState → GameState`, wired through `ApplicationLifetimeScope`; `GameState` loads `_postBootstrapScene` (a `SceneDefinitionAsset`). `EditorBootstrap` forces boot-first play-testing.
- **Scopes:** `ApplicationLifetimeScope` (app) → `Level : LifetimeScope` (per level).
- **Services:** singletons were removed in favor of a centralized `Services` accessor. Roster: Audio, Input, Scenes, Steam (+Null), UnityServices (+Null), **EntitiesService** (`IEntitiesService` / `EntitiesQueries` — central entity registry+queries, replaced `EnemyManager` tracking), **SpawnerService** (progression-based spawning from `LevelConfigurationAsset` / `EnemyWaveDefinition` / `EnemySpawnEntry`).
- **Settings pattern:** `BaseSettingsAsset` / `ISettings` / `ISettingsAsset` ScriptableObjects; each service has an `IFoo` + `FooService` + (often) `NullFooService` + `FooSettingsAsset`.
- **Combat:** MonoBehaviour per-state FSM via a **custom** `UnitStateMachine` (in the runtime asmdef, *not* UnityHFSM). Player has ~20 verb-states (`PlayerGroundPunch`, `PlayerGroundKick`, `PlayerTryGrab`/`PlayerGrabEnemy`/`PlayerThrowEnemy`, `PlayerJump`/`PlayerJumpAttack`, `PlayerWeaponAttack`, `PlayerGrabItem`, plus shared `Unit*` states: `UnitHit`, `UnitKnockDown`, `UnitStandUp`, `UnitDeath`, `UnitDefend`, `UnitDropWeapon`). Enemy has ~7 states. Supporting: `HealthSystem`, `UnitActions`, `UnitDefinitionAsset` (data-driven), `WeaponAttachment`, `AttackData`.
- **Legacy (being retired):** `WaveManager`, `EnemyManager` tracking, older scenes `04_Level2`/`05_Level3` (no DI scope → broken). Only `03_Level1` uses the new `Level` + `SpawnerService` + `LevelConfigurationAsset` system.

### The three-way gap the scaffolding must resolve

An agent working here needs three kinds of knowledge that don't live cleanly in one place:

1. **Current architecture** — DI / ApplicationFSM / Services / Entities / Spawner / Level.
2. **Combat design intent** — the verb model is **engine-agnostic** and mid-migration from MonoBehaviour FSM to DOTS/ECS (active work; `EntitiesService` is the bridge).
3. **GDD north-star** — auto-engage / Finish-Ready / Momentum / roguelite Protocols / Overcharge / the Signal, **most of which is not built yet**.

## 2. Goals & success criteria

**Goal:** a hand-crafted, project-side AI context layer so any Claude Code agent understands the current
architecture, the engine-agnostic combat design, and the GDD north-star — without re-deriving them each session.

**Success:** a fresh agent, after reading `CLAUDE.md` + the relevant skill, can:
1. Place any feature in the correct assembly / service / DI scope.
2. Follow conventions precisely enough that a reviewer can cite a specific rule for any violation.
3. Avoid the known DI-bootstrap / spawn / scene-migration traps.
4. Distinguish what is **built** vs **in-migration** vs **GDD-target-not-built** before proposing work.

## 3. Non-goals (YAGNI)

- No `networking` skill (single-player).
- No slash commands, agents, or hooks.
- No Copilot / `AGENTS.md` / `.github/copilot-instructions.md` (Claude Code only).
- No plugin packaging (project-side only, not a distributable plugin).
- No deep documentation of churning DOTS internals as if stable.
- No documentation encouraging work on legacy scenes (`04`/`05`) or `WaveManager`.

## 4. Format & file layout

```
neon-responder/
├── CLAUDE.md                          # NEW — project brief + rules + skill index (repo root, auto-loaded)
└── .claude/
    └── skills/
        ├── neon-architecture/SKILL.md
        ├── neon-conventions/SKILL.md
        ├── neon-combat/SKILL.md
        ├── neon-game-systems/SKILL.md
        ├── neon-recipes/SKILL.md
        └── neon-troubleshooting/SKILL.md
```

- **Skill name prefix:** `neon-` (mirrors Squido's `{game}-*`; short, collision-free).
- **Frontmatter:** each `SKILL.md` has `name` + a `description` that ends with "Use when …" triggers, per the schematic.
- **CLAUDE.md** at repo root (none exists today; Claude Code auto-loads it).

## 5. Cross-cutting conventions (used across all skills)

### 5.1 GDD is design ground-truth; scaffolding is implementation truth
`CLAUDE.md` links the Notion GDD and states: *design intent* lives in the GDD; *how it's built* lives in
the skills. Skills reference the GDD rather than duplicating it.

### 5.2 Status tags
Wherever built-vs-planned matters, use a consistent tag so an agent instantly knows whether it's reading
reality or intent:

- `[BUILT]` — exists in code on master, documented from the code.
- `[IN-MIGRATION → DOTS]` — exists but actively being reworked toward DOTS/ECS; document direction, not churning internals.
- `[GDD-TARGET · not built]` — described in the GDD only; must **never** be described as if it exists in code.

## 6. The six skills

Each skill follows the `project-skill-schematic` quality bar: populated sections (no placeholders),
correct frontmatter, compiling/pseudo-labeled code, unambiguous conventions, real (not hypothetical)
troubleshooting entries, executable recipes. Every skill cites real files and **never invents APIs** — if a
detail isn't verified in code, it is tagged `[GDD-TARGET]` or omitted.

### 6.1 `neon-architecture`
**Purpose:** how the game is wired.
Sections:
- **Core identity table** — Engine (Unity 6), Language (C#), Render (URP 17.3.0 + 2D Pixel Perfect), Networking (**none — single-player**), DI (VContainer), Async (UniTask), Reactive (R3), App FSM (UnityHFSM), Platform (PC/Steam + Unity Services).
- **Main architectural pattern** — service-oriented over VContainer DI, booted by a lifecycle FSM.
- **Boot / ApplicationFSM** — `InitialState → PlatformState → UnityServicesState → GameServicesState → GameState`; `GameState` loads `_postBootstrapScene`; health-checked service transitions.
- **DI scopes** — `ApplicationLifetimeScope` → `Level : LifetimeScope`; the centralized `Services` accessor (singletons removed).
- **Service roster** — Audio, Input, Scenes, Steam, UnityServices, Entities, Spawner (one line each: responsibility + interface).
- **Assembly map** — `BrainlessLabs.Neon` / `.Lifecycle` / `.Editor` + reference direction.
- **Critical rules** — DI-bootstrap to run; register services in the DI container, resolve via `Services`/injection (not singletons); no networking assumptions.

### 6.2 `neon-conventions`
**Purpose:** how code is written here.
Sections:
- **Folder structure** — `Assets/_neon/{Scripts,Scenes,Prefabs,Sprites,…}` tree; `Scripts/` subdivision.
- **Assembly definitions** — naming (`BrainlessLabs.Neon[.Lifecycle|.Editor]`), reference rules (Lifecycle → runtime; Editor → editor-only).
- **Namespace map** — `BrainlessLabs.Neon` (runtime), `BrainlessLabs.Neon.Lifecycle`, `BrainlessLabs.Neon.Editor`.
- **Naming table** — confirmed from code (`_camelCase` private fields, `PascalCase` methods/properties, etc.).
- **The Service pattern** — `IFoo` + `FooService` + `NullFooService` + `FooSettingsAsset : BaseSettingsAsset`; where each lives; DI registration.
- **Settings-asset pattern** — `BaseSettingsAsset` / `ISettings` / `ISettingsAsset`, `[CreateAssetMenu]`, `Assets/Resources/Settings/` conventions.
- **`[SerializeField]` + custom attributes** — `ShowIf`, `ReadOnly`, `Help` and their drawers.
- **Async** — UniTask (+ CancellationToken expectations).
- **Prohibited patterns** — singletons, `WaveManager`, pressing Play directly in a level scene, inventing APIs.

### 6.3 `neon-combat`
**Purpose:** the engine-agnostic combat design + migration status.
Sections:
- **Engine-status banner** — combat *design* is engine-agnostic; *today* it is MonoBehaviour FSM (`[BUILT]`); DOTS/ECS migration is active (`[IN-MIGRATION → DOTS]`), bridged by `EntitiesService`.
- **The verb model / "Act" dispatcher** — punch, kick, grab, throw-enemy, jump-attack, weapon-attack, weapon-throw, pickup; how they map to the GDD's single contextual "Act" button (dispatch priority) `[GDD-TARGET for the dispatcher; verb-states BUILT]`.
- **The custom `UnitStateMachine`** — how states are structured (distinct from the lifecycle UnityHFSM), `UnitState` base, transitions.
- **Player states catalog** — table: state → what it does → GDD verb role.
- **Enemy states catalog** — the ~7 enemy states + `EnemyBehaviour` targeting.
- **Supporting types** — `HealthSystem`, `UnitActions`, `UnitDefinitionAsset`, `WeaponAttachment`, `AttackData`.
- **Momentum / Finish-Ready** — `[GDD-TARGET · not built]`, pointer to GDD §0.4 + §9.

### 6.4 `neon-game-systems`
**Purpose:** systems catalog, built vs target.
Sections:
- **System catalog table** — all major systems + status tag.
- **Per-system (BUILT):** `EntitiesService` + `EntitiesQueries` (registry/queries); `SpawnerService` + `LevelConfigurationAsset` / `EnemyWaveDefinition` / `EnemySpawnEntry` (progression spawning); `Level` controller + scope; `HealthSystem`; Camera (`CameraFollow`, `CameraShake`, `ParalaxScrolling`); Items/pickups; UI/HUD (`UIManager`, `UIHUDHealthBar`, …); Scenes (`ScenesService`, `SceneDefinitionAsset`).
- **GDD target systems — NOT built yet (`[GDD-TARGET · not built]`):** auto-engage, Finish-Ready state, Momentum meter, roguelite Protocols + XP/Neon-Charge/Overcharge economy, the Signal meta-difficulty. Each: one-line intent + GDD section pointer.

### 6.5 `neon-recipes`
**Purpose:** executable, step-by-step workflows.
Sections:
- **Add a service** — interface + implementation + Null variant + settings asset + DI registration + `Services`/injection access.
- **Add a Player/Enemy state** — subclass the unit state, wire transitions, hook `UnitActions`/animation.
- **Add a level** — create `LevelConfigurationAsset` (+ `EnemyWaveDefinition`s), a scene with a `Level` scope, a `SceneDefinitionAsset`; wire `_postBootstrapScene` for boot-into.
- **Play-test a level in-editor** — the DI-bootstrap flow (enable `_enableEditorBootstrap` on `BootstrapSettingsAsset`; boot chain; progression-based wave triggers; nudge player X to fire waves). *(from the `neon-responder-run-and-verify` memory)*
- **Add a ScriptableObject settings asset** — `BaseSettingsAsset` subclass + `[CreateAssetMenu]` + `Resources/Settings/`.
- **Feature flow / bugfix flow** — requirement → branch → implement per conventions → verify (runtime) → PR.
- **Code-review checklist** — conventions + DI + no-legacy + status-tag accuracy.

### 6.6 `neon-troubleshooting`
**Purpose:** known traps, Symptom → Cause → Fix.
Sections (seeded from the `neon-responder-run-and-verify` memory + codebase):
- **DI / bootstrap** — "Play in a level scene fails to resolve `IEntitiesService`" → level scope has no parent app scope → enable editor bootstrap.
- **Spawning** — the **`AI_Active` spawn gap**: `SpawnerService` spawns + injects + registers enemies and they acquire the target, but never sets `EnemyBehaviour.AI_Active = true`, so enemies spot the player but never attack → one-line fix in `SpawnerService.SpawnUnit`.
- **Scene migration status** — only `03_Level1` migrated; `04`/`05` are legacy (no DI scope, `[Inject]` never populates) → don't rebuild; GDD abandons them.
- **Editor bootstrap flakiness** — drops out of Play mode when external file changes (git ops / asset writes) trigger a Unity refresh mid-session.
- **Debugging workflows** — runtime-first (per user working style): reproduce in-editor before declaring a fix.

## 7. CLAUDE.md contents

- **Identity** — one-liner + genre-fusion summary.
- **Design ground-truth** — Notion GDD link + the "GDD = design truth, skills = implementation truth" rule.
- **Non-negotiable rules** — DI-bootstrap to run; combat design stays engine-agnostic; no singletons / no `WaveManager` / no direct scene-Play; single-player (no networking); most GDD systems are not yet built (check status tags).
- **Assembly / namespace map** — quick reference.
- **Skill index** — "where to look": one line per skill with when to read it.
- **Status-tag legend** — `[BUILT]` / `[IN-MIGRATION → DOTS]` / `[GDD-TARGET · not built]`.

## 8. Content sources & accuracy

- **Codebase** (master `c27e365`): `Assets/_neon/Scripts/**`, `*.asmdef`.
- **Memory:** `neon-responder-combat-architecture`, `neon-responder-gdd`, `neon-responder-run-and-verify`.
- **GDD:** the Notion document (design intent only).
- **Rule:** every non-GDD claim is verified against code and cites a file; GDD-only systems are tagged `[GDD-TARGET]` and never presented as existing code.

## 9. Decisions log

| Decision | Choice |
|---|---|
| Deliverable shape | Skills + CLAUDE.md only (no commands/agents/plugin) |
| Overall approach | B — Neon-tuned 6-skill set (combat as its own skill) |
| DOTS/combat stance | Current master (DOTS merged) is canonical; combat design engine-agnostic; migration tagged |
| Consumers | Claude Code only |
| Skill prefix | `neon-` |
| CLAUDE.md location | Repo root |
| Networking skill | Dropped (single-player) |
| Branch | `claude/ai-scaffolding` off master |

## 10. Out-of-band note (not part of the scaffolding)

The repo contains a stray `dev/null/` directory (git hook files `post-checkout`, `post-commit`, `post-merge`,
`pre-push`) — an artifact of a script redirecting to `/dev/null` on Windows, which created literal files.
Flagged for cleanup separately; not addressed by this spec.
