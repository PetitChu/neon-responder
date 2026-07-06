# Feel & Level — Plan B: Render Foundation (PPv2) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up a Post-Processing Stack v2 pipeline on the built-in RP camera — bloom (neon signage + Finish-Ready glow), color grading, vignette — and replace M4's red uGUI whiff-flash with a true fullscreen **desaturate** pulse, without regressing frame rate at the 150-chaff density.

**Architecture:** Built-in RP (URP is installed but unassigned — do **not** switch pipelines). PPv2 (`com.unity.postprocessing`) is the correct post stack for built-in RP. A `PostProcessLayer` goes on the Main Camera (the one carrying the Plan-0 `CinemachineBrain`); a global `PostProcessVolume` + profile holds bloom/grade/vignette. Whiff = a **dedicated high-priority whiff volume** (`ColorGrading` saturation −100) whose *weight* is pulsed 1→0 by a small `WhiffPostFx` component; `FeedbackSystem` calls it instead of alpha-lerping the red `CanvasGroup`. The weight curve is a pure function (unit-tested); everything else is scene/profile authoring verified in play mode. The HUD/finish-prompt canvas is Screen-Space-Overlay, which composites *after* post — so bloom cannot blow out HUD glyphs.

**Tech Stack:** Unity 6.3.5, built-in Render Pipeline, Post-Processing Stack v2 (`com.unity.postprocessing`, namespace `UnityEngine.Rendering.PostProcessing`), C#, Unity Test Framework (EditMode). Source spec: `docs/superpowers/specs/2026-07-05-feel-and-level-pass-design.md` §6. Branch: `claude/feel-and-level-pass` (Plans 0/A/A.a merged).

**Independence:** Plan B is independent of the swarm plans (spec §3: `0 ∥ A ∥ B`) but stacks on Plan 0's camera rig (already merged) — the `PostProcessLayer` attaches to the camera Plan 0 rebuilt.

**Scope boundary:** Delivers the PPv2 pipeline + whiff desaturate + a perf gate. Rain / wet-ground reflections / final neon grade values are a later mood pass; Plan B ships sensible baseline profile values that Plan C/mood can tune against the real level.

---

## Ground truth (verified in code)

- `FeedbackSystem` (`Assets/_neon/Scripts/Feel/FeedbackSystem.cs`): `[SerializeField] CanvasGroup whiffFlash` (red uGUI vignette, alpha 0 at rest). `OnVerbWhiffed(UnitActions unit, ATTACKTYPE attackType)` guards `unit.isPlayer`, plays `_audio.PlaySFX("Whiff", …)` (record-scratch), then `StartCoroutine(WhiffFlashRoutine())`. `WhiffFlashRoutine` lerps `whiffFlash.alpha` 0.6→0 over `_config.WhiffFlashSeconds` on **unscaled** time. Subscribes in `OnEnable` to `UnitActions.onVerbWhiffed`.
- The record-scratch SFX **stays**; only the *visual* (red flash → desaturate) changes.
- After Plan 0: Main Camera in `03_Level1` has `CinemachineBrain` (+ `CinemachineImpulseSource` + rewritten `CameraShake`); it is orthographic; `PixelPerfectCamera`/`CameraFollow` are off it. (Camera is an unpacked instance scene-local — Plan 0 memory.)
- Project runs **built-in RP** (`m_CustomRenderPipeline: {fileID: 0}`); instanced ambient uses `Neon/InstancedUnlit` + `Graphics.DrawMeshInstanced` (verify via `UnityStats.instancedBatches`, not MCP screenshots).
- Game asmdef `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef` will need a `Unity.Postprocessing.Runtime` reference for `FeedbackSystem`/`WhiffPostFx` to use PPv2 types.

## File structure (what changes)

- **Modify:** `Packages/manifest.json` — add `com.unity.postprocessing`.
- **Modify:** `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef` — add `Unity.Postprocessing.Runtime`.
- **Create:** `Assets/_neon/Scripts/Feel/WhiffFx.cs` — pure whiff-weight curve.
- **Create:** `Assets/_neon/Scripts/Feel/WhiffPostFx.cs` — owns the whiff volume, pulses its weight.
- **Modify:** `Assets/_neon/Scripts/Feel/FeedbackSystem.cs` — whiff drives `WhiffPostFx` instead of the red `CanvasGroup`.
- **Create (asset):** a base PPv2 profile + a whiff PPv2 profile under `Assets/_neon/Rendering/` (names below).
- **Scene:** `Assets/_neon/Scenes/Game/03_Level1.unity` — `PostProcessLayer` on Main Camera; global base `PostProcessVolume`; whiff `PostProcessVolume` + `WhiffPostFx`.
- **Create (test):** `Assets/_neon/Tests/EditMode/Feel/WhiffFxTests.cs`.

---

## Task 1: Install PPv2 + verify the API

**Files:**
- Modify: `Packages/manifest.json`, `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef`

- [x] **Step 1: Add the package**

Package Manager → Add by name → `com.unity.postprocessing`, or add to `Packages/manifest.json`:

```json
"com.unity.postprocessing": "3.4.0"
```

(Use the latest 3.x the registry offers for Unity 6.3.5.)

- [x] **Step 2: Add the assembly reference**

Add `"Unity.Postprocessing.Runtime"` to the `references` array in `BrainlessLabs.Neon.asmdef` (alongside `Unity.Cinemachine`, `Unity.2D.PixelPerfect`, etc.).

- [x] **Step 3: Verify the API surface (no invented APIs)**

After the domain reload, confirm these resolve (`UnityEngine.Rendering.PostProcessing`): `PostProcessLayer`, `PostProcessVolume`, `PostProcessProfile`, `Bloom`, `ColorGrading`, `Vignette`. Confirm `ColorGrading.saturation` (a `FloatParameter`) and `PostProcessVolume.weight`/`.priority`/`.isGlobal` exist. Adjust later tasks if a name differs in the installed version.

Run: `mcp__unityMCP__read_console`. Expected: **no compile errors** after package + asmdef.

- [x] **Step 4: Commit**

```bash
git add Packages/manifest.json Packages/packages-lock.json Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef
git commit -m "build: add Post-Processing Stack v2 + asmdef reference (Plan B)"
```

---

## Task 2: Base post-processing volume (bloom / grade / vignette)

Scene/asset authoring; verified in play mode.

**Files:**
- Create: `Assets/_neon/Rendering/NeonPostBase.asset` (PostProcessProfile)
- Modify: `Assets/_neon/Scenes/Game/03_Level1.unity`

- [x] **Step 1: Add the PostProcessLayer to the Main Camera**

On the Plan-0 Main Camera (the one with `CinemachineBrain`): add `PostProcessLayer`. Set its `Volume Layer` to a dedicated layer (create/assign a `PostProcessing` layer) and set the trigger to the camera. This auto-pulls PPv2's resources.

- [x] **Step 2: Create the base profile + a global volume**

Create `Assets/_neon/Rendering/NeonPostBase.asset` (PostProcessProfile) with:
- **Bloom** — modest intensity, threshold set so only bright neon/glow blooms (not the whole scene); this is what makes the Finish-Ready glow + signage pop.
- **Color Grading** (LDR/Neutral) — a light neon base grade (slight saturation lift, cool/teal shadows or warm highlights to taste). Baseline only; mood pass tunes.
- **Vignette** — subtle, to frame the fight.

Add a `GameObject "Post Global"` in `03_Level1` on the `PostProcessing` layer with a `PostProcessVolume`: `isGlobal = true`, `priority = 0`, profile = `NeonPostBase`.

- [x] **Step 3: Play-verify (Recipe 4 boot, Game view — not MCP screenshots)**
  - Neon/bright elements + Finish-Ready chaff glow **bloom**; the scene isn't washed out (threshold correct).
  - The **HUD + finish prompt stay crisp** (Overlay composites after post — confirm no bloom bleed on glyphs).
  - No console errors; instanced ambient still draws (`UnityStats.instancedBatches`).

- [x] **Step 4: Commit**

```bash
git add Assets/_neon/Rendering/NeonPostBase.asset* Assets/_neon/Scenes/Game/03_Level1.unity
git commit -m "feat: PPv2 base volume (bloom/grade/vignette) on 03_Level1 (Plan B)"
```

---

## Task 3: Whiff desaturate (pure curve → TDD; volume pulse; rewire FeedbackSystem)

**Files:**
- Create: `Assets/_neon/Scripts/Feel/WhiffFx.cs`
- Test: `Assets/_neon/Tests/EditMode/Feel/WhiffFxTests.cs`
- Create: `Assets/_neon/Scripts/Feel/WhiffPostFx.cs`
- Create: `Assets/_neon/Rendering/NeonPostWhiff.asset` (PostProcessProfile — ColorGrading saturation −100)
- Modify: `Assets/_neon/Scripts/Feel/FeedbackSystem.cs`
- Modify: `03_Level1.unity`

- [x] **Step 1: Write the failing test for the weight curve**

```csharp
using NUnit.Framework;
using BrainlessLabs.Neon;

namespace BrainlessLabs.Neon.Tests
{
    public class WhiffFxTests
    {
        [Test] public void PeaksAtStart() => Assert.AreEqual(1f, WhiffFx.WeightAt(0f, 0.3f), 0.001f);
        [Test] public void ZeroAtEnd()   => Assert.AreEqual(0f, WhiffFx.WeightAt(0.3f, 0.3f), 0.001f);
        [Test] public void HalfwayIsHalf()=> Assert.AreEqual(0.5f, WhiffFx.WeightAt(0.15f, 0.3f), 0.001f);
        [Test] public void ClampsPastEnd()=> Assert.AreEqual(0f, WhiffFx.WeightAt(1f, 0.3f), 0.001f);
    }
}
```

- [x] **Step 2: Run to verify it fails**

Run EditMode. Expected: FAIL — `WhiffFx` undefined.

- [x] **Step 3: Implement the pure curve**

```csharp
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>Whiff desaturate weight: full at the moment of the whiff, linear decay to 0.</summary>
    public static class WhiffFx
    {
        public static float WeightAt(float elapsed, float duration)
        {
            if (duration <= 0f) return 0f;
            return Mathf.Clamp01(1f - elapsed / duration);
        }
    }
}
```

- [x] **Step 4: Run to verify it passes**

Run EditMode. Expected: 4 `WhiffFxTests` PASS; suite green (Plan A.a's 155 + 4 = 159).

- [x] **Step 5: Add the whiff volume component**

```csharp
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Fullscreen whiff desaturate. Owns a high-priority global PostProcessVolume whose profile
    /// grades saturation to grey; Pulse() drives its weight 1→0 (WhiffFx curve) on unscaled time.
    /// Replaces M4's red uGUI flash. Lives in each swarm/combat level scene.
    /// </summary>
    public class WhiffPostFx : MonoBehaviour
    {
        [SerializeField] private PostProcessVolume _whiffVolume; // isGlobal, priority > base, ColorGrading saturation -100, weight 0 at rest

        private float _elapsed, _duration;
        private bool _active;

        public void Pulse(float seconds)
        {
            _duration = Mathf.Max(0.01f, seconds);
            _elapsed = 0f;
            _active = true;
        }

        private void Update()
        {
            if (!_active || _whiffVolume == null) return;
            _elapsed += Time.unscaledDeltaTime; // whiff often coincides with hitstop — unscaled, like the old routine
            _whiffVolume.weight = WhiffFx.WeightAt(_elapsed, _duration);
            if (_elapsed >= _duration) { _whiffVolume.weight = 0f; _active = false; }
        }
    }
}
```

- [x] **Step 6: Author the whiff profile + volume in the scene**

Create `Assets/_neon/Rendering/NeonPostWhiff.asset` with **ColorGrading** only, `saturation = -100` (full grey). Add `GameObject "Post Whiff"` on the `PostProcessing` layer: `PostProcessVolume` (`isGlobal = true`, `priority = 10` > base, `weight = 0`, profile = `NeonPostWhiff`) + `WhiffPostFx` (wire its `_whiffVolume`).

- [x] **Step 7: Rewire FeedbackSystem to pulse the desaturate instead of the red flash**

In `FeedbackSystem`: cache `WhiffPostFx` in `Start` (`_whiffPostFx = Object.FindFirstObjectByType<WhiffPostFx>();`). In `OnVerbWhiffed`, keep the SFX line, and replace the red-flash coroutine with a pulse:

```csharp
private void OnVerbWhiffed(UnitActions unit, ATTACKTYPE attackType)
{
    if (unit == null || !unit.isPlayer) return;
    _audio?.PlaySFX("Whiff", PlayerPos()); // record-scratch stays
    _whiffPostFx?.Pulse(_config.WhiffFlashSeconds);
}
```

Delete `WhiffFlashRoutine` and the `_whiffRoutine` field. Leave the `whiffFlash` `CanvasGroup` field for now but unassign it in the scene (or remove the field + scene object) — the desaturate replaces it. Add `using` if needed (types are in the same asmdef namespace).

- [x] **Step 8: Compile + play-verify**

`mcp__unityMCP__read_console` — no errors. Boot via Recipe 4; whiff a verb (swing at nothing). Confirm: **record-scratch SFX + a brief fullscreen desaturate-to-grey** that decays out — and the old red flash is gone.

- [x] **Step 9: Commit**

```bash
git add Assets/_neon/Scripts/Feel/WhiffFx.cs Assets/_neon/Scripts/Feel/WhiffPostFx.cs Assets/_neon/Tests/EditMode/Feel/WhiffFxTests.cs Assets/_neon/Rendering/NeonPostWhiff.asset* Assets/_neon/Scripts/Feel/FeedbackSystem.cs Assets/_neon/Scenes/Game/03_Level1.unity
git commit -m "feat: whiff fullscreen desaturate via PPv2 (replaces red flash) + tests (Plan B)"
```

---

## Task 4: Perf gate at the density budget

**Files:**
- Verify only (profiler / stats)

- [x] **Step 1: Measure with the stack on, at cap**

Boot via Recipe 4, Level 01 with swarm enabled at the `ChaffCap` ceiling (~150). With PPv2 active, capture frame time (Profiler, or an on-screen FPS). Compare against the pre-PPv2 baseline (spike headroom ~197 FPS at cap 150).

- [x] **Step 2: Judge**

Expected: PPv2 (bloom + grade + vignette) is a fixed fullscreen cost — comfortably within headroom on PC. If bloom is disproportionately expensive, lower its resolution/iterations. **Done = no material regression** (stays well above target framerate). Record the measured number.

- [x] **Step 3: Legibility check under bloom (world-space feedback)**

Confirm world-space feedback that bloom *does* touch stays legible: Finish-Ready glow reads (not a white blob), and `FloatingTextSpawner` popups aren't blown out. If blown out, raise the bloom threshold. (HUD/finish prompt are Overlay → already safe.)

- [x] **Step 4: (No commit unless a profile value was tuned — then commit the profile.)**

---

## Task 5: Green sweep & acceptance

- [x] **Step 1: Full EditMode suite**

Run EditMode. Expected: **green** — Plan A.a's 155 + 4 (`WhiffFxTests`) = 159, no regressions.

- [x] **Step 2: Acceptance play-test (Recipe 4 boot, Level 01)** — confirm all:
  - Bloom makes neon/glow pop; scene not washed out; vignette frames the fight.
  - HUD + finish prompt crisp (no bloom bleed).
  - Whiff = record-scratch + fullscreen desaturate pulse; no red flash.
  - Frame rate within headroom at 150 chaff.
  - No console errors.

- [x] **Step 3: Commit**

```bash
git add -A
git commit -m "chore: Plan B green sweep — PPv2 pipeline + whiff desaturate verified"
```

---

## Self-Review

**1. Spec coverage (§6 Plan B):**
- "install `com.unity.postprocessing`; PostProcessLayer + global volume/profile: bloom / grade / vignette" → Tasks 1, 2. ✓
- "whiff = animate grade saturation toward grey on the whiff signal; record-scratch stays; replaces red flash" → Task 3 (`WhiffPostFx` pulses a saturation −100 volume weight; `FeedbackSystem` rewired; SFX kept; red flash removed). ✓
- "HUD Overlay composites after post → bloom won't blow out glyphs" → verified in Tasks 2/5. ✓
- "perf gate at 150-chaff scene" → Task 4. ✓

**2. Placeholder scan:** No "TBD/handle later". Baseline profile values are deliberately "sensible baseline, mood pass tunes" (scoped), not vague requirements. Version-sensitive PPv2 names are gated behind the Task-1 confirmation.

**3. Type consistency:** `WhiffFx.WeightAt(float, float)` matches Task-3 test/impl and `WhiffPostFx.Update` caller. `WhiffPostFx.Pulse(float)` matches its def and the `FeedbackSystem.OnVerbWhiffed` caller. `_config.WhiffFlashSeconds` reused (already exists in `FeelConfig`).

**Known non-unit-tested surface:** the PPv2 install, profiles, volumes, and camera layer are scene/asset config verified in play mode (Recipe 4) — post-processing look isn't unit-testable. `WhiffFx.WeightAt` is the extracted testable core.

**Deviation flag:** whiff is implemented as a **dedicated whiff volume whose weight is pulsed**, rather than mutating the base grade's `saturation` in place (spec §6 wording). Same visible result (a fullscreen saturation drop), but it can't corrupt the base grade if interrupted mid-pulse — safer.

---

## Execution Handoff

**Plan complete and saved to `docs/superpowers/plans/2026-07-05-feel-and-level-plan-b-render.md`.** Two execution options:

1. **Subagent-Driven (recommended)** — fresh subagent per task; Tasks 2–5 need a live Unity Editor over the MCP for profile/scene authoring + play-test.
2. **Inline Execution** — here with checkpoints.

Which approach — or review the plan yourself first?
