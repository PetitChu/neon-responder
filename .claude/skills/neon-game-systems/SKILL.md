---
name: neon-game-systems
description: "Neon Responder game systems — Entities/Spawner/Level, Health, Scenes, Camera, UI (built), plus the GDD-target systems (auto-engage, Finish-Ready, Momentum, Protocols, Signal) not yet built. Use when working on gameplay systems or checking what exists vs planned in Neon Responder."
---

# Neon Responder — Game Systems

**Neon Responder: Night Shift** — PC (Steam) 2D side-scroll beat-'em-up, Unity 6, single-player (NOT VR).
All gameplay code lives under `Assets/_neon/Scripts/` in namespace `BrainlessLabs.Neon`.

This skill separates **what is in code today** from the **GDD design target**. The headline GDD
combat/progression systems are design intent only — do not present them as existing or wire against them.

- Design ground-truth (GDD): https://app.notion.com/p/Neon_Responder_GDD-3935c0654b6b80beaa94f61de4bbf47d
  (Do not fetch here; use the `neon-responder-gdd` memory note for target-system intent.)

---

## 1. System catalog

| System | Where | Status |
| --- | --- | --- |
| Entities registry (`IEntitiesService` / `EntitiesService`) | `Scripts/Entities/` | `[BUILT]` |
| Entity gameplay queries (`EntitiesQueries`) | `Scripts/Entities/` | `[BUILT]` |
| Spawner + wave progression (`SpawnerService`, `LevelConfigurationAsset`, `EnemyWaveDefinition`, `EnemySpawnEntry`) | `Scripts/Spawner/` | `[BUILT]` |
| Level controller + per-level DI scope (`Level`, `LevelBound`, `Surface`) | `Scripts/Level/` | `[BUILT]` |
| Health (`HealthSystem`) | `Scripts/Units/HealthSystem.cs` | `[BUILT]` |
| Unit definition / settings (`UnitDefinitionAsset`, `UnitSettings`, `UNITTYPE`) | `Scripts/Units/` | `[BUILT]` |
| Scenes (`ScenesService`, `SceneDefinitionAsset`, `SceneType`, settings) | `Scripts/Scenes/` | `[BUILT]` |
| Camera (`CameraFollow`, `CameraShake`, `ParalaxScrolling`) | `Scripts/Camera/` | `[BUILT]` |
| Items / pickups (`Item`, `HealthPickup`, `WeaponPickup`, `Projectile`) | `Scripts/Items/` | `[BUILT]` |
| UI / HUD (`UIManager`, `UIHUDHealthBar`, `UIButton`, `UILevelSelection`, `LevelProgress`) | `Scripts/UI/` | `[BUILT]` |
| Combat verbs (punch/kick/grab/throw/jump/weapon) as FSM states | `Scripts/Units/PlayerStates/`, `EnemyStates/` | `[BUILT]` (MonoBehaviour FSM) |
| `WaveManager` (legacy, `[Obsolete]`) | `Scripts/WaveManager/` | `[BUILT — deprecated]` |
| Auto-Engage | GDD §core loop | `[GDD-TARGET · not built]` |
| Finish-Ready state | GDD §core loop | `[GDD-TARGET · not built]` |
| Momentum meter (Cool→Warm→Hot→Overdrive) | GDD §Momentum | `[GDD-TARGET · not built]` |
| Roguelite Protocols + XP / Neon-Charge / Overcharge economy | GDD §Protocols | `[GDD-TARGET · not built]` |
| The Signal (dawn meta-difficulty) | GDD §Signal | `[GDD-TARGET · not built]` |

---

## 2. Built systems

### EntitiesService + EntitiesQueries `[BUILT]`
`Scripts/Entities/IEntitiesService.cs`, `EntitiesService.cs`, `EntitiesQueries.cs`

Central entity registry. **Replaced the old static `EnemyManager` tracking** — `HealthSystem`, `Level`,
and `WaveManager` no longer keep their own enemy lists (see the comment in `HealthSystem.cs` and the
inlined counts in `WaveManager`).

- **`TrackedEntity`** (struct): `int Id`, `UNITTYPE UnitType`, `GameObject GameObject`, `UnitDefinitionAsset Definition`. DOTS-friendly (integer IDs, type categorization).
- **`IEntitiesService`**: `Register(go, unitType, definition=null) → int id`, `Unregister(id)`, `GetAll()`, `GetByType(UNITTYPE)`, `GetCount(UNITTYPE)`, `GetFirstByType(UNITTYPE)`, `TryGetByGameObject(go, out entity)`, events `OnEntityRegistered` / `OnEntityUnregistered`.
- **`EntitiesService`** (impl): keeps three dictionaries (by id, by type, GameObject→id); auto-increments IDs from 1; pre-populates lists for `PLAYER`/`ENEMY`/`NPC`; `IDisposable`. Registering the same GameObject twice returns the existing id.
- **`EntitiesQueries`** (static extension methods on `IEntitiesService`, null-safe on receiver): `GetEnemyAttackerCount()`, `DisableAllEnemyAI()`, `GetNearbyDownedEnemy(pos, range)`, `AnyEnemyDetectedPlayer()`. These read combat state off the entity's `UnitStateMachine` / `EnemyBehaviour` / `HealthSystem` / `UnitActions` components — kept off the interface so the service stays a thin DOTS-facing registry.
- **DI**: registered singleton in `Scripts/Lifecycle/ApplicationFSM/States/GameServicesState.cs` as `EntitiesService` → `IEntitiesService`.
- **Data flow**: `SpawnerService` registers spawned players/enemies; `Level.OnUnitDeath` unregisters dead enemies; `SpawnerService` listens to `OnEntityUnregistered` to count wave deaths.

### SpawnerService + spawn config `[BUILT]`
`Scripts/Spawner/SpawnerService.cs`, `LevelConfigurationAsset.cs`, `EnemyWaveDefinition.cs`, `EnemySpawnEntry.cs`

Level-scoped (plain C#, not a MonoBehaviour) service that spawns players and enemy waves. Constructed and
owned by `Level`, ticked each frame. Uses `VContainer.IObjectResolver.InjectGameObject` to inject
dependencies into spawned prefabs.

- **`SpawnerService`** ctor: `(IEntitiesService, LevelConfigurationAsset, Level, IObjectResolver)`. Subscribes to `IEntitiesService.OnEntityUnregistered`.
  - `SpawnPlayers()` — spawns `Config.DefaultPlayerDefinition` at `PlayerSpawnProgression` (currently single player; comment notes co-op later), sets `UnitSettings.playerId = 1`, registers as `PLAYER`.
  - `StartWaves()` → `AdvanceToNextWave()`; `Tick(dt)` drives trigger checks + per-frame enemy spawning; `TriggerWave(index)` for `Manual` waves.
  - Wave completes when all spawned enemies are dead (tracked via `_waveEntityIds` + `OnEntityUnregistered`), then auto-advances.
  - Events: `OnWaveStarted(int)`, `OnWaveCompleted(int)`, `OnAllWavesCompleted`, `OnPlayerSpawned(GameObject)`, `OnEnemySpawned(GameObject)`. Props: `CurrentWaveIndex`, `TotalWaves`, `AllWavesCompleted`, `WavesStarted`. `IDisposable`.
  - `SpawnUnit` applies `UnitDefinitionAsset` data onto the instance: `UnitSettings.unitName` (from `DisplayName`) and `HealthSystem.maxHp/currentHp` (from `MaxHealth`, when > 0).
- **`LevelConfigurationAsset`** (`ScriptableObject`, menu `Neon/Level/Level Configuration`): `DefaultPlayerDefinition`, `PlayerSpawnProgression` (0–1), `PlayerSpawnDirection`, `List<EnemyWaveDefinition> Waves`, `EndLevelWhenAllWavesCompleted`, `SlowMotionOnLastKill`, menu names `LevelCompletedMenu` / `AllLevelsCompletedMenu` / `GameOverMenu`.
- **`EnemyWaveDefinition`** (`[Serializable]`): `WaveName`; `WaveTriggerType` (`PreviousWaveCompleted`, `ProgressionPercent`, `DistanceFromStart`, `Manual`) + `TriggerProgressionPercent` / `TriggerDistance`; `List<EnemySpawnEntry> Entries`; `MaxActiveEnemies`, `CooldownBetweenSpawns`; `SpawnPositionMode` (`RelativeToPlayer`, `AtProgression`) + `SpawnDistanceFromPlayer` / `SpawnProgression` / `SpawnYRange`; camera bound `HasCameraBound` + `CameraBoundProgression`.
- **`EnemySpawnEntry`** (`[Serializable]`): `UnitDefinition`, `Count`, `SpawnInterval`.

### Level controller + per-level scope `[BUILT]`
`Scripts/Level/Level.cs`, `LevelBound.cs`, `Surface.cs`

- **`Level`** : `VContainer.Unity.LifetimeScope` — one per level scene. `Configure` injects all scene root GameObjects on build-callback, giving a scene-scoped DI container that inherits app services. In `Start`: resolves `IEntitiesService`, constructs `SpawnerService`, spawns player, starts waves. `Update` ticks the spawner. Subscribes to `HealthSystem.onUnitDeath` to unregister dead enemies, drive slow-mo on last kill, and open completion / game-over menus via `UIManager.ShowMenu(...)`.
  - **Geometry**: `_levelStartX`, `_levelLength`; `ProgressionToWorldX(0–1)` maps progression → world X (used by spawner + camera-bound logic). `SetCameraBoundFromProgression` creates a dynamic `LevelBound` and assigns it to `CameraFollow.levelBound`.
  - Serializes `LevelConfigurationAsset _configuration` + debug read-only wave fields; `OnDrawGizmos` visualizes bounds / progression markers / player spawn.
  - Marks completion into `LevelProgress.levelsCompleted` (see UI).
- **`LevelBound`** : editor gizmo marker; `CameraFollow` clamps the camera's right edge to it.
- **`Surface`** : footstep-SFX trigger marker; `OnValidate` forces its `Collider2D` to trigger + `Surface` layer.

### HealthSystem `[BUILT]`
`Scripts/Units/HealthSystem.cs` — MonoBehaviour on players, enemies, and destructible objects.

- Fields: `maxHp`, `currentHp`, `invulnerable`; derived `isDead` (hp==0), `healthPercentage`, `isPlayer`/`isEnemy` (by tag "Player"/"Enemy"). Health-bar + SFX + hit-flash + shake + effect settings.
- `SubstractHealth(int)` / `AddHealth(int)`; on hit plays SFX via injected `IAudioService`, hit-flash, shake. On reaching 0: fires `onUnitDeath(gameObject)` for players/enemies, else `Destroy`s the object.
- **Static events**: `onHealthChange(HealthSystem)` (HUD bars subscribe), `onUnitDeath(GameObject)` (`Level` / legacy `WaveManager` subscribe). Note: **no internal enemy list** — tracking moved to `EntitiesService`.

### Scenes `[BUILT]`
`Scripts/Scenes/` — `IScenesService`, `ScenesService`, `SceneDefinitionAsset`, `SceneType`, `IScenesSettings`, `ScenesSettings(Asset)`, `SceneReferenceExtensions`

- **`IScenesService` / `ScenesService`**: `CurrentScene` + `LoadSceneAsync(SceneDefinitionAsset)` / `LoadSceneAsync(string sceneName)` (UniTask). Loads single-mode; calls `LifetimeScope.EnqueueParent(scope)` first so the loaded scene's `LifetimeScope` inherits app services. Scene list comes from `ScenesSettingsAsset.InstanceAsset.Settings.SceneDefinitions`.
- **`SceneDefinitionAsset`** (`ScriptableObject`, menu `Neon/Scenes/Scene Definition`): `SceneReference` (Eflatun.SceneReference), `SceneName`, `SceneType`.
- **`SceneType`** enum: `Menu`, `Level`.
- **`ScenesSettingsAsset`** : `BaseSettingsAsset<...>` (project settings pattern); `IScenesSettings` exposes `SceneDefinitions`.
- **DI**: registered singleton in `GameServicesState.cs` as `ScenesService` → `IScenesService`.
- Note: in-game level flow also uses raw `SceneManager.LoadScene` from `UIButton` / `UILevelSelection`.

### Camera `[BUILT]`
`Scripts/Camera/`

- **`CameraFollow`**: follows tagged "Player" targets (center of all), damped X/Y, clamps to a view area, optional backtracking lock, keeps targets on screen, and clamps its right edge to `levelBound`. `additionalYOffset` driven by shake.
- **`CameraShake`** (`[RequireComponent(CameraFollow)]`): `ShowCamShake()` / `ShowCamShake(intensity, duration)` runs an `AnimationCurve` into `CameraFollow.additionalYOffset`. `DoCamShake.cs` is a small trigger helper.
- **`ParalaxScrolling`**: moves background layers by `ParallaxScale` relative to camera delta.

### Items / pickups `[BUILT]`
`Scripts/Items/`

- **`Item`** (base, extends `ObjectSorting`): `itemName`, `pickupSFX`, optional bounce animation; `virtual OnPickUpItem(GameObject target)`.
- **`HealthPickup`** : `OnPickUpItem` heals `target.HealthSystem.AddHealth(healthRecover)`, plays SFX via `IAudioService`, destroys itself.
- **`WeaponPickup`** : `OnPickUpItem` equips via `target`'s `WeaponAttachment.equipWeapon(this)`; carries `AttackData`, `timesToUse`, depletion type, attach sprite/offset.
- **`Projectile`** (`[RequireComponent(SpriteRenderer)]`): travels in `dir`, does sprite-bounds overlap vs "Enemy" tagged objects, applies `AttackData.damage` + optional knockdown, spawns hit effect, self-destructs after `timeToLive`.
- Pickups are collected via the player's grab flow (`Units/PlayerStates/PlayerGrabItem.cs` → `OnPickUpItem`), not trigger-collision.

### UI / HUD `[BUILT]`
`Scripts/UI/`

- **`UIManager`**: named-menu switcher — `ShowMenu(menuName)` disables all `UIMenu` entries then enables the match. `Level` calls this for level-completed / all-levels-completed / game-over.
- **`UIHUDHealthBar`**: subscribes to `HealthSystem.onHealthChange`; renders `PlayerHealthBar` / `EnemyHealthBar` / `BossHealthBar` (fill + name + portrait from `UnitSettings`).
- **`UIButton`**: gamepad/keyboard navigation via injected `IInputService`, click/select SFX via `IAudioService`, `LoadScene` / `ReloadCurrentScene` / `QuitApplication` helpers.
- **`UILevelSelection`** + static **`LevelProgress`** (`levelsCompleted`, `isLastLevel`): builds the level-select list, locks/unlocks by prior completion, loads via `SceneManager.LoadScene`. `LevelProgress` is the persisted-in-memory progression flag read by `Level`/`WaveManager`.
- Others: `UIFader`, `UIExitSign`, `UIHandPointer`, `UISetPlayerInactive`.

### Legacy: WaveManager `[BUILT — deprecated]`
`Scripts/WaveManager/WaveManager.cs` — marked `[System.Obsolete]`. Child-GameObject-based wave activation
kept for backward compatibility with old scenes. **For new levels use `Level` + `LevelConfigurationAsset` + `SpawnerService`.** It no longer depends on `EnemyManager` (enemy counts inlined via `FindObjectsByType<HealthSystem>`).

---

## 3. GDD target systems — NOT built `[GDD-TARGET · not built]`

These are the headline GDD systems. **They do NOT exist in code.** There are combat *verb* FSM states
today (punch/kick/grab/throw-enemy/jump-attack/weapon-attack/weapon-throw/pickup under
`Units/PlayerStates`), but the loop/economy/meta layers below are design intent only. Do not fabricate
classes, interfaces, or ScriptableObjects for them, and do not present them as existing.

- **Auto-Engage** `[GDD-TARGET · not built]` — enemies auto-close and the player auto-basic-attacks so play centers on the contextual verb. GDD §core loop.
- **Finish-Ready state** `[GDD-TARGET · not built]` — enemy enters a finishable window prompting the contextual "Act" verb (v0.4 dispatcher, replaced v0.3 Takedown QTE). GDD §core loop.
- **Momentum meter** `[GDD-TARGET · not built]` — Cool→Warm→Hot→Overdrive multiplier, raised only by finishing hits; scales fire rate / earn rate. GDD §Momentum.
- **Roguelite Protocols + economy** `[GDD-TARGET · not built]` — XP level-ups, Neon-Charge shop, Overcharge finisher (the run-growth stack). GDD §Protocols.
- **The Signal** `[GDD-TARGET · not built]` — dawn-countdown meta-difficulty pressure. GDD §Signal.

See the `neon-responder-gdd` and `neon-responder-combat-architecture` memory notes for intent; the DOTS/ECS
combat refactor runs in parallel and design is kept engine-agnostic.
