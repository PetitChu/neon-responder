---
name: neon-recipes
description: "Neon Responder recipes — step-by-step workflows for adding a service, a unit state, a level, a settings asset, and play-testing a level in-editor. Use when implementing features or following established workflows in Neon Responder."
---

# Neon Responder — Recipes

Executable workflows for the common jobs in **Neon Responder: Night Shift** (PC/Steam 2D side-scroll beat-'em-up, Unity 6, single-player — **NOT VR**, don't apply Quest/VR framing). All paths are relative to the repo root `G:/Brainless Labs/neon-responder`. Code namespace is `BrainlessLabs.Neon` (lifecycle types in `BrainlessLabs.Neon.Lifecycle`).

Conventions in one line: DI = **VContainer**, async = **UniTask** (`Cysharp.Threading.Tasks`), settings are `BaseSettingsAsset<TAsset,TSettings>` singletons under `Assets/Resources/Settings/`. **Runtime is ground truth** — verify features by playing, not by reading (Recipe 4).

---

## 1. Add a service

Services follow `IFoo` + `FooService` + (optional) `NullFooService` + a settings asset, registered in the boot FSM. Template quartet: `Assets/_neon/Scripts/Audio/{IAudioService,AudioService,AudioSettingsAsset}.cs`.

1. **Interface** — `Assets/_neon/Scripts/Foo/IFooService.cs`, namespace `BrainlessLabs.Neon`. Declare only what callers need.
2. **Implementation** — `Assets/_neon/Scripts/Foo/FooService.cs` implementing `IFooService`. Add `System.IDisposable` if it owns GameObjects/streams (see `AudioService.Dispose`). Read settings in the ctor via `FooSettingsAsset.InstanceAsset.Settings` (matches `AudioService` line 16).
3. **Null variant (only if the service can be absent)** — `NullFooService : IFooService`, no-op body. Lifecycle infra services do this (`Assets/_neon/Scripts/Lifecycle/Services/Platform/NullSteamService.cs`); gameplay services like `AudioService` have **no** Null variant — don't add one unless a platform/editor path needs it.
4. **Settings asset (only if configurable)** — see Recipe 5.
5. **Register in the boot FSM** — edit `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameServicesState.cs`. Add a `RegisterFooService` helper mirroring the existing ones and call it from `RegisterTypes`:
   ```csharp
   private static void RegisterFooService(IContainerBuilder builder)
   {
       builder.Register<FooService>(Lifetime.Singleton).As<IFooService>();
   }
   ```
   `RegisterTypes` already calls `RegisterAudioService/InputService/ScenesService/EntitiesService` — add your call there. **Do NOT register in `ApplicationLifetimeScope`** (its remark says: add registrations to state child scopes, not the root — it only holds FSM infra + entry states).
6. **Consume it.**
   - **MonoBehaviour in a level scene:** field-inject — `[Inject] private IFooService _foo;` (see `PlayAudioOnStart.cs`, `Projectile.cs`, `EnemyBehaviour.cs`). The `Level` scope injects every scene root in its build callback (`Level.Configure` → `container.InjectGameObject`).
   - **From a `LifetimeScope` / state:** `Container.Resolve<IFooService>()` (see `Level.Start` resolving `IEntitiesService`).
   - **From a unit state (`UnitState`):** reach services through the `unit` (`UnitActions`) properties — `unit.InputService`, `unit.AudioService`, `unit.Entities` (the `[Inject]` fields live on `UnitActions`). There is **no** global static `Services` locator — everything flows through the container.

---

## 2. Add a Player / Enemy state

Combat is a MonoBehaviour FSM: `UnitStateMachine : UnitActions` runs the current `UnitState`. Template: `Assets/_neon/Scripts/Units/PlayerStates/{PlayerIdle,PlayerGroundPunch}.cs`. Base class: `Assets/_neon/Scripts/StateMachine/UnitState.cs`.

1. **Create the state** — `Assets/_neon/Scripts/Units/PlayerStates/PlayerFoo.cs` (or `Units/EnemyStates/…`), namespace `BrainlessLabs.Neon`, `public class PlayerFoo : UnitState`.
2. **Override the lifecycle methods you need** (exact names from `UnitState`): `Enter()`, `Exit()`, `Update()`, `LateUpdate()`, `FixedUpdate()`. Also available: `virtual bool canGrab`, and `float stateStartTime` (set for you on entry). The active unit is the field `unit` (type `UnitActions`).
3. **Hook animation / actions via `unit`** — e.g. `unit.animator.Play(animationName)`, `unit.StopMoving()`, `unit.CheckForHit(attackData)`, `unit.GetAnimDuration(name)`. Attack data comes from `unit.settings` (e.g. `unit.settings.groundPunch` in `PlayerGroundPunch`).
4. **Read input via `unit.InputService`** (players) — e.g. `unit.InputService.PunchKeyDown(playerId)` where `playerId => unit.settings.playerId`.
5. **Wire transitions** — you transition by calling `unit.UnitStateMachine.SetState(new NextState())` from inside `Update()` (this exits the old state and enters the new). To reach your new state, add a branch in the state(s) that precede it — most player transitions originate in `PlayerIdle.Update()`. Time-boxed states self-exit back to idle: `if (Time.time - stateStartTime > animDuration) unit.UnitStateMachine.SetState(new PlayerIdle());` (see `PlayerGroundPunch`).
6. **Entry states are fixed** — `UnitStateMachine.Start()` sets `PlayerIdle` for players / `EnemyIdle` for enemies. New states are reached through transitions, not by changing `Start`.

---

## 3. Add a level

A level = a `LevelConfigurationAsset` (spawn rules) + a scene whose root carries a `Level : LifetimeScope` + a `SceneDefinitionAsset` naming that scene. Only `03_Level1` uses this system; `04_Level2`/`05_Level3` are **legacy `WaveManager`** — do not extend them (see Code-review checklist).

1. **Level config** — Create → **Neon/Level/Level Configuration** (`LevelConfigurationAsset`, `Assets/_neon/Scripts/Spawner/LevelConfigurationAsset.cs`). Set `DefaultPlayerDefinition` (a `UnitDefinitionAsset`), `PlayerSpawnProgression` (0–1), `PlayerSpawnDirection`.
2. **Author waves** — fill `Waves` (list of `EnemyWaveDefinition`). Per wave: `TriggerType` (`PreviousWaveCompleted` / `ProgressionPercent` / `DistanceFromStart` / `Manual`), the matching trigger value (e.g. `TriggerProgressionPercent`), `Entries` (list of `EnemySpawnEntry`: `UnitDefinition`, `Count`, `SpawnInterval`), plus `MaxActiveEnemies`, `CooldownBetweenSpawns`, `SpawnPositionMode`. For a first playable wave, use `ProgressionPercent` at a low value (~0.05) so it fires shortly after spawn.
3. **Create the scene** — new scene under `Assets/_neon/Scenes/` (Unity 6: keep a Camera + Directional Light). Add an empty GameObject, attach the **`Level`** component (`Assets/_neon/Scripts/Level/Level.cs`), assign your `LevelConfigurationAsset` to its `_configuration` field, and set `_levelStartX` / `_levelLength` to span the playable belt. `Level` is a `LifetimeScope`: at `Start` it resolves `IEntitiesService`, builds a `SpawnerService`, spawns the player, and starts waves.
4. **Scene definition** — Create → **Neon/Scenes/Scene Definition** (`SceneDefinitionAsset`, alongside existing ones in `Assets/_neon/Scenes/Configs/`, e.g. `SceneDefinition_Level1`). Set `_sceneReference` to the new scene, `_sceneName`, and `_sceneType = Level`.
5. **Boot into it** — point `_postBootstrapScene` at this `SceneDefinitionAsset` (see Recipe 4). `GameState` loads `PostBootstrapScene` via `IScenesService.LoadSceneAsync` once boot completes.
6. **Verify at runtime** (Recipe 4) — never assume waves fire; walk the player to the trigger % and watch the console (`[Level] Wave 1/N started.`).

---

## 4. Play-test a level in-editor (authoritative flow)

Pressing Play directly in a level scene **fails** — `Level.Start` can't resolve `IEntitiesService` because services are only registered during the boot FSM (`GameServicesState`). You must boot first.

1. Open the **BootstrapSettingsAsset** at `Assets/Resources/Settings/BootstrapSettingsAsset.asset` (inspector renders a custom UI from `BootstrapSettings.Editor_OnGUI`).
2. Tick **Enable Editor Bootstrap** (serialized field `_enableEditorBootstrap`). This makes `EditorBootstrap` (`[InitializeOnLoadMethod]`, `Assets/_neon/Scripts/Editor/Bootstrap/EditorBootstrap.cs`) call `EditorBootstrapUtilities.SetupInternalBootstrap()`, which sets `EditorSceneManager.playModeStartScene` so **Play always boots from the bootstrap scene first**.
3. Confirm **Bootstrap Scene** (`_bootstrapScene`) points at the boot scene (may be an empty scene) and that **Execute Bootstrap Sequence** (`_executeBootstrapSequence`) is on — `ApplicationLifetimeScope.Configure` early-returns if `ExecuteBootstrapSequence` is false.
4. Set **Post-Bootstrap Scene** (`_postBootstrapScene`) to the target level's `SceneDefinitionAsset` (e.g. `SceneDefinition_Level1` → `03_Level1`).
5. Press **Play**. Boot runs `InitialState → PlatformState → UnityServicesState → GameServicesState → GameState`; on entering `GameState`, it loads `_postBootstrapScene` (`[Lifecycle] Loading post-bootstrap scene: …` in console).
6. **Trigger waves** — waves are progression-based. Level1 wave 0 fires at ~5% progression. Walk right, or in a headless/scripted check nudge the player's `transform.x` forward past `Level.ProgressionToWorldX(triggerPercent)`. Watch for `[Level] Wave 1/N started.`
7. **Gotcha:** editor bootstrap flakily drops out of Play mode when external file changes (git ops, asset writes) make Unity refresh mid-session. Don't run git/asset writes while play-testing.

---

## 5. Add a ScriptableObject settings asset

Settings are `BaseSettingsAsset<TAsset,TSettings>` singletons resolved from `Resources/Settings/<AssetTypeName>` (path is auto-derived from the asset type name — see `BaseSettingsAsset.s_resourcesAssetPath`). Templates: `AudioSettingsAsset.cs`, `BootstrapSettingsAsset.cs`.

1. **Settings data** — `Assets/_neon/Scripts/Foo/FooSettings.cs`: `[System.Serializable] public class FooSettings : ISettings { … }` with your `[SerializeField]` fields (mirror `BootstrapSettings`).
2. **Asset wrapper** — `Assets/_neon/Scripts/Foo/FooSettingsAsset.cs`:
   ```csharp
   namespace BrainlessLabs.Neon
   {
       public class FooSettingsAsset : BaseSettingsAsset<FooSettingsAsset, FooSettings> { }
   }
   ```
   (One-liner, exactly like `AudioSettingsAsset` / `BootstrapSettingsAsset`.) The base already provides `InstanceAsset`, `Settings`, editor `GetOrCreateSettingsAsset()`, and auto-reload — **do not** add a `[CreateAssetMenu]`; the asset is created/loaded at `Assets/Resources/Settings/FooSettingsAsset.asset` on first access.
3. **Create the `.asset`** — enter Play once (or call `FooSettingsAsset.GetOrCreateSettingsAsset()` in the editor); the base auto-creates it under `Assets/Resources/Settings/`. Fill fields in the inspector.
4. **Read it** — `var s = FooSettingsAsset.InstanceAsset.Settings;` (ctor-time in a service, as `AudioService` does).

> Note: `[CreateAssetMenu]` **is** the right tool for plain `ScriptableObject` config assets that are NOT settings singletons — e.g. `LevelConfigurationAsset` (`menuName = "Neon/Level/Level Configuration"`) and `SceneDefinitionAsset` (`menuName = "Neon/Scenes/Scene Definition"`). Use `BaseSettingsAsset` only for global singletons.

---

## 6. Feature / bugfix flow

1. **Requirement** — restate the goal in one sentence; if it's flagged critical, surface directional forks as questions before coding (don't guess).
2. **Branch** — `git checkout -b claude/<short-desc>` off `master`. Don't reuse a prior *failed-attempt* branch without asking.
3. **Implement per conventions** — follow the neon patterns above (service quartet + DI registration, `UnitState` transitions, `BaseSettingsAsset`, VContainer/UniTask). Keep scope tight: *prototype, don't go too far into features* (studio norm for this jam-scoped repo).
4. **Verify AT RUNTIME** — runtime is ground truth here. Play-test via Recipe 4 (boot → target level → trigger the affected behavior). For anything spawn/wave/AI-related, confirm in the running game and console, not by reading code. Front-load a concrete run the user can repeat.
5. **PR** — open a PR to `master` summarizing the change + how it was runtime-verified. Push before piling up local commits (studio push floor: prompt to push at ~25 commits ahead).

---

## 7. Code-review checklist

- **Conventions:** new service is `IFoo`+`FooService`(+`NullFooService` only if a fallback path exists); settings use `BaseSettingsAsset` (no `[CreateAssetMenu]` on those); async uses UniTask; namespace `BrainlessLabs.Neon`(`.Lifecycle`).
- **DI correctness:** services registered in `GameServicesState.RegisterTypes` (via a `Register…` helper), **not** in `ApplicationLifetimeScope`. Consumers use `[Inject]` (scene MonoBehaviours), `Container.Resolve<T>()` (scopes/states), or `unit.<Service>` properties (unit states) — never a hand-rolled static locator. No `new FooService()` outside registration/where the container can't reach (e.g. `Level` legitimately `new`s `SpawnerService` with `Container`).
- **State machine:** transitions go through `unit.UnitStateMachine.SetState(...)`; overridden methods match `UnitState` (`Enter/Exit/Update/LateUpdate/FixedUpdate`); time-boxed states return to `PlayerIdle`.
- **No legacy:** reject new use of `WaveManager`, or edits that rebuild `04_Level2`/`05_Level3` scene-placed waves into config assets — that multi-level template structure is abandoned (GDD targets one belt arena + DOTS swarm). New levels use `Level` + `LevelConfigurationAsset` + `SpawnerService` only.
- **Status-tag accuracy:** claims of "works"/"fixed" must be backed by a runtime play-test (Recipe 4), not code reading. Watch for known gaps (e.g. spawned enemies spotting but not attacking if `EnemyBehaviour.AI_Active` is never set) before signing off.
