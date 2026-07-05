# Neon Engine Base — Plan 3: M2 Growth (Economy · Protocols · Progression · Finish Challenges) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the growth layer — `IEconomySystem` (Momentum-multiplied XP/Charge/Overcharge), data-driven `ProtocolDefinitionAsset` + `IProtocolService` (draft, stacking, hidden-tree gating), `IProgressionSystem` (XP curve → slow-mo 1-of-3 level-up picker), 8 authored protocols from the design catalog, and the tiered hero `IFinishChallenge` — so a run **snowballs** and **Overdrive screams** (spec §7 M2 gate).

**Architecture:** Everything rides the M0/M1 spine as designed: every protocol is a **bundle of stat modifiers** applied via `StatSheet.AddModifier` (Momentum stays the only multiplier — protocol doc §8.1); the economy never knows what Momentum is (it reads `Run.GainMultiplier`); the picker slow-mo routes through `IGameplayClock`, which this milestone makes the true owner of `Time.timeScale`. Behavior-flavored protocols resolve through **derived stats** (`PlayerMaxHealthPct`, `GrabDurationScale`, `HealPerFinish`, `FinishAoeRadius`) consumed by one `ProtocolEffectsSystem`. The hero finish challenge extends `FinishResolver` (verbs still untouched — it observes the same hit events).

**Tech Stack:** unchanged — Unity 6000.3.5f2 (built-in RP), VContainer, R3, Entities 1.4.4, EditMode NUnit, uGUI.

**Spec:** `docs/superpowers/specs/2026-07-04-neon-engine-base-design.md` §5.3, §7 M2
**Design input:** `docs/rgd/protocol-stack-v0.1.md` (v0.3 — Hard Split: protocols are level-up-draft ONLY; the Neon Charge shop is M3+ Specials territory; §8.4 gives the exact per-protocol values used here)
**Prior state:** M1 gate signed off (`plan2` doc §"M1 gate record") — 48/48 tests, ~197 FPS @ ChaffCap 150.
**Branch:** create `claude/neon-m2-growth` off `master`.

---

## Decisions locked (Sebastien, 2026-07-04)

| Fork | Decision |
|---|---|
| **P1 — M2 protocol set** | **Buildable 8** from the catalog (exact §8.4 values): *Wide Sweep*, *Overclocked Coil* (Auto-Gear) · *Afterburner*, *Executioner's Cadence*, gated *Redline Governor* (Momentum ×3) · *Iron Grip* (Brawler) · *Concussive Finish* (Execution) · *Vampiric Cadence* (Defense). Satisfies spec §7 (6–8, ≥1 Brawler, ≥1 Momentum). *Redline Governor* (`Requires: Afterburner`) proves hidden-tree gating end-to-end. The doc's own MVP-cut members needing unbuilt mechanics (Split Fire → projectiles, Human Wrecking Ball/Wrecking Finish → throw-clip, Rapid Reboot → objective) defer to M3+. |
| **P2 — hero finish challenge** | **2-input sequence (PUNCH→KICK), escalating to 3 (PUNCH→KICK→PUNCH) at Hot/Overdrive**, per-input windows tightening with tier. Completing = instant finish + Momentum; target dying mid-sequence = plain kill, **no** Momentum (v0.4: payout on the *completed* challenge). Chaff stay single-verb (locked, R7). |

## M1 gate flags this plan pays down (Task 1)

1. **Hero-tier chip can kill** — floor hero chips at 1 HP (mirror the chaff `IsChip` floor). This also makes the hero challenge coherent: chip softens to gold, only verbs kill.
2. **Whiff counter anomaly** — a crowd-connecting punch also logged +1 whiff; fix = only report a whiff if the verb's hitbox was ever active during the state.
3. **HP tuning** — wave enemies die to 2 chips (10 HP) and the player dies to a stiff breeze (10 HP): retune wave enemies to 40, player to 100 (also makes the §8.1 "HP floor 50" guardrail meaningful for *Overclocked Coil*).
4. **Momentum decay aggressive** — not retuned here; instead decay becomes a **stat** (Task 4), so *Afterburner* addresses it in-fiction and the knob is live for tuning.

(Chaff blobbing is a Layer-1 feel item → M4 polish, out of scope.)

## Landed-code facts this plan builds on (verified 2026-07-04, post-M1)

- Test asmdef already references `VContainer` + `Unity.Collections` (M1 execution deviation) — no asmdef edits needed this plan.
- `AutoEngageSystem` uses the **chips-owed accumulator** (`dt × rate`), not `1/rate` intervals.
- Settings assets do **NOT** auto-create in Play mode — they must be created in the editor (`GetOrCreateSettingsAsset()`); Task 2 adds a permanent `Neon/Settings` menu utility so this stops being a trap.
- `FinishResolver` defers its stagger via `Tick` (order 25) and exposes `HandleDamage`/`HandleWhiff` publicly for tests — the challenge integrates into exactly these seams.
- `grabDuration` is read live from the player's `UnitSettings` by both `PlayerGrabEnemy` and `EnemyGrabbed` → *Iron Grip* works as a derived-stat rescale of that field.
- The bridge currently **clears** sim death events unread — Task 5 turns them into `ChaffDied` (kill XP).

---

## Working agreements (read once, apply to every task)

1. **Unity import + zero console errors before every test/play step** (refresh via editor focus or `mcp__unityMCP__refresh_unity`).
2. **EditMode tests:** Test Runner → EditMode → Run All (or `mcp__unityMCP__run_tests`, `mode: "EditMode"`), scoped to `BrainlessLabs.Neon.Tests.EditMode` (third-party DTT tests are pre-broken; ignore). The 48 M1 tests stay green except where a task explicitly rewrites them (Task 11 rewrites `FinishResolverTests`).
3. **Play-testing = Recipe 4**: boot with Post-Bootstrap Scene = `SceneDefinition_Level1`. No git/asset writes in Play mode. `BootstrapSettingsAsset` boot-target flips stay uncommitted.
4. **Commits include `.meta` files**; every commit body ends with `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
5. **Do not touch:** `WaveManager`, `04_Level2`/`05_Level3`, `ApplicationLifetimeScope`, `Assets/Addons/`, `Assets/_neon/Spikes/`, `Assets/_generated/`.
6. **Guardrails are law (protocol doc §8.1):** Momentum is the only multiplier — every protocol modifier in this plan is `Add`/`PctAdd`, never `Mult`. Hard caps stay enforced where they live (auto-rate ≤6/s and arc ≤180° in `AutoEngageSystem`; HP floor 50 in `ProtocolEffectsSystem`). The Finish-Ready threshold (≤25%) is untouched by everything here.
7. **Verbs stay behaviorally unchanged.** The challenge observes hits; it never alters verb states. The only verb-file edit in this plan is the Task 1 whiff-accuracy fix.
8. **`StatId` is append-only** (values may serialize into protocol assets — that's now real, not theoretical).
9. If any landed signature mismatches this plan at execution time, re-read the source file before adapting.

---

## File structure

**Created — protocols & growth (`BrainlessLabs.Neon`):**

| Path | Responsibility |
|---|---|
| `Assets/_neon/Scripts/Protocols/ProtocolFamily.cs` | `ProtocolFamily` + `ProtocolRarity` + `StatSheetTarget` enums |
| `Assets/_neon/Scripts/Protocols/ProtocolDefinitionAsset.cs` | The data-driven upgrade: modifiers bundle + prerequisite + stacking |
| `Assets/_neon/Scripts/Protocols/IProtocolService.cs` | Catalog, stacks, gating, draft roll, acquire |
| `Assets/_neon/Scripts/Protocols/ProtocolService.cs` | Implementation (rarity-weighted roll, per-copy modifiers) |
| `Assets/_neon/Scripts/Protocols/ProtocolEffectsSystem.cs` | Derived-stat consumer: max-HP, grab duration, heal-on-finish, finish AoE |
| `Assets/_neon/Scripts/Growth/GrowthSettings.cs` | Economy/progression/challenge tuning (`ISettings`) |
| `Assets/_neon/Scripts/Growth/GrowthSettingsAsset.cs` | `BaseSettingsAsset` wrapper |
| `Assets/_neon/Scripts/Growth/GrowthConfig.cs` | Asset-free snapshot (testability seam) |
| `Assets/_neon/Scripts/Growth/IEconomySystem.cs` | Three ledgers (XP / Neon Charge / Overcharge) |
| `Assets/_neon/Scripts/Growth/EconomySystem.cs` | Gains ×`Run.GainMultiplier`, fractional-remainder accurate |
| `Assets/_neon/Scripts/Growth/IProgressionSystem.cs` | Level, pending choice, `Choose(index)` |
| `Assets/_neon/Scripts/Growth/ProgressionSystem.cs` | ⌈10·N^1.35⌉ curve → banked level-ups → slow-mo draft |
| `Assets/_neon/Scripts/Engagement/IFinishChallenge.cs` | Challenge contract |
| `Assets/_neon/Scripts/Engagement/SequenceFinishChallenge.cs` | Pure verb-sequence challenge (windows, resets) |
| `Assets/_neon/Scripts/Editor/SettingsAssetCreator.cs` | `Neon/Settings/Create All Settings Assets` menu (kills the auto-create trap) |
| `Assets/_neon/Scripts/UI/UIHUDXpBar.cs` | XP fill + level (consumes `XpProgressChanged`) |
| `Assets/_neon/Scripts/UI/UIHUDOverchargeMeter.cs` | Overcharge fill (consumes `OverchargeChanged`) |
| `Assets/_neon/Scripts/UI/UILevelUpPicker.cs` | 1-of-3 modal (consumes `LevelUpChoicesReady`, commands `IProgressionSystem.Choose`) |
| `Assets/_neon/Protocols/*.asset` ×8 | The authored protocol assets (editor work, Task 7) |

**Created — tests:** `MomentumKnobTests.cs`, `EconomySystemTests.cs`, `ProtocolServiceTests.cs`, `ProtocolEffectsSystemTests.cs`, `ProgressionSystemTests.cs`, `SequenceFinishChallengeTests.cs` (+ `FinishResolverTests.cs` rewritten).

**Modified:**

| Path | Change |
|---|---|
| `Assets/_neon/Scripts/Engagement/AutoEngageSystem.cs` | Hero-chip 1 HP floor (Task 1) |
| `Assets/_neon/Scripts/Units/PlayerStates/PlayerAttack.cs` | Whiff requires hitbox-was-active (Task 1) |
| `Assets/_neon/Scripts/Stats/StatId.cs` | Append Momentum-knob + growth-knob ids |
| `Assets/_neon/Scripts/Signals/GameplayEvents.cs` | Append M2 signal structs |
| `Assets/_neon/Scripts/Clock/GameplayClock.cs` | Clock owns `Time.timeScale` (spec §4.1) |
| `Assets/_neon/Scripts/Engagement/MomentumSystem.cs` | Decay / below-Hot bonus / Overdrive multiplier become stats |
| `Assets/_neon/Scripts/Swarm/ISwarmBridge.cs` + `NullSwarmBridge.cs` + `SwarmBridge.cs` | `ChaffDied` publish on drain + `ApplyAreaDamage` |
| `Assets/_neon/Tests/EditMode/Fakes.cs` | `FakeSwarmBridge.ApplyAreaDamage` recorder + `FakeMomentumSystem` |
| `Assets/_neon/Scripts/Engagement/FinishResolver.cs` | Hero finish = tiered sequence challenge |
| `Assets/_neon/Scripts/UI/UIHUDFinishPrompt.cs` | Challenge verb + progress override |
| `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs` | Register Economy, ProtocolService, Progression |
| `Assets/_neon/Scripts/Level/Level.cs` | Register `GrowthConfig` + `ProtocolEffectsSystem`; `FinishResolver` gains deps |
| Wave-enemy + player `UnitDefinitionAsset`s, `03_Level1` canvas | HP retune (Task 1), HUD wiring (Task 10) |

---

### Task 1: Branch + M1 tuning debts

**Files:**
- Modify: `Assets/_neon/Scripts/Engagement/AutoEngageSystem.cs`
- Modify: `Assets/_neon/Scripts/Units/PlayerStates/PlayerAttack.cs`
- Editor: wave-enemy + player `UnitDefinitionAsset` HP values

- [ ] **Step 1: Create the branch**

```bash
git -C "G:/Brainless Labs/neon-responder" checkout master
git checkout -b claude/neon-m2-growth
```

- [ ] **Step 2: Floor hero-tier chips at 1 HP**

In `AutoEngageSystem.FireChip`, change:

```csharp
            else
            {
                heroTarget.GetComponent<HealthSystem>()?.SubstractHealth(damage);
            }
```

to:

```csharp
            else
            {
                // M1 gate flag: chip pushes hero-tier toward Finish-Ready but never
                // kills — mirrors the chaff-side IsChip floor. Verbs do the killing.
                var heroHealth = heroTarget.GetComponent<HealthSystem>();
                if (heroHealth != null)
                {
                    int applied = Mathf.Min(damage, heroHealth.currentHp - 1);
                    if (applied > 0) heroHealth.SubstractHealth(applied);
                }
            }
```

- [ ] **Step 3: Whiff requires an active hitbox (M1 anomaly fix)**

In `Assets/_neon/Scripts/Units/PlayerStates/PlayerAttack.cs`, add a tracking field — change:

```csharp
        private bool damageDealt; //true if the attack has hit something
```

to:

```csharp
        private bool damageDealt; //true if the attack has hit something
        private bool hitboxWasActive; //true if this attack's hitbox ever activated (whiff-report guard)
```

In `Update()`, change:

```csharp
            //check for hit until damage was dealt
            if(!damageDealt) damageDealt = unit.CheckForHit(attackData);
```

to:

```csharp
            //check for hit until damage was dealt
            if(!hitboxWasActive && unit.HitBoxActive()) hitboxWasActive = true;
            if(!damageDealt) damageDealt = unit.CheckForHit(attackData);
```

In `Exit()`, change:

```csharp
            //whiff-cost seam (spec §5.1): a completed punch/kick that hit nothing.
            //Grab whiffs are exempt (v0.4) — grabs never enter this state.
            if(!damageDealt && attackData != null) unit.ReportVerbWhiff(attackData.attackType);
```

to:

```csharp
            //whiff-cost seam (spec §5.1): a completed punch/kick that hit nothing.
            //Grab whiffs are exempt (v0.4) — grabs never enter this state.
            //hitboxWasActive guard (M1 gate anomaly): a state that never activated its
            //hitbox (interrupted first-frame exits) is not a whiff.
            if(!damageDealt && hitboxWasActive && attackData != null) unit.ReportVerbWhiff(attackData.attackType);
```

- [ ] **Step 4: HP retune (editor, not in Play mode)**

1. Select the `Level` object in `03_Level1` → open its `LevelConfigurationAsset` (`_configuration`).
2. `DefaultPlayerDefinition` asset → set **Max Health = 100**.
3. Every `UnitDefinition` referenced in `Waves → Entries` → set **Max Health = 40**.
4. Note the asset names touched (for the commit).

- [ ] **Step 5: Compile, tests, runtime verify (Recipe 4)**

Refresh Unity: zero errors, 48/48 tests PASS (no test touches these paths). Boot into Level1:
- Wave enemies chip down to **1 HP and hold** (gold, alive) — auto-engage no longer executes heroes.
- Punch pure empty air → whiff fires (stagger + COOL). Punch into the crowd → finishes, **no** whiff.
- Player survives more than a few hits (100 HP). Exit Play mode.

- [ ] **Step 6: Commit**

```bash
git add "Assets/_neon/Scripts/Engagement/AutoEngageSystem.cs" "Assets/_neon/Scripts/Units/PlayerStates/PlayerAttack.cs"
git add -A "Assets/_neon"
git commit -m "fix: hero-chip 1HP floor + hitbox-gated whiff + HP retune (M1 gate flags)"
```

---

### Task 2: Data layer — StatIds, signals, protocol data types, GrowthSettings

Pure data + one editor utility. Compile-and-commit.

**Files:**
- Modify: `Assets/_neon/Scripts/Stats/StatId.cs`
- Modify: `Assets/_neon/Scripts/Signals/GameplayEvents.cs`
- Create: `Assets/_neon/Scripts/Protocols/ProtocolFamily.cs`
- Create: `Assets/_neon/Scripts/Protocols/ProtocolDefinitionAsset.cs`
- Create: `Assets/_neon/Scripts/Growth/GrowthSettings.cs`
- Create: `Assets/_neon/Scripts/Growth/GrowthSettingsAsset.cs`
- Create: `Assets/_neon/Scripts/Growth/GrowthConfig.cs`
- Create: `Assets/_neon/Scripts/Editor/SettingsAssetCreator.cs`

- [ ] **Step 1: Append the new StatIds**

In `Assets/_neon/Scripts/Stats/StatId.cs`, append inside the enum after `GainMultiplier = 101,`:

```csharp

        // Momentum knobs (bases seeded by MomentumSystem; Protocols modify via modifiers)
        MomentumDecaySeconds = 200,
        MomentumBonusStepsBelowHot = 201,
        OverdriveMultiplier = 202,

        // Growth-layer derived knobs (bases seeded + consumed by ProtocolEffectsSystem)
        PlayerMaxHealthPct = 300,
        GrabDurationScale = 301,
        FinishAoeRadius = 302,
        HealPerFinish = 303,
```

- [ ] **Step 2: Append the M2 signals**

Append to `Assets/_neon/Scripts/Signals/GameplayEvents.cs`, inside the namespace after `VerbWhiffed`:

```csharp

    /// <summary>A chaff agent died (any cause — a finish also emits its death). Kill XP hangs off this.</summary>
    public readonly struct ChaffDied
    {
        public readonly Vector2 Position;

        public ChaffDied(Vector2 position)
        {
            Position = position;
        }
    }

    /// <summary>Raw XP grant from the economy (already Momentum-multiplied).</summary>
    public readonly struct XpGained
    {
        public readonly int Amount;
        public readonly int TotalXp;

        public XpGained(int amount, int totalXp)
        {
            Amount = amount;
            TotalXp = totalXp;
        }
    }

    /// <summary>Progression's view for the HUD bar: position within the current level.</summary>
    public readonly struct XpProgressChanged
    {
        public readonly int Level;
        public readonly int XpIntoLevel;
        public readonly int XpForNextLevel;

        public XpProgressChanged(int level, int xpIntoLevel, int xpForNextLevel)
        {
            Level = level;
            XpIntoLevel = xpIntoLevel;
            XpForNextLevel = xpForNextLevel;
        }
    }

    public readonly struct PlayerLevelChanged
    {
        public readonly int Level;

        public PlayerLevelChanged(int level)
        {
            Level = level;
        }
    }

    public readonly struct NeonChargeChanged
    {
        public readonly int Total;

        public NeonChargeChanged(int total)
        {
            Total = total;
        }
    }

    public readonly struct OverchargeChanged
    {
        public readonly int Value;
        public readonly int Cap;

        public OverchargeChanged(int value, int cap)
        {
            Value = value;
            Cap = cap;
        }
    }

    /// <summary>Level-up: pick 1 of 3 (slow-mo held until Choose).</summary>
    public readonly struct LevelUpChoicesReady
    {
        public readonly int Level;
        public readonly ProtocolDefinitionAsset[] Choices;

        public LevelUpChoicesReady(int level, ProtocolDefinitionAsset[] choices)
        {
            Level = level;
            Choices = choices;
        }
    }

    public readonly struct ProtocolAcquired
    {
        public readonly ProtocolDefinitionAsset Protocol;
        public readonly int StackCount;

        public ProtocolAcquired(ProtocolDefinitionAsset protocol, int stackCount)
        {
            Protocol = protocol;
            StackCount = stackCount;
        }
    }

    /// <summary>Hero-tier finish-challenge state for the HUD prompt (chaff stay single-verb).</summary>
    public readonly struct FinishChallengeChanged
    {
        public readonly bool Active;
        public readonly Vector2 TargetPosition;
        public readonly ATTACKTYPE ExpectedVerb;
        public readonly int Progress;
        public readonly int Total;

        public FinishChallengeChanged(bool active, Vector2 targetPosition, ATTACKTYPE expectedVerb, int progress, int total)
        {
            Active = active;
            TargetPosition = targetPosition;
            ExpectedVerb = expectedVerb;
            Progress = progress;
            Total = total;
        }
    }
```

- [ ] **Step 3: Protocol enums + asset type**

`Assets/_neon/Scripts/Protocols/ProtocolFamily.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>The eight GDD families (protocol doc §1).</summary>
    public enum ProtocolFamily
    {
        AutoGear = 0,
        Momentum = 1,
        Execution = 2,
        Brawler = 3,
        Scavenger = 4,
        Specials = 5,
        Defense = 6,
        Objective = 7
    }

    /// <summary>Rarity drives draft weight (protocol doc §2/§8.2).</summary>
    public enum ProtocolRarity
    {
        Stock = 0,
        Tuned = 1,
        Prototype = 2,
        Blacksite = 3
    }

    /// <summary>Which M0 stat sheet a protocol modifier lands on.</summary>
    public enum StatSheetTarget
    {
        Player = 0,
        Run = 1
    }
}
```

`Assets/_neon/Scripts/Protocols/ProtocolDefinitionAsset.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// One stat modifier a protocol applies (guardrail §8.1: Add/PctAdd only —
    /// Momentum is the game's single multiplier; never author Mult here).
    /// </summary>
    [System.Serializable]
    public class ProtocolStatModifier
    {
        public StatSheetTarget Sheet = StatSheetTarget.Player;
        public StatId Stat;
        public StatOp Op = StatOp.Add;
        public float Value;
    }

    /// <summary>
    /// A data-driven upgrade (spec §5.3): a bundle of stat modifiers + stacking +
    /// an optional prerequisite (hidden-tree gating, protocol doc §3). Applying one
    /// is IProtocolService.Acquire — adding a Protocol to the game is authoring an
    /// asset, not writing system code.
    /// </summary>
    [CreateAssetMenu(fileName = "Protocol", menuName = "Neon/Protocols/Protocol Definition")]
    public class ProtocolDefinitionAsset : ScriptableObject
    {
        [Tooltip("Card title, e.g. \"Overclocked Coil\".")]
        public string DisplayName;

        public ProtocolFamily Family;
        public ProtocolRarity Rarity;

        [TextArea]
        [Tooltip("Card body — what the player reads on the level-up pick.")]
        public string Description;

        [Tooltip("1 = unique (behavior protocols). >1 = flat-bump stackable (doc §8.1 rule 6).")]
        public int MaxStacks = 1;

        [Tooltip("Hidden-tree gate (doc §3, Requires-X): not offered until this protocol is in the stack. N-of-family / pair gates land with the first Blacksite (M3+).")]
        public ProtocolDefinitionAsset Prerequisite;

        [Tooltip("Modifiers applied by the FIRST copy.")]
        public List<ProtocolStatModifier> FirstCopyModifiers = new();

        [Tooltip("Modifiers applied by each ADDITIONAL copy (lets stacks diminish per doc §8.4, e.g. Vampiric +2 then +1).")]
        public List<ProtocolStatModifier> AdditionalCopyModifiers = new();
    }
}
```

- [ ] **Step 4: Growth settings + config**

`Assets/_neon/Scripts/Growth/GrowthSettings.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// M2 growth tuning. Economy/progression numbers from protocol doc §8.3;
    /// challenge shape per decision P2. All are playtest starting knobs.
    /// </summary>
    [Serializable]
    public class GrowthSettings : ISettings
    {
        [Header("Economy (doc §8.3 — gains are ×Run.GainMultiplier)")]
        [SerializeField] private int _xpPerKill = 1;
        [SerializeField] private int _chargePerFinish = 2;
        [SerializeField] private int _overchargePerFinish = 8;
        [SerializeField] private int _overchargeCap = 100;

        [Header("Progression (XP cost to clear level N = ceil(base × N^exponent))")]
        [SerializeField] private float _xpCostBase = 10f;
        [SerializeField] private float _xpCostExponent = 1.35f;
        [SerializeField, Range(0.01f, 1f)] private float _levelUpSlowMoScale = 0.1f;

        [Header("Protocol catalog (the draft pool)")]
        [SerializeField] private List<ProtocolDefinitionAsset> _protocolCatalog = new();

        [Header("Hero finish challenge (P2 — chaff stay single-verb)")]
        [SerializeField] private ATTACKTYPE[] _challengeSequenceBase = { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK };
        [SerializeField] private ATTACKTYPE[] _challengeSequenceHot = { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK, ATTACKTYPE.PUNCH };
        [SerializeField] private float _challengeInputWindowSeconds = 0.9f;
        [SerializeField] private float _challengeWindowTightenPerTier = 0.1f;

        [Header("Protocol effect hooks")]
        [SerializeField] private int _finishAoeDamage = 6;

        public int XpPerKill => _xpPerKill;
        public int ChargePerFinish => _chargePerFinish;
        public int OverchargePerFinish => _overchargePerFinish;
        public int OverchargeCap => _overchargeCap;
        public float XpCostBase => _xpCostBase;
        public float XpCostExponent => _xpCostExponent;
        public float LevelUpSlowMoScale => _levelUpSlowMoScale;
        public List<ProtocolDefinitionAsset> ProtocolCatalog => _protocolCatalog;
        public ATTACKTYPE[] ChallengeSequenceBase => _challengeSequenceBase;
        public ATTACKTYPE[] ChallengeSequenceHot => _challengeSequenceHot;
        public float ChallengeInputWindowSeconds => _challengeInputWindowSeconds;
        public float ChallengeWindowTightenPerTier => _challengeWindowTightenPerTier;
        public int FinishAoeDamage => _finishAoeDamage;

#if UNITY_EDITOR
        public void Editor_OnGUI(UnityEngine.Object target)
        {
            var serializedObject = new UnityEditor.SerializedObject(target);
            serializedObject.UpdateIfRequiredOrScript();
            var settingsProperty = serializedObject.FindProperty("_settings");
            UnityEditor.EditorGUILayout.PropertyField(settingsProperty, new GUIContent("Growth Settings"), includeChildren: true);
            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}
```

`Assets/_neon/Scripts/Growth/GrowthSettingsAsset.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    public class GrowthSettingsAsset : BaseSettingsAsset<GrowthSettingsAsset, GrowthSettings> { }
}
```

`Assets/_neon/Scripts/Growth/GrowthConfig.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>Asset-free snapshot of GrowthSettings (EditMode-testable systems).</summary>
    public readonly struct GrowthConfig
    {
        public readonly int XpPerKill;
        public readonly int ChargePerFinish;
        public readonly int OverchargePerFinish;
        public readonly int OverchargeCap;
        public readonly float XpCostBase;
        public readonly float XpCostExponent;
        public readonly float LevelUpSlowMoScale;
        public readonly ATTACKTYPE[] ChallengeSequenceBase;
        public readonly ATTACKTYPE[] ChallengeSequenceHot;
        public readonly float ChallengeInputWindowSeconds;
        public readonly float ChallengeWindowTightenPerTier;
        public readonly int FinishAoeDamage;

        public GrowthConfig(int xpPerKill, int chargePerFinish, int overchargePerFinish, int overchargeCap,
            float xpCostBase, float xpCostExponent, float levelUpSlowMoScale,
            ATTACKTYPE[] challengeSequenceBase, ATTACKTYPE[] challengeSequenceHot,
            float challengeInputWindowSeconds, float challengeWindowTightenPerTier, int finishAoeDamage)
        {
            XpPerKill = xpPerKill;
            ChargePerFinish = chargePerFinish;
            OverchargePerFinish = overchargePerFinish;
            OverchargeCap = overchargeCap;
            XpCostBase = xpCostBase;
            XpCostExponent = xpCostExponent;
            LevelUpSlowMoScale = levelUpSlowMoScale;
            ChallengeSequenceBase = challengeSequenceBase;
            ChallengeSequenceHot = challengeSequenceHot;
            ChallengeInputWindowSeconds = challengeInputWindowSeconds;
            ChallengeWindowTightenPerTier = challengeWindowTightenPerTier;
            FinishAoeDamage = finishAoeDamage;
        }

        public static GrowthConfig FromSettings()
        {
            var s = GrowthSettingsAsset.InstanceAsset.Settings;
            return new GrowthConfig(s.XpPerKill, s.ChargePerFinish, s.OverchargePerFinish, s.OverchargeCap,
                s.XpCostBase, s.XpCostExponent, s.LevelUpSlowMoScale,
                s.ChallengeSequenceBase, s.ChallengeSequenceHot,
                s.ChallengeInputWindowSeconds, s.ChallengeWindowTightenPerTier, s.FinishAoeDamage);
        }
    }
}
```

- [ ] **Step 5: The settings-asset creator (kills the M1 auto-create trap)**

`Assets/_neon/Scripts/Editor/SettingsAssetCreator.cs`:

```csharp
using UnityEditor;
using UnityEngine;

namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// Settings assets only auto-create via the editor-side GetOrCreateSettingsAsset
    /// (M1 execution deviation 2: Play mode just Resources.Loads and crashes if the
    /// asset is missing). One menu item creates every settings singleton the game needs.
    /// </summary>
    public static class SettingsAssetCreator
    {
        [MenuItem("Neon/Settings/Create All Settings Assets")]
        public static void CreateAll()
        {
            BootstrapSettingsAsset.GetOrCreateSettingsAsset();
            AudioSettingsAsset.GetOrCreateSettingsAsset();
            ScenesSettingsAsset.GetOrCreateSettingsAsset();
            EngagementSettingsAsset.GetOrCreateSettingsAsset();
            GrowthSettingsAsset.GetOrCreateSettingsAsset();
            AssetDatabase.SaveAssets();
            Debug.Log("[Neon] All settings assets present under Assets/Resources/Settings/.");
        }
    }
}
```

(If any `GetOrCreateSettingsAsset` name mismatches the `BaseSettingsAsset` API at execution time, read `Assets/_neon/Scripts/Settings/BaseSettingsAsset.cs` and use its actual editor-creation member — working agreement 9.)

- [ ] **Step 6: Compile + create the asset + commit**

1. Refresh Unity: zero errors, 48 tests PASS.
2. Run **Neon → Settings → Create All Settings Assets** — confirm `Assets/Resources/Settings/GrowthSettingsAsset.asset` appears.
3. Commit:

```bash
git add "Assets/_neon/Scripts/Stats/StatId.cs" "Assets/_neon/Scripts/Signals/GameplayEvents.cs" "Assets/_neon/Scripts/Protocols" "Assets/_neon/Scripts/Protocols.meta" "Assets/_neon/Scripts/Growth" "Assets/_neon/Scripts/Growth.meta" "Assets/_neon/Scripts/Editor/SettingsAssetCreator.cs" "Assets/_neon/Scripts/Editor/SettingsAssetCreator.cs.meta" "Assets/Resources/Settings/GrowthSettingsAsset.asset" "Assets/Resources/Settings/GrowthSettingsAsset.asset.meta"
git commit -m "feat: M2 data layer - protocol assets, growth settings, signals, stat ids"
```

---

### Task 3: `GameplayClock` owns `Time.timeScale`

Spec §4.1: the clock is the *sole owner* of gameplay time, "scaled by hitstop / level-up slow-mo / pause". M1's clock scale only slowed clock-ticked systems — the MonoBehaviour combat world and the ECS sim run on Unity time and ignored it. This task makes clock scale sources drive `Time.timeScale`, so the level-up slow-mo (Task 9) slows *everything* through one seam.

**Files:**
- Modify: `Assets/_neon/Scripts/Clock/GameplayClock.cs`

- [ ] **Step 1: Apply the effective scale to engine time**

In `GameplayClock.cs`, change:

```csharp
        void ITickable.Tick()
        {
            if (!_loggedFirstTick)
            {
                UnityEngine.Debug.Log("[Gameplay] GameplayClock ticking.");
                _loggedFirstTick = true;
            }
            Advance(UnityEngine.Time.deltaTime);
        }
```

to:

```csharp
        private float _lastAppliedTimeScale = 1f;

        void ITickable.Tick()
        {
            if (!_loggedFirstTick)
            {
                UnityEngine.Debug.Log("[Gameplay] GameplayClock ticking.");
                _loggedFirstTick = true;
            }

            // Spec §4.1: the clock owns engine time. Scale sources (hitstop, level-up
            // slow-mo, pause) drive Time.timeScale so the WHOLE world slows — Mono
            // combat, animators, physics, and the ECS sim included. Written only on
            // change so Level's legacy last-kill SlowMotionRoutine (direct
            // Time.timeScale writes) keeps working between clock changes; that
            // routine migrates onto a clock source with M3's RunService.
            float effectiveScale = EffectiveScale;
            if (!UnityEngine.Mathf.Approximately(effectiveScale, _lastAppliedTimeScale))
            {
                UnityEngine.Time.timeScale = effectiveScale;
                _lastAppliedTimeScale = effectiveScale;
            }

            // Unscaled delta × EffectiveScale (inside Advance) — using the scaled
            // deltaTime here would double-apply the scale now that we set timeScale.
            Advance(UnityEngine.Time.unscaledDeltaTime);
        }
```

- [ ] **Step 2: Compile, tests, runtime sanity**

Refresh Unity: zero errors, 48 tests PASS (the tests drive `Advance` directly — semantics unchanged). Boot into Level1, play 30s: identical behavior to before (no scale source is active yet; `Time.timeScale` stays 1). Exit Play mode.

- [ ] **Step 3: Commit**

```bash
git add "Assets/_neon/Scripts/Clock/GameplayClock.cs"
git commit -m "feat: GameplayClock owns Time.timeScale via scale sources (spec 4.1)"
```

---

### Task 4: Momentum knobs become stats (test-first)

*Afterburner* (decay 2.5→4.2s), *Executioner's Cadence* (double steps below Hot), and *Redline Governor* (Overdrive ×2.5→×3.0) all tune Momentum — so decay, below-Hot bonus steps, and the Overdrive multiplier move onto the **Player stat sheet**, seeded from config. Protocols then modify them like any other stat. Also fixes the M1 "decay aggressive" flag by making the knob live.

**Files:**
- Modify: `Assets/_neon/Scripts/Engagement/MomentumSystem.cs`
- Test: `Assets/_neon/Tests/EditMode/MomentumKnobTests.cs`

- [ ] **Step 1: Write the failing tests**

`Assets/_neon/Tests/EditMode/MomentumKnobTests.cs`:

```csharp
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class MomentumKnobTests
    {
        private GameplayClock _clock;
        private GameplaySignals _signals;
        private StatSystem _stats;
        private MomentumSystem _momentum;
        private ModifierSource _source;

        private static MomentumConfig TestConfig => new(stepsPerTier: 3, decaySeconds: 2.5f,
            tierMultipliers: new[] { 1f, 1.3f, 1.7f, 2.5f });

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _signals = new GameplaySignals();
            _stats = new StatSystem();
            _momentum = new MomentumSystem(_clock, _signals, _stats, TestConfig);
            _source = ModifierSource.Create("test-protocol");
        }

        [TearDown]
        public void TearDown()
        {
            _momentum.Dispose();
            _signals.Dispose();
        }

        private void Finish() => _signals.Publish(new EnemyFinished(Vector2.zero, wasChaff: true));

        [Test]
        public void Ctor_SeedsKnobStats()
        {
            Assert.AreEqual(2.5f, _stats.Player.GetValue(StatId.MomentumDecaySeconds), 0.0001f);
            Assert.AreEqual(0f, _stats.Player.GetValue(StatId.MomentumBonusStepsBelowHot), 0.0001f);
            Assert.AreEqual(2.5f, _stats.Player.GetValue(StatId.OverdriveMultiplier), 0.0001f);
        }

        [Test]
        public void DecayStat_ExtendsTheIdleWindow()
        {
            // Afterburner: 2.5s → 4.2s
            _stats.Player.AddModifier(StatId.MomentumDecaySeconds, StatOp.Add, 1.7f, _source);
            Finish(); Finish(); Finish(); // Warm

            _clock.Advance(3f);           // would have decayed at 2.5s
            Assert.AreEqual(MomentumTier.Warm, _momentum.Tier);

            _clock.Advance(1.5f);         // 4.5s total idle > 4.2s
            Assert.AreEqual(MomentumTier.Cool, _momentum.Tier);
        }

        [Test]
        public void BonusStepsBelowHot_DoubleMomentumWhileRecovering()
        {
            // Executioner's Cadence: +1 bonus step below Hot → 2 finishes reach Warm.
            _stats.Player.AddModifier(StatId.MomentumBonusStepsBelowHot, StatOp.Add, 1f, _source);

            Finish(); Finish();           // 2 × 2 steps = 4 → Warm

            Assert.AreEqual(MomentumTier.Warm, _momentum.Tier);
        }

        [Test]
        public void BonusSteps_DoNotApplyAtHotOrAbove()
        {
            _stats.Player.AddModifier(StatId.MomentumBonusStepsBelowHot, StatOp.Add, 1f, _source);

            Finish(); Finish(); Finish(); // 6 steps → Hot
            Assert.AreEqual(MomentumTier.Hot, _momentum.Tier);

            Finish(); Finish(); Finish(); // +1 each at Hot → 9 → Overdrive
            Assert.AreEqual(MomentumTier.Overdrive, _momentum.Tier);
        }

        [Test]
        public void OverdriveMultiplierStat_GovernsTheTopTier()
        {
            // Redline Governor: ×2.5 → ×3.0
            _stats.Player.AddModifier(StatId.OverdriveMultiplier, StatOp.Add, 0.5f, _source);

            for (int i = 0; i < 9; i++) Finish();

            Assert.AreEqual(MomentumTier.Overdrive, _momentum.Tier);
            Assert.AreEqual(3.0f, _stats.Player.GetValue(StatId.DamageMultiplier), 0.0001f);
        }

        [Test]
        public void OverdriveMultiplier_AcquiredWhileAtOverdrive_RefreshesOnProtocolAcquired()
        {
            for (int i = 0; i < 9; i++) Finish(); // Overdrive at ×2.5
            _stats.Player.AddModifier(StatId.OverdriveMultiplier, StatOp.Add, 0.5f, _source);

            var protocol = ScriptableObject.CreateInstance<ProtocolDefinitionAsset>();
            _signals.Publish(new ProtocolAcquired(protocol, 1));

            Assert.AreEqual(3.0f, _stats.Player.GetValue(StatId.DamageMultiplier), 0.0001f);
            Object.DestroyImmediate(protocol);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Refresh Unity, run EditMode tests. Expected: the 6 new `MomentumKnobTests` FAIL (`Ctor_SeedsKnobStats` etc. — the stats are never seeded); the existing 48 still PASS.

- [ ] **Step 3: Implement**

In `Assets/_neon/Scripts/Engagement/MomentumSystem.cs`:

**(a)** Add a third subscription field after `_whiffSubscription`:

```csharp
        private readonly IDisposable _whiffSubscription;
```

becomes:

```csharp
        private readonly IDisposable _whiffSubscription;
        private readonly IDisposable _protocolSubscription;
```

**(b)** In the constructor, change:

```csharp
            // Multiplier stats read as ×1 when nothing has touched them.
            _stats.Player.SetBase(StatId.DamageMultiplier, 1f);
            _stats.Run.SetBase(StatId.GainMultiplier, 1f);

            _finishSubscription = _signals.On<EnemyFinished>().Subscribe(_ => OnFinish());
            _whiffSubscription = _signals.On<VerbWhiffed>().Subscribe(_ => ResetToCool());
```

to:

```csharp
            // Multiplier stats read as ×1 when nothing has touched them.
            _stats.Player.SetBase(StatId.DamageMultiplier, 1f);
            _stats.Run.SetBase(StatId.GainMultiplier, 1f);

            // Momentum's own knobs live on the sheet so Protocols tune them
            // (Afterburner / Executioner's Cadence / Redline Governor).
            _stats.Player.SetBase(StatId.MomentumDecaySeconds, config.DecaySeconds);
            _stats.Player.SetBase(StatId.MomentumBonusStepsBelowHot, 0f);
            _stats.Player.SetBase(StatId.OverdriveMultiplier,
                config.TierMultipliers[(int)MomentumTier.Overdrive]);

            _finishSubscription = _signals.On<EnemyFinished>().Subscribe(_ => OnFinish());
            _whiffSubscription = _signals.On<VerbWhiffed>().Subscribe(_ => ResetToCool());
            // A protocol acquired while already at a tier must re-fold that tier's
            // multiplier (e.g. Redline Governor taken AT Overdrive).
            _protocolSubscription = _signals.On<ProtocolAcquired>().Subscribe(_ => ApplyTier(Tier, publish: false));
```

**(c)** In `Tick`, change:

```csharp
            _idleSeconds += deltaTime;
            if (_idleSeconds < _config.DecaySeconds) return;
```

to:

```csharp
            _idleSeconds += deltaTime;
            float decayWindow = Mathf.Max(0.5f, _stats.Player.GetValue(StatId.MomentumDecaySeconds));
            if (_idleSeconds < decayWindow) return;
```

**(d)** In `OnFinish`, change:

```csharp
            int maxSteps = _config.StepsPerTier * ((int)MomentumTier.Overdrive);
            _steps = Mathf.Min(_steps + 1, maxSteps);
```

to:

```csharp
            int maxSteps = _config.StepsPerTier * ((int)MomentumTier.Overdrive);
            int gained = 1;
            if (Tier < MomentumTier.Hot)
            {
                gained += Mathf.Max(0, Mathf.RoundToInt(_stats.Player.GetValue(StatId.MomentumBonusStepsBelowHot)));
            }
            _steps = Mathf.Min(_steps + gained, maxSteps);
```

**(e)** In `ApplyTier`, change:

```csharp
            float multiplier = _config.TierMultipliers[Mathf.Clamp((int)tier, 0, _config.TierMultipliers.Length - 1)];
```

to:

```csharp
            float multiplier = tier == MomentumTier.Overdrive
                ? Mathf.Max(1f, _stats.Player.GetValue(StatId.OverdriveMultiplier))
                : _config.TierMultipliers[Mathf.Clamp((int)tier, 0, _config.TierMultipliers.Length - 1)];
```

**(f)** In `Dispose`, change:

```csharp
            _finishSubscription?.Dispose();
            _whiffSubscription?.Dispose();
```

to:

```csharp
            _finishSubscription?.Dispose();
            _whiffSubscription?.Dispose();
            _protocolSubscription?.Dispose();
```

- [ ] **Step 4: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: **54/54 PASS** (48 + 6 knob tests; the original 8 `MomentumSystemTests` are unaffected — the seeded stats equal the config values they already assert against).

- [ ] **Step 5: Commit**

```bash
git add "Assets/_neon/Scripts/Engagement/MomentumSystem.cs" "Assets/_neon/Tests/EditMode/MomentumKnobTests.cs" "Assets/_neon/Tests/EditMode/MomentumKnobTests.cs.meta"
git commit -m "feat: Momentum decay/steps/Overdrive-multiplier become protocol-tunable stats (M2)"
```

---

### Task 5: `IEconomySystem` (test-first) + `ChaffDied` from the bridge

Three ledgers at three timescales (spec §5.3): **XP** (kills → level-ups), **Neon Charge** (finishes → M3 shop), **Overcharge** (finishes → M4 finisher). Every gain reads `Run.GainMultiplier` — the economy never knows what Momentum is. Kill XP needs death events for chaff, so the bridge's silent drain becomes a `ChaffDied` publish.

**Files:**
- Modify: `Assets/_neon/Scripts/Swarm/SwarmBridge.cs`
- Create: `Assets/_neon/Scripts/Growth/IEconomySystem.cs`
- Create: `Assets/_neon/Scripts/Growth/EconomySystem.cs`
- Modify: `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs`
- Test: `Assets/_neon/Tests/EditMode/EconomySystemTests.cs`

- [ ] **Step 1: Bridge publishes `ChaffDied` on drain**

In `SwarmBridge.Tick`, change:

```csharp
            // Drain sim events. M1 consumes nothing from chip-deaths (they are NOT
            // finishes — v0.4); feedback hooks arrive in M4.
            entityManager.GetBuffer<SwarmEventRecord>(_controlEntity).Clear();
```

to:

```csharp
            // Drain sim events → kill XP hangs off ChaffDied. A finish ALSO emits its
            // death here (finish rewards flow separately off EnemyFinished — Charge/
            // Overcharge; kills — XP; no double-granting because the reward types differ).
            var events = entityManager.GetBuffer<SwarmEventRecord>(_controlEntity);
            for (int i = 0; i < events.Length; i++)
            {
                if (events[i].Kind == SwarmEventRecord.KIND_CHAFF_DIED)
                {
                    _signals.Publish(new ChaffDied(new Vector2(events[i].Position.x, events[i].Position.y)));
                }
            }
            events.Clear();
```

- [ ] **Step 2: Write the failing tests**

`Assets/_neon/Tests/EditMode/EconomySystemTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class EconomySystemTests
    {
        private GameplaySignals _signals;
        private StatSystem _stats;
        private EconomySystem _economy;
        private readonly List<XpGained> _xpEvents = new();
        private System.IDisposable _xpSub;

        private static GrowthConfig TestConfig => new(
            xpPerKill: 1, chargePerFinish: 2, overchargePerFinish: 8, overchargeCap: 20,
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
            _xpEvents.Clear();
            _xpSub = _signals.On<XpGained>().Subscribe(e => _xpEvents.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            _xpSub?.Dispose();
            _economy.Dispose();
            _signals.Dispose();
        }

        [Test]
        public void ChaffDied_GrantsKillXp()
        {
            _signals.Publish(new ChaffDied(Vector2.zero));

            Assert.AreEqual(1, _economy.Xp);
            Assert.AreEqual(1, _xpEvents.Count);
            Assert.AreEqual(1, _xpEvents[0].TotalXp);
        }

        [Test]
        public void GainMultiplier_AccumulatesFractionally()
        {
            var momentum = ModifierSource.Create("momentum");
            _stats.Run.AddModifier(StatId.GainMultiplier, StatOp.Mult, 1.3f, momentum);

            for (int i = 0; i < 3; i++) _signals.Publish(new ChaffDied(Vector2.zero));
            Assert.AreEqual(3, _economy.Xp);  // 3 × 1.3 = 3.9 → 3 whole

            _signals.Publish(new ChaffDied(Vector2.zero));
            Assert.AreEqual(5, _economy.Xp);  // 5.2 → 5 (the 0.9 remainder paid out)
        }

        [Test]
        public void Finish_GrantsChargeAndOvercharge_NotXp()
        {
            _signals.Publish(new EnemyFinished(Vector2.zero, wasChaff: true));

            Assert.AreEqual(0, _economy.Xp);
            Assert.AreEqual(2, _economy.NeonCharge);
            Assert.AreEqual(8, _economy.Overcharge);
        }

        [Test]
        public void Overcharge_CapsAtConfig()
        {
            for (int i = 0; i < 5; i++) _signals.Publish(new EnemyFinished(Vector2.zero, wasChaff: true));

            Assert.AreEqual(20, _economy.Overcharge); // cap 20 in TestConfig
        }

        [Test]
        public void HeroDeath_GrantsKillXp()
        {
            var enemy = new GameObject("Enemy");
            enemy.tag = "Enemy";

            _economy.OnUnitDeath(enemy);

            Assert.AreEqual(1, _economy.Xp);
            Object.DestroyImmediate(enemy);
        }

        [Test]
        public void PlayerDeath_GrantsNothing()
        {
            var player = new GameObject("Player");
            player.tag = "Player";

            _economy.OnUnitDeath(player);

            Assert.AreEqual(0, _economy.Xp);
            Object.DestroyImmediate(player);
        }
    }
}
```

- [ ] **Step 3: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`EconomySystem` does not exist yet). Proceed.

- [ ] **Step 4: Implement**

`Assets/_neon/Scripts/Growth/IEconomySystem.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Three ledgers at three timescales (spec §5.3 / GDD §8). Gains are
    /// ×Run.GainMultiplier internally — the economy never knows what Momentum is.
    /// </summary>
    public interface IEconomySystem
    {
        /// <summary>In-run XP total (kills). Progression levels off XpGained.</summary>
        int Xp { get; }

        /// <summary>Between-encounter currency (finishes). Spent in the M3 Specials shop.</summary>
        int NeonCharge { get; }

        /// <summary>Moment-to-moment meter (finishes). Spent by the M4 Overcharge finisher.</summary>
        int Overcharge { get; }
    }
}
```

`Assets/_neon/Scripts/Growth/EconomySystem.cs`:

```csharp
using System;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    public sealed class EconomySystem : IEconomySystem, IDisposable
    {
        private readonly IGameplaySignals _signals;
        private readonly IStatSystem _stats;
        private readonly GrowthConfig _config;
        private readonly IDisposable _chaffDiedSubscription;
        private readonly IDisposable _finishSubscription;

        private float _xpFraction;
        private float _chargeFraction;
        private float _overchargeFraction;

        public int Xp { get; private set; }
        public int NeonCharge { get; private set; }
        public int Overcharge { get; private set; }

        public EconomySystem(IGameplaySignals signals, IStatSystem stats, GrowthConfig config)
        {
            _signals = signals;
            _stats = stats;
            _config = config;

            _chaffDiedSubscription = _signals.On<ChaffDied>().Subscribe(_ => GrantXp(_config.XpPerKill));
            _finishSubscription = _signals.On<EnemyFinished>().Subscribe(_ => OnFinish());
            HealthSystem.onUnitDeath += OnUnitDeath;
        }

        public void Dispose()
        {
            _chaffDiedSubscription?.Dispose();
            _finishSubscription?.Dispose();
            HealthSystem.onUnitDeath -= OnUnitDeath;
        }

        /// <summary>Public so EditMode tests can drive it (static event can't be raised externally).</summary>
        public void OnUnitDeath(GameObject unit)
        {
            if (unit == null || !unit.CompareTag("Enemy")) return;
            GrantXp(_config.XpPerKill);
        }

        private void OnFinish()
        {
            NeonCharge += GrantWhole(_config.ChargePerFinish, ref _chargeFraction);
            _signals.Publish(new NeonChargeChanged(NeonCharge));

            int overchargeGain = GrantWhole(_config.OverchargePerFinish, ref _overchargeFraction);
            Overcharge = Mathf.Min(Overcharge + overchargeGain, _config.OverchargeCap);
            _signals.Publish(new OverchargeChanged(Overcharge, _config.OverchargeCap));
        }

        private void GrantXp(int baseAmount)
        {
            int granted = GrantWhole(baseAmount, ref _xpFraction);
            if (granted == 0) return;

            Xp += granted;
            _signals.Publish(new XpGained(granted, Xp));
        }

        // Fractional remainders accumulate so ×1.3 on a 1-XP kill pays out fairly
        // over time instead of rounding away every gain.
        private int GrantWhole(int baseAmount, ref float fraction)
        {
            float multiplier = _stats.Run.GetValue(StatId.GainMultiplier);
            if (multiplier <= 0f) multiplier = 1f;

            fraction += baseAmount * multiplier;
            int whole = Mathf.FloorToInt(fraction);
            fraction -= whole;
            return whole;
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: **60/60 PASS** (54 + 6 economy).

- [ ] **Step 6: Register in `GameplayServicesState`**

Add to `RegisterTypes` after `RegisterNullSwarmBridge(builder);`:

```csharp
            RegisterEconomySystem(builder);
```

and the helper:

```csharp
        private static void RegisterEconomySystem(IContainerBuilder builder)
        {
            builder.Register<EconomySystem>(Lifetime.Singleton)
                .WithParameter(GrowthConfig.FromSettings())
                .As<IEconomySystem>();
            builder.RegisterBuildCallback(container => container.Resolve<IEconomySystem>());
        }
```

- [ ] **Step 7: Runtime check + commit**

Boot into Level1, punch a few gold chaff, exit. No errors; (XP is invisible until Task 10 — the EditMode suite is the real gate here).

```bash
git add "Assets/_neon/Scripts/Swarm/SwarmBridge.cs" "Assets/_neon/Scripts/Growth/IEconomySystem.cs" "Assets/_neon/Scripts/Growth/IEconomySystem.cs.meta" "Assets/_neon/Scripts/Growth/EconomySystem.cs" "Assets/_neon/Scripts/Growth/EconomySystem.cs.meta" "Assets/_neon/Tests/EditMode/EconomySystemTests.cs" "Assets/_neon/Tests/EditMode/EconomySystemTests.cs.meta" "Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs"
git commit -m "feat: EconomySystem - XP/Charge/Overcharge ledgers x GainMultiplier (M2)"
```

---

### Task 6: `IProtocolService` — draft, stacking, hidden-tree gating (test-first)

The architectural payoff of the stat spine: acquiring a protocol = applying its modifier bundle with a per-copy `ModifierSource`. Rarity-weighted 1-of-3 roll (doc §8.2 base weights; Signal scaling arrives in M3), no duplicates within a draft, `Requires:` gating keeps *Redline Governor* invisible until *Afterburner* is stacked.

**Files:**
- Create: `Assets/_neon/Scripts/Protocols/IProtocolService.cs`
- Create: `Assets/_neon/Scripts/Protocols/ProtocolService.cs`
- Modify: `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs`
- Test: `Assets/_neon/Tests/EditMode/ProtocolServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

`Assets/_neon/Tests/EditMode/ProtocolServiceTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class ProtocolServiceTests
    {
        private StatSystem _stats;
        private GameplaySignals _signals;
        private readonly List<ProtocolDefinitionAsset> _createdAssets = new();

        [SetUp]
        public void SetUp()
        {
            _stats = new StatSystem();
            _signals = new GameplaySignals();
        }

        [TearDown]
        public void TearDown()
        {
            _signals.Dispose();
            foreach (var asset in _createdAssets) Object.DestroyImmediate(asset);
            _createdAssets.Clear();
        }

        private ProtocolDefinitionAsset MakeProtocol(string name, ProtocolRarity rarity, int maxStacks,
            ProtocolDefinitionAsset prerequisite,
            (StatId stat, StatOp op, float value)[] firstCopy,
            (StatId stat, StatOp op, float value)[] additionalCopy = null)
        {
            var asset = ScriptableObject.CreateInstance<ProtocolDefinitionAsset>();
            asset.name = name;
            asset.DisplayName = name;
            asset.Rarity = rarity;
            asset.MaxStacks = maxStacks;
            asset.Prerequisite = prerequisite;
            foreach (var (stat, op, value) in firstCopy)
                asset.FirstCopyModifiers.Add(new ProtocolStatModifier { Sheet = StatSheetTarget.Player, Stat = stat, Op = op, Value = value });
            foreach (var (stat, op, value) in additionalCopy ?? new (StatId, StatOp, float)[0])
                asset.AdditionalCopyModifiers.Add(new ProtocolStatModifier { Sheet = StatSheetTarget.Player, Stat = stat, Op = op, Value = value });
            _createdAssets.Add(asset);
            return asset;
        }

        private ProtocolService MakeService(params ProtocolDefinitionAsset[] catalog)
        {
            return new ProtocolService(_stats, _signals, catalog, randomSeed: 12345);
        }

        [Test]
        public void Acquire_AppliesFirstCopyModifiers()
        {
            _stats.Player.SetBase(StatId.AutoEngageArcDegrees, 120f);
            var wideSweep = MakeProtocol("WideSweep", ProtocolRarity.Stock, 2, null,
                new[] { (StatId.AutoEngageArcDegrees, StatOp.Add, 30f) });
            var service = MakeService(wideSweep);

            service.Acquire(wideSweep);

            Assert.AreEqual(150f, _stats.Player.GetValue(StatId.AutoEngageArcDegrees), 0.0001f);
            Assert.AreEqual(1, service.GetStackCount(wideSweep));
        }

        [Test]
        public void SecondCopy_UsesAdditionalModifiers()
        {
            _stats.Player.SetBase(StatId.HealPerFinish, 0f);
            var vampiric = MakeProtocol("Vampiric", ProtocolRarity.Tuned, 3, null,
                new[] { (StatId.HealPerFinish, StatOp.Add, 2f) },
                new[] { (StatId.HealPerFinish, StatOp.Add, 1f) });
            var service = MakeService(vampiric);

            service.Acquire(vampiric);
            service.Acquire(vampiric);

            Assert.AreEqual(3f, _stats.Player.GetValue(StatId.HealPerFinish), 0.0001f); // 2 + 1
            Assert.AreEqual(2, service.GetStackCount(vampiric));
        }

        [Test]
        public void MaxStacks_BlocksFurtherCopies()
        {
            var unique = MakeProtocol("IronGrip", ProtocolRarity.Tuned, 1, null,
                new[] { (StatId.GrabDurationScale, StatOp.PctAdd, 0.5f) });
            var service = MakeService(unique);

            service.Acquire(unique);
            service.Acquire(unique); // ignored

            Assert.AreEqual(1, service.GetStackCount(unique));
            Assert.IsFalse(service.IsAvailable(unique));
        }

        [Test]
        public void Prerequisite_GatesUntilAcquired()
        {
            var afterburner = MakeProtocol("Afterburner", ProtocolRarity.Tuned, 1, null,
                new[] { (StatId.MomentumDecaySeconds, StatOp.Add, 1.7f) });
            var governor = MakeProtocol("RedlineGovernor", ProtocolRarity.Prototype, 1, afterburner,
                new[] { (StatId.OverdriveMultiplier, StatOp.Add, 0.5f) });
            var service = MakeService(afterburner, governor);

            Assert.IsFalse(service.IsAvailable(governor));
            for (int i = 0; i < 20; i++)
            {
                CollectionAssert.DoesNotContain(service.RollChoices(3), governor);
            }

            service.Acquire(afterburner);

            Assert.IsTrue(service.IsAvailable(governor));
        }

        [Test]
        public void GatedAcquire_IsIgnored()
        {
            var afterburner = MakeProtocol("Afterburner", ProtocolRarity.Tuned, 1, null,
                new[] { (StatId.MomentumDecaySeconds, StatOp.Add, 1.7f) });
            var governor = MakeProtocol("RedlineGovernor", ProtocolRarity.Prototype, 1, afterburner,
                new[] { (StatId.OverdriveMultiplier, StatOp.Add, 0.5f) });
            var service = MakeService(afterburner, governor);

            service.Acquire(governor); // gated → no-op

            Assert.AreEqual(0, service.GetStackCount(governor));
        }

        [Test]
        public void RollChoices_NoDuplicatesWithinOneDraft()
        {
            var a = MakeProtocol("A", ProtocolRarity.Stock, 3, null, new[] { (StatId.HealPerFinish, StatOp.Add, 1f) });
            var b = MakeProtocol("B", ProtocolRarity.Stock, 3, null, new[] { (StatId.HealPerFinish, StatOp.Add, 1f) });
            var c = MakeProtocol("C", ProtocolRarity.Stock, 3, null, new[] { (StatId.HealPerFinish, StatOp.Add, 1f) });
            var service = MakeService(a, b, c);

            var choices = service.RollChoices(3);

            Assert.AreEqual(3, choices.Count);
            CollectionAssert.AllItemsAreUnique(choices);
        }

        [Test]
        public void RollChoices_ShrinksWhenPoolIsExhausted()
        {
            var only = MakeProtocol("Only", ProtocolRarity.Stock, 1, null, new[] { (StatId.HealPerFinish, StatOp.Add, 1f) });
            var service = MakeService(only);

            Assert.AreEqual(1, service.RollChoices(3).Count);

            service.Acquire(only);

            Assert.AreEqual(0, service.RollChoices(3).Count);
        }

        [Test]
        public void Acquire_PublishesProtocolAcquired()
        {
            ProtocolAcquired received = default;
            using var sub = _signals.On<ProtocolAcquired>().Subscribe(e => received = e);
            var wideSweep = MakeProtocol("WideSweep", ProtocolRarity.Stock, 2, null,
                new[] { (StatId.AutoEngageArcDegrees, StatOp.Add, 30f) });
            var service = MakeService(wideSweep);

            service.Acquire(wideSweep);

            Assert.AreEqual(wideSweep, received.Protocol);
            Assert.AreEqual(1, received.StackCount);
        }
    }
}
```

- [ ] **Step 2: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`ProtocolService` does not exist yet). Proceed.

- [ ] **Step 3: Implement**

`Assets/_neon/Scripts/Protocols/IProtocolService.cs`:

```csharp
using System.Collections.Generic;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The in-run protocol stack (spec §5.3, protocol doc v0.3 Hard Split:
    /// level-up-draft only). Acquiring = applying the asset's stat-modifier bundle.
    /// </summary>
    public interface IProtocolService
    {
        IReadOnlyList<ProtocolDefinitionAsset> Catalog { get; }
        int GetStackCount(ProtocolDefinitionAsset protocol);

        /// <summary>Below max stacks AND its prerequisite (if any) is in the stack.</summary>
        bool IsAvailable(ProtocolDefinitionAsset protocol);

        /// <summary>Rarity-weighted pick-N (no duplicates within one draft). May return fewer when the pool runs dry.</summary>
        IReadOnlyList<ProtocolDefinitionAsset> RollChoices(int count);

        void Acquire(ProtocolDefinitionAsset protocol);
    }
}
```

`Assets/_neon/Scripts/Protocols/ProtocolService.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    public sealed class ProtocolService : IProtocolService
    {
        // Doc §8.2 base weights. Signal scaling (×band) arrives with M3's Signal.
        private const float WEIGHT_STOCK = 100f;
        private const float WEIGHT_TUNED = 50f;
        private const float WEIGHT_PROTOTYPE = 18f;
        // Blacksite: 0 until gated + guaranteed-offered — lands with the first Blacksite (M3+).

        private readonly IStatSystem _stats;
        private readonly IGameplaySignals _signals;
        private readonly List<ProtocolDefinitionAsset> _catalog;
        private readonly Dictionary<ProtocolDefinitionAsset, int> _stacks = new();
        private readonly System.Random _random;

        public ProtocolService(IStatSystem stats, IGameplaySignals signals,
            IReadOnlyList<ProtocolDefinitionAsset> catalog, int randomSeed)
        {
            _stats = stats;
            _signals = signals;
            _catalog = catalog != null
                ? new List<ProtocolDefinitionAsset>(catalog)
                : new List<ProtocolDefinitionAsset>();
            _random = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);
        }

        public IReadOnlyList<ProtocolDefinitionAsset> Catalog => _catalog;

        public int GetStackCount(ProtocolDefinitionAsset protocol)
        {
            return protocol != null && _stacks.TryGetValue(protocol, out int count) ? count : 0;
        }

        public bool IsAvailable(ProtocolDefinitionAsset protocol)
        {
            if (protocol == null) return false;
            if (GetStackCount(protocol) >= Mathf.Max(1, protocol.MaxStacks)) return false;
            // Hidden-tree gating (doc §3): invisible until the prerequisite is stacked.
            if (protocol.Prerequisite != null && GetStackCount(protocol.Prerequisite) == 0) return false;
            return true;
        }

        public IReadOnlyList<ProtocolDefinitionAsset> RollChoices(int count)
        {
            var pool = new List<ProtocolDefinitionAsset>();
            foreach (var protocol in _catalog)
            {
                if (IsAvailable(protocol)) pool.Add(protocol);
            }

            var choices = new List<ProtocolDefinitionAsset>();
            while (choices.Count < count && pool.Count > 0)
            {
                float totalWeight = 0f;
                foreach (var protocol in pool) totalWeight += RarityWeight(protocol.Rarity);

                float roll = (float)(_random.NextDouble() * totalWeight);
                int pickedIndex = pool.Count - 1;
                for (int i = 0; i < pool.Count; i++)
                {
                    roll -= RarityWeight(pool[i].Rarity);
                    if (roll <= 0f)
                    {
                        pickedIndex = i;
                        break;
                    }
                }

                choices.Add(pool[pickedIndex]);
                pool.RemoveAt(pickedIndex); // no duplicates within one draft
            }
            return choices;
        }

        public void Acquire(ProtocolDefinitionAsset protocol)
        {
            if (!IsAvailable(protocol)) return;

            int newStackCount = GetStackCount(protocol) + 1;
            _stacks[protocol] = newStackCount;

            var modifiers = newStackCount == 1 ? protocol.FirstCopyModifiers : protocol.AdditionalCopyModifiers;
            var source = ModifierSource.Create($"protocol:{protocol.name}#{newStackCount}");
            foreach (var modifier in modifiers)
            {
                var sheet = modifier.Sheet == StatSheetTarget.Run ? _stats.Run : _stats.Player;
                sheet.AddModifier(modifier.Stat, modifier.Op, modifier.Value, source);
            }

            _signals.Publish(new ProtocolAcquired(protocol, newStackCount));
        }

        private static float RarityWeight(ProtocolRarity rarity)
        {
            switch (rarity)
            {
                case ProtocolRarity.Stock: return WEIGHT_STOCK;
                case ProtocolRarity.Tuned: return WEIGHT_TUNED;
                case ProtocolRarity.Prototype: return WEIGHT_PROTOTYPE;
                default: return 0f;
            }
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: **68/68 PASS** (60 + 8 protocol).

- [ ] **Step 5: Register in `GameplayServicesState`**

Add to `RegisterTypes` after `RegisterEconomySystem(builder);`:

```csharp
            RegisterProtocolService(builder);
```

and the helper (add `using System.Collections.Generic;` to the file's usings):

```csharp
        private static void RegisterProtocolService(IContainerBuilder builder)
        {
            builder.Register<ProtocolService>(Lifetime.Singleton)
                .WithParameter<IReadOnlyList<ProtocolDefinitionAsset>>(GrowthSettingsAsset.InstanceAsset.Settings.ProtocolCatalog)
                .WithParameter<int>(0) // unseeded RNG at runtime; tests seed explicitly
                .As<IProtocolService>();
        }
```

- [ ] **Step 6: Compile + boot check + commit**

Boot chain still clean (empty catalog until Task 7 — the service tolerates it). Then:

```bash
git add "Assets/_neon/Scripts/Protocols/IProtocolService.cs" "Assets/_neon/Scripts/Protocols/IProtocolService.cs.meta" "Assets/_neon/Scripts/Protocols/ProtocolService.cs" "Assets/_neon/Scripts/Protocols/ProtocolService.cs.meta" "Assets/_neon/Tests/EditMode/ProtocolServiceTests.cs" "Assets/_neon/Tests/EditMode/ProtocolServiceTests.cs.meta" "Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs"
git commit -m "feat: ProtocolService - rarity-weighted draft, stacking, hidden-tree gating (M2)"
```

---

### Task 7: Author the 8 protocol assets (editor work)

Values are the protocol doc's §8.4 numbers verbatim. Guardrail check while authoring: **every Op is Add or PctAdd — never Mult** (§8.1).

- [ ] **Step 1: Create the assets**

Create folder `Assets/_neon/Protocols/`. For each row: right-click → Create → **Neon/Protocols/Protocol Definition**, name it, fill the fields. All modifiers target **Sheet = Player**.

| Asset name | DisplayName | Family | Rarity | MaxStacks | Prerequisite | FirstCopyModifiers | AdditionalCopyModifiers | Description |
|---|---|---|---|---|---|---|---|---|
| `Protocol_WideSweep` | Wide Sweep | AutoGear | Stock | 2 | — | `AutoEngageArcDegrees · Add · 30` | `AutoEngageArcDegrees · Add · 30` | Auto-attack arc +30° (hard cap 180°). |
| `Protocol_OverclockedCoil` | Overclocked Coil | AutoGear | Tuned | 1 | — | `AutoEngageRate · PctAdd · 0.5` and `PlayerMaxHealthPct · PctAdd · -0.1` | — | +50% auto-attack rate, −10% max HP. |
| `Protocol_Afterburner` | Afterburner | Momentum | Tuned | 1 | — | `MomentumDecaySeconds · Add · 1.7` | — | Momentum decays 40% slower (2.5s → 4.2s). |
| `Protocol_ExecutionersCadence` | Executioner's Cadence | Momentum | Tuned | 1 | — | `MomentumBonusStepsBelowHot · Add · 1` | — | Finishing hits below Hot grant double Momentum. |
| `Protocol_RedlineGovernor` | Redline Governor | Momentum | Prototype | 1 | `Protocol_Afterburner` | `OverdriveMultiplier · Add · 0.5` | — | Overdrive multiplier ×2.5 → ×3.0. |
| `Protocol_IronGrip` | Iron Grip | Brawler | Tuned | 1 | — | `GrabDurationScale · PctAdd · 0.5` | — | Grab holds 50% longer (3s → 4.5s). |
| `Protocol_ConcussiveFinish` | Concussive Finish | Execution | Stock | 3 | — | `FinishAoeRadius · Add · 0.8` | `FinishAoeRadius · Add · 0.2` | Finishing hits detonate a small shockwave. |
| `Protocol_VampiricCadence` | Vampiric Cadence | Defense | Tuned | 3 | — | `HealPerFinish · Add · 2` | `HealPerFinish · Add · 1` | Finishing hits restore health. |

- [ ] **Step 2: Fill the catalog**

Open `Assets/Resources/Settings/GrowthSettingsAsset.asset` → **Protocol Catalog** → add all 8 assets.

- [ ] **Step 3: Commit**

```bash
git add "Assets/_neon/Protocols" "Assets/_neon/Protocols.meta" "Assets/Resources/Settings/GrowthSettingsAsset.asset"
git commit -m "content: author the M2 protocol set - 7 draftable + gated Redline Governor"
```

---

### Task 8: `ProtocolEffectsSystem` + bridge area damage (test-first)

One Level-scoped consumer turns the four derived stats into world effects: `PlayerMaxHealthPct` → player `maxHp` (floor 50, §8.1), `GrabDurationScale` → player `UnitSettings.grabDuration`, `HealPerFinish` → `AddHealth` on finish, `FinishAoeRadius` → splash damage at the finish position (chaff via a new bridge method, hero-tier directly).

**Files:**
- Modify: `Assets/_neon/Scripts/Swarm/ISwarmBridge.cs`, `NullSwarmBridge.cs`, `SwarmBridge.cs`
- Modify: `Assets/_neon/Tests/EditMode/Fakes.cs`
- Create: `Assets/_neon/Scripts/Protocols/ProtocolEffectsSystem.cs`
- Modify: `Assets/_neon/Scripts/Level/Level.cs`
- Test: `Assets/_neon/Tests/EditMode/ProtocolEffectsSystemTests.cs`

- [ ] **Step 1: Add `ApplyAreaDamage` to the seam**

In `ISwarmBridge.cs`, append inside the interface after `ApplyVerbHit`:

```csharp

        /// <summary>Radial chaff damage (e.g. Concussive Finish). Non-finish damage — may kill.</summary>
        void ApplyAreaDamage(Vector2 center, float radius, int damage);
```

In `NullSwarmBridge.cs`, append inside the class:

```csharp

        public void ApplyAreaDamage(Vector2 center, float radius, int damage)
        {
        }
```

In `SwarmBridge.cs`, append after `ApplyVerbHit` (before `TryInitialize`):

```csharp

        public void ApplyAreaDamage(Vector2 center, float radius, int damage)
        {
            if (!_initialized || radius <= 0f) return;

            using var entities = _chaffQuery.ToEntityArray(Allocator.Temp);
            using var positions = _chaffQuery.ToComponentDataArray<BeltPosition>(Allocator.Temp);

            var centerF = new float2(center.x, center.y);
            float radiusSq = radius * radius;
            var damageBuffer = _world.EntityManager.GetBuffer<SwarmDamageCommand>(_controlEntity);

            for (int i = 0; i < entities.Length; i++)
            {
                if (math.lengthsq(positions[i].Value - centerF) > radiusSq) continue;
                damageBuffer.Add(new SwarmDamageCommand { Target = entities[i], Amount = damage, IsChip = 0 });
            }
        }
```

In `Fakes.cs`, add to `FakeSwarmBridge` (after `ApplyVerbHit`):

```csharp

        public readonly List<(Vector2 Center, float Radius, int Damage)> AreaDamageCalls = new();

        public void ApplyAreaDamage(Vector2 center, float radius, int damage) => AreaDamageCalls.Add((center, radius, damage));
```

- [ ] **Step 2: Write the failing tests**

`Assets/_neon/Tests/EditMode/ProtocolEffectsSystemTests.cs`:

```csharp
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class ProtocolEffectsSystemTests
    {
        private GameplaySignals _signals;
        private StatSystem _stats;
        private FakeEntitiesService _entities;
        private FakeSwarmBridge _bridge;
        private ProtocolEffectsSystem _effects;
        private GameObject _player;
        private HealthSystem _health;
        private UnitSettings _settings;
        private ModifierSource _source;

        private static GrowthConfig TestConfig => new(
            xpPerKill: 1, chargePerFinish: 2, overchargePerFinish: 8, overchargeCap: 100,
            xpCostBase: 10f, xpCostExponent: 1.35f, levelUpSlowMoScale: 0.1f,
            challengeSequenceBase: new[] { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK },
            challengeSequenceHot: new[] { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK, ATTACKTYPE.PUNCH },
            challengeInputWindowSeconds: 0.9f, challengeWindowTightenPerTier: 0.1f, finishAoeDamage: 6);

        [SetUp]
        public void SetUp()
        {
            _signals = new GameplaySignals();
            _stats = new StatSystem();
            _entities = new FakeEntitiesService();
            _bridge = new FakeSwarmBridge();
            _effects = new ProtocolEffectsSystem(_signals, _stats, _entities, _bridge, TestConfig);
            _source = ModifierSource.Create("test-protocol");

            _player = new GameObject("Player");
            _health = _player.AddComponent<HealthSystem>();
            _health.maxHp = 100;
            _health.currentHp = 100;
            _settings = _player.AddComponent<UnitSettings>();
            _settings.grabDuration = 3f;
            _entities.Register(_player, UNITTYPE.PLAYER); // fires OnEntityRegistered → base capture
        }

        [TearDown]
        public void TearDown()
        {
            _effects.Dispose();
            _signals.Dispose();
            Object.DestroyImmediate(_player);
        }

        private void AcquireSomething()
        {
            var protocol = ScriptableObject.CreateInstance<ProtocolDefinitionAsset>();
            _signals.Publish(new ProtocolAcquired(protocol, 1));
            Object.DestroyImmediate(protocol);
        }

        [Test]
        public void Ctor_SeedsDerivedStatBases()
        {
            Assert.AreEqual(1f, _stats.Player.GetValue(StatId.PlayerMaxHealthPct), 0.0001f);
            Assert.AreEqual(1f, _stats.Player.GetValue(StatId.GrabDurationScale), 0.0001f);
            Assert.AreEqual(0f, _stats.Player.GetValue(StatId.FinishAoeRadius), 0.0001f);
            Assert.AreEqual(0f, _stats.Player.GetValue(StatId.HealPerFinish), 0.0001f);
        }

        [Test]
        public void MaxHealthPct_RescalesPlayerHp_PreservingRatio()
        {
            _health.currentHp = 50; // 50%
            _stats.Player.AddModifier(StatId.PlayerMaxHealthPct, StatOp.PctAdd, -0.1f, _source);

            AcquireSomething();

            Assert.AreEqual(90, _health.maxHp);
            Assert.AreEqual(45, _health.currentHp);
        }

        [Test]
        public void MaxHealth_FloorsAt50()
        {
            _stats.Player.AddModifier(StatId.PlayerMaxHealthPct, StatOp.PctAdd, -0.9f, _source);

            AcquireSomething();

            Assert.AreEqual(50, _health.maxHp); // §8.1 HP floor
        }

        [Test]
        public void GrabDurationScale_RescalesFromCapturedBase()
        {
            _stats.Player.AddModifier(StatId.GrabDurationScale, StatOp.PctAdd, 0.5f, _source);

            AcquireSomething();
            Assert.AreEqual(4.5f, _settings.grabDuration, 0.0001f);

            AcquireSomething(); // reapplying must NOT compound (base was captured once)
            Assert.AreEqual(4.5f, _settings.grabDuration, 0.0001f);
        }

        [Test]
        public void HealPerFinish_HealsOnEnemyFinished()
        {
            _health.currentHp = 40;
            _stats.Player.AddModifier(StatId.HealPerFinish, StatOp.Add, 2f, _source);

            _signals.Publish(new EnemyFinished(Vector2.zero, wasChaff: true));

            Assert.AreEqual(42, _health.currentHp);
        }

        [Test]
        public void FinishAoe_CallsBridgeWithScaledDamage()
        {
            _stats.Player.SetBase(StatId.DamageMultiplier, 1f);
            var momentum = ModifierSource.Create("momentum");
            _stats.Player.AddModifier(StatId.DamageMultiplier, StatOp.Mult, 2f, momentum);
            _stats.Player.AddModifier(StatId.FinishAoeRadius, StatOp.Add, 0.8f, _source);

            _signals.Publish(new EnemyFinished(new Vector2(3f, 1f), wasChaff: true));

            Assert.AreEqual(1, _bridge.AreaDamageCalls.Count);
            Assert.AreEqual(new Vector2(3f, 1f), _bridge.AreaDamageCalls[0].Center);
            Assert.AreEqual(0.8f, _bridge.AreaDamageCalls[0].Radius, 0.0001f);
            Assert.AreEqual(12, _bridge.AreaDamageCalls[0].Damage); // 6 × 2
        }

        [Test]
        public void NoAoe_WhenRadiusIsZero()
        {
            _signals.Publish(new EnemyFinished(Vector2.zero, wasChaff: true));

            Assert.AreEqual(0, _bridge.AreaDamageCalls.Count);
        }
    }
}
```

- [ ] **Step 3: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`ProtocolEffectsSystem` does not exist yet). Proceed.

- [ ] **Step 4: Implement**

`Assets/_neon/Scripts/Protocols/ProtocolEffectsSystem.cs`:

```csharp
using System;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Turns the growth-layer derived stats into world effects (Level scope):
    /// PlayerMaxHealthPct → player maxHp (floor 50, doc §8.1) ·
    /// GrabDurationScale → player UnitSettings.grabDuration ·
    /// HealPerFinish → AddHealth on EnemyFinished (Vampiric Cadence) ·
    /// FinishAoeRadius → splash at the finish position (Concussive Finish).
    /// Bases for the derived values are captured ONCE per player spawn so
    /// re-application never compounds.
    /// </summary>
    public sealed class ProtocolEffectsSystem : IDisposable
    {
        private const int MAX_HP_FLOOR = 50; // protocol doc §8.1 hard cap

        private readonly IGameplaySignals _signals;
        private readonly IStatSystem _stats;
        private readonly IEntitiesService _entities;
        private readonly ISwarmBridge _bridge;
        private readonly GrowthConfig _config;
        private readonly IDisposable _acquiredSubscription;
        private readonly IDisposable _finishSubscription;

        private GameObject _player;
        private int _baseMaxHp;
        private float _baseGrabDuration;

        public ProtocolEffectsSystem(IGameplaySignals signals, IStatSystem stats,
            IEntitiesService entities, ISwarmBridge bridge, GrowthConfig config)
        {
            _signals = signals;
            _stats = stats;
            _entities = entities;
            _bridge = bridge;
            _config = config;

            _stats.Player.SetBase(StatId.PlayerMaxHealthPct, 1f);
            _stats.Player.SetBase(StatId.GrabDurationScale, 1f);
            _stats.Player.SetBase(StatId.FinishAoeRadius, 0f);
            _stats.Player.SetBase(StatId.HealPerFinish, 0f);

            _acquiredSubscription = _signals.On<ProtocolAcquired>().Subscribe(_ => ApplyDerivedToPlayer());
            _finishSubscription = _signals.On<EnemyFinished>().Subscribe(OnFinish);
            _entities.OnEntityRegistered += OnEntityRegistered;
        }

        public void Dispose()
        {
            _acquiredSubscription?.Dispose();
            _finishSubscription?.Dispose();
            _entities.OnEntityRegistered -= OnEntityRegistered;
        }

        private void OnEntityRegistered(TrackedEntity entity)
        {
            if (entity.UnitType != UNITTYPE.PLAYER || entity.GameObject == null) return;

            _player = entity.GameObject;
            var health = _player.GetComponent<HealthSystem>();
            _baseMaxHp = health != null ? health.maxHp : 0;
            var settings = _player.GetComponent<UnitSettings>();
            _baseGrabDuration = settings != null ? settings.grabDuration : 0f;

            ApplyDerivedToPlayer();
        }

        private void ApplyDerivedToPlayer()
        {
            if (_player == null) return;

            var health = _player.GetComponent<HealthSystem>();
            if (health != null && _baseMaxHp > 0)
            {
                float ratio = health.healthPercentage;
                int newMax = Mathf.Max(MAX_HP_FLOOR,
                    Mathf.RoundToInt(_baseMaxHp * _stats.Player.GetValue(StatId.PlayerMaxHealthPct)));
                if (newMax != health.maxHp)
                {
                    health.maxHp = newMax;
                    health.currentHp = Mathf.Clamp(Mathf.RoundToInt(newMax * ratio), 1, newMax);
                    health.AddHealth(0); // republish onHealthChange so bars refresh
                }
            }

            var settings = _player.GetComponent<UnitSettings>();
            if (settings != null && _baseGrabDuration > 0f)
            {
                settings.grabDuration = _baseGrabDuration * _stats.Player.GetValue(StatId.GrabDurationScale);
            }
        }

        private void OnFinish(EnemyFinished finished)
        {
            int heal = Mathf.RoundToInt(_stats.Player.GetValue(StatId.HealPerFinish));
            if (heal > 0 && _player != null)
            {
                _player.GetComponent<HealthSystem>()?.AddHealth(heal);
            }

            float radius = _stats.Player.GetValue(StatId.FinishAoeRadius);
            if (radius <= 0f) return;

            float damageMultiplier = _stats.Player.GetValue(StatId.DamageMultiplier);
            if (damageMultiplier <= 0f) damageMultiplier = 1f;
            int damage = Mathf.Max(1, Mathf.RoundToInt(_config.FinishAoeDamage * damageMultiplier));

            _bridge.ApplyAreaDamage(finished.Position, radius, damage);

            // Hero-tier splash (runtime path — SubstractHealth touches injected audio,
            // so EditMode coverage stops at the bridge call above).
            var enemies = _entities.GetByType(UNITTYPE.ENEMY);
            float radiusSq = radius * radius;
            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i].GameObject;
                if (go == null) continue;
                var health = go.GetComponent<HealthSystem>();
                if (health == null || health.isDead) continue;
                if (((Vector2)go.transform.position - finished.Position).sqrMagnitude > radiusSq) continue;
                health.SubstractHealth(damage);
            }
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: **75/75 PASS** (68 + 7 effects).

- [ ] **Step 6: Register in the Level scope**

In `Level.RegisterEngagementSystems`, after `builder.RegisterInstance(EngagementConfig.FromSettings());` add:

```csharp
            builder.RegisterInstance(GrowthConfig.FromSettings());
```

and after the `FinishResolver` registration line add:

```csharp
            builder.Register<ProtocolEffectsSystem>(Lifetime.Scoped).AsSelf();
```

In the `Configure` build callback, after `container.Resolve<FinishResolver>();` add:

```csharp
                    container.Resolve<ProtocolEffectsSystem>();
```

- [ ] **Step 7: Commit**

```bash
git add "Assets/_neon/Scripts/Swarm/ISwarmBridge.cs" "Assets/_neon/Scripts/Swarm/NullSwarmBridge.cs" "Assets/_neon/Scripts/Swarm/SwarmBridge.cs" "Assets/_neon/Tests/EditMode/Fakes.cs" "Assets/_neon/Scripts/Protocols/ProtocolEffectsSystem.cs" "Assets/_neon/Scripts/Protocols/ProtocolEffectsSystem.cs.meta" "Assets/_neon/Tests/EditMode/ProtocolEffectsSystemTests.cs" "Assets/_neon/Tests/EditMode/ProtocolEffectsSystemTests.cs.meta" "Assets/_neon/Scripts/Level/Level.cs"
git commit -m "feat: ProtocolEffectsSystem - derived stats to world effects + bridge AoE (M2)"
```

---

### Task 9: `IProgressionSystem` — XP curve → slow-mo 1-of-3 draft (test-first)

⌈10·N^1.35⌉ per level (doc §8.3). Level-ups bank; one draft offered at a time; slow-mo (via the clock — now the owner of `Time.timeScale`, Task 3) holds until `Choose`.

**Files:**
- Create: `Assets/_neon/Scripts/Growth/IProgressionSystem.cs`
- Create: `Assets/_neon/Scripts/Growth/ProgressionSystem.cs`
- Modify: `Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs`
- Test: `Assets/_neon/Tests/EditMode/ProgressionSystemTests.cs`

- [ ] **Step 1: Write the failing tests**

`Assets/_neon/Tests/EditMode/ProgressionSystemTests.cs` (uses the REAL `ProtocolService` with a tiny catalog — draft + acquire integration included):

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class ProgressionSystemTests
    {
        private GameplayClock _clock;
        private GameplaySignals _signals;
        private StatSystem _stats;
        private ProtocolService _protocols;
        private ProgressionSystem _progression;
        private readonly List<ProtocolDefinitionAsset> _createdAssets = new();
        private readonly List<LevelUpChoicesReady> _offers = new();
        private System.IDisposable _offerSub;

        private static GrowthConfig TestConfig => new(
            xpPerKill: 1, chargePerFinish: 2, overchargePerFinish: 8, overchargeCap: 100,
            xpCostBase: 10f, xpCostExponent: 1.35f, levelUpSlowMoScale: 0.1f,
            challengeSequenceBase: new[] { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK },
            challengeSequenceHot: new[] { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK, ATTACKTYPE.PUNCH },
            challengeInputWindowSeconds: 0.9f, challengeWindowTightenPerTier: 0.1f, finishAoeDamage: 6);

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _signals = new GameplaySignals();
            _stats = new StatSystem();
            _protocols = new ProtocolService(_stats, _signals, MakeCatalog(3), randomSeed: 777);
            _progression = new ProgressionSystem(_signals, _clock, _protocols, TestConfig);
            _offers.Clear();
            _offerSub = _signals.On<LevelUpChoicesReady>().Subscribe(e => _offers.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            _offerSub?.Dispose();
            _progression.Dispose();
            _signals.Dispose();
            foreach (var asset in _createdAssets) Object.DestroyImmediate(asset);
            _createdAssets.Clear();
        }

        private List<ProtocolDefinitionAsset> MakeCatalog(int count)
        {
            var catalog = new List<ProtocolDefinitionAsset>();
            for (int i = 0; i < count; i++)
            {
                var asset = ScriptableObject.CreateInstance<ProtocolDefinitionAsset>();
                asset.name = $"TestProtocol{i}";
                asset.DisplayName = asset.name;
                asset.Rarity = ProtocolRarity.Stock;
                asset.MaxStacks = 5;
                asset.FirstCopyModifiers.Add(new ProtocolStatModifier
                {
                    Sheet = StatSheetTarget.Player, Stat = StatId.HealPerFinish, Op = StatOp.Add, Value = 1f
                });
                asset.AdditionalCopyModifiers.Add(new ProtocolStatModifier
                {
                    Sheet = StatSheetTarget.Player, Stat = StatId.HealPerFinish, Op = StatOp.Add, Value = 1f
                });
                _createdAssets.Add(asset);
                catalog.Add(asset);
            }
            return catalog;
        }

        private void Xp(int total) => _signals.Publish(new XpGained(1, total));

        [Test]
        public void BelowFirstThreshold_NoLevelUp()
        {
            Xp(9); // cost(1) = ceil(10·1^1.35) = 10

            Assert.AreEqual(1, _progression.Level);
            Assert.IsFalse(_progression.AwaitingChoice);
        }

        [Test]
        public void FirstThreshold_LevelsAndOffersDraft_WithSlowMo()
        {
            Xp(10);

            Assert.AreEqual(2, _progression.Level);
            Assert.IsTrue(_progression.AwaitingChoice);
            Assert.AreEqual(1, _offers.Count);
            Assert.AreEqual(3, _offers[0].Choices.Length);
            Assert.AreEqual(0.1f, _clock.EffectiveScale, 0.0001f);
        }

        [Test]
        public void CurveMatchesDoc_SecondLevelAt36Total()
        {
            // cost(2) = ceil(10·2^1.35) = 26 → level 3 at 10 + 26 = 36 total XP.
            Xp(35);
            _progression.Choose(0); // clear the level-2 draft
            Assert.AreEqual(2, _progression.Level);

            Xp(36);
            Assert.AreEqual(3, _progression.Level);
        }

        [Test]
        public void Choose_Acquires_ClearsSlowMo()
        {
            Xp(10);
            var chosen = _offers[0].Choices[1];

            _progression.Choose(1);

            Assert.AreEqual(1, _protocols.GetStackCount(chosen));
            Assert.IsFalse(_progression.AwaitingChoice);
            Assert.AreEqual(1f, _clock.EffectiveScale, 0.0001f);
        }

        [Test]
        public void MultiLevelJump_BanksOffers_ServesThemSequentially()
        {
            Xp(36); // clears level-1 AND level-2 thresholds in one grant

            Assert.AreEqual(3, _progression.Level);
            Assert.AreEqual(1, _offers.Count);   // one draft at a time

            _progression.Choose(0);

            Assert.AreEqual(2, _offers.Count);   // the banked one follows immediately
            Assert.IsTrue(_progression.AwaitingChoice);
        }

        [Test]
        public void EmptyCatalog_LevelsWithoutDraft_NoSlowMo()
        {
            _progression.Dispose();
            var emptyService = new ProtocolService(_stats, _signals, new List<ProtocolDefinitionAsset>(), randomSeed: 1);
            _progression = new ProgressionSystem(_signals, _clock, emptyService, TestConfig);

            Xp(10);

            Assert.AreEqual(2, _progression.Level);
            Assert.IsFalse(_progression.AwaitingChoice);
            Assert.AreEqual(1f, _clock.EffectiveScale, 0.0001f);
        }

        [Test]
        public void Choose_WithNothingPending_IsIgnored()
        {
            Assert.DoesNotThrow(() => _progression.Choose(0));
            Assert.AreEqual(1, _progression.Level);
        }
    }
}
```

- [ ] **Step 2: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`ProgressionSystem` does not exist yet). Proceed.

- [ ] **Step 3: Implement**

`Assets/_neon/Scripts/Growth/IProgressionSystem.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Watches XP thresholds → level-up → slow-mo 1-of-3 protocol draft (spec §5.3).
    /// </summary>
    public interface IProgressionSystem
    {
        int Level { get; }

        /// <summary>True while a draft is on screen (slow-mo held).</summary>
        bool AwaitingChoice { get; }

        /// <summary>Apply the drafted pick (0-based). No-op when nothing is pending.</summary>
        void Choose(int index);
    }
}
```

`Assets/_neon/Scripts/Growth/ProgressionSystem.cs`:

```csharp
using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    public sealed class ProgressionSystem : IProgressionSystem, IDisposable
    {
        private readonly IGameplaySignals _signals;
        private readonly IGameplayClock _clock;
        private readonly IProtocolService _protocols;
        private readonly GrowthConfig _config;
        private readonly ModifierSource _slowMoSource = ModifierSource.Create("levelup-slowmo");
        private readonly IDisposable _xpSubscription;

        private int _totalXp;
        private int _levelStartXp;
        private int _nextLevelAtXp;
        private int _pendingOffers;
        private IReadOnlyList<ProtocolDefinitionAsset> _currentChoices;

        public int Level { get; private set; } = 1;
        public bool AwaitingChoice { get; private set; }

        public ProgressionSystem(IGameplaySignals signals, IGameplayClock clock,
            IProtocolService protocols, GrowthConfig config)
        {
            _signals = signals;
            _clock = clock;
            _protocols = protocols;
            _config = config;
            _nextLevelAtXp = XpCostForLevel(1);
            _xpSubscription = _signals.On<XpGained>().Subscribe(e => OnXp(e.TotalXp));
        }

        public void Dispose()
        {
            _xpSubscription?.Dispose();
            _clock.ClearScale(_slowMoSource);
        }

        public void Choose(int index)
        {
            if (!AwaitingChoice || _currentChoices == null || _currentChoices.Count == 0) return;

            int clampedIndex = Mathf.Clamp(index, 0, _currentChoices.Count - 1);
            _protocols.Acquire(_currentChoices[clampedIndex]);

            AwaitingChoice = false;
            _currentChoices = null;
            _clock.ClearScale(_slowMoSource);
            TryOfferNext();
        }

        private void OnXp(int totalXp)
        {
            _totalXp = totalXp;
            while (_totalXp >= _nextLevelAtXp)
            {
                Level++;
                _levelStartXp = _nextLevelAtXp;
                _nextLevelAtXp += XpCostForLevel(Level);
                _pendingOffers++;
                _signals.Publish(new PlayerLevelChanged(Level));
            }

            _signals.Publish(new XpProgressChanged(Level, _totalXp - _levelStartXp, _nextLevelAtXp - _levelStartXp));
            TryOfferNext();
        }

        private void TryOfferNext()
        {
            while (!AwaitingChoice && _pendingOffers > 0)
            {
                _pendingOffers--;
                var choices = _protocols.RollChoices(3);
                if (choices.Count == 0) continue; // pool dry: the level is banked, no draft

                _currentChoices = choices;
                AwaitingChoice = true;
                _clock.SetScale(_slowMoSource, _config.LevelUpSlowMoScale);

                var choicesArray = new ProtocolDefinitionAsset[choices.Count];
                for (int i = 0; i < choices.Count; i++) choicesArray[i] = choices[i];
                _signals.Publish(new LevelUpChoicesReady(Level, choicesArray));
            }
        }

        // XP required to clear level N (protocol doc §8.3: ceil(10 × N^1.35)).
        private int XpCostForLevel(int level)
        {
            return Mathf.CeilToInt(_config.XpCostBase * Mathf.Pow(level, _config.XpCostExponent));
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: **82/82 PASS** (75 + 7 progression).

- [ ] **Step 5: Register in `GameplayServicesState`**

Add to `RegisterTypes` after `RegisterProtocolService(builder);`:

```csharp
            RegisterProgressionSystem(builder);
```

and the helper:

```csharp
        private static void RegisterProgressionSystem(IContainerBuilder builder)
        {
            builder.Register<ProgressionSystem>(Lifetime.Singleton)
                .WithParameter(GrowthConfig.FromSettings())
                .As<IProgressionSystem>();
            builder.RegisterBuildCallback(container => container.Resolve<IProgressionSystem>());
        }
```

- [ ] **Step 6: Commit**

```bash
git add "Assets/_neon/Scripts/Growth/IProgressionSystem.cs" "Assets/_neon/Scripts/Growth/IProgressionSystem.cs.meta" "Assets/_neon/Scripts/Growth/ProgressionSystem.cs" "Assets/_neon/Scripts/Growth/ProgressionSystem.cs.meta" "Assets/_neon/Tests/EditMode/ProgressionSystemTests.cs" "Assets/_neon/Tests/EditMode/ProgressionSystemTests.cs.meta" "Assets/_neon/Scripts/Lifecycle/ApplicationFSM/States/GameplayServicesState.cs"
git commit -m "feat: ProgressionSystem - XP curve to slow-mo 1-of-3 draft (M2)"
```

---

### Task 10: Growth HUD — XP bar, Overcharge meter, level-up picker

Signs are pure signal consumers; the picker additionally *commands* `IProgressionSystem.Choose` (interactive UI is a command path, not a sign — the spec's consumer rule is for feedback). uGUI buttons are unaffected by `Time.timeScale`, so the picker is fully usable at slow-mo 0.1.

**Files:**
- Create: `Assets/_neon/Scripts/UI/UIHUDXpBar.cs`
- Create: `Assets/_neon/Scripts/UI/UIHUDOverchargeMeter.cs`
- Create: `Assets/_neon/Scripts/UI/UILevelUpPicker.cs`
- Editor: canvas wiring in `03_Level1`

- [ ] **Step 1: XP bar**

`Assets/_neon/Scripts/UI/UIHUDXpBar.cs`:

```csharp
using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon {

    //XP progress within the current level + level label. Pure signals consumer.
    public class UIHUDXpBar : MonoBehaviour {

        [SerializeField] private Image fillImage;
        [SerializeField] private Text levelLabel;
        [Inject] private IGameplaySignals _signals;
        private IDisposable _subscription;

        void Start(){
            if(_signals == null) return; //scene without DI injection
            if(fillImage != null) fillImage.fillAmount = 0f;
            if(levelLabel != null) levelLabel.text = "LV 1";
            _subscription = _signals.On<XpProgressChanged>().Subscribe(Apply);
        }

        void OnDestroy(){
            _subscription?.Dispose();
        }

        void Apply(XpProgressChanged progress){
            if(fillImage != null && progress.XpForNextLevel > 0){
                fillImage.fillAmount = Mathf.Clamp01((float)progress.XpIntoLevel / progress.XpForNextLevel);
            }
            if(levelLabel != null) levelLabel.text = $"LV {progress.Level}";
        }
    }
}
```

- [ ] **Step 2: Overcharge meter**

`Assets/_neon/Scripts/UI/UIHUDOverchargeMeter.cs`:

```csharp
using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon {

    //Overcharge fill (spending arrives with the M4 finisher). Pure signals consumer.
    public class UIHUDOverchargeMeter : MonoBehaviour {

        [SerializeField] private Image fillImage;
        [Inject] private IGameplaySignals _signals;
        private IDisposable _subscription;

        void Start(){
            if(_signals == null) return;
            if(fillImage != null) fillImage.fillAmount = 0f;
            _subscription = _signals.On<OverchargeChanged>().Subscribe(Apply);
        }

        void OnDestroy(){
            _subscription?.Dispose();
        }

        void Apply(OverchargeChanged overcharge){
            if(fillImage != null && overcharge.Cap > 0){
                fillImage.fillAmount = Mathf.Clamp01((float)overcharge.Value / overcharge.Cap);
            }
        }
    }
}
```

- [ ] **Step 3: The picker**

`Assets/_neon/Scripts/UI/UILevelUpPicker.cs`:

```csharp
using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon {

    //The 1-of-3 level-up draft (spec §5.3). Consumes LevelUpChoicesReady and
    //commands IProgressionSystem.Choose. Buttons work at slow-mo (UI ignores timeScale).
    public class UILevelUpPicker : MonoBehaviour {

        [SerializeField] private GameObject panel;
        [SerializeField] private Button[] choiceButtons = new Button[3];
        [SerializeField] private Text[] choiceTitles = new Text[3];
        [SerializeField] private Text[] choiceDescriptions = new Text[3];
        [Inject] private IGameplaySignals _signals;
        [Inject] private IProgressionSystem _progression;
        private IDisposable _subscription;

        void Start(){
            if(_signals == null || _progression == null) return; //scene without DI injection
            if(panel != null) panel.SetActive(false);
            for(int i = 0; i < choiceButtons.Length; i++){
                int index = i; //capture for the closure
                if(choiceButtons[i] != null) choiceButtons[i].onClick.AddListener(() => OnChoice(index));
            }
            _subscription = _signals.On<LevelUpChoicesReady>().Subscribe(Show);
        }

        void OnDestroy(){
            _subscription?.Dispose();
        }

        void Show(LevelUpChoicesReady levelUp){
            if(panel == null) return;
            panel.SetActive(true);
            for(int i = 0; i < choiceButtons.Length; i++){
                bool hasChoice = i < levelUp.Choices.Length && levelUp.Choices[i] != null;
                if(choiceButtons[i] != null) choiceButtons[i].gameObject.SetActive(hasChoice);
                if(!hasChoice) continue;

                var protocol = levelUp.Choices[i];
                if(choiceTitles[i] != null) choiceTitles[i].text = $"{protocol.DisplayName}\n<size=60%>{protocol.Family} · {protocol.Rarity}</size>";
                if(choiceDescriptions[i] != null) choiceDescriptions[i].text = protocol.Description;
            }
        }

        void OnChoice(int index){
            if(panel != null) panel.SetActive(false);
            _progression.Choose(index);
        }
    }
}
```

- [ ] **Step 4: Scene wiring (editor, not in Play mode)**

Open `03_Level1`, find the HUD Canvas (the one with the Momentum meter from M1):
1. `XpBar` (RectTransform anchored under the Momentum meter): child `Fill` (**UI → Image**, Filled/Horizontal, ~200×12) + child `LevelLabel` (**Text**). Add `UIHUDXpBar`, assign both.
2. `OverchargeMeter` (next to the Momentum meter): child `Fill` (Filled/Horizontal, distinct color, ~200×12). Add `UIHUDOverchargeMeter`, assign.
3. `LevelUpPicker` (root-level under the canvas): child `Panel` (full-screen dimmed Image, ~70% black) containing three `ChoiceButton0/1/2` (**UI → Button**, ~260×140, centered row), each with two Texts: `Title` (top, bold) and `Description` (body). Add `UILevelUpPicker` to `LevelUpPicker`, assign `panel`, the 3 buttons, 3 titles, 3 descriptions.
4. Save the scene.

- [ ] **Step 5: Runtime gate (Recipe 4) — the growth loop, live**

Boot into Level1 and fight:
1. Kills fill the **XP bar**; finishes fill **Overcharge**.
2. At 10 XP: the world drops to slow-mo, the **picker** appears with 3 real cards (names, families, rarities, descriptions from the assets).
3. Pick *Wide Sweep* if offered → auto-engage visibly chips a wider arc afterward. Any pick: slow-mo releases instantly, combat resumes.
4. **Gating spot-check:** *Redline Governor* must never appear in a draft until *Afterburner* was picked in this session (the EditMode test proves it deterministically; this is the eyes-on confirmation).
5. Level a second time (26 more XP) — draft cadence feels right at current chip tuning? Note it for the gate record.
6. Exit Play mode. Zero errors.

- [ ] **Step 6: Commit**

```bash
git add "Assets/_neon/Scripts/UI/UIHUDXpBar.cs" "Assets/_neon/Scripts/UI/UIHUDXpBar.cs.meta" "Assets/_neon/Scripts/UI/UIHUDOverchargeMeter.cs" "Assets/_neon/Scripts/UI/UIHUDOverchargeMeter.cs.meta" "Assets/_neon/Scripts/UI/UILevelUpPicker.cs" "Assets/_neon/Scripts/UI/UILevelUpPicker.cs.meta" "Assets/_neon/Scenes/Game/03_Level1.unity"
git commit -m "feat: growth HUD - XP bar, Overcharge meter, level-up picker (M2)"
```

---

### Task 11: Tiered `IFinishChallenge` (test-first)

Decision P2: Finish-Ready **heroes** demand a landed verb sequence — PUNCH→KICK, escalating to PUNCH→KICK→PUNCH at Hot/Overdrive, per-input windows tightening with tier. Completing = instant finish + Momentum; dying mid-sequence = plain kill, no payout. Chaff stay single-verb (bridge-side, untouched). The challenge is pure logic + a `FinishResolver` rewrite on the same observation seam; **verbs unchanged**.

**Files:**
- Create: `Assets/_neon/Scripts/Engagement/IFinishChallenge.cs`
- Create: `Assets/_neon/Scripts/Engagement/SequenceFinishChallenge.cs`
- Modify: `Assets/_neon/Tests/EditMode/Fakes.cs` (add `FakeMomentumSystem`)
- Rewrite: `Assets/_neon/Scripts/Engagement/FinishResolver.cs`
- Rewrite: `Assets/_neon/Tests/EditMode/FinishResolverTests.cs`
- Modify: `Assets/_neon/Scripts/UI/UIHUDFinishPrompt.cs`
- Test: `Assets/_neon/Tests/EditMode/SequenceFinishChallengeTests.cs`

- [ ] **Step 1: Add the momentum fake**

Append to `Assets/_neon/Tests/EditMode/Fakes.cs` inside the namespace:

```csharp

    internal sealed class FakeMomentumSystem : IMomentumSystem
    {
        public MomentumTier Tier { get; set; } = MomentumTier.Cool;
    }
```

- [ ] **Step 2: Write the failing challenge tests**

`Assets/_neon/Tests/EditMode/SequenceFinishChallengeTests.cs`:

```csharp
using NUnit.Framework;

namespace BrainlessLabs.Neon.Tests
{
    public class SequenceFinishChallengeTests
    {
        private static readonly ATTACKTYPE[] PunchKick = { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK };

        [Test]
        public void ExpectedVerb_Advances()
        {
            var challenge = new SequenceFinishChallenge(PunchKick, inputWindowSeconds: 0.9f, startTime: 0f);

            bool completed = challenge.TryAdvance(ATTACKTYPE.PUNCH, 0.1f);

            Assert.IsFalse(completed);
            Assert.AreEqual(1, challenge.Progress);
            Assert.AreEqual(ATTACKTYPE.KICK, challenge.ExpectedVerb);
        }

        [Test]
        public void FullSequence_Completes()
        {
            var challenge = new SequenceFinishChallenge(PunchKick, 0.9f, 0f);

            challenge.TryAdvance(ATTACKTYPE.PUNCH, 0.1f);
            bool completed = challenge.TryAdvance(ATTACKTYPE.KICK, 0.5f);

            Assert.IsTrue(completed);
            Assert.IsTrue(challenge.IsComplete);
        }

        [Test]
        public void WrongVerb_MatchingSequenceStart_RestartsAtOne()
        {
            var challenge = new SequenceFinishChallenge(PunchKick, 0.9f, 0f);
            challenge.TryAdvance(ATTACKTYPE.PUNCH, 0.1f);

            challenge.TryAdvance(ATTACKTYPE.PUNCH, 0.3f); // expected KICK

            Assert.AreEqual(1, challenge.Progress); // the punch re-opens a fresh attempt
        }

        [Test]
        public void WrongVerb_NotSequenceStart_ResetsToZero()
        {
            var challenge = new SequenceFinishChallenge(PunchKick, 0.9f, 0f);
            challenge.TryAdvance(ATTACKTYPE.PUNCH, 0.1f);

            challenge.TryAdvance(ATTACKTYPE.WEAPON, 0.3f);

            Assert.AreEqual(0, challenge.Progress);
        }

        [Test]
        public void ExpiredWindow_ResetsBeforeEvaluating()
        {
            var challenge = new SequenceFinishChallenge(PunchKick, 0.9f, 0f);
            challenge.TryAdvance(ATTACKTYPE.PUNCH, 0.1f);

            bool completed = challenge.TryAdvance(ATTACKTYPE.KICK, 2f); // 1.9s later — stale

            Assert.IsFalse(completed);
            Assert.AreEqual(0, challenge.Progress); // KICK isn't the sequence start
        }
    }
}
```

- [ ] **Step 3: Implement the challenge**

`Assets/_neon/Scripts/Engagement/IFinishChallenge.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>
    /// A per-target, tier-scaled finish challenge (spec §5.1). Chaff use the
    /// implicit single-verb challenge inside the bridge; hero-tier uses
    /// SequenceFinishChallenge. Momentum pays out on COMPLETION only (v0.4).
    /// </summary>
    public interface IFinishChallenge
    {
        ATTACKTYPE ExpectedVerb { get; }
        int Progress { get; }
        int Total { get; }
        bool IsComplete { get; }

        /// <summary>Feed a landed verb hit. Returns true when this hit completes the challenge.</summary>
        bool TryAdvance(ATTACKTYPE verb, float gameplayNow);
    }
}
```

`Assets/_neon/Scripts/Engagement/SequenceFinishChallenge.cs`:

```csharp
using System.Collections.Generic;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Land the verb sequence on the target within a per-input window. A stale or
    /// wrong input resets — but a hit matching the sequence START re-opens a fresh
    /// attempt at step 1 (missed inputs shouldn't feel like a dead target).
    /// </summary>
    public sealed class SequenceFinishChallenge : IFinishChallenge
    {
        private readonly IReadOnlyList<ATTACKTYPE> _sequence;
        private readonly float _inputWindowSeconds;
        private float _lastAdvanceTime;

        public int Progress { get; private set; }
        public int Total => _sequence.Count;
        public bool IsComplete => Progress >= Total;
        public ATTACKTYPE ExpectedVerb => IsComplete ? ATTACKTYPE.NONE : _sequence[Progress];

        public SequenceFinishChallenge(IReadOnlyList<ATTACKTYPE> sequence, float inputWindowSeconds, float startTime)
        {
            _sequence = sequence;
            _inputWindowSeconds = inputWindowSeconds;
            _lastAdvanceTime = startTime;
        }

        public bool TryAdvance(ATTACKTYPE verb, float gameplayNow)
        {
            if (IsComplete) return false;

            bool expired = Progress > 0 && gameplayNow - _lastAdvanceTime > _inputWindowSeconds;
            if (expired || verb != ExpectedVerb)
            {
                Progress = verb == _sequence[0] ? 1 : 0;
                _lastAdvanceTime = gameplayNow;
                return IsComplete; // true only for degenerate 1-length sequences
            }

            Progress++;
            _lastAdvanceTime = gameplayNow;
            return IsComplete;
        }
    }
}
```

Run EditMode tests: the 5 challenge tests PASS (pure logic; nothing else compiled against it yet). Expected total: 87.

- [ ] **Step 4: Rewrite `FinishResolver`**

Replace `Assets/_neon/Scripts/Engagement/FinishResolver.cs` entirely with:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The observe-and-tag seam (spec §5.1/§5.3): watches the existing verbs through
    /// the static combat events; verbs stay untouched. M2 hero finish rule (P2): a
    /// Finish-Ready hero demands a landed verb SEQUENCE (2 inputs; 3 at Hot+, windows
    /// tightening per tier). Completing = instant finish + Momentum; dying mid-sequence
    /// = plain kill, no payout (v0.4). Chaff stay single-verb inside
    /// SwarmBridge.ApplyVerbHit. Whiffs publish VerbWhiffed + stagger the player.
    /// Stagger and the finish execution-kill are applied on the NEXT clock tick —
    /// both events fire inside state transitions / hit resolution, where a reentrant
    /// SetState / SubstractHealth would race the code that raised them.
    /// </summary>
    public sealed class FinishResolver : IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 25; // after the selector (20), before Momentum decay (30)
        private const float MIN_INPUT_WINDOW = 0.3f;

        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly IMomentumSystem _momentum;
        private readonly EngagementConfig _config;
        private readonly GrowthConfig _growth;
        private readonly Dictionary<GameObject, SequenceFinishChallenge> _challenges = new();
        private readonly List<GameObject> _pruneScratch = new();
        private UnitActions _pendingStagger;
        private GameObject _pendingFinishKill;

        public FinishResolver(IGameplayClock clock, IGameplaySignals signals,
            IMomentumSystem momentum, EngagementConfig config, GrowthConfig growth)
        {
            _clock = clock;
            _signals = signals;
            _momentum = momentum;
            _config = config;
            _growth = growth;
            UnitActions.onUnitDealDamage += HandleDamage;
            UnitActions.onVerbWhiffed += HandleWhiff;
            _clock.Register(this, TICK_ORDER);
        }

        public void Dispose()
        {
            UnitActions.onUnitDealDamage -= HandleDamage;
            UnitActions.onVerbWhiffed -= HandleWhiff;
            _clock.Unregister(this);
            _challenges.Clear();
        }

        public void Tick(float deltaTime)
        {
            if (_pendingStagger != null)
            {
                var stateMachine = _pendingStagger.UnitStateMachine;
                _pendingStagger = null;
                if (stateMachine != null) stateMachine.SetState(new PlayerWhiffStagger(_config.WhiffStaggerSeconds));
            }

            if (_pendingFinishKill != null)
            {
                var health = _pendingFinishKill.GetComponent<HealthSystem>();
                _pendingFinishKill = null;
                if (health != null && !health.isDead) health.SubstractHealth(health.currentHp);
            }

            PruneChallenges();
        }

        /// <summary>Public so EditMode tests can drive it (static events can't be raised externally).</summary>
        public void HandleDamage(GameObject recipient, AttackData attackData)
        {
            if (recipient == null || attackData?.inflictor == null) return;
            if (!attackData.inflictor.CompareTag("Player")) return;

            var marker = recipient.GetComponent<FinishReadyMarker>();
            if (marker == null || !marker.IsReady)
            {
                DropChallenge(recipient);
                return;
            }

            var sequence = _momentum.Tier >= MomentumTier.Hot
                ? _growth.ChallengeSequenceHot
                : _growth.ChallengeSequenceBase;
            float window = Mathf.Max(MIN_INPUT_WINDOW,
                _growth.ChallengeInputWindowSeconds - (int)_momentum.Tier * _growth.ChallengeWindowTightenPerTier);

            if (!_challenges.TryGetValue(recipient, out var challenge) || challenge.Total != sequence.Length)
            {
                challenge = new SequenceFinishChallenge(sequence, window, _clock.GameplayTime);
                _challenges[recipient] = challenge;
            }

            bool completed = challenge.TryAdvance(attackData.attackType, _clock.GameplayTime);
            var health = recipient.GetComponent<HealthSystem>();

            if (completed)
            {
                DropChallenge(recipient);
                _signals.Publish(new EnemyFinished(recipient.transform.position, wasChaff: false));
                // Execute the finish: the completed challenge kills outright (unless
                // this hit already did). Deferred to Tick — we're inside CheckForHit.
                if (health != null && !health.isDead) _pendingFinishKill = recipient;
                return;
            }

            if (health != null && health.isDead)
            {
                // Died mid-sequence: a plain kill, no Momentum (v0.4 pays on completion).
                DropChallenge(recipient);
                return;
            }

            _signals.Publish(new FinishChallengeChanged(true, recipient.transform.position,
                challenge.ExpectedVerb, challenge.Progress, challenge.Total));
        }

        /// <summary>Public so EditMode tests can drive it.</summary>
        public void HandleWhiff(UnitActions unit, ATTACKTYPE attackType)
        {
            if (unit == null || !unit.isPlayer) return;

            _signals.Publish(new VerbWhiffed(attackType));
            _pendingStagger = unit;
        }

        private void DropChallenge(GameObject target)
        {
            if (_challenges.Remove(target))
            {
                _signals.Publish(new FinishChallengeChanged(false, Vector2.zero, ATTACKTYPE.NONE, 0, 0));
            }
        }

        private void PruneChallenges()
        {
            if (_challenges.Count == 0) return;

            _pruneScratch.Clear();
            foreach (var pair in _challenges)
            {
                var target = pair.Key;
                if (target == null
                    || target.GetComponent<HealthSystem>()?.isDead == true
                    || target.GetComponent<FinishReadyMarker>()?.IsReady != true)
                {
                    _pruneScratch.Add(target);
                }
            }
            foreach (var stale in _pruneScratch) DropChallenge(stale);
        }
    }
}
```

(`Level.cs` needs no registration edit: VContainer resolves the new ctor deps — `IMomentumSystem` from the session scope, `GrowthConfig` from the Task 8 instance registration.)

- [ ] **Step 5: Rewrite the resolver tests**

Replace `Assets/_neon/Tests/EditMode/FinishResolverTests.cs` entirely with:

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
        private FakeMomentumSystem _momentum;
        private FinishResolver _resolver;
        private readonly List<GameObject> _spawned = new();
        private readonly List<EnemyFinished> _finishes = new();
        private readonly List<VerbWhiffed> _whiffs = new();
        private readonly List<FinishChallengeChanged> _challengeEvents = new();
        private System.IDisposable _finishSub;
        private System.IDisposable _whiffSub;
        private System.IDisposable _challengeSub;

        private static EngagementConfig EngagementTestConfig => new(
            ratePerSecond: 1.5f, chipDamage: 8, arcDegrees: 120f, range: 4f,
            finishReadyThreshold: 0.25f, finishReadyGlow: Color.yellow, whiffStaggerSeconds: 0.5f);

        private static GrowthConfig GrowthTestConfig => new(
            xpPerKill: 1, chargePerFinish: 2, overchargePerFinish: 8, overchargeCap: 100,
            xpCostBase: 10f, xpCostExponent: 1.35f, levelUpSlowMoScale: 0.1f,
            challengeSequenceBase: new[] { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK },
            challengeSequenceHot: new[] { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK, ATTACKTYPE.PUNCH },
            challengeInputWindowSeconds: 0.9f, challengeWindowTightenPerTier: 0.1f, finishAoeDamage: 6);

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _signals = new GameplaySignals();
            _momentum = new FakeMomentumSystem();
            _resolver = new FinishResolver(_clock, _signals, _momentum, EngagementTestConfig, GrowthTestConfig);
            _finishes.Clear();
            _whiffs.Clear();
            _challengeEvents.Clear();
            _finishSub = _signals.On<EnemyFinished>().Subscribe(e => _finishes.Add(e));
            _whiffSub = _signals.On<VerbWhiffed>().Subscribe(e => _whiffs.Add(e));
            _challengeSub = _signals.On<FinishChallengeChanged>().Subscribe(e => _challengeEvents.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            _finishSub?.Dispose();
            _whiffSub?.Dispose();
            _challengeSub?.Dispose();
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

        private (GameObject enemy, AttackData Punch, AttackData Kick) MakeReadyEnemy(int hp)
        {
            var enemy = Spawn("Enemy");
            var marker = enemy.AddComponent<FinishReadyMarker>();
            marker.SetReady(true, Color.yellow);
            var health = enemy.AddComponent<HealthSystem>();
            health.maxHp = 40;
            health.currentHp = hp;

            var inflictor = Spawn("Inflictor");
            inflictor.tag = "Player";
            var punch = new AttackData("p", 2, inflictor, ATTACKTYPE.PUNCH, knockdown: false);
            var kick = new AttackData("k", 2, inflictor, ATTACKTYPE.KICK, knockdown: false);
            return (enemy, punch, kick);
        }

        [Test]
        public void FirstMatchingHit_StartsChallenge()
        {
            var (enemy, punch, _) = MakeReadyEnemy(hp: 10);

            _resolver.HandleDamage(enemy, punch);

            Assert.AreEqual(0, _finishes.Count);
            Assert.IsTrue(_challengeEvents[_challengeEvents.Count - 1].Active);
            Assert.AreEqual(ATTACKTYPE.KICK, _challengeEvents[_challengeEvents.Count - 1].ExpectedVerb);
            Assert.AreEqual(1, _challengeEvents[_challengeEvents.Count - 1].Progress);
        }

        [Test]
        public void CompletedSequence_PublishesEnemyFinished()
        {
            var (enemy, punch, kick) = MakeReadyEnemy(hp: 10);

            _resolver.HandleDamage(enemy, punch);
            _resolver.HandleDamage(enemy, kick);

            Assert.AreEqual(1, _finishes.Count);
            Assert.IsFalse(_finishes[0].WasChaff);
            // The execution-kill runs on the next Tick at runtime — not invoked here
            // because HealthSystem.SubstractHealth touches the injected audio service.
        }

        [Test]
        public void WrongVerb_DoesNotComplete()
        {
            var (enemy, punch, _) = MakeReadyEnemy(hp: 10);

            _resolver.HandleDamage(enemy, punch);
            _resolver.HandleDamage(enemy, punch); // expected KICK

            Assert.AreEqual(0, _finishes.Count);
            Assert.AreEqual(1, _challengeEvents[_challengeEvents.Count - 1].Progress); // restarted at 1
        }

        [Test]
        public void HotTier_DemandsThreeInputs()
        {
            _momentum.Tier = MomentumTier.Hot;
            var (enemy, punch, kick) = MakeReadyEnemy(hp: 10);

            _resolver.HandleDamage(enemy, punch);
            _resolver.HandleDamage(enemy, kick);
            Assert.AreEqual(0, _finishes.Count); // 2/3

            _resolver.HandleDamage(enemy, punch);
            Assert.AreEqual(1, _finishes.Count);
        }

        [Test]
        public void DyingMidSequence_IsNotAFinish()
        {
            var (enemy, punch, _) = MakeReadyEnemy(hp: 10);
            enemy.GetComponent<HealthSystem>().currentHp = 0; // the hit killed before resolution

            _resolver.HandleDamage(enemy, punch);

            Assert.AreEqual(0, _finishes.Count);
        }

        [Test]
        public void NotReadyTarget_NoChallenge()
        {
            var (enemy, punch, _) = MakeReadyEnemy(hp: 10);
            enemy.GetComponent<FinishReadyMarker>().SetReady(false, Color.yellow);

            _resolver.HandleDamage(enemy, punch);

            Assert.AreEqual(0, _finishes.Count);
            Assert.AreEqual(0, _challengeEvents.Count);
        }

        [Test]
        public void EnemyInflictedHit_Ignored()
        {
            var (enemy, _, _) = MakeReadyEnemy(hp: 10);
            var enemyInflictor = Spawn("EnemyInflictor");
            var attack = new AttackData("e", 2, enemyInflictor, ATTACKTYPE.PUNCH, knockdown: false);

            _resolver.HandleDamage(enemy, attack);

            Assert.AreEqual(0, _challengeEvents.Count);
        }

        [Test]
        public void PlayerWhiff_PublishesVerbWhiffed()
        {
            var player = Spawn("Player");
            player.AddComponent<UnitSettings>();
            var actions = player.AddComponent<UnitActions>();

            _resolver.HandleWhiff(actions, ATTACKTYPE.KICK);

            Assert.AreEqual(1, _whiffs.Count);
            Assert.AreEqual(ATTACKTYPE.KICK, _whiffs[0].AttackType);
        }

        [Test]
        public void EnemyWhiff_Ignored()
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

- [ ] **Step 6: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: **90/90 PASS** (82 + 5 challenge + resolver 6→9 = +3).

- [ ] **Step 7: Prompt shows the challenge**

In `Assets/_neon/Scripts/UI/UIHUDFinishPrompt.cs`:

**(a)** change:

```csharp
        [Inject] private IGameplaySignals _signals;
        private IDisposable _subscription;
        private bool hasTarget;
        private Vector2 targetPosition;
```

to:

```csharp
        [Inject] private IGameplaySignals _signals;
        private IDisposable _subscription;
        private IDisposable _challengeSubscription;
        private bool hasTarget;
        private bool challengeActive;
        private Vector2 targetPosition;
```

**(b)** change:

```csharp
            _subscription = _signals.On<FinishReadyPromptChanged>().Subscribe(Apply);
```

to:

```csharp
            _subscription = _signals.On<FinishReadyPromptChanged>().Subscribe(Apply);
            _challengeSubscription = _signals.On<FinishChallengeChanged>().Subscribe(ApplyChallenge);
```

**(c)** change:

```csharp
        void OnDestroy(){
            _subscription?.Dispose();
        }
```

to:

```csharp
        void OnDestroy(){
            _subscription?.Dispose();
            _challengeSubscription?.Dispose();
        }
```

**(d)** change:

```csharp
        void Apply(FinishReadyPromptChanged prompt){
            hasTarget = prompt.HasTarget;
```

to:

```csharp
        void Apply(FinishReadyPromptChanged prompt){
            if(challengeActive) return; //an active hero challenge owns the prompt
            hasTarget = prompt.HasTarget;
```

**(e)** append after the `Apply` method:

```csharp

        void ApplyChallenge(FinishChallengeChanged challenge){
            challengeActive = challenge.Active;
            if(!challenge.Active) return; //the next selector publish reasserts the normal prompt

            hasTarget = true;
            targetPosition = challenge.TargetPosition;
            if(promptRoot != null) promptRoot.gameObject.SetActive(true);
            if(verbLabel != null) verbLabel.text = $"{challenge.ExpectedVerb} {challenge.Progress}/{challenge.Total}";
            if(countLabel != null) countLabel.text = "FINISH!";
        }
```

- [ ] **Step 8: Runtime gate (Recipe 4)**

Boot into Level1, trigger a wave, chip a hero to gold:
1. Punch it → prompt flips to **KICK 1/2** with "FINISH!". Kick → the enemy dies instantly, Momentum steps.
2. Punch-punch (wrong second verb) → progress restarts; the target stays gold at 1 HP thanks to Task 1's chip floor.
3. Push Momentum to Hot (chaff finishes), soften another hero → the challenge reads x/3.
4. Chaff finishes still single-punch throughout. Exit; zero errors.

- [ ] **Step 9: Commit**

```bash
git add "Assets/_neon/Scripts/Engagement/IFinishChallenge.cs" "Assets/_neon/Scripts/Engagement/IFinishChallenge.cs.meta" "Assets/_neon/Scripts/Engagement/SequenceFinishChallenge.cs" "Assets/_neon/Scripts/Engagement/SequenceFinishChallenge.cs.meta" "Assets/_neon/Scripts/Engagement/FinishResolver.cs" "Assets/_neon/Tests/EditMode/Fakes.cs" "Assets/_neon/Tests/EditMode/FinishResolverTests.cs" "Assets/_neon/Tests/EditMode/SequenceFinishChallengeTests.cs" "Assets/_neon/Tests/EditMode/SequenceFinishChallengeTests.cs.meta" "Assets/_neon/Scripts/UI/UIHUDFinishPrompt.cs"
git commit -m "feat: tiered hero finish challenge - verb sequences, Hot escalation (M2)"
```

---

### Task 12: M2 gate — the build snowballs, Overdrive screams

Spec §7 M2 gate + protocol doc §8.6 instrumentation questions. Runtime is ground truth; the *scream* judgment is Sebastien's.

- [ ] **Step 1: Full test suite**

Expected: **90/90 PASS** (23 M0 + 25 M1 + 42 M2 — record the exact split).

- [ ] **Step 2: Snowball run (one continuous 8–10 min session in Level1)**

Record in the gate record:
1. **Draft cadence:** level-ups in the first 5 minutes (doc target ≈3–4 per encounter-length; flag if the ⌈10·N^1.35⌉ exponent starves or floods — §8.6 Q2, "first number to instrument").
2. **Picks change play visibly:** Wide Sweep (wider chip arc), Overclocked Coil (faster chips + smaller HP pool), Vampiric Cadence (sustain), Concussive Finish (splash deaths around finishes).
3. **Gating proof, eyes-on:** *Redline Governor* never drafts before *Afterburner* is taken; after taking Afterburner, it eventually appears (EditMode already proves it deterministically).
4. **Hero challenges:** 2-input at Cool/Warm, 3-input at Hot+; dying mid-sequence pays no Momentum; whiff/stagger still behaves in the mix.
5. **Overdrive screams (the gate's word):** with 4+ protocols stacked, hold Overdrive — chip cadence/damage and splash visibly ~2.5–3× the fresh-build baseline (§8.1 target ceiling reads as *screaming*, not trivial). **Sebastien's hands-on verdict required.**
6. FPS spot-check at ChaffCap 150 during a level-up-heavy fight (no regression from ~197 avg).

- [ ] **Step 3: Gate record + push**

Append `## M2 gate record` to the bottom of THIS document (date, machine, exact test count, cadence numbers, the six answers above, deviations encountered), then:

```bash
git add "docs/superpowers/plans/2026-07-04-neon-engine-base-plan3-m2-growth.md"
git commit -m "docs: record M2 gate (snowball + Overdrive scream)"
git push -u origin claude/neon-m2-growth
```

- [ ] **Step 4: Hand off**

Report the gate record to Sebastien. **Plan 4 (M3: `RunService` encounter FSM, `RebootNodeObjective`, `ISignalSystem`, shop beat) is written after this gate** — it consumes the run-reset seams that now exist (`ProtocolService` stacks, economy ledgers, `Level.SlowMotionRoutine` migration onto the clock) and the protocol doc's Signal-scaled draft weights.

---

## Deviations from the design docs (deliberate, M2-scoped)

1. **Iron Grip is duration-only** — the doc adds "no squirm-free", but no squirm/struggle mechanic exists in the grab code; nothing to disable until one does.
2. **Challenge sequences validate against landed verb HITS** (`attackData.attackType` at the hit seam), not raw `comboData` combo-matching — same input language the combo engine drives, but validation lives with the finish seam. The spec's "validated via the existing comboData engine" is honored in spirit (the combo engine is what lets PUNCH→KICK chain at all); revisit if design wants challenge sequences authored as `Combo` assets.
3. **Weapon hits reset hero challenges** (WEAPON is in neither sequence) — acceptable while Scavenger protocols are out of scope.
4. **Blacksite rarity, N-of-family and pair gates are unimplemented** — the M2 set needs only `Requires:`-gating; the rest lands with the first Blacksite (M3+).
5. **Finish execution-kill and whiff stagger are one-clock-tick deferred** (reentrancy: the events fire inside `CheckForHit`/`SetState`).
6. **Stack diminishing via two modifier lists** (`FirstCopyModifiers`/`AdditionalCopyModifiers`) — preserves the doc's exact per-copy values (Vampiric +2 then +1; Concussive +0.8 then +0.2).
7. **`Level.SlowMotionRoutine` still writes `Time.timeScale` directly**; the clock writes only on change so they coexist. Migrates onto a clock scale source with M3's `RunService`.
8. **Challenge expiry is evaluated lazily** at the next landed hit (no per-frame countdown publish); the prompt shows the last-known progress until then.
9. **`Quick Ignition` deferred** — "start each encounter at Warm" has no meaning until M3 introduces encounters.

## Spec coverage self-check (for reviewers)

- Spec §7 M2: `IEconomySystem` Momentum-multiplied ✅ (Task 5) · `ProtocolDefinitionAsset` + `IProgressionSystem` with slow-mo 1-of-3 picker ✅ (Tasks 2/6/9/10) · 6–8 protocols incl. ≥1 Brawler + ≥1 Momentum ✅ (Task 7: 8 total — 1 Brawler, 3 Momentum) · tiered `IFinishChallenge` via escalating sequences ✅ (Task 11) · gate: snowball + Overdrive screams + EditMode tests for economy & protocol stacking ✅ (Tasks 5/6/12).
- Spec §5.3 architecture: protocols are modifier bundles on `IStatSystem` ✅ · "adding a Protocol is authoring a ScriptableObject" ✅ (Task 7 wrote zero system code) · economy reads the gain multiplier blindly ✅ · level-up slow-mo via `IGameplayClock` ✅ (Tasks 3/9).
- Protocol doc §8: exact §8.4 values ✅ · §8.2 base draft weights ✅ (Signal scaling deferred to M3 with the Signal itself) · §8.1 guardrails: no protocol Mult ✅, auto-rate/arc caps already enforced ✅, HP floor 50 ✅ (Task 8), threshold untouched ✅, Momentum finishing-only ✅.
- Spec §10 non-negotiables: DI-bootstrap gates ✅ · VContainer-only ✅ · FSM-state/Level-scope registration ✅ · verbs unchanged (Task 1's whiff fix is accuracy, not behavior) ✅ · no legacy ✅ · runtime gates per task ✅ · no invented APIs (all seams re-read post-M1) ✅ · assembly direction untouched ✅.
- M1 gate flags: all four addressed (Tasks 1 and 4) ✅.
- Deferred to M3/M4 (per plan series): RunService/encounters, objective, Signal (+ Signal-scaled rarity), shop, Specials, Overcharge *spending*, run-reset of stacks/ledgers, remaining 28 catalog protocols.

---

## M2 gate record

**Date:** 2026-07-04 · **Machine:** Sebastien's Windows 11 dev box (G:\Brainless Labs\neon-responder) · **Executor:** Claude (executing-plans, in-session) · **Branch:** `claude/neon-m2-growth` (12 commits off `master` @ 074a6ea)

**Tests:** **90/90 EditMode PASS** (`BrainlessLabs.Neon.Tests.EditMode`) — 23 M0 + 25 M1 + 42 M2 (MomentumKnob 6 · Economy 6 · ProtocolService 8 · ProtocolEffects 7 · Progression 7 · SequenceChallenge 5 · FinishResolver 9, rewritten from 6). Suite green after every task; every task's expected count matched the plan exactly.

**Runtime verification (agent-driven via editor scripting; hands-on feel pass PENDING — see item 5):**

1. **Draft cadence:** thresholds land at L2@10 / L3@36 / L4@81 / L5@151 total XP (1 XP/kill × GainMultiplier, fractional-remainder accurate). Live run: a 123-kill sweep produced LV4 + banked drafts served strictly one-at-a-time. Real-fight cadence (target ≈3–4 per encounter-length) needs the hands-on session — §8.6 Q2 stays the first number to instrument.
2. **Picks change play:** *Wide Sweep* verified live — arc stat 120°→150° the frame it was picked. *Concussive Finish* / *Vampiric Cadence* stacked live; their AoE/heal paths are EditMode-verified (heal not observable at full HP in the check run).
3. **Gating:** *Redline Governor* never appeared in any live draft before *Afterburner*; EditMode proves gate + unlock deterministically (20-roll sweep + post-acquire availability).
4. **Hero challenges:** 2-input PUNCH→KICK verified live end-to-end: prompt overrides to "KICK 1/2 · FINISH!", completion publishes `EnemyFinished` (charge +2 / overcharge +8 paid through the economy), execution-kill lands on the next clock tick through the real death flow. **Window expiry verified live** (a stale second input resets the attempt; the target stays gold at 1 HP thanks to the Task 1 chip floor). 3-input Hot escalation and died-mid-sequence-no-payout: EditMode-proven.
5. **Overdrive screams:** **PENDING — Sebastien's hands-on verdict required** (with 4+ protocols stacked, hold Overdrive; chip cadence/damage and splash should read ~2.5–3× fresh-build baseline).
6. **FPS:** **234 avg over 300 frames at ChaffCap 150** (worst frame 19.9 ms), editor **unfocused** — no regression vs the M1 ~197 focused baseline (number not directly comparable, but comfortably above).

**Additional live checks:** level-up slow-mo drops `Time.timeScale` to 0.1 through `GameplayClock` (whole-world, spec §4.1) and releases on `Choose` (write-on-change, next tick). Boot chain clean with all three new session services registered. M1-flag payoffs live: hero-tier chips floor at 1/40 HP and hold gold; player 100 HP; wave enemies 40 HP; degenerate no-hitbox attacks no longer log whiffs (the hitbox-gated guard suppressed a synthetic zero-window punch — the exact anomaly class from the M1 gate).

**Deviations encountered:** none beyond the plan's own deviation list — every landed-code signature the plan predicted matched at execution time. The whiff hands-on air-check folds into the pending feel pass (real whiffs provably still fire: real swings activate the hitbox, which is a precondition for the damage M1 verified hands-on).
