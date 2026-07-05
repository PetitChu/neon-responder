# Neon Engine Base — Plan 2: M1 Engagement Spine (R1 Prototype) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the M1 remainder — DOTS chaff/ambient swarm + `SwarmBridge`, `AutoEngageSystem`, Finish-Ready marking + single-prompt selector, `MomentumSystem`, `FinishResolver` wiring the existing verbs (single-verb chaff finish), whiff cost, the AI_Active spawn-gap fix, and a minimal uGUI HUD — proving the GDD's two core bets (R1 density, R2 not-idle feel) in `03_Level1`.

**Architecture:** The ECS sim (`BrainlessLabs.Neon.Simulation`) owns chaff/ambient truth; a single `ISwarmBridge` seam in `Neon` (registered per-`Level`, `NullSwarmBridge` as session default) unifies targeting/damage/finish across DOTS chaff and MonoBehaviour hero-tier. Engagement systems are pure C# `IGameplayTickable`s on the M0 clock (AutoEngage 0 → FinishReady 10 → Selector 20 → Momentum 30); all cross-system talk goes through `IGameplaySignals`; Momentum is one `Mult` modifier source on the M0 stat sheets. Rendering is a projection: pooled `SpriteRenderer` proxies (hot chaff) + `Graphics.DrawMeshInstanced` (ambient) per the spike verdict.

**Tech Stack:** Unity 6000.3.5f2 (**built-in render pipeline** — spike-verified; URP is installed but inactive), VContainer, R3, Unity Entities 1.4.4 + Burst, com.unity.test-framework (EditMode), uGUI.

**Spec:** `docs/superpowers/specs/2026-07-04-neon-engine-base-design.md` §5.1–§5.2, §7 M1
**Inputs:** `2026-07-04-swarm-render-spike-verdict.md` (PASS — hybrid confirmed), `2026-07-04-plan2-m1-pre-brief.md` (verified seams, F1 blocker)
**Branch:** create `claude/neon-m1-engagement` off `master` (master @ `09b57dc` contains all M0 + spike work)

---

## Fork decisions (answered by Sebastien, 2026-07-04)

| Fork | Decision |
|---|---|
| **F1 spine visibility** (blocker: scene scopes parent to `GameServicesState`'s scope and cannot resolve the spine) | **Move the `ScenesService` registration** from `GameServicesState` down to `GameplayServicesState`. One-line move; scene scopes then see spine + everything above. |
| **F2 chaff damage model** | **Sim-owned Health** (spec as written). ECS `SwarmHealth` on chaff; chip + verb hits flow through the bridge; Mono `HealthSystem` stays hero-tier only. |
| **F3 M1 HUD scope** | **Simple uGUI under the existing canvas** — real placements, zero styling polish, pure `IGameplaySignals` consumer. |
| **F4 chaff spatial queries** | **Bridge-only** (spec as written). No colliders on chaff; verb hitboxes hit chaff via the bridge's box query; one spatial source of truth. |
| **F5 branch base** | New branch `claude/neon-m1-engagement` off `master` (which now equals `claude/neon-engine-base`). Remote backup exists on `origin/claude/neon-engine-base`. |

## Spike-verdict constraints inherited (do not re-litigate)

1. **Built-in RP:** ambient draws use `Graphics.DrawMeshInstanced` + an instancing-safe unlit shader (pattern: `Assets/_neon/Spikes/SwarmRenderSpike/SpikeAmbientInstanced.shader`). `RenderMeshInstanced`, URP shaders, and `Sprites/Default` under manual instancing all fail **silently**.
2. **Stable entity↔proxy mapping** in the real render rig (spike's index-order mapping was perf-only).
3. **No `OnGUI` in the real HUD** (GC hitches measured).
4. **MCP editor screenshots do not capture `Graphics.Draw*`** — verify instanced draws via `UnityStats.instancedBatches` or the Game view, not MCP screenshots.

---

## Working agreements (read once, apply to every task)

1. **Unity must import new files before tests/play.** After creating/editing `.cs`/`.asmdef`/`.shader` files, focus the editor (or `mcp__unityMCP__refresh_unity`), wait for compile, check console for **zero errors**. Every "Run tests"/"Play" step implies this.
2. **EditMode tests:** Test Runner window → EditMode → Run All, or `mcp__unityMCP__run_tests` with `mode: "EditMode"`. The M0 suite (23 tests) must stay green throughout.
3. **Play-testing = Recipe 4** (`neon-recipes`): boot via `BootstrapSettingsAsset` with **Post-Bootstrap Scene = `SceneDefinition_Level1`**. Never press Play directly in a level scene. No git/asset writes while in Play mode.
4. **Commits include `.meta` files.** Trailer on every commit body: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
5. **Do not touch:** `WaveManager`, `04_Level2`/`05_Level3`, `ApplicationLifetimeScope`, `Assets/Addons/`, the spike folder (reference only).
6. **DI rules:** no statics/singletons (existing static *events* on `UnitActions`/`HealthSystem` are the established observation seam — extending that pattern is allowed); session services register in FSM states, per-run systems in the `Level` scope; consumers use `[Inject]` / ctor injection / `unit.<Service>`.
7. **Combat verbs stay unchanged.** Every combat hook in this plan is additive (new branch in `CheckForHit`, new `Exit` lines, new state, new static event). If a step would change existing verb *behavior*, stop — that's a spec violation.
8. **Existing types referenced here were read from source** on 2026-07-04 (`UnitActions`, `HealthSystem`, `EnemyBehaviour`, `SpawnerService`, `Level`, `ScenesService`, `EntitiesQueries`, `PlayerAttack`, `PlayerWeaponAttack`, `UnitHit`, `UnitSettings`, `LevelConfigurationAsset`, `GameplayClock`, `StatSheet`, `GameplaySignals`, settings base types). If a signature doesn't match at execution time, re-read the file before adapting.

---

## File structure

**Created — data + spine consumers (`BrainlessLabs.Neon`):**

| Path | Responsibility |
|---|---|
| `Assets/_neon/Scripts/Signals/GameplayEvents.cs` | M1 signal structs + `MomentumTier` enum |
| `Assets/_neon/Scripts/Engagement/EngagementSettings.cs` | Tuning values (`ISettings`) |
| `Assets/_neon/Scripts/Engagement/EngagementSettingsAsset.cs` | `BaseSettingsAsset` wrapper |
| `Assets/_neon/Scripts/Engagement/EngagementConfig.cs` | Plain config structs (`EngagementConfig`, `MomentumConfig`) — keep systems testable without asset loads |
| `Assets/_neon/Scripts/Engagement/IMomentumSystem.cs` | Momentum interface |
| `Assets/_neon/Scripts/Engagement/MomentumSystem.cs` | Tiers, decay, the single Mult modifier, whiff reset |
| `Assets/_neon/Scripts/Engagement/AutoEngageSystem.cs` | Rhythm chip attack across both worlds |
| `Assets/_neon/Scripts/Engagement/FinishReadyMarker.cs` | Hero-tier ready flag + glow tint |
| `Assets/_neon/Scripts/Engagement/FinishReadySystem.cs` | Marks hero-tier ready (≤25% HP or knocked down) |
| `Assets/_neon/Scripts/Engagement/FinishReadySelector.cs` | Single-prompt selector (R7) + "+N ready" |
| `Assets/_neon/Scripts/Engagement/FinishResolver.cs` | Observes hits/whiffs → `EnemyFinished`/`VerbWhiffed` + whiff stagger |
| `Assets/_neon/Scripts/Units/PlayerStates/PlayerWhiffStagger.cs` | 0.5s vulnerability stagger state |
| `Assets/_neon/Scripts/Swarm/TargetRef.cs` | Unified ECS-or-Mono target handle |
| `Assets/_neon/Scripts/Swarm/ISwarmBridge.cs` | The Layer-1 seam |
| `Assets/_neon/Scripts/Swarm/NullSwarmBridge.cs` | Session-default no-op (scenes without a swarm) |
| `Assets/_neon/Scripts/Swarm/SwarmBridge.cs` | Real bridge: world-state push, queries, damage/finish, event drain |
| `Assets/_neon/Scripts/Swarm/SwarmConfig.cs` | Per-level swarm config struct |
| `Assets/_neon/Scripts/Swarm/SwarmRenderRig.cs` | Stable-mapped proxy pool + instanced ambient (scene MonoBehaviour) |
| `Assets/_neon/Shaders/NeonInstancedUnlit.shader` | Instancing-safe unlit shader (promoted spike pattern) |
| `Assets/_neon/Scripts/UI/UIHUDMomentumMeter.cs` | Momentum tier meter (signals consumer) |
| `Assets/_neon/Scripts/UI/UIHUDFinishPrompt.cs` | Verb prompt + "+N ready" (signals consumer) |

**Created — sim (`BrainlessLabs.Neon.Simulation`):**

| Path | Responsibility |
|---|---|
| `Assets/_neon/Scripts/Simulation/SwarmComponents.cs` | `BeltPosition`, `SwarmVelocity`, `SwarmHealth`, `FinishReadyTag`, `SwarmWorldState`, command/event buffers |
| `Assets/_neon/Scripts/Simulation/SwarmSpawnSystem.cs` | Belt-end chaff flood + one-time ambient seed, caps enforced |
| `Assets/_neon/Scripts/Simulation/SwarmSteeringSystem.cs` | Chaff seek-player, ambient wander (Burst) |
| `Assets/_neon/Scripts/Simulation/SwarmDamageSystem.cs` | Consumes damage/kill commands |
| `Assets/_neon/Scripts/Simulation/FinishReadyEvalSystem.cs` | Enables `FinishReadyTag` at ≤ threshold HP |
| `Assets/_neon/Scripts/Simulation/SwarmDeathSystem.cs` | Despawn + death events |

**Created — tests:**

| Path | Responsibility |
|---|---|
| `Assets/_neon/Tests/EditMode/Fakes.cs` | `FakeEntitiesService`, `FakeSwarmBridge` |
| `Assets/_neon/Tests/EditMode/MomentumSystemTests.cs` | Steps/tiers/decay/multipliers/whiff reset |
| `Assets/_neon/Tests/EditMode/AutoEngageSystemTests.cs` | Cadence, stat-driven rate, nearest-target pick |
| `Assets/_neon/Tests/EditMode/FinishReadySelectorTests.cs` | One-prompt rule, count, nearest priority |
| `Assets/_neon/Tests/EditMode/FinishResolverTests.cs` | Finish detection, whiff publish, grab exemption |

**Modified:**

| Path | Change |
|---|---|
| `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameServicesState.cs` | Remove `ScenesService` registration (F1) |
| `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs` | Add `ScenesService` (F1), `MomentumSystem`, `NullSwarmBridge` registrations |
| `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef` | Add `Unity.Entities`, `Unity.Collections`, `Unity.Mathematics` references |
| `Assets/_neon/Scripts/Stats/StatId.cs` | Append `AutoEngageRange = 3` |
| `Assets/_neon/Scripts/Spawner/LevelConfigurationAsset.cs` | Add `SwarmDensityBlock` (data extension) |
| `Assets/_neon/Scripts/Level/Level.cs` | Register + eager-create per-level engagement systems in `Configure` |
| `Assets/_neon/Scripts/Spawner/SpawnerService.cs` | AI_Active spawn-gap fix in `SpawnWaveEnemy` |
| `Assets/_neon/Scripts/Units/UnitActions.cs` | `[Inject] ISwarmBridge` + additive chaff branch in `CheckForHit` + `onVerbWhiffed` event |
| `Assets/_neon/Scripts/Units/PlayerStates/PlayerAttack.cs` | Whiff report in `Exit` |
| `Assets/_neon/Scripts/Units/PlayerStates/PlayerWeaponAttack.cs` | Whiff report in new `Exit` |
| `Assets/_neon/Scenes/Game/03_Level1.unity` | SwarmRenderRig + HUD wiring (editor work) |
| `LevelConfiguration_Level1` asset | Enable swarm block (editor work) |

---

### Task 1: Branch + F1 spine-visibility fix

The pre-brief's runtime probe proved scene scopes resolve `IEntitiesService` but fail on `IStatSystem`/`IGameplayClock` — `ScenesService` captures `GameServicesState`'s scope as every scene's DI parent. Moving its registration one state deeper makes the deepest session scope the scene parent. Everything M1 puts in the level scene depends on this.

**Files:**
- Modify: `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameServicesState.cs`
- Modify: `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs`

- [ ] **Step 1: Create the branch**

```bash
git -C "G:/Brainless Labs/neon-responder" checkout master
git checkout -b claude/neon-m1-engagement
```

- [ ] **Step 2: Remove the ScenesService registration from `GameServicesState`**

In `GameServicesState.cs`, delete the call and the helper — change:

```csharp
        protected override void RegisterTypes(IContainerBuilder builder)
        {
            base.RegisterTypes(builder);
            RegisterNextState(builder);
            RegisterAudioService(builder);
            RegisterInputService(builder);
            RegisterScenesService(builder);
            RegisterEntitiesService(builder);
        }
```

to:

```csharp
        protected override void RegisterTypes(IContainerBuilder builder)
        {
            base.RegisterTypes(builder);
            RegisterNextState(builder);
            RegisterAudioService(builder);
            RegisterInputService(builder);
            RegisterEntitiesService(builder);
        }
```

and delete this whole helper method:

```csharp
        private static void RegisterScenesService(IContainerBuilder builder)
        {
            builder.Register<ScenesService>(Lifetime.Singleton)
                .As<IScenesService>();
        }
```

- [ ] **Step 3: Add it to `GameplayServicesState`**

In `GameplayServicesState.cs`, change:

```csharp
        protected override void RegisterTypes(IContainerBuilder builder)
        {
            base.RegisterTypes(builder);
            RegisterNextState(builder);
            RegisterGameplaySignals(builder);
            RegisterStatSystem(builder);
            RegisterGameplayClock(builder);
        }
```

to:

```csharp
        protected override void RegisterTypes(IContainerBuilder builder)
        {
            base.RegisterTypes(builder);
            RegisterNextState(builder);
            RegisterGameplaySignals(builder);
            RegisterStatSystem(builder);
            RegisterGameplayClock(builder);
            RegisterScenesService(builder);
        }
```

and add this helper below `RegisterGameplayClock` (F1: registered here so every loaded scene's scope parents to THIS scope and can resolve the spine):

```csharp
        // F1 spine-visibility fix: ScenesService captures its owning scope as the DI
        // parent of every loaded scene (LifetimeScope.EnqueueParent). It must live in
        // the DEEPEST session scope or scenes cannot resolve the spine services.
        private static void RegisterScenesService(IContainerBuilder builder)
        {
            builder.Register<ScenesService>(Lifetime.Singleton)
                .As<IScenesService>();
        }
```

- [ ] **Step 4: Compile + boot regression test (Recipe 4)**

Refresh Unity, zero errors, all 23 EditMode tests PASS. Then boot play-test into `03_Level1`:
- Full state chain logs (`GameServicesState → GameplayServicesState → GameState`), `[Gameplay] GameplayClock ticking.`, `[Lifecycle] Loading post-bootstrap scene: …`.
- Level1 loads, player spawns, wave 1 fires when walking right — no `VContainerException` anywhere.
- `GameState` still resolves `IScenesService` (the scene loaded at all = proof).

Exit Play mode.

- [ ] **Step 5: Commit**

```bash
git add "Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameServicesState.cs" "Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs"
git commit -m "fix: move ScenesService to GameplayServicesState so scene scopes see the spine (F1)"
```

---

### Task 2: Data layer — signals, StatId, settings, configs, swarm-density block

Pure data, no behavior: compile-and-commit (no TDD needed).

**Files:**
- Create: `Assets/_neon/Scripts/Signals/GameplayEvents.cs`
- Modify: `Assets/_neon/Scripts/Stats/StatId.cs`
- Create: `Assets/_neon/Scripts/Engagement/EngagementSettings.cs`
- Create: `Assets/_neon/Scripts/Engagement/EngagementSettingsAsset.cs`
- Create: `Assets/_neon/Scripts/Engagement/EngagementConfig.cs`
- Create: `Assets/_neon/Scripts/Swarm/SwarmConfig.cs`
- Modify: `Assets/_neon/Scripts/Spawner/LevelConfigurationAsset.cs`

- [ ] **Step 1: Signal structs**

`Assets/_neon/Scripts/Signals/GameplayEvents.cs`:

```csharp
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>Momentum tiers (GDD §9): multipliers ×1.0 / ×1.3 / ×1.7 / ×2.5.</summary>
    public enum MomentumTier
    {
        Cool = 0,
        Warm = 1,
        Hot = 2,
        Overdrive = 3
    }

    /// <summary>A finishing hit completed on a Finish-Ready target. Momentum steps on THIS only (v0.4).</summary>
    public readonly struct EnemyFinished
    {
        public readonly Vector2 Position;
        public readonly bool WasChaff;

        public EnemyFinished(Vector2 position, bool wasChaff)
        {
            Position = position;
            WasChaff = wasChaff;
        }
    }

    public readonly struct MomentumTierChanged
    {
        public readonly MomentumTier Previous;
        public readonly MomentumTier Current;

        public MomentumTierChanged(MomentumTier previous, MomentumTier current)
        {
            Previous = previous;
            Current = current;
        }
    }

    /// <summary>Selector output — the SINGLE prompted target (R7 one-prompt rule) + total ready count.</summary>
    public readonly struct FinishReadyPromptChanged
    {
        public readonly bool HasTarget;
        public readonly Vector2 TargetPosition;
        public readonly ATTACKTYPE SuggestedVerb;
        public readonly int ReadyCount;

        public FinishReadyPromptChanged(bool hasTarget, Vector2 targetPosition, ATTACKTYPE suggestedVerb, int readyCount)
        {
            HasTarget = hasTarget;
            TargetPosition = targetPosition;
            SuggestedVerb = suggestedVerb;
            ReadyCount = readyCount;
        }
    }

    /// <summary>A completed punch/kick/weapon swing that hit nothing (grab whiffs exempt — v0.4).</summary>
    public readonly struct VerbWhiffed
    {
        public readonly ATTACKTYPE AttackType;

        public VerbWhiffed(ATTACKTYPE attackType)
        {
            AttackType = attackType;
        }
    }
}
```

- [ ] **Step 2: Append the range stat**

In `Assets/_neon/Scripts/Stats/StatId.cs`, change:

```csharp
        // Auto-engage (bases set by AutoEngageSystem in M1)
        AutoEngageRate = 0,
        AutoEngageDamage = 1,
        AutoEngageArcDegrees = 2,
```

to:

```csharp
        // Auto-engage (bases set by AutoEngageSystem in M1)
        AutoEngageRate = 0,
        AutoEngageDamage = 1,
        AutoEngageArcDegrees = 2,
        AutoEngageRange = 3,
```

- [ ] **Step 3: Engagement settings (Recipe 5 pattern)**

`Assets/_neon/Scripts/Engagement/EngagementSettings.cs`:

```csharp
using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Tuning for the M1 engagement spine. Numbers from spec §5.1/§9 and the
    /// protocol doc's guardrails (auto-rate hard cap 6/s, arc cap 180°,
    /// Momentum the only multiplier at ×1.0/1.3/1.7/2.5).
    /// </summary>
    [Serializable]
    public class EngagementSettings : ISettings
    {
        [Header("Auto-engage")]
        [SerializeField] private float _autoEngageRatePerSecond = 1.5f;
        [SerializeField] private int _autoEngageChipDamage = 8;
        [SerializeField] private float _autoEngageArcDegrees = 120f;
        [SerializeField] private float _autoEngageRange = 4f;

        [Header("Finish-Ready")]
        [SerializeField, Range(0f, 1f)] private float _finishReadyHealthThreshold = 0.25f;
        [SerializeField] private Color _finishReadyGlow = new(1f, 0.85f, 0.2f, 1f);

        [Header("Momentum")]
        [SerializeField] private int _momentumStepsPerTier = 3;
        [SerializeField] private float _momentumDecaySeconds = 2.5f;
        [SerializeField] private float[] _momentumTierMultipliers = { 1f, 1.3f, 1.7f, 2.5f };
        [SerializeField] private float _whiffStaggerSeconds = 0.5f;

        [Header("Chaff")]
        [SerializeField] private int _chaffMaxHealth = 24;

        public float AutoEngageRatePerSecond => _autoEngageRatePerSecond;
        public int AutoEngageChipDamage => _autoEngageChipDamage;
        public float AutoEngageArcDegrees => _autoEngageArcDegrees;
        public float AutoEngageRange => _autoEngageRange;
        public float FinishReadyHealthThreshold => _finishReadyHealthThreshold;
        public Color FinishReadyGlow => _finishReadyGlow;
        public int MomentumStepsPerTier => _momentumStepsPerTier;
        public float MomentumDecaySeconds => _momentumDecaySeconds;
        public float[] MomentumTierMultipliers => _momentumTierMultipliers;
        public float WhiffStaggerSeconds => _whiffStaggerSeconds;
        public int ChaffMaxHealth => _chaffMaxHealth;

#if UNITY_EDITOR
        public void Editor_OnGUI(UnityEngine.Object target)
        {
            var serializedObject = new UnityEditor.SerializedObject(target);
            serializedObject.UpdateIfRequiredOrScript();
            var settingsProperty = serializedObject.FindProperty("_settings");
            UnityEditor.EditorGUILayout.PropertyField(settingsProperty, new GUIContent("Engagement Settings"), includeChildren: true);
            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}
```

`Assets/_neon/Scripts/Engagement/EngagementSettingsAsset.cs` (one-liner, exactly like `AudioSettingsAsset` — the base auto-creates the `.asset` under `Assets/Resources/Settings/` on first access; do NOT add `[CreateAssetMenu]`):

```csharp
namespace BrainlessLabs.Neon
{
    public class EngagementSettingsAsset : BaseSettingsAsset<EngagementSettingsAsset, EngagementSettings> { }
}
```

- [ ] **Step 4: Plain config structs (testability seam — systems never load assets)**

`Assets/_neon/Scripts/Engagement/EngagementConfig.cs`:

```csharp
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>Asset-free snapshot of EngagementSettings for the per-level systems (EditMode-testable).</summary>
    public readonly struct EngagementConfig
    {
        public readonly float AutoEngageRatePerSecond;
        public readonly int AutoEngageChipDamage;
        public readonly float AutoEngageArcDegrees;
        public readonly float AutoEngageRange;
        public readonly float FinishReadyHealthThreshold;
        public readonly Color FinishReadyGlow;
        public readonly float WhiffStaggerSeconds;

        public EngagementConfig(float ratePerSecond, int chipDamage, float arcDegrees, float range,
            float finishReadyThreshold, Color finishReadyGlow, float whiffStaggerSeconds)
        {
            AutoEngageRatePerSecond = ratePerSecond;
            AutoEngageChipDamage = chipDamage;
            AutoEngageArcDegrees = arcDegrees;
            AutoEngageRange = range;
            FinishReadyHealthThreshold = finishReadyThreshold;
            FinishReadyGlow = finishReadyGlow;
            WhiffStaggerSeconds = whiffStaggerSeconds;
        }

        public static EngagementConfig FromSettings()
        {
            var s = EngagementSettingsAsset.InstanceAsset.Settings;
            return new EngagementConfig(s.AutoEngageRatePerSecond, s.AutoEngageChipDamage,
                s.AutoEngageArcDegrees, s.AutoEngageRange, s.FinishReadyHealthThreshold,
                s.FinishReadyGlow, s.WhiffStaggerSeconds);
        }
    }

    /// <summary>Asset-free snapshot of the Momentum tuning.</summary>
    public readonly struct MomentumConfig
    {
        public readonly int StepsPerTier;
        public readonly float DecaySeconds;
        public readonly float[] TierMultipliers;

        public MomentumConfig(int stepsPerTier, float decaySeconds, float[] tierMultipliers)
        {
            StepsPerTier = stepsPerTier;
            DecaySeconds = decaySeconds;
            TierMultipliers = tierMultipliers;
        }

        public static MomentumConfig FromSettings()
        {
            var s = EngagementSettingsAsset.InstanceAsset.Settings;
            return new MomentumConfig(s.MomentumStepsPerTier, s.MomentumDecaySeconds, s.MomentumTierMultipliers);
        }
    }
}
```

`Assets/_neon/Scripts/Swarm/SwarmConfig.cs`:

```csharp
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>Per-level swarm parameters, built by Level from its LevelConfigurationAsset + geometry.</summary>
    public readonly struct SwarmConfig
    {
        public readonly bool Enabled;
        public readonly int ChaffCap;
        public readonly int AmbientCap;
        public readonly float SpawnRatePerSecond;
        public readonly int ChaffMaxHealth;
        public readonly float ChaffMoveSpeed;
        public readonly Vector2 BeltMin;
        public readonly Vector2 BeltMax;
        public readonly float FinishReadyThreshold;

        public SwarmConfig(bool enabled, int chaffCap, int ambientCap, float spawnRatePerSecond,
            int chaffMaxHealth, float chaffMoveSpeed, Vector2 beltMin, Vector2 beltMax, float finishReadyThreshold)
        {
            Enabled = enabled;
            ChaffCap = chaffCap;
            AmbientCap = ambientCap;
            SpawnRatePerSecond = spawnRatePerSecond;
            ChaffMaxHealth = chaffMaxHealth;
            ChaffMoveSpeed = chaffMoveSpeed;
            BeltMin = beltMin;
            BeltMax = beltMax;
            FinishReadyThreshold = finishReadyThreshold;
        }

        public static SwarmConfig From(LevelConfigurationAsset config, Level level)
        {
            var block = config.Swarm;
            var settings = EngagementSettingsAsset.InstanceAsset.Settings;
            return new SwarmConfig(
                block.EnableSwarm,
                block.ChaffCap,
                block.AmbientCap,
                block.ChaffSpawnRatePerSecond,
                settings.ChaffMaxHealth,
                block.ChaffMoveSpeed,
                new Vector2(level.LevelStartX, block.BeltYMin),
                new Vector2(level.LevelStartX + level.LevelLength, block.BeltYMax),
                settings.FinishReadyHealthThreshold);
        }
    }
}
```

- [ ] **Step 5: Swarm-density block on the level config (data extension, spec §5.2 job 4)**

In `Assets/_neon/Scripts/Spawner/LevelConfigurationAsset.cs`, insert between the `Waves` header block and the `Completion` header block:

```csharp
        [Header("Swarm")]
        [Tooltip("DOTS chaff/ambient density for this level (spec §6 budget: chaff 80-150, ambient ~100).")]
        public SwarmDensityBlock Swarm = new();
```

and append this nested class inside the same file, after the `LevelConfigurationAsset` class closing brace but inside the namespace:

```csharp
    [System.Serializable]
    public class SwarmDensityBlock
    {
        [Tooltip("Master switch — off leaves the level exactly as it was pre-M1.")]
        public bool EnableSwarm = false;

        [Range(0, 150)] public int ChaffCap = 120;
        [Range(0, 150)] public int AmbientCap = 100;

        [Tooltip("Chaff spawned per second (flooding from both belt ends) until the cap is reached.")]
        public float ChaffSpawnRatePerSecond = 8f;

        [Tooltip("Chaff walk speed toward the player.")]
        public float ChaffMoveSpeed = 1.6f;

        [Tooltip("Belt depth band (world Y) the swarm walks in.")]
        public float BeltYMin = -3.5f;
        public float BeltYMax = -0.5f;
    }
```

- [ ] **Step 6: Compile, tests, settings asset**

Refresh Unity — zero errors, 23 tests PASS. Then enter Play mode once via Recipe 4 and exit: this auto-creates `Assets/Resources/Settings/EngagementSettingsAsset.asset` (first `InstanceAsset` access happens in Task 5's registrations, so alternatively just verify it appears by then — the asset must exist and be committed before the M1 gate).

- [ ] **Step 7: Commit**

```bash
git add "Assets/_neon/Scripts/Signals/GameplayEvents.cs" "Assets/_neon/Scripts/Signals/GameplayEvents.cs.meta" "Assets/_neon/Scripts/Stats/StatId.cs" "Assets/_neon/Scripts/Engagement" "Assets/_neon/Scripts/Engagement.meta" "Assets/_neon/Scripts/Swarm" "Assets/_neon/Scripts/Swarm.meta" "Assets/_neon/Scripts/Spawner/LevelConfigurationAsset.cs"
git commit -m "feat: M1 data layer - signals, engagement settings, swarm density block"
```

---

### Task 3: `MomentumSystem` (test-first) + session registration

Pure C# (spec §5.1): +1 step per finish, 3 steps/tier, tiers Cool→Warm→Hot→Overdrive, decay −1 tier per 2.5s idle, whiff → reset to Cool. Registers **one Mult modifier** into `Player.DamageMultiplier` + `Run.GainMultiplier` (the "skill × stack" bridge). Run-agnostic → lives in `GameplayServicesState` (spec §4.3).

**Files:**
- Create: `Assets/_neon/Scripts/Engagement/IMomentumSystem.cs`
- Create: `Assets/_neon/Scripts/Engagement/MomentumSystem.cs`
- Modify: `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs`
- Test: `Assets/_neon/Tests/EditMode/MomentumSystemTests.cs`

- [ ] **Step 1: Write the failing tests**

`Assets/_neon/Tests/EditMode/MomentumSystemTests.cs` (drives the real M0 `GameplayClock` via `Advance` — Momentum registers itself at order 30):

```csharp
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class MomentumSystemTests
    {
        private GameplayClock _clock;
        private GameplaySignals _signals;
        private StatSystem _stats;
        private MomentumSystem _momentum;

        private static MomentumConfig TestConfig => new(stepsPerTier: 3, decaySeconds: 2.5f,
            tierMultipliers: new[] { 1f, 1.3f, 1.7f, 2.5f });

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _signals = new GameplaySignals();
            _stats = new StatSystem();
            _momentum = new MomentumSystem(_clock, _signals, _stats, TestConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _momentum.Dispose();
            _signals.Dispose();
        }

        private void Finish() => _signals.Publish(new EnemyFinished(Vector2.zero, wasChaff: true));

        [Test]
        public void StartsCool_WithNeutralMultipliers()
        {
            Assert.AreEqual(MomentumTier.Cool, _momentum.Tier);
            Assert.AreEqual(1f, _stats.Player.GetValue(StatId.DamageMultiplier), 0.0001f);
            Assert.AreEqual(1f, _stats.Run.GetValue(StatId.GainMultiplier), 0.0001f);
        }

        [Test]
        public void ThreeFinishes_ReachWarm_AppliesMultiplier()
        {
            Finish(); Finish(); Finish();

            Assert.AreEqual(MomentumTier.Warm, _momentum.Tier);
            Assert.AreEqual(1.3f, _stats.Player.GetValue(StatId.DamageMultiplier), 0.0001f);
            Assert.AreEqual(1.3f, _stats.Run.GetValue(StatId.GainMultiplier), 0.0001f);
        }

        [Test]
        public void NineFinishes_ReachOverdrive_CapsThere()
        {
            for (int i = 0; i < 12; i++) Finish();

            Assert.AreEqual(MomentumTier.Overdrive, _momentum.Tier);
            Assert.AreEqual(2.5f, _stats.Player.GetValue(StatId.DamageMultiplier), 0.0001f);
        }

        [Test]
        public void TierChange_PublishesSignal()
        {
            MomentumTierChanged received = default;
            using var sub = _signals.On<MomentumTierChanged>().Subscribe(e => received = e);

            Finish(); Finish(); Finish();

            Assert.AreEqual(MomentumTier.Cool, received.Previous);
            Assert.AreEqual(MomentumTier.Warm, received.Current);
        }

        [Test]
        public void IdleDecay_DropsOneTierPerWindow()
        {
            for (int i = 0; i < 6; i++) Finish();          // Hot
            Assert.AreEqual(MomentumTier.Hot, _momentum.Tier);

            _clock.Advance(2.6f);                          // one idle window
            Assert.AreEqual(MomentumTier.Warm, _momentum.Tier);
            Assert.AreEqual(1.3f, _stats.Player.GetValue(StatId.DamageMultiplier), 0.0001f);

            _clock.Advance(2.6f);                          // another
            Assert.AreEqual(MomentumTier.Cool, _momentum.Tier);
        }

        [Test]
        public void Finish_ResetsIdleTimer()
        {
            Finish(); Finish(); Finish();                  // Warm
            _clock.Advance(2f);                            // not yet decayed
            Finish();                                      // resets idle
            _clock.Advance(2f);                            // still under window since last finish

            Assert.AreEqual(MomentumTier.Warm, _momentum.Tier);
        }

        [Test]
        public void Whiff_ResetsToCool()
        {
            for (int i = 0; i < 9; i++) Finish();          // Overdrive
            _signals.Publish(new VerbWhiffed(ATTACKTYPE.PUNCH));

            Assert.AreEqual(MomentumTier.Cool, _momentum.Tier);
            Assert.AreEqual(1f, _stats.Player.GetValue(StatId.DamageMultiplier), 0.0001f);
        }

        [Test]
        public void Dispose_RemovesModifiersAndStopsTicking()
        {
            Finish(); Finish(); Finish();                  // Warm, ×1.3
            _momentum.Dispose();

            Assert.AreEqual(1f, _stats.Player.GetValue(StatId.DamageMultiplier), 0.0001f);
            Assert.DoesNotThrow(() => _clock.Advance(3f)); // unregistered from clock
        }
    }
}
```

- [ ] **Step 2: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`MomentumSystem` does not exist yet). Proceed.

- [ ] **Step 3: Implement**

`Assets/_neon/Scripts/Engagement/IMomentumSystem.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Momentum: the skill meter (spec §5.1). Steps on EnemyFinished only;
    /// its tier is the ONLY global multiplier in the game (protocol doc §8.1),
    /// applied as one Mult modifier on Player.DamageMultiplier + Run.GainMultiplier.
    /// </summary>
    public interface IMomentumSystem
    {
        MomentumTier Tier { get; }
    }
}
```

`Assets/_neon/Scripts/Engagement/MomentumSystem.cs`:

```csharp
using System;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    public sealed class MomentumSystem : IMomentumSystem, IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 30; // reserved band: Momentum decay (IGameplayClock doc)

        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly IStatSystem _stats;
        private readonly MomentumConfig _config;
        private readonly ModifierSource _source = ModifierSource.Create("momentum");
        private readonly IDisposable _finishSubscription;
        private readonly IDisposable _whiffSubscription;

        private int _steps;
        private float _idleSeconds;

        public MomentumTier Tier { get; private set; } = MomentumTier.Cool;

        public MomentumSystem(IGameplayClock clock, IGameplaySignals signals, IStatSystem stats, MomentumConfig config)
        {
            _clock = clock;
            _signals = signals;
            _stats = stats;
            _config = config;

            // Multiplier stats read as ×1 when nothing has touched them.
            _stats.Player.SetBase(StatId.DamageMultiplier, 1f);
            _stats.Run.SetBase(StatId.GainMultiplier, 1f);

            _finishSubscription = _signals.On<EnemyFinished>().Subscribe(_ => OnFinish());
            _whiffSubscription = _signals.On<VerbWhiffed>().Subscribe(_ => ResetToCool());

            _clock.Register(this, TICK_ORDER);
            ApplyTier(MomentumTier.Cool, publish: false);
        }

        public void Tick(float deltaTime)
        {
            if (_steps == 0) return;

            _idleSeconds += deltaTime;
            if (_idleSeconds < _config.DecaySeconds) return;

            // Decay: -1 tier per idle window; steps snap to the bottom of the lower tier.
            _idleSeconds = 0f;
            int lowerTier = Mathf.Max(0, (int)Tier - 1);
            _steps = lowerTier * _config.StepsPerTier;
            var loweredTier = (MomentumTier)lowerTier;
            ApplyTier(loweredTier, publish: loweredTier != Tier); // no Cool→Cool spam
        }

        public void Dispose()
        {
            _finishSubscription?.Dispose();
            _whiffSubscription?.Dispose();
            _clock.Unregister(this);
            _stats.Player.RemoveBySource(_source);
            _stats.Run.RemoveBySource(_source);
        }

        private void OnFinish()
        {
            int maxSteps = _config.StepsPerTier * ((int)MomentumTier.Overdrive);
            _steps = Mathf.Min(_steps + 1, maxSteps);
            _idleSeconds = 0f;
            var newTier = (MomentumTier)Mathf.Min(_steps / _config.StepsPerTier, (int)MomentumTier.Overdrive);
            if (newTier != Tier) ApplyTier(newTier, publish: true);
        }

        private void ResetToCool()
        {
            _steps = 0;
            _idleSeconds = 0f;
            if (Tier != MomentumTier.Cool) ApplyTier(MomentumTier.Cool, publish: true);
        }

        private void ApplyTier(MomentumTier tier, bool publish)
        {
            var previous = Tier;
            Tier = tier;

            float multiplier = _config.TierMultipliers[Mathf.Clamp((int)tier, 0, _config.TierMultipliers.Length - 1)];
            _stats.Player.RemoveBySource(_source);
            _stats.Run.RemoveBySource(_source);
            _stats.Player.AddModifier(StatId.DamageMultiplier, StatOp.Mult, multiplier, _source);
            _stats.Run.AddModifier(StatId.GainMultiplier, StatOp.Mult, multiplier, _source);

            if (publish) _signals.Publish(new MomentumTierChanged(previous, tier));
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: 31/31 PASS (23 M0 + 8 Momentum).

- [ ] **Step 5: Register in `GameplayServicesState`**

Add to `RegisterTypes` (after `RegisterScenesService(builder);`):

```csharp
            RegisterMomentumSystem(builder);
```

and add the helper (eager-resolved via build callback — nothing else resolves it, and it must exist to subscribe/tick):

```csharp
        private static void RegisterMomentumSystem(IContainerBuilder builder)
        {
            builder.Register<MomentumSystem>(Lifetime.Singleton)
                .WithParameter(MomentumConfig.FromSettings())
                .As<IMomentumSystem>();
            builder.RegisterBuildCallback(container => container.Resolve<IMomentumSystem>());
        }
```

- [ ] **Step 6: Boot check + commit**

Boot play-test (Recipe 4): chain logs clean, no exceptions (Momentum constructs at `GameplayServicesState` entry; `EngagementSettingsAsset.asset` auto-creates on first access — confirm it appeared under `Assets/Resources/Settings/`). Exit Play mode.

```bash
git add "Assets/_neon/Scripts/Engagement/IMomentumSystem.cs" "Assets/_neon/Scripts/Engagement/IMomentumSystem.cs.meta" "Assets/_neon/Scripts/Engagement/MomentumSystem.cs" "Assets/_neon/Scripts/Engagement/MomentumSystem.cs.meta" "Assets/_neon/Tests/EditMode/MomentumSystemTests.cs" "Assets/_neon/Tests/EditMode/MomentumSystemTests.cs.meta" "Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs" "Assets/Resources/Settings/EngagementSettingsAsset.asset" "Assets/Resources/Settings/EngagementSettingsAsset.asset.meta"
git commit -m "feat: MomentumSystem - tiers, decay, the single Mult modifier (M1)"
```

---

### Task 4: Swarm sim — components + systems (`BrainlessLabs.Neon.Simulation`)

The sim owns chaff/ambient truth (F2). Commands in / events out via dynamic buffers on a control singleton the bridge owns. Compile-checked here; runtime-verified in Tasks 5–6.

**Files:**
- Create: `Assets/_neon/Scripts/Simulation/SwarmComponents.cs`
- Create: `Assets/_neon/Scripts/Simulation/SwarmSpawnSystem.cs`
- Create: `Assets/_neon/Scripts/Simulation/SwarmSteeringSystem.cs`
- Create: `Assets/_neon/Scripts/Simulation/SwarmDamageSystem.cs`
- Create: `Assets/_neon/Scripts/Simulation/FinishReadyEvalSystem.cs`
- Create: `Assets/_neon/Scripts/Simulation/SwarmDeathSystem.cs`

- [ ] **Step 1: Components**

`Assets/_neon/Scripts/Simulation/SwarmComponents.cs` (extends the existing `SwarmAgent`/`SwarmTier` from M0):

```csharp
using Unity.Entities;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>2D belt position (x = along the belt, y = depth lane band).</summary>
    public struct BeltPosition : IComponentData
    {
        public float2 Value;
        public int LaneIndex;
    }

    public struct SwarmVelocity : IComponentData
    {
        public float2 Value;
    }

    /// <summary>Chaff only (F2: sim-owned health). Ambient agents have no health.</summary>
    public struct SwarmHealth : IComponentData
    {
        public int Current;
        public int Max;
    }

    /// <summary>Enabled when a chaff agent is at or under the Finish-Ready threshold.</summary>
    public struct FinishReadyTag : IComponentData, IEnableableComponent
    {
    }

    /// <summary>
    /// Control singleton written by the SwarmBridge every gameplay tick.
    /// The sim reads it; it never reads Mono state directly.
    /// </summary>
    public struct SwarmWorldState : IComponentData
    {
        public float2 PlayerPosition;
        public float PlayerFacingSign;
        public int ChaffCap;
        public int AmbientCap;
        public float SpawnRatePerSecond;
        public int ChaffMaxHealth;
        public float ChaffMoveSpeed;
        public float2 BeltMin;
        public float2 BeltMax;
        public float FinishReadyThreshold;
        public byte Enabled;
    }

    /// <summary>
    /// Damage command from the Mono side. IsChip = 1 for auto-engage chip, which
    /// pushes toward Finish-Ready but NEVER kills (spec §5.1 — floored at 1 HP by
    /// SwarmDamageSystem); IsChip = 0 for verb damage, which may kill.
    /// </summary>
    public struct SwarmDamageCommand : IBufferElementData
    {
        public Entity Target;
        public int Amount;
        public byte IsChip;
    }

    /// <summary>Instant-kill command (finishing verb hit on a Finish-Ready chaff).</summary>
    public struct SwarmKillCommand : IBufferElementData
    {
        public Entity Target;
    }

    /// <summary>Events out of the sim, drained by the bridge each tick.</summary>
    public struct SwarmEventRecord : IBufferElementData
    {
        public const byte KIND_CHAFF_DIED = 0;

        public byte Kind;
        public float2 Position;
    }
}
```

- [ ] **Step 2: Spawn system**

`Assets/_neon/Scripts/Simulation/SwarmSpawnSystem.cs`:

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>
    /// Floods chaff from both belt ends up to ChaffCap; seeds AmbientCap scattered
    /// vibe props once. Idle in scenes with no SwarmWorldState singleton.
    /// </summary>
    [BurstCompile]
    public partial struct SwarmSpawnSystem : ISystem
    {
        private const int LANE_COUNT = 3;

        private EntityArchetype _chaffArchetype;
        private EntityArchetype _ambientArchetype;
        private EntityQuery _chaffQuery;
        private EntityQuery _ambientQuery;
        private Random _random;
        private float _spawnAccumulator;
        private bool _spawnFromLeft;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SwarmWorldState>();

            _chaffArchetype = state.EntityManager.CreateArchetype(
                typeof(SwarmAgent), typeof(BeltPosition), typeof(SwarmVelocity),
                typeof(SwarmHealth), typeof(FinishReadyTag));
            _ambientArchetype = state.EntityManager.CreateArchetype(
                typeof(SwarmAgent), typeof(BeltPosition), typeof(SwarmVelocity));

            _chaffQuery = state.GetEntityQuery(ComponentType.ReadOnly<SwarmHealth>());
            _ambientQuery = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
                .WithAll<SwarmAgent, BeltPosition>()
                .WithNone<SwarmHealth>()
                .Build(ref state);

            _random = new Random(0x9E3779B9u);
        }

        public void OnUpdate(ref SystemState state)
        {
            var world = SystemAPI.GetSingleton<SwarmWorldState>();
            if (world.Enabled == 0) return;

            SeedAmbient(ref state, in world);
            FloodChaff(ref state, in world);
        }

        private void SeedAmbient(ref SystemState state, in SwarmWorldState world)
        {
            int missing = world.AmbientCap - _ambientQuery.CalculateEntityCount();
            for (int i = 0; i < missing; i++)
            {
                var entity = state.EntityManager.CreateEntity(_ambientArchetype);
                var position = _random.NextFloat2(world.BeltMin, world.BeltMax);
                state.EntityManager.SetComponentData(entity, new SwarmAgent { Tier = SwarmTier.Ambient });
                state.EntityManager.SetComponentData(entity, new BeltPosition { Value = position, LaneIndex = 0 });
                state.EntityManager.SetComponentData(entity, new SwarmVelocity
                {
                    Value = _random.NextFloat2Direction() * _random.NextFloat(0.2f, 0.8f)
                });
            }
        }

        private void FloodChaff(ref SystemState state, in SwarmWorldState world)
        {
            int chaffCount = _chaffQuery.CalculateEntityCount();
            _spawnAccumulator += world.SpawnRatePerSecond * SystemAPI.Time.DeltaTime;

            while (_spawnAccumulator >= 1f && chaffCount < world.ChaffCap)
            {
                _spawnAccumulator -= 1f;
                chaffCount++;

                int lane = _random.NextInt(0, LANE_COUNT);
                float laneY = math.lerp(world.BeltMin.y, world.BeltMax.y, (lane + 0.5f) / LANE_COUNT);
                float spawnX = _spawnFromLeft ? world.BeltMin.x : world.BeltMax.x;
                _spawnFromLeft = !_spawnFromLeft;

                var entity = state.EntityManager.CreateEntity(_chaffArchetype);
                state.EntityManager.SetComponentData(entity, new SwarmAgent { Tier = SwarmTier.Chaff });
                state.EntityManager.SetComponentData(entity, new BeltPosition { Value = new float2(spawnX, laneY), LaneIndex = lane });
                state.EntityManager.SetComponentData(entity, new SwarmVelocity { Value = float2.zero });
                state.EntityManager.SetComponentData(entity, new SwarmHealth { Current = world.ChaffMaxHealth, Max = world.ChaffMaxHealth });
                state.EntityManager.SetComponentEnabled<FinishReadyTag>(entity, false);
            }

            if (chaffCount >= world.ChaffCap) _spawnAccumulator = 0f;
        }
    }
}
```

- [ ] **Step 3: Steering system**

`Assets/_neon/Scripts/Simulation/SwarmSteeringSystem.cs`:

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>
    /// Chaff seek the player and crowd at a stop radius; ambient bounce-wander.
    /// M1 simplification (documented deviation): per-lane Y + jitter instead of
    /// true separation steering.
    /// </summary>
    [BurstCompile]
    [UpdateAfter(typeof(SwarmSpawnSystem))]
    public partial struct SwarmSteeringSystem : ISystem
    {
        private const float STOP_RADIUS = 0.9f;
        private const float STEER_LERP = 0.08f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SwarmWorldState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var world = SystemAPI.GetSingleton<SwarmWorldState>();
            if (world.Enabled == 0) return;
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Chaff: seek player, hold lane depth.
            foreach (var (position, velocity, entity) in
                     SystemAPI.Query<RefRW<BeltPosition>, RefRW<SwarmVelocity>>()
                         .WithAll<SwarmHealth>()
                         .WithEntityAccess())
            {
                float laneY = math.lerp(world.BeltMin.y, world.BeltMax.y,
                    (position.ValueRO.LaneIndex + 0.5f) / 3f);
                var target = new float2(world.PlayerPosition.x, laneY);
                float2 toTarget = target - position.ValueRO.Value;
                float distance = math.length(toTarget);

                // Deterministic per-entity jitter so the crowd doesn't stack on one point.
                float jitter = (entity.Index % 7 - 3) * 0.15f;
                float2 desired = distance > STOP_RADIUS + jitter
                    ? math.normalizesafe(toTarget) * world.ChaffMoveSpeed
                    : float2.zero;

                var newVelocity = math.lerp(velocity.ValueRO.Value, desired, STEER_LERP);
                var newPosition = math.clamp(position.ValueRO.Value + newVelocity * deltaTime,
                    world.BeltMin, world.BeltMax);

                velocity.ValueRW.Value = newVelocity;
                position.ValueRW.Value = newPosition;
            }

            // Ambient: bounce-wander (spike pattern).
            foreach (var (position, velocity) in
                     SystemAPI.Query<RefRW<BeltPosition>, RefRW<SwarmVelocity>>()
                         .WithNone<SwarmHealth>())
            {
                float2 p = position.ValueRO.Value + velocity.ValueRO.Value * deltaTime;
                float2 v = velocity.ValueRO.Value;

                if (p.x < world.BeltMin.x || p.x > world.BeltMax.x)
                {
                    v.x = -v.x;
                    p.x = math.clamp(p.x, world.BeltMin.x, world.BeltMax.x);
                }
                if (p.y < world.BeltMin.y || p.y > world.BeltMax.y)
                {
                    v.y = -v.y;
                    p.y = math.clamp(p.y, world.BeltMin.y, world.BeltMax.y);
                }

                position.ValueRW.Value = p;
                velocity.ValueRW.Value = v;
            }
        }
    }
}
```

- [ ] **Step 4: Damage, eval, and death systems**

`Assets/_neon/Scripts/Simulation/SwarmDamageSystem.cs`:

```csharp
using Unity.Burst;
using Unity.Entities;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>Consumes bridge damage/kill commands. (Spec's SwarmChipSystem folded in — see plan deviations.)</summary>
    [BurstCompile]
    public partial struct SwarmDamageSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SwarmWorldState>();
            state.RequireForUpdate<SwarmDamageCommand>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var damageBuffer = SystemAPI.GetSingletonBuffer<SwarmDamageCommand>();
            for (int i = 0; i < damageBuffer.Length; i++)
            {
                var command = damageBuffer[i];
                if (!state.EntityManager.Exists(command.Target)) continue;
                if (!state.EntityManager.HasComponent<SwarmHealth>(command.Target)) continue;

                var health = state.EntityManager.GetComponentData<SwarmHealth>(command.Target);
                health.Current -= command.Amount;
                // Chip pushes toward Finish-Ready but never kills (spec §5.1):
                // without this floor, chip (8) skips the ready band (≤6 of 24)
                // and the loop starves. Only verbs (IsChip = 0) finish chaff.
                if (command.IsChip == 1 && health.Current < 1) health.Current = 1;
                state.EntityManager.SetComponentData(command.Target, health);
            }
            damageBuffer.Clear();

            var killBuffer = SystemAPI.GetSingletonBuffer<SwarmKillCommand>();
            for (int i = 0; i < killBuffer.Length; i++)
            {
                var command = killBuffer[i];
                if (!state.EntityManager.Exists(command.Target)) continue;
                if (!state.EntityManager.HasComponent<SwarmHealth>(command.Target)) continue;

                var health = state.EntityManager.GetComponentData<SwarmHealth>(command.Target);
                health.Current = 0;
                state.EntityManager.SetComponentData(command.Target, health);
            }
            killBuffer.Clear();
        }
    }
}
```

`Assets/_neon/Scripts/Simulation/FinishReadyEvalSystem.cs`:

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>Enables FinishReadyTag on chaff at or under the health threshold (spec §5.1).</summary>
    [BurstCompile]
    [UpdateAfter(typeof(SwarmDamageSystem))]
    public partial struct FinishReadyEvalSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SwarmWorldState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var world = SystemAPI.GetSingleton<SwarmWorldState>();

            foreach (var (health, finishReady) in
                     SystemAPI.Query<RefRO<SwarmHealth>, EnabledRefRW<FinishReadyTag>>()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                int threshold = (int)math.ceil(health.ValueRO.Max * world.FinishReadyThreshold);
                finishReady.ValueRW = health.ValueRO.Current > 0 && health.ValueRO.Current <= threshold;
            }
        }
    }
}
```

`Assets/_neon/Scripts/Simulation/SwarmDeathSystem.cs`:

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>Despawns dead chaff and records events for the bridge to drain.</summary>
    [BurstCompile]
    [UpdateAfter(typeof(FinishReadyEvalSystem))]
    public partial struct SwarmDeathSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SwarmWorldState>();
            state.RequireForUpdate<SwarmEventRecord>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var events = SystemAPI.GetSingletonBuffer<SwarmEventRecord>();
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (health, position, entity) in
                     SystemAPI.Query<RefRO<SwarmHealth>, RefRO<BeltPosition>>().WithEntityAccess())
            {
                if (health.ValueRO.Current > 0) continue;

                events.Add(new SwarmEventRecord
                {
                    Kind = SwarmEventRecord.KIND_CHAFF_DIED,
                    Position = position.ValueRO.Value
                });
                commandBuffer.DestroyEntity(entity);
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
}
```

- [ ] **Step 5: Compile check + commit**

Refresh Unity: zero errors, Burst compiles the four `[BurstCompile]` OnUpdates without warnings, 31 EditMode tests still PASS. (If `EntityQueryBuilder.Build(ref state)` or `EnabledRefRW` fail to resolve, the Entities version drifted — check against installed 1.4.4 API before adapting.)

```bash
git add "Assets/_neon/Scripts/Simulation"
git commit -m "feat: swarm sim - components, spawn/steer/damage/eval/death systems (M1)"
```

---

### Task 5: `ISwarmBridge` — the Layer-1 seam + Level-scope wiring

The single seam between worlds (spec §5.2, F4: bridge-only spatial queries). `NullSwarmBridge` registers as the session default in `GameplayServicesState`; the real `SwarmBridge` registers in the `Level` scope and shadows it (child-scope registrations win in VContainer), so scenes without a swarm inject safely.

**Files:**
- Modify: `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef`
- Create: `Assets/_neon/Scripts/Swarm/TargetRef.cs`
- Create: `Assets/_neon/Scripts/Swarm/ISwarmBridge.cs`
- Create: `Assets/_neon/Scripts/Swarm/NullSwarmBridge.cs`
- Create: `Assets/_neon/Scripts/Swarm/SwarmBridge.cs`
- Modify: `Assets/_neon/Scripts/Entities/EntitiesQueries.cs`
- Modify: `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs`
- Modify: `Assets/_neon/Scripts/Level/Level.cs`

- [ ] **Step 1: Reference the DOTS assemblies from `Neon`**

In `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef`, the `references` array currently reads:

```json
    "references": [
        "BrainlessLabs.Neon.Simulation",
        "Eflatun.SceneReference",
        "R3.Unity",
        "UniTask",
        "Unity.2D.PixelPerfect",
        "Unity.InputSystem",
        "VContainer"
    ],
```

Change it to:

```json
    "references": [
        "BrainlessLabs.Neon.Simulation",
        "Eflatun.SceneReference",
        "R3.Unity",
        "UniTask",
        "Unity.2D.PixelPerfect",
        "Unity.Collections",
        "Unity.Entities",
        "Unity.InputSystem",
        "Unity.Mathematics",
        "VContainer"
    ],
```

(Reference direction stays legal: `Neon → Simulation` + Unity packages. `Simulation` still references no BrainlessLabs assembly.)

- [ ] **Step 2: `TargetRef`**

`Assets/_neon/Scripts/Swarm/TargetRef.cs`:

```csharp
using Unity.Entities;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Unified target handle across both worlds: an ECS chaff entity OR a
    /// MonoBehaviour hero-tier GameObject. The engagement spine never knows which.
    /// </summary>
    public readonly struct TargetRef
    {
        public readonly Entity Entity;
        public readonly GameObject GameObject;
        public readonly Vector2 Position;

        public bool IsChaff => GameObject == null && Entity != Entity.Null;
        public bool IsValid => GameObject != null || Entity != Entity.Null;

        public TargetRef(Entity entity, Vector2 position)
        {
            Entity = entity;
            GameObject = null;
            Position = position;
        }

        public TargetRef(GameObject gameObject, Vector2 position)
        {
            Entity = Entity.Null;
            GameObject = gameObject;
            Position = position;
        }
    }
}
```

- [ ] **Step 3: The seam interface + null default**

`Assets/_neon/Scripts/Swarm/ISwarmBridge.cs`:

```csharp
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The single seam between the DOTS swarm and the MonoBehaviour world
    /// (spec §5.2): targeting queries, damage/finish commands. Bridge-only
    /// spatial truth for chaff (F4) — chaff have no colliders.
    /// </summary>
    public interface ISwarmBridge
    {
        /// <summary>Nearest living chaff inside the facing arc, or false.</summary>
        bool TryGetNearestHot(Vector2 origin, float facingSign, float arcDegrees, float range, out TargetRef target);

        /// <summary>Nearest Finish-Ready chaff to the origin (arc-free — the selector wants proximity), or false.</summary>
        bool TryGetNearestFinishReady(Vector2 origin, out TargetRef target);

        int CountHot();
        int CountFinishReady();

        /// <summary>Auto-engage chip on a chaff target (queued into the sim).</summary>
        void ApplyChip(in TargetRef target, int damage);

        /// <summary>
        /// A verb hitbox sweep against chaff. Finish-Ready chaff die as a FINISH
        /// (publishes EnemyFinished); others take verb damage. Returns true if any
        /// chaff was hit (feeds the whiff decision).
        /// </summary>
        bool ApplyVerbHit(Bounds hitBounds, AttackData attackData);
    }
}
```

`Assets/_neon/Scripts/Swarm/NullSwarmBridge.cs`:

```csharp
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>Session default for scenes without a swarm (menus, training hall).</summary>
    public sealed class NullSwarmBridge : ISwarmBridge
    {
        public bool TryGetNearestHot(Vector2 origin, float facingSign, float arcDegrees, float range, out TargetRef target)
        {
            target = default;
            return false;
        }

        public bool TryGetNearestFinishReady(Vector2 origin, out TargetRef target)
        {
            target = default;
            return false;
        }

        public int CountHot() => 0;
        public int CountFinishReady() => 0;

        public void ApplyChip(in TargetRef target, int damage)
        {
        }

        public bool ApplyVerbHit(Bounds hitBounds, AttackData attackData) => false;
    }
}
```

- [ ] **Step 4: The real bridge**

`Assets/_neon/Scripts/Swarm/SwarmBridge.cs`:

```csharp
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using BrainlessLabs.Neon.Simulation;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Level-scoped bridge: pushes Mono world state into the sim, queries chaff,
    /// queues damage/kill commands, drains sim events. Ticks at order -10 so the
    /// engagement systems (0/10/20/30) always see a fresh sim view.
    /// </summary>
    public sealed class SwarmBridge : ISwarmBridge, IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = -10;
        private const float VERB_HIT_PADDING = 0.3f; // chaff have no colliders; pad the hitbox by an agent radius

        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly IEntitiesService _entities;
        private readonly SwarmConfig _config;

        private World _world;
        private Entity _controlEntity;
        private EntityQuery _chaffQuery;
        private EntityQuery _readyQuery;
        private EntityQuery _agentQuery;
        private bool _initialized;
        private bool _capLogged;

        public SwarmBridge(IGameplayClock clock, IGameplaySignals signals, IEntitiesService entities, SwarmConfig config)
        {
            _clock = clock;
            _signals = signals;
            _entities = entities;
            _config = config;
            _clock.Register(this, TICK_ORDER);
        }

        public void Tick(float deltaTime)
        {
            if (!_config.Enabled || !TryInitialize()) return;

            var entityManager = _world.EntityManager;
            var state = entityManager.GetComponentData<SwarmWorldState>(_controlEntity);

            var player = _entities.GetFirstByType(UNITTYPE.PLAYER).GameObject;
            if (player != null)
            {
                state.PlayerPosition = new float2(player.transform.position.x, player.transform.position.y);
                var actions = player.GetComponent<UnitActions>();
                if (actions != null) state.PlayerFacingSign = (int)actions.dir;
            }
            entityManager.SetComponentData(_controlEntity, state);

            // Drain sim events. M1 consumes nothing from chip-deaths (they are NOT
            // finishes — v0.4); feedback hooks arrive in M4.
            entityManager.GetBuffer<SwarmEventRecord>(_controlEntity).Clear();

            if (!_capLogged && CountHot() >= _config.ChaffCap)
            {
                _capLogged = true;
                Debug.Log($"[Swarm] Chaff cap reached ({_config.ChaffCap}).");
            }
        }

        public void Dispose()
        {
            _clock.Unregister(this);
            if (!_initialized || _world == null || !_world.IsCreated) return;

            var entityManager = _world.EntityManager;
            using var allAgents = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SwarmAgent>());
            entityManager.DestroyEntity(allAgents);
            if (entityManager.Exists(_controlEntity)) entityManager.DestroyEntity(_controlEntity);
        }

        public bool TryGetNearestHot(Vector2 origin, float facingSign, float arcDegrees, float range, out TargetRef target)
        {
            target = default;
            if (!_initialized) return false;

            using var entities = _chaffQuery.ToEntityArray(Allocator.Temp);
            using var positions = _chaffQuery.ToComponentDataArray<BeltPosition>(Allocator.Temp);

            float cosHalfArc = math.cos(math.radians(math.min(arcDegrees, 360f) * 0.5f));
            var facing = new float2(math.sign(facingSign == 0f ? 1f : facingSign), 0f);
            var originF = new float2(origin.x, origin.y);
            float bestDistanceSq = range * range;
            int bestIndex = -1;

            for (int i = 0; i < entities.Length; i++)
            {
                float2 toAgent = positions[i].Value - originF;
                float distanceSq = math.lengthsq(toAgent);
                if (distanceSq > bestDistanceSq || distanceSq < 1e-6f) continue;
                if (arcDegrees < 360f && math.dot(math.normalize(toAgent), facing) < cosHalfArc) continue;

                bestDistanceSq = distanceSq;
                bestIndex = i;
            }

            if (bestIndex < 0) return false;
            var p = positions[bestIndex].Value;
            target = new TargetRef(entities[bestIndex], new Vector2(p.x, p.y));
            return true;
        }

        public bool TryGetNearestFinishReady(Vector2 origin, out TargetRef target)
        {
            target = default;
            if (!_initialized) return false;

            using var entities = _readyQuery.ToEntityArray(Allocator.Temp);
            using var positions = _readyQuery.ToComponentDataArray<BeltPosition>(Allocator.Temp);

            var originF = new float2(origin.x, origin.y);
            float bestDistanceSq = float.MaxValue;
            int bestIndex = -1;

            for (int i = 0; i < entities.Length; i++)
            {
                float distanceSq = math.lengthsq(positions[i].Value - originF);
                if (distanceSq >= bestDistanceSq) continue;
                bestDistanceSq = distanceSq;
                bestIndex = i;
            }

            if (bestIndex < 0) return false;
            var p = positions[bestIndex].Value;
            target = new TargetRef(entities[bestIndex], new Vector2(p.x, p.y));
            return true;
        }

        public int CountHot() => _initialized ? _chaffQuery.CalculateEntityCount() : 0;
        public int CountFinishReady() => _initialized ? _readyQuery.CalculateEntityCount() : 0;

        public void ApplyChip(in TargetRef target, int damage)
        {
            if (!_initialized || !target.IsChaff) return;
            _world.EntityManager.GetBuffer<SwarmDamageCommand>(_controlEntity)
                .Add(new SwarmDamageCommand { Target = target.Entity, Amount = damage, IsChip = 1 });
        }

        public bool ApplyVerbHit(Bounds hitBounds, AttackData attackData)
        {
            if (!_initialized || attackData == null) return false;

            var entityManager = _world.EntityManager;
            using var entities = _chaffQuery.ToEntityArray(Allocator.Temp);
            using var positions = _chaffQuery.ToComponentDataArray<BeltPosition>(Allocator.Temp);

            var min = new float2(hitBounds.min.x - VERB_HIT_PADDING, hitBounds.min.y - VERB_HIT_PADDING);
            var max = new float2(hitBounds.max.x + VERB_HIT_PADDING, hitBounds.max.y + VERB_HIT_PADDING);
            var damageBuffer = entityManager.GetBuffer<SwarmDamageCommand>(_controlEntity);
            var killBuffer = entityManager.GetBuffer<SwarmKillCommand>(_controlEntity);
            bool hitAny = false;

            for (int i = 0; i < entities.Length; i++)
            {
                var p = positions[i].Value;
                if (p.x < min.x || p.x > max.x || p.y < min.y || p.y > max.y) continue;

                hitAny = true;
                if (entityManager.IsComponentEnabled<FinishReadyTag>(entities[i]))
                {
                    // Single-verb chaff finish (spec §7 M1): any verb connect = finish.
                    killBuffer.Add(new SwarmKillCommand { Target = entities[i] });
                    _signals.Publish(new EnemyFinished(new Vector2(p.x, p.y), wasChaff: true));
                }
                else
                {
                    damageBuffer.Add(new SwarmDamageCommand { Target = entities[i], Amount = attackData.damage, IsChip = 0 });
                }
            }

            return hitAny;
        }

        private bool TryInitialize()
        {
            if (_initialized) return _world != null && _world.IsCreated;

            _world = World.DefaultGameObjectInjectionWorld;
            if (_world == null || !_world.IsCreated) return false;

            var entityManager = _world.EntityManager;
            _controlEntity = entityManager.CreateEntity(
                typeof(SwarmWorldState), typeof(SwarmDamageCommand),
                typeof(SwarmKillCommand), typeof(SwarmEventRecord));
            entityManager.SetComponentData(_controlEntity, new SwarmWorldState
            {
                PlayerFacingSign = 1f,
                ChaffCap = _config.ChaffCap,
                AmbientCap = _config.AmbientCap,
                SpawnRatePerSecond = _config.SpawnRatePerSecond,
                ChaffMaxHealth = _config.ChaffMaxHealth,
                ChaffMoveSpeed = _config.ChaffMoveSpeed,
                BeltMin = new float2(_config.BeltMin.x, _config.BeltMin.y),
                BeltMax = new float2(_config.BeltMax.x, _config.BeltMax.y),
                FinishReadyThreshold = _config.FinishReadyThreshold,
                Enabled = 1
            });

            _chaffQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BeltPosition>(), ComponentType.ReadOnly<SwarmHealth>());
            _readyQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BeltPosition>(), ComponentType.ReadOnly<FinishReadyTag>());

            _initialized = true;
            Debug.Log($"[Swarm] Bridge online: chaff cap {_config.ChaffCap}, ambient cap {_config.AmbientCap}.");
            return true;
        }
    }
}
```

- [ ] **Step 5: Hero-tier arc query (additive extension, mirrors the existing `EntitiesQueries` style)**

Append inside the `EntitiesQueries` class in `Assets/_neon/Scripts/Entities/EntitiesQueries.cs` (before the closing brace):

```csharp
        /// <summary>
        /// Nearest living tracked enemy inside the facing arc (auto-engage targeting,
        /// hero-tier side of the bridge query). facingSign: +1 right, -1 left.
        /// </summary>
        public static GameObject GetNearestEnemyInArc(this IEntitiesService entities,
            Vector2 origin, float facingSign, float arcDegrees, float range)
        {
            if (entities == null) return null;

            float cosHalfArc = Mathf.Cos(Mathf.Min(arcDegrees, 360f) * 0.5f * Mathf.Deg2Rad);
            var facing = new Vector2(Mathf.Sign(facingSign == 0f ? 1f : facingSign), 0f);
            float bestSqrDistance = range * range;
            GameObject best = null;

            var enemies = entities.GetByType(UNITTYPE.ENEMY);
            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i].GameObject;
                if (go == null) continue;
                if (go.GetComponent<HealthSystem>()?.isDead == true) continue;

                Vector2 toEnemy = (Vector2)go.transform.position - origin;
                float sqrDistance = toEnemy.sqrMagnitude;
                if (sqrDistance > bestSqrDistance || sqrDistance < 1e-6f) continue;
                if (arcDegrees < 360f && Vector2.Dot(toEnemy.normalized, facing) < cosHalfArc) continue;

                bestSqrDistance = sqrDistance;
                best = go;
            }
            return best;
        }
```

- [ ] **Step 6: Register the null default in `GameplayServicesState`**

Add to `RegisterTypes` (after `RegisterMomentumSystem(builder);`):

```csharp
            RegisterNullSwarmBridge(builder);
```

and the helper:

```csharp
        // Session default: scenes without a swarm (menus, training hall) still
        // inject ISwarmBridge safely. Level scopes shadow this with SwarmBridge.
        private static void RegisterNullSwarmBridge(IContainerBuilder builder)
        {
            builder.Register<NullSwarmBridge>(Lifetime.Singleton)
                .As<ISwarmBridge>();
        }
```

- [ ] **Step 7: Register the real bridge in the `Level` scope**

In `Assets/_neon/Scripts/Level/Level.cs`, replace the existing `Configure` method:

```csharp
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterBuildCallback(container =>
            {
                // Inject all scene MonoBehaviours with [Inject] attributes
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    container.InjectGameObject(root);
                }
            });
        }
```

with:

```csharp
        protected override void Configure(IContainerBuilder builder)
        {
            RegisterEngagementSystems(builder);

            builder.RegisterBuildCallback(container =>
            {
                // Inject all scene MonoBehaviours with [Inject] attributes
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    container.InjectGameObject(root);
                }

                // Eager-create the per-level engagement systems: nothing else
                // resolves them, and they must exist to register into the clock.
                if (_configuration != null)
                {
                    container.Resolve<ISwarmBridge>();
                }
            });
        }

        // Per-run systems live in the Level scope (spec §4.3) and tear down with it.
        private void RegisterEngagementSystems(IContainerBuilder builder)
        {
            if (_configuration == null) return;

            builder.RegisterInstance(SwarmConfig.From(_configuration, this));
            builder.RegisterInstance(EngagementConfig.FromSettings());
            builder.Register<SwarmBridge>(Lifetime.Scoped).As<ISwarmBridge>();
        }
```

(Tasks 8–10 add more registrations + eager resolves to these two methods.)

- [ ] **Step 8: Enable the swarm for Level1 + runtime probe (Recipe 4)**

1. In the editor, select the `Level` GameObject in `03_Level1` → find its `_configuration` asset (`LevelConfigurationAsset`) → in the new **Swarm** block tick **EnableSwarm** (leave caps at defaults 120/100).
2. Refresh/compile: zero errors, 31 tests PASS.
3. Boot play-test into Level1. Expected console: `[Swarm] Bridge online: chaff cap 120, ambient cap 100.` then within ~15s `[Swarm] Chaff cap reached (120).` No errors.
4. Optional deep check: Window → Entities → Hierarchy shows growing entity count (nothing renders yet — that's Task 6).
5. Exit Play mode.

- [ ] **Step 9: Commit**

```bash
git add "Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef" "Assets/_neon/Scripts/Swarm" "Assets/_neon/Scripts/Entities/EntitiesQueries.cs" "Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs" "Assets/_neon/Scripts/Level/Level.cs"
git commit -m "feat: SwarmBridge seam - unified targeting/damage/finish across worlds (M1)"
```

Also commit the Level1 config change (find the modified `.asset` via `git status`):

```bash
git add -A "Assets/_neon"
git commit -m "chore: enable swarm density block on Level1 config"
```

---

### Task 6: Swarm rendering — proxies + instanced ambient in `03_Level1`

The spike verdict's render recipe, promoted to real code: stable entity↔proxy mapping, Finish-Ready glow tint on proxies (the hot-vs-ambient sign, spec §5.5), `DrawMeshInstanced` ambient with the instancing-safe shader.

**Files:**
- Create: `Assets/_neon/Shaders/NeonInstancedUnlit.shader`
- Create: `Assets/_neon/Scripts/Swarm/SwarmRenderRig.cs`
- Editor: material `Assets/_neon/Materials/SwarmAmbient.mat`, scene wiring in `03_Level1`

- [ ] **Step 1: Promote the instanced shader**

`Assets/_neon/Shaders/NeonInstancedUnlit.shader` (spike pattern, renamed — the spike file stays untouched):

```csharp
// Instancing-safe unlit alpha-blended shader for the BUILT-IN render pipeline.
// Required for Graphics.DrawMeshInstanced: Sprites/Default reads per-instance
// arrays only the SpriteRenderer batcher populates (draws fully transparent),
// and URP shaders do nothing while no SRP asset is assigned.
Shader "Neon/InstancedUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _Color;
            }
            ENDCG
        }
    }
}
```

- [ ] **Step 2: The render rig**

`Assets/_neon/Scripts/Swarm/SwarmRenderRig.cs`:

```csharp
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using BrainlessLabs.Neon.Simulation;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Pure render projection of the swarm sim (no DI, no gameplay):
    /// hot chaff → pooled SpriteRenderer proxies with STABLE entity↔proxy mapping
    /// (spike verdict) + Finish-Ready glow tint; ambient → DrawMeshInstanced quads.
    /// Place one in each swarm-enabled level scene.
    /// </summary>
    public class SwarmRenderRig : MonoBehaviour
    {
        [Header("Hot chaff proxies")]
        [SerializeField] private Sprite _chaffSprite;
        [SerializeField] private Color _hotColor = new(1f, 0.35f, 0.65f, 1f);
        [SerializeField] private Color _finishReadyColor = new(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private int _proxyCapacity = 150;

        [Header("Ambient instancing (built-in RP: DrawMeshInstanced + Neon/InstancedUnlit)")]
        [SerializeField] private Material _ambientMaterial;
        [SerializeField] private float _ambientSize = 0.8f;

        private Transform[] _proxies;
        private SpriteRenderer[] _proxyRenderers;
        private readonly Dictionary<Entity, int> _entityToProxy = new();
        private readonly Stack<int> _freeProxies = new();
        private readonly HashSet<Entity> _seenThisFrame = new();
        private readonly HashSet<Entity> _readySet = new();
        private readonly List<Entity> _releaseScratch = new();

        private EntityQuery _chaffQuery;
        private EntityQuery _readyQuery;
        private EntityQuery _ambientQuery;
        private Mesh _quad;
        private Matrix4x4[] _ambientMatrices;
        private bool _ready;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || _chaffSprite == null)
            {
                Debug.LogError("[Swarm] SwarmRenderRig: missing ECS world or chaff sprite.");
                enabled = false;
                return;
            }
            if (_ambientMaterial != null && !_ambientMaterial.enableInstancing)
            {
                Debug.LogError("[Swarm] SwarmRenderRig: ambient material must have GPU instancing enabled.");
            }

            var entityManager = world.EntityManager;
            _chaffQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BeltPosition>(), ComponentType.ReadOnly<SwarmHealth>());
            _readyQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BeltPosition>(), ComponentType.ReadOnly<FinishReadyTag>());
            _ambientQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SwarmAgent, BeltPosition>()
                .WithNone<SwarmHealth>()
                .Build(entityManager);

            _proxies = new Transform[_proxyCapacity];
            _proxyRenderers = new SpriteRenderer[_proxyCapacity];
            for (int i = 0; i < _proxyCapacity; i++)
            {
                var proxy = new GameObject($"ChaffProxy_{i}");
                proxy.transform.SetParent(transform, worldPositionStays: false);
                var spriteRenderer = proxy.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = _chaffSprite;
                spriteRenderer.color = _hotColor;
                proxy.SetActive(false);
                _proxies[i] = proxy.transform;
                _proxyRenderers[i] = spriteRenderer;
                _freeProxies.Push(i);
            }

            _quad = BuildQuad();
            _ready = true;
        }

        private void LateUpdate()
        {
            if (!_ready) return;
            SyncProxies();
            DrawAmbient();
        }

        private void SyncProxies()
        {
            using var entities = _chaffQuery.ToEntityArray(Allocator.Temp);
            using var positions = _chaffQuery.ToComponentDataArray<BeltPosition>(Allocator.Temp);

            _readySet.Clear();
            using (var readyEntities = _readyQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < readyEntities.Length; i++) _readySet.Add(readyEntities[i]);
            }

            _seenThisFrame.Clear();
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                _seenThisFrame.Add(entity);

                if (!_entityToProxy.TryGetValue(entity, out int index))
                {
                    if (_freeProxies.Count == 0) continue; // cap misconfigured above capacity
                    index = _freeProxies.Pop();
                    _entityToProxy[entity] = index;
                    _proxies[index].gameObject.SetActive(true);
                }

                var p = positions[i].Value;
                _proxies[index].position = new Vector3(p.x, p.y, 0f);
                _proxyRenderers[index].color = _readySet.Contains(entity) ? _finishReadyColor : _hotColor;
            }

            _releaseScratch.Clear();
            foreach (var pair in _entityToProxy)
            {
                if (!_seenThisFrame.Contains(pair.Key)) _releaseScratch.Add(pair.Key);
            }
            foreach (var dead in _releaseScratch)
            {
                int index = _entityToProxy[dead];
                _entityToProxy.Remove(dead);
                _proxies[index].gameObject.SetActive(false);
                _freeProxies.Push(index);
            }
        }

        private void DrawAmbient()
        {
            if (_ambientMaterial == null) return;

            using var positions = _ambientQuery.ToComponentDataArray<BeltPosition>(Allocator.Temp);
            if (positions.Length == 0) return;

            if (_ambientMatrices == null || _ambientMatrices.Length < positions.Length)
            {
                _ambientMatrices = new Matrix4x4[positions.Length];
            }

            var scale = new Vector3(_ambientSize, _ambientSize, 1f);
            for (int i = 0; i < positions.Length; i++)
            {
                var p = positions[i].Value;
                // z = 1: ambient behind the chaff proxies (z = 0).
                _ambientMatrices[i] = Matrix4x4.TRS(new Vector3(p.x, p.y, 1f), Quaternion.identity, scale);
            }

            Graphics.DrawMeshInstanced(_quad, 0, _ambientMaterial, _ambientMatrices, positions.Length);
        }

        private static Mesh BuildQuad()
        {
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f)
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                },
                triangles = new[] { 0, 2, 1, 2, 3, 1 }
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
```

- [ ] **Step 3: Material + scene wiring (editor work, not in Play mode)**

1. Create folder `Assets/_neon/Materials/` if missing. Right-click → Create → Material → name `SwarmAmbient`. Shader: **Neon/InstancedUnlit**. Texture: `Assets/_neon/Sprites/Effects/HitEffect.png` (placeholder). Tint: mid-grey ~`#808080` at ~60% alpha (ambient must read *muted* vs hot — spec §5.5). Tick **Enable GPU Instancing**.
2. Open `Assets/_neon/Scenes/Game/03_Level1.unity`. Add a root GameObject `SwarmRenderRig` at origin with the `SwarmRenderRig` component. Assign: **Chaff Sprite** = `HitEffect.png` (placeholder — swap for a humanoid silhouette when art lands), **Ambient Material** = `SwarmAmbient`. Save the scene.

- [ ] **Step 4: Runtime gate (Recipe 4)**

Boot into Level1:
1. Within seconds: a muted instanced crowd (ambient) + saturated pink chaff proxies flooding in from both belt ends and converging on the player. Chaff crowd around the player at arm's length.
2. Game view **Stats** overlay: ambient collapses into a handful of batches (NOT ~100). Remember: MCP screenshots won't show the ambient — judge in the Game view.
3. FPS sanity: no visible hitching (formal R1 measurement is Task 12).
4. Exit Play mode.

- [ ] **Step 5: Commit**

```bash
git add "Assets/_neon/Shaders" "Assets/_neon/Shaders.meta" "Assets/_neon/Scripts/Swarm/SwarmRenderRig.cs" "Assets/_neon/Scripts/Swarm/SwarmRenderRig.cs.meta" "Assets/_neon/Materials" "Assets/_neon/Materials.meta" "Assets/_neon/Scenes/Game/03_Level1.unity"
git commit -m "feat: swarm render rig - stable proxy pool + instanced ambient (M1)"
```

---

### Task 7: AI_Active spawn-gap fix

The known M1 gap (`neon-troubleshooting`, folded into M1 by spec §3): `SpawnerService.SpawnUnit` never enables `EnemyBehaviour.AI_Active`, so spawned enemies spot the player but never act. The on-switch mirrors `EntitiesQueries.DisableAllEnemyAI` (the existing off-switch).

**Files:**
- Modify: `Assets/_neon/Scripts/Spawner/SpawnerService.cs`

- [ ] **Step 1: Enable AI on wave-spawned enemies**

In `SpawnerService.SpawnWaveEnemy`, change:

```csharp
            var enemy = SpawnUnit(entry.UnitDefinition, spawnPos, spawnDir);
            if (enemy == null) return;

            int entityId = _entitiesService.Register(enemy, UNITTYPE.ENEMY, entry.UnitDefinition);
            _waveEntityIds.Add(entityId);
```

to:

```csharp
            var enemy = SpawnUnit(entry.UnitDefinition, spawnPos, spawnDir);
            if (enemy == null) return;

            // AI_Active spawn-gap fix (neon-troubleshooting): spawned enemies never had
            // their AI enabled — only scene-placed values worked. Mirrors the off-switch
            // in EntitiesQueries.DisableAllEnemyAI.
            var enemyBehaviour = enemy.GetComponent<EnemyBehaviour>();
            if (enemyBehaviour != null) enemyBehaviour.AI_Active = true;

            int entityId = _entitiesService.Register(enemy, UNITTYPE.ENEMY, entry.UnitDefinition);
            _waveEntityIds.Add(entityId);
```

- [ ] **Step 2: Runtime verify (Recipe 4 — this is the gap's own reproduction test)**

Boot into Level1, walk right until `[Level] Wave 1/N started.`, then **do not attack**. Expected: spawned enemies close distance and attack the player within a few seconds (pre-fix behavior: they idle at spawn after spotting). Take the hit — that's the pass signal. Exit Play mode.

- [ ] **Step 3: Commit**

```bash
git add "Assets/_neon/Scripts/Spawner/SpawnerService.cs"
git commit -m "fix: enable AI_Active on wave-spawned enemies (M1 spawn-gap)"
```

---

### Task 8: `AutoEngageSystem` (test-first)

The "setup" half of the loop (spec §5.1): every rhythm interval, chip the nearest hot enemy in the facing arc — chaff via the bridge, hero-tier via `HealthSystem.SubstractHealth` (no hit-reaction state change: chip is pressure, verbs are the finish). All knobs live on the stat sheet, so Protocols/Momentum upgrade it for free.

**Files:**
- Modify: `Assets/_neon/Tests/EditMode/BrainlessLabs.Neon.Tests.EditMode.asmdef` (add `Unity.Entities` — `TargetRef` exposes an `Entity` field)
- Create: `Assets/_neon/Tests/EditMode/Fakes.cs`
- Create: `Assets/_neon/Scripts/Engagement/AutoEngageSystem.cs`
- Modify: `Assets/_neon/Scripts/Level/Level.cs`
- Test: `Assets/_neon/Tests/EditMode/AutoEngageSystemTests.cs`

- [ ] **Step 1: Add `Unity.Entities` to the test asmdef**

In `Assets/_neon/Tests/EditMode/BrainlessLabs.Neon.Tests.EditMode.asmdef`, change:

```json
    "references": [
        "BrainlessLabs.Neon",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
```

to:

```json
    "references": [
        "BrainlessLabs.Neon",
        "Unity.Entities",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
```

- [ ] **Step 2: Shared fakes**

`Assets/_neon/Tests/EditMode/Fakes.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    internal sealed class FakeEntitiesService : IEntitiesService
    {
        private readonly List<TrackedEntity> _entities = new();
        private int _nextId = 1;

        public event Action<TrackedEntity> OnEntityRegistered;
        public event Action<TrackedEntity> OnEntityUnregistered;

        public int Register(GameObject gameObject, UNITTYPE unitType, UnitDefinitionAsset definition = null)
        {
            var entity = new TrackedEntity { Id = _nextId++, UnitType = unitType, GameObject = gameObject, Definition = definition };
            _entities.Add(entity);
            OnEntityRegistered?.Invoke(entity);
            return entity.Id;
        }

        public void Unregister(int entityId)
        {
            int index = _entities.FindIndex(e => e.Id == entityId);
            if (index < 0) return;
            var entity = _entities[index];
            _entities.RemoveAt(index);
            OnEntityUnregistered?.Invoke(entity);
        }

        public IReadOnlyList<TrackedEntity> GetAll() => _entities;
        public IReadOnlyList<TrackedEntity> GetByType(UNITTYPE unitType) => _entities.FindAll(e => e.UnitType == unitType);
        public int GetCount(UNITTYPE unitType) => GetByType(unitType).Count;

        public TrackedEntity GetFirstByType(UNITTYPE unitType)
        {
            foreach (var entity in _entities)
            {
                if (entity.UnitType == unitType) return entity;
            }
            return default;
        }

        public bool TryGetByGameObject(GameObject gameObject, out TrackedEntity entity)
        {
            foreach (var candidate in _entities)
            {
                if (candidate.GameObject == gameObject)
                {
                    entity = candidate;
                    return true;
                }
            }
            entity = default;
            return false;
        }
    }

    internal sealed class FakeSwarmBridge : ISwarmBridge
    {
        public TargetRef? NearestHot;
        public TargetRef? NearestFinishReady;
        public int HotCount;
        public int FinishReadyCount;
        public readonly List<(TargetRef Target, int Damage)> ChipCalls = new();

        public bool TryGetNearestHot(Vector2 origin, float facingSign, float arcDegrees, float range, out TargetRef target)
        {
            if (NearestHot.HasValue)
            {
                target = NearestHot.Value;
                return true;
            }
            target = default;
            return false;
        }

        public bool TryGetNearestFinishReady(Vector2 origin, out TargetRef target)
        {
            if (NearestFinishReady.HasValue)
            {
                target = NearestFinishReady.Value;
                return true;
            }
            target = default;
            return false;
        }

        public int CountHot() => HotCount;
        public int CountFinishReady() => FinishReadyCount;

        public void ApplyChip(in TargetRef target, int damage) => ChipCalls.Add((target, damage));

        public bool ApplyVerbHit(Bounds hitBounds, AttackData attackData) => false;
    }
}
```

- [ ] **Step 3: Write the failing tests**

`Assets/_neon/Tests/EditMode/AutoEngageSystemTests.cs` (chaff path only — the hero-tier `SubstractHealth` path calls the injected audio service and is covered by this task's runtime gate instead):

```csharp
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class AutoEngageSystemTests
    {
        private GameplayClock _clock;
        private StatSystem _stats;
        private FakeEntitiesService _entities;
        private FakeSwarmBridge _bridge;
        private AutoEngageSystem _autoEngage;
        private GameObject _player;

        private static EngagementConfig TestConfig => new(
            ratePerSecond: 2f, chipDamage: 8, arcDegrees: 120f, range: 4f,
            finishReadyThreshold: 0.25f, finishReadyGlow: Color.yellow, whiffStaggerSeconds: 0.5f);

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _stats = new StatSystem();
            _entities = new FakeEntitiesService();
            _bridge = new FakeSwarmBridge();
            _player = new GameObject("TestPlayer");
            _entities.Register(_player, UNITTYPE.PLAYER);
            _autoEngage = new AutoEngageSystem(_clock, _stats, _entities, _bridge, TestConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _autoEngage.Dispose();
            Object.DestroyImmediate(_player);
        }

        private static TargetRef ChaffAt(float x) => new(new Entity { Index = 1, Version = 1 }, new Vector2(x, 0f));

        [Test]
        public void Fires_AtConfiguredRate()
        {
            _bridge.NearestHot = ChaffAt(1f);

            _clock.Advance(1f); // rate 2/s

            Assert.AreEqual(2, _bridge.ChipCalls.Count);
        }

        [Test]
        public void RateStat_DrivesCadence()
        {
            _bridge.NearestHot = ChaffAt(1f);
            _stats.Player.SetBase(StatId.AutoEngageRate, 4f);

            _clock.Advance(1f);

            Assert.AreEqual(4, _bridge.ChipCalls.Count);
        }

        [Test]
        public void RateStat_HardCappedAtSix()
        {
            _bridge.NearestHot = ChaffAt(1f);
            _stats.Player.SetBase(StatId.AutoEngageRate, 40f); // protocol-doc cap: 6/s

            _clock.Advance(1f);

            Assert.AreEqual(6, _bridge.ChipCalls.Count);
        }

        [Test]
        public void ChipDamage_ScaledByDamageMultiplier()
        {
            _bridge.NearestHot = ChaffAt(1f);
            var source = ModifierSource.Create("test");
            _stats.Player.SetBase(StatId.DamageMultiplier, 1f);
            _stats.Player.AddModifier(StatId.DamageMultiplier, StatOp.Mult, 2f, source);

            _clock.Advance(0.5f); // one shot at 2/s

            Assert.AreEqual(1, _bridge.ChipCalls.Count);
            Assert.AreEqual(16, _bridge.ChipCalls[0].Damage); // 8 × 2
        }

        [Test]
        public void NoTargets_DoesNothing()
        {
            Assert.DoesNotThrow(() => _clock.Advance(2f));
            Assert.AreEqual(0, _bridge.ChipCalls.Count);
        }

        [Test]
        public void NearerHero_WinsOverChaff_NoChipCall()
        {
            _bridge.NearestHot = ChaffAt(3f);
            var hero = new GameObject("TestEnemy");
            hero.transform.position = new Vector3(1f, 0f, 0f); // in front (facing defaults right)
            _entities.Register(hero, UNITTYPE.ENEMY);

            _clock.Advance(0.5f);

            Assert.AreEqual(0, _bridge.ChipCalls.Count); // chip went to the hero, not the bridge
            Object.DestroyImmediate(hero);
        }
    }
}
```

- [ ] **Step 4: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`AutoEngageSystem` does not exist yet). Proceed.

- [ ] **Step 5: Implement**

`Assets/_neon/Scripts/Engagement/AutoEngageSystem.cs`:

```csharp
using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The automated basic attack (spec §5.1) — a NEW attack; manual verbs untouched.
    /// Every rhythm interval, chips the nearest hot enemy in the player's facing arc:
    /// chaff via the bridge, hero-tier via HealthSystem.SubstractHealth (no hit-reaction
    /// state change — chip is pressure toward Finish-Ready; the verbs are the finish).
    /// All knobs read from the stat sheet each tick, so upgrades apply live.
    /// </summary>
    public sealed class AutoEngageSystem : IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 0;      // reserved band (IGameplayClock doc)
        private const float RATE_HARD_CAP = 6f;    // protocol-doc guardrail: auto-rate ≤ 6/s
        private const float ARC_HARD_CAP = 180f;   // protocol-doc guardrail: arc ≤ 180°

        private readonly IGameplayClock _clock;
        private readonly IStatSystem _stats;
        private readonly IEntitiesService _entities;
        private readonly ISwarmBridge _bridge;
        private float _accumulator;

        public AutoEngageSystem(IGameplayClock clock, IStatSystem stats, IEntitiesService entities,
            ISwarmBridge bridge, EngagementConfig config)
        {
            _clock = clock;
            _stats = stats;
            _entities = entities;
            _bridge = bridge;

            // Seed the tunable bases (idempotent — Protocols modify via modifiers, not bases).
            _stats.Player.SetBase(StatId.AutoEngageRate, config.AutoEngageRatePerSecond);
            _stats.Player.SetBase(StatId.AutoEngageDamage, config.AutoEngageChipDamage);
            _stats.Player.SetBase(StatId.AutoEngageArcDegrees, config.AutoEngageArcDegrees);
            _stats.Player.SetBase(StatId.AutoEngageRange, config.AutoEngageRange);

            _clock.Register(this, TICK_ORDER);
        }

        public void Tick(float deltaTime)
        {
            float rate = Mathf.Clamp(_stats.Player.GetValue(StatId.AutoEngageRate), 0.01f, RATE_HARD_CAP);
            float interval = 1f / rate;
            _accumulator += deltaTime;

            while (_accumulator >= interval)
            {
                _accumulator -= interval;
                FireChip();
            }
        }

        public void Dispose()
        {
            _clock.Unregister(this);
        }

        private void FireChip()
        {
            var player = _entities.GetFirstByType(UNITTYPE.PLAYER).GameObject;
            if (player == null) return;

            Vector2 origin = player.transform.position;
            var actions = player.GetComponent<UnitActions>();
            float facingSign = actions != null ? (int)actions.dir : 1f;
            float arc = Mathf.Min(_stats.Player.GetValue(StatId.AutoEngageArcDegrees), ARC_HARD_CAP);
            float range = _stats.Player.GetValue(StatId.AutoEngageRange);

            float damageMultiplier = _stats.Player.GetValue(StatId.DamageMultiplier);
            if (damageMultiplier <= 0f) damageMultiplier = 1f;
            int damage = Mathf.Max(1, Mathf.RoundToInt(_stats.Player.GetValue(StatId.AutoEngageDamage) * damageMultiplier));

            // Nearest hot target across BOTH worlds — bridge answers for chaff,
            // EntitiesQueries for hero-tier; nearest wins. The spine never knows which world.
            bool hasChaff = _bridge.TryGetNearestHot(origin, facingSign, arc, range, out var chaffTarget);
            var heroTarget = _entities.GetNearestEnemyInArc(origin, facingSign, arc, range);

            float chaffSqrDistance = hasChaff ? (chaffTarget.Position - origin).sqrMagnitude : float.MaxValue;
            float heroSqrDistance = heroTarget != null
                ? ((Vector2)heroTarget.transform.position - origin).sqrMagnitude
                : float.MaxValue;

            if (!hasChaff && heroTarget == null) return;

            if (chaffSqrDistance <= heroSqrDistance)
            {
                _bridge.ApplyChip(in chaffTarget, damage);
            }
            else
            {
                heroTarget.GetComponent<HealthSystem>()?.SubstractHealth(damage);
            }
        }
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: 37/37 PASS (31 + 6 AutoEngage).

- [ ] **Step 7: Register in the Level scope**

In `Level.RegisterEngagementSystems`, after the `SwarmBridge` registration add:

```csharp
            builder.Register<AutoEngageSystem>(Lifetime.Scoped).AsSelf();
```

and in the `Configure` build callback, after `container.Resolve<ISwarmBridge>();` add:

```csharp
                    container.Resolve<AutoEngageSystem>();
```

- [ ] **Step 8: Runtime gate (Recipe 4)**

Boot into Level1, stand still facing the incoming chaff:
1. Chaff in front of the player turn **gold** (Finish-Ready tint) after ~2 chips and **stay alive at 1 HP** — chip pushes toward Finish-Ready, never kills (the whole loop depends on this: auto-engage softens, YOUR verb finishes).
2. Walk to the wave trigger; once hero-tier enemies spawn and close in, their HP bars tick down from chips too (hero path, untestable in EditMode).
3. Exit Play mode.

- [ ] **Step 9: Commit**

```bash
git add "Assets/_neon/Tests/EditMode/BrainlessLabs.Neon.Tests.EditMode.asmdef" "Assets/_neon/Tests/EditMode/Fakes.cs" "Assets/_neon/Tests/EditMode/Fakes.cs.meta" "Assets/_neon/Tests/EditMode/AutoEngageSystemTests.cs" "Assets/_neon/Tests/EditMode/AutoEngageSystemTests.cs.meta" "Assets/_neon/Scripts/Engagement/AutoEngageSystem.cs" "Assets/_neon/Scripts/Engagement/AutoEngageSystem.cs.meta" "Assets/_neon/Scripts/Level/Level.cs"
git commit -m "feat: AutoEngageSystem - stat-driven rhythm chip across both worlds (M1)"
```

---

### Task 9: Finish-Ready hero-tier marking + the single-prompt selector (test-first)

`FinishReadySystem` (order 10) marks hero-tier enemies ready at ≤25% HP **or knocked down**; `FinishReadySelector` (order 20) enforces the R7 one-prompt rule across both worlds and publishes the prompt + "+N ready" count.

**Files:**
- Create: `Assets/_neon/Scripts/Engagement/FinishReadyMarker.cs`
- Create: `Assets/_neon/Scripts/Engagement/FinishReadySystem.cs`
- Create: `Assets/_neon/Scripts/Engagement/FinishReadySelector.cs`
- Modify: `Assets/_neon/Scripts/Level/Level.cs`
- Test: `Assets/_neon/Tests/EditMode/FinishReadySelectorTests.cs`

- [ ] **Step 1: Write the failing tests**

`Assets/_neon/Tests/EditMode/FinishReadySelectorTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using R3;
using Unity.Entities;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class FinishReadySelectorTests
    {
        private GameplayClock _clock;
        private GameplaySignals _signals;
        private FakeEntitiesService _entities;
        private FakeSwarmBridge _bridge;
        private FinishReadySelector _selector;
        private GameObject _player;
        private readonly List<FinishReadyPromptChanged> _received = new();
        private System.IDisposable _subscription;
        private readonly List<GameObject> _spawned = new();

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _signals = new GameplaySignals();
            _entities = new FakeEntitiesService();
            _bridge = new FakeSwarmBridge();
            _player = Spawn("TestPlayer", Vector2.zero);
            _entities.Register(_player, UNITTYPE.PLAYER);
            _selector = new FinishReadySelector(_clock, _signals, _entities, _bridge);
            _received.Clear();
            _subscription = _signals.On<FinishReadyPromptChanged>().Subscribe(e => _received.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            _subscription?.Dispose();
            _selector.Dispose();
            _signals.Dispose();
            foreach (var go in _spawned) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        private GameObject Spawn(string name, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            _spawned.Add(go);
            return go;
        }

        private GameObject SpawnReadyEnemy(Vector2 position)
        {
            var enemy = Spawn("ReadyEnemy", position);
            var marker = enemy.AddComponent<FinishReadyMarker>();
            marker.SetReady(true, Color.yellow);
            _entities.Register(enemy, UNITTYPE.ENEMY);
            return enemy;
        }

        [Test]
        public void NoReadyTargets_PublishesNoTarget()
        {
            _clock.Advance(0.016f);

            Assert.AreEqual(1, _received.Count);
            Assert.IsFalse(_received[0].HasTarget);
            Assert.AreEqual(0, _received[0].ReadyCount);
        }

        [Test]
        public void ReadyHero_IsPrompted()
        {
            SpawnReadyEnemy(new Vector2(2f, 0f));

            _clock.Advance(0.016f);

            Assert.IsTrue(_received[_received.Count - 1].HasTarget);
            Assert.AreEqual(new Vector2(2f, 0f), _received[_received.Count - 1].TargetPosition);
            Assert.AreEqual(1, _received[_received.Count - 1].ReadyCount);
        }

        [Test]
        public void OnePromptOnly_NearestHeroWins()
        {
            SpawnReadyEnemy(new Vector2(5f, 0f));
            SpawnReadyEnemy(new Vector2(1f, 0f));

            _clock.Advance(0.016f);

            var last = _received[_received.Count - 1];
            Assert.AreEqual(new Vector2(1f, 0f), last.TargetPosition); // single prompt, nearest
            Assert.AreEqual(2, last.ReadyCount);                      // but the count shows all
        }

        [Test]
        public void NearerChaff_WinsThePrompt()
        {
            SpawnReadyEnemy(new Vector2(5f, 0f));
            _bridge.NearestFinishReady = new TargetRef(new Entity { Index = 1, Version = 1 }, new Vector2(1f, 0f));
            _bridge.FinishReadyCount = 3;

            _clock.Advance(0.016f);

            var last = _received[_received.Count - 1];
            Assert.AreEqual(new Vector2(1f, 0f), last.TargetPosition);
            Assert.AreEqual(4, last.ReadyCount); // 1 hero + 3 chaff
        }

        [Test]
        public void UnchangedState_DoesNotRepublish()
        {
            SpawnReadyEnemy(new Vector2(2f, 0f));

            _clock.Advance(0.016f);
            _clock.Advance(0.016f);
            _clock.Advance(0.016f);

            Assert.AreEqual(1, _received.Count);
        }
    }
}
```

- [ ] **Step 2: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`FinishReadyMarker`/`FinishReadySelector` do not exist yet). Proceed.

- [ ] **Step 3: Implement the marker**

`Assets/_neon/Scripts/Engagement/FinishReadyMarker.cs`:

```csharp
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Hero-tier Finish-Ready flag + glow sign (spec §5.1). Added at runtime by
    /// FinishReadySystem; FinishResolver reads IsReady at the hit seam.
    /// </summary>
    public class FinishReadyMarker : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private Color _originalColor;
        private bool _hasRenderer;

        public bool IsReady { get; private set; }

        private void Awake()
        {
            var settings = GetComponent<UnitSettings>();
            _spriteRenderer = settings != null && settings.spriteRenderer != null
                ? settings.spriteRenderer
                : GetComponent<SpriteRenderer>();
            _hasRenderer = _spriteRenderer != null;
            if (_hasRenderer) _originalColor = _spriteRenderer.color;
        }

        public void SetReady(bool ready, Color glowColor)
        {
            if (IsReady == ready) return;
            IsReady = ready;
            if (_hasRenderer) _spriteRenderer.color = ready ? glowColor : _originalColor;
        }
    }
}
```

- [ ] **Step 4: Implement the marking system**

`Assets/_neon/Scripts/Engagement/FinishReadySystem.cs`:

```csharp
using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Marks hero-tier enemies Finish-Ready at ≤ threshold HP or while knocked
    /// down (spec §5.1: "≤25% HP (or staggered)"). Chaff-side marking lives in
    /// the sim's FinishReadyEvalSystem.
    /// </summary>
    public sealed class FinishReadySystem : IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 10; // reserved band (IGameplayClock doc)

        private readonly IGameplayClock _clock;
        private readonly IEntitiesService _entities;
        private readonly EngagementConfig _config;

        public FinishReadySystem(IGameplayClock clock, IEntitiesService entities, EngagementConfig config)
        {
            _clock = clock;
            _entities = entities;
            _config = config;
            _clock.Register(this, TICK_ORDER);
        }

        public void Tick(float deltaTime)
        {
            var enemies = _entities.GetByType(UNITTYPE.ENEMY);
            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i].GameObject;
                if (go == null) continue;

                var health = go.GetComponent<HealthSystem>();
                if (health == null) continue;

                var marker = go.GetComponent<FinishReadyMarker>();
                if (marker == null) marker = go.AddComponent<FinishReadyMarker>();

                var state = go.GetComponent<UnitStateMachine>()?.GetCurrentState();
                bool staggered = state is UnitKnockDown || state is UnitKnockDownGrounded;
                bool ready = !health.isDead
                    && (health.healthPercentage <= _config.FinishReadyHealthThreshold || staggered);
                marker.SetReady(ready, _config.FinishReadyGlow);
            }
        }

        public void Dispose()
        {
            _clock.Unregister(this);
        }
    }
}
```

- [ ] **Step 5: Implement the selector**

`Assets/_neon/Scripts/Engagement/FinishReadySelector.cs`:

```csharp
using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Picks the SINGLE highest-priority Finish-Ready target across both worlds
    /// (R7 one-prompt rule): nearest to the player. Publishes the prompt + the
    /// "+N ready" count; only publishes on change.
    /// </summary>
    public sealed class FinishReadySelector : IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 20; // reserved band (IGameplayClock doc)
        private const float REPUBLISH_POSITION_SQR = 0.04f;

        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly IEntitiesService _entities;
        private readonly ISwarmBridge _bridge;

        private bool _lastHasTarget;
        private Vector2 _lastPosition;
        private int _lastCount = -1;

        public FinishReadySelector(IGameplayClock clock, IGameplaySignals signals,
            IEntitiesService entities, ISwarmBridge bridge)
        {
            _clock = clock;
            _signals = signals;
            _entities = entities;
            _bridge = bridge;
            _clock.Register(this, TICK_ORDER);
        }

        public void Tick(float deltaTime)
        {
            var player = _entities.GetFirstByType(UNITTYPE.PLAYER).GameObject;
            if (player == null)
            {
                PublishIfChanged(false, Vector2.zero, 0);
                return;
            }
            Vector2 origin = player.transform.position;

            GameObject bestHero = null;
            float bestHeroSqrDistance = float.MaxValue;
            int heroReadyCount = 0;
            var enemies = _entities.GetByType(UNITTYPE.ENEMY);
            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i].GameObject;
                if (go == null) continue;
                var marker = go.GetComponent<FinishReadyMarker>();
                if (marker == null || !marker.IsReady) continue;

                heroReadyCount++;
                float sqrDistance = ((Vector2)go.transform.position - origin).sqrMagnitude;
                if (sqrDistance < bestHeroSqrDistance)
                {
                    bestHeroSqrDistance = sqrDistance;
                    bestHero = go;
                }
            }

            bool hasChaff = _bridge.TryGetNearestFinishReady(origin, out var chaffTarget);
            float chaffSqrDistance = hasChaff ? (chaffTarget.Position - origin).sqrMagnitude : float.MaxValue;
            int readyCount = heroReadyCount + _bridge.CountFinishReady();

            if (bestHero == null && !hasChaff)
            {
                PublishIfChanged(false, Vector2.zero, readyCount);
                return;
            }

            Vector2 targetPosition = chaffSqrDistance <= bestHeroSqrDistance
                ? chaffTarget.Position
                : (Vector2)bestHero.transform.position;
            PublishIfChanged(true, targetPosition, readyCount);
        }

        public void Dispose()
        {
            _clock.Unregister(this);
        }

        private void PublishIfChanged(bool hasTarget, Vector2 position, int count)
        {
            bool changed = hasTarget != _lastHasTarget
                || count != _lastCount
                || (hasTarget && (position - _lastPosition).sqrMagnitude > REPUBLISH_POSITION_SQR);
            if (!changed) return;

            _lastHasTarget = hasTarget;
            _lastPosition = position;
            _lastCount = count;
            // Single-verb M1: the prompt verb is always PUNCH (tiered challenges are M2).
            _signals.Publish(new FinishReadyPromptChanged(hasTarget, position, ATTACKTYPE.PUNCH, count));
        }
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: 42/42 PASS (37 + 5 selector).

- [ ] **Step 7: Register in the Level scope**

In `Level.RegisterEngagementSystems`, after the `AutoEngageSystem` line add:

```csharp
            builder.Register<FinishReadySystem>(Lifetime.Scoped).AsSelf();
            builder.Register<FinishReadySelector>(Lifetime.Scoped).AsSelf();
```

and in the build callback, after `container.Resolve<AutoEngageSystem>();` add:

```csharp
                    container.Resolve<FinishReadySystem>();
                    container.Resolve<FinishReadySelector>();
```

- [ ] **Step 8: Runtime gate (Recipe 4)**

Boot into Level1, trigger the first wave, fight an enemy down below ~25% HP (or knock one down with a kick): its sprite tints **gold** while low/downed and reverts if healed/stood-up-above-threshold. Chaff still glow from Task 8. Exit Play mode.

- [ ] **Step 9: Commit**

```bash
git add "Assets/_neon/Scripts/Engagement/FinishReadyMarker.cs" "Assets/_neon/Scripts/Engagement/FinishReadyMarker.cs.meta" "Assets/_neon/Scripts/Engagement/FinishReadySystem.cs" "Assets/_neon/Scripts/Engagement/FinishReadySystem.cs.meta" "Assets/_neon/Scripts/Engagement/FinishReadySelector.cs" "Assets/_neon/Scripts/Engagement/FinishReadySelector.cs.meta" "Assets/_neon/Tests/EditMode/FinishReadySelectorTests.cs" "Assets/_neon/Tests/EditMode/FinishReadySelectorTests.cs.meta" "Assets/_neon/Scripts/Level/Level.cs"
git commit -m "feat: Finish-Ready marking + single-prompt selector (M1, R7)"
```

---

### Task 10: Combat seam — `FinishResolver` + whiff cost (test-first)

The observe-and-tag hook (spec §5.1): verbs stay untouched behaviorally; we add (a) a chaff sweep to `CheckForHit`, (b) a whiff report on verb-state exit, (c) `FinishResolver` translating the static combat events into `EnemyFinished`/`VerbWhiffed` + the 0.5s stagger. Hero-tier M1 finish rule: a player verb hit that **kills** a Finish-Ready enemy. Chaff finishes already resolve inside `SwarmBridge.ApplyVerbHit` (Task 5).

**Files:**
- Modify: `Assets/_neon/Scripts/Units/UnitActions.cs`
- Modify: `Assets/_neon/Scripts/Units/PlayerStates/PlayerAttack.cs`
- Modify: `Assets/_neon/Scripts/Units/PlayerStates/PlayerWeaponAttack.cs`
- Create: `Assets/_neon/Scripts/Units/PlayerStates/PlayerWhiffStagger.cs`
- Create: `Assets/_neon/Scripts/Engagement/FinishResolver.cs`
- Modify: `Assets/_neon/Scripts/Level/Level.cs`
- Test: `Assets/_neon/Tests/EditMode/FinishResolverTests.cs`

- [ ] **Step 1: Write the failing tests**

`Assets/_neon/Tests/EditMode/FinishResolverTests.cs` (handlers are public because the static `UnitActions` events cannot be raised from a test):

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class FinishResolverTests
    {
        private GameplayClock _clock;
        private GameplaySignals _signals;
        private FinishResolver _resolver;
        private readonly List<GameObject> _spawned = new();
        private readonly List<EnemyFinished> _finishes = new();
        private readonly List<VerbWhiffed> _whiffs = new();
        private System.IDisposable _finishSub;
        private System.IDisposable _whiffSub;

        private static EngagementConfig TestConfig => new(
            ratePerSecond: 1.5f, chipDamage: 8, arcDegrees: 120f, range: 4f,
            finishReadyThreshold: 0.25f, finishReadyGlow: Color.yellow, whiffStaggerSeconds: 0.5f);

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _signals = new GameplaySignals();
            _resolver = new FinishResolver(_clock, _signals, TestConfig);
            _finishes.Clear();
            _whiffs.Clear();
            _finishSub = _signals.On<EnemyFinished>().Subscribe(e => _finishes.Add(e));
            _whiffSub = _signals.On<VerbWhiffed>().Subscribe(e => _whiffs.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            _finishSub?.Dispose();
            _whiffSub?.Dispose();
            _resolver.Dispose();
            _signals.Dispose();
            foreach (var go in _spawned) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        private GameObject Spawn(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private (GameObject enemy, AttackData attack) MakeHit(bool ready, int remainingHp, bool playerInflictor = true)
        {
            var enemy = Spawn("Enemy");
            var marker = enemy.AddComponent<FinishReadyMarker>();
            marker.SetReady(ready, Color.yellow);
            var health = enemy.AddComponent<HealthSystem>();
            health.maxHp = 10;
            health.currentHp = remainingHp;

            var inflictor = Spawn("Inflictor");
            if (playerInflictor) inflictor.tag = "Player";
            var attack = new AttackData("test", 5, inflictor, ATTACKTYPE.PUNCH, knockdown: false);
            return (enemy, attack);
        }

        [Test]
        public void KillingHit_OnFinishReadyTarget_PublishesEnemyFinished()
        {
            var (enemy, attack) = MakeHit(ready: true, remainingHp: 0);

            _resolver.HandleDamage(enemy, attack);

            Assert.AreEqual(1, _finishes.Count);
            Assert.IsFalse(_finishes[0].WasChaff);
        }

        [Test]
        public void KillingHit_OnNotReadyTarget_IsNotAFinish()
        {
            var (enemy, attack) = MakeHit(ready: false, remainingHp: 0);

            _resolver.HandleDamage(enemy, attack);

            Assert.AreEqual(0, _finishes.Count);
        }

        [Test]
        public void NonKillingHit_OnReadyTarget_IsNotAFinish()
        {
            var (enemy, attack) = MakeHit(ready: true, remainingHp: 2);

            _resolver.HandleDamage(enemy, attack);

            Assert.AreEqual(0, _finishes.Count);
        }

        [Test]
        public void EnemyInflictedKill_IsNotAFinish()
        {
            var (enemy, attack) = MakeHit(ready: true, remainingHp: 0, playerInflictor: false);

            _resolver.HandleDamage(enemy, attack);

            Assert.AreEqual(0, _finishes.Count);
        }

        [Test]
        public void PlayerWhiff_PublishesVerbWhiffed()
        {
            var player = Spawn("Player");
            player.AddComponent<UnitSettings>(); // unitType defaults to PLAYER
            var actions = player.AddComponent<UnitActions>();

            _resolver.HandleWhiff(actions, ATTACKTYPE.KICK);

            Assert.AreEqual(1, _whiffs.Count);
            Assert.AreEqual(ATTACKTYPE.KICK, _whiffs[0].AttackType);
        }

        [Test]
        public void EnemyWhiff_IsIgnored()
        {
            var enemy = Spawn("Enemy");
            var settings = enemy.AddComponent<UnitSettings>();
            settings.unitType = UNITTYPE.ENEMY;
            var actions = enemy.AddComponent<UnitActions>();

            _resolver.HandleWhiff(actions, ATTACKTYPE.PUNCH);

            Assert.AreEqual(0, _whiffs.Count);
        }
    }
}
```

- [ ] **Step 2: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`FinishResolver` does not exist yet). Proceed.

- [ ] **Step 3: Additive `UnitActions` edits**

In `Assets/_neon/Scripts/Units/UnitActions.cs`:

**(a)** After the existing injected fields/properties:

```csharp
        [Inject] private IEntitiesService _entitiesService;
        public IInputService InputService => _inputService;
        public IAudioService AudioService => _audioService;
        public IEntitiesService Entities => _entitiesService;
```

change to:

```csharp
        [Inject] private IEntitiesService _entitiesService;
        [Inject] private ISwarmBridge _swarmBridge;
        public IInputService InputService => _inputService;
        public IAudioService AudioService => _audioService;
        public IEntitiesService Entities => _entitiesService;
        public ISwarmBridge SwarmBridge => _swarmBridge;
```

(Safe everywhere: `NullSwarmBridge` is the session default, `Level` scopes shadow it with the real bridge.)

**(b)** After the existing damage event:

```csharp
        public delegate void OnUnitDealDamage(GameObject recipient, AttackData attackData);
	    public static event OnUnitDealDamage onUnitDealDamage;
```

add:

```csharp
        public delegate void OnVerbWhiffed(UnitActions unit, ATTACKTYPE attackType);
        public static event OnVerbWhiffed onVerbWhiffed;

        //report a completed verb that hit nothing (whiff-cost seam — FinishResolver listens)
        public void ReportVerbWhiff(ATTACKTYPE attackType){
            onVerbWhiffed?.Invoke(this, attackType);
        }
```

**(c)** In `CheckForHit`, change the ending:

```csharp
                    damageDealt = true;
                }
            }
            return damageDealt;
        }
```

to:

```csharp
                    damageDealt = true;
                }
            }

            //additive swarm seam (spec §5.2): the same hitbox also sweeps DOTS chaff via the
            //bridge (chaff have no colliders — F4). Finish-Ready chaff die as a FINISH inside
            //the bridge (single-verb chaff finish). Existing verb behavior above is unchanged.
            if(isPlayer && HitBoxActive() && _swarmBridge != null){
                if(_swarmBridge.ApplyVerbHit(settings.hitBox.bounds, attackData)) damageDealt = true;
            }
            return damageDealt;
        }
```

- [ ] **Step 4: Whiff reports on verb-state exit**

In `Assets/_neon/Scripts/Units/PlayerStates/PlayerAttack.cs`, replace:

```csharp
        public override void Exit() {

            //reset combo if nothing was hit
            if(!damageDealt && unit.settings.continueComboOnHit) unit.attackList.Clear();
        }
```

with:

```csharp
        public override void Exit() {

            //reset combo if nothing was hit
            if(!damageDealt && unit.settings.continueComboOnHit) unit.attackList.Clear();

            //whiff-cost seam (spec §5.1): a completed punch/kick that hit nothing.
            //Grab whiffs are exempt (v0.4) — grabs never enter this state.
            if(!damageDealt && attackData != null) unit.ReportVerbWhiff(attackData.attackType);
        }
```

In `Assets/_neon/Scripts/Units/PlayerStates/PlayerWeaponAttack.cs`, add after the `Update()` method (before the class closing brace):

```csharp
        public override void Exit(){

            //whiff-cost seam (spec §5.1): a completed weapon swing that hit nothing
            if(!damageDealt) unit.ReportVerbWhiff(ATTACKTYPE.WEAPON);
        }
```

- [ ] **Step 5: The stagger state (Recipe 2 pattern)**

`Assets/_neon/Scripts/Units/PlayerStates/PlayerWhiffStagger.cs`:

```csharp
using UnityEngine;

namespace BrainlessLabs.Neon {

    //brief self-stagger after a whiffed verb (spec §5.1 whiff cost: Momentum reset
    //+ 0.5s vulnerability window). Entered by FinishResolver, not by player input.
    public class PlayerWhiffStagger : UnitState {

        private readonly float duration;

        public PlayerWhiffStagger(float duration){
            this.duration = duration;
        }

        public override void Enter(){
            unit.StopMoving();
            unit.animator.Play("Hit", 0, 0); //reuse the hit-reaction anim as the stagger tell
        }

        public override void Update(){
            if(Time.time - stateStartTime > duration) unit.UnitStateMachine.SetState(new PlayerIdle());
        }
    }
}
```

- [ ] **Step 6: `FinishResolver`**

`Assets/_neon/Scripts/Engagement/FinishResolver.cs`:

```csharp
using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The observe-and-tag seam (spec §5.1): watches the existing verbs through the
    /// static combat events. M1 hero-tier finish rule: a player verb hit that KILLS a
    /// Finish-Ready enemy (chaff finishes resolve inside SwarmBridge.ApplyVerbHit).
    /// Whiffs publish VerbWhiffed (Momentum resets on it) and stagger the player.
    /// The stagger is applied on the NEXT clock tick — onVerbWhiffed fires inside a
    /// state transition (PlayerAttack.Exit runs during SetState), and a reentrant
    /// SetState there would race the outgoing transition.
    /// </summary>
    public sealed class FinishResolver : IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 25; // after the selector (20), before Momentum decay (30)

        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly EngagementConfig _config;
        private UnitActions _pendingStagger;

        public FinishResolver(IGameplayClock clock, IGameplaySignals signals, EngagementConfig config)
        {
            _clock = clock;
            _signals = signals;
            _config = config;
            UnitActions.onUnitDealDamage += HandleDamage;
            UnitActions.onVerbWhiffed += HandleWhiff;
            _clock.Register(this, TICK_ORDER);
        }

        public void Dispose()
        {
            UnitActions.onUnitDealDamage -= HandleDamage;
            UnitActions.onVerbWhiffed -= HandleWhiff;
            _clock.Unregister(this);
        }

        public void Tick(float deltaTime)
        {
            if (_pendingStagger == null) return;

            var stateMachine = _pendingStagger.UnitStateMachine;
            _pendingStagger = null;
            if (stateMachine != null) stateMachine.SetState(new PlayerWhiffStagger(_config.WhiffStaggerSeconds));
        }

        /// <summary>Public so EditMode tests can drive it (static events can't be raised externally).</summary>
        public void HandleDamage(GameObject recipient, AttackData attackData)
        {
            if (recipient == null || attackData?.inflictor == null) return;
            if (!attackData.inflictor.CompareTag("Player")) return;

            var marker = recipient.GetComponent<FinishReadyMarker>();
            if (marker == null || !marker.IsReady) return;

            var health = recipient.GetComponent<HealthSystem>();
            if (health == null || !health.isDead) return; // the finishing hit is the killing hit (M1)

            _signals.Publish(new EnemyFinished(recipient.transform.position, wasChaff: false));
        }

        /// <summary>Public so EditMode tests can drive it.</summary>
        public void HandleWhiff(UnitActions unit, ATTACKTYPE attackType)
        {
            if (unit == null || !unit.isPlayer) return;

            _signals.Publish(new VerbWhiffed(attackType));
            _pendingStagger = unit;
        }
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: 48/48 PASS (42 + 6 resolver).

- [ ] **Step 8: Register in the Level scope**

In `Level.RegisterEngagementSystems`, after the `FinishReadySelector` line add:

```csharp
            builder.Register<FinishResolver>(Lifetime.Scoped).AsSelf();
```

and in the build callback, after `container.Resolve<FinishReadySelector>();` add:

```csharp
                    container.Resolve<FinishResolver>();
```

- [ ] **Step 9: Runtime gate — the full loop (Recipe 4)**

Boot into Level1. This is the first time the whole core loop runs:
1. Auto-engage softens chaff → gold glow → **punch a gold chaff** → it dies instantly (single-verb finish).
2. String finishes quickly: after 3 the meter tier rises (verify via Momentum log or Task 11's meter; for now watch chip damage visibly speed up? — no: rate is not tier-scaled; instead confirm via chip numbers on hero-tier HP, or just proceed — the meter lands next task).
3. **Whiff on purpose** (punch empty air away from the swarm): the player flinches (~0.5s "Hit" anim) and control returns.
4. Fight a wave enemy to low HP (gold) and land the killing verb — no errors; grab-throw a healthy enemy — no whiff fires on grabs.
5. Exit Play mode. Zero console errors throughout.

- [ ] **Step 10: Commit**

```bash
git add "Assets/_neon/Scripts/Units/UnitActions.cs" "Assets/_neon/Scripts/Units/PlayerStates/PlayerAttack.cs" "Assets/_neon/Scripts/Units/PlayerStates/PlayerWeaponAttack.cs" "Assets/_neon/Scripts/Units/PlayerStates/PlayerWhiffStagger.cs" "Assets/_neon/Scripts/Units/PlayerStates/PlayerWhiffStagger.cs.meta" "Assets/_neon/Scripts/Engagement/FinishResolver.cs" "Assets/_neon/Scripts/Engagement/FinishResolver.cs.meta" "Assets/_neon/Tests/EditMode/FinishResolverTests.cs" "Assets/_neon/Tests/EditMode/FinishResolverTests.cs.meta" "Assets/_neon/Scripts/Level/Level.cs"
git commit -m "feat: FinishResolver + whiff cost - verbs wired into the loop (M1)"
```

---

### Task 11: Minimal HUD — Momentum meter + Finish-Ready prompt (F3)

Pure `IGameplaySignals` consumers (spec §5.5 throughline) — no system references, no `OnGUI`. Simple uGUI under the level's canvas.

**Files:**
- Create: `Assets/_neon/Scripts/UI/UIHUDMomentumMeter.cs`
- Create: `Assets/_neon/Scripts/UI/UIHUDFinishPrompt.cs`
- Editor: canvas wiring in `03_Level1`

- [ ] **Step 1: Momentum meter**

`Assets/_neon/Scripts/UI/UIHUDMomentumMeter.cs`:

```csharp
using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon {

    //Momentum tier meter (M1-minimal): fill + color per tier. Pure signals consumer.
    public class UIHUDMomentumMeter : MonoBehaviour {

        [SerializeField] private Image fillImage;
        [SerializeField] private Text tierLabel;
        [SerializeField] private Color[] tierColors = {
            new(0.5f, 0.5f, 0.5f, 1f),   //Cool
            new(1f, 0.8f, 0.3f, 1f),     //Warm
            new(1f, 0.5f, 0.15f, 1f),    //Hot
            new(1f, 0.15f, 0.3f, 1f)     //Overdrive
        };
        [Inject] private IGameplaySignals _signals;
        private IDisposable _subscription;

        void Start(){
            if(_signals == null) return; //scene without DI injection
            Apply(MomentumTier.Cool);
            _subscription = _signals.On<MomentumTierChanged>().Subscribe(e => Apply(e.Current));
        }

        void OnDestroy(){
            _subscription?.Dispose();
        }

        void Apply(MomentumTier tier){
            int index = Mathf.Clamp((int)tier, 0, tierColors.Length - 1);
            if(fillImage != null){
                fillImage.fillAmount = ((int)tier + 1) / 4f;
                fillImage.color = tierColors[index];
            }
            if(tierLabel != null) tierLabel.text = tier.ToString().ToUpper();
        }
    }
}
```

- [ ] **Step 2: Finish prompt**

`Assets/_neon/Scripts/UI/UIHUDFinishPrompt.cs`:

```csharp
using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon {

    //The single verb-glyph prompt (R7) + "+N ready" counter. Follows the prompted
    //target on screen (Screen Space - Overlay canvas). Pure signals consumer.
    public class UIHUDFinishPrompt : MonoBehaviour {

        [SerializeField] private RectTransform promptRoot;
        [SerializeField] private Text verbLabel;
        [SerializeField] private Text countLabel;
        [SerializeField] private Vector2 screenOffset = new(0f, 60f);
        [Inject] private IGameplaySignals _signals;
        private IDisposable _subscription;
        private bool hasTarget;
        private Vector2 targetPosition;

        void Start(){
            if(_signals == null) return; //scene without DI injection
            if(promptRoot != null) promptRoot.gameObject.SetActive(false);
            _subscription = _signals.On<FinishReadyPromptChanged>().Subscribe(Apply);
        }

        void OnDestroy(){
            _subscription?.Dispose();
        }

        void LateUpdate(){
            if(!hasTarget || promptRoot == null) return;
            var cam = Camera.main;
            if(cam == null) return;
            promptRoot.position = (Vector2)cam.WorldToScreenPoint(targetPosition) + screenOffset;
        }

        void Apply(FinishReadyPromptChanged prompt){
            hasTarget = prompt.HasTarget;
            targetPosition = prompt.TargetPosition;
            if(promptRoot != null) promptRoot.gameObject.SetActive(prompt.HasTarget);
            if(verbLabel != null) verbLabel.text = prompt.SuggestedVerb.ToString(); //"PUNCH" glyph art comes with M4 polish
            if(countLabel != null) countLabel.text = prompt.ReadyCount > 1 ? $"+{prompt.ReadyCount - 1} ready" : "";
        }
    }
}
```

- [ ] **Step 3: Scene wiring (editor, not in Play mode)**

Open `03_Level1`:
1. Locate the scene's UI **Canvas** (the one hosting `UIHUDHealthBar`). If its Render Mode is not *Screen Space - Overlay*, note it and anchor the prompt bottom-center instead of following the target (set `promptRoot` to a fixed anchored element; the follow code is harmless — `Camera.main` positioning simply won't match — but fixed-anchor is the honest fallback).
2. Add child `MomentumMeter` (empty RectTransform, anchored top-left under the health bar):
   - child `Fill`: **UI → Image**, Image Type **Filled / Horizontal**, ~200×20 px.
   - child `TierLabel`: **UI → Legacy → Text**.
   - Add `UIHUDMomentumMeter` to `MomentumMeter`, assign `fillImage` + `tierLabel`.
3. Add child `FinishPrompt` (empty RectTransform):
   - child `PromptRoot` (RectTransform) containing `VerbLabel` (**Text**, bold, e.g. 28pt) and `CountLabel` (**Text**, smaller, below).
   - Add `UIHUDFinishPrompt` to `FinishPrompt`, assign `promptRoot`, `verbLabel`, `countLabel`.
4. Save the scene.

(Scene root injection covers `[Inject]` on both components — `Level.Configure` injects every root's hierarchy.)

- [ ] **Step 4: Runtime gate (Recipe 4)**

Boot into Level1:
1. Meter shows **COOL** grey; punch three gold chaff quickly → **WARM** and color shift; keep going → HOT/OVERDRIVE; idle ~2.5s per tier → decays back; whiff → snaps to COOL.
2. The **PUNCH** prompt hovers over exactly ONE gold target with "+N ready" when more exist; it retargets as you move.
3. Zero console errors; no GC-hitch feel from the HUD.
4. Exit Play mode.

- [ ] **Step 5: Commit**

```bash
git add "Assets/_neon/Scripts/UI/UIHUDMomentumMeter.cs" "Assets/_neon/Scripts/UI/UIHUDMomentumMeter.cs.meta" "Assets/_neon/Scripts/UI/UIHUDFinishPrompt.cs" "Assets/_neon/Scripts/UI/UIHUDFinishPrompt.cs.meta" "Assets/_neon/Scenes/Game/03_Level1.unity"
git commit -m "feat: minimal HUD - Momentum meter + single finish prompt (M1)"
```

---

### Task 12: M1 gate — R1 density + R2 feel + record + push

Spec §7 M1 gate (both core bets): **R1** 150 hot + 100 ambient holds target FPS; **R2** hands feel busy, not idle. Runtime is ground truth.

- [ ] **Step 1: Full test suite**

Run all EditMode tests. Expected: **48/48 PASS** (23 M0 + 8 Momentum + 6 AutoEngage + 5 Selector + 6 Resolver).

- [ ] **Step 2: R1 density measurement**

1. Set Level1's swarm block to **ChaffCap = 150** (spec R1 population), AmbientCap 100.
2. Boot into Level1, play actively for **60+ seconds** at cap (wait for `[Swarm] Chaff cap reached (150).`).
3. Record: FPS (Stats overlay / profiler), worst frame, batches, `HotProxyPool`-equivalent sync cost (`SwarmRenderRig.LateUpdate` in the profiler), and any GC spikes.
4. **Pass bar: ≥60 FPS sustained** during real combat (chips + finishes + waves). The spike measured 330+ FPS headroom for render-only; this now includes gameplay.
5. Decide: keep ChaffCap 150 or settle at 120 (record which).

- [ ] **Step 3: R2 feel + legibility checklist (subjective, but structured)**

In one continuous run, answer each honestly in the gate record:
- Do your hands stay busy — finishing, repositioning, whiff-managing — while auto-engage grinds? (R2, the M1 gate *is* this test)
- Is "gold = finishable NOW" readable at full density? (R9 early read)
- Is exactly ONE prompt ever shown? (R7)
- Does Momentum visibly climb from finishing and sting on whiff?
- Do spawned wave enemies fight back? (AI_Active fix regression)

- [ ] **Step 4: Write the gate record + push**

Append a `## M1 gate record` section to the bottom of THIS plan document: date, machine, test count, R1 numbers, R2/R9/R7 answers, ChaffCap decision, deviations encountered. Then:

```bash
git add "docs/superpowers/plans/2026-07-04-neon-engine-base-plan2-m1-engagement.md"
git commit -m "docs: record M1 gate (R1 density + R2 feel)"
git push -u origin claude/neon-m1-engagement
```

- [ ] **Step 5: Hand off**

Report the gate record to Sebastien. **Plan 3 (M2 growth: economy, Protocols via `docs/rgd/protocol-stack-v0.1.md`, progression, tiered finish challenges) is written after this gate** — the tiered-challenge design consumes the real `FinishResolver`/`comboData` seams as they landed here. Also flag: `docs/rgd/` is still untracked — commit those design docs when convenient.

---

## Deviations from spec (deliberate, M1-scoped)

1. **`HotFlag`/`EngageIntent` components deferred** — all chaff are hot in M1 (`SwarmAgent.Tier == Chaff`); steering intent is inline in `SwarmSteeringSystem`. They return when hot/cold chaff states diverge.
2. **`SwarmChipSystem` folded into `SwarmDamageSystem`** — one command-consuming system; chip vs verb damage distinguished per command (`IsChip`).
3. **Chip never kills** (floors chaff at 1 HP): the literal reading of "tuned to push toward Finish-Ready, not kill" — otherwise chip (8) skips the ready band (≤6 of 24) entirely and the loop starves. Verbs are the only chaff killers.
4. **Separation steering simplified** to lane-Y + per-entity jitter (spec says seek+separation); a feel pass can upgrade it inside the sim without touching anything above Layer 1.
5. **Hero-tier verb damage is not Momentum-scaled in M1** — verbs stay untouched (spec rule); the multiplier feeds chip damage now and the M2 economy next. Hero finish = the killing verb hit on a ready target.
6. **Whiff stagger applied one clock-tick deferred** — `onVerbWhiffed` fires inside `PlayerAttack.Exit` (mid-`SetState`); an immediate reentrant `SetState` would race the outgoing transition.
7. **`FinishResolver.HandleDamage`/`HandleWhiff` are public** — static events can't be raised from tests; the methods are the test seam.

## Spec coverage self-check (for reviewers)

- §7 M1 items: DOTS swarm ✅ (Task 4) · SwarmBridge ✅ (Task 5) · AutoEngage ✅ (Task 8) · FinishReady + selector ✅ (Tasks 4/9) · Momentum ✅ (Task 3) · FinishResolver wiring existing verbs, single-verb chaff finish ✅ (Tasks 5/10) · whiff cost ✅ (Task 10) · AI_Active fix ✅ (Task 7) · minimal HUD (meter + glow + prompt) ✅ (Tasks 6/9/11) · gate R1+R2 ✅ (Task 12).
- §5.2 bridge jobs: targeting ✅ · resolution ✅ · render sync (proxy pool + instanced ambient, spike-corrected to `DrawMeshInstanced`) ✅ · spawn ownership split (SpawnerService keeps hero-tier; `SwarmSpawnSystem` owns chaff/ambient; `LevelConfigurationAsset` swarm block) ✅. `EntitiesService` stays hero-tier-only ✅.
- §10 non-negotiables: DI-bootstrap (every gate uses Recipe 4) ✅ · VContainer-only ✅ (Null-variant pattern per neon-recipes for the absent-bridge case) · FSM-state/Level-scope registration ✅ · verbs engine-agnostic + unchanged (additive hooks only) ✅ · no legacy touched ✅ · runtime gates per task ✅ · no invented APIs (all seams read from source; pre-brief `file:line` verified) ✅ · assembly direction (`Neon → Simulation`; Simulation stays a leaf) ✅.
- Fork decisions F1–F5 honored: Tasks 1 (F1), 4/5 (F2), 11 (F3), 5/10 (F4), 1 (F5).
- Deferred (per plan series): tiered `IFinishChallenge`, economy/XP, Protocols, run FSM, Signal, Siren Pulse/Overcharge, per-verb hitstop profiles → Plans 3–5.

---

## M1 gate record

**Date:** 2026-07-04 · **Machine:** sebch dev PC (Windows 11 Pro, Unity 6000.3.5f2, in-editor Play mode) · **Branch:** `claude/neon-m1-engagement` (12 commits off `master` @ `66b1aec`)
**Executed by:** Claude (agentic run, Unity MCP-driven; all runtime gates instrumented via editor code injection — no human at the keyboard yet)

### Test suite

**48/48 EditMode PASS** (23 M0 + 8 Momentum + 6 AutoEngage + 5 Selector + 6 Resolver). Pre-existing third-party DTT.Utils package test failures excluded (broken before this branch; run scoped to `BrainlessLabs.Neon.Tests.EditMode`).

### R1 density measurement (ChaffCap 150 + AmbientCap 100)

Two clean self-freezing measurement windows at cap, editor untouched during capture, active combat running (auto-engage chips at 1.5/s, 10 AI-active wave enemies attacking, periodic forced player punches/kicks into the crowd, finishes + Momentum churn):

| Window | Duration | Frames | Avg FPS | Avg frame | Worst frame | GC gen0 |
|---|---|---|---|---|---|---|
| 1 | 48.2 s | 9,871 | **205** | 4.88 ms | 45.5 ms | 3 |
| 2 | 30.0 s | 5,617 | **187** | 5.34 ms | 44.0 ms | 2 |

- **Pass bar ≥60 FPS sustained: PASS** (~197 FPS combined avg over 78 s at cap; >3× the bar).
- Batches ~56 total; ambient's 100 quads collapse into **1 instanced batch** (`UnityStats.instancedBatches = 1`, `DrawMeshInstanced` + `Neon/InstancedUnlit` — spike constraint honored; verified via UnityStats, not MCP screenshots).
- Worst frames (~45 ms) are isolated single-frame spikes consistent with editor-side GC/unfocused-editor scheduling; no visible sustained hitching. ~1 gen0 GC collection per 15 s during combat (proxy sync + bridge queries use Temp allocs; fine at M1, watch in M4 polish).
- **ChaffCap decision: keep 150.** Massive headroom; no reason to settle at 120.

### R2 / R9 / R7 checklist

Answered from instrumented runtime evidence; the two *feel* items need Sebastien's hands-on pass (agent could not hold a controller):

- **R2 hands busy while auto-engage grinds:** MECHANICS VERIFIED / FEEL PENDING HUMAN PASS. The full loop demonstrably runs in-game: chips soften chaff → gold at 1 HP (never chip-killed) → player verb connect = instant finish (`EnemyFinished` counted via live probe: punches into the crowd produced finishes + verb damage on 110 chaff in one swing) → Momentum steps (Cool→Warm on 3 finishes, ×1.3 DamageMultiplier applied) → whiff resets to Cool + 0.5 s stagger. Whether hands *feel* busy needs a real playthrough.
- **R9 "gold = finishable NOW" readable at density:** PARTIAL. Gold vs pink tint renders correctly at cap (verified per-proxy). Legibility concern: chaff converge into a dense blob at the player (lane-jitter separation is weak — deviation 4), which may smear the gold read in the pile. Needs eyes-on.
- **R7 exactly one prompt:** PASS. With 3 simultaneous gold chaff: one active prompt, "+2 ready" count, prompt follows the nearest target on the ScreenSpaceOverlay canvas.
- **Momentum climbs from finishing / stings on whiff:** PASS. Meter: COOL grey 0.25 → WARM 0.5 warm-gold on a 3-finish burst; decays back per 2.5 s idle window; empty-air punch published `VerbWhiffed` and snapped tier to COOL.
- **Spawned wave enemies fight back (AI_Active fix):** PASS. Ten wave enemies spawned AI-active, closed distance, and beat a stationary 10 HP player to death unaided. (Pre-fix they idled at spawn.)

**Sebastien's feel pass (2026-07-04): GATE SIGNED OFF** — "ok enough to pass to the next phase." The R2/R9 pending items above are accepted for M1; the tuning flags below carry into Plan 3.

### Deviations encountered during execution (beyond the plan's own deviation list)

1. **Test asmdef needs `VContainer` + `Unity.Collections` refs** — `GameplayClock` implements VContainer's `ITickable`, and Entities source-gen demands Unity.Collections; plan only listed `Unity.Entities`.
2. **`EngagementSettingsAsset` doesn't auto-create at runtime** — `InstanceAsset` only `Resources.Load`s in Play mode; creation is the editor-only `GetOrCreateSettingsAsset()` (boot crashed at `GameplayServicesState` until created). Recipe 5's "enter Play once" creation path is wrong as documented.
3. **AutoEngage accumulator flipped to chips-owed (`dt × rate`)** — the planned `1/rate` interval comparison drops the last chip of a window to float rounding (`1f/6f > 1/6`); caught by the 6/s hard-cap test.

### Observations for Plan 3 (not fixed here — flagging, not redesigning)

- **Hero-tier chip can kill** (no 1 HP floor like chaff, per plan): at Level1 tuning (chip 8 vs 10 HP wave enemies) auto-engage alone kills heroes in ~2 chips, shrinking the hero verb-finish window to near zero. Either floor hero chips like chaff or retune enemy HP in M2.
- **Chaff crowd blobs**: all 150 converge to a single point at the player's stop radius; the per-entity jitter (`entity.Index % 7`) barely spreads them. The planned feel pass inside `SwarmSteeringSystem` is genuinely needed for R9 legibility.
- **Whiff counter anomaly**: one crowd-connecting punch registered +1 whiff alongside its finishes (suspect first-frame `animFinished`/interrupted-exit artifact in `PlayerAttack`). Behavior stayed correct; worth a look when tiered challenges consume the whiff seam.
- **Momentum decay (2.5 s/tier) is aggressive** relative to how fast gold targets appear at current chip tuning — expect Warm to be hard to hold in real play.
- `BootstrapSettingsAsset._postBootstrapScene` switched MainMenu→Level1 locally for play-testing (left uncommitted deliberately).
- `docs/rgd/` remains untracked — commit those design docs when convenient.
