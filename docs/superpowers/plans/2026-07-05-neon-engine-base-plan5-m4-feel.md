# Neon Engine Base — Plan 5: M4 Actives + Finishers + Feel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Land the MVP's feel + actives layer (spec §5.3/§5.5, §7 M4) — **Siren Pulse** (cooldown+Charge active that manufactures a Finish-Ready wave), the **Overcharge finisher** (meter-gated screen-clear that spends the meter M2 already fills), **per-verb hitstop/shake profiles**, tier-up flourish, whiff record-scratch, "NODE RESTORED" callouts, +N XP/Charge popups, finisher freeze-frame, and **audio layering by Momentum tier + Signal** — closing the engine-base series with the full vertical slice screaming.

**Architecture:** Feedback is the spec's Layer 5 — a **pure consumer of `IGameplaySignals` + `IGameplayClock`**; systems stay headless. New actives (`ISpecialSystem`, the finisher) are Level-scoped services whose *logic* (cooldown, Charge/meter gates) is EditMode-tested, and whose *world effect* is delegated to the existing `SwarmBridge` (mass Finish-Ready / screen-clear) + existing combat states (hero knockdown) — **no verb behavior changes**. All hitstop/slow-mo/freeze routes through the clock's scale sources (which M2 made own `Time.timeScale`). View-layer pieces (hitstop pulse, `CameraShake`, floating text, audio) are scene MonoBehaviours using **unscaled** time so they read correctly while gameplay is frozen.

**Scope boundary (decision F3 + Feel & Level overlap):** M4 does the **systemic** feel + **functional** HUD for the new meters. **Cosmetic** HUD polish — verb-glyph art, meter restyling, QTE-prompt redesign — **defers to the Feel & Level pass** (`docs/superpowers/plans/2026-07-04-feel-and-level-pass-pre-brief.md`), which redoes them against the delivered Level 01 design + real sprites. Do that art **once**.

**Tech Stack:** unchanged — Unity 6000.3.5f2 (**built-in RP** — no post-processing stack; see whiff deviation), VContainer, R3, UnityHFSM, Entities 1.4.4, Input System (generated `PlayerControls`), EditMode NUnit, uGUI.

**Spec:** `docs/superpowers/specs/2026-07-04-neon-engine-base-design.md` §5.3 (Specials/Overcharge), §5.5 (feedback), §7 M4
**Design input:** `docs/rgd/special-moves-v0.1.md` (Siren Pulse = the MVP Special: Pulse type, radial stun+knockback+reveal, mass-triggers Finish-Ready, cd 6s / Charge 20; Overcharge distinct from Specials — meter-gated, R4 "clears chaff not bosses")
**Prior state:** M3 gate signed off + merged to master (`plan4` doc §"M3 gate record") — 117/117 tests, full run ends on dawn. Carried hands-on flags (M2 Overdrive-scream, M3 run-length/legibility) ride into this plan's gate.
**Branch:** create `claude/neon-m4-feel` off `master` (master tip `c37b467` contains all M0–M3).

---

## Decisions locked (Sebastien, 2026-07-05)

| Fork | Decision |
|---|---|
| **F1 — HUD/QTE-UI polish scope** | **Systemic feel only.** M4 builds engine-level feedback (hitstop/shake/freeze, callouts, +N popups, audio, tier flourish, actives) + **functional** HUD for the Special cooldown & Overcharge-ready meters. Cosmetic polish (verb glyphs, meter restyling, QTE-prompt redesign) **defers to Feel & Level** so HUD art is authored once against the real level/sprites. |
| **F2 — Overcharge finisher trigger** | **Manual button, gated on a full meter.** A dedicated finisher input, usable only when Overcharge is full; firing consumes the meter. Player owns the timing; distinct from the cooldown Special (GDD §6.9). |
| **F3 — audio depth** | **Track-swap + stingers.** A small `AudioService` extension: crossfade between intensity tracks by a combined Momentum-tier + Signal "heat" band, plus one-shot stingers (tier-up, finisher, node-restored). Falls back to stingers-only if only one music track exists. Multi-stem layering defers. |

## Assumptions (stated per Sebastien's rules — correct before execution if wrong)

- **MVP actives = exactly two:** Siren Pulse (the one Special, cooldown+Charge) + the Overcharge finisher (meter-gated). No shop-bought Specials, ranks, or family overlays (those are the special-moves doc's full-game layer; the M3 shop stays Heal+Continue — a later pass adds Special stock to the same screen).
- **Siren Pulse effect** = within a radius: chaff → mass Finish-Ready (via a new bridge method, no kill); hero-tier → knockdown (existing `UnitKnockDown` state, which the M1 `FinishReadySystem` already reads as Finish-Ready via the staggered path). "Reveal" = a brief light flash (feedback), not a fog-of-war system (none exists).
- **Overcharge finisher effect** = screen-radius chaff **finish-clear** (killed as finishes → mass Momentum + Charge/Overcharge churn = the "scream") + a brief full freeze-frame; the continuing swarm flood "refreshes the field." Tuned to chaff only (R4) — hero-tier take a knockdown, not death.
- **Whiff "desaturate" is deferred** — the project runs **built-in RP with no post-processing stack** (render-pipeline memory), so a true fullscreen desaturate needs infra out of scope. M4 ships the whiff **record-scratch SFX + a quick red uGUI vignette flash**; true desaturate folds into the Feel & Level pass. Documented deviation.
- **Feedback lives in `03_Level1`** for the gate; when the Feel & Level pass builds Level 01, these scene components move with the HUD (they're pure consumers, so they port cleanly).

---

## Landed-code facts this plan builds on (verified 2026-07-05, post-M3)

- `IEconomySystem`: `Xp`/`NeonCharge`/`Overcharge` getters + `TrySpend(int)` (M3). **No Overcharge consume** — Task 2 adds `TryConsumeOvercharge()`. `Overcharge` is capped (`OverchargeCap`, GrowthSettings); "full" = `Overcharge >= OverchargeCap`.
- `ISwarmBridge`: `TryGetNearestHot/FinishReady`, `CountHot/FinishReady`, `ApplyChip`, `ApplyVerbHit`, `ApplyAreaDamage(center,radius,damage)` (M2). Task 4 adds `MassFinishReady(center,radius)` + `FinishAllChaff(center,radius)`. `NullSwarmBridge` + `FakeSwarmBridge` must gain the same members.
- `IGameplayClock`: `SetScale(source,scale)` / `ClearScale(source)` own `Time.timeScale` (M2). Hitstop/freeze = a scale source; **release must use unscaled time** (scaled dt ≈ 0 during freeze) → feedback view components use `Time.unscaledDeltaTime`/coroutines, not the clock tick.
- `CameraShake` (on the main camera, `RequireComponent(CameraFollow)`): `ShowCamShake()` + `ShowCamShake(float intensity, float duration)` — per-verb profiles call the overload. `UnitActions.CamShake()` already reaches it via `Camera.main`.
- `IInputService` / `InputService` wrap a generated `PlayerControls` asset (`Assets/_neon/Scripts/Input/PlayerControls.inputactions`). Adding Special/Finisher = new actions in the asset (regenerates `PlayerControls.cs`) + new `IInputService` members. Existing pattern: one `InputAction` field per verb, `WasPressedThisFrame()`.
- `IAudioService`: `PlaySFX(name,pos?,parent?)`, `GetSFXDuration(name)`, `PlayMusic(name)`. Track-swap needs a small additive method (Task 10); stingers use `PlaySFX`.
- Signals already present (M0–M3): `EnemyFinished`, `MomentumTierChanged`, `VerbWhiffed`, `FinishReadyPromptChanged`, `FinishChallengeChanged`, `XpGained`, `NeonChargeChanged`, `OverchargeChanged`, `ObjectiveProgress`, `ObjectiveCompleted`, `SignalChanged`, `RunPhaseChanged`, `RunEnded`, `ProtocolAcquired`, `PlayerLevelChanged`, `XpProgressChanged`, `LevelUpChoicesReady`. `EnemyFinished` carries `Position` + `WasChaff`; `UnitActions.onUnitDealDamage(recipient, attackData)` + `onVerbWhiffed(unit, attackType)` are the static combat seams FeedbackSystem reads (same as `FinishResolver`).
- `UnitKnockDown` state ctor: `new UnitKnockDown(AttackData attackData, float forceX, float forceY)` (M1). `EntitiesService.GetByType(UNITTYPE.ENEMY)` enumerates hero-tier for radius effects.
- Level-scoped systems register in `Level.RegisterEngagementSystems` + eager-resolve in the `Configure` build callback (the M1–M3 pattern). Session services register in `GameplayServicesState`.

---

## Working agreements (read once, apply to every task)

1. **Unity import + zero console errors before every test/play step.** Editing `PlayerControls.inputactions` regenerates `PlayerControls.cs` — wait for that compile.
2. **EditMode tests:** Test Runner → EditMode → Run All (or `mcp__unityMCP__run_tests`, `mode: "EditMode"`), scoped to `BrainlessLabs.Neon.Tests.EditMode`. The 117 M0–M3 tests stay green throughout.
3. **Play-testing = Recipe 4**: boot with Post-Bootstrap Scene = `SceneDefinition_Level1`. No git/asset writes in Play mode. `BootstrapSettingsAsset` boot-target flips stay uncommitted.
4. **Commits include `.meta` files**; every commit body ends with `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
5. **Do not touch:** `WaveManager`, `04_Level2`/`05_Level3`, `ApplicationLifetimeScope`, `Assets/Addons/`, `Assets/_neon/Spikes/`, `Assets/_generated/`, `GeneratedAssets/`.
6. **Verbs stay behaviorally unchanged.** Actives invoke existing states (`UnitKnockDown`) and the bridge; feedback only *observes*. No edits to `PlayerAttack`/`PlayerWeaponAttack`/etc. this plan.
7. **Guardrails (protocol doc §8.1):** Momentum stays the only global multiplier — no active/feedback introduces a `Mult`. The finisher's mass finishes step Momentum through the normal `EnemyFinished` path (that's the intended scream, not a new multiplier). R4: the finisher clears chaff, never hero-tier/boss.
8. **Feel is hands-on-judged.** Every feel task has a runtime play-test; the *quality* verdict (does it feel good) is Sebastien's, like the M2 Overdrive-scream / M3 run-length flags. Tests cover logic, not juice.
9. If any landed signature mismatches this plan at execution time, re-read the source file before adapting.

---

## File structure

**Created — actives + feedback (`BrainlessLabs.Neon`):**

| Path | Responsibility |
|---|---|
| `Assets/_neon/Scripts/Feel/FeelSettings.cs` | Per-verb hitstop/shake profiles + flourish/whiff/finisher/audio knobs (`ISettings`) |
| `Assets/_neon/Scripts/Feel/FeelSettingsAsset.cs` | `BaseSettingsAsset` wrapper |
| `Assets/_neon/Scripts/Feel/FeelConfig.cs` | Asset-free snapshot + `HitProfile` struct + profile-selection helper |
| `Assets/_neon/Scripts/Feel/FeedbackSystem.cs` | Scene MB: hitstop (clock) + `CameraShake` per verb/finish, tier flourish, whiff scratch (unscaled) |
| `Assets/_neon/Scripts/Feel/FloatingTextSpawner.cs` | Scene MB: +N popups (XP/Charge) + callouts ("NODE RESTORED") |
| `Assets/_neon/Scripts/Feel/SignalMusicDirector.cs` | Scene MB: track-swap by tier+Signal heat band + stingers |
| `Assets/_neon/Scripts/Specials/ISpecialSystem.cs` | Special surface (`CanActivate`, `CooldownNormalized`, `TryActivate`) |
| `Assets/_neon/Scripts/Specials/SpecialSystem.cs` | Siren Pulse: input, cd + Charge gate, effect via bridge + hero knockdown |
| `Assets/_neon/Scripts/Specials/IOverchargeFinisher.cs` | Finisher surface (`IsReady`, `TryFire`) |
| `Assets/_neon/Scripts/Specials/OverchargeFinisher.cs` | Meter-gate + consume, screen-clear via bridge, freeze-frame |
| `Assets/_neon/Scripts/Specials/SpecialConfig.cs` | Asset-free snapshot (cd, cost, radii) |
| `Assets/_neon/Scripts/UI/UIHUDSpecialMeter.cs` | Functional Special-cooldown + Overcharge-ready indicators (consumer) |

**Created — audio + tests:** `AudioService.CrossfadeMusic` (extend existing), `EconomyOverchargeTests.cs`, `SpecialSystemTests.cs`, `OverchargeFinisherTests.cs`, `FeelProfileTests.cs`.

**Modified:**

| Path | Change |
|---|---|
| `Assets/_neon/Scripts/Growth/IEconomySystem.cs` + `EconomySystem.cs` | `TryConsumeOvercharge()` |
| `Assets/_neon/Scripts/Swarm/ISwarmBridge.cs` + `NullSwarmBridge.cs` + `SwarmBridge.cs` | `MassFinishReady` + `FinishAllChaff` |
| `Assets/_neon/Scripts/Input/PlayerControls.inputactions` (+ regenerated `PlayerControls.cs`) | Special + Finisher actions |
| `Assets/_neon/Scripts/Input/IInputService.cs` + `InputService.cs` | `SpecialKeyDown` + `FinisherKeyDown` |
| `Assets/_neon/Scripts/Signals/GameplayEvents.cs` | `SpecialStateChanged`, `OverchargeReadyChanged`, `Callout`, `OverchargeFinisherFired` |
| `Assets/_neon/Scripts/Audio/IAudioService.cs` + `AudioService.cs` | `CrossfadeMusic(name, seconds)` |
| `Assets/_neon/Scripts/Level/Level.cs` | Register + eager-resolve `SpecialSystem` + `OverchargeFinisher` |
| `Assets/_neon/Scripts/Editor/SettingsAssetCreator.cs` | Add `FeelSettingsAsset` |
| `Assets/_neon/Tests/EditMode/Fakes.cs` | Bridge fakes gain `MassFinishReady`/`FinishAllChaff`; `FakeInputService` |
| `03_Level1` scene + `LevelConfiguration_Level1` | Feedback MBs, HUD meters, audio director (Task 11) |

---

### Task 1: Branch + data layer (signals, FeelSettings/Config + HitProfile, SpecialConfig)

Pure data. Compile-and-commit.

**Files:**
- Modify: `Assets/_neon/Scripts/Signals/GameplayEvents.cs`
- Create: `Assets/_neon/Scripts/Feel/FeelSettings.cs`
- Create: `Assets/_neon/Scripts/Feel/FeelSettingsAsset.cs`
- Create: `Assets/_neon/Scripts/Feel/FeelConfig.cs`
- Create: `Assets/_neon/Scripts/Specials/SpecialConfig.cs`
- Modify: `Assets/_neon/Scripts/Editor/SettingsAssetCreator.cs`

- [ ] **Step 1: Branch**

```bash
git -C "G:/Brainless Labs/neon-responder" checkout master
git checkout -b claude/neon-m4-feel
```

- [ ] **Step 2: Append M4 signals**

Append to `Assets/_neon/Scripts/Signals/GameplayEvents.cs`, inside the namespace after the last struct:

```csharp

    /// <summary>Special (Siren Pulse) cooldown/availability for the HUD. 1 = ready.</summary>
    public readonly struct SpecialStateChanged
    {
        public readonly bool Ready;
        public readonly float CooldownNormalized; // 0 (just fired) → 1 (ready)

        public SpecialStateChanged(bool ready, float cooldownNormalized)
        {
            Ready = ready;
            CooldownNormalized = cooldownNormalized;
        }
    }

    /// <summary>The Overcharge finisher became available / unavailable (meter full ↔ spent).</summary>
    public readonly struct OverchargeReadyChanged
    {
        public readonly bool Ready;

        public OverchargeReadyChanged(bool ready)
        {
            Ready = ready;
        }
    }

    /// <summary>The Overcharge finisher fired (feedback + audio hook). Count = chaff cleared.</summary>
    public readonly struct OverchargeFinisherFired
    {
        public readonly Vector2 Position;
        public readonly int ChaffCleared;

        public OverchargeFinisherFired(Vector2 position, int chaffCleared)
        {
            Position = position;
            ChaffCleared = chaffCleared;
        }
    }

    /// <summary>A floating world-space callout ("NODE RESTORED", "SIREN PULSE"). Feedback consumes it.</summary>
    public readonly struct Callout
    {
        public readonly string Text;
        public readonly Vector2 Position;

        public Callout(string text, Vector2 position)
        {
            Text = text;
            Position = position;
        }
    }
```

- [ ] **Step 3: The hit profile + feel settings**

`Assets/_neon/Scripts/Feel/FeelConfig.cs` (the shared runtime types + the testable selection helper):

```csharp
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>One feedback profile: a hitstop dip (clock scale for a moment) + a camera shake.</summary>
    [System.Serializable]
    public struct HitProfile
    {
        [Range(0f, 1f)] public float HitstopScale;   // 0.05 = near-freeze; 1 = none
        public float HitstopSeconds;                  // unscaled
        public float ShakeIntensity;
        public float ShakeSeconds;

        public HitProfile(float hitstopScale, float hitstopSeconds, float shakeIntensity, float shakeSeconds)
        {
            HitstopScale = hitstopScale;
            HitstopSeconds = hitstopSeconds;
            ShakeIntensity = shakeIntensity;
            ShakeSeconds = shakeSeconds;
        }
    }

    /// <summary>Asset-free snapshot of FeelSettings + the per-verb profile selector (EditMode-testable).</summary>
    public readonly struct FeelConfig
    {
        public readonly HitProfile Punch;
        public readonly HitProfile Kick;
        public readonly HitProfile Weapon;
        public readonly HitProfile Throw;    // throw-enemy = the biggest hit in the kit (§0.4.f)
        public readonly HitProfile DefaultHit;
        public readonly HitProfile Finish;
        public readonly HitProfile TierUp;
        public readonly float FinisherFreezeSeconds;
        public readonly float WhiffFlashSeconds;

        public FeelConfig(HitProfile punch, HitProfile kick, HitProfile weapon, HitProfile @throw,
            HitProfile defaultHit, HitProfile finish, HitProfile tierUp,
            float finisherFreezeSeconds, float whiffFlashSeconds)
        {
            Punch = punch; Kick = kick; Weapon = weapon; Throw = @throw;
            DefaultHit = defaultHit; Finish = finish; TierUp = tierUp;
            FinisherFreezeSeconds = finisherFreezeSeconds; WhiffFlashSeconds = whiffFlashSeconds;
        }

        /// <summary>Pick the profile for a landed verb (throw is the heaviest; grab-punch/kick map to their base).</summary>
        public HitProfile ProfileForVerb(ATTACKTYPE attackType)
        {
            switch (attackType)
            {
                case ATTACKTYPE.PUNCH:
                case ATTACKTYPE.GRABPUNCH:
                    return Punch;
                case ATTACKTYPE.KICK:
                case ATTACKTYPE.GRABKICK:
                    return Kick;
                case ATTACKTYPE.WEAPON:
                    return Weapon;
                case ATTACKTYPE.GRABTHROW:
                    return Throw;
                default:
                    return DefaultHit;
            }
        }

        public static FeelConfig FromSettings()
        {
            var s = FeelSettingsAsset.InstanceAsset.Settings;
            return new FeelConfig(s.Punch, s.Kick, s.Weapon, s.Throw, s.DefaultHit, s.Finish, s.TierUp,
                s.FinisherFreezeSeconds, s.WhiffFlashSeconds);
        }
    }
}
```

`Assets/_neon/Scripts/Feel/FeelSettings.cs`:

```csharp
using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Feel + actives tuning (spec §5.5). Per-verb hitstop/shake profiles, tier-up
    /// flourish, whiff flash, finisher freeze, and the Actives knobs (Siren Pulse +
    /// Overcharge finisher). All playtest starting values.
    /// </summary>
    [Serializable]
    public class FeelSettings : ISettings
    {
        [Header("Per-verb hit profiles (hitstopScale, hitstopSeconds, shakeIntensity, shakeSeconds)")]
        [SerializeField] private HitProfile _punch = new(0.08f, 0.04f, 0.10f, 0.15f);
        [SerializeField] private HitProfile _kick = new(0.06f, 0.06f, 0.16f, 0.20f);
        [SerializeField] private HitProfile _weapon = new(0.05f, 0.07f, 0.20f, 0.22f);
        [SerializeField] private HitProfile _throw = new(0.03f, 0.10f, 0.35f, 0.35f); // biggest hit in the kit
        [SerializeField] private HitProfile _defaultHit = new(0.10f, 0.03f, 0.08f, 0.12f);
        [SerializeField] private HitProfile _finish = new(0.04f, 0.09f, 0.28f, 0.30f);

        [Header("Flourishes")]
        [SerializeField] private HitProfile _tierUp = new(0.15f, 0.08f, 0.22f, 0.30f);
        [SerializeField] private float _finisherFreezeSeconds = 0.35f;
        [SerializeField] private float _whiffFlashSeconds = 0.25f;

        [Header("Actives — Siren Pulse (special-moves doc §2)")]
        [SerializeField] private float _sirenCooldownSeconds = 6f;
        [SerializeField] private int _sirenChargeCost = 20;
        [SerializeField] private float _sirenRadius = 5f;

        [Header("Actives — Overcharge finisher (meter-gated; R4 clears chaff)")]
        [SerializeField] private float _finisherRadius = 12f; // screen-ish

        public HitProfile Punch => _punch;
        public HitProfile Kick => _kick;
        public HitProfile Weapon => _weapon;
        public HitProfile Throw => _throw;
        public HitProfile DefaultHit => _defaultHit;
        public HitProfile Finish => _finish;
        public HitProfile TierUp => _tierUp;
        public float FinisherFreezeSeconds => _finisherFreezeSeconds;
        public float WhiffFlashSeconds => _whiffFlashSeconds;
        public float SirenCooldownSeconds => _sirenCooldownSeconds;
        public int SirenChargeCost => _sirenChargeCost;
        public float SirenRadius => _sirenRadius;
        public float FinisherRadius => _finisherRadius;

#if UNITY_EDITOR
        public void Editor_OnGUI(UnityEngine.Object target)
        {
            var serializedObject = new UnityEditor.SerializedObject(target);
            serializedObject.UpdateIfRequiredOrScript();
            var settingsProperty = serializedObject.FindProperty("_settings");
            UnityEditor.EditorGUILayout.PropertyField(settingsProperty, new GUIContent("Feel Settings"), includeChildren: true);
            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}
```

`Assets/_neon/Scripts/Feel/FeelSettingsAsset.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    public class FeelSettingsAsset : BaseSettingsAsset<FeelSettingsAsset, FeelSettings> { }
}
```

- [ ] **Step 4: Actives config snapshot**

`Assets/_neon/Scripts/Specials/SpecialConfig.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>Asset-free snapshot of the actives tuning (EditMode-testable systems).</summary>
    public readonly struct SpecialConfig
    {
        public readonly float SirenCooldownSeconds;
        public readonly int SirenChargeCost;
        public readonly float SirenRadius;
        public readonly float FinisherRadius;
        public readonly float FinisherFreezeSeconds;

        public SpecialConfig(float sirenCooldownSeconds, int sirenChargeCost, float sirenRadius,
            float finisherRadius, float finisherFreezeSeconds)
        {
            SirenCooldownSeconds = sirenCooldownSeconds;
            SirenChargeCost = sirenChargeCost;
            SirenRadius = sirenRadius;
            FinisherRadius = finisherRadius;
            FinisherFreezeSeconds = finisherFreezeSeconds;
        }

        public static SpecialConfig FromSettings()
        {
            var s = FeelSettingsAsset.InstanceAsset.Settings;
            return new SpecialConfig(s.SirenCooldownSeconds, s.SirenChargeCost, s.SirenRadius,
                s.FinisherRadius, s.FinisherFreezeSeconds);
        }
    }
}
```

- [ ] **Step 5: Add FeelSettingsAsset to the creator**

In `Assets/_neon/Scripts/Editor/SettingsAssetCreator.cs`, add before `AssetDatabase.SaveAssets();`:

```csharp
            FeelSettingsAsset.GetOrCreateSettingsAsset();
```

- [ ] **Step 6: Compile, create asset, tests, commit**

1. Refresh Unity: zero errors, 117 tests PASS.
2. Run **Neon → Settings → Create All Settings Assets** — confirm `Assets/Resources/Settings/FeelSettingsAsset.asset` appears.
3. Commit:

```bash
git add "Assets/_neon/Scripts/Signals/GameplayEvents.cs" "Assets/_neon/Scripts/Feel" "Assets/_neon/Scripts/Feel.meta" "Assets/_neon/Scripts/Specials" "Assets/_neon/Scripts/Specials.meta" "Assets/_neon/Scripts/Editor/SettingsAssetCreator.cs" "Assets/Resources/Settings/FeelSettingsAsset.asset" "Assets/Resources/Settings/FeelSettingsAsset.asset.meta"
git commit -m "feat: M4 data layer - feel profiles, actives config, signals"
```

---

### Task 2: `EconomySystem.TryConsumeOvercharge` (test-first)

The finisher's meter gate + spend. "Full" = `Overcharge >= OverchargeCap`; firing zeroes the meter.

**Files:**
- Modify: `Assets/_neon/Scripts/Growth/IEconomySystem.cs`
- Modify: `Assets/_neon/Scripts/Growth/EconomySystem.cs`
- Test: `Assets/_neon/Tests/EditMode/EconomyOverchargeTests.cs`

- [ ] **Step 1: Write the failing tests**

`Assets/_neon/Tests/EditMode/EconomyOverchargeTests.cs`:

```csharp
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class EconomyOverchargeTests
    {
        private GameplaySignals _signals;
        private StatSystem _stats;
        private EconomySystem _economy;

        // overchargePerFinish 8, cap 24 → full after 3 finishes.
        private static GrowthConfig TestConfig => new(
            xpPerKill: 1, chargePerFinish: 2, overchargePerFinish: 8, overchargeCap: 24,
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

        private void Finishes(int n)
        {
            for (int i = 0; i < n; i++) _signals.Publish(new EnemyFinished(Vector2.zero, wasChaff: true));
        }

        [Test]
        public void NotFull_ConsumeFails_NoChange()
        {
            Finishes(2); // 16 < 24

            Assert.IsFalse(_economy.TryConsumeOvercharge());
            Assert.AreEqual(16, _economy.Overcharge);
        }

        [Test]
        public void Full_ConsumeSucceeds_Zeroes()
        {
            Finishes(3); // 24 (capped)
            Assert.AreEqual(24, _economy.Overcharge);

            Assert.IsTrue(_economy.TryConsumeOvercharge());
            Assert.AreEqual(0, _economy.Overcharge);
        }

        [Test]
        public void Consume_PublishesOverchargeChanged()
        {
            Finishes(3);
            int last = -1;
            using var sub = _signals.On<OverchargeChanged>().Subscribe(e => last = e.Value);

            _economy.TryConsumeOvercharge();

            Assert.AreEqual(0, last);
        }

        [Test]
        public void RefillsAfterConsume()
        {
            Finishes(3);
            _economy.TryConsumeOvercharge();
            Finishes(3);

            Assert.AreEqual(24, _economy.Overcharge);
            Assert.IsTrue(_economy.TryConsumeOvercharge());
        }
    }
}
```

- [ ] **Step 2: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`TryConsumeOvercharge` does not exist). Proceed.

- [ ] **Step 3: Implement**

In `Assets/_neon/Scripts/Growth/IEconomySystem.cs`, add to the interface:

```csharp
        /// <summary>Fire the Overcharge finisher: if the meter is full, zero it and return true.</summary>
        bool TryConsumeOvercharge();
```

In `Assets/_neon/Scripts/Growth/EconomySystem.cs`, add the method (the cap lives in `_config.OverchargeCap`):

```csharp
        public bool TryConsumeOvercharge()
        {
            if (Overcharge < _config.OverchargeCap) return false;

            Overcharge = 0;
            _overchargeFraction = 0f;
            _signals.Publish(new OverchargeChanged(Overcharge, _config.OverchargeCap));
            return true;
        }
```

- [ ] **Step 4: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: **121/121 PASS** (117 + 4).

- [ ] **Step 5: Commit**

```bash
git add "Assets/_neon/Scripts/Growth/IEconomySystem.cs" "Assets/_neon/Scripts/Growth/EconomySystem.cs" "Assets/_neon/Tests/EditMode/EconomyOverchargeTests.cs" "Assets/_neon/Tests/EditMode/EconomyOverchargeTests.cs.meta"
git commit -m "feat: EconomySystem.TryConsumeOvercharge - the finisher's meter gate (M4)"
```

---

### Task 3: Input — Special + Finisher actions

Two new actions on the generated `PlayerControls` asset + two new `IInputService` members. Editor asset edit (regenerates `PlayerControls.cs`) + code.

**Files:**
- Modify: `Assets/_neon/Scripts/Input/PlayerControls.inputactions` (editor)
- Modify: `Assets/_neon/Scripts/Input/IInputService.cs`
- Modify: `Assets/_neon/Scripts/Input/InputService.cs`
- Modify: `Assets/_neon/Tests/EditMode/Fakes.cs` (add `FakeInputService`)

- [ ] **Step 1: Add the actions (editor)**

Open `Assets/_neon/Scripts/Input/PlayerControls.inputactions` in the Input Actions editor. In the **Player** action map add two **Button** actions:
- **Special** — binding: keyboard **A**, gamepad **Left Shoulder** (matches special-moves doc §1 "A / LB").
- **Finisher** — binding: keyboard **S**, gamepad **Left Trigger** (doc §1 "S / LT").

Click **Save Asset** (with "Generate C# Class" already enabled on this asset, it regenerates `PlayerControls.cs` with `Player.Special` / `Player.Finisher`). Wait for the recompile; confirm zero console errors.

> If the asset does not have "Generate C# Class" enabled (check its importer inspector), enable it, point it at the existing `PlayerControls.cs` path, and Apply — the existing `InputService` already depends on the generated class, so it's on.

- [ ] **Step 2: Extend the interface**

In `Assets/_neon/Scripts/Input/IInputService.cs`, add to the interface:

```csharp
        bool SpecialKeyDown(int playerId);
        bool FinisherKeyDown(int playerId);
```

- [ ] **Step 3: Wire the implementation**

In `Assets/_neon/Scripts/Input/InputService.cs`:

Add fields alongside the others:

```csharp
        private readonly InputAction _special;
        private readonly InputAction _finisher;
```

In the constructor, after `_jump = _playerInput.Player.Jump;`:

```csharp
            _special = _playerInput.Player.Special;
            _finisher = _playerInput.Player.Finisher;
```

after `_jump.Enable();`:

```csharp
            _special.Enable();
            _finisher.Enable();
```

Add the methods (mirror `PunchKeyDown` — edge-triggered):

```csharp
        public bool SpecialKeyDown(int playerId)
        {
            return _special?.WasPressedThisFrame() ?? false;
        }

        public bool FinisherKeyDown(int playerId)
        {
            return _finisher?.WasPressedThisFrame() ?? false;
        }
```

and in `Dispose`, after `_jump.Disable();`:

```csharp
            _special.Disable();
            _finisher.Disable();
```

- [ ] **Step 4: Add `FakeInputService` for tests**

Append to `Assets/_neon/Tests/EditMode/Fakes.cs` inside the namespace:

```csharp

    internal sealed class FakeInputService : IInputService
    {
        public bool Special;
        public bool Finisher;

        public bool PunchKeyDown(int playerId) => false;
        public bool KickKeyDown(int playerId) => false;
        public bool DefendKeyDown(int playerId) => false;
        public bool GrabKeyDown(int playerId) => false;
        public bool JumpKeyDown(int playerId) => false;
        public UnityEngine.Vector2 GetInputVector(int playerId) => UnityEngine.Vector2.zero;
        public bool JoypadDirInputDetected(int playerId) => false;

        // Consumed-on-read, mirroring WasPressedThisFrame edge semantics.
        public bool SpecialKeyDown(int playerId) { bool v = Special; Special = false; return v; }
        public bool FinisherKeyDown(int playerId) { bool v = Finisher; Finisher = false; return v; }
    }
```

- [ ] **Step 5: Compile + tests + commit**

Refresh Unity: zero errors, 121 tests PASS. Boot into Level1 (Recipe 4), tap A and S — nothing happens yet (no consumer), but no errors and input resolves. Exit.

```bash
git add "Assets/_neon/Scripts/Input/PlayerControls.inputactions" "Assets/_neon/Scripts/Input/PlayerControls.cs" "Assets/_neon/Scripts/Input/IInputService.cs" "Assets/_neon/Scripts/Input/InputService.cs" "Assets/_neon/Tests/EditMode/Fakes.cs"
git commit -m "feat: input - Special (A/LB) + Finisher (S/LT) actions (M4)"
```

---

### Task 4: Bridge — `MassFinishReady` + `FinishAllChaff`

Two additive bridge methods the actives call. `MassFinishReady` drops in-radius chaff to the Finish-Ready threshold (Siren Pulse — no kill); `FinishAllChaff` kills in-radius chaff **as finishes** (the finisher's screen-clear — publishes `EnemyFinished` per kill so Momentum/economy churn = the scream). Both return the count affected.

**Files:**
- Modify: `Assets/_neon/Scripts/Swarm/ISwarmBridge.cs`
- Modify: `Assets/_neon/Scripts/Swarm/NullSwarmBridge.cs`
- Modify: `Assets/_neon/Scripts/Swarm/SwarmBridge.cs`
- Modify: `Assets/_neon/Tests/EditMode/Fakes.cs` (bridge fake gains the two members)

- [ ] **Step 1: Interface + null**

In `ISwarmBridge.cs`, append inside the interface after `ApplyAreaDamage`:

```csharp

        /// <summary>Drop in-radius chaff to the Finish-Ready threshold (no kill) — Siren Pulse. Returns count.</summary>
        int MassFinishReady(Vector2 center, float radius);

        /// <summary>Kill in-radius chaff AS finishes (each publishes EnemyFinished) — the Overcharge finisher. Returns count.</summary>
        int FinishAllChaff(Vector2 center, float radius);
```

In `NullSwarmBridge.cs`, append inside the class:

```csharp

        public int MassFinishReady(Vector2 center, float radius) => 0;
        public int FinishAllChaff(Vector2 center, float radius) => 0;
```

- [ ] **Step 2: Real bridge**

In `SwarmBridge.cs`, append after `ApplyAreaDamage` (before `TryInitialize`). These mirror `ApplyAreaDamage`'s query pattern; `MassFinishReady` sets each chaff's `SwarmHealth.Current` to the finish-ready threshold via a damage command sized to leave it exactly at/under threshold, and `FinishAllChaff` reuses the kill path + the finish publish that `ApplyVerbHit` already uses for ready chaff.

```csharp
        public int MassFinishReady(Vector2 center, float radius)
        {
            if (!_initialized || radius <= 0f) return 0;

            using var entities = _chaffQuery.ToEntityArray(Allocator.Temp);
            using var positions = _chaffQuery.ToComponentDataArray<BeltPosition>(Allocator.Temp);
            using var healths = _chaffQuery.ToComponentDataArray<SwarmHealth>(Allocator.Temp);

            var centerF = new float2(center.x, center.y);
            float radiusSq = radius * radius;
            var damageBuffer = _world.EntityManager.GetBuffer<SwarmDamageCommand>(_controlEntity);
            int count = 0;

            for (int i = 0; i < entities.Length; i++)
            {
                if (math.lengthsq(positions[i].Value - centerF) > radiusSq) continue;

                int threshold = Mathf.Max(1, Mathf.CeilToInt(healths[i].Max * _config.FinishReadyThreshold));
                if (healths[i].Current <= threshold) { count++; continue; } // already ready

                // Non-chip damage that lands it exactly at the threshold (FinishReadyEvalSystem flips the tag next tick).
                int damage = healths[i].Current - threshold;
                damageBuffer.Add(new SwarmDamageCommand { Target = entities[i], Amount = damage, IsChip = 0 });
                count++;
            }
            return count;
        }

        public int FinishAllChaff(Vector2 center, float radius)
        {
            if (!_initialized || radius <= 0f) return 0;

            using var entities = _chaffQuery.ToEntityArray(Allocator.Temp);
            using var positions = _chaffQuery.ToComponentDataArray<BeltPosition>(Allocator.Temp);

            var centerF = new float2(center.x, center.y);
            float radiusSq = radius * radius;
            var killBuffer = _world.EntityManager.GetBuffer<SwarmKillCommand>(_controlEntity);
            int count = 0;

            for (int i = 0; i < entities.Length; i++)
            {
                var p = positions[i].Value;
                if (math.lengthsq(p - centerF) > radiusSq) continue;

                killBuffer.Add(new SwarmKillCommand { Target = entities[i] });
                _signals.Publish(new EnemyFinished(new Vector2(p.x, p.y), wasChaff: true)); // finisher = mass finishes (the scream)
                count++;
            }
            return count;
        }
```

(These reference `SwarmHealth`, `SwarmDamageCommand`, `SwarmKillCommand`, `_config.FinishReadyThreshold` — all existing from M1. `Mathf`/`float2` already imported in `SwarmBridge`.)

- [ ] **Step 3: Fake bridge (for the actives' EditMode tests)**

In `Assets/_neon/Tests/EditMode/Fakes.cs`, add to `FakeSwarmBridge` (after the `ApplyAreaDamage` recorder from M2):

```csharp

        public readonly List<(Vector2 Center, float Radius)> MassFinishReadyCalls = new();
        public readonly List<(Vector2 Center, float Radius)> FinishAllChaffCalls = new();
        public int MassFinishReadyReturn;
        public int FinishAllChaffReturn;

        public int MassFinishReady(Vector2 center, float radius)
        {
            MassFinishReadyCalls.Add((center, radius));
            return MassFinishReadyReturn;
        }

        public int FinishAllChaff(Vector2 center, float radius)
        {
            FinishAllChaffCalls.Add((center, radius));
            return FinishAllChaffReturn;
        }
```

- [ ] **Step 4: Compile + tests + commit**

Refresh Unity: zero errors, 121 tests PASS (bridge isn't EditMode-covered directly; the fake keeps the actives tests compiling). Runtime effect verified in Tasks 5–6.

```bash
git add "Assets/_neon/Scripts/Swarm/ISwarmBridge.cs" "Assets/_neon/Scripts/Swarm/NullSwarmBridge.cs" "Assets/_neon/Scripts/Swarm/SwarmBridge.cs" "Assets/_neon/Tests/EditMode/Fakes.cs"
git commit -m "feat: bridge MassFinishReady + FinishAllChaff for the actives (M4)"
```

---

### Task 5: `ISpecialSystem` — Siren Pulse (test-first)

The MVP Special (special-moves doc §2): a cooldown+Charge active that manufactures a Finish-Ready wave. Level-scoped service on the clock (order 45, after the run). Logic (input → gate on cooldown + Charge → spend → effect → cooldown) is EditMode-tested with fakes; the world effect (bridge `MassFinishReady` + hero knockdown) is driven from the same method and verified at runtime.

**Files:**
- Create: `Assets/_neon/Scripts/Specials/ISpecialSystem.cs`
- Create: `Assets/_neon/Scripts/Specials/SpecialSystem.cs`
- Modify: `Assets/_neon/Scripts/Level/Level.cs`
- Test: `Assets/_neon/Tests/EditMode/SpecialSystemTests.cs`

- [ ] **Step 1: Write the failing tests**

`Assets/_neon/Tests/EditMode/SpecialSystemTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class SpecialSystemTests
    {
        private GameplayClock _clock;
        private GameplaySignals _signals;
        private FakeEntitiesService _entities;
        private FakeSwarmBridge _bridge;
        private FakeInputService _input;
        private FakeEconomy _economy;
        private SpecialSystem _special;
        private GameObject _player;

        private static SpecialConfig TestConfig => new(
            sirenCooldownSeconds: 6f, sirenChargeCost: 20, sirenRadius: 5f,
            finisherRadius: 12f, finisherFreezeSeconds: 0.35f);

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _signals = new GameplaySignals();
            _entities = new FakeEntitiesService();
            _bridge = new FakeSwarmBridge();
            _input = new FakeInputService();
            _economy = new FakeEconomy { NeonChargeValue = 100 };
            _player = new GameObject("Player");
            _entities.Register(_player, UNITTYPE.PLAYER);
            _special = new SpecialSystem(_clock, _signals, _entities, _bridge, _input, _economy, TestConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _special.Dispose();
            _signals.Dispose();
            Object.DestroyImmediate(_player);
        }

        [Test]
        public void StartsReady()
        {
            Assert.IsTrue(_special.CanActivate);
            Assert.AreEqual(1f, _special.CooldownNormalized, 0.0001f);
        }

        [Test]
        public void Activate_SpendsCharge_FiresMassFinishReady()
        {
            _input.Special = true;
            _clock.Advance(0.016f);

            Assert.AreEqual(80, _economy.NeonChargeValue);          // 100 - 20
            Assert.AreEqual(1, _bridge.MassFinishReadyCalls.Count);
            Assert.AreEqual(5f, _bridge.MassFinishReadyCalls[0].Radius, 0.0001f);
            Assert.IsFalse(_special.CanActivate);                    // now on cooldown
        }

        [Test]
        public void Activate_WhenTooPoor_DoesNothing()
        {
            _economy.NeonChargeValue = 10; // < 20
            _input.Special = true;
            _clock.Advance(0.016f);

            Assert.AreEqual(10, _economy.NeonChargeValue);
            Assert.AreEqual(0, _bridge.MassFinishReadyCalls.Count);
            Assert.IsTrue(_special.CanActivate); // stayed ready
        }

        [Test]
        public void Cooldown_BlocksReactivation_ThenRecovers()
        {
            _input.Special = true;
            _clock.Advance(0.016f);   // fired
            _input.Special = true;
            _clock.Advance(0.016f);   // still cooling — ignored
            Assert.AreEqual(1, _bridge.MassFinishReadyCalls.Count);

            _clock.Advance(6f);       // full cooldown
            Assert.IsTrue(_special.CanActivate);

            _input.Special = true;
            _clock.Advance(0.016f);
            Assert.AreEqual(2, _bridge.MassFinishReadyCalls.Count);
        }

        [Test]
        public void PublishesSpecialStateChanged_OnFireAndRecover()
        {
            var states = new List<SpecialStateChanged>();
            using var sub = _signals.On<SpecialStateChanged>().Subscribe(states.Add);

            _input.Special = true;
            _clock.Advance(0.016f);              // fire → Ready false
            _clock.Advance(6f);                  // recover → Ready true

            Assert.IsTrue(states.Count >= 2);
            Assert.IsFalse(states[0].Ready);     // first emit on fire
            Assert.IsTrue(states[states.Count - 1].Ready);
        }

        [Test]
        public void PublishesCallout_OnFire()
        {
            Callout got = default;
            using var sub = _signals.On<Callout>().Subscribe(e => got = e);

            _input.Special = true;
            _clock.Advance(0.016f);

            Assert.AreEqual("SIREN PULSE", got.Text);
        }
    }
}
```

This needs a `FakeEconomy`. Add it to `Fakes.cs`:

```csharp

    internal sealed class FakeEconomy : IEconomySystem
    {
        public int XpValue;
        public int NeonChargeValue;
        public int OverchargeValue;
        public bool OverchargeFull;

        public int Xp => XpValue;
        public int NeonCharge => NeonChargeValue;
        public int Overcharge => OverchargeValue;

        public bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (NeonChargeValue < amount) return false;
            NeonChargeValue -= amount;
            return true;
        }

        public bool TryConsumeOvercharge()
        {
            if (!OverchargeFull) return false;
            OverchargeValue = 0;
            OverchargeFull = false;
            return true;
        }
    }
```

- [ ] **Step 2: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`SpecialSystem` does not exist). Proceed.

- [ ] **Step 3: Implement**

`Assets/_neon/Scripts/Specials/ISpecialSystem.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The MVP Special — Siren Pulse (spec §5.3, special-moves doc §2): a cooldown +
    /// Neon Charge active that manufactures a Finish-Ready wave. Grants no Momentum
    /// itself — the finishes it enables do (v0.4 rule).
    /// </summary>
    public interface ISpecialSystem
    {
        bool CanActivate { get; }
        float CooldownNormalized { get; } // 0 (just fired) → 1 (ready)
    }
}
```

`Assets/_neon/Scripts/Specials/SpecialSystem.cs`:

```csharp
using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    public sealed class SpecialSystem : ISpecialSystem, IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 45; // after RunService (40)
        private const float KNOCKDOWN_FORCE_X = 2f;
        private const float KNOCKDOWN_FORCE_Y = 2f;

        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly IEntitiesService _entities;
        private readonly ISwarmBridge _bridge;
        private readonly IInputService _input;
        private readonly IEconomySystem _economy;
        private readonly SpecialConfig _config;

        private float _cooldownRemaining;

        public bool CanActivate => _cooldownRemaining <= 0f && _economy.NeonCharge >= _config.SirenChargeCost;
        public float CooldownNormalized =>
            _config.SirenCooldownSeconds <= 0f ? 1f : 1f - Mathf.Clamp01(_cooldownRemaining / _config.SirenCooldownSeconds);

        public SpecialSystem(IGameplayClock clock, IGameplaySignals signals, IEntitiesService entities,
            ISwarmBridge bridge, IInputService input, IEconomySystem economy, SpecialConfig config)
        {
            _clock = clock;
            _signals = signals;
            _entities = entities;
            _bridge = bridge;
            _input = input;
            _economy = economy;
            _config = config;
            _clock.Register(this, TICK_ORDER);
        }

        public void Dispose() => _clock.Unregister(this);

        public void Tick(float deltaTime)
        {
            bool wasReady = _cooldownRemaining <= 0f;
            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);
                if (_cooldownRemaining <= 0f) PublishState(); // recovered
            }

            if (_input.SpecialKeyDown(1)) TryActivate();

            // Keep the meter live for the HUD while cooling (cheap; only when relevant).
            if (!wasReady && _cooldownRemaining > 0f) PublishState();
        }

        private void TryActivate()
        {
            if (_cooldownRemaining > 0f) return;
            if (!_economy.TrySpend(_config.SirenChargeCost)) return;

            var player = _entities.GetFirstByType(UNITTYPE.PLAYER).GameObject;
            Vector2 origin = player != null ? (Vector2)player.transform.position : Vector2.zero;

            // Chaff → mass Finish-Ready (bridge). Hero-tier → knockdown (→ Finish-Ready
            // via the staggered path FinishReadySystem already reads). No verb change.
            _bridge.MassFinishReady(origin, _config.SirenRadius);
            KnockdownHeroesInRadius(origin, _config.SirenRadius);

            _cooldownRemaining = _config.SirenCooldownSeconds;
            _signals.Publish(new Callout("SIREN PULSE", origin));
            PublishState();
        }

        private void KnockdownHeroesInRadius(Vector2 center, float radius)
        {
            float radiusSq = radius * radius;
            var enemies = _entities.GetByType(UNITTYPE.ENEMY);
            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i].GameObject;
                if (go == null) continue;
                if (((Vector2)go.transform.position - center).sqrMagnitude > radiusSq) continue;

                var settings = go.GetComponent<UnitSettings>();
                if (settings == null || !settings.canBeKnockedDown) continue;
                var stateMachine = go.GetComponent<UnitStateMachine>();
                if (stateMachine == null) continue;
                if (stateMachine.GetCurrentState() is UnitKnockDown) continue;

                var pulse = new AttackData("SirenPulse", 0, go, ATTACKTYPE.NONE, knockdown: true);
                stateMachine.SetState(new UnitKnockDown(pulse, KNOCKDOWN_FORCE_X, KNOCKDOWN_FORCE_Y));
            }
        }

        private void PublishState() => _signals.Publish(new SpecialStateChanged(CanActivate, CooldownNormalized));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: **127/127 PASS** (121 + 6).

- [ ] **Step 5: Register in the Level scope**

In `Level.RegisterEngagementSystems`, after the `RunService` registration add:

```csharp
            builder.RegisterInstance(SpecialConfig.FromSettings());
            builder.Register<SpecialSystem>(Lifetime.Scoped).As<ISpecialSystem>();
```

In `Level.Configure`'s build callback eager-resolve block, after `container.Resolve<IRunService>();` add:

```csharp
                    container.Resolve<ISpecialSystem>();
```

- [ ] **Step 6: Runtime gate (Recipe 4)**

Boot into Level1, build some Neon Charge (finish chaff), then press **A**:
- Nearby chaff snap to gold (Finish-Ready); nearby hero enemies flop into knockdown (→ gold).
- Charge drops by 20; pressing A again immediately does nothing (cooldown); after ~6s it works again.
- "SIREN PULSE" callout fires (visible once Task 9's callout spawner is in; for now confirm no errors + the mass-finish-ready effect).
- Exit. Zero errors.

- [ ] **Step 7: Commit**

```bash
git add "Assets/_neon/Scripts/Specials/ISpecialSystem.cs" "Assets/_neon/Scripts/Specials/ISpecialSystem.cs.meta" "Assets/_neon/Scripts/Specials/SpecialSystem.cs" "Assets/_neon/Scripts/Specials/SpecialSystem.cs.meta" "Assets/_neon/Tests/EditMode/SpecialSystemTests.cs" "Assets/_neon/Tests/EditMode/SpecialSystemTests.cs.meta" "Assets/_neon/Tests/EditMode/Fakes.cs" "Assets/_neon/Scripts/Level/Level.cs"
git commit -m "feat: SpecialSystem - Siren Pulse manufactures a Finish-Ready wave (M4)"
```

---

### Task 6: `OverchargeFinisher` — meter-gated screen-clear (test-first)

The finisher (spec §5.3, F2 manual): when Overcharge is full, the Finisher key clears chaff in a screen radius (as finishes → the scream) + a brief freeze-frame. Consumes the meter. R4: chaff only (heroes take a knockdown, reused from Siren Pulse's helper path — kept simple here as chaff-clear + freeze; heroes are untouched by the clear).

**Files:**
- Create: `Assets/_neon/Scripts/Specials/IOverchargeFinisher.cs`
- Create: `Assets/_neon/Scripts/Specials/OverchargeFinisher.cs`
- Modify: `Assets/_neon/Scripts/Level/Level.cs`
- Test: `Assets/_neon/Tests/EditMode/OverchargeFinisherTests.cs`

- [ ] **Step 1: Write the failing tests**

`Assets/_neon/Tests/EditMode/OverchargeFinisherTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class OverchargeFinisherTests
    {
        private GameplayClock _clock;
        private GameplaySignals _signals;
        private FakeEntitiesService _entities;
        private FakeSwarmBridge _bridge;
        private FakeInputService _input;
        private FakeEconomy _economy;
        private OverchargeFinisher _finisher;
        private GameObject _player;

        private static SpecialConfig TestConfig => new(
            sirenCooldownSeconds: 6f, sirenChargeCost: 20, sirenRadius: 5f,
            finisherRadius: 12f, finisherFreezeSeconds: 0.35f);

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _signals = new GameplaySignals();
            _entities = new FakeEntitiesService();
            _bridge = new FakeSwarmBridge { FinishAllChaffReturn = 42 };
            _input = new FakeInputService();
            _economy = new FakeEconomy { OverchargeFull = true };
            _player = new GameObject("Player");
            _entities.Register(_player, UNITTYPE.PLAYER);
            _finisher = new OverchargeFinisher(_clock, _signals, _entities, _bridge, _input, _economy, TestConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _finisher.Dispose();
            _signals.Dispose();
            Object.DestroyImmediate(_player);
        }

        [Test]
        public void Ready_WhenMeterFull()
        {
            Assert.IsTrue(_finisher.IsReady);
        }

        [Test]
        public void Fire_WhenReady_ClearsChaff_ConsumesMeter()
        {
            _input.Finisher = true;
            _clock.Advance(0.016f);

            Assert.AreEqual(1, _bridge.FinishAllChaffCalls.Count);
            Assert.AreEqual(12f, _bridge.FinishAllChaffCalls[0].Radius, 0.0001f);
            Assert.IsFalse(_economy.OverchargeFull); // consumed
            Assert.IsFalse(_finisher.IsReady);
        }

        [Test]
        public void Fire_WhenNotFull_DoesNothing()
        {
            _economy.OverchargeFull = false;
            _input.Finisher = true;
            _clock.Advance(0.016f);

            Assert.AreEqual(0, _bridge.FinishAllChaffCalls.Count);
        }

        [Test]
        public void Fire_AppliesFreezeFrame_ThenReleases()
        {
            _input.Finisher = true;
            _clock.Advance(0.016f);

            Assert.AreEqual(0f, _clock.EffectiveScale, 0.0001f); // frozen

            _finisher.Tick(0f); // simulate the unscaled release path (drives the timer)
            // Freeze release is unscaled-timed at runtime; the finisher exposes ReleaseFreezeForTest.
            _finisher.ReleaseFreezeForTest();
            Assert.AreEqual(1f, _clock.EffectiveScale, 0.0001f);
        }

        [Test]
        public void PublishesFinisherFired()
        {
            OverchargeFinisherFired got = default;
            using var sub = _signals.On<OverchargeFinisherFired>().Subscribe(e => got = e);

            _input.Finisher = true;
            _clock.Advance(0.016f);

            Assert.AreEqual(42, got.ChaffCleared);
        }

        [Test]
        public void PublishesReadyChanged_OnConsume()
        {
            var readies = new List<OverchargeReadyChanged>();
            using var sub = _signals.On<OverchargeReadyChanged>().Subscribe(readies.Add);

            _input.Finisher = true;
            _clock.Advance(0.016f);

            Assert.IsTrue(readies.Count >= 1);
            Assert.IsFalse(readies[readies.Count - 1].Ready);
        }
    }
}
```

- [ ] **Step 2: Verify the failing state**

Refresh Unity. Expected: COMPILE ERROR (`OverchargeFinisher` does not exist). Proceed.

- [ ] **Step 3: Add the readiness member to the economy**

`IsReady` needs "is the meter full?" *without consuming*. The interface only exposes `Overcharge`, not the cap — so add a readiness member to `IEconomySystem` (cleaner than probing).

In `Assets/_neon/Scripts/Growth/IEconomySystem.cs`, add:

```csharp
        /// <summary>True when the Overcharge meter is at cap (the finisher's gate).</summary>
        bool IsOverchargeFull { get; }
```

In `Assets/_neon/Scripts/Growth/EconomySystem.cs`, add the property (cap lives in `_config.OverchargeCap`):

```csharp
        public bool IsOverchargeFull => Overcharge >= _config.OverchargeCap;
```

In `Assets/_neon/Tests/EditMode/Fakes.cs`, add to `FakeEconomy`:

```csharp
        public bool IsOverchargeFull => OverchargeFull;
```

- [ ] **Step 4: Implement the finisher**

`Assets/_neon/Scripts/Specials/IOverchargeFinisher.cs`:

```csharp
namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The Overcharge finisher (spec §5.3, GDD §6.9): a meter-gated screen-clear,
    /// distinct from the cooldown Special. Manual-fire when full (F2). R4: clears chaff.
    /// </summary>
    public interface IOverchargeFinisher
    {
        bool IsReady { get; }
    }
}
```

`Assets/_neon/Scripts/Specials/OverchargeFinisher.cs`:

```csharp
using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    public sealed class OverchargeFinisher : IOverchargeFinisher, IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 46; // after SpecialSystem (45)

        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly IEntitiesService _entities;
        private readonly ISwarmBridge _bridge;
        private readonly IInputService _input;
        private readonly IEconomySystem _economy;
        private readonly SpecialConfig _config;
        private readonly ModifierSource _freezeSource = ModifierSource.Create("finisher-freeze");

        private bool _freezing;
        private float _freezeReleaseAt; // unscaled time
        private bool _lastReady;

        public bool IsReady => _economy.IsOverchargeFull;

        public OverchargeFinisher(IGameplayClock clock, IGameplaySignals signals, IEntitiesService entities,
            ISwarmBridge bridge, IInputService input, IEconomySystem economy, SpecialConfig config)
        {
            _clock = clock;
            _signals = signals;
            _entities = entities;
            _bridge = bridge;
            _input = input;
            _economy = economy;
            _config = config;
            _clock.Register(this, TICK_ORDER);
            _lastReady = IsReady;
        }

        public void Dispose()
        {
            _clock.Unregister(this);
            _clock.ClearScale(_freezeSource);
        }

        public void Tick(float deltaTime)
        {
            // Ready-edge → HUD.
            bool ready = IsReady;
            if (ready != _lastReady) { _lastReady = ready; _signals.Publish(new OverchargeReadyChanged(ready)); }

            // Unscaled freeze release (scaled dt is 0 during the freeze).
            if (_freezing && Time.unscaledTime >= _freezeReleaseAt) ReleaseFreeze();

            if (_input.FinisherKeyDown(1)) TryFire();
        }

        private void TryFire()
        {
            if (!_economy.TryConsumeOvercharge()) return;

            var player = _entities.GetFirstByType(UNITTYPE.PLAYER).GameObject;
            Vector2 origin = player != null ? (Vector2)player.transform.position : Vector2.zero;

            int cleared = _bridge.FinishAllChaff(origin, _config.FinisherRadius);

            _clock.SetScale(_freezeSource, 0f);      // freeze-frame
            _freezing = true;
            _freezeReleaseAt = Time.unscaledTime + _config.FinisherFreezeSeconds;

            _signals.Publish(new OverchargeFinisherFired(origin, cleared));
            _signals.Publish(new Callout("OVERCHARGE", origin));
            _signals.Publish(new OverchargeReadyChanged(false));
            _lastReady = false;
        }

        private void ReleaseFreeze()
        {
            _freezing = false;
            _clock.ClearScale(_freezeSource);
        }

        // Test seam: the freeze release is unscaled-time-gated (Time.unscaledTime),
        // which doesn't advance deterministically in EditMode — release directly.
        public void ReleaseFreezeForTest() => ReleaseFreeze();
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Refresh Unity, run EditMode tests. Expected: **133/133 PASS** (127 + 6).

- [ ] **Step 6: Register in the Level scope**

In `Level.RegisterEngagementSystems`, after the `SpecialSystem` registration add:

```csharp
            builder.Register<OverchargeFinisher>(Lifetime.Scoped).As<IOverchargeFinisher>();
```

In the eager-resolve block, after `container.Resolve<ISpecialSystem>();`:

```csharp
                    container.Resolve<IOverchargeFinisher>();
```

- [ ] **Step 7: Runtime gate (Recipe 4)**

Boot into Level1, finish chaff until the Overcharge meter is full, press **S**:
- Chaff across the screen die in a wave (finishes); Momentum spikes toward Overdrive; a brief freeze-frame hits then releases; the meter empties.
- Pressing S when not full does nothing.
- Exit. Zero errors.

- [ ] **Step 8: Commit**

```bash
git add "Assets/_neon/Scripts/Specials/IOverchargeFinisher.cs" "Assets/_neon/Scripts/Specials/IOverchargeFinisher.cs.meta" "Assets/_neon/Scripts/Specials/OverchargeFinisher.cs" "Assets/_neon/Scripts/Specials/OverchargeFinisher.cs.meta" "Assets/_neon/Scripts/Growth/IEconomySystem.cs" "Assets/_neon/Scripts/Growth/EconomySystem.cs" "Assets/_neon/Tests/EditMode/OverchargeFinisherTests.cs" "Assets/_neon/Tests/EditMode/OverchargeFinisherTests.cs.meta" "Assets/_neon/Tests/EditMode/Fakes.cs" "Assets/_neon/Scripts/Level/Level.cs"
git commit -m "feat: OverchargeFinisher - meter-gated screen-clear + freeze-frame (M4)"
```

---

### Task 7: `FeedbackSystem` — per-verb hitstop + shake, tier flourish, whiff scratch

The spec's Layer 5 core (§5.5). A **scene MonoBehaviour** (like `SwarmRenderRig`) — it uses **unscaled** time so hitstop/freeze read correctly while gameplay time is scaled. It observes the static combat events + signals and applies clock hitstop + `CameraShake` per profile. The pure profile-selection helper (`FeelConfig.ProfileForVerb`, from Task 1) is EditMode-tested here.

**Files:**
- Create: `Assets/_neon/Scripts/Feel/FeedbackSystem.cs`
- Test: `Assets/_neon/Tests/EditMode/FeelProfileTests.cs`

- [ ] **Step 1: Write the failing tests (the pure selector)**

`Assets/_neon/Tests/EditMode/FeelProfileTests.cs`:

```csharp
using NUnit.Framework;

namespace BrainlessLabs.Neon.Tests
{
    public class FeelProfileTests
    {
        private static FeelConfig Config()
        {
            // Distinct shake intensities so selection is unambiguous.
            var punch = new HitProfile(0.08f, 0.04f, 0.10f, 0.15f);
            var kick = new HitProfile(0.06f, 0.06f, 0.16f, 0.20f);
            var weapon = new HitProfile(0.05f, 0.07f, 0.20f, 0.22f);
            var thrown = new HitProfile(0.03f, 0.10f, 0.35f, 0.35f);
            var def = new HitProfile(0.10f, 0.03f, 0.08f, 0.12f);
            var finish = new HitProfile(0.04f, 0.09f, 0.28f, 0.30f);
            var tierUp = new HitProfile(0.15f, 0.08f, 0.22f, 0.30f);
            return new FeelConfig(punch, kick, weapon, thrown, def, finish, tierUp, 0.35f, 0.25f);
        }

        [Test]
        public void Punch_And_GrabPunch_MapToPunch()
        {
            var c = Config();
            Assert.AreEqual(c.Punch.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.PUNCH).ShakeIntensity, 0.0001f);
            Assert.AreEqual(c.Punch.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.GRABPUNCH).ShakeIntensity, 0.0001f);
        }

        [Test]
        public void Kick_And_GrabKick_MapToKick()
        {
            var c = Config();
            Assert.AreEqual(c.Kick.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.KICK).ShakeIntensity, 0.0001f);
            Assert.AreEqual(c.Kick.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.GRABKICK).ShakeIntensity, 0.0001f);
        }

        [Test]
        public void Throw_IsTheBiggestHit()
        {
            var c = Config();
            var thrown = c.ProfileForVerb(ATTACKTYPE.GRABTHROW);
            Assert.Greater(thrown.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.PUNCH).ShakeIntensity);
            Assert.Greater(thrown.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.KICK).ShakeIntensity);
            Assert.Greater(thrown.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.WEAPON).ShakeIntensity);
        }

        [Test]
        public void Weapon_MapsToWeapon()
        {
            var c = Config();
            Assert.AreEqual(c.Weapon.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.WEAPON).ShakeIntensity, 0.0001f);
        }

        [Test]
        public void UnknownVerb_FallsBackToDefault()
        {
            var c = Config();
            Assert.AreEqual(c.DefaultHit.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.NONE).ShakeIntensity, 0.0001f);
            Assert.AreEqual(c.DefaultHit.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.GROUNDPOUND).ShakeIntensity, 0.0001f);
        }
    }
}
```

- [ ] **Step 2: Run tests — they pass immediately (the helper shipped in Task 1)**

Refresh Unity, run EditMode tests. Expected: **138/138 PASS** (133 + 5). `FeelConfig.ProfileForVerb` already exists from Task 1, so these are green on first run — they lock the mapping contract before the consumer is built.

- [ ] **Step 3: Implement the feedback MonoBehaviour**

`Assets/_neon/Scripts/Feel/FeedbackSystem.cs`:

```csharp
using System;
using System.Collections;
using R3;
using UnityEngine;
using VContainer;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Spec §5.5 feedback: a pure CONSUMER of the combat seams + signals. Applies
    /// per-verb hitstop (clock scale dip) + CameraShake, a finish beat, the tier-up
    /// flourish, and the whiff record-scratch + red flash. Scene MonoBehaviour on
    /// UNSCALED time (hitstop release can't use the frozen gameplay clock).
    /// Injected clock is used only to set/clear scale sources — timing is unscaled.
    /// </summary>
    public class FeedbackSystem : MonoBehaviour
    {
        [SerializeField] private CanvasGroup whiffFlash; // full-screen red vignette (uGUI), alpha 0 at rest

        [Inject] private IGameplaySignals _signals;
        [Inject] private IGameplayClock _clock;
        [Inject] private IAudioService _audio;

        private FeelConfig _config;
        private CameraShake _cameraShake;
        private readonly ModifierSource _hitstopSource = ModifierSource.Create("hitstop");
        private Coroutine _hitstopRoutine;
        private Coroutine _whiffRoutine;
        private IDisposable _damageSub;   // via static event, see OnEnable
        private IDisposable _finishSub;
        private IDisposable _tierSub;

        void Start()
        {
            if (_signals == null || _clock == null) { enabled = false; return; } // scene w/o DI
            _config = FeelConfig.FromSettings();
            _cameraShake = Camera.main != null ? Camera.main.GetComponent<CameraShake>() : null;
            if (whiffFlash != null) whiffFlash.alpha = 0f;

            _finishSub = _signals.On<EnemyFinished>().Subscribe(f => Play(_config.Finish, f.Position));
            _tierSub = _signals.On<MomentumTierChanged>().Subscribe(OnTier);
        }

        void OnEnable()
        {
            UnitActions.onUnitDealDamage += OnUnitDealDamage;
            UnitActions.onVerbWhiffed += OnVerbWhiffed;
        }

        void OnDisable()
        {
            UnitActions.onUnitDealDamage -= OnUnitDealDamage;
            UnitActions.onVerbWhiffed -= OnVerbWhiffed;
        }

        void OnDestroy()
        {
            _finishSub?.Dispose();
            _tierSub?.Dispose();
            _clock?.ClearScale(_hitstopSource);
        }

        private void OnUnitDealDamage(GameObject recipient, AttackData attackData)
        {
            if (attackData?.inflictor == null || !attackData.inflictor.CompareTag("Player")) return;
            var pos = recipient != null ? (Vector2)recipient.transform.position : Vector2.zero;
            Play(_config.ProfileForVerb(attackData.attackType), pos);
        }

        private void OnTier(MomentumTierChanged e)
        {
            if ((int)e.Current <= (int)e.Previous) return; // flourish only on tier UP
            Play(_config.TierUp, PlayerPos());
            _audio?.PlaySFX("MomentumTierUp", PlayerPos());
        }

        private void OnVerbWhiffed(UnitActions unit, ATTACKTYPE attackType)
        {
            if (unit == null || !unit.isPlayer) return;
            _audio?.PlaySFX("Whiff", PlayerPos()); // record-scratch
            if (_whiffRoutine != null) StopCoroutine(_whiffRoutine);
            _whiffRoutine = StartCoroutine(WhiffFlashRoutine());
        }

        private void Play(HitProfile profile, Vector2 position)
        {
            if (_cameraShake != null && profile.ShakeIntensity > 0f)
                _cameraShake.ShowCamShake(profile.ShakeIntensity, profile.ShakeSeconds);

            if (profile.HitstopSeconds > 0f && profile.HitstopScale < 1f)
            {
                if (_hitstopRoutine != null) StopCoroutine(_hitstopRoutine);
                _hitstopRoutine = StartCoroutine(HitstopRoutine(profile.HitstopScale, profile.HitstopSeconds));
            }
        }

        private IEnumerator HitstopRoutine(float scale, float seconds)
        {
            _clock.SetScale(_hitstopSource, scale);
            yield return new WaitForSecondsRealtime(seconds); // UNSCALED — scaled time is dilated during hitstop
            _clock.ClearScale(_hitstopSource);
            _hitstopRoutine = null;
        }

        private IEnumerator WhiffFlashRoutine()
        {
            if (whiffFlash == null) yield break;
            float t = 0f;
            while (t < _config.WhiffFlashSeconds)
            {
                whiffFlash.alpha = Mathf.Lerp(0.6f, 0f, t / _config.WhiffFlashSeconds);
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            whiffFlash.alpha = 0f;
            _whiffRoutine = null;
        }

        private Vector2 PlayerPos()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? (Vector2)player.transform.position : Vector2.zero;
        }
    }
}
```

> **Why a MonoBehaviour, not a clock tickable:** hitstop dilates the gameplay clock's own delta toward 0, so a clock-ticked release would never fire. `WaitForSecondsRealtime` / `Time.unscaledDeltaTime` release on wall-clock time. This mirrors the `SwarmRenderRig` scene-MB pattern; it's a pure consumer (no gameplay authority).

- [ ] **Step 4: Compile + tests + commit**

Refresh Unity: zero errors, 138 tests PASS. (Scene wiring — the `whiffFlash` CanvasGroup + placing the component — is Task 11's scene pass, alongside the HUD; a runtime feel check rides Task 11 too. This task ships the system + its contract test.)

```bash
git add "Assets/_neon/Scripts/Feel/FeedbackSystem.cs" "Assets/_neon/Scripts/Feel/FeedbackSystem.cs.meta" "Assets/_neon/Tests/EditMode/FeelProfileTests.cs" "Assets/_neon/Tests/EditMode/FeelProfileTests.cs.meta"
git commit -m "feat: FeedbackSystem - per-verb hitstop/shake, tier flourish, whiff scratch (M4)"
```

---

### Task 8: Callouts + XP/Charge popups — `FloatingTextSpawner`

Floating world-space text (spec §5.5): +N popups on XP/Charge gains, and callouts ("NODE RESTORED", "SIREN PULSE", "OVERCHARGE"). A scene MonoBehaviour pooling world-space `TextMesh`/uGUI labels. Pure consumer.

**Files:**
- Create: `Assets/_neon/Scripts/Feel/FloatingTextSpawner.cs`

- [ ] **Step 1: Implement**

`Assets/_neon/Scripts/Feel/FloatingTextSpawner.cs`:

```csharp
using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using VContainer;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Spec §5.5 signs: +N XP/Charge popups + callouts. Pure consumer. Pools
    /// world-space TextMesh labels that rise + fade on UNSCALED time (readable
    /// during hitstop/freeze). ObjectiveCompleted → "NODE RESTORED"; Callout → its text.
    /// </summary>
    public class FloatingTextSpawner : MonoBehaviour
    {
        [SerializeField] private TextMesh labelPrefab;   // a world-space 3D Text prefab
        [SerializeField] private int poolSize = 24;
        [SerializeField] private float riseSpeed = 1.5f;
        [SerializeField] private float lifeSeconds = 0.9f;
        [SerializeField] private Color xpColor = new(0.6f, 0.9f, 1f);
        [SerializeField] private Color chargeColor = new(1f, 0.85f, 0.2f);
        [SerializeField] private Color calloutColor = new(0.4f, 1f, 0.6f);

        [Inject] private IGameplaySignals _signals;

        private readonly List<TextMesh> _pool = new();
        private readonly List<float> _spawnedAt = new();
        private int _next;
        private readonly List<IDisposable> _subs = new();

        void Start()
        {
            if (_signals == null || labelPrefab == null) { enabled = false; return; }

            for (int i = 0; i < poolSize; i++)
            {
                var label = Instantiate(labelPrefab, transform);
                label.gameObject.SetActive(false);
                _pool.Add(label);
                _spawnedAt.Add(0f);
            }

            _subs.Add(_signals.On<XpGained>().Subscribe(e => Spawn($"+{e.Amount} XP", PlayerPos(), xpColor)));
            _subs.Add(_signals.On<NeonChargeChanged>().Subscribe(OnCharge));
            _subs.Add(_signals.On<Callout>().Subscribe(e => Spawn(e.Text, e.Position, calloutColor)));
            _subs.Add(_signals.On<ObjectiveCompleted>().Subscribe(_ => Spawn("NODE RESTORED", PlayerPos(), calloutColor)));
        }

        void OnDestroy()
        {
            foreach (var sub in _subs) sub.Dispose();
            _subs.Clear();
        }

        private int _lastCharge;

        private void OnCharge(NeonChargeChanged e)
        {
            int delta = e.Total - _lastCharge;
            _lastCharge = e.Total;
            if (delta > 0) Spawn($"+{delta} ⚡", PlayerPos(), chargeColor); // gains only; spends are silent
        }

        void LateUpdate()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                var label = _pool[i];
                if (!label.gameObject.activeSelf) continue;

                float age = Time.unscaledTime - _spawnedAt[i];
                if (age >= lifeSeconds) { label.gameObject.SetActive(false); continue; }

                label.transform.position += Vector3.up * (riseSpeed * Time.unscaledDeltaTime);
                var c = label.color;
                c.a = Mathf.Lerp(1f, 0f, age / lifeSeconds);
                label.color = c;
            }
        }

        private void Spawn(string text, Vector2 worldPos, Color color)
        {
            int slot = _next;
            _next = (_next + 1) % _pool.Count;

            var label = _pool[slot];
            label.text = text;
            label.color = color;
            label.transform.position = new Vector3(worldPos.x, worldPos.y + 1f, 0f);
            label.gameObject.SetActive(true);
            _spawnedAt[slot] = Time.unscaledTime;
        }

        private Vector2 PlayerPos()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? (Vector2)player.transform.position : Vector2.zero;
        }
    }
}
```

- [ ] **Step 2: Compile check + commit**

Refresh Unity: zero errors, 138 tests PASS. Scene wiring + runtime feel in Task 11.

```bash
git add "Assets/_neon/Scripts/Feel/FloatingTextSpawner.cs" "Assets/_neon/Scripts/Feel/FloatingTextSpawner.cs.meta"
git commit -m "feat: FloatingTextSpawner - +N popups + callouts (M4)"
```

---

### Task 9: Audio — `CrossfadeMusic` + `SignalMusicDirector` (F3)

Track-swap + stingers by a combined Momentum-tier + Signal "heat" band. A tiny additive `AudioService` method (crossfade) + a scene director that consumes `MomentumTierChanged` + `SignalChanged`. Degrades to stingers-only if only one music track exists (verify assets at execution).

**Files:**
- Modify: `Assets/_neon/Scripts/Audio/IAudioService.cs`
- Modify: `Assets/_neon/Scripts/Audio/AudioService.cs`
- Create: `Assets/_neon/Scripts/Feel/SignalMusicDirector.cs`

- [ ] **Step 1: Extend the audio service**

In `Assets/_neon/Scripts/Audio/IAudioService.cs`, add:

```csharp
        /// <summary>Crossfade the music bed to another track over <paramref name="seconds"/>. No-op if already on it.</summary>
        void CrossfadeMusic(string name, float seconds);
```

In `Assets/_neon/Scripts/Audio/AudioService.cs`, implement it. **Re-read the file first** (working agreement 9) — it owns the music `AudioSource`(s) and the `AudioConfigurationAsset` lookup. Implement `CrossfadeMusic` to: if `name` equals the current track, return; else start a coroutine (via a persistent runner GameObject the service already uses, or `PlayMusic`'s existing source) that fades the current source volume to 0 while fading a second source playing `name` up over `seconds`, then swaps roles. If `AudioService` has only one music source, the honest MVP fallback is a hard `PlayMusic(name)` when the band changes (no crossfade) — do that and log once; note it in the gate record. Keep the implementation faithful to the file's actual source/mixer structure; do not invent members.

> The exact body depends on `AudioService`'s real internals (music source count, mixer groups, config lookup) — this is the one place the plan can't pin every line without the file open. Read `AudioService.cs`, follow its existing `PlayMusic` pattern, and add the smallest crossfade (or single-source hard-swap fallback) that fits. Everything downstream only needs the `CrossfadeMusic(name, seconds)` signature.

- [ ] **Step 2: The music director**

`Assets/_neon/Scripts/Feel/SignalMusicDirector.cs`:

```csharp
using System;
using R3;
using UnityEngine;
using VContainer;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Spec §5.5 "audio layering by Momentum tier + Signal" (F3: track-swap + stingers).
    /// A combined 0–3 "heat" band = max(Momentum tier, Signal band) picks the music bed;
    /// tier-up/finisher/node-restored fire one-shot stingers. Pure consumer.
    /// Track names are placeholders — wire to real clips in AudioConfiguration (verify
    /// which exist; fewer tracks → fewer distinct bands, the director still runs).
    /// </summary>
    public class SignalMusicDirector : MonoBehaviour
    {
        [SerializeField] private string[] _bandTracks = { "Music_Night_0", "Music_Night_1", "Music_Night_2", "Music_Dawn" };
        [SerializeField] private float _crossfadeSeconds = 1.5f;

        [Inject] private IGameplaySignals _signals;
        [Inject] private IAudioService _audio;

        private int _momentumBand;
        private int _signalBand;
        private int _currentBand = -1;
        private readonly System.Collections.Generic.List<IDisposable> _subs = new();

        void Start()
        {
            if (_signals == null || _audio == null) { enabled = false; return; }

            _subs.Add(_signals.On<MomentumTierChanged>().Subscribe(e => { _momentumBand = (int)e.Current; Reevaluate(); }));
            _subs.Add(_signals.On<SignalChanged>().Subscribe(e =>
            {
                _signalBand = e.Dawn > 0f ? Mathf.Clamp(Mathf.RoundToInt(e.Value / e.Dawn * 3f), 0, 3) : 0;
                Reevaluate();
            }));
            _subs.Add(_signals.On<OverchargeFinisherFired>().Subscribe(_ => _audio.PlaySFX("FinisherStinger")));
            _subs.Add(_signals.On<ObjectiveCompleted>().Subscribe(_ => _audio.PlaySFX("NodeRestoredStinger")));

            Reevaluate(); // set the opening bed
        }

        void OnDestroy()
        {
            foreach (var sub in _subs) sub.Dispose();
            _subs.Clear();
        }

        private void Reevaluate()
        {
            if (_bandTracks == null || _bandTracks.Length == 0) return;
            int heat = Mathf.Max(_momentumBand, _signalBand);
            int band = Mathf.Clamp(heat, 0, _bandTracks.Length - 1);
            if (band == _currentBand) return;
            _currentBand = band;
            _audio.CrossfadeMusic(_bandTracks[band], _crossfadeSeconds);
        }
    }
}
```

- [ ] **Step 3: Compile + tests + commit**

Refresh Unity: zero errors, 138 tests PASS.

```bash
git add "Assets/_neon/Scripts/Audio/IAudioService.cs" "Assets/_neon/Scripts/Audio/AudioService.cs" "Assets/_neon/Scripts/Feel/SignalMusicDirector.cs" "Assets/_neon/Scripts/Feel/SignalMusicDirector.cs.meta"
git commit -m "feat: audio - CrossfadeMusic + SignalMusicDirector (tier+Signal heat) (M4)"
```

---

### Task 10: Functional HUD — Special cooldown + Overcharge-ready meters (F1)

The **functional** meters for the new actives (F1 = systemic + functional, not cosmetic). A pure consumer; styling defers to Feel & Level.

**Files:**
- Create: `Assets/_neon/Scripts/UI/UIHUDSpecialMeter.cs`

- [ ] **Step 1: Implement**

`Assets/_neon/Scripts/UI/UIHUDSpecialMeter.cs`:

```csharp
using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon {

    //Functional readouts for the two M4 actives: Siren Pulse cooldown fill + a
    //"FINISHER READY" flag when Overcharge is full. Pure signals consumer; cosmetic
    //polish (icons/glyphs/animation) is the Feel & Level pass.
    public class UIHUDSpecialMeter : MonoBehaviour {

        [SerializeField] private Image specialCooldownFill; // 0 (cooling) → 1 (ready)
        [SerializeField] private Text specialReadyLabel;    // "SIREN READY"/"" 
        [SerializeField] private GameObject finisherReadyRoot; // shown only when Overcharge full

        [Inject] private IGameplaySignals _signals;
        private IDisposable _specialSub;
        private IDisposable _finisherSub;

        void Start(){
            if(_signals == null) return; //scene without DI injection
            if(specialCooldownFill != null) specialCooldownFill.fillAmount = 1f;
            if(specialReadyLabel != null) specialReadyLabel.text = "SIREN READY";
            if(finisherReadyRoot != null) finisherReadyRoot.SetActive(false);

            _specialSub = _signals.On<SpecialStateChanged>().Subscribe(ApplySpecial);
            _finisherSub = _signals.On<OverchargeReadyChanged>().Subscribe(ApplyFinisher);
        }

        void OnDestroy(){
            _specialSub?.Dispose();
            _finisherSub?.Dispose();
        }

        void ApplySpecial(SpecialStateChanged s){
            if(specialCooldownFill != null) specialCooldownFill.fillAmount = s.CooldownNormalized;
            if(specialReadyLabel != null) specialReadyLabel.text = s.Ready ? "SIREN READY" : "";
        }

        void ApplyFinisher(OverchargeReadyChanged f){
            if(finisherReadyRoot != null) finisherReadyRoot.SetActive(f.Ready);
        }
    }
}
```

- [ ] **Step 2: Compile check + commit**

Refresh Unity: zero errors, 138 tests PASS. Scene wiring in Task 11.

```bash
git add "Assets/_neon/Scripts/UI/UIHUDSpecialMeter.cs" "Assets/_neon/Scripts/UI/UIHUDSpecialMeter.cs.meta"
git commit -m "feat: functional HUD - Special cooldown + Overcharge-ready meters (M4)"
```

---

### Task 11: Scene wiring — feedback, HUD, audio in `03_Level1` + first feel bring-up

Editor work (not in Play mode). Places all the M4 scene consumers so the runtime feel comes alive.

- [ ] **Step 1: Feedback + whiff flash**

Open `03_Level1`. On the HUD Canvas:
- Add a full-screen **WhiffFlash**: an `Image` (solid red, stretched to fill), wrapped in a **CanvasGroup** with alpha 0. This is `FeedbackSystem.whiffFlash`.
- Add a root GameObject **FeedbackSystem** with the `FeedbackSystem` component; assign `whiffFlash`. (`[Inject]` resolves via `Level.Configure` root injection; `CameraShake` is found on `Camera.main` at Start.)
- Confirm the main camera has a `CameraShake` component with a non-empty `CameraShakeAnimation` curve (it's `[RequireComponent(CameraFollow)]`; the shake no-ops on an empty curve — set a simple 0→1→0 curve if absent).

- [ ] **Step 2: Floating text**

- Create a world-space **`TextMesh`** prefab (`FloatingLabel`) — small, bright, no background — under `Assets/_neon/Prefabs/` (create the folder if missing).
- Add a **FloatingTextSpawner** GameObject; assign `labelPrefab` = `FloatingLabel`.

- [ ] **Step 3: Audio director**

- Add a **SignalMusicDirector** GameObject. Set `_bandTracks` to the music track names that actually exist in the `AudioConfiguration` (open it; use however many exist — 1 track is fine, the director just won't change bands). Stinger names (`MomentumTierUp`, `Whiff`, `FinisherStinger`, `NodeRestoredStinger`) should point at real SFX entries; add placeholders to the SFX config or accept silent stingers (note which in the gate record).

- [ ] **Step 4: HUD meters**

- On the HUD Canvas add **UIHUDSpecialMeter**: a `specialCooldownFill` (Filled Image, reuse the `UISprite` used by other bars — remember M2/M3 lesson: null-sprite Filled Images ignore `fillAmount`), a `specialReadyLabel` (Text), and a `finisherReadyRoot` (a small "FINISHER READY [S]" panel, hidden by default). Assign all three.

- [ ] **Step 5: First feel bring-up (Recipe 4)**

Boot into Level1 and exercise everything:
- **Hitstop/shake:** punches/kicks/weapon hits have escalating hit-punch; throw-enemy is the meatiest. No stuck slow-mo (hitstop always releases — it's unscaled).
- **Tier-up flourish:** climbing Momentum tiers fires the flourish + stinger.
- **Whiff:** a swing into empty air → record-scratch SFX + a red flash pulse (then clears).
- **Siren Pulse (A):** manufactures a Finish-Ready wave + "SIREN PULSE" callout; cooldown fill on the HUD drains then refills.
- **Overcharge finisher (S):** at full meter, screen-clear + freeze-frame + "OVERCHARGE" callout + "+N" churn; "FINISHER READY" flag shows only when full.
- **Callouts/popups:** "NODE RESTORED" on objective complete; +XP / +⚡ popups on gains.
- **Audio:** music bed shifts as Momentum/Signal heat rises (or single-track + stingers if that's all that exists).
- Exit. Zero errors.

- [ ] **Step 6: Commit**

```bash
git add "Assets/_neon/Scenes/Game/03_Level1.unity" "Assets/_neon/Prefabs"
git add -A "Assets/_neon"
git commit -m "chore: wire M4 feedback/HUD/audio into Level1 (M4)"
```

---

### Task 12: M4 gate — the full §16-derived acceptance pass + series close

Spec §7 M4 gate: *the full §16 checklist.* The GDD §16 checklist lives in Notion (not in-repo); this gate is built from **spec §5.5 (feedback deliverables) + §7 M4 + the risk register (§9) + all carried hands-on flags**, and is the acceptance set to **reconcile against GDD §16 when Notion is reachable** (note any gaps for Sebastien).

- [ ] **Step 1: Full test suite**

Expected: **138/138 PASS** — 23 M0 + 25 M1 + 42 M2 + 27 M3 + 21 M4 (Overcharge 4 · Special 6 · Finisher 6 · FeelProfile 5). Record the exact split.

- [ ] **Step 2: M4 feature acceptance (one continuous session in Level1)**

Tick each spec §5.5 / §7 M4 deliverable, present + working:
- [ ] Siren Pulse: cooldown+Charge active, manufactures a Finish-Ready wave (chaff gold + heroes knocked down), grants no Momentum itself.
- [ ] Overcharge finisher: manual at full meter, chaff screen-clear (mass finishes → Momentum spike), freeze-frame, consumes the meter; distinct-feeling from Siren Pulse.
- [ ] Per-verb hitstop + shake profiles: punch/kick/weapon/throw escalate; throw is biggest.
- [ ] Tier-up flourish on Momentum up.
- [ ] Whiff record-scratch (+ red flash; true desaturate deferred — deviation).
- [ ] "NODE RESTORED" callout + other callouts.
- [ ] XP/Charge +N popups.
- [ ] Finisher freeze-frame.
- [ ] Audio layering by Momentum tier + Signal (track-swap+stingers, or documented single-track fallback).
- [ ] Functional HUD: Special cooldown + Overcharge-ready meters.

- [ ] **Step 3: Carried hands-on flags — resolve now with the full feel layer**

The whole point of M4 is these finally get their fair test:
- [ ] **M2 Overdrive "screams"** — with the finisher + tier flourish + audio + hitstop, does holding Overdrive read as ~2.5–3× and *feel* like screaming? (§8.1 target; §16.)
- [ ] **M3 run legibility in chaos** — objective/arrow/glow + the new callouts readable in the dawn pile? (Note residual chaff-blob for Feel & Level.)
- [ ] **Full-run wall-clock** — a real hands-on run start→dawn in the 10–15 min target (knobs still live).
- [ ] Mid-fight FPS spot-check at dawn density with actives + feedback firing (no regression vs ~197).

- [ ] **Step 4: §16 reconciliation note**

Open the GDD §16 in Notion (if reachable) and diff its checklist against Step 2/3 above. Record any §16 item this milestone does NOT satisfy (and whether it's MVP-scoped or a later pass). If Notion is unreachable, say so — the spec-derived set stands as the acceptance proxy.

- [ ] **Step 5: Gate record + push**

Append `## M4 gate record` to the bottom of THIS document (date, machine, exact test count, the Step 2 checklist results, the Step 3 hands-on verdicts, §16 reconciliation, deviations encountered), then:

```bash
git add "docs/superpowers/plans/2026-07-05-neon-engine-base-plan5-m4-feel.md"
git commit -m "docs: record M4 gate (actives + feel; engine-base series complete)"
git push -u origin claude/neon-m4-feel
```

- [ ] **Step 6: Series close + hand off**

This completes the **engine-base plan series (M0–M4)** — the full MVP vertical slice (spec §0.4.g). Report to Sebastien:
- The M4 gate record + any §16 gaps.
- **Next up is the Feel & Level pass** — its own brainstorm→spec→plan workflow, now unblocked (level design delivered at `docs/levels/level-01-downtown-strip.md`). Its pre-brief (`docs/superpowers/plans/2026-07-04-feel-and-level-pass-pre-brief.md`) already flags the M4↔Feel overlaps this plan deliberately deferred: **cosmetic HUD/QTE polish, verb-glyph art, meter restyling, whiff true-desaturate, and the chaff-blob separation** — all to be authored once against the real level + sprites.
- Post-MVP priority order stays as spec §7 lists it (weapon-throw → jump-attack → elite execution + Sirencatcher → Jammer/Bruiser → Boss → Rescue → 2nd Special → biomes).

---

## Deviations from the spec (deliberate, M4-scoped)

1. **Cosmetic HUD polish deferred to Feel & Level** (decision F1) — M4 ships systemic feel + *functional* meters; verb glyphs, meter restyling, QTE-prompt redesign are authored once against the real level/sprites. Spec §5.5's "full HUD polish" is split across M4 (function) + Feel & Level (form).
2. **Whiff "desaturate" → record-scratch SFX + red uGUI flash** — built-in RP has no post-processing stack (render memory); true fullscreen desaturate is infra out of scope, folded into Feel & Level.
3. **Siren Pulse "reveal" = a light flash, not fog-of-war** — no visibility system exists; the reveal is cosmetic feedback.
4. **Overcharge finisher clears chaff only** (R4) — heroes take a knockdown at most; it never one-shots hero-tier/boss.
5. **Audio is track-swap + stingers, not multi-stem layering** (decision F3) — with a documented single-track hard-swap fallback if only one music clip exists.
6. **`AudioService.CrossfadeMusic` is the one under-specified body** — its internals depend on the file's real music-source/mixer structure; the plan pins the signature + behavior and defers the exact lines to execution-time read (working agreement 9).
7. **Feedback/floating-text/audio-director are scene MonoBehaviours on unscaled time**, not clock tickables — hitstop/freeze dilate the gameplay clock toward 0, so releases must run on wall-clock time (mirrors `SwarmRenderRig`).
8. **Finisher freeze release is unscaled-time-gated** (`Time.unscaledTime`) with a `ReleaseFreezeForTest` seam — EditMode can't advance unscaled time deterministically.
9. **Post-win growth quiescence not added here** — the M3 flag (drafts can pop over RunWon) rides into the deferred run-reset work; M4 doesn't touch run lifecycle.
10. **No new-run reset** — one run per boot, unchanged from M3.

## Spec coverage self-check (for reviewers)

- Spec §7 M4: Siren Pulse ✅ (Task 5) · Overcharge finisher ✅ (Task 6) · per-verb hitstop/shake profiles ✅ (Tasks 1/7) · tier-up flourish ✅ (Task 7) · whiff scratch ✅ (Task 7, desaturate→deviation 2) · callouts ✅ (Task 8) · audio layering ✅ (Task 9, F3) · full HUD polish → **function** in M4 (Task 10) + **form** in Feel & Level (F1/deviation 1) · gate: §16-derived acceptance ✅ (Task 12).
- Spec §5.3 actives: `ISpecialSystem` + Overcharge, MVP = 1 Special (Siren Pulse) + the finisher, both "manufacture a wave of Finish-Ready" ✅ · Overcharge tuned to clear chaff not bosses (R4) ✅.
- Spec §5.5 feedback: pure consumer of Signals + Clock ✅ (all M4 view components subscribe, never reach into systems) · hitstop/slow-mo routes through `IGameplayClock` ✅ · reuse `CameraShake` with per-verb profiles ✅ · throw-enemy = biggest hit ✅.
- Design input: special-moves Siren Pulse (Pulse type, cd 6s/Charge 20, mass Finish-Ready) ✅; shop stays Heal+Continue (Special stock is a later pass) ✅.
- Spec §10 non-negotiables: DI-bootstrap gates ✅ · VContainer-only (actives Level-scoped, feedback scene-injected) ✅ · no static locators (extends the existing static combat *events*, the sanctioned seam) ✅ · **combat verbs unchanged** (actives invoke existing `UnitKnockDown`; feedback only observes) ✅ · no legacy touched ✅ · runtime gate per task + full acceptance gate ✅ · no invented APIs (every landed seam re-read post-M3; `AudioService.CrossfadeMusic` body deferred to file-open per deviation 6) ✅ · assembly direction unchanged ✅.
- Guardrails: no active/feedback adds a `Mult` (Momentum stays the only multiplier); the finisher's mass finishes step Momentum through the normal `EnemyFinished` path ✅.
- Series complete: M0 spine · M1 engagement · M2 growth · M3 run · **M4 feel** — the MVP vertical slice (spec §0.4.g). Feel & Level pass is the next, separate workflow.

---

## M4 gate record

**Date:** 2026-07-05 · **Machine:** sebch / Windows 11 Pro (G: drive working copy) · **Branch:** `claude/neon-m4-feel` (11 commits off master `c37b467`, tip `beba27a`) · **Executor:** Claude (Fable 5), executing-plans workflow, in-place branch (no worktree — live Unity Editor attached to this checkout).

### Step 1 — Test suite

**138/138 PASS** (EditMode, `BrainlessLabs.Neon.Tests.EditMode`, 1.2s).
Split: 117 M0–M3 (unchanged, green throughout) + 21 M4 = **Overcharge 4 · Special 6 · Finisher 6 · FeelProfile 5** — exactly the plan's predicted split.

### Step 2 — M4 feature acceptance (runtime-verified in Level1 play sessions)

- [x] **Siren Pulse** — A key through the real input path: charge −20, `MassFinishReady` flipped 1→80 chaff to Finish-Ready, in-radius wave enemy knocked down (frame-sampled: `UnitKnockDown` on the frame after the press), cooldown blocked an immediate re-press, recovered after ~6s, too-poor press was a no-op. Grants no Momentum itself.
- [x] **Overcharge finisher** — S key at full meter: all 80 (later 150) chaff cleared **as finishes**, freeze-frame applied and released on unscaled schedule, meter consumed, not-full press a no-op. Distinct from the Special (meter vs cooldown gate).
- [x] **Per-verb hitstop + shake profiles** — profile mapping contract locked by FeelProfileTests (throw biggest); finish-profile hitstop observed live (ts → 0.04 for 0.09s, clean release). Escalation punch<kick<weapon<throw is knob data (`FeelSettingsAsset`), judged hands-on.
- [x] **Tier-up flourish** — 20-finish burst drove Cool→Overdrive; flourish profile + `MomentumTierUp` stinger fired per tier-up.
- [x] **Whiff record-scratch + red flash** — `ReportVerbWhiff` through the real seam: flash alpha 0.60 → fade → 0, `Whiff` SFX fired. (True desaturate = deviation 2, deferred.)
- [x] **"NODE RESTORED" + callouts** — ObjectiveCompleted → NODE RESTORED world label; Callout signals ("SIREN PULSE"/"OVERCHARGE") spawn labels.
- [x] **XP/Charge +N popups** — `+2 ⚡` observed on finish; XP popups on kills (pool of 24, unscaled rise+fade).
- [x] **Finisher freeze-frame** — ts 0 during freeze, released on schedule (verified via internal-state probe after an earlier false alarm — see observations).
- [x] **Audio layering** — `CrossfadeMusic` (two-source unscaled crossfade) + `SignalMusicDirector` (heat band = max(tier, signal band)) built; **single-track F3 fallback in effect** (see deviations). Stingers fire on tier-up / finisher / node-restored.
- [x] **Functional HUD** — SpecialMeter fill drains/refills with `SpecialStateChanged`, SIREN READY label toggles, FINISHER READY [S] flag tracks `OverchargeReadyChanged`. View layer verified by direct signal drive; key→system verified in Tasks 5/6 sessions.

### Step 3 — Carried hands-on flags

- **FPS spot-check (measured):** at forced dawn density (SpawnNastiness ×1.875 → 150-chaff cap): **237 FPS baseline, 214 FPS with Siren + finisher + full feedback firing**, worst single frame 18.6ms (the 150-kill burst). No regression vs the M1 ~197 reference. ✅
- **M2 Overdrive "screams"** — full feel layer now in place (finisher + flourish + hitstop + stingers). **Verdict is Sebastien's hands-on** — open.
- **M3 run legibility in chaos** — callouts + popups added; chaff-blob separation still deferred to Feel & Level. **Hands-on** — open.
- **Full-run wall-clock 10–15 min** — knobs live (RebootDurationSeconds 50s, encounter count, wave sizes); machine floor was 5.2 min in M3. **Hands-on** — open.

### Step 4 — §16 reconciliation (GDD fetched from Notion 2026-07-05)

| §16 item | Status |
|---|---|
| Hands busy, not idle (R2) | Built (verbs + actives + feel); **verdict hands-on** |
| Find the prompt in <2s | Prompt shipped M1; new-player readability **hands-on** (rides M3 legibility flag) |
| Overdrive *feels* screaming | Feel layer complete; **hands-on** (the M2 flag's fair test) |
| Whiff tense-but-fair | Whiff feedback now audible/visible; **hands-on** |
| Objective legible in chaos | NODE RESTORED + arrow/bar shipped; **hands-on** |
| Belt holds FPS at peak density | **✅ measured 214–237 FPS at 150 cap with actives firing** |
| Full run 10–15 min ends on dawn | Dawn-end verified M3; wall-clock **hands-on** |

No §16 item is *unimplemented*; five of seven await Sebastien's hands-on quality verdict — they are the Feel & Level pass's entry checklist.

### Deviations & observations encountered in execution

1. **`SpecialSystem.CanActivate` is cooldown-only** (plan's impl snippet included a charge gate its own test contract rejected). Affordability stays `TryActivate`'s `TrySpend`; keeps `SpecialStateChanged` from going stale (charge changes never re-publish).
2. **Gamepad LB overlap:** Special(LB) collides with the pre-existing Defend LB binding (Defend also holds RB, shared with Grab). Keyboard A/S clean. → Feel & Level binding-hygiene pass (GDD 0.4.c already flags the RB double-bind).
3. **Music fallback:** music config's "Level 1" item has `clip: []` (no gameplay track exists in the project). Director runs single-band; **stingers-only F3 fallback**. Also pre-existing: scene `PlayMusic` GO calls `PlaySFX("Level1")` (wrong API + wrong name) — untouched, superseded by the director once a track exists.
4. **Stinger placeholders:** `MomentumTierUp`←ItemPickup, `Whiff`←Whoosh, `FinisherStinger`←DrumBarrelBreak, `NodeRestoredStinger`←UIButtonSelect. Real authoring = Feel & Level.
5. **Finisher Overcharge self-refill (balance flag):** the finisher's own mass finishes (80–150 × overchargePerFinish) instantly re-cap the meter at density → chain-fire is possible. The "churn" is spec-intended; the *full self-refill* likely wants a post-fire grant lockout or finisher-kill overcharge discount. **Sebastien's call.**
6. **Finisher kill-burst pops stacked level-up drafts** (4 drafts / 80 kills observed) — slow-mo holds until picked, mid-scream. Rides the M3 growth-quiescence flag (deviation 9).
7. **GameplayClock giant-dt observation (pre-existing, M2):** `Advance(Time.unscaledDeltaTime)` is unclamped — a multi-second editor stall delivers one giant tick that burns cooldowns/timers instantly. Surfaced as a measurement artifact; player-build impact limited to OS-level stalls. Clock-hardening candidate (clamp at source), not fixed in M4.
8. **Editor input focus:** synthetic keyboard events verified end-to-end in the Task 5/6 sessions (editor focused); later sessions ran unfocused (`PointersAndKeyboardsRespectGameViewFocus`) so the Task 11 bring-up drove the view layer by direct signal publish instead — full chain proven by composition of the two verified halves.
9. **Scene wiring done headlessly** (editor scripts, not the GUI steps the plan describes) — same objects, same references, saved scene diff-reviewed via git.

### Series close

**M0 spine · M1 engagement · M2 growth · M3 run · M4 feel — the engine-base series is complete** (spec §0.4.g vertical slice). Next: the **Feel & Level pass** (own brainstorm→spec→plan workflow), unblocked — level design delivered at `docs/levels/level-01-downtown-strip.md`, pre-brief at `docs/superpowers/plans/2026-07-04-feel-and-level-pass-pre-brief.md`. Deferred there from M4: cosmetic HUD/QTE polish, verb glyphs, meter restyling, whiff true-desaturate, chaff-blob separation, real music tracks + stingers, gamepad binding hygiene. Post-MVP priority order unchanged (spec §7): weapon-throw → jump-attack → elite execution + Sirencatcher → Jammer/Bruiser → Boss → Rescue → 2nd Special → biomes.
