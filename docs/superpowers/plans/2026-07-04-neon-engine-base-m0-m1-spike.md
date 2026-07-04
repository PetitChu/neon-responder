# Neon Engine Base — M0 Spine + M1 Render Spike Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the Layer-0 engine spine (`IStatSystem`, `IGameplaySignals`, `IGameplayClock` + `GameplayServicesState`) test-first, then run the M1 2D-ECS render spike (150 hot proxy sprites + 100 instanced ambient) that gates the rest of Milestone 1.

**Architecture:** Three decoupled pure-C# spine services registered in a new `GameplayServicesState` FSM state (inserted between `GameServicesState` and `GameState`), plus a new empty `BrainlessLabs.Neon.Simulation` DOTS assembly. The spike is a self-contained throwaway assembly + scene that answers the highest-risk unknown (2D sprite rendering of ECS agents) before anything is built on it.

**Tech Stack:** Unity 6000.3.5f2, C#, VContainer 1.17 (DI), R3 (event bus), UnityHFSM (app FSM), Unity Entities 1.4.4 / Burst 1.8.27 / Collections 2.6.4 / Mathematics 1.3.3, com.unity.test-framework 1.6.0 (EditMode NUnit tests), URP 17.3.0 (2D).

**Spec:** `docs/superpowers/specs/2026-07-04-neon-engine-base-design.md` (M0 + M1-spike portion of §7)

**Branch:** `claude/neon-engine-base` (already checked out)

---

## Plan series & scope

The spec's §7 milestones are the plan spine: **M0 → M1 → M2 → M3 → M4**. This document is **Plan 1 of the series** and covers:

- **All of M0** — spine services, `GameplayServicesState`, `Simulation` asmdef, EditMode tests, boot gate.
- **The M1 render spike only** — the spec (§5.2, §9) calls the 2D-ECS render path the highest-risk unknown and mandates it be done *in isolation before anything is built on it*. Its verdict (pass / fallback-to-pooled-MonoBehaviour) determines the internals of everything else in M1.

Follow-up plans (written after each gate, against the then-real code):

- **Plan 2 — M1 remainder:** DOTS swarm + `SwarmBridge`, `AutoEngageSystem`, `FinishReadySystem` + selector, `Momentum`, `FinishResolver` combat seam (confirmed against real hit-path code), AI_Active spawn-gap fix, minimal HUD. Gated on the spike verdict from this plan.
- **Plan 3 — M2 (growth):** economy, Protocols, progression, tiered finish challenges.
- **Plan 4 — M3 (run/objective/Signal).** **Plan 5 — M4 (actives/feel).**

Do NOT start Plan-2 work inside this plan. This plan is done when the M0 gate passes and the spike verdict document is committed.

---

## Working agreements (read once, apply to every task)

1. **Unity must import new files before tests/play.** After creating or editing `.cs`/`.asmdef` files from outside the editor, focus the Unity editor (or run `mcp__unityMCP__refresh_unity`) and wait for compilation. Then check the console (`mcp__unityMCP__read_console` filtered to errors, or the Console window) — **zero errors** before proceeding. Every "Run tests" or "Play" step implies this.
2. **Running EditMode tests:** Unity → Window → General → Test Runner → EditMode tab → Run All. Agentic workers: use `mcp__unityMCP__run_tests` with `mode: "EditMode"`. "Expected: FAIL/PASS" below refers to the named test methods.
3. **Commits include `.meta` files.** Unity generates a `.meta` beside every new file/folder — `git add` the meta with its file. Use `git status` to catch strays.
4. **Never run git/asset-writing commands while the editor is in Play mode** (editor bootstrap flakiness — `neon-troubleshooting`).
5. **Commit message trailer:** end every commit body with `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
6. **Do not touch:** `WaveManager` (obsolete), scenes `04_Level2`/`05_Level3`, `ApplicationLifetimeScope` registrations, anything under `Assets/Addons/`.
7. **DI rules (spec §10):** no static/`Instance` accessors, no `new FooService()` outside registration, services registered in FSM states only. Namespaces are flat per assembly (`BrainlessLabs.Neon`, `.Lifecycle`, `.Simulation`) — folder depth does not add sub-namespaces.

---

## File structure

**M0 — created:**

| Path | Responsibility |
|---|---|
| `Assets/_neon/Tests/EditMode/BrainlessLabs.Neon.Tests.EditMode.asmdef` | EditMode test assembly (first in the project for `_neon`) |
| `Assets/_neon/Tests/EditMode/TestAssemblySmokeTests.cs` | Proves the test assembly compiles + references `BrainlessLabs.Neon` |
| `Assets/_neon/Scripts/Stats/StatOp.cs` | `Add / PctAdd / Mult` fold operations |
| `Assets/_neon/Scripts/Stats/ModifierSource.cs` | Opaque handle identifying who added a modifier (also reused by the clock's scale sources) |
| `Assets/_neon/Scripts/Stats/StatModifier.cs` | One modifier: op + value + source |
| `Assets/_neon/Scripts/Stats/StatId.cs` | Enum key for every "gets stronger / gets harder" number |
| `Assets/_neon/Scripts/Stats/Stat.cs` | One stat: base + modifier list, folded on query (internal) |
| `Assets/_neon/Scripts/Stats/StatSheet.cs` | Keyed collection of Stats; `AddModifier`/`RemoveBySource` |
| `Assets/_neon/Scripts/Stats/IStatSystem.cs` | Service interface: `Player` + `Run` sheets |
| `Assets/_neon/Scripts/Stats/StatSystem.cs` | Service implementation |
| `Assets/_neon/Tests/EditMode/StatSheetTests.cs` | Fold order, stacking, RemoveBySource, refold |
| `Assets/_neon/Tests/EditMode/StatSystemTests.cs` | Sheet independence |
| `Assets/_neon/Scripts/Signals/IGameplaySignals.cs` | Typed event bus interface |
| `Assets/_neon/Scripts/Signals/GameplaySignals.cs` | R3 `Subject<T>`-per-type implementation |
| `Assets/_neon/Tests/EditMode/GameplaySignalsTests.cs` | Publish/subscribe, isolation, unsubscribe |
| `Assets/_neon/Scripts/Clock/IGameplayTickable.cs` | Ordered-tick participant interface |
| `Assets/_neon/Scripts/Clock/IGameplayClock.cs` | Gameplay time + ordered tick + scale sources |
| `Assets/_neon/Scripts/Clock/GameplayClock.cs` | Implementation; VContainer `ITickable` entry point |
| `Assets/_neon/Tests/EditMode/GameplayClockTests.cs` | Scaled time, multiplied scale sources, tick order |
| `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs` | New FSM state registering the spine |
| `Assets/_neon/Scripts/Simulation/BrainlessLabs.Neon.Simulation.asmdef` | DOTS leaf assembly (references Unity.Entities/Burst/Collections/Mathematics only) |
| `Assets/_neon/Scripts/Simulation/SwarmAgent.cs` | First real sim component (`SwarmTier` Chaff/Ambient) |

**M0 — modified:**

| Path | Change |
|---|---|
| `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef` | Add references: `R3.Unity`, `BrainlessLabs.Neon.Simulation` |
| `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameServicesState.cs` | Retarget `NextStateType` to `GameplayServicesState` |

**M1 spike — created (throwaway, self-contained):**

| Path | Responsibility |
|---|---|
| `Assets/_neon/Spikes/SwarmRenderSpike/BrainlessLabs.Neon.SwarmRenderSpike.asmdef` | Spike assembly — references DOTS packages only, nothing references it |
| `Assets/_neon/Spikes/SwarmRenderSpike/SpikeComponents.cs` | `SpikePosition`, `SpikeVelocity`, `HotAgentTag`, `AmbientAgentTag`, `SpikeBounds` |
| `Assets/_neon/Spikes/SwarmRenderSpike/SpikeMoveSystem.cs` | Burst `ISystem` bounce-wander movement |
| `Assets/_neon/Spikes/SwarmRenderSpike/SpikeBootstrap.cs` | Spawns 150 hot + 100 ambient entities |
| `Assets/_neon/Spikes/SwarmRenderSpike/HotProxyPool.cs` | 150 pooled `SpriteRenderer` proxies synced from ECS |
| `Assets/_neon/Spikes/SwarmRenderSpike/AmbientInstancedRenderer.cs` | `Graphics.RenderMeshInstanced` quad rendering |
| `Assets/_neon/Spikes/SwarmRenderSpike/SpikeFpsHud.cs` | On-screen smoothed FPS |
| `Assets/_neon/Spikes/SwarmRenderSpike/SpikeAmbient.mat` | URP Unlit material, GPU instancing enabled |
| `Assets/_neon/Scenes/Spikes/99_SwarmRenderSpike.unity` | Spike scene (camera + spike rig, no `Level`) |
| `Assets/_neon/Scenes/Configs/SceneDefinition_SwarmRenderSpike.asset` | Boots the spike through the DI flow (Recipe 4) |
| `docs/superpowers/plans/2026-07-04-swarm-render-spike-verdict.md` | Measured verdict + decision for Plan 2 |

---

## Milestone M0 — Spine (headless, test-first)

### Task 1: EditMode test assembly

**Files:**
- Create: `Assets/_neon/Tests/EditMode/BrainlessLabs.Neon.Tests.EditMode.asmdef`
- Create: `Assets/_neon/Tests/EditMode/TestAssemblySmokeTests.cs`

- [ ] **Step 1: Create the test asmdef**

```json
{
    "name": "BrainlessLabs.Neon.Tests.EditMode",
    "rootNamespace": "BrainlessLabs.Neon.Tests",
    "references": [
        "BrainlessLabs.Neon",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Create the smoke test**

`Assets/_neon/Tests/EditMode/TestAssemblySmokeTests.cs`:

```csharp
using NUnit.Framework;

namespace BrainlessLabs.Neon.Tests
{
    public class TestAssemblySmokeTests
    {
        [Test]
        public void TestAssembly_References_NeonRuntime()
        {
            // SceneType lives in BrainlessLabs.Neon — proves the asmdef reference resolves.
            Assert.IsNotNull(typeof(SceneType));
        }
    }
}
```

- [ ] **Step 3: Refresh Unity, confirm compile, run the test**

Refresh Unity (working agreement 1). Run EditMode tests (working agreement 2).
Expected: `TestAssembly_References_NeonRuntime` PASS, 0 console errors.

- [ ] **Step 4: Commit**

```bash
git add "Assets/_neon/Tests" "Assets/_neon/Tests.meta"
git commit -m "test: add BrainlessLabs.Neon.Tests.EditMode assembly with smoke test"
```

---

### Task 2: Stat foundation — `StatSheet` (test-first)

The load-bearing piece (spec §4.1): every "gets stronger / gets harder" number is a `Stat` = `baseValue` + modifier list, folded on query as `(base + ΣAdd) × (1 + ΣPctAdd) × ΠMult`. Momentum, Protocols, and the Signal will all be plain modifier sources here.

**Files:**
- Create: `Assets/_neon/Scripts/Stats/StatOp.cs`
- Create: `Assets/_neon/Scripts/Stats/ModifierSource.cs`
- Create: `Assets/_neon/Scripts/Stats/StatModifier.cs`
- Create: `Assets/_neon/Scripts/Stats/StatId.cs`
- Create: `Assets/_neon/Scripts/Stats/Stat.cs`
- Create: `Assets/_neon/Scripts/Stats/StatSheet.cs`
- Test: `Assets/_neon/Tests/EditMode/StatSheetTests.cs`

- [ ] **Step 1: Create the value types (final implementations — trivial, and the tests need them to compile)**

`Assets/_neon/Scripts/Stats/StatOp.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>
    /// How a modifier folds into a stat: (base + ΣAdd) × (1 + ΣPctAdd) × ΠMult.
    /// </summary>
    public enum StatOp
    {
        Add,
        PctAdd,
        Mult
    }
}
```

`Assets/_neon/Scripts/Stats/ModifierSource.cs`:

```csharp
using System;
using System.Threading;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Opaque handle identifying who added a modifier (a Protocol, Momentum, the Signal, ...).
    /// Also used by IGameplayClock as a time-scale source handle.
    /// </summary>
    public readonly struct ModifierSource : IEquatable<ModifierSource>
    {
        private static int s_nextId;

        public readonly int Id;
        public readonly string DebugName;

        private ModifierSource(int id, string debugName)
        {
            Id = id;
            DebugName = debugName;
        }

        public static ModifierSource Create(string debugName)
        {
            return new ModifierSource(Interlocked.Increment(ref s_nextId), debugName);
        }

        public bool Equals(ModifierSource other) => Id == other.Id;
        public override bool Equals(object obj) => obj is ModifierSource other && Equals(other);
        public override int GetHashCode() => Id;
        public override string ToString() => $"{DebugName}#{Id}";
    }
}
```

`Assets/_neon/Scripts/Stats/StatModifier.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>
    /// One modifier on a stat: operation, value, and the source that owns it.
    /// </summary>
    public readonly struct StatModifier
    {
        public readonly StatOp Op;
        public readonly float Value;
        public readonly ModifierSource Source;

        public StatModifier(StatOp op, float value, ModifierSource source)
        {
            Op = op;
            Value = value;
            Source = source;
        }
    }
}
```

`Assets/_neon/Scripts/Stats/StatId.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Keys for every tunable gameplay number. Extend as systems land
    /// (append only — do not renumber, values may be serialized by Protocol assets later).
    /// </summary>
    public enum StatId
    {
        // Auto-engage (bases set by AutoEngageSystem in M1)
        AutoEngageRate = 0,
        AutoEngageDamage = 1,
        AutoEngageArcDegrees = 2,

        // Cross-cutting multipliers (Momentum registers modifiers here)
        DamageMultiplier = 100,
        GainMultiplier = 101,
    }
}
```

- [ ] **Step 2: Create failing skeletons for `Stat` and `StatSheet`**

`Assets/_neon/Scripts/Stats/Stat.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    internal sealed class Stat
    {
        public float BaseValue
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }

        public float Value => throw new System.NotImplementedException();

        public void Add(StatModifier modifier) => throw new System.NotImplementedException();

        public int RemoveBySource(ModifierSource source) => throw new System.NotImplementedException();
    }
}
```

`Assets/_neon/Scripts/Stats/StatSheet.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>
    /// A keyed collection of stats. Systems query folded values; modifier owners
    /// add/remove modifiers by source handle. Pure C#, no Unity dependencies.
    /// </summary>
    public sealed class StatSheet
    {
        public float GetValue(StatId id) => throw new System.NotImplementedException();

        public float GetBase(StatId id) => throw new System.NotImplementedException();

        public void SetBase(StatId id, float value) => throw new System.NotImplementedException();

        public void AddModifier(StatId id, StatOp op, float value, ModifierSource source) => throw new System.NotImplementedException();

        public int RemoveBySource(ModifierSource source) => throw new System.NotImplementedException();
    }
}
```

- [ ] **Step 3: Write the failing tests**

`Assets/_neon/Tests/EditMode/StatSheetTests.cs`:

```csharp
using NUnit.Framework;

namespace BrainlessLabs.Neon.Tests
{
    public class StatSheetTests
    {
        [Test]
        public void GetValue_UnsetStat_ReturnsZero()
        {
            var sheet = new StatSheet();

            Assert.AreEqual(0f, sheet.GetValue(StatId.AutoEngageDamage));
        }

        [Test]
        public void GetValue_BaseOnly_ReturnsBase()
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatId.AutoEngageDamage, 8f);

            Assert.AreEqual(8f, sheet.GetValue(StatId.AutoEngageDamage));
        }

        [Test]
        public void Fold_Order_Is_BasePlusAdd_TimesPctAdd_TimesMult()
        {
            var sheet = new StatSheet();
            var src = ModifierSource.Create("test");
            sheet.SetBase(StatId.AutoEngageDamage, 10f);
            sheet.AddModifier(StatId.AutoEngageDamage, StatOp.Add, 5f, src);      // (10 + 5)
            sheet.AddModifier(StatId.AutoEngageDamage, StatOp.PctAdd, 0.2f, src); // × 1.2 = 18
            sheet.AddModifier(StatId.AutoEngageDamage, StatOp.Mult, 2f, src);     // × 2   = 36

            Assert.AreEqual(36f, sheet.GetValue(StatId.AutoEngageDamage), 0.0001f);
        }

        [Test]
        public void PctAdd_StacksAdditively()
        {
            var sheet = new StatSheet();
            var src = ModifierSource.Create("test");
            sheet.SetBase(StatId.GainMultiplier, 10f);
            sheet.AddModifier(StatId.GainMultiplier, StatOp.PctAdd, 0.1f, src);
            sheet.AddModifier(StatId.GainMultiplier, StatOp.PctAdd, 0.1f, src);

            // 10 × (1 + 0.1 + 0.1) = 12, NOT 10 × 1.1 × 1.1 = 12.1
            Assert.AreEqual(12f, sheet.GetValue(StatId.GainMultiplier), 0.0001f);
        }

        [Test]
        public void Mult_StacksMultiplicatively()
        {
            var sheet = new StatSheet();
            var src = ModifierSource.Create("test");
            sheet.SetBase(StatId.DamageMultiplier, 10f);
            sheet.AddModifier(StatId.DamageMultiplier, StatOp.Mult, 2f, src);
            sheet.AddModifier(StatId.DamageMultiplier, StatOp.Mult, 2f, src);

            Assert.AreEqual(40f, sheet.GetValue(StatId.DamageMultiplier), 0.0001f);
        }

        [Test]
        public void RemoveBySource_RemovesOnlyThatSource_AcrossAllStats()
        {
            var sheet = new StatSheet();
            var momentum = ModifierSource.Create("momentum");
            var protocol = ModifierSource.Create("protocol");
            sheet.SetBase(StatId.DamageMultiplier, 10f);
            sheet.SetBase(StatId.GainMultiplier, 10f);
            sheet.AddModifier(StatId.DamageMultiplier, StatOp.Add, 5f, momentum);
            sheet.AddModifier(StatId.GainMultiplier, StatOp.Add, 5f, momentum);
            sheet.AddModifier(StatId.DamageMultiplier, StatOp.Add, 3f, protocol);

            int removed = sheet.RemoveBySource(momentum);

            Assert.AreEqual(2, removed);
            Assert.AreEqual(13f, sheet.GetValue(StatId.DamageMultiplier), 0.0001f); // protocol's +3 survives
            Assert.AreEqual(10f, sheet.GetValue(StatId.GainMultiplier), 0.0001f);
        }

        [Test]
        public void SetBase_AfterModifiers_Refolds()
        {
            var sheet = new StatSheet();
            var src = ModifierSource.Create("test");
            sheet.SetBase(StatId.AutoEngageRate, 1f);
            sheet.AddModifier(StatId.AutoEngageRate, StatOp.Mult, 2f, src);
            Assert.AreEqual(2f, sheet.GetValue(StatId.AutoEngageRate), 0.0001f);

            sheet.SetBase(StatId.AutoEngageRate, 3f);

            Assert.AreEqual(6f, sheet.GetValue(StatId.AutoEngageRate), 0.0001f);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they fail**

Refresh Unity, run EditMode tests.
Expected: all 7 `StatSheetTests` FAIL with `NotImplementedException`. `TestAssembly_References_NeonRuntime` still PASSES.

- [ ] **Step 5: Implement `Stat` and `StatSheet`**

Replace `Assets/_neon/Scripts/Stats/Stat.cs` entirely with:

```csharp
using System.Collections.Generic;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// One stat: a base value plus a list of modifiers, folded lazily on query.
    /// Fold order: (base + ΣAdd) × (1 + ΣPctAdd) × ΠMult.
    /// </summary>
    internal sealed class Stat
    {
        private readonly List<StatModifier> _modifiers = new();
        private float _baseValue;
        private float _foldedValue;
        private bool _dirty = true;

        public float BaseValue
        {
            get => _baseValue;
            set
            {
                _baseValue = value;
                _dirty = true;
            }
        }

        public float Value
        {
            get
            {
                if (_dirty)
                {
                    Fold();
                }
                return _foldedValue;
            }
        }

        public void Add(StatModifier modifier)
        {
            _modifiers.Add(modifier);
            _dirty = true;
        }

        public int RemoveBySource(ModifierSource source)
        {
            int removed = _modifiers.RemoveAll(m => m.Source.Equals(source));
            if (removed > 0)
            {
                _dirty = true;
            }
            return removed;
        }

        private void Fold()
        {
            float add = 0f;
            float pctAdd = 0f;
            float mult = 1f;

            foreach (var modifier in _modifiers)
            {
                switch (modifier.Op)
                {
                    case StatOp.Add:
                        add += modifier.Value;
                        break;
                    case StatOp.PctAdd:
                        pctAdd += modifier.Value;
                        break;
                    case StatOp.Mult:
                        mult *= modifier.Value;
                        break;
                }
            }

            _foldedValue = (_baseValue + add) * (1f + pctAdd) * mult;
            _dirty = false;
        }
    }
}
```

Replace `Assets/_neon/Scripts/Stats/StatSheet.cs` entirely with:

```csharp
using System.Collections.Generic;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// A keyed collection of stats. Systems query folded values; modifier owners
    /// add/remove modifiers by source handle. Pure C#, no Unity dependencies.
    /// Unset stats read as 0 (base 0, no modifiers).
    /// </summary>
    public sealed class StatSheet
    {
        private readonly Dictionary<StatId, Stat> _stats = new();

        public float GetValue(StatId id)
        {
            return _stats.TryGetValue(id, out var stat) ? stat.Value : 0f;
        }

        public float GetBase(StatId id)
        {
            return _stats.TryGetValue(id, out var stat) ? stat.BaseValue : 0f;
        }

        public void SetBase(StatId id, float value)
        {
            GetOrCreate(id).BaseValue = value;
        }

        public void AddModifier(StatId id, StatOp op, float value, ModifierSource source)
        {
            GetOrCreate(id).Add(new StatModifier(op, value, source));
        }

        /// <summary>
        /// Removes every modifier owned by <paramref name="source"/> across all stats.
        /// Returns the number of modifiers removed.
        /// </summary>
        public int RemoveBySource(ModifierSource source)
        {
            int total = 0;
            foreach (var stat in _stats.Values)
            {
                total += stat.RemoveBySource(source);
            }
            return total;
        }

        private Stat GetOrCreate(StatId id)
        {
            if (!_stats.TryGetValue(id, out var stat))
            {
                stat = new Stat();
                _stats[id] = stat;
            }
            return stat;
        }
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Refresh Unity, run EditMode tests.
Expected: all 8 tests PASS (7 StatSheetTests + smoke test).

- [ ] **Step 7: Commit**

```bash
git add "Assets/_neon/Scripts/Stats" "Assets/_neon/Scripts/Stats.meta" "Assets/_neon/Tests/EditMode/StatSheetTests.cs" "Assets/_neon/Tests/EditMode/StatSheetTests.cs.meta"
git commit -m "feat: add StatSheet spine - folded stats with source-handle modifiers (M0)"
```

---

### Task 3: `IStatSystem` service (test-first)

Two sheets to start (spec §4.1): **player** and **run/global**.

**Files:**
- Create: `Assets/_neon/Scripts/Stats/IStatSystem.cs`
- Create: `Assets/_neon/Scripts/Stats/StatSystem.cs`
- Test: `Assets/_neon/Tests/EditMode/StatSystemTests.cs`

- [ ] **Step 1: Write the failing test**

`Assets/_neon/Tests/EditMode/StatSystemTests.cs`:

```csharp
using NUnit.Framework;

namespace BrainlessLabs.Neon.Tests
{
    public class StatSystemTests
    {
        [Test]
        public void PlayerAndRunSheets_AreIndependent()
        {
            IStatSystem statSystem = new StatSystem();
            statSystem.Player.SetBase(StatId.DamageMultiplier, 1f);
            statSystem.Run.SetBase(StatId.DamageMultiplier, 5f);

            Assert.AreEqual(1f, statSystem.Player.GetValue(StatId.DamageMultiplier));
            Assert.AreEqual(5f, statSystem.Run.GetValue(StatId.DamageMultiplier));
        }

        [Test]
        public void Sheets_AreStableInstances()
        {
            IStatSystem statSystem = new StatSystem();

            Assert.AreSame(statSystem.Player, statSystem.Player);
            Assert.AreNotSame(statSystem.Player, statSystem.Run);
        }
    }
}
```

- [ ] **Step 2: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`IStatSystem`/`StatSystem` do not exist yet). That is this step's "failing" state — proceed.

- [ ] **Step 3: Implement**

`Assets/_neon/Scripts/Stats/IStatSystem.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Owns the stat sheets. Momentum, Protocols, and the Signal are all just
    /// modifier sources on these sheets — they never reference each other.
    /// </summary>
    public interface IStatSystem
    {
        /// <summary>Per-player stats (auto-engage rate/damage, damage multiplier, ...).</summary>
        StatSheet Player { get; }

        /// <summary>Run/global stats (gain multiplier, spawn nastiness, objective speed, ...).</summary>
        StatSheet Run { get; }
    }
}
```

`Assets/_neon/Scripts/Stats/StatSystem.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    public sealed class StatSystem : IStatSystem
    {
        public StatSheet Player { get; } = new();
        public StatSheet Run { get; } = new();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: all 10 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add "Assets/_neon/Scripts/Stats/IStatSystem.cs" "Assets/_neon/Scripts/Stats/IStatSystem.cs.meta" "Assets/_neon/Scripts/Stats/StatSystem.cs" "Assets/_neon/Scripts/Stats/StatSystem.cs.meta" "Assets/_neon/Tests/EditMode/StatSystemTests.cs" "Assets/_neon/Tests/EditMode/StatSystemTests.cs.meta"
git commit -m "feat: add IStatSystem service with player + run sheets (M0)"
```

---

### Task 4: `IGameplaySignals` — R3 typed event bus (test-first)

The decoupling backbone (spec §4.1): one struct per event, publishers never know subscribers. Event structs themselves land with their publishers (M1+); M0 ships the bus.

**Files:**
- Modify: `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef` (add `R3.Unity` reference)
- Create: `Assets/_neon/Scripts/Signals/IGameplaySignals.cs`
- Create: `Assets/_neon/Scripts/Signals/GameplaySignals.cs`
- Test: `Assets/_neon/Tests/EditMode/GameplaySignalsTests.cs`

- [ ] **Step 1: Add the R3 reference to the runtime asmdef**

Edit `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef` — the `references` array currently reads:

```json
    "references": [
        "Eflatun.SceneReference",
        "UniTask",
        "Unity.2D.PixelPerfect",
        "Unity.InputSystem",
        "VContainer"
    ],
```

Change it to (keep every other field of the file untouched):

```json
    "references": [
        "Eflatun.SceneReference",
        "R3.Unity",
        "UniTask",
        "Unity.2D.PixelPerfect",
        "Unity.InputSystem",
        "VContainer"
    ],
```

(The core `R3.dll` comes from `Assets/Packages/R3.1.3.0/` via NuGetForUnity and is auto-referenced because `overrideReferences` is false — this mirrors how `BrainlessLabs.Neon.Lifecycle.asmdef` already consumes R3.)

- [ ] **Step 2: Write the failing test**

`Assets/_neon/Tests/EditMode/GameplaySignalsTests.cs`:

```csharp
using System;
using NUnit.Framework;
using R3;

namespace BrainlessLabs.Neon.Tests
{
    public class GameplaySignalsTests
    {
        private readonly struct TestSignalA
        {
            public readonly int Value;
            public TestSignalA(int value) { Value = value; }
        }

        private readonly struct TestSignalB
        {
            public readonly int Value;
            public TestSignalB(int value) { Value = value; }
        }

        [Test]
        public void Publish_DeliversPayloadToSubscriber()
        {
            using var signals = new GameplaySignals();
            int received = 0;
            using var subscription = signals.On<TestSignalA>().Subscribe(s => received = s.Value);

            signals.Publish(new TestSignalA(42));

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Publish_DifferentSignalTypes_AreIsolated()
        {
            using var signals = new GameplaySignals();
            int aCount = 0;
            int bCount = 0;
            using var subA = signals.On<TestSignalA>().Subscribe(_ => aCount++);
            using var subB = signals.On<TestSignalB>().Subscribe(_ => bCount++);

            signals.Publish(new TestSignalA(1));

            Assert.AreEqual(1, aCount);
            Assert.AreEqual(0, bCount);
        }

        [Test]
        public void MultipleSubscribers_AllReceive()
        {
            using var signals = new GameplaySignals();
            int first = 0;
            int second = 0;
            using var sub1 = signals.On<TestSignalA>().Subscribe(s => first = s.Value);
            using var sub2 = signals.On<TestSignalA>().Subscribe(s => second = s.Value);

            signals.Publish(new TestSignalA(7));

            Assert.AreEqual(7, first);
            Assert.AreEqual(7, second);
        }

        [Test]
        public void DisposedSubscription_StopsReceiving()
        {
            using var signals = new GameplaySignals();
            int count = 0;
            var subscription = signals.On<TestSignalA>().Subscribe(_ => count++);

            signals.Publish(new TestSignalA(1));
            subscription.Dispose();
            signals.Publish(new TestSignalA(2));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNotThrow()
        {
            using var signals = new GameplaySignals();

            Assert.DoesNotThrow(() => signals.Publish(new TestSignalA(1)));
        }
    }
}
```

- [ ] **Step 3: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`GameplaySignals` does not exist yet). Proceed.

- [ ] **Step 4: Implement**

`Assets/_neon/Scripts/Signals/IGameplaySignals.cs`:

```csharp
using R3;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Typed gameplay event bus. One struct per event; publishers never know
    /// their subscribers. HUD/feedback layers are pure consumers of this bus.
    /// </summary>
    public interface IGameplaySignals
    {
        /// <summary>Publish a signal to all current subscribers of T.</summary>
        void Publish<T>(T signal) where T : struct;

        /// <summary>The observable stream of T signals. Subscribe with R3 operators.</summary>
        Observable<T> On<T>() where T : struct;
    }
}
```

`Assets/_neon/Scripts/Signals/GameplaySignals.cs`:

```csharp
using System;
using System.Collections.Generic;
using R3;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// R3-backed implementation: lazily creates one Subject per signal type.
    /// Registered as a singleton; VContainer disposes it with its scope.
    /// </summary>
    public sealed class GameplaySignals : IGameplaySignals, IDisposable
    {
        private readonly Dictionary<Type, object> _subjects = new();

        public void Publish<T>(T signal) where T : struct
        {
            GetSubject<T>().OnNext(signal);
        }

        public Observable<T> On<T>() where T : struct
        {
            return GetSubject<T>();
        }

        public void Dispose()
        {
            foreach (var subject in _subjects.Values)
            {
                ((IDisposable)subject).Dispose();
            }
            _subjects.Clear();
        }

        private Subject<T> GetSubject<T>() where T : struct
        {
            if (_subjects.TryGetValue(typeof(T), out var existing))
            {
                return (Subject<T>)existing;
            }

            var subject = new Subject<T>();
            _subjects[typeof(T)] = subject;
            return subject;
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: all 15 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add "Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef" "Assets/_neon/Scripts/Signals" "Assets/_neon/Scripts/Signals.meta" "Assets/_neon/Tests/EditMode/GameplaySignalsTests.cs" "Assets/_neon/Tests/EditMode/GameplaySignalsTests.cs.meta"
git commit -m "feat: add IGameplaySignals R3 event bus (M0)"
```

---

### Task 5: `IGameplayClock` — ordered tick + scaled gameplay time (test-first)

Sole owner of gameplay tick + gameplay-time (spec §4.1). Hitstop / slow-mo / pause each hold a scale source; effective scale is their product. Systems register into an ordered tick so update order is deterministic. Driven by VContainer's `ITickable` entry-point loop (same mechanism as `ApplicationFSMRunner`); the core is `Advance(float)`, which tests call directly.

**Files:**
- Create: `Assets/_neon/Scripts/Clock/IGameplayTickable.cs`
- Create: `Assets/_neon/Scripts/Clock/IGameplayClock.cs`
- Create: `Assets/_neon/Scripts/Clock/GameplayClock.cs`
- Test: `Assets/_neon/Tests/EditMode/GameplayClockTests.cs`

- [ ] **Step 1: Write the failing test**

`Assets/_neon/Tests/EditMode/GameplayClockTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;

namespace BrainlessLabs.Neon.Tests
{
    public class GameplayClockTests
    {
        private sealed class RecordingTickable : IGameplayTickable
        {
            private readonly List<string> _log;
            private readonly string _name;
            public float LastDelta { get; private set; }

            public RecordingTickable(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public void Tick(float deltaTime)
            {
                LastDelta = deltaTime;
                _log.Add(_name);
            }
        }

        [Test]
        public void Advance_AccumulatesGameplayTime()
        {
            var clock = new GameplayClock();

            clock.Advance(0.5f);
            clock.Advance(0.5f);

            Assert.AreEqual(1f, clock.GameplayTime, 0.0001f);
            Assert.AreEqual(0.5f, clock.DeltaTime, 0.0001f);
        }

        [Test]
        public void SetScale_ScalesDeltaAndTime()
        {
            var clock = new GameplayClock();
            var slowMo = ModifierSource.Create("slowmo");

            clock.SetScale(slowMo, 0.5f);
            clock.Advance(1f);

            Assert.AreEqual(0.5f, clock.DeltaTime, 0.0001f);
            Assert.AreEqual(0.5f, clock.GameplayTime, 0.0001f);
        }

        [Test]
        public void MultipleScaleSources_Multiply()
        {
            var clock = new GameplayClock();
            clock.SetScale(ModifierSource.Create("hitstop"), 0.5f);
            clock.SetScale(ModifierSource.Create("slowmo"), 0.5f);

            clock.Advance(1f);

            Assert.AreEqual(0.25f, clock.DeltaTime, 0.0001f);
        }

        [Test]
        public void ClearScale_RestoresFullSpeed()
        {
            var clock = new GameplayClock();
            var pause = ModifierSource.Create("pause");
            clock.SetScale(pause, 0f);
            clock.Advance(1f);
            Assert.AreEqual(0f, clock.GameplayTime, 0.0001f);

            clock.ClearScale(pause);
            clock.Advance(1f);

            Assert.AreEqual(1f, clock.GameplayTime, 0.0001f);
        }

        [Test]
        public void Tickables_RunInOrder_NotRegistrationOrder()
        {
            var clock = new GameplayClock();
            var log = new List<string>();
            // Registered out of order on purpose. Spec order: AutoEngage(0) → FinishReady(10) → Selector(20) → MomentumDecay(30).
            clock.Register(new RecordingTickable("selector", log), order: 20);
            clock.Register(new RecordingTickable("autoEngage", log), order: 0);
            clock.Register(new RecordingTickable("finishReady", log), order: 10);

            clock.Advance(0.016f);

            CollectionAssert.AreEqual(new[] { "autoEngage", "finishReady", "selector" }, log);
        }

        [Test]
        public void SameOrder_PreservesRegistrationOrder()
        {
            var clock = new GameplayClock();
            var log = new List<string>();
            clock.Register(new RecordingTickable("first", log), order: 5);
            clock.Register(new RecordingTickable("second", log), order: 5);

            clock.Advance(0.016f);

            CollectionAssert.AreEqual(new[] { "first", "second" }, log);
        }

        [Test]
        public void Tickables_ReceiveScaledDelta()
        {
            var clock = new GameplayClock();
            var log = new List<string>();
            var tickable = new RecordingTickable("t", log);
            clock.Register(tickable, order: 0);
            clock.SetScale(ModifierSource.Create("slowmo"), 0.25f);

            clock.Advance(1f);

            Assert.AreEqual(0.25f, tickable.LastDelta, 0.0001f);
        }

        [Test]
        public void Unregister_StopsTicking()
        {
            var clock = new GameplayClock();
            var log = new List<string>();
            var tickable = new RecordingTickable("t", log);
            clock.Register(tickable, order: 0);
            clock.Advance(0.016f);

            clock.Unregister(tickable);
            clock.Advance(0.016f);

            Assert.AreEqual(1, log.Count);
        }
    }
}
```

- [ ] **Step 2: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`IGameplayTickable`/`GameplayClock` do not exist yet). Proceed.

- [ ] **Step 3: Implement**

`Assets/_neon/Scripts/Clock/IGameplayTickable.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>
    /// A system that participates in the ordered gameplay tick.
    /// Receives the gameplay-scaled delta time.
    /// </summary>
    public interface IGameplayTickable
    {
        void Tick(float deltaTime);
    }
}
```

`Assets/_neon/Scripts/Clock/IGameplayClock.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Sole owner of gameplay tick and gameplay time. All hitstop / slow-mo /
    /// pause effects route through scale sources here (product of all scales).
    /// Lower <c>order</c> ticks earlier; ties tick in registration order.
    /// Reserved order bands (spec §4.1): AutoEngage 0, FinishReady eval 10,
    /// selector 20, Momentum decay 30.
    /// </summary>
    public interface IGameplayClock
    {
        /// <summary>Accumulated gameplay-scaled time in seconds.</summary>
        float GameplayTime { get; }

        /// <summary>Gameplay-scaled delta of the most recent tick.</summary>
        float DeltaTime { get; }

        /// <summary>Product of all active scale sources (1 when none).</summary>
        float EffectiveScale { get; }

        void Register(IGameplayTickable tickable, int order);
        void Unregister(IGameplayTickable tickable);

        /// <summary>Set (or update) this source's time scale. 0 = paused, 1 = full speed.</summary>
        void SetScale(ModifierSource source, float scale);

        /// <summary>Remove this source's contribution.</summary>
        void ClearScale(ModifierSource source);
    }
}
```

`Assets/_neon/Scripts/Clock/GameplayClock.cs`:

```csharp
using System.Collections.Generic;
using VContainer.Unity;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Registered as a VContainer entry point (ITickable) so the DI player loop
    /// drives Advance() once per frame with Time.deltaTime. Tests call Advance()
    /// directly. Do not register/unregister tickables from inside Tick.
    /// </summary>
    public sealed class GameplayClock : IGameplayClock, ITickable
    {
        private struct Entry
        {
            public int Order;
            public int Sequence;
            public IGameplayTickable Tickable;
        }

        private readonly List<Entry> _entries = new();
        private readonly Dictionary<ModifierSource, float> _scales = new();
        private int _nextSequence;
        private bool _orderDirty;
        private bool _loggedFirstTick;

        public float GameplayTime { get; private set; }
        public float DeltaTime { get; private set; }

        public float EffectiveScale
        {
            get
            {
                float scale = 1f;
                foreach (var value in _scales.Values)
                {
                    scale *= value;
                }
                return scale;
            }
        }

        void ITickable.Tick()
        {
            if (!_loggedFirstTick)
            {
                UnityEngine.Debug.Log("[Gameplay] GameplayClock ticking.");
                _loggedFirstTick = true;
            }
            Advance(UnityEngine.Time.deltaTime);
        }

        /// <summary>Advance gameplay time by an unscaled delta and run the ordered tick.</summary>
        public void Advance(float unscaledDeltaTime)
        {
            DeltaTime = unscaledDeltaTime * EffectiveScale;
            GameplayTime += DeltaTime;

            if (_orderDirty)
            {
                _entries.Sort(static (a, b) =>
                    a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Sequence.CompareTo(b.Sequence));
                _orderDirty = false;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].Tickable.Tick(DeltaTime);
            }
        }

        public void Register(IGameplayTickable tickable, int order)
        {
            _entries.Add(new Entry { Order = order, Sequence = _nextSequence++, Tickable = tickable });
            _orderDirty = true;
        }

        public void Unregister(IGameplayTickable tickable)
        {
            _entries.RemoveAll(e => ReferenceEquals(e.Tickable, tickable));
        }

        public void SetScale(ModifierSource source, float scale)
        {
            _scales[source] = scale;
        }

        public void ClearScale(ModifierSource source)
        {
            _scales.Remove(source);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: all 23 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add "Assets/_neon/Scripts/Clock" "Assets/_neon/Scripts/Clock.meta" "Assets/_neon/Tests/EditMode/GameplayClockTests.cs" "Assets/_neon/Tests/EditMode/GameplayClockTests.cs.meta"
git commit -m "feat: add IGameplayClock - ordered tick with scale sources (M0)"
```

---

### Task 6: `GameplayServicesState` — register the spine in the boot FSM

New FSM state between `GameServicesState` and `GameState` (spec §4.3), registering the run-agnostic engine singletons. This honors non-negotiable rule 3: FSM-state registration, never `ApplicationLifetimeScope`. The FSM chain nests: each state registers its `NextStateType` and adds it as a sub-state via a health-checked transition, so this scope stays alive for the whole session — exactly like `GameServicesState` does today.

**Files:**
- Create: `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs`
- Modify: `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameServicesState.cs` (lines 8–14: doc comment + `NextStateType`)

- [ ] **Step 1: Create the new state**

`Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs` (mirrors `GameServicesState` exactly — same base class, same transition helper):

```csharp
using System;
using VContainer;
using VContainer.Unity;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Registers the run-agnostic gameplay engine spine (spec §4.1):
    /// IGameplaySignals (event bus), IStatSystem (stat sheets),
    /// IGameplayClock (ordered gameplay tick, driven as an ITickable entry point).
    /// Transitions to GameState when all services are healthy.
    /// </summary>
    internal class GameplayServicesState : LifetimeStateMachine
    {
        public readonly Type NextStateType = typeof(GameState);

        public GameplayServicesState(LifetimeScope lifetimeScope) : base(lifetimeScope)
        {
        }

        protected override void RegisterTypes(IContainerBuilder builder)
        {
            base.RegisterTypes(builder);
            RegisterNextState(builder);
            RegisterGameplaySignals(builder);
            RegisterStatSystem(builder);
            RegisterGameplayClock(builder);
        }

        private static void RegisterGameplaySignals(IContainerBuilder builder)
        {
            builder.Register<GameplaySignals>(Lifetime.Singleton)
                .As<IGameplaySignals>();
        }

        private static void RegisterStatSystem(IContainerBuilder builder)
        {
            builder.Register<StatSystem>(Lifetime.Singleton)
                .As<IStatSystem>();
        }

        private static void RegisterGameplayClock(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameplayClock>()
                .As<IGameplayClock>();
        }

        private void RegisterNextState(IContainerBuilder builder)
        {
            builder.Register(NextStateType, Lifetime.Transient).AsSelf();
        }

        protected override void OnLifetimeScopeReady(IObjectResolver container)
        {
            base.OnLifetimeScopeReady(container);
            CreateAndAddTargetStateWithHealthCheckedTransition(
                container,
                NextStateType.Name,
                NextStateType
            );
        }
    }
}
```

(`GameplaySignals` is `IDisposable`; VContainer disposes registered singletons with the scope. `RegisterEntryPoint<GameplayClock>()` hooks its `ITickable.Tick()` into the DI player loop — the same mechanism `ApplicationFSMRunner` and the platform services already use.)

- [ ] **Step 2: Retarget `GameServicesState` at the new state**

In `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameServicesState.cs`, change:

```csharp
    /// <summary>
    /// Initializes game-specific services.
    /// Currently a placeholder for future services.
    /// Transitions to GameState when all services are healthy.
    /// </summary>
    internal class GameServicesState : LifetimeStateMachine
    {
        public readonly Type NextStateType = typeof(GameState);
```

to:

```csharp
    /// <summary>
    /// Initializes game-specific services.
    /// Transitions to GameplayServicesState when all services are healthy.
    /// </summary>
    internal class GameServicesState : LifetimeStateMachine
    {
        public readonly Type NextStateType = typeof(GameplayServicesState);
```

Nothing else in the file changes.

- [ ] **Step 3: Compile + run all EditMode tests**

Refresh Unity. Expected: 0 console errors, all 23 tests still PASS.

- [ ] **Step 4: Boot play-test (Recipe 4 — runtime is ground truth)**

1. Open `Assets/Resources/Settings/BootstrapSettingsAsset.asset`; confirm **Enable Editor Bootstrap** and **Execute Bootstrap Sequence** are ticked and **Post-Bootstrap Scene** points at `SceneDefinition_Level1`.
2. Press Play (or `mcp__unityMCP__manage_editor` play).
3. Watch the console for this exact sequence (interleaved with other logs):
   - `[Lifecycle] Entering state: GameServicesState`
   - `[Lifecycle] Entering state: GameplayServicesState` ← the new state
   - `[Lifecycle] Entering state: GameState`
   - `[Lifecycle] Game state entered. Bootstrap complete!`
   - `[Gameplay] GameplayClock ticking.` ← proves the entry-point tick is live
   - `[Lifecycle] Loading post-bootstrap scene: …`
4. Confirm `03_Level1` loads and plays as before (player spawns, no errors).
5. Exit Play mode before doing anything else (working agreement 4).

Expected: all six log lines present, zero errors. If `[Gameplay] GameplayClock ticking.` never appears, the entry-point loop is not running in the child scope — **stop and investigate against VContainer's entry-point docs before proceeding** (this plan's later milestones depend on the clock ticking).

- [ ] **Step 5: Commit**

```bash
git add "Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs" "Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs.meta" "Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameServicesState.cs"
git commit -m "feat: add GameplayServicesState registering the engine spine (M0)"
```

---

### Task 7: `BrainlessLabs.Neon.Simulation` assembly (empty ECS bootstrap)

The DOTS leaf assembly (spec §4.3): references Unity DOTS packages only, none of ours; `Neon → Simulation` is the only inbound reference. Ships with the first real component so the assembly actually compiles into existence (Unity skips asmdefs with zero scripts).

**Files:**
- Create: `Assets/_neon/Scripts/Simulation/BrainlessLabs.Neon.Simulation.asmdef`
- Create: `Assets/_neon/Scripts/Simulation/SwarmAgent.cs`
- Modify: `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef` (add `BrainlessLabs.Neon.Simulation` reference)

- [ ] **Step 1: Create the asmdef**

`Assets/_neon/Scripts/Simulation/BrainlessLabs.Neon.Simulation.asmdef`:

```json
{
    "name": "BrainlessLabs.Neon.Simulation",
    "rootNamespace": "BrainlessLabs.Neon.Simulation",
    "references": [
        "Unity.Entities",
        "Unity.Burst",
        "Unity.Collections",
        "Unity.Mathematics"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Create the first sim component**

`Assets/_neon/Scripts/Simulation/SwarmAgent.cs`:

```csharp
using Unity.Entities;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>
    /// Swarm density tiers (spec §5.2): Chaff = hot, hittable, finish-ready-capable;
    /// Ambient = pure-vibe props.
    /// </summary>
    public enum SwarmTier : byte
    {
        Chaff = 0,
        Ambient = 1
    }

    /// <summary>
    /// Marks an entity as a swarm agent. The full component set
    /// (BeltPosition, Velocity, Health, HotFlag, FinishReadyTag, EngageIntent)
    /// lands with the swarm systems in Milestone 1 (Plan 2).
    /// </summary>
    public struct SwarmAgent : IComponentData
    {
        public SwarmTier Tier;
    }
}
```

- [ ] **Step 3: Lock the reference direction `Neon → Simulation`**

Edit `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef` — the `references` array (as of Task 4) reads:

```json
    "references": [
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
        "Unity.InputSystem",
        "VContainer"
    ],
```

- [ ] **Step 4: Compile + verify no reference-direction violations**

Refresh Unity. Expected: 0 console errors. Sanity checks:
- `BrainlessLabs.Neon.Simulation.asmdef` references NO BrainlessLabs assembly (it is a pure leaf).
- `BrainlessLabs.Neon.Lifecycle.asmdef` and `BrainlessLabs.Neon.Editor.asmdef` are untouched.

- [ ] **Step 5: Run all EditMode tests**

Expected: all 23 tests still PASS.

- [ ] **Step 6: Commit**

```bash
git add "Assets/_neon/Scripts/Simulation" "Assets/_neon/Scripts/Simulation.meta" "Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef"
git commit -m "feat: add BrainlessLabs.Neon.Simulation DOTS leaf assembly (M0)"
```

---

### Task 8: M0 gate — full verification + push

Spec §7 M0 gate: *compiles, boots clean, EditMode tests green for stat folding/modifier stacking.*

- [ ] **Step 1: Run the full EditMode suite**

Expected: 23/23 PASS (1 smoke + 7 StatSheet + 2 StatSystem + 5 GameplaySignals + 8 GameplayClock).

- [ ] **Step 2: Full boot play-test (Recipe 4)**

Repeat Task 6 Step 4 end-to-end once more on the final M0 code: boot → all six log lines → `03_Level1` playable → walk right until `[Level] Wave 1/N started.` appears → zero console errors. Exit Play mode.

- [ ] **Step 3: Record the gate**

Append to the bottom of this plan document a short `## M0 gate record` section: date, test count, the observed boot log sequence, and any deviations. Commit it with the message `docs: record M0 gate pass`.

- [ ] **Step 4: Push the branch**

```bash
git push -u origin claude/neon-engine-base
```

(Studio push floor: don't accumulate unpushed commits. M0 is a natural push point.)

---

## Milestone M1 — render spike (gates the rest of M1)

**What this answers (spec §5.2 / §9):** Unity has no turnkey 2D-sprite ECS renderer. Can we hold target FPS with **150 hot chaff as pooled `SpriteRenderer` proxies synced from ECS** + **~100 ambient as `Graphics.RenderMeshInstanced` quads**? Everything here is **throwaway**: self-contained assembly, nothing references it, deliberately zero DI/service usage (so it breaks no DI rules), deleted or archived after the verdict.

**Pass bar:** ≥ 60 FPS sustained (smoothed, 60s) in editor Play mode on the dev machine, with the ECS sim moving all 250 agents every frame. Editor numbers are pessimistic; if editor lands 45–60, a standalone development build measure decides.

### Task 9: Spike assembly + ECS sim

**Files:**
- Create: `Assets/_neon/Spikes/SwarmRenderSpike/BrainlessLabs.Neon.SwarmRenderSpike.asmdef`
- Create: `Assets/_neon/Spikes/SwarmRenderSpike/SpikeComponents.cs`
- Create: `Assets/_neon/Spikes/SwarmRenderSpike/SpikeMoveSystem.cs`
- Create: `Assets/_neon/Spikes/SwarmRenderSpike/SpikeBootstrap.cs`

- [ ] **Step 1: Create the spike asmdef**

`Assets/_neon/Spikes/SwarmRenderSpike/BrainlessLabs.Neon.SwarmRenderSpike.asmdef`:

```json
{
    "name": "BrainlessLabs.Neon.SwarmRenderSpike",
    "rootNamespace": "BrainlessLabs.Neon.SwarmRenderSpike",
    "references": [
        "Unity.Entities",
        "Unity.Burst",
        "Unity.Collections",
        "Unity.Mathematics"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

(Deliberately does NOT reference any BrainlessLabs assembly — the spike must not leak into the game, and no game code may reference the spike.)

- [ ] **Step 2: Create the components**

`Assets/_neon/Spikes/SwarmRenderSpike/SpikeComponents.cs` — note we use our own 2D position component, not `Unity.Transforms.LocalTransform`, matching the final design's belt-position model (spec §5.2):

```csharp
using Unity.Entities;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.SwarmRenderSpike
{
    public struct SpikePosition : IComponentData
    {
        public float2 Value;
    }

    public struct SpikeVelocity : IComponentData
    {
        public float2 Value;
    }

    /// <summary>Hot chaff — rendered via pooled SpriteRenderer proxies.</summary>
    public struct HotAgentTag : IComponentData
    {
    }

    /// <summary>Ambient vibe props — rendered via Graphics.RenderMeshInstanced.</summary>
    public struct AmbientAgentTag : IComponentData
    {
    }

    /// <summary>Singleton: the belt rect agents bounce inside.</summary>
    public struct SpikeBounds : IComponentData
    {
        public float2 Min;
        public float2 Max;
    }
}
```

- [ ] **Step 3: Create the Burst movement system**

`Assets/_neon/Spikes/SwarmRenderSpike/SpikeMoveSystem.cs`:

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.SwarmRenderSpike
{
    /// <summary>
    /// Bounce-wander inside SpikeBounds. RequireForUpdate keeps this system idle
    /// in every scene that has no spike entities (systems live in the default
    /// world regardless of scene).
    /// </summary>
    [BurstCompile]
    public partial struct SpikeMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpikeBounds>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var bounds = SystemAPI.GetSingleton<SpikeBounds>();
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (position, velocity) in
                     SystemAPI.Query<RefRW<SpikePosition>, RefRW<SpikeVelocity>>())
            {
                float2 p = position.ValueRO.Value + velocity.ValueRO.Value * deltaTime;
                float2 v = velocity.ValueRO.Value;

                if (p.x < bounds.Min.x || p.x > bounds.Max.x)
                {
                    v.x = -v.x;
                    p.x = math.clamp(p.x, bounds.Min.x, bounds.Max.x);
                }
                if (p.y < bounds.Min.y || p.y > bounds.Max.y)
                {
                    v.y = -v.y;
                    p.y = math.clamp(p.y, bounds.Min.y, bounds.Max.y);
                }

                position.ValueRW.Value = p;
                velocity.ValueRW.Value = v;
            }
        }
    }
}
```

- [ ] **Step 4: Create the entity bootstrap**

`Assets/_neon/Spikes/SwarmRenderSpike/SpikeBootstrap.cs`:

```csharp
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BrainlessLabs.Neon.SwarmRenderSpike
{
    /// <summary>
    /// Spawns the spike population into the default ECS world.
    /// Throwaway spike driver — intentionally uses no DI services.
    /// </summary>
    public class SpikeBootstrap : MonoBehaviour
    {
        [SerializeField] private int _hotCount = 150;
        [SerializeField] private int _ambientCount = 100;
        [SerializeField] private Vector2 _boundsMin = new(-16f, -4.5f);
        [SerializeField] private Vector2 _boundsMax = new(16f, 4.5f);
        [SerializeField] private float _minSpeed = 0.5f;
        [SerializeField] private float _maxSpeed = 3f;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("[Spike] No default ECS world — automatic Entities bootstrap is disabled?");
                return;
            }

            var entityManager = world.EntityManager;

            var boundsEntity = entityManager.CreateEntity(typeof(SpikeBounds));
            entityManager.SetComponentData(boundsEntity, new SpikeBounds
            {
                Min = new float2(_boundsMin.x, _boundsMin.y),
                Max = new float2(_boundsMax.x, _boundsMax.y)
            });

            var random = new Unity.Mathematics.Random(1234);
            Spawn<HotAgentTag>(entityManager, ref random, _hotCount);
            Spawn<AmbientAgentTag>(entityManager, ref random, _ambientCount);

            Debug.Log($"[Spike] Spawned {_hotCount} hot + {_ambientCount} ambient agents.");
        }

        private void OnDestroy()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                return;
            }

            var entityManager = world.EntityManager;
            using var agents = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpikePosition>());
            entityManager.DestroyEntity(agents);
            using var bounds = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpikeBounds>());
            entityManager.DestroyEntity(bounds);
        }

        private void Spawn<TTag>(EntityManager entityManager, ref Unity.Mathematics.Random random, int count)
            where TTag : struct, IComponentData
        {
            var archetype = entityManager.CreateArchetype(
                typeof(SpikePosition), typeof(SpikeVelocity), typeof(TTag));
            using var entities = entityManager.CreateEntity(archetype, count, Allocator.Temp);

            var min = new float2(_boundsMin.x, _boundsMin.y);
            var max = new float2(_boundsMax.x, _boundsMax.y);

            foreach (var entity in entities)
            {
                entityManager.SetComponentData(entity, new SpikePosition
                {
                    Value = random.NextFloat2(min, max)
                });
                entityManager.SetComponentData(entity, new SpikeVelocity
                {
                    Value = random.NextFloat2Direction() * random.NextFloat(_minSpeed, _maxSpeed)
                });
            }
        }
    }
}
```

- [ ] **Step 5: Compile check**

Refresh Unity. Expected: 0 console errors. (If `SystemAPI.Query` or `ISystem` fail to resolve, confirm the asmdef references landed and Burst compiled — check the console for Burst compilation messages.)

- [ ] **Step 6: Commit**

```bash
git add "Assets/_neon/Spikes" "Assets/_neon/Spikes.meta"
git commit -m "spike: ECS sim for swarm render spike - 250 bounce-wander agents (M1 spike)"
```

---

### Task 10: Spike renderers — hot proxy pool + instanced ambient + FPS HUD

**Files:**
- Create: `Assets/_neon/Spikes/SwarmRenderSpike/HotProxyPool.cs`
- Create: `Assets/_neon/Spikes/SwarmRenderSpike/AmbientInstancedRenderer.cs`
- Create: `Assets/_neon/Spikes/SwarmRenderSpike/SpikeFpsHud.cs`

- [ ] **Step 1: Create the hot proxy pool**

`Assets/_neon/Spikes/SwarmRenderSpike/HotProxyPool.cs`:

```csharp
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BrainlessLabs.Neon.SwarmRenderSpike
{
    /// <summary>
    /// R6 hot-chaff path: a pool of SpriteRenderer proxies whose transforms are
    /// copied from ECS each LateUpdate. Index-based mapping is fine for the spike
    /// (perf question only); the real bridge will need stable entity↔proxy mapping.
    /// </summary>
    public class HotProxyPool : MonoBehaviour
    {
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _hotColor = new(1f, 0.25f, 0.6f, 1f);
        [SerializeField] private int _capacity = 150;

        private Transform[] _proxies;
        private EntityQuery _query;
        private bool _ready;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("[Spike] HotProxyPool: no default ECS world.");
                return;
            }
            if (_sprite == null)
            {
                Debug.LogError("[Spike] HotProxyPool: no sprite assigned.");
                return;
            }

            _query = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<SpikePosition>(),
                ComponentType.ReadOnly<HotAgentTag>());

            _proxies = new Transform[_capacity];
            for (int i = 0; i < _capacity; i++)
            {
                var proxy = new GameObject($"HotProxy_{i}");
                proxy.transform.SetParent(transform, worldPositionStays: false);
                var spriteRenderer = proxy.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = _sprite;
                spriteRenderer.color = _hotColor;
                proxy.SetActive(false);
                _proxies[i] = proxy.transform;
            }

            _ready = true;
        }

        private void LateUpdate()
        {
            if (!_ready)
            {
                return;
            }

            using var positions = _query.ToComponentDataArray<SpikePosition>(Allocator.Temp);
            int active = Mathf.Min(positions.Length, _proxies.Length);

            for (int i = 0; i < active; i++)
            {
                var proxy = _proxies[i];
                if (!proxy.gameObject.activeSelf)
                {
                    proxy.gameObject.SetActive(true);
                }
                var p = positions[i].Value;
                proxy.localPosition = new Vector3(p.x, p.y, 0f);
            }

            for (int i = active; i < _proxies.Length; i++)
            {
                if (_proxies[i].gameObject.activeSelf)
                {
                    _proxies[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
```

- [ ] **Step 2: Create the ambient instanced renderer**

`Assets/_neon/Spikes/SwarmRenderSpike/AmbientInstancedRenderer.cs`:

```csharp
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BrainlessLabs.Neon.SwarmRenderSpike
{
    /// <summary>
    /// R6 ambient path: draws every ambient agent as an instanced textured quad.
    /// The assigned material must have "Enable GPU Instancing" ticked.
    /// </summary>
    public class AmbientInstancedRenderer : MonoBehaviour
    {
        [SerializeField] private Material _material;
        [SerializeField] private float _agentSize = 1f;

        private Mesh _quad;
        private EntityQuery _query;
        private Matrix4x4[] _matrices;
        private bool _ready;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("[Spike] AmbientInstancedRenderer: no default ECS world.");
                return;
            }
            if (_material == null)
            {
                Debug.LogError("[Spike] AmbientInstancedRenderer: no material assigned.");
                return;
            }
            if (!_material.enableInstancing)
            {
                Debug.LogError("[Spike] AmbientInstancedRenderer: material must have GPU instancing enabled.");
                return;
            }

            _query = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<SpikePosition>(),
                ComponentType.ReadOnly<AmbientAgentTag>());
            _quad = BuildQuad();
            _ready = true;
        }

        private void Update()
        {
            if (!_ready)
            {
                return;
            }

            using var positions = _query.ToComponentDataArray<SpikePosition>(Allocator.Temp);
            if (positions.Length == 0)
            {
                return;
            }

            if (_matrices == null || _matrices.Length < positions.Length)
            {
                _matrices = new Matrix4x4[positions.Length];
            }

            var scale = new Vector3(_agentSize, _agentSize, 1f);
            for (int i = 0; i < positions.Length; i++)
            {
                var p = positions[i].Value;
                // z = 1 puts ambient behind the hot proxies (z = 0).
                _matrices[i] = Matrix4x4.TRS(new Vector3(p.x, p.y, 1f), Quaternion.identity, scale);
            }

            // Explicit worldBounds: the default zero-size bounds can get the whole
            // instanced draw frustum-culled.
            var renderParams = new RenderParams(_material)
            {
                worldBounds = new Bounds(Vector3.zero, new Vector3(200f, 50f, 10f))
            };
            Graphics.RenderMeshInstanced(renderParams, _quad, 0, _matrices, positions.Length);
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

- [ ] **Step 3: Create the FPS HUD**

`Assets/_neon/Spikes/SwarmRenderSpike/SpikeFpsHud.cs`:

```csharp
using UnityEngine;

namespace BrainlessLabs.Neon.SwarmRenderSpike
{
    public class SpikeFpsHud : MonoBehaviour
    {
        private float _smoothedDelta;

        private void Update()
        {
            _smoothedDelta = _smoothedDelta <= 0f
                ? Time.unscaledDeltaTime
                : Mathf.Lerp(_smoothedDelta, Time.unscaledDeltaTime, 0.05f);
        }

        private void OnGUI()
        {
            float fps = _smoothedDelta > 0f ? 1f / _smoothedDelta : 0f;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 22 };
            GUI.Label(new Rect(10, 10, 500, 30), $"FPS (smoothed): {fps:F1}", style);
        }
    }
}
```

- [ ] **Step 4: Compile check + commit**

Refresh Unity, expect 0 errors, then:

```bash
git add "Assets/_neon/Spikes/SwarmRenderSpike"
git commit -m "spike: hot proxy pool + instanced ambient renderer + FPS HUD (M1 spike)"
```

---

### Task 11: Spike scene, material, and boot wiring

Editor work — do it in Unity (or via `mcp__unityMCP__manage_scene` / `manage_material` / `manage_gameobject`). No git/asset writes while in Play mode.

- [ ] **Step 1: Create the instanced material**

In the Project window at `Assets/_neon/Spikes/SwarmRenderSpike/`: right-click → Create → Material, name it `SpikeAmbient`. In the inspector:
- Shader: **Universal Render Pipeline/Unlit**
- Surface Type: **Transparent**
- Base Map texture: `Assets/_neon/Sprites/Effects/HitEffect.png` (any sprite texture works), Base color ~50% grey (ambient must read muted vs hot — spec §5.5's hot-vs-ambient rule)
- Tick **Enable GPU Instancing** (bottom of the inspector)

- [ ] **Step 2: Create the spike scene**

1. File → New Scene → save as `Assets/_neon/Scenes/Spikes/99_SwarmRenderSpike.unity`.
2. Ensure a **Main Camera** exists (tag `MainCamera`): Projection **Orthographic**, Size **6**, Position `(0, 0, -10)`, Background solid dark (e.g. `#101018`).
3. Create an empty GameObject `SpikeRig` at origin and add all four spike components:
   - `SpikeBootstrap` (defaults: 150 hot / 100 ambient)
   - `HotProxyPool` — assign **Sprite** = `Assets/_neon/Sprites/Effects/HitEffect.png`
   - `AmbientInstancedRenderer` — assign **Material** = `SpikeAmbient`
   - `SpikeFpsHud`
4. Save the scene. **Deliberately no `Level` component and no `[Inject]` anywhere** — the scene resolves zero services, so it cannot hit the DI-bootstrap failure mode.

- [ ] **Step 3: Add the scene to the build scene list**

File → Build Profiles (Unity 6) → Scene List → **Add Open Scenes** with `99_SwarmRenderSpike` open. (Required for `IScenesService.LoadSceneAsync` to load it at runtime.)

- [ ] **Step 4: Create the scene definition**

Project window at `Assets/_neon/Scenes/Configs/`: right-click → Create → **Neon/Scenes/Scene Definition**, name it `SceneDefinition_SwarmRenderSpike`. Set:
- `_sceneReference` → `99_SwarmRenderSpike`
- `_sceneName` → `SwarmRenderSpike`
- `_sceneType` → `Level`

- [ ] **Step 5: Boot into the spike (Recipe 4)**

1. Open `Assets/Resources/Settings/BootstrapSettingsAsset.asset`.
2. Note the current **Post-Bootstrap Scene** value (you will restore it in Task 12), then set it to `SceneDefinition_SwarmRenderSpike`.
3. Press Play. Expected console sequence: the full boot chain from Task 6 (including `[Lifecycle] Entering state: GameplayServicesState` and `[Gameplay] GameplayClock ticking.`), then `[Lifecycle] Loading post-bootstrap scene: SwarmRenderSpike`, then `[Spike] Spawned 150 hot + 100 ambient agents.`
4. Visually confirm: ~150 tinted sprites bouncing around, a muted instanced-quad crowd behind them, FPS label top-left. Zero console errors.
5. Exit Play mode.

- [ ] **Step 6: Commit**

Do NOT commit `Assets/Resources/Settings/BootstrapSettingsAsset.asset` — the post-bootstrap flip to the spike scene is a local toggle; Task 12 restores it.

```bash
git add "Assets/_neon/Spikes/SwarmRenderSpike" "Assets/_neon/Scenes/Spikes" "Assets/_neon/Scenes/Spikes.meta" "Assets/_neon/Scenes/Configs/SceneDefinition_SwarmRenderSpike.asset" "Assets/_neon/Scenes/Configs/SceneDefinition_SwarmRenderSpike.asset.meta" "ProjectSettings/EditorBuildSettings.asset"
git commit -m "spike: swarm render spike scene + boot wiring (M1 spike)"
```

---

### Task 12: Measure, record the verdict, decide

**Files:**
- Create: `docs/superpowers/plans/2026-07-04-swarm-render-spike-verdict.md`
- Modify: `Assets/Resources/Settings/BootstrapSettingsAsset.asset` (restore post-bootstrap scene)

- [ ] **Step 1: Measurement run**

Boot into the spike (Task 11 Step 5) and stay in Play mode for **at least 60 seconds**. Record:
1. Smoothed FPS after 60s (the HUD label) — and the lowest value you observe.
2. Game view Stats overlay: **Batches / SetPass calls** (expect ambient to collapse into a handful of instanced batches; if batches ≈ 100+ for ambient, instancing is NOT working — check the material toggle).
3. Unity Profiler (Window → Analysis → Profiler): CPU main-thread ms; note the cost of `HotProxyPool.LateUpdate` (the proxy-sync tax — the number Plan 2's bridge design needs) and `AmbientInstancedRenderer.Update`.
4. Exit Play mode.

- [ ] **Step 2: Restore the boot target**

Set **Post-Bootstrap Scene** in `BootstrapSettingsAsset.asset` back to the value noted in Task 11 Step 2 (`SceneDefinition_Level1`).

- [ ] **Step 3: Write the verdict document**

Create `docs/superpowers/plans/2026-07-04-swarm-render-spike-verdict.md` with the measured numbers filled in:

```markdown
# Swarm render spike — verdict

- **Date:**
- **Machine:** (CPU / GPU)
- **Population:** 150 hot proxies + 100 instanced ambient, all moved by SpikeMoveSystem every frame
- **Smoothed FPS @60s (editor):**
- **Lowest observed FPS:**
- **Batches / SetPass (Stats overlay):**
- **HotProxyPool.LateUpdate cost (ms):**
- **AmbientInstancedRenderer.Update cost (ms):**
- **Standalone build FPS (only if editor was 45–60):**

## Verdict (pick one)

- [ ] **PASS** (≥60 FPS sustained): Plan 2 proceeds on the hybrid design as specced —
      DOTS sim + pooled SpriteRenderer proxies for hot chaff + instanced ambient.
- [ ] **MARGINAL** (45–60 FPS): profile first. If HotProxyPool.LateUpdate dominates, the
      proxy-sync loop is the target (batch transforms via Transform.SetPositionAndRotation,
      or sync only on-screen agents). If instanced ambient dominates, drop ambient to a
      static decorative pool. Re-measure once; if still <60, take FAIL.
- [ ] **FAIL** (<45 FPS or a blocking defect): take the spec's fallback — hot chaff as
      pooled MonoBehaviours (no ECS for chaff), ambient stays instanced or becomes a
      decorative pool. Only Layer-1 swarm internals change; the engagement spine still
      talks to the bridge (spec §5.2). Plan 2 is written against the fallback.

## Notes

(anything surprising: Burst warnings, GC allocs in the sync loop, editor-vs-build gap, ...)
```

- [ ] **Step 4: Commit + push**

After Step 2's restore, `git status` should show `BootstrapSettingsAsset.asset` clean (it matches HEAD again). If it still shows modified, the restore missed — fix that before committing.

```bash
git add "docs/superpowers/plans/2026-07-04-swarm-render-spike-verdict.md"
git commit -m "docs: record swarm render spike verdict (M1 spike gate)"
git push
```

- [ ] **Step 5: Plan complete — hand off**

This plan ends here. Report the M0 gate record and the spike verdict to Sebastien. **Plan 2 (M1 remainder) must be written against the verdict** — do not start swarm/bridge/auto-engage work without it.

---

## Spec coverage self-check (for reviewers)

- Spec §7 M0: Simulation asmdef ✅ (Task 7) · StatSystem/StatSheet/StatModifier ✅ (Tasks 2–3) · IGameplaySignals R3 ✅ (Task 4) · IGameplayClock ✅ (Task 5) · GameplayServicesState ✅ (Task 6) · gate: compiles/boots/tests ✅ (Task 8).
- Spec §7 M1 "spike first": 150 hot proxy pool + ~100 instanced ambient ✅ (Tasks 9–11) · measured verdict + fallback decision ✅ (Task 12).
- Spec §10 non-negotiables: DI-bootstrap honored (spike boots via Recipe 4, resolves zero services) · VContainer-only, no statics (`ModifierSource.Create` is a value-type ID mint, not a service locator) · FSM-state registration · no legacy touched · runtime gates in Tasks 6/8/11/12 · no invented APIs (every existing type referenced was read from source: `LifetimeStateMachine`, `GameServicesState`, `SceneDefinitionAsset`, `BootstrapSettingsAsset`) · assembly direction preserved (Task 7 Step 4).
- Deliberately deferred to Plan 2+ (spec §7): swarm systems, SwarmBridge, AutoEngage, FinishReady, Momentum, FinishResolver, AI_Active fix, HUD, economy, protocols, run FSM, Signal, actives.

---

## M0 gate record

- **Date:** 2026-07-04
- **EditMode tests:** 23/23 PASS (1 smoke + 7 StatSheet + 2 StatSystem + 5 GameplaySignals + 8 GameplayClock), assembly `BrainlessLabs.Neon.Tests.EditMode`.
- **Observed boot log sequence (editor Play, bootstrap DI flow):**
  1. `[Lifecycle] Entering state: GameServicesState`
  2. `[Lifecycle] Entering state: GameplayServicesState` ← new state
  3. `[Gameplay] GameplayClock ticking.` ← entry-point tick live (fires as soon as the scope builds, before GameState entry)
  4. `[Lifecycle] Entering state: GameState`
  5. `[Lifecycle] Loading post-bootstrap scene: Level One`
  6. `[Lifecycle] Game state entered. Bootstrap complete!`
- **Runtime check:** `03_Level1` loaded, `Player(Clone)` spawned; player moved right → `[SpawnerService] Starting wave 0: 'level-01-wave-01-easy' (10 enemies)` + `[Level] Wave 1/1 started.` Zero game errors.
- **Deviations:**
  1. Test asmdef needed `R3.dll` added to `precompiledReferences` (it sets `overrideReferences: true`, so the precompiled R3 core DLL is not auto-referenced). Committed with Task 4.
  2. `BootstrapSettingsAsset` post-bootstrap scene at HEAD is `SceneDefinition_MainMenu`, not `SceneDefinition_Level1` as the plan assumed. Flipped locally to Level1 for the play-tests (uncommitted local toggle, same treatment as the Task 11 spike flip); final restore target is the HEAD value (MainMenu).
  3. Full-project EditMode runs include pre-existing third-party `DTT.Utils` test failures (broken addon tests, unrelated). Test runs were scoped to `BrainlessLabs.Neon.Tests.EditMode`.
