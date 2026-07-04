---
name: neon-architecture
description: "Neon Responder architecture — VContainer DI, ApplicationFSM (UnityHFSM) boot chain, LifetimeScope hierarchy, service roster, assembly map. Use when designing features, understanding game structure, or making architectural decisions in Neon Responder."
---

# Neon Responder — architecture

Neon Responder: Night Shift is a **PC (Steam) 2D side-scroll beat-'em-up**,
Unity 6, **single-player — NOT VR** (ignore any Quest/XR framing that leaks in
from the Notion space this lives under). Design ground truth is the GDD
(Notion "Neon_Responder_GDD"); this skill covers runtime structure only.

Status tags below: `[BUILT]` = in code now · `[IN-MIGRATION → DOTS]` = exists as
plain C# but authored to map onto DOTS/ECS later · `[GDD-TARGET · not built]` =
design intent only. Check the tag before assuming a system exists.

## 1. Core identity

| Concern | Choice |
|---|---|
| Engine | Unity 6 |
| Language | C# |
| Render | URP 17.3.0 + Unity 2D Pixel Perfect |
| Networking | **none** — single-player |
| DI | VContainer |
| Async | UniTask (`Cysharp.Threading.Tasks`) |
| Reactive | R3 |
| App FSM | UnityHFSM (application lifecycle only) |
| Platform | PC / Steam + Unity Services |

## 2. Main architectural pattern

**Service-oriented over VContainer DI, booted by a lifecycle FSM.** Behaviour is
split into small `IFoo` services (Audio, Input, Scenes, Entities, …) registered in
VContainer and resolved by injection. A UnityHFSM state machine (the
*application* FSM — distinct from the MonoBehaviour combat FSM) sequences boot,
each state owning a child DI scope where it registers its services. Gameplay
scenes get their own child scope via `Level : LifetimeScope`.

Combat is a separate **MonoBehaviour FSM** brawler (`UnitStateMachine`, `Player*`
states) — see the `neon-combat` skill; not covered here.

## 3. Boot / ApplicationFSM

`ApplicationLifetimeScope` (a `LifetimeScope`, `DontDestroyOnLoad`) registers
`ApplicationFSMRunner` (a VContainer `IInitializable`/`ITickable` entry point).
On `Initialize()` it builds the FSM via `ApplicationFSMBuilder` (UnityHFSM
`StateMachine`) and ticks `OnLogic()` each frame. Boot only runs if
`BootstrapSettingsAsset.InstanceAsset.Settings.ExecuteBootstrapSequence` is true.

State order (each is a `LifetimeStateMachine` that creates a child DI scope on
enter, disposes it on exit, and advances via a **health-checked** transition once
its services report healthy):

```
InitialState → PlatformState → UnityServicesState → GameServicesState → GameState
```

- **InitialState** — immediate pass-through (early config hook). Transitions to
  PlatformState unconditionally.
- **PlatformState** — registers `ISteamService` (`SteamService`, or
  `NullSteamService` in editor / when `STEAMWORKS_NET` is undefined) as an
  `IHealthCheckedService`.
- **UnityServicesState** — registers `IUnityServicesInitializer`
  (`UnityServicesInitializer`, or `NullUnityServicesInitializer` in editor).
- **GameServicesState** — registers the gameplay services as `Singleton`:
  `AudioService`, `InputService`, `ScenesService`, `EntitiesService`.
- **GameState** — final; on enter, its `RegisterTypes` build callback resolves
  `IScenesService` and loads `settings.PostBootstrapScene` (a
  `SceneDefinitionAsset`) via `LoadSceneAsync(...).Forget(...)`.

**Health-checked transitions** (`HealthCheckedServicesTransition`, factory-built
per state): combine every registered `IHealthCheckedService.HealthStream` (R3),
transition only when **all** report `ServiceStatus.Healthy` (with a 0.5s debounce
for stability). `ApplicationTransition` is the always-true base. States with no
health-checked services transition immediately.

Editor entry: `EditorBootstrap` (`[InitializeOnLoadMethod]`) reads
`BootstrapSettings.EnableEditorBootstrap` and sets/clears
`playModeStartScene` so pressing Play starts at the bootstrap scene rather than
the currently-open scene.

## 4. DI scopes

```
ApplicationLifetimeScope  (root, DontDestroyOnLoad)
 └─ per-FSM-state child scope   (created in LifetimeStateMachine.OnEnter, disposed OnExit)
 └─ Level : LifetimeScope       (one per loaded level scene)
```

`Level : LifetimeScope` inherits all app-level services through its **parent**
scope. The chain is wired by `ScenesService`: before each
`SceneManager.LoadSceneAsync(..., Single)` it calls
`LifetimeScope.EnqueueParent(_lifetimeScope)` so the incoming scene's
`LifetimeScope` (the `Level`) resolves against the app container. In
`Level.Configure` a build callback runs `container.InjectGameObject(root)` over
every scene root, so scene MonoBehaviours get their `[Inject]` fields. `Level`
itself resolves `IEntitiesService` from `Container` in `Start()` and constructs
the level-scoped `SpawnerService`.

## 5. Service access (no static accessor)

**There is no `Services` static accessor.** A static `Services.cs` singleton
existed but was **deleted** (commit "Remove singleton pattern from services…" —
the message reads oddly, but it *removed* the static class and moved everything to
DI). Resolve services one of two ways:

- **Non-MonoBehaviour** (services, FSM states): **constructor injection**.
- **Scene / spawned MonoBehaviours**: `[Inject] private IFoo _foo;` field
  injection — populated by `Level` (scene roots) or `SpawnerService`
  (`_container.InjectGameObject(instance)` on spawned units).

Never `new` a service, and never reintroduce a static/`Instance` accessor.

## 6. Service roster

| Service | Interface | Responsibility |
|---|---|---|
| Audio | `IAudioService` | Play SFX / music; SFX duration lookup. `[BUILT]` |
| Input | `IInputService` | Per-`playerId` button/stick queries (Input System). `[BUILT]` |
| Scenes | `IScenesService` | UniTask single-mode scene load; enqueues parent scope for DI inheritance. `[BUILT]` |
| Steam | `ISteamService` (+ `NullSteamService`) | Steam platform init; null-object off-Steam/editor. `[BUILT]` |
| Unity Services | `IUnityServicesInitializer` (+ `NullUnityServicesInitializer`) | Unity Gaming Services init; null-object in editor. `[BUILT]` |
| Entities | `IEntitiesService` (+ `EntitiesQueries`) | Central entity registry: register/unregister, integer IDs, type queries, GameObject lookup. Replaces the old static `EnemyManager`. `[IN-MIGRATION → DOTS]` |
| Spawner | `SpawnerService` (concrete, no interface) | Progression/wave spawning of players + enemies from `LevelConfigurationAsset`; level-scoped, owned by `Level`. `[IN-MIGRATION → DOTS]` |

`EntitiesQueries` are gameplay-facing **extension methods** on `IEntitiesService`
(attacker count, disable-all-AI, nearby-downed, etc.) — kept off the interface so
the registry stays a thin DOTS-facing surface. Both Entities and Spawner are plain
C# today but deliberately DOTS-shaped (integer IDs, type buckets) for a parallel
ECS refactor; keep gameplay/design engine-agnostic.

## 7. Assembly map

Three asmdefs under `Assets/_neon/Scripts/`; asmdef name == `rootNamespace`.

| asmdef | Scope | References |
|---|---|---|
| `BrainlessLabs.Neon` | Runtime core: services, gameplay, settings, entities | Eflatun.SceneReference, UniTask, Unity.2D.PixelPerfect, Unity.InputSystem, VContainer |
| `BrainlessLabs.Neon.Lifecycle` | App boot/FSM + boot services (Steam, Unity Services, health) | **`BrainlessLabs.Neon`** + R3.Unity, UnityHFSM, Unity.Services.Core (`STEAMWORKS_NET` versionDefine) |
| `BrainlessLabs.Neon.Editor` | Editor-only: bootstrap, drawers, custom inspectors, project settings | `BrainlessLabs.Neon` (+ a Unity built-in) |

Reference direction: **`Lifecycle → Neon`** and **`Editor → Neon`**. Runtime never
references Editor; `Neon` never references `Lifecycle`.

## 8. Critical rules

- **Must DI-bootstrap to run.** Pressing **Play directly in a level scene** breaks
  DI — `Level.Start` can't resolve `IEntitiesService` (no parent app scope). Boot
  through the ApplicationFSM: enable editor bootstrap in `BootstrapSettingsAsset`.
  See the `neon-troubleshooting` and `neon-recipes` skills.
- **Resolve via DI only.** Constructor injection (non-Mono) or `[Inject]` fields
  (Mono). No `new FooService()`, no static/`Instance` accessors — the static
  `Services` accessor was removed and must not return.
- **Register services in an FSM state**, not one god-scope
  (`GameServicesState` for gameplay services; platform services in their states).
  See naming/registration detail in the `neon-conventions` skill.
- **No networking.** Single-player; do not add multiplayer assumptions.
- **Check the status tag** before assuming a GDD system exists in code —
  `[GDD-TARGET · not built]` is design intent, and Entities/Spawner are
  `[IN-MIGRATION → DOTS]` (plain C# now, ECS later).
