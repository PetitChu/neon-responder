---
name: neon-troubleshooting
description: "Neon Responder troubleshooting — DI-bootstrap failures, the AI_Active spawn gap, scene-migration status, and editor-bootstrap flakiness. Use when debugging errors or investigating known problems in Neon Responder."
---

# Neon Responder — Troubleshooting

Neon Responder: Night Shift is a **PC (Steam) 2D side-scroll beat-'em-up**, Unity 6, single-player, **not VR** — ignore any Quest/VR framing. Combat is a MonoBehaviour FSM brawler with a DOTS/entity refactor (`EntitiesService`, `SpawnerService`, `Level`) landing in parallel. Runtime behavior is ground truth here: reproduce in-editor before declaring a fix.

## Known issues

### 1. Pressing Play directly in a level scene fails (no DI parent scope)
- **Symptom:** Play a level scene (e.g. `03_Level1`) directly and it breaks — `Level.Start` logs `Could not resolve IEntitiesService from container`, and no player/enemies spawn.
- **Cause:** `Level : LifetimeScope` (`Assets/_neon/Scripts/Level/Level.cs`). In `Start()` it calls `Container.Resolve<IEntitiesService>()`, but services (`IEntitiesService`, audio, input) are only registered during the `ApplicationFSM` boot (`GameServicesState`). Started standalone, the `Level` scope has no parent app scope, so the resolve returns null and `Start` bails early.
- **Fix:** Always boot through the bootstrap flow. Enable editor bootstrap so Play boots first (see neon-recipes / neon-responder-run-and-verify play-test flow), then loads the level. Don't hit Play from inside a level scene.

### 2. The `AI_Active` spawn gap — enemies spot the player but never attack
- **Symptom:** Wave enemies spawn, are DI-injected, register in `EntitiesService`, and acquire the player (`EnemyBehaviour.targetSpotted` flips true), but they just stand there and never attack.
- **Cause:** `EnemyBehaviour.DoSomething()` (`Assets/_neon/Scripts/Units/EnemyBehaviour.cs`, line 40) begins with `if(!AI_Active) return;`. The `AI_Active` field defaults to `false`, and `SpawnerService.SpawnUnit` (`Assets/_neon/Scripts/Spawner/SpawnerService.cs`, ~line 335) instantiates the prefab, injects, sets direction / `UnitSettings` / `HealthSystem` — but **never sets `AI_Active = true`**. So the AI is spotted-but-dormant. (Confirmed against code — matches the run-and-verify memory exactly.)
- **Fix:** One-line set in `SpawnUnit` after injection, for enemy units — e.g. `instance.GetComponent<EnemyBehaviour>()?.SetActive(true)` / set `AI_Active = true`. Scope it to enemies (players don't have `EnemyBehaviour`). Reproduce first: spawn a wave, confirm `targetSpotted` true + no attacks, apply, confirm attacks.

### 3. Scene-migration status — only `03_Level1` works
- **Symptom:** `04_Level2` / `05_Level3` are effectively broken — enemies don't spawn/behave, `[Inject]` fields are null.
- **Cause:** Only `03_Level1` uses the new `Level` + `SpawnerService` + `LevelConfigurationAsset` system (a `LifetimeScope` that injects the scene). `04_Level2` / `05_Level3` still use the legacy `WaveManager` (`Assets/_neon/Scripts/WaveManager/WaveManager.cs`) and have **no DI scope**, so their `[Inject]` fields never populate.
- **Fix:** Do **not** rebuild 04/05 into the config-asset system. The GDD (one belt arena, DOTS swarm) has abandoned that multi-level small-wave structure — it's template legacy. Work against `03_Level1` for the migrated path; leave 04/05 alone unless explicitly asked.

### 4. Editor bootstrap drops out of Play mid-session
- **Symptom:** During a play-test, Unity exits Play mode unexpectedly / the boot flow stops taking effect.
- **Cause:** External file changes (git operations, asset writes) trigger a Unity asset refresh + domain reload mid-Play, which kicks the editor out of Play mode. `EditorBootstrap` hooks `[InitializeOnLoadMethod]` and sets `EditorSceneManager.playModeStartScene`, but a refresh mid-session disrupts the running session.
- **Fix:** Avoid external writes (git ops, file generation, asset writes) during an active play-test. If it drops, just re-enter Play once Unity has finished refreshing.

## Debugging workflow (runtime-first)

Runtime behavior is ground truth for this project — code reading and agent summaries can mislead. Before declaring any fix verified:

1. **Boot correctly.** Enable editor bootstrap via `BootstrapSettingsAsset` (`Assets/Resources/Settings/`), field `EnableEditorBootstrap` (serialized `_enableEditorBootstrap`, editor toggle "Enable Editor Bootstrap"). This sets `EditorSceneManager.playModeStartScene` to `_bootstrapScene` so Play always boots the `ApplicationFSM` first. To boot straight into a level, point `_postBootstrapScene` (a `SceneDefinitionAsset` in `Assets/_neon/Scenes/Configs/`, e.g. `SceneDefinition_Level1` → `03_Level1`) at that level.
2. **Reproduce the bug in-editor** and read the actual Console output — don't infer from source alone. Watch the `[SpawnerService]` / `[Level]` logs (wave start/complete, spawn counts) and the `Level` inspector debug fields (`_currentWave`, `_totalWaves`, `_allWavesCompleted`).
3. **Trigger waves** by walking the player right — wave triggers are progression-based (Level1 wave 0 fires at ~5% progression). In a headless/quick test, nudge the player transform.x forward to cross the trigger.
4. **Apply the fix, then reproduce again** and confirm the observed behavior changed. Only then call it fixed.

## Gotchas

- **`AI_Active` gates *all* enemy decisions, not just attacks.** `DoSomething()` returns immediately when it's false — no attacking *and* no move/reposition. `targetSpotted` being true is unrelated; it's set in `Update` independently of `AI_Active`.
- **`Level.Start` fails silently-ish.** On a missing config or unresolved `IEntitiesService` it `Debug.LogError`s and returns from `Start`, so nothing spawns and there's no exception — check the Console for the `[Level]` error rather than expecting a crash.
- **Wave completion is death-driven.** `SpawnerService` advances waves via `EntitiesService.OnEntityUnregistered`; enemies are unregistered in `Level.OnUnitDeath` (via `HealthSystem.onUnitDeath`). If enemies never die (e.g. the `AI_Active` gap means the player can't get a fight going, or unregistration breaks), waves stall and never advance.
- **Two enemy-tracking systems coexist mid-migration.** The new registry is `EntitiesService` (replaced the static `EnemyManager`); legacy `WaveManager` still exists for 04/05. Don't mix them — confirm which system a scene uses before debugging its spawning.
- **`FindObjectOfType<UIManager>` for end-of-level screens.** `Level` looks up `UIManager` at runtime for completion/game-over menus; if the boot flow didn't bring a `UIManager` into the scene set, those menus silently won't show (a `[Level]` warning is logged).
