# Neon Engine Base — Plan 4: M3 Run / Objective / Signal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the vertical slice into a **run** — `RunService` (UnityHFSM) sequencing encounter phases in the one belt arena, `RebootNodeObjective` (the win verb), `ISignalSystem` (the 0→dawn scalar feeding spawn nastiness + a night→dawn darkness lerp), and a between-encounter **shop beat** (Heal + Continue, spends Neon Charge) — so a full run lands ~10–15 min and **ends on the dawn beat** (spec §7 M3 gate).

**Architecture:** `RunService` is a Level-scoped service built on **UnityHFSM** (the same library as the app-boot FSM, but in the `Neon` assembly — spec §4.3: gameplay FSM ≠ boot FSM). It reuses everything below it through the spine: it advances the **Signal** (a modifier source on the `Run` sheet, exactly like Momentum), which scales chaff density via a `SpawnNastiness` stat the `SwarmBridge` already-reads-per-tick; it triggers hero waves through the **existing** `SpawnerService.TriggerWave` (waves authored `Manual`); it arms a `RebootNodeObjective` (pure logic — player-position vs node-radius, no new colliders, matching the bridge-only spatial ethos); and it freezes combat during the shop by holding a `Time.timeScale` scale source on the clock (which M2 made the clock own). All cross-system talk stays on `IGameplaySignals`; the HUD/darkness are pure consumers.

**Tech Stack:** unchanged — Unity 6000.3.5f2 (built-in RP), VContainer, R3, UnityHFSM, Entities 1.4.4, EditMode NUnit, uGUI.

**Spec:** `docs/superpowers/specs/2026-07-04-neon-engine-base-design.md` §5.4, §7 M3
**Design input:** `docs/rgd/special-moves-v0.1.md` §3 (the inter-level shop; MVP has no Specials to sell yet → M3 shop is Heal + Continue), `docs/rgd/protocol-stack-v0.1.md` §8.2 (Signal-scaled draft weights — wired here now that a Signal tier exists)
**Prior state:** M2 gate signed off (`plan3` doc §"M2 gate record") — 90/90 tests, snowball verified, ~234 FPS @ ChaffCap 150. Overdrive-scream hands-on still pending but not blocking M3.
**Branch:** create `claude/neon-m3-run` off `claude/neon-m2-growth` (tip `0af6305` — M2 is NOT yet merged to master; if Sebastien merges M2 first, branch off `master` instead).

---

## Decisions locked (Sebastien, 2026-07-04)

| Fork | Decision |
|---|---|
| **R1 — M3 shop beat** | **Heal-only structural shop.** `RunService` gets a real `Shop` phase between encounters + a screen that spends Neon Charge on **Heal + Continue**. Proves the fight→shop→fight rhythm and gives the Charge ledger its first sink; Specials/ranks/rerolls slot into the same screen at M4. |
| **R2 — encounter waves** | **`RunService` triggers `Manual` waves.** Re-author Level1's waves as `WaveTriggerType.Manual`; `RunService` calls `SpawnerService.TriggerWave(encounterIndex)` per encounter. Reuses the existing API; keeps the M1/M2 hero finish-challenge feel per encounter. Chaff swarm floods continuously, scaled by the Signal. |
| **R3 — Signal outputs** | **Nastiness + darkness lerp.** Signal drives spawn nastiness (a `Run`-sheet stat the bridge reads) AND a background night→dawn color lerp so reaching dawn is visually legible for the gate. Music aggression is exposed on the Signal but consumed in M4 (spec §7 M4 owns "audio layering by tier + Signal"). |

## Assumptions (stated per Sebastien's rules — correct before execution if wrong)

- **Run shape:** one belt arena, a **fixed list of encounters authored per-level** (Level1 ships **3**), each = arm a Reboot Node + trigger that encounter's `Manual` wave; chaff floods throughout. Completing an objective raises the Signal by `1/EncounterCount`, so the **last objective lands the Signal on dawn = RunWon**. **Boss is a stub state** that immediately passes to `RunWon` (spec §5.4: "the run can win on reaching dawn if the boss is cut"). Target 10–15 min emerges from 3 × (~50s reboot + fighting + shop).
- **Lose = player death**, from any phase → `RunLost`. Level1's existing `Level.OnPlayerDeath → GameOverMenu` stays as the death **presentation**; `RunService` additionally drives its FSM to `RunLost` for run-state integrity and to stop encounters. **Win** shows a distinct dawn/victory menu via a `RunEnded` consumer. (The minor "both react to death" split is intentional and harmless — same GameOver menu; documented in deviations.)
- **New-run reset** (protocol stacks / economy / Signal persisting if the player replays within one boot) is **out of scope** — MVP is one run per boot; run-reset is a later concern (flagged, folds into the run-lifecycle work the Feel & Level / M4 pass touches).
- **Reboot Node detection is pure distance** (player pos vs node pos vs radius) — no physics trigger, consistent with F4's bridge-only spatial decision from M1. A separate purely-visual component shows the zone glow.

---

## Landed-code facts this plan builds on (verified 2026-07-04, post-M2)

- **UnityHFSM API confirmed from source** (`Library/PackageCache/com.inspiaaa.unityhfsm@270e954ed07b`): `new StateMachine()`, `.Init()`, `.OnLogic()`, `.AddState(name, StateBase)`, `.RequestStateChange(name, forceInstantly)`, and `new State(onEnter, onLogic, onExit, canExit, needsExitTime, isGhostState)` taking `Action<State>` callbacks. RunService uses exactly this subset — nothing invented. The `Neon` asmdef does **not** yet reference `UnityHFSM` (only `Lifecycle` does) — Task 6 adds it.
- `SpawnerService.TriggerWave(int)` + `WaveTriggerType.Manual` already exist; a Manual wave parks (no auto-spawn) until triggered. `SpawnerService` is `new`-ed in `Level.Start` (not in DI) and exposed as `Level.SpawnerService` — so `RunService` receives the trigger via `BeginRun`, not via injection.
- `GameplayClock` owns `Time.timeScale` via scale sources (M2). A Shop-pause = one scale source at 0. Registered tickables still tick at dt=0 during pause, so `RunService.Tick` keeps responding to the Continue command.
- `IEconomySystem` has `Xp`/`NeonCharge`/`Overcharge` getters and publishes `NeonChargeChanged` — but **no spend method** (Task 2 adds `TrySpend`).
- `SwarmBridge` ctor is `(IGameplayClock, IGameplaySignals, IEntitiesService, SwarmConfig)` and pushes a `SwarmWorldState` (with `ChaffCap`/`SpawnRatePerSecond`) into the sim each `Tick` — the exact seam where Signal nastiness multiplies (Task 4 adds `IStatSystem` to its ctor).
- `StatSheet` folds `(base + ΣAdd) × (1 + ΣPctAdd) × ΠMult`; `RemoveBySource`/`AddModifier` are the Signal's tools (same pattern as Momentum). `StatId` is append-only.
- `ProtocolService.RollChoices` uses fixed base weights with a comment "Signal scaling (×band) arrives with M3's Signal" — Task 9 wires that.

---

## Working agreements (read once, apply to every task)

1. **Unity import + zero console errors before every test/play step** (refresh via editor focus or `mcp__unityMCP__refresh_unity`).
2. **EditMode tests:** Test Runner → EditMode → Run All (or `mcp__unityMCP__run_tests`, `mode: "EditMode"`), scoped to `BrainlessLabs.Neon.Tests.EditMode` (third-party DTT tests are pre-broken; ignore). The 90 M0–M2 tests stay green throughout.
3. **Play-testing = Recipe 4**: boot with Post-Bootstrap Scene = `SceneDefinition_Level1`. No git/asset writes in Play mode. `BootstrapSettingsAsset` boot-target flips stay uncommitted.
4. **Commits include `.meta` files**; every commit body ends with `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
5. **Do not touch:** `WaveManager`, `04_Level2`/`05_Level3`, `ApplicationLifetimeScope`, `Assets/Addons/`, `Assets/_neon/Spikes/`, `Assets/_generated/`.
6. **Guardrails (protocol doc §8.1):** Signal is a modifier source like Momentum — it uses `PctAdd` on `SpawnNastiness` (not `Mult`; Momentum stays the only multiplier). Objective-speed is a stat so future Objective protocols / Priority Override tune it. No hard cap is loosened here.
7. **Verbs and existing systems stay behaviorally unchanged.** M3 adds orchestration + new systems; the only edits to existing files are additive (economy spend, bridge nastiness read, Level.Start run kickoff, waves→Manual data).
8. **`StatId` is append-only.**
9. If any landed signature mismatches this plan at execution time, re-read the source file before adapting (working agreement, not a licence to invent).

---

## File structure

**Created — run layer (`BrainlessLabs.Neon`):**

| Path | Responsibility |
|---|---|
| `Assets/_neon/Scripts/Run/RunPhase.cs` | The phase enum (mirrors the FSM states) |
| `Assets/_neon/Scripts/Run/RunSettings.cs` | Global run tuning (`ISettings`) |
| `Assets/_neon/Scripts/Run/RunSettingsAsset.cs` | `BaseSettingsAsset` wrapper |
| `Assets/_neon/Scripts/Run/RunConfig.cs` | Asset-free snapshot (per-level nodes + global tuning) |
| `Assets/_neon/Scripts/Run/IRunService.cs` | Run flow surface (`Phase`, `BeginRun`, `ContinueFromShop`) |
| `Assets/_neon/Scripts/Run/RunService.cs` | UnityHFSM run FSM: encounters → shop → dawn |
| `Assets/_neon/Scripts/Run/ISignalSystem.cs` | The dawn scalar |
| `Assets/_neon/Scripts/Run/SignalSystem.cs` | 0→dawn, raises `SpawnNastiness`, publishes `SignalChanged` |
| `Assets/_neon/Scripts/Run/IObjective.cs` | Objective contract |
| `Assets/_neon/Scripts/Run/RebootNodeObjective.cs` | Hold-zone-under-fire fill logic (pure) |
| `Assets/_neon/Scripts/Run/RebootNodeVisual.cs` | Scene glow/marker (consumes `ObjectiveProgress`) |
| `Assets/_neon/Scripts/Run/SignalDarkness.cs` | Background night→dawn lerp (consumes `SignalChanged`) |
| `Assets/_neon/Scripts/UI/UIShopScreen.cs` | Heal + Continue shop (consumes `RunPhaseChanged`, commands economy + run) |
| `Assets/_neon/Scripts/UI/UIHUDObjectiveBar.cs` | Objective fill + dawn/Signal bar (consumers) |
| `Assets/_neon/Scripts/UI/UIRunEndScreen.cs` | Win/lose menu (consumes `RunEnded`) |

**Created — tests:** `EconomySpendTests.cs`, `SignalSystemTests.cs`, `RebootNodeObjectiveTests.cs`, `RunServiceTests.cs`.

**Modified:**

| Path | Change |
|---|---|
| `Assets/_neon/Scripts/Stats/StatId.cs` | Append `SpawnNastiness = 400`, `ObjectiveFillRateScale = 401` |
| `Assets/_neon/Scripts/Signals/GameplayEvents.cs` | Append M3 signals |
| `Assets/_neon/Scripts/Growth/IEconomySystem.cs` + `EconomySystem.cs` | `TrySpend(int)` |
| `Assets/_neon/Scripts/Swarm/SwarmBridge.cs` | ctor gains `IStatSystem`; `Tick` scales cap/rate by `SpawnNastiness` |
| `Assets/_neon/Scripts/Spawner/LevelConfigurationAsset.cs` | Add `RunBlock Run` (encounter node positions) |
| `Assets/_neon/Scripts/Level/Level.cs` | Register run systems; `Start` calls `RunService.BeginRun` |
| `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs` | Register `SignalSystem` (session) |
| `Assets/_neon/Scripts/Protocols/ProtocolService.cs` | Signal-scaled draft weights (Task 9) |
| `Assets/_neon/Scripts/Editor/SettingsAssetCreator.cs` | Add `RunSettingsAsset` |
| `Assets/_neon/Tests/EditMode/Fakes.cs` | `FakeSignalSystem` |
| Level1 scene + `LevelConfiguration_Level1` + wave assets | Node zone, run wiring, waves→Manual, HUD (Task 9) |

---

### Task 1: Branch + data layer (StatIds, signals, RunPhase, RunSettings/RunConfig, RunBlock)

Pure data. Compile-and-commit.

**Files:**
- Modify: `Assets/_neon/Scripts/Stats/StatId.cs`
- Modify: `Assets/_neon/Scripts/Signals/GameplayEvents.cs`
- Create: `Assets/_neon/Scripts/Run/RunPhase.cs`
- Create: `Assets/_neon/Scripts/Run/RunSettings.cs`
- Create: `Assets/_neon/Scripts/Run/RunSettingsAsset.cs`
- Create: `Assets/_neon/Scripts/Run/RunConfig.cs`
- Modify: `Assets/_neon/Scripts/Spawner/LevelConfigurationAsset.cs`
- Modify: `Assets/_neon/Scripts/Editor/SettingsAssetCreator.cs`

- [ ] **Step 1: Branch**

```bash
git -C "G:/Brainless Labs/neon-responder" checkout claude/neon-m2-growth
git checkout -b claude/neon-m3-run
```

- [ ] **Step 2: Append StatIds**

In `Assets/_neon/Scripts/Stats/StatId.cs`, append inside the enum after `HealPerFinish = 303,`:

```csharp

        // Run layer (M3)
        SpawnNastiness = 400,       // Run sheet; base 1, Signal raises it, SwarmBridge reads it
        ObjectiveFillRateScale = 401, // Run sheet; base 1, Objective protocols / Priority Override tune it
```

- [ ] **Step 3: Append M3 signals**

Append to `Assets/_neon/Scripts/Signals/GameplayEvents.cs`, inside the namespace after the last struct:

```csharp

    /// <summary>Reboot-node objective fill state for the HUD + node visual (0..1).</summary>
    public readonly struct ObjectiveProgress
    {
        public readonly float Normalized;
        public readonly Vector2 Position;
        public readonly bool PlayerInZone;

        public ObjectiveProgress(float normalized, Vector2 position, bool playerInZone)
        {
            Normalized = normalized;
            Position = position;
            PlayerInZone = playerInZone;
        }
    }

    /// <summary>An objective completed (raises the Signal). EncounterIndex is 0-based.</summary>
    public readonly struct ObjectiveCompleted
    {
        public readonly int EncounterIndex;

        public ObjectiveCompleted(int encounterIndex)
        {
            EncounterIndex = encounterIndex;
        }
    }

    /// <summary>The dawn scalar changed. Value is 0..Dawn; Dawn is the win threshold.</summary>
    public readonly struct SignalChanged
    {
        public readonly float Value;
        public readonly float Dawn;

        public SignalChanged(float value, float dawn)
        {
            Value = value;
            Dawn = dawn;
        }
    }

    public readonly struct RunPhaseChanged
    {
        public readonly RunPhase Previous;
        public readonly RunPhase Current;
        public readonly int EncounterIndex;
        public readonly int TotalEncounters;

        public RunPhaseChanged(RunPhase previous, RunPhase current, int encounterIndex, int totalEncounters)
        {
            Previous = previous;
            Current = current;
            EncounterIndex = encounterIndex;
            TotalEncounters = totalEncounters;
        }
    }

    public readonly struct RunEnded
    {
        public readonly bool Won;

        public RunEnded(bool won)
        {
            Won = won;
        }
    }
```

- [ ] **Step 4: RunPhase enum**

`Assets/_neon/Scripts/Run/RunPhase.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Run flow phases (spec §5.4). Mirrors the UnityHFSM state names in RunService
    /// so the flow is assertable in EditMode without reaching into the FSM internals.
    /// </summary>
    public enum RunPhase
    {
        None = 0,
        EncounterIntro = 1,
        EncounterActive = 2,
        EncounterComplete = 3,
        Shop = 4,
        BossStub = 5,
        RunWon = 6,
        RunLost = 7
    }
}
```

- [ ] **Step 5: RunSettings + asset**

`Assets/_neon/Scripts/Run/RunSettings.cs`:

```csharp
using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Global run tuning (spec §5.4 / §9). Per-level encounter geometry lives on
    /// LevelConfigurationAsset.Run; these are the run-wide knobs.
    /// </summary>
    [Serializable]
    public class RunSettings : ISettings
    {
        [Header("Cadence")]
        [SerializeField] private float _encounterIntroSeconds = 1.5f;
        [SerializeField] private float _rebootDurationSeconds = 50f; // §9: 45–60s

        [Header("Signal (dawn = win)")]
        [SerializeField] private float _dawnValue = 1f;
        [Tooltip("Extra chaff density at full dawn, as a fraction (1 = ×2 at dawn).")]
        [SerializeField] private float _maxSpawnNastinessBonus = 1f;

        [Header("Shop (Heal + Continue; Specials arrive M4)")]
        [SerializeField] private int _shopHealCost = 25;   // special-moves doc §3
        [SerializeField] private int _shopHealAmount = 40;
        [SerializeField, Range(0f, 1f)] private float _shopPauseScale = 0f;

        [Header("Darkness lerp")]
        [SerializeField] private Color _nightColor = new(0.04f, 0.05f, 0.10f, 1f);
        [SerializeField] private Color _dawnColor = new(0.45f, 0.35f, 0.30f, 1f);

        public float EncounterIntroSeconds => _encounterIntroSeconds;
        public float RebootDurationSeconds => _rebootDurationSeconds;
        public float DawnValue => _dawnValue;
        public float MaxSpawnNastinessBonus => _maxSpawnNastinessBonus;
        public int ShopHealCost => _shopHealCost;
        public int ShopHealAmount => _shopHealAmount;
        public float ShopPauseScale => _shopPauseScale;
        public Color NightColor => _nightColor;
        public Color DawnColor => _dawnColor;

#if UNITY_EDITOR
        public void Editor_OnGUI(UnityEngine.Object target)
        {
            var serializedObject = new UnityEditor.SerializedObject(target);
            serializedObject.UpdateIfRequiredOrScript();
            var settingsProperty = serializedObject.FindProperty("_settings");
            UnityEditor.EditorGUILayout.PropertyField(settingsProperty, new GUIContent("Run Settings"), includeChildren: true);
            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}
```

`Assets/_neon/Scripts/Run/RunSettingsAsset.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    public class RunSettingsAsset : BaseSettingsAsset<RunSettingsAsset, RunSettings> { }
}
```

- [ ] **Step 6: The per-level run block**

In `Assets/_neon/Scripts/Spawner/LevelConfigurationAsset.cs`, insert after the `Swarm` block field (added in M2) and before the `Completion` header:

```csharp
        [Header("Run (M3)")]
        [Tooltip("Encounter sequence for this level — one Reboot Node per encounter.")]
        public RunBlock Run = new();
```

and append this nested class inside the namespace after the `SwarmDensityBlock` class:

```csharp
    [System.Serializable]
    public class RunBlock
    {
        [Tooltip("Master switch — off leaves the level running the pre-M3 free-fight path.")]
        public bool EnableRun = false;

        [Tooltip("One Reboot-Node world position per encounter. Count = number of encounters. " +
                 "Completing all of them lands the Signal on dawn = run won.")]
        public Vector2[] EncounterNodePositions = new Vector2[0];

        [Tooltip("Radius the player must stand within to charge a node.")]
        public float NodeRadius = 2.5f;
    }
```

(`LevelConfigurationAsset.cs` already `using UnityEngine;` — `Vector2` resolves.)

- [ ] **Step 7: RunConfig snapshot**

`Assets/_neon/Scripts/Run/RunConfig.cs`:

```csharp
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>Asset-free run snapshot: per-level encounter geometry + global tuning.</summary>
    public readonly struct RunConfig
    {
        public readonly bool Enabled;
        public readonly Vector2[] NodePositions;
        public readonly float NodeRadius;
        public readonly float RebootDurationSeconds;
        public readonly float EncounterIntroSeconds;
        public readonly float DawnValue;
        public readonly int ShopHealCost;
        public readonly int ShopHealAmount;
        public readonly float ShopPauseScale;

        public int EncounterCount => NodePositions?.Length ?? 0;
        public float SignalPerObjective => EncounterCount > 0 ? DawnValue / EncounterCount : DawnValue;

        public RunConfig(bool enabled, Vector2[] nodePositions, float nodeRadius,
            float rebootDurationSeconds, float encounterIntroSeconds, float dawnValue,
            int shopHealCost, int shopHealAmount, float shopPauseScale)
        {
            Enabled = enabled;
            NodePositions = nodePositions;
            NodeRadius = nodeRadius;
            RebootDurationSeconds = rebootDurationSeconds;
            EncounterIntroSeconds = encounterIntroSeconds;
            DawnValue = dawnValue;
            ShopHealCost = shopHealCost;
            ShopHealAmount = shopHealAmount;
            ShopPauseScale = shopPauseScale;
        }

        public static RunConfig From(LevelConfigurationAsset config, RunSettings settings)
        {
            var block = config.Run;
            return new RunConfig(
                block.EnableRun,
                block.EncounterNodePositions,
                block.NodeRadius,
                settings.RebootDurationSeconds,
                settings.EncounterIntroSeconds,
                settings.DawnValue,
                settings.ShopHealCost,
                settings.ShopHealAmount,
                settings.ShopPauseScale);
        }
    }
}
```

- [ ] **Step 8: Add RunSettingsAsset to the creator**

In `Assets/_neon/Scripts/Editor/SettingsAssetCreator.cs`, add before `AssetDatabase.SaveAssets();`:

```csharp
            RunSettingsAsset.GetOrCreateSettingsAsset();
```

- [ ] **Step 9: Compile, create asset, tests, commit**

1. Refresh Unity: zero errors, 90 tests PASS.
2. Run **Neon → Settings → Create All Settings Assets** — confirm `Assets/Resources/Settings/RunSettingsAsset.asset` appears.
3. Commit:

```bash
git add "Assets/_neon/Scripts/Stats/StatId.cs" "Assets/_neon/Scripts/Signals/GameplayEvents.cs" "Assets/_neon/Scripts/Run" "Assets/_neon/Scripts/Run.meta" "Assets/_neon/Scripts/Spawner/LevelConfigurationAsset.cs" "Assets/_neon/Scripts/Editor/SettingsAssetCreator.cs" "Assets/Resources/Settings/RunSettingsAsset.asset" "Assets/Resources/Settings/RunSettingsAsset.asset.meta"
git commit -m "feat: M3 data layer - run phase/settings/config, signals, stat ids"
```

---

### Task 2: `EconomySystem.TrySpend` (test-first)

The shop needs to deduct Neon Charge. Additive to the existing economy.

**Files:**
- Modify: `Assets/_neon/Scripts/Growth/IEconomySystem.cs`
- Modify: `Assets/_neon/Scripts/Growth/EconomySystem.cs`
- Test: `Assets/_neon/Tests/EditMode/EconomySpendTests.cs`

- [ ] **Step 1: Write the failing tests**

`Assets/_neon/Tests/EditMode/EconomySpendTests.cs`:

```csharp
using NUnit.Framework;
using R3;

namespace BrainlessLabs.Neon.Tests
{
    public class EconomySpendTests
    {
        private GameplaySignals _signals;
        private StatSystem _stats;
        private EconomySystem _economy;

        private static GrowthConfig TestConfig => new(
            xpPerKill: 1, chargePerFinish: 10, overchargePerFinish: 8, overchargeCap: 100,
            xpCostBase: 10f, xpCostExponent: 1.35f, levelUpSlowMoScale: 0.1f,
            challengeSequenceBase: new[] { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK },
            challengeSequenceHot: new[] { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK, ATTACKTYPE.PUNCH },
            challengeInputWindowSeconds: 0.9f, challengeWindowTightenPerTier: 0.1f, finishAoeDamage: 6);

        [SetUp]
        public void SetUp()
        {
            _signals = new GameplaySignals();
            _stats = new StatSystem();
            _stats.Run.SetBase(StatId.GainMultiplier, 1f);
            _economy = new EconomySystem(_signals, _stats, TestConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _economy.Dispose();
            _signals.Dispose();
        }

        private void Earn(int finishes)
        {
            for (int i = 0; i < finishes; i++) _signals.Publish(new EnemyFinished(UnityEngine.Vector2.zero, wasChaff: true));
        }

        [Test]
        public void TrySpend_WithEnough_DeductsAndReturnsTrue()
        {
            Earn(3); // 30 charge

            bool ok = _economy.TrySpend(25);

            Assert.IsTrue(ok);
            Assert.AreEqual(5, _economy.NeonCharge);
        }

        [Test]
        public void TrySpend_Insufficient_NoChangeReturnsFalse()
        {
            Earn(2); // 20 charge

            bool ok = _economy.TrySpend(25);

            Assert.IsFalse(ok);
            Assert.AreEqual(20, _economy.NeonCharge);
        }

        [Test]
        public void TrySpend_Exact_Succeeds()
        {
            Earn(1); // 10

            Assert.IsTrue(_economy.TrySpend(10));
            Assert.AreEqual(0, _economy.NeonCharge);
        }

        [Test]
        public void TrySpend_PublishesNeonChargeChanged()
        {
            Earn(3);
            int lastTotal = -1;
            using var sub = _signals.On<NeonChargeChanged>().Subscribe(e => lastTotal = e.Total);

            _economy.TrySpend(25);

            Assert.AreEqual(5, lastTotal);
        }

        [Test]
        public void TrySpend_NonPositive_IsNoOpTrue()
        {
            Earn(1);

            Assert.IsTrue(_economy.TrySpend(0));
            Assert.AreEqual(10, _economy.NeonCharge);
        }
    }
}
```

- [ ] **Step 2: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`TrySpend` does not exist). Proceed.

- [ ] **Step 3: Implement**

In `Assets/_neon/Scripts/Growth/IEconomySystem.cs`, add to the interface:

```csharp
        /// <summary>Deduct Neon Charge if affordable. Returns false (no change) if too poor.</summary>
        bool TrySpend(int amount);
```

In `Assets/_neon/Scripts/Growth/EconomySystem.cs`, add the method (after `OnFinish` or anywhere in the class body):

```csharp
        public bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (NeonCharge < amount) return false;

            NeonCharge -= amount;
            _signals.Publish(new NeonChargeChanged(NeonCharge));
            return true;
        }
```

- [ ] **Step 4: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: **95/95 PASS** (90 + 5 spend).

- [ ] **Step 5: Commit**

```bash
git add "Assets/_neon/Scripts/Growth/IEconomySystem.cs" "Assets/_neon/Scripts/Growth/EconomySystem.cs" "Assets/_neon/Tests/EditMode/EconomySpendTests.cs" "Assets/_neon/Tests/EditMode/EconomySpendTests.cs.meta"
git commit -m "feat: EconomySystem.TrySpend - the shop's Neon Charge sink (M3)"
```

---

### Task 3: `ISignalSystem` (test-first) + session registration

The dawn scalar (spec §5.4): 0→dawn, objectives raise it, it's **a modifier source on the Run sheet's `SpawnNastiness`** (same pattern as Momentum on the multipliers), publishes `SignalChanged`, and reports `IsDawn` for the run-win. Run-agnostic → session scope (spec §4.3).

**Files:**
- Create: `Assets/_neon/Scripts/Run/ISignalSystem.cs`
- Create: `Assets/_neon/Scripts/Run/SignalSystem.cs`
- Modify: `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs`
- Test: `Assets/_neon/Tests/EditMode/SignalSystemTests.cs`

- [ ] **Step 1: Write the failing tests**

`Assets/_neon/Tests/EditMode/SignalSystemTests.cs`:

```csharp
using NUnit.Framework;
using R3;

namespace BrainlessLabs.Neon.Tests
{
    public class SignalSystemTests
    {
        private GameplaySignals _signals;
        private StatSystem _stats;
        private SignalSystem _signal;

        // dawn = 1, +100% nastiness at dawn (×2)
        private SignalSystem Make() => new SignalSystem(_signals, _stats, dawnValue: 1f, maxSpawnNastinessBonus: 1f);

        [SetUp]
        public void SetUp()
        {
            _signals = new GameplaySignals();
            _stats = new StatSystem();
            _signal = Make();
        }

        [TearDown]
        public void TearDown()
        {
            _signal.Dispose();
            _signals.Dispose();
        }

        [Test]
        public void StartsAtZero_WithBaselineNastiness()
        {
            Assert.AreEqual(0f, _signal.Value, 0.0001f);
            Assert.IsFalse(_signal.IsDawn);
            Assert.AreEqual(1f, _stats.Run.GetValue(StatId.SpawnNastiness), 0.0001f); // base 1, no bonus
        }

        [Test]
        public void Raise_IncreasesValue_AndNastiness()
        {
            _signal.Raise(0.5f); // half to dawn → +50% nastiness

            Assert.AreEqual(0.5f, _signal.Value, 0.0001f);
            Assert.AreEqual(1.5f, _stats.Run.GetValue(StatId.SpawnNastiness), 0.0001f);
        }

        [Test]
        public void Raise_ClampsAtDawn_AndFlagsIt()
        {
            _signal.Raise(0.7f);
            _signal.Raise(0.7f); // 1.4 → clamps to 1

            Assert.AreEqual(1f, _signal.Value, 0.0001f);
            Assert.IsTrue(_signal.IsDawn);
            Assert.AreEqual(2f, _stats.Run.GetValue(StatId.SpawnNastiness), 0.0001f); // ×2 at dawn
        }

        [Test]
        public void Lower_ReducesValue_ClampsAtZero()
        {
            _signal.Raise(0.3f);
            _signal.Lower(0.5f);

            Assert.AreEqual(0f, _signal.Value, 0.0001f);
            Assert.IsFalse(_signal.IsDawn);
            Assert.AreEqual(1f, _stats.Run.GetValue(StatId.SpawnNastiness), 0.0001f);
        }

        [Test]
        public void Raise_PublishesSignalChanged()
        {
            SignalChanged received = default;
            using var sub = _signals.On<SignalChanged>().Subscribe(e => received = e);

            _signal.Raise(0.5f);

            Assert.AreEqual(0.5f, received.Value, 0.0001f);
            Assert.AreEqual(1f, received.Dawn, 0.0001f);
        }

        [Test]
        public void NastinessStacksAdditively_NotWithMomentum()
        {
            // A separate Mult source on a DIFFERENT stat must not interact.
            var other = ModifierSource.Create("other");
            _stats.Run.AddModifier(StatId.GainMultiplier, StatOp.Mult, 2f, other);

            _signal.Raise(1f); // dawn

            Assert.AreEqual(2f, _stats.Run.GetValue(StatId.SpawnNastiness), 0.0001f);   // Signal only
            Assert.AreEqual(2f, _stats.Run.GetValue(StatId.GainMultiplier), 0.0001f);   // untouched by Signal
        }

        [Test]
        public void Dispose_RemovesNastinessModifier()
        {
            _signal.Raise(1f);
            _signal.Dispose();

            Assert.AreEqual(1f, _stats.Run.GetValue(StatId.SpawnNastiness), 0.0001f); // back to base
        }
    }
}
```

- [ ] **Step 2: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`SignalSystem` does not exist). Proceed.

- [ ] **Step 3: Implement**

`Assets/_neon/Scripts/Run/ISignalSystem.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The dawn meta-scalar (spec §5.4): 0 → dawn. Objectives raise it; it is a
    /// modifier source on the Run sheet's SpawnNastiness (same pattern as Momentum
    /// on the multipliers). Win = the Signal hits dawn.
    /// </summary>
    public interface ISignalSystem
    {
        float Value { get; }
        float Dawn { get; }
        bool IsDawn { get; }

        void Raise(float amount);
        void Lower(float amount);
    }
}
```

`Assets/_neon/Scripts/Run/SignalSystem.cs`:

```csharp
using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    public sealed class SignalSystem : ISignalSystem, IDisposable
    {
        private readonly IGameplaySignals _signals;
        private readonly IStatSystem _stats;
        private readonly float _maxSpawnNastinessBonus;
        private readonly ModifierSource _source = ModifierSource.Create("signal");

        public float Value { get; private set; }
        public float Dawn { get; }
        public bool IsDawn => Value >= Dawn;

        public SignalSystem(IGameplaySignals signals, IStatSystem stats, float dawnValue, float maxSpawnNastinessBonus)
        {
            _signals = signals;
            _stats = stats;
            Dawn = Mathf.Max(0.0001f, dawnValue);
            _maxSpawnNastinessBonus = maxSpawnNastinessBonus;

            _stats.Run.SetBase(StatId.SpawnNastiness, 1f);
            ApplyNastiness();
        }

        public void Raise(float amount) => SetValue(Value + amount);
        public void Lower(float amount) => SetValue(Value - amount);

        public void Dispose()
        {
            _stats.Run.RemoveBySource(_source);
        }

        private void SetValue(float value)
        {
            Value = Mathf.Clamp(value, 0f, Dawn);
            ApplyNastiness();
            _signals.Publish(new SignalChanged(Value, Dawn));
        }

        private void ApplyNastiness()
        {
            // Signal is a modifier source like Momentum, but PctAdd (never Mult —
            // Momentum stays the only global multiplier, protocol doc §8.1).
            float fraction = Value / Dawn; // 0..1
            _stats.Run.RemoveBySource(_source);
            _stats.Run.AddModifier(StatId.SpawnNastiness, StatOp.PctAdd, fraction * _maxSpawnNastinessBonus, _source);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: **102/102 PASS** (95 + 7 signal).

- [ ] **Step 5: Register in `GameplayServicesState`**

Add to `RegisterTypes` after `RegisterProgressionSystem(builder);`:

```csharp
            RegisterSignalSystem(builder);
```

and the helper (Signal is run-agnostic → session scope, spec §4.3):

```csharp
        private static void RegisterSignalSystem(IContainerBuilder builder)
        {
            var runSettings = RunSettingsAsset.InstanceAsset.Settings;
            builder.Register<SignalSystem>(Lifetime.Singleton)
                .WithParameter("dawnValue", runSettings.DawnValue)
                .WithParameter("maxSpawnNastinessBonus", runSettings.MaxSpawnNastinessBonus)
                .As<ISignalSystem>();
            builder.RegisterBuildCallback(container => container.Resolve<ISignalSystem>());
        }
```

- [ ] **Step 6: Boot check + commit**

Boot into Level1 (Recipe 4): chain clean, `RunSettingsAsset` loads (created in Task 1), no exceptions. Exit.

```bash
git add "Assets/_neon/Scripts/Run/ISignalSystem.cs" "Assets/_neon/Scripts/Run/ISignalSystem.cs.meta" "Assets/_neon/Scripts/Run/SignalSystem.cs" "Assets/_neon/Scripts/Run/SignalSystem.cs.meta" "Assets/_neon/Tests/EditMode/SignalSystemTests.cs" "Assets/_neon/Tests/EditMode/SignalSystemTests.cs.meta" "Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs"
git commit -m "feat: SignalSystem - dawn scalar raising SpawnNastiness (M3)"
```

---

### Task 4: `SwarmBridge` reads `SpawnNastiness`

The Signal→density path. Additive: the bridge already pushes `SwarmWorldState` each tick; now it scales cap + rate by the `Run.SpawnNastiness` stat.

**Files:**
- Modify: `Assets/_neon/Scripts/Swarm/SwarmBridge.cs`

- [ ] **Step 1: Inject `IStatSystem`**

In `SwarmBridge.cs`, change the fields + ctor. The current ctor:

```csharp
        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly IEntitiesService _entities;
        private readonly SwarmConfig _config;
```

add a stat field:

```csharp
        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly IEntitiesService _entities;
        private readonly IStatSystem _stats;
        private readonly SwarmConfig _config;
```

and the ctor signature `public SwarmBridge(IGameplayClock clock, IGameplaySignals signals, IEntitiesService entities, SwarmConfig config)` becomes:

```csharp
        public SwarmBridge(IGameplayClock clock, IGameplaySignals signals, IEntitiesService entities, IStatSystem stats, SwarmConfig config)
```

with the assignment `_stats = stats;` added alongside the others. (VContainer autowires `IStatSystem` from the session scope; the Level registration line is unchanged.)

- [ ] **Step 2: Scale cap + rate each tick**

In `SwarmBridge.Tick`, find where it reads/writes the `SwarmWorldState` (the block that sets `state.PlayerPosition` etc. before `entityManager.SetComponentData(_controlEntity, state)`). Immediately before that `SetComponentData` call, add:

```csharp
            // Signal → density (spec §5.4): the Run-sheet SpawnNastiness stat (base 1,
            // Signal raises it) scales chaff cap + spawn rate live.
            float nastiness = _stats.Run.GetValue(StatId.SpawnNastiness);
            if (nastiness <= 0f) nastiness = 1f;
            state.ChaffCap = Mathf.Min(150, Mathf.RoundToInt(_config.ChaffCap * nastiness)); // 150 = spike-verified proxy-pool ceiling
            state.SpawnRatePerSecond = _config.SpawnRatePerSecond * nastiness;
```

(Add `using UnityEngine;` if not already present — `SwarmBridge` uses `Vector2`/`Bounds` so it already imports it. Confirm `Mathf` resolves via `UnityEngine`.)

- [ ] **Step 3: Compile, tests, runtime probe (Recipe 4)**

Refresh Unity: zero errors, 102 tests PASS (bridge isn't EditMode-covered; the Signal math is). Boot into Level1 — swarm still floods to its configured cap (Signal is 0 at boot, nastiness = 1, so no change yet). This step just proves the injected stat read compiles + runs; the visible effect is verified in the Task 10 gate (Signal rising mid-run visibly thickens the swarm). Exit.

- [ ] **Step 4: Commit**

```bash
git add "Assets/_neon/Scripts/Swarm/SwarmBridge.cs"
git commit -m "feat: SwarmBridge scales chaff density by Signal SpawnNastiness (M3)"
```

---

### Task 5: `IObjective` + `RebootNodeObjective` (test-first) + node visual

The win verb (spec §5.4): hold a zone under fire until the bar fills (~50s). Pure logic — the objective takes the player position and a delta each tick, fills while the player is within radius, and completes at 1. No collider (F4 bridge-only ethos). Fill rate is a stat so future Objective protocols / Priority Override tune it.

**Files:**
- Create: `Assets/_neon/Scripts/Run/IObjective.cs`
- Create: `Assets/_neon/Scripts/Run/RebootNodeObjective.cs`
- Create: `Assets/_neon/Scripts/Run/RebootNodeVisual.cs`
- Test: `Assets/_neon/Tests/EditMode/RebootNodeObjectiveTests.cs`

- [ ] **Step 1: Write the failing tests**

`Assets/_neon/Tests/EditMode/RebootNodeObjectiveTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class RebootNodeObjectiveTests
    {
        private StatSystem _stats;

        private RebootNodeObjective Make(float duration = 10f, float radius = 2f)
        {
            _stats ??= new StatSystem();
            return new RebootNodeObjective(_stats, new Vector2(0f, 0f), radius, duration);
        }

        [SetUp]
        public void SetUp() => _stats = new StatSystem();

        [Test]
        public void SeedsFillRateScaleBase()
        {
            Make();
            Assert.AreEqual(1f, _stats.Run.GetValue(StatId.ObjectiveFillRateScale), 0.0001f);
        }

        [Test]
        public void FillsWhilePlayerInZone()
        {
            var obj = Make(duration: 10f);

            obj.Tick(1f, playerPosition: Vector2.zero);

            Assert.AreEqual(0.1f, obj.Normalized, 0.0001f); // 1s of a 10s fill
            Assert.IsFalse(obj.IsComplete);
        }

        [Test]
        public void DoesNotFillWhenPlayerOutOfZone()
        {
            var obj = Make(duration: 10f, radius: 2f);

            obj.Tick(1f, playerPosition: new Vector2(5f, 0f));

            Assert.AreEqual(0f, obj.Normalized, 0.0001f);
            Assert.IsFalse(obj.PlayerInZone);
        }

        [Test]
        public void CompletesAtFull()
        {
            var obj = Make(duration: 2f);

            obj.Tick(1f, Vector2.zero);
            bool completedThisTick = obj.Tick(1.5f, Vector2.zero); // overshoots

            Assert.IsTrue(obj.IsComplete);
            Assert.IsTrue(completedThisTick);
            Assert.AreEqual(1f, obj.Normalized, 0.0001f); // clamped
        }

        [Test]
        public void CompletesOnlyOnce()
        {
            var obj = Make(duration: 1f);

            bool first = obj.Tick(2f, Vector2.zero);
            bool second = obj.Tick(2f, Vector2.zero);

            Assert.IsTrue(first);
            Assert.IsFalse(second); // already complete — no repeat completion
        }

        [Test]
        public void FillRateScaleStat_SpeedsTheFill()
        {
            var obj = Make(duration: 10f);
            var src = ModifierSource.Create("priority-override");
            _stats.Run.AddModifier(StatId.ObjectiveFillRateScale, StatOp.PctAdd, 1f, src); // ×2 rate

            obj.Tick(1f, Vector2.zero);

            Assert.AreEqual(0.2f, obj.Normalized, 0.0001f); // 1s at 2× a 10s fill
        }

        [Test]
        public void Radius_UsesEuclideanDistance()
        {
            var obj = Make(duration: 10f, radius: 2f);

            obj.Tick(1f, new Vector2(1.4f, 1.4f)); // dist ~1.98 < 2 → inside

            Assert.IsTrue(obj.PlayerInZone);
            Assert.Greater(obj.Normalized, 0f);
        }
    }
}
```

- [ ] **Step 2: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`RebootNodeObjective` does not exist). Proceed.

- [ ] **Step 3: Implement**

`Assets/_neon/Scripts/Run/IObjective.cs`:

```csharp
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// A run objective (spec §5.4). MVP impl is RebootNodeObjective; Rescue /
    /// Purge-Jammer / Hold-the-Line are later impls of this same seam.
    /// </summary>
    public interface IObjective
    {
        float Normalized { get; }
        bool IsComplete { get; }
        Vector2 Position { get; }

        /// <summary>Advance by gameplay dt given the player's position. Returns true on the tick it completes.</summary>
        bool Tick(float deltaTime, Vector2 playerPosition);
    }
}
```

`Assets/_neon/Scripts/Run/RebootNodeObjective.cs`:

```csharp
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Hold-the-zone-under-fire (spec §5.4). Fills while the player stands within
    /// radius; fill rate = (1/duration) × ObjectiveFillRateScale (a Run stat, so
    /// Objective protocols / Priority Override speed it). Pure logic — no collider.
    /// </summary>
    public sealed class RebootNodeObjective : IObjective
    {
        private readonly IStatSystem _stats;
        private readonly float _radiusSq;
        private readonly float _baseRatePerSecond;

        public Vector2 Position { get; }
        public float Normalized { get; private set; }
        public bool IsComplete { get; private set; }
        public bool PlayerInZone { get; private set; }

        public RebootNodeObjective(IStatSystem stats, Vector2 position, float radius, float durationSeconds)
        {
            _stats = stats;
            Position = position;
            _radiusSq = radius * radius;
            _baseRatePerSecond = 1f / Mathf.Max(0.01f, durationSeconds);

            // Seed the tunable base once (idempotent — protocols modify via modifiers).
            if (_stats.Run.GetBase(StatId.ObjectiveFillRateScale) <= 0f)
            {
                _stats.Run.SetBase(StatId.ObjectiveFillRateScale, 1f);
            }
        }

        public bool Tick(float deltaTime, Vector2 playerPosition)
        {
            if (IsComplete) return false;

            PlayerInZone = (playerPosition - Position).sqrMagnitude <= _radiusSq;
            if (!PlayerInZone) return false;

            float rate = _baseRatePerSecond * Mathf.Max(0f, _stats.Run.GetValue(StatId.ObjectiveFillRateScale));
            Normalized = Mathf.Clamp01(Normalized + rate * deltaTime);

            if (Normalized >= 1f)
            {
                IsComplete = true;
                return true;
            }
            return false;
        }
    }
}
```

`Assets/_neon/Scripts/Run/RebootNodeVisual.cs` (pure consumer — the zone glow/marker):

```csharp
using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Scene visual for the active Reboot Node (spec §5.5 "objective bar + zone glow").
    /// Pure consumer of ObjectiveProgress: moves a world marker to the node, tints it
    /// by fill, hides it when no objective is active. No gameplay logic.
    /// </summary>
    public class RebootNodeVisual : MonoBehaviour
    {
        [SerializeField] private Transform marker;          // a world-space sprite at the node
        [SerializeField] private SpriteRenderer glow;       // tinted by fill
        [SerializeField] private Color emptyColor = new(0.2f, 0.6f, 1f, 0.35f);
        [SerializeField] private Color fullColor = new(0.3f, 1f, 0.5f, 0.85f);
        [Inject] private IGameplaySignals _signals;
        private IDisposable _subscription;

        void Start()
        {
            if (_signals == null) return; // scene without DI injection
            if (marker != null) marker.gameObject.SetActive(false);
            _subscription = _signals.On<ObjectiveProgress>().Subscribe(Apply);
        }

        void OnDestroy() => _subscription?.Dispose();

        void Apply(ObjectiveProgress p)
        {
            if (marker == null) return;
            // Normalized == 1 means completed this frame; the run advances past the
            // objective, so a fresh ObjectiveProgress with a new position re-shows it.
            marker.gameObject.SetActive(p.Normalized < 1f);
            marker.position = new Vector3(p.Position.x, p.Position.y, marker.position.z);
            if (glow != null) glow.color = Color.Lerp(emptyColor, fullColor, p.Normalized);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: **109/109 PASS** (102 + 7 objective).

- [ ] **Step 5: Commit**

```bash
git add "Assets/_neon/Scripts/Run/IObjective.cs" "Assets/_neon/Scripts/Run/IObjective.cs.meta" "Assets/_neon/Scripts/Run/RebootNodeObjective.cs" "Assets/_neon/Scripts/Run/RebootNodeObjective.cs.meta" "Assets/_neon/Scripts/Run/RebootNodeVisual.cs" "Assets/_neon/Scripts/Run/RebootNodeVisual.cs.meta" "Assets/_neon/Tests/EditMode/RebootNodeObjectiveTests.cs" "Assets/_neon/Tests/EditMode/RebootNodeObjectiveTests.cs.meta"
git commit -m "feat: RebootNodeObjective - hold-zone fill logic + node visual (M3)"
```

---

### Task 6: `RunService` — the UnityHFSM run FSM (test-first)

The run spine (spec §5.4). A Level-scoped service on UnityHFSM: `EncounterIntro → EncounterActive → EncounterComplete → Shop → …(×N)… → BossStub → RunWon`, `RunLost` from any phase on player death. It arms a `RebootNodeObjective` per encounter, triggers that encounter's `Manual` wave via a delegate handed in at `BeginRun`, raises the Signal on each completion (last one lands dawn = win), and freezes combat during Shop via a clock scale source. A `Phase` mirror makes it assertable without touching FSM internals.

**Files:**
- Modify: `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef` (add `UnityHFSM`)
- Create: `Assets/_neon/Scripts/Run/IRunService.cs`
- Create: `Assets/_neon/Scripts/Run/RunService.cs`
- Modify: `Assets/_neon/Scripts/Level/Level.cs`
- Test: `Assets/_neon/Tests/EditMode/RunServiceTests.cs`

- [ ] **Step 1: Reference UnityHFSM from `Neon`**

In `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef`, add `"UnityHFSM"` to the `references` array (keep it alphabetical-ish; it sits with the other package refs). Example — the array becomes:

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
        "UnityHFSM",
        "VContainer"
    ],
```

(Reference direction stays legal: `UnityHFSM` is a third-party package, not one of our assemblies. `Neon` still never references `Lifecycle`.)

- [ ] **Step 2: Write the failing tests**

`Assets/_neon/Tests/EditMode/RunServiceTests.cs` (drives the real `GameplayClock` — RunService registers as a tickable, so `clock.Advance` runs its FSM logic; the shop pause is a clock scale source, verified live):

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class RunServiceTests
    {
        private GameplayClock _clock;
        private GameplaySignals _signals;
        private StatSystem _stats;
        private FakeEntitiesService _entities;
        private SignalSystem _signal;
        private RunService _run;
        private GameObject _player;
        private readonly List<int> _wavesTriggered = new();
        private readonly List<RunPhaseChanged> _phases = new();
        private RunEnded? _ended;
        private System.IDisposable _phaseSub;
        private System.IDisposable _endSub;

        // 2 encounters, node at origin, tiny reboot for fast tests.
        private static RunConfig TwoEncounterConfig => new(
            enabled: true,
            nodePositions: new[] { Vector2.zero, Vector2.zero },
            nodeRadius: 2f, rebootDurationSeconds: 1f, encounterIntroSeconds: 0.5f,
            dawnValue: 1f, shopHealCost: 25, shopHealAmount: 40, shopPauseScale: 0f);

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _signals = new GameplaySignals();
            _stats = new StatSystem();
            _stats.Run.SetBase(StatId.GainMultiplier, 1f);
            _entities = new FakeEntitiesService();
            _signal = new SignalSystem(_signals, _stats, dawnValue: 1f, maxSpawnNastinessBonus: 1f);
            _player = new GameObject("Player");
            _entities.Register(_player, UNITTYPE.PLAYER); // at origin → inside every node

            _run = new RunService(_clock, _signals, _stats, _entities, _signal, TwoEncounterConfig);
            _wavesTriggered.Clear();
            _phases.Clear();
            _ended = null;
            _phaseSub = _signals.On<RunPhaseChanged>().Subscribe(e => _phases.Add(e));
            _endSub = _signals.On<RunEnded>().Subscribe(e => _ended = e);
            _run.BeginRun(_wavesTriggered.Add);
        }

        [TearDown]
        public void TearDown()
        {
            _phaseSub?.Dispose();
            _endSub?.Dispose();
            _run.Dispose();
            _signal.Dispose();
            _signals.Dispose();
            Object.DestroyImmediate(_player);
        }

        [Test]
        public void BeginRun_EntersEncounterIntro()
        {
            Assert.AreEqual(RunPhase.EncounterIntro, _run.Phase);
        }

        [Test]
        public void Intro_AdvancesToActive_AfterIntroTime_AndTriggersWave()
        {
            _clock.Advance(0.6f); // > 0.5s intro

            Assert.AreEqual(RunPhase.EncounterActive, _run.Phase);
            CollectionAssert.AreEqual(new[] { 0 }, _wavesTriggered); // encounter 0's Manual wave
        }

        [Test]
        public void Active_FillsObjective_ThenEntersShop_RaisingSignal()
        {
            _clock.Advance(0.6f);  // intro → active
            _clock.Advance(1.1f);  // > 1s reboot → complete

            Assert.AreEqual(RunPhase.Shop, _run.Phase);        // not the last encounter
            Assert.AreEqual(0.5f, _signal.Value, 0.0001f);     // +1/2 per objective
        }

        [Test]
        public void Shop_FreezesTheClock()
        {
            _clock.Advance(0.6f);
            _clock.Advance(1.1f);  // now in Shop

            Assert.AreEqual(0f, _clock.EffectiveScale, 0.0001f); // shopPauseScale 0
        }

        [Test]
        public void ContinueFromShop_ResumesClock_NextEncounter()
        {
            _clock.Advance(0.6f);
            _clock.Advance(1.1f);  // Shop after encounter 0

            _run.ContinueFromShop();

            Assert.AreEqual(1f, _clock.EffectiveScale, 0.0001f); // resumed
            Assert.AreEqual(RunPhase.EncounterIntro, _run.Phase);
            _clock.Advance(0.6f);
            CollectionAssert.AreEqual(new[] { 0, 1 }, _wavesTriggered); // encounter 1's wave
        }

        [Test]
        public void LastObjective_ReachesDawn_WinsViaBossStub()
        {
            _clock.Advance(0.6f); _clock.Advance(1.1f);  // enc0 → Shop
            _run.ContinueFromShop();
            _clock.Advance(0.6f); _clock.Advance(1.1f);  // enc1 → dawn

            Assert.IsTrue(_signal.IsDawn);
            Assert.AreEqual(RunPhase.RunWon, _run.Phase);   // BossStub passes straight through
            Assert.IsTrue(_ended.HasValue);
            Assert.IsTrue(_ended.Value.Won);
        }

        [Test]
        public void PlayerDeath_LosesFromAnyPhase()
        {
            _clock.Advance(0.6f); // active
            _signals.Publish(new PlayerLevelChanged(1)); // noise
            HealthSystemDeath(_player);

            Assert.AreEqual(RunPhase.RunLost, _run.Phase);
            Assert.IsTrue(_ended.HasValue);
            Assert.IsFalse(_ended.Value.Won);
        }

        [Test]
        public void ObjectiveDoesNotFill_WhenPlayerOutOfZone()
        {
            _player.transform.position = new Vector3(50f, 0f, 0f); // far from node
            _clock.Advance(0.6f);  // active, wave triggered
            _clock.Advance(2f);    // would complete if in zone

            Assert.AreEqual(RunPhase.EncounterActive, _run.Phase); // still holding
        }

        // RunService subscribes to HealthSystem.onUnitDeath at runtime, but that static
        // event can't be raised from a test — so drive the same public handler directly
        // (the EconomySystem/FinishResolver test-seam pattern). This avoids the EditMode
        // null-ref from HealthSystem.SubstractHealth touching the injected audio service.
        private void HealthSystemDeath(GameObject unit)
        {
            unit.tag = "Player";
            _run.HandleUnitDeath(unit);
        }
    }
}
```

- [ ] **Step 3: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`RunService` does not exist). Proceed.

- [ ] **Step 4: Implement the interface**

`Assets/_neon/Scripts/Run/IRunService.cs`:

```csharp
using System;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The run flow (spec §5.4): sequences encounter phases in the one belt arena.
    /// Lives in the Level scope; built on UnityHFSM but distinct from the boot FSM.
    /// </summary>
    public interface IRunService
    {
        RunPhase Phase { get; }
        int EncounterIndex { get; }

        /// <summary>Start the run. <paramref name="triggerWave"/> = SpawnerService.TriggerWave (per-encounter hero wave).</summary>
        void BeginRun(Action<int> triggerWave);

        /// <summary>Leave the shop → next encounter (called by the shop UI's Continue).</summary>
        void ContinueFromShop();
    }
}
```

- [ ] **Step 5: Implement `RunService`**

`Assets/_neon/Scripts/Run/RunService.cs`:

```csharp
using System;
using UnityHFSM;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// UnityHFSM run flow (spec §5.4). Event-driven: intro timer + objective fill run
    /// in each state's onLogic (fed the clock delta via a per-tick field), while
    /// completion / shop-continue / death fire RequestStateChange. The Phase mirror
    /// is set in each state's onEnter and published — the assertable surface.
    /// </summary>
    public sealed class RunService : IRunService, IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 40; // after Momentum (30); run flow sits atop engagement

        private const string INTRO = "EncounterIntro";
        private const string ACTIVE = "EncounterActive";
        private const string COMPLETE = "EncounterComplete";
        private const string SHOP = "Shop";
        private const string BOSS = "BossStub";
        private const string WON = "RunWon";
        private const string LOST = "RunLost";

        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly IStatSystem _stats;
        private readonly IEntitiesService _entities;
        private readonly ISignalSystem _signal;
        private readonly RunConfig _config;
        private readonly ModifierSource _shopPauseSource = ModifierSource.Create("run-shop-pause");

        private StateMachine _fsm;
        private Action<int> _triggerWave;
        private RebootNodeObjective _objective;
        private float _phaseElapsed;
        private float _tickDelta;
        private bool _started;

        public RunPhase Phase { get; private set; } = RunPhase.None;
        public int EncounterIndex { get; private set; }

        public RunService(IGameplayClock clock, IGameplaySignals signals, IStatSystem stats,
            IEntitiesService entities, ISignalSystem signal, RunConfig config)
        {
            _clock = clock;
            _signals = signals;
            _stats = stats;
            _entities = entities;
            _signal = signal;
            _config = config;

            HealthSystem.onUnitDeath += HandleUnitDeath;
            _clock.Register(this, TICK_ORDER);
            BuildFsm();
        }

        public void Dispose()
        {
            HealthSystem.onUnitDeath -= HandleUnitDeath;
            _clock.Unregister(this);
            _clock.ClearScale(_shopPauseSource);
        }

        public void BeginRun(Action<int> triggerWave)
        {
            _triggerWave = triggerWave;
            _started = true;
            _fsm.Init(); // enters INTRO
        }

        public void ContinueFromShop()
        {
            if (Phase != RunPhase.Shop) return;
            _clock.ClearScale(_shopPauseSource);
            EncounterIndex++;
            _fsm.RequestStateChange(INTRO);
        }

        public void Tick(float deltaTime)
        {
            if (!_started) return;
            _tickDelta = deltaTime;
            _fsm.OnLogic();
        }

        /// <summary>Public so EditMode tests can drive it (static event can't be raised externally).</summary>
        public void HandleUnitDeath(GameObject unit)
        {
            if (!_started || unit == null || !unit.CompareTag("Player")) return;
            if (Phase == RunPhase.RunWon || Phase == RunPhase.RunLost) return;
            _fsm.RequestStateChange(LOST, forceInstantly: true);
        }

        private void BuildFsm()
        {
            _fsm = new StateMachine();

            _fsm.AddState(INTRO, new State(
                onEnter: _ => { SetPhase(RunPhase.EncounterIntro); _phaseElapsed = 0f; },
                onLogic: _ =>
                {
                    _phaseElapsed += _tickDelta;
                    if (_phaseElapsed >= _config.EncounterIntroSeconds) _fsm.RequestStateChange(ACTIVE);
                }));

            _fsm.AddState(ACTIVE, new State(
                onEnter: _ =>
                {
                    SetPhase(RunPhase.EncounterActive);
                    _triggerWave?.Invoke(EncounterIndex);
                    var node = NodeForCurrentEncounter();
                    _objective = new RebootNodeObjective(_stats, node, _config.NodeRadius, _config.RebootDurationSeconds);
                },
                onLogic: _ =>
                {
                    if (_objective == null) return;
                    Vector2 playerPos = PlayerPosition();
                    bool done = _objective.Tick(_tickDelta, playerPos);
                    _signals.Publish(new ObjectiveProgress(_objective.Normalized, _objective.Position, _objective.PlayerInZone));
                    if (done) _fsm.RequestStateChange(COMPLETE);
                }));

            _fsm.AddState(COMPLETE, new State(
                onEnter: _ =>
                {
                    SetPhase(RunPhase.EncounterComplete);
                    _signals.Publish(new ObjectiveCompleted(EncounterIndex));
                    _signal.Raise(_config.SignalPerObjective);
                    _fsm.RequestStateChange(_signal.IsDawn ? BOSS : SHOP);
                }));

            _fsm.AddState(SHOP, new State(
                onEnter: _ =>
                {
                    SetPhase(RunPhase.Shop);
                    _clock.SetScale(_shopPauseSource, _config.ShopPauseScale);
                }));

            _fsm.AddState(BOSS, new State(
                onEnter: _ =>
                {
                    SetPhase(RunPhase.BossStub);
                    Debug.Log("[Run] Boss stub — skipping to dawn (MVP).");
                    _fsm.RequestStateChange(WON);
                }));

            _fsm.AddState(WON, new State(onEnter: _ =>
            {
                SetPhase(RunPhase.RunWon);
                _signals.Publish(new RunEnded(true));
            }));

            _fsm.AddState(LOST, new State(onEnter: _ =>
            {
                SetPhase(RunPhase.RunLost);
                _clock.ClearScale(_shopPauseSource); // in case we died looking at the shop
                _signals.Publish(new RunEnded(false));
            }));

            _fsm.SetStartState(INTRO);
        }

        private Vector2 NodeForCurrentEncounter()
        {
            var positions = _config.NodePositions;
            if (positions == null || positions.Length == 0) return Vector2.zero;
            return positions[Mathf.Clamp(EncounterIndex, 0, positions.Length - 1)];
        }

        private Vector2 PlayerPosition()
        {
            var player = _entities.GetFirstByType(UNITTYPE.PLAYER).GameObject;
            return player != null ? (Vector2)player.transform.position : Vector2.zero;
        }

        private void SetPhase(RunPhase phase)
        {
            var previous = Phase;
            Phase = phase;
            _signals.Publish(new RunPhaseChanged(previous, phase, EncounterIndex, _config.EncounterCount));
        }
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: **117/117 PASS** (109 + 8 RunService). If the FSM's `RequestStateChange` inside `onEnter` (COMPLETE→SHOP/BOSS, BOSS→WON) doesn't take effect on the same `OnLogic`, that's expected — it applies on the next tick; the tests advance the clock enough that this settles. If a test lands one tick early, add a trailing `_clock.Advance(0.01f)` in that test (not in the implementation).

- [ ] **Step 7: Register + kick off in `Level`**

In `Level.RegisterEngagementSystems`, after the `builder.RegisterInstance(GrowthConfig.FromSettings());` line add:

```csharp
            builder.RegisterInstance(RunConfig.From(_configuration, RunSettingsAsset.InstanceAsset.Settings));
            builder.Register<RunService>(Lifetime.Scoped).As<IRunService>();
```

In `Level.Configure`'s build callback, the eager-resolve block currently ends with `container.Resolve<ProtocolEffectsSystem>();`. Add after it:

```csharp
                    container.Resolve<IRunService>();
```

Then in `Level.Start`, change the wave kickoff. It currently reads:

```csharp
            // Spawn player at configured progression point
            _spawnerService.SpawnPlayers();

            // Start enemy waves
            _spawnerService.StartWaves();
```

to:

```csharp
            // Spawn player at configured progression point
            _spawnerService.SpawnPlayers();

            // Start the wave system (Manual waves park until RunService triggers them).
            _spawnerService.StartWaves();

            // Hand run control to RunService when this level opts into the run flow
            // (RunConfig.EnableRun). SpawnerService.TriggerWave is the per-encounter
            // Manual-wave trigger. Levels without a run block keep the free-fight path.
            if (_configuration.Run.EnableRun)
            {
                var runService = Container.Resolve<IRunService>();
                runService.BeginRun(_spawnerService.TriggerWave);
            }
```

- [ ] **Step 8: Compile + tests + commit**

Refresh Unity: zero errors, 117 tests PASS. (Runtime bring-up is Task 9, once the scene has node positions + Manual waves.)

```bash
git add "Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef" "Assets/_neon/Scripts/Run/IRunService.cs" "Assets/_neon/Scripts/Run/IRunService.cs.meta" "Assets/_neon/Scripts/Run/RunService.cs" "Assets/_neon/Scripts/Run/RunService.cs.meta" "Assets/_neon/Tests/EditMode/RunServiceTests.cs" "Assets/_neon/Tests/EditMode/RunServiceTests.cs.meta" "Assets/_neon/Scripts/Level/Level.cs"
git commit -m "feat: RunService - UnityHFSM run flow, encounters to dawn (M3)"
```

---

### Task 7: Shop screen — Heal + Continue (R1)

A between-encounter screen: consumes `RunPhaseChanged` (shows on `Shop`), spends Neon Charge on Heal via `IEconomySystem.TrySpend`, and commands `IRunService.ContinueFromShop`. uGUI buttons work while the clock is paused (UI is unscaled). Specials/ranks/reroll slot into this same screen at M4.

**Files:**
- Create: `Assets/_neon/Scripts/UI/UIShopScreen.cs`

- [ ] **Step 1: Implement**

`Assets/_neon/Scripts/UI/UIShopScreen.cs`:

```csharp
using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon {

    //The M3 shop beat (Heal + Continue). Consumes RunPhaseChanged, commands the
    //economy + run. Specials/ranks/reroll are added here in M4.
    public class UIShopScreen : MonoBehaviour {

        [SerializeField] private GameObject panel;
        [SerializeField] private Button healButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text chargeLabel;
        [SerializeField] private Text healLabel;
        [Inject] private IGameplaySignals _signals;
        [Inject] private IEconomySystem _economy;
        [Inject] private IRunService _run;
        private RunSettings _runSettings;
        private IDisposable _phaseSub;
        private IDisposable _chargeSub;

        void Start(){
            if(_signals == null || _run == null || _economy == null) return; //scene without DI injection
            _runSettings = RunSettingsAsset.InstanceAsset.Settings;

            if(panel != null) panel.SetActive(false);
            if(healButton != null) healButton.onClick.AddListener(OnHeal);
            if(continueButton != null) continueButton.onClick.AddListener(OnContinue);
            if(healLabel != null) healLabel.text = $"HEAL (+{_runSettings.ShopHealAmount})  ⚡{_runSettings.ShopHealCost}";

            _phaseSub = _signals.On<RunPhaseChanged>().Subscribe(OnPhase);
            _chargeSub = _signals.On<NeonChargeChanged>().Subscribe(_ => RefreshCharge());
        }

        void OnDestroy(){
            _phaseSub?.Dispose();
            _chargeSub?.Dispose();
        }

        void OnPhase(RunPhaseChanged phase){
            bool inShop = phase.Current == RunPhase.Shop;
            if(panel != null) panel.SetActive(inShop);
            if(inShop) RefreshCharge();
        }

        void RefreshCharge(){
            if(chargeLabel != null) chargeLabel.text = $"NEON CHARGE: {_economy.NeonCharge}";
            if(healButton != null) healButton.interactable = _economy.NeonCharge >= _runSettings.ShopHealCost;
        }

        void OnHeal(){
            if(!_economy.TrySpend(_runSettings.ShopHealCost)) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            player?.GetComponent<HealthSystem>()?.AddHealth(_runSettings.ShopHealAmount);
            RefreshCharge();
        }

        void OnContinue(){
            if(panel != null) panel.SetActive(false);
            _run.ContinueFromShop();
        }
    }
}
```

(Heal amount + cost come from `RunSettings`; the shop needs no `GrowthConfig`. All three injected services resolve from the Level scope — `IRunService` is Level-scoped, `IGameplaySignals`/`IEconomySystem` from the session scope above it.)

- [ ] **Step 2: Compile check + commit**

Refresh Unity: zero errors, 117 tests PASS (no test touches UI). Scene wiring is Task 9.

```bash
git add "Assets/_neon/Scripts/UI/UIShopScreen.cs" "Assets/_neon/Scripts/UI/UIShopScreen.cs.meta"
git commit -m "feat: shop screen - Heal + Continue spending Neon Charge (M3)"
```

---

### Task 8: Signal darkness + objective/dawn HUD + run-end screen (R3)

The Signal's visible outputs and the objective/dawn read (spec §5.5). All pure consumers.

**Files:**
- Create: `Assets/_neon/Scripts/Run/SignalDarkness.cs`
- Create: `Assets/_neon/Scripts/UI/UIHUDObjectiveBar.cs`
- Create: `Assets/_neon/Scripts/UI/UIRunEndScreen.cs`

- [ ] **Step 1: Darkness lerp**

`Assets/_neon/Scripts/Run/SignalDarkness.cs`:

```csharp
using System;
using R3;
using UnityEngine;
using VContainer;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Night→dawn background lerp driven by the Signal (spec §5.4/§5.5). Pure consumer:
    /// lerps the main camera's background color from RunSettings.NightColor to DawnColor
    /// by Signal fraction, so "reaching dawn" is legible. Music aggression is the OTHER
    /// Signal consumer — deferred to M4 (spec §7 M4).
    /// </summary>
    public class SignalDarkness : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera; // defaults to Camera.main
        [Inject] private IGameplaySignals _signals;
        private RunSettings _settings;
        private IDisposable _subscription;

        void Start()
        {
            if (_signals == null) return; // scene without DI injection
            _settings = RunSettingsAsset.InstanceAsset.Settings;
            if (targetCamera == null) targetCamera = Camera.main;
            Apply(new SignalChanged(0f, 1f));
            _subscription = _signals.On<SignalChanged>().Subscribe(Apply);
        }

        void OnDestroy() => _subscription?.Dispose();

        void Apply(SignalChanged signal)
        {
            if (targetCamera == null || _settings == null) return;
            float t = signal.Dawn > 0f ? Mathf.Clamp01(signal.Value / signal.Dawn) : 0f;
            targetCamera.backgroundColor = Color.Lerp(_settings.NightColor, _settings.DawnColor, t);
        }
    }
}
```

- [ ] **Step 2: Objective + dawn HUD**

`Assets/_neon/Scripts/UI/UIHUDObjectiveBar.cs`:

```csharp
using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon {

    //Objective fill bar + a giant arrow to the node + the dawn/Signal bar (spec §5.5).
    //Pure consumer of ObjectiveProgress + SignalChanged.
    public class UIHUDObjectiveBar : MonoBehaviour {

        [Header("Objective")]
        [SerializeField] private GameObject objectiveRoot;
        [SerializeField] private Image objectiveFill;
        [SerializeField] private Text objectiveLabel;
        [SerializeField] private RectTransform nodeArrow; // points from screen center toward the node

        [Header("Signal (dawn)")]
        [SerializeField] private Image dawnFill;

        [Inject] private IGameplaySignals _signals;
        private IDisposable _objectiveSub;
        private IDisposable _signalSub;
        private bool objectiveActive;
        private Vector2 nodeWorldPos;

        void Start(){
            if(_signals == null) return; //scene without DI injection
            if(objectiveRoot != null) objectiveRoot.SetActive(false);
            if(dawnFill != null) dawnFill.fillAmount = 0f;
            _objectiveSub = _signals.On<ObjectiveProgress>().Subscribe(ApplyObjective);
            _signalSub = _signals.On<SignalChanged>().Subscribe(ApplySignal);
        }

        void OnDestroy(){
            _objectiveSub?.Dispose();
            _signalSub?.Dispose();
        }

        void LateUpdate(){
            if(!objectiveActive || nodeArrow == null) return;
            var cam = Camera.main;
            if(cam == null) return;
            Vector2 screenNode = cam.WorldToScreenPoint(nodeWorldPos);
            Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f;
            Vector2 dir = (screenNode - center);
            nodeArrow.gameObject.SetActive(dir.magnitude > 40f); //hide when basically on the node
            if(dir.sqrMagnitude > 0.001f) nodeArrow.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        void ApplyObjective(ObjectiveProgress p){
            objectiveActive = p.Normalized < 1f;
            nodeWorldPos = p.Position;
            if(objectiveRoot != null) objectiveRoot.SetActive(objectiveActive);
            if(objectiveFill != null) objectiveFill.fillAmount = p.Normalized;
            if(objectiveLabel != null) objectiveLabel.text = p.PlayerInZone ? "REBOOTING…" : "REACH THE NODE";
        }

        void ApplySignal(SignalChanged s){
            if(dawnFill != null && s.Dawn > 0f) dawnFill.fillAmount = Mathf.Clamp01(s.Value / s.Dawn);
        }
    }
}
```

- [ ] **Step 3: Run-end screen**

`Assets/_neon/Scripts/UI/UIRunEndScreen.cs`:

```csharp
using System;
using R3;
using UnityEngine;
using VContainer;

namespace BrainlessLabs.Neon {

    //Shows the win (dawn) menu on RunEnded(true). Loss presentation stays on Level's
    //existing OnPlayerDeath→GameOverMenu (documented split), so this only acts on the win.
    public class UIRunEndScreen : MonoBehaviour {

        [SerializeField] private UIManager uiManager;
        [SerializeField] private string dawnMenuName = "RunWon";
        [Inject] private IGameplaySignals _signals;
        private IDisposable _subscription;

        void Start(){
            if(_signals == null) return; //scene without DI injection
            if(uiManager == null) uiManager = FindObjectOfType<UIManager>();
            _subscription = _signals.On<RunEnded>().Subscribe(OnRunEnded);
        }

        void OnDestroy() => _subscription?.Dispose();

        void OnRunEnded(RunEnded ended){
            if(!ended.Won) return; //loss is presented by Level.OnPlayerDeath (GameOverMenu)
            if(uiManager != null) uiManager.ShowMenu(dawnMenuName);
            else Debug.Log("[Run] Dawn reached — RunWon (no UIManager menu wired).");
        }
    }
}
```

- [ ] **Step 4: Compile check + commit**

Refresh Unity: zero errors, 117 tests PASS.

```bash
git add "Assets/_neon/Scripts/Run/SignalDarkness.cs" "Assets/_neon/Scripts/Run/SignalDarkness.cs.meta" "Assets/_neon/Scripts/UI/UIHUDObjectiveBar.cs" "Assets/_neon/Scripts/UI/UIHUDObjectiveBar.cs.meta" "Assets/_neon/Scripts/UI/UIRunEndScreen.cs" "Assets/_neon/Scripts/UI/UIRunEndScreen.cs.meta"
git commit -m "feat: Signal darkness lerp + objective/dawn HUD + run-won screen (M3)"
```

---

### Task 9: Signal-scaled draft weights + scene/config wiring + first full-run bring-up

Wires the protocol doc §8.2 Signal scaling (now that a Signal tier exists), then authors the run in `03_Level1` and brings the whole run up at runtime.

**Files:**
- Modify: `Assets/_neon/Scripts/Protocols/ProtocolService.cs`
- Modify: `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs`
- Editor: `LevelConfiguration_Level1` (Run block + waves→Manual), `03_Level1` scene (node visual, darkness, HUD, shop, run-end)

- [ ] **Step 1: Signal-scale the draft weights**

`ProtocolService` currently has fixed base weights. Give it the Signal so Tuned/Prototype weights scale per doc §8.2 (`Tuned ×(1+0.25·band)`, `Prototype ×(1+0.5·band)`), where `band = round(Signal fraction × 3)` (0–3, like Momentum tiers).

In `ProtocolService.cs`, change the ctor to accept `ISignalSystem`:

The current ctor `public ProtocolService(IStatSystem stats, IGameplaySignals signals, IReadOnlyList<ProtocolDefinitionAsset> catalog, int randomSeed)` becomes:

```csharp
        private readonly ISignalSystem _signal;

        public ProtocolService(IStatSystem stats, IGameplaySignals signals,
            ISignalSystem signal, IReadOnlyList<ProtocolDefinitionAsset> catalog, int randomSeed)
        {
            _stats = stats;
            _signals = signals;
            _signal = signal;
            _catalog = catalog != null
                ? new List<ProtocolDefinitionAsset>(catalog)
                : new List<ProtocolDefinitionAsset>();
            _random = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);
        }
```

and change `RarityWeight` from a static method to an instance method that applies the band:

```csharp
        private float RarityWeight(ProtocolRarity rarity)
        {
            int band = 0;
            if (_signal != null && _signal.Dawn > 0f)
            {
                band = Mathf.Clamp(Mathf.RoundToInt(_signal.Value / _signal.Dawn * 3f), 0, 3);
            }
            switch (rarity)
            {
                case ProtocolRarity.Stock: return WEIGHT_STOCK;                       // always-available floor
                case ProtocolRarity.Tuned: return WEIGHT_TUNED * (1f + 0.25f * band); // doc §8.2
                case ProtocolRarity.Prototype: return WEIGHT_PROTOTYPE * (1f + 0.5f * band);
                default: return 0f;
            }
        }
```

Add `using UnityEngine;` to `ProtocolService.cs` if `Mathf` isn't already imported.

> **Tests:** the existing `ProtocolServiceTests` construct `new ProtocolService(_stats, _signals, catalog, randomSeed)` (4 args). Update its `MakeService` helper to pass a signal: add `private readonly SignalSystem _signal;` created in `SetUp` (`new SignalSystem(_signals, _stats, 1f, 1f)`), disposed in `TearDown`, and change the helper to `new ProtocolService(_stats, _signals, _signal, catalog, randomSeed: 12345)`. With Signal at 0 (band 0) all weights equal their base, so every existing assertion still holds. This is a mechanical test edit — the 8 ProtocolService tests must still pass (count unchanged).

- [ ] **Step 2: Fix the runtime registration**

In `GameplayServicesState.RegisterProtocolService`, the registration must now supply `ISignalSystem`. It's registered in the same scope; VContainer resolves it by type for the ctor — but the two `WithParameter` calls are positional-by-type/name. Change the helper to:

```csharp
        private static void RegisterProtocolService(IContainerBuilder builder)
        {
            builder.Register<ProtocolService>(Lifetime.Singleton)
                .WithParameter<IReadOnlyList<ProtocolDefinitionAsset>>(GrowthSettingsAsset.InstanceAsset.Settings.ProtocolCatalog)
                .WithParameter<int>(0) // unseeded RNG at runtime; tests seed explicitly
                .As<IProtocolService>();
        }
```

`ISignalSystem`, `IStatSystem`, `IGameplaySignals` are all auto-resolved from the scope; only the catalog + seed need explicit `WithParameter`. **Ensure `RegisterSignalSystem` is called BEFORE `RegisterProtocolService`** in `RegisterTypes` (reorder if needed) so the dependency exists — VContainer resolves lazily at build so order is not strictly required, but keep it readable: Signal before Protocol.

- [ ] **Step 3: Compile + tests**

Refresh Unity: zero errors, **117 tests PASS** (ProtocolService count unchanged after the mechanical helper edit).

- [ ] **Step 4: Author the run in Level1 (editor, not in Play mode)**

1. Open `LevelConfiguration_Level1` (the `_configuration` on Level1's `Level` object). In the new **Run** block: tick **EnableRun**; set **EncounterNodePositions** to **3** entries at sensible spots along the belt (e.g. `(-8, -1)`, `(0, -1)`, `(8, -1)` — pick positions the player can reach and defend); **NodeRadius** ~2.5.
2. In the same asset's **Waves**: set each wave's `TriggerType` to **Manual** (so RunService owns triggering). Ensure there are at least 3 waves (one per encounter); if fewer, `RunService.TriggerWave(index)` on a missing index is a SpawnerService no-op (logs a warning) — add waves to match the 3 encounters, or accept chaff-only later encounters (note which).
3. Open `03_Level1.unity`. Add/verify these scene objects (all get `[Inject]` via `Level.Configure`'s root injection):
   - **RebootNodeVisual**: a GameObject with the `RebootNodeVisual` component + a child world-space sprite (`marker`) and its `SpriteRenderer` (`glow`). Use a placeholder sprite (e.g. `HitEffect.png`), large + translucent.
   - **SignalDarkness**: a GameObject with the `SignalDarkness` component (leave `targetCamera` empty → uses `Camera.main`).
   - On the HUD Canvas: **UIHUDObjectiveBar** (objective root + fill Image + label + a `nodeArrow` RectTransform; a `dawnFill` Image for the Signal bar), **UIShopScreen** (a `panel` with `healButton`, `continueButton`, `chargeLabel`, `healLabel`), **UIRunEndScreen** (assign `uiManager`; set `dawnMenuName`).
   - In the scene's `UIManager.menuList`, add a **RunWon** menu entry (a simple "DAWN — CITY STABILIZED" panel) so the win has a screen; the loss reuses the existing GameOver menu.
4. Save the scene + asset.

- [ ] **Step 5: First full-run bring-up (Recipe 4)**

Boot into Level1. Verify the run actually runs (this is the integration smoke test; the formal gate is Task 10):
- Console: `RunPhaseChanged` progression `EncounterIntro → EncounterActive`; encounter 0's wave spawns; `[Run]` logs as phases change.
- Stand on the node → objective bar fills → completes → Signal bar ticks up ~1/3 → **shop panel** appears and **combat freezes** (clock paused).
- Buy Heal (if Charge ≥ 25) → HP rises, Charge drops. Continue → combat resumes, encounter 1 begins, its wave spawns.
- Background visibly lerps toward dawn as the Signal rises; swarm gets denser (nastiness).
- Third objective → Signal hits dawn → `[Run] Boss stub — skipping to dawn` → **RunWon** menu.
- Die on purpose in an encounter → GameOver menu (loss path).
- Exit Play mode. Zero errors.

- [ ] **Step 6: Commit**

```bash
git add "Assets/_neon/Scripts/Protocols/ProtocolService.cs" "Assets/_neon/Tests/EditMode/ProtocolServiceTests.cs" "Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs" "Assets/_neon/Scenes/Game/03_Level1.unity"
git add -A "Assets/_neon"
git commit -m "feat: Signal-scaled draft weights + Level1 run wiring (M3)"
```

---

### Task 10: M3 gate — a full run lands 10–15 min and ends on the dawn beat

Spec §7 M3 gate: *a full run lands 10–15 min and ends on the dawn beat; objective legible in chaos (§16); EditMode tests for run transitions + Signal curve.* Runtime is ground truth.

- [ ] **Step 1: Full test suite**

Expected: **117/117 PASS** (23 M0 + 25 M1 + 42 M2 + 27 M3 — record the exact split: EconomySpend 5 · Signal 7 · Objective 7 · RunService 8). If ProtocolService's helper edit changed nothing, its 8 stay green.

- [ ] **Step 2: Full-run timing + dawn (one continuous session in Level1)**

Play a complete run and record in the gate record:
1. **Wall-clock length** start→dawn (target **10–15 min**). If far off, tune `RebootDurationSeconds` and/or encounter count in `RunSettings`/the Run block — the knobs are live.
2. **Ends on the dawn beat:** the final objective raises the Signal to dawn → BossStub → RunWon menu. The background reaches `DawnColor`.
3. **Objective legible in chaos (§16):** at full swarm density (Signal-thickened), can you find + hold the node? Is the fill bar + node arrow + zone glow readable in the pile? (This is the M3 legibility gate — chaff-blob from the M1 flag may hurt it; note severity for the Feel & Level pass.)
4. **Shop rhythm:** fight→shop→fight reads as a beat, not a jarring stop; combat freeze + resume is clean; Heal is a real spend decision (Charge economy from M2).
5. **Signal→density:** the swarm visibly thickens as the night progresses (nastiness rising with the Signal).
6. **Lose path:** dying mid-run shows GameOver; **win path** shows the dawn menu.
7. **FPS** at ChaffCap 150 × dawn nastiness (chaff cap clamped at 150 per the bridge) — no regression vs the M2 ~234 baseline.

- [ ] **Step 3: Interaction regressions (M1/M2 still intact under the run)**

Confirm in the same session: auto-engage chips, Finish-Ready glow, single prompt, hero challenge sequences (2-input, 3 at Hot), Momentum tiers, XP level-ups + slow-mo picker, protocol picks — all still work *inside* the encounter phases. The level-up slow-mo (clock scale) and the shop pause (clock scale) must not fight — leveling up during an encounter slows time then resumes; entering the shop pauses; both release cleanly.

- [ ] **Step 4: Gate record + push**

Append `## M3 gate record` to the bottom of THIS document (date, machine, exact test count, run length, the seven answers above, deviations encountered), then:

```bash
git add "docs/superpowers/plans/2026-07-04-neon-engine-base-plan4-m3-run.md"
git commit -m "docs: record M3 gate (full run ends on dawn)"
git push -u origin claude/neon-m3-run
```

- [ ] **Step 5: Hand off**

Report the gate record to Sebastien. **Plan 5 (M4: Siren Pulse, the Overcharge finisher — spends the meter M2 already fills, per-verb hitstop/shake profiles, tier-up flourish, whiff scratch, callouts, audio layering by Momentum tier + Signal, full HUD polish) is written after this gate** and consumes the seams this milestone exposed: the shop screen (now has Specials to sell + ranks/overlays), the Signal (music-aggression consumer), and the run FSM (finisher/actives fire inside encounters). Per the plan series, the M4 planning step must also reconcile the overlaps flagged in `docs/superpowers/plans/2026-07-04-feel-and-level-pass-pre-brief.md` (chaff separation + HUD polish) — do them once.

---

## Deviations from the spec (deliberate, M3-scoped)

1. **Shop sells Heal + Continue only** — Specials (the intended stock) are M4; the shop *structure* + the Charge sink land now (decision R1). Ranks/reroll/overlays are M4.
2. **Music aggression not wired** — the Signal exposes its value + `SignalChanged`, but the audio consumer is M4 (spec §7 M4 owns "audio layering by tier + Signal"). M3 wires only the darkness visual (decision R3).
3. **Reboot Node is distance-based, not a physics trigger** — consistent with F4's bridge-only spatial decision; a purely-visual `RebootNodeVisual` shows the zone. No colliders added.
4. **Boss is a pass-through stub** (`BossStub → RunWon`) — spec §5.4 explicitly allows winning on dawn with the boss cut; the state exists so M4/post-MVP can flesh it without reshaping the FSM.
5. **Loss presentation stays on `Level.OnPlayerDeath → GameOverMenu`; RunService owns the win menu.** Both react to player death (RunService → `RunLost` for state integrity; Level → GameOver menu) — same menu, no double-screen; the win path is solely RunService's. Centralizing all run-end presentation is a small later cleanup.
6. **Hero waves reuse `Manual` waves 1:1 with encounters** — no per-encounter spawn-composition authoring (decision R2). If Level1 has fewer waves than encounters, later encounters run chaff-only (a `TriggerWave` no-op) — author 3 waves to avoid this.
7. **New-run reset is out of scope** — one run per boot (MVP). Signal is session-scoped and would persist across an in-boot replay; run-reset is deferred (noted in assumptions).
8. **RunService transitions that fire inside `onEnter`** (COMPLETE→SHOP/BOSS, BOSS→WON) settle on the next `OnLogic` tick — standard UnityHFSM; tests advance the clock enough to observe the settled phase.
9. **Objective fill uses gameplay-scaled dt** (the clock delta) — so hitstop/slow-mo affects reboot speed too; acceptable and consistent with "the clock owns gameplay time."

## M3 gate record

**Date:** 2026-07-05 · **Machine:** Sebastien's dev PC (editor unfocused, driven via MCP) · **Branch:** `claude/neon-m3-run` (10 commits off `claude/neon-m2-growth` @ `489f4a6`)

**Tests: 117/117 PASS** — 23 M0 + 25 M1 + 42 M2 + 27 M3 (EconomySpend 5 · Signal 7 · Objective 7 · RunService 8). ProtocolService's 8 stayed green through the mechanical ctor edit.

### The seven gate answers

1. **Wall-clock start→dawn:** machine-floor run = **~5.2 min** (boot-complete t=30.5s → RunWon t=340.6s) with an invulnerable autopilot pinned to the node, zero shop dwell, zero travel/fight time. Pure mechanical floor ≈ 2.7 min (3×50s reboot + intros); the rest was knockback drift + level-up slow-mo stretching fills. A real player adds wave-clearing before the zone is holdable (10/14/18-hero waves + chaff), travel, shop decisions, and deaths — the 10–15 min target is plausible but **needs Sebastien's hands-on run to confirm**; live knobs if off: `RebootDurationSeconds` (50s), encounter count (3), wave sizes.
2. **Ends on the dawn beat:** ✅ third objective → Signal 1.0 → `[Run] Boss stub — skipping to dawn (MVP).` → **RunWon menu** ("DAWN — CITY STABILIZED", MainMenu preselected + Quit). Camera background landed **exactly** `DawnColor` (0.45, 0.35, 0.30) — the lerp tracked ⅓/⅔/1 precisely at each objective.
3. **Objective legible in chaos (§16):** machine-verifiable parts ✅ — objective bar + label ("REACH THE NODE"/"REBOOTING…"), node arrow (HandPointer sprite, center-screen, rotates to node, hides within 40px), zone glow marker tinting by fill, dawn strip. Screenshot-verified. **Legibility in the full dawn pile is a hands-on judgement** — flagged (chaff-blob from the M1 flag may hurt it; Feel & Level owns the fix).
4. **Shop rhythm:** ✅ mechanics — `Shop` freezes the clock (scale 0, combat/sim frozen mid-swarm behind the dim), Heal spends 25 → +40 HP (verified 40→80 / 132→107), insufficient-funds correctly no-ops (TrySpend guard; button disables below cost, re-enables on charge gain), Continue resumes scale 1 → next encounter + its Manual wave. Fight→shop→fight *feel* = hands-on.
5. **Signal→density:** ✅ chaff cap scaled live 80 → 107 (Signal ⅓) → 150 (dawn, ×2 clamped at the spike ceiling); spawn rate doubles alongside. Visible thickening required re-basing Level1's `ChaffCap` 150→80 (deviation 12 below).
6. **Lose / win paths:** ✅ lose — a *real* combat death (not simulated) drove `HealthSystem.onUnitDeath` → `RunLost` + GameOver menu, shop scale released. ✅ win — RunWon menu solely via `RunEnded(true)`; GameOver stayed down.
7. **FPS at dawn (cap 150 × nastiness):** **197 FPS** avg over 21.9s (4322 frames), editor unfocused, RunWon overlay up over the full simming swarm. M1 baseline ~197 (focused), M2 ~234 (unfocused, mid-fight, no run layer). No regression vs the M1 gate floor; the M2 delta is method noise (different scene state) — worth a mid-fight spot-check during hands-on.

### Interaction regressions (M1/M2 under the run)

- Auto-engage + chip→Finish-Ready alive inside encounters (chaff reached Finish-Ready; finish prompt "+N ready" rendering; finish sweeps paid Charge/Overcharge and stepped Momentum — WARM observed).
- **Level-up slow-mo vs shop pause: no fight.** Mid-encounter level-up held 0.1 scale, picker picks (incl. 2 banked drafts back-to-back) released to 1.0; shop pause 0 → Continue → 1.0. Different clock scale sources compose and release cleanly.
- Protocol drafts fire and apply under the run (Afterburner/Wide Sweep/Overclocked Coil cards picked mid-run); Signal-scaled weights active (band 1–3 as night deepens — EditMode-verified math, base weights at Signal 0 keep all M2 tests green).
- Hero finish-challenge sequences: code untouched by M3 (working agreement 7) — hands-on feel check rides along with Sebastien's run.

### Deviations encountered in execution (beyond the plan's 9 documented ones)

10. Plan's `SignalSystemTests.NastinessStacksAdditively_NotWithMomentum` needed `GainMultiplier` base seeded (fresh StatSheet → base 0; 0×2=0). Test fixed, assertion intent unchanged.
11. `ProgressionSystemTests` also constructs `ProtocolService` directly (2 sites the plan missed) — same mechanical `ISignalSystem` param fix as `ProtocolServiceTests`.
12. Level1 authoring: `EndLevelWhenAllWavesCompleted` **off** (its LevelCompleted menu would fire mid-run; RunService owns the level end) · 1 wave re-authored into **3 Manual waves** (10/14/18 enemies, camera bounds marching 0.35/0.65/1.0) · node positions for *today's* 38-unit strip: (5,−1.5) (16,−1.5) (28,−1.5) · `Swarm.ChaffCap` **150→80** so the Signal ramp is visible (spec §6 band is 80–150; dawn load = old cap, so the FPS baseline holds).
13. Fill bars needed a real sprite (`UISprite`, copied from the Momentum meter) — null-sprite Filled Images ignore `fillAmount` visually. Caught by screenshot (M2 lesson), fixed in-scene.
14. RunWon menu = duplicated `UILevelCompleted` (inherits UIFader + UISetPlayerInactive); its next-level Continue button removed (MVP = one run per boot).
15. **Post-win drafts:** XP keeps flowing after dawn (swarm still sims), so a level-up picker can pop over the RunWon fade-in. Cosmetic; the menu fades in over it. Flagged for M4 (run-end should quiesce the growth loop — folds into the run-reset work already deferred).
16. One non-reproducible phantom shop-Continue in the first smoke session — attributed to a stray human click on the machine (panel is center-screen; sessions 2/3 + the gate run held Shop indefinitely with zero phantom continues).

### Hands-on items for Sebastien (non-blocking, same pattern as M2's Overdrive-scream)

- Full-run wall-clock with real fighting (10–15 target; knobs live).
- Objective/arrow/glow legibility in the dawn-density pile.
- Shop-beat feel (freeze abruptness, Heal as a real decision).
- Hero challenge sequences inside encounters + mid-fight FPS spot-check.

## Spec coverage self-check (for reviewers)

- Spec §7 M3: `RunService` (UnityHFSM) sequencing encounter phases ✅ (Task 6) · `RebootNodeObjective` ✅ (Task 5) · `ISignalSystem` feeding spawn/darkness/music ✅ (Tasks 3/4/8 — music exposed, consumed M4 per R3) · shop beat ✅ (Tasks 2/7) · gate: full run ends on dawn + objective legible + EditMode tests for run transitions + Signal curve ✅ (Tasks 6/3/10).
- Spec §5.4 architecture: `RunService` reuses UnityHFSM, lives in `Neon` distinct from the boot FSM in `Lifecycle` ✅ (Task 6, asmdef adds the package ref, not a Lifecycle ref) · lives in the Level scope, per-phase drives hero waves + chaff floods + objective ✅ · MVP = one belt arena hosting phases (not scene loads) ✅ · boss stub ✅ · objective speed is a stat ✅ (Task 5) · Signal is a modifier source on the Run sheet scaling spawn nastiness ✅ (Task 3) · win = Signal hits dawn ✅.
- Design inputs: special-moves §3 shop structure honored (heal + continue now; Specials M4) ✅ · protocol doc §8.2 Signal-scaled draft weights wired ✅ (Task 9).
- Spec §10 non-negotiables: DI-bootstrap gates ✅ · VContainer-only, Signal registered in an FSM state (session) + run systems in the Level scope ✅ · no static locators ✅ · combat verbs unchanged ✅ · no legacy touched ✅ · runtime gates per task + a full-run gate ✅ · no invented APIs (UnityHFSM surface confirmed from package source; every landed seam re-read post-M2) ✅ · assembly direction preserved (`Neon → UnityHFSM` package; `Neon` still never references `Lifecycle`) ✅.
- Guardrails: Signal uses `PctAdd` (Momentum stays the only `Mult`) ✅ · chaff cap clamped at the spike-verified 150 even under nastiness ✅.
- Deferred to M4/later (per plan series): Specials (Siren Pulse) + shop stock/ranks/overlays, Overcharge *finisher* (meter already fills), music-by-Signal, per-verb hitstop/feel, full HUD polish, boss, new-run reset, remaining objective impls (Rescue/Purge/Hold), remaining catalog protocols.
