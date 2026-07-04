---
name: neon-conventions
description: "Neon Responder conventions — folder layout, assembly/namespace naming, the service + Null + settings-asset pattern, naming rules, prohibited patterns. Use when writing code or reviewing for consistency in Neon Responder."
---

# Neon Responder — conventions

Neon Responder: Night Shift is a PC (Steam) 2D side-scroll beat-'em-up, Unity 6,
single-player (NOT VR — ignore any Quest/XR framing). Stack: VContainer (DI),
UniTask (async), UnityHFSM + a custom `LifetimeStateMachine` (app lifecycle),
R3 (reactive), Eflatun.SceneReference, Input System, 2D Pixel Perfect, URP.

Two code eras coexist: **newer service/lifecycle code** (the canonical style below)
and **older gameplay MonoBehaviours** (e.g. `CameraFollow`, `HealthSystem`) that use
public fields, tabs, and no underscores. Write all new code in the canonical style;
don't mimic the legacy style just because a nearby file uses it.

## 1. Folder structure

All game code lives under `Assets/_neon/`. Scripts under `Assets/_neon/Scripts/`:

```
Assets/_neon/
├── Animation/  Audio/  Fonts/  Level/  Materials/
├── Prefabs/    Scenes/ Shaders/ Sprites/ Units/
└── Scripts/
    ├── Attributes/     # custom PropertyAttributes (ShowIf, ReadOnly, Help)
    ├── Audio/          # IAudioService quartet + AudioConfigurationAsset
    ├── Bootstrap/      # BootstrapSettings quartet (boot toggles)
    ├── Camera/         # CameraFollow, CameraShake, ParalaxScrolling (legacy style)
    ├── Editor/         # BrainlessLabs.Neon.Editor asmdef (Editor-only)
    │   └── Drawers/    # property drawers for the custom attributes
    ├── Entities/       # IEntitiesService (DOTS-friendly registry, replaces EnemyManager)
    ├── Input/          # IInputService + generated PlayerControls
    ├── Items/          # pickups, projectiles
    ├── Level/          # Level : LifetimeScope (per-level DI scope)
    ├── Lifecycle/      # BrainlessLabs.Neon.Lifecycle asmdef (app FSM + boot services)
    ├── Objects/        # interactive scene objects
    ├── Scenes/         # IScenesService quartet (UniTask scene loading)
    ├── Settings/       # BaseSettingsAsset / ISettings / ISettingsAsset
    ├── Spawner/        # SpawnerService + LevelConfigurationAsset
    ├── StateMachine/   # combat/unit FSM base
    ├── Tools/          # dev tooling
    ├── UI/             # UIButton, UIHandPointer, etc.
    ├── Units/          # brawler: Player*/Enemy* states, HealthSystem, UnitActions
    └── WaveManager/    # LEGACY — [Obsolete], being retired (see §9)
```

Settings assets live at runtime under `Assets/Resources/Settings/` (see §6).

## 2. Assembly definitions

Three asmdefs under `Assets/_neon/Scripts/`; asmdef name == `rootNamespace`.

| asmdef | Location | Platforms | Notes |
|---|---|---|---|
| `BrainlessLabs.Neon` | `Scripts/BrainlessLabs.Neon.asmdef` | all | Runtime core. Refs: Eflatun.SceneReference, UniTask, Unity.2D.PixelPerfect, Unity.InputSystem, VContainer. |
| `BrainlessLabs.Neon.Lifecycle` | `Scripts/Lifecycle/…Lifecycle.asmdef` | all | App boot/FSM + boot services. **References `BrainlessLabs.Neon`** (runtime). Adds R3.Unity, UnityHFSM, Unity.Services.Core; `STEAMWORKS_NET` versionDefine. |
| `BrainlessLabs.Neon.Editor` | `Scripts/Editor/…Editor.asmdef` | **Editor only** | Property drawers, custom editors, project settings. |

Reference direction: `Lifecycle → Neon` (runtime). Never reference Editor from runtime.

## 3. Namespace map

Namespace matches the owning asmdef's `rootNamespace` — folder depth does NOT add
sub-namespaces (e.g. `Scripts/Audio`, `Scripts/Units`, `Scripts/Scenes` are all
`BrainlessLabs.Neon`).

- `BrainlessLabs.Neon` — all runtime code: services, settings, gameplay, attributes.
- `BrainlessLabs.Neon.Lifecycle` — application FSM, lifetime scopes, boot services
  (`SteamService`, `UnityServicesInitializer`), health-check plumbing.
- `BrainlessLabs.Neon.Editor` — Editor-only drawers, custom inspectors, project settings.

## 4. Naming conventions

Confirmed from canonical (newer) code. Apply these to all new code.

| Element | Style | Example |
|---|---|---|
| Private field | `_camelCase` | `private readonly AudioConfigurationAsset _sfxConfiguration;` |
| Injected field | `[Inject] private IFoo _foo;` | `[Inject] private IAudioService _audioService;` |
| Serialized private field | `[SerializeField] private camelCase` | `[SerializeField] private float borderMargin;` |
| Public property | `PascalCase` | `public SceneDefinitionAsset CurrentScene { get; private set; }` |
| Method | `PascalCase` | `LoadSceneAsync`, `PlaySFX` |
| Interface | `I` + `PascalCase` | `IAudioService`, `IScenesService` |
| Service impl | `FooService` | `AudioService`, `ScenesService` |
| Null-object impl | `NullFooService` | `NullSteamService` |
| Settings asset | `FooSettingsAsset` | `AudioSettingsAsset` |
| ScriptableObject asset | `FooDefinitionAsset` / `FooConfigurationAsset` | `SceneDefinitionAsset`, `AudioConfigurationAsset` |
| Static field | `s_camelCase` | `private static readonly string s_resourcesAssetPath;` |
| Braces | Allman (own line), 4-space indent | — |

Note: legacy gameplay files (`CameraFollow`, `HealthSystem`) use public fields and
tabs — do not treat them as the standard.

## 5. The Service pattern

Quartet: interface `IFoo` + `FooService` + (often) `NullFooService` + `FooSettingsAsset`,
all in the same folder (e.g. `Scripts/Audio/`). Boot/platform services live in
`Scripts/Lifecycle/Services/…` under `BrainlessLabs.Neon.Lifecycle`.

- **Interface** — small, e.g. `IAudioService { void PlaySFX(...); void PlayMusic(...); }`.
- **Impl** — `public class AudioService : IAudioService, System.IDisposable`; pulls
  config from its settings asset in the ctor:
  `var settings = AudioSettingsAsset.InstanceAsset.Settings;`.
- **Null-object** — `NullSteamService : ISteamService` for editor/non-Steam builds;
  returns benign defaults. Choose it at registration via `#if`.

**DI registration** (VContainer) happens in FSM states, NOT in one god-scope.
Gameplay services register in `GameServicesState.RegisterTypes`:

```csharp
builder.Register<AudioService>(Lifetime.Singleton).As<IAudioService>();
```

Platform services pick the Null impl by compile symbol:

```csharp
#if UNITY_EDITOR || !STEAMWORKS_NET
    builder.RegisterEntryPoint<NullSteamService>().As<ISteamService>().As<IHealthCheckedService>();
#else
    builder.RegisterEntryPoint<SteamService>().As<ISteamService>().As<IHealthCheckedService>();
#endif
```

**Consumers get services two ways:**
- Non-MonoBehaviour (services, states): **constructor injection**.
- Scene MonoBehaviours: `[Inject] private IFoo _foo;` field injection. The owning
  `Level : LifetimeScope` injects them in a build callback
  (`container.InjectGameObject(root)` over scene roots). Do not `new` a service or
  resolve via a static.

## 6. Settings-asset pattern

Config is a ScriptableObject singleton loaded from `Resources`.

- `FooSettings : IFooSettings, ISettings` — a `[Serializable]` plain class holding the
  fields (`[SerializeField] private …` + public accessors). `ISettings` requires an
  `Editor_OnGUI(Object target)` hook (Editor-only) for custom settings UI.
- `FooSettingsAsset : BaseSettingsAsset<FooSettingsAsset, FooSettings>` — usually an
  empty body; the base (`Scripts/Settings/BaseSettingsAsset.cs`) does the work.
- **Access:** `FooSettingsAsset.InstanceAsset.Settings` — the base loads
  `Resources.Load("Settings/FooSettingsAsset")` and caches/reloads it.
- **Location:** the `.asset` lives at `Assets/Resources/Settings/FooSettingsAsset.asset`
  (e.g. `AudioSettingsAsset.asset`, `BootstrapSettingsAsset.asset`, `ScenesSettingsAsset.asset`).
- Content ScriptableObjects (not settings singletons) use
  `[CreateAssetMenu(menuName = "Neon/…")]`, e.g.
  `[CreateAssetMenu(fileName = "AudioConfiguration", menuName = "Neon/Audio/Audio Configuration")]`.
  Settings-asset classes themselves have no `CreateAssetMenu` (created via base/editor).

## 7. `[SerializeField]` + custom attributes

Expose inspector fields as `[SerializeField] private` (canonical) — not public fields.
Custom attributes live in `Scripts/Attributes/`; drawers in `Scripts/Editor/Drawers/`.

- **`[ShowIf(conditionField, compareValues…)]`** — conditionally shows a field when a
  sibling field matches; omit values to compare against `true`.
  `[ShowIf("HasCameraBound")]` or `[ShowIf("TriggerType", WaveTriggerType.Percent)]`.
- **`[ReadOnly]`** — displays a field but blocks editing. NOTE: the class is named
  **`ReadOnlyProperty`** (usage: `[ReadOnlyProperty] public int foo;`), not `ReadOnly`.
- **`[Help("…")]`** — draws a yellow help label above a field: `[Help("Explains the field")]`.

## 8. Async

- Use **UniTask** (`Cysharp.Threading.Tasks`), not `Task`/coroutines, for new async.
  Async methods are `async UniTask` and named `…Async` (e.g. `LoadSceneAsync`).
- `await` Unity async ops directly: `await SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);`.
- **CancellationToken is not yet used anywhere in the codebase.** When adding genuinely
  cancellable async, thread a `CancellationToken` through per UniTask norms — but don't
  bolt tokens onto simple one-shot loads just for ceremony.

## 9. Prohibited patterns

| Don't | Do instead |
|---|---|
| Singletons / static service access (`Instance`, static managers) | Register the service in a VContainer FSM state (`GameServicesState`); inject via ctor or `[Inject]`. |
| `WaveManager` (marked `[Obsolete]`, being retired) | Use `Level` + `LevelConfigurationAsset` + `SpawnerService`. Only `03_Level1` is migrated; don't rebuild `04`/`05`'s legacy waves. |
| Static `EnemyManager` enemy tracking | Use `IEntitiesService` (DOTS-friendly registry). |
| Pressing **Play directly in a level scene** | Breaks DI — `Level.Start` can't resolve services (no parent app scope). Boot via `ApplicationFSM`: enable editor bootstrap in `BootstrapSettingsAsset` so Play starts at the bootstrap scene. |
| Inventing service/API names or config fields | Confirm the exact member exists in code before calling it. Match the quartet + settings-asset patterns above. |
| Public inspector fields (new code) | `[SerializeField] private` with an accessor if needed. |

Combat design is being recast as an engine-agnostic brawler-verb layer with a parallel
DOTS refactor underway — keep new work engine-agnostic. For combat/design intent, see
the GDD (Notion "Neon_Responder_GDD"); this skill covers code conventions only.
