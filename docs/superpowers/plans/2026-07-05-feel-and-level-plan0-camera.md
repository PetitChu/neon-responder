# Feel & Level — Plan 0: Camera & Scale (Cinemachine) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the fixed `PixelPerfectCamera` rig with a Cinemachine orthographic rig that (a) has a zoomed-out baseline so the avatar + enemies read smaller, (b) supports real per-zone zoom via priority-blended vcams, (c) preserves the per-wave arena lock and camera shake, all with the build staying green.

**Architecture:** A `CinemachineBrain` on the main camera drives orthographic `CinemachineCamera` vcams. A base vcam follows the player and is bounded by a `CinemachineConfiner2D` whose shape is driven per-wave (replacing the old `CameraFollow.levelBound` right-edge clamp). Shake moves from the bespoke `CameraShake`→`CameraFollow.additionalYOffset` bob to a Cinemachine Impulse (source on the camera, listener on the vcam) — `CameraShake`'s public API is preserved so its three callers need no changes. Per-zone zoom is authored (in Plan C) via extra vcams that a `ZoneCameraTrigger` promotes on entry.

**Tech Stack:** Unity 6.3.5, Cinemachine 3.x (`com.unity.cinemachine`, namespace `Unity.Cinemachine`), C#, Unity Test Framework (EditMode). Source spec: `docs/superpowers/specs/2026-07-05-feel-and-level-pass-design.md` (§2 decision 2, §4). Branch: `claude/feel-and-level-pass`.

**Scope boundary:** This plan delivers the camera *rig + baseline + shake + bounds + per-zone capability*. It does **not** author Level 01's actual zone vcams or tune per-zone sizes — that is Plan C (level build). This plan proves the capability with a smoke test.

**Deviation from spec (flagged):** The spec (§4) said "migrate `CameraShake` → Impulse … update callers `FeedbackSystem.cs`, `UnitActions.cs`, `DoCamShake.cs`." This plan instead **preserves `CameraShake`'s public API and reimplements its body** on top of a Cinemachine impulse — so the three callers stay byte-for-byte unchanged. Lower blast radius, same result. If review prefers a clean rename to a new type, that's a follow-up.

---

## Ground truth (verified in code before writing)

- `Assets/_neon/Scripts/Camera/CameraFollow.cs` — MonoBehaviour on Main Camera. Follows `targets[]` (Player tag), damps, clamps to a view-area rect (`Left/Right/Top/Bottom`), and clamps the **right edge to `levelBound.transform.position.x`** (lines 70–79). Reads `PixelPerfectCamera.refResolution*/assetsPPU` for gizmos. `additionalYOffset` is written by `CameraShake` and applied in `LateUpdate`.
- `Assets/_neon/Scripts/Camera/CameraShake.cs` — `[RequireComponent(CameraFollow)]`; `ShowCamShake()` / `ShowCamShake(float intensity, float duration)` run a coroutine that drives `CameraFollow.additionalYOffset` from an `AnimationCurve`.
- Shake callers: `FeedbackSystem.cs:36,86-87` (`Camera.main.GetComponent<CameraShake>()`, `ShowCamShake(intensity, seconds)`), `UnitActions.cs:416-418` (`CamShake()`), `DoCamShake.cs:9` (`ShowCamShake()`).
- **Live arena-lock path:** `Level.SetCameraBoundFromProgression(float)` (`Level.cs:78-98`) creates/moves a `DynamicLevelBound` (`LevelBound`) and sets `Camera.main.GetComponent<CameraFollow>().levelBound`. **`WaveManager.cs` also sets `levelBound` but is `[Obsolete]` legacy — do not touch it** (CLAUDE.md rule 5). Both are null-guarded, so removing `CameraFollow` cannot NRE them.
- `LevelBound.cs` — a simple visualizer MonoBehaviour; its `transform.position.x` is the bound.
- `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef` references `Unity.2D.PixelPerfect`. It will need a `Unity.Cinemachine` reference added.
- Asset provenance: `CameraFollow`/`CameraShake` came from the "Beat 'Em Up - Game Template 2D" package (see `.meta`). They are project-owned now; safe to modify.

## File structure (what changes)

- **Modify:** `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef` — add `Unity.Cinemachine` assembly reference.
- **Modify:** `Packages/manifest.json` — add `com.unity.cinemachine`.
- **Create:** `Assets/_neon/Scripts/Camera/NeonCameraBounds.cs` — pure static helpers for the confiner rect + no-backtrack math (unit-tested).
- **Create:** `Assets/_neon/Scripts/Camera/NeonCameraConfinerDriver.cs` — owns the confiner's `PolygonCollider2D`; `SetRightBound(float)` rebuilds the shape via `NeonCameraBounds`.
- **Create:** `Assets/_neon/Scripts/Camera/CinemachineTargetBinder.cs` — assigns the base vcam's `Follow` to the runtime-spawned Player.
- **Create:** `Assets/_neon/Scripts/Camera/ZoneCameraTrigger.cs` — a 2D trigger that promotes a target vcam's priority (per-zone zoom capability for Plan C).
- **Rewrite (body only, API preserved):** `Assets/_neon/Scripts/Camera/CameraShake.cs` — impulse-based.
- **Modify:** `Assets/_neon/Scripts/Level/Level.cs` — `SetCameraBoundFromProgression` drives `NeonCameraConfinerDriver` instead of `CameraFollow.levelBound`.
- **Create (test):** `NeonCameraBoundsTests.cs` in the `BrainlessLabs.Neon.Tests.EditMode` assembly folder.
- **Scene:** `Assets/_neon/Scenes/Game/03_Level1.unity` — Main Camera gets `CinemachineBrain` + `CinemachineImpulseSource` + the (reimplemented) `CameraShake`; `PixelPerfectCamera` + `CameraFollow` removed. New `CM Base Camera` vcam GameObject with lens/Follow/Confiner2D/ImpulseListener/binder.
- **Remove from camera (not deleted as classes):** `CameraFollow` usage. `CameraFollow.cs` stays in the project (legacy `WaveManager`/editor still reference the type), just not on the live camera.

---

## Task 1: Install & verify Cinemachine 3.x

**Files:**
- Modify: `Packages/manifest.json`
- Modify: `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef`

- [ ] **Step 1: Add the package**

Add Cinemachine via Package Manager (Window → Package Manager → + → Add package by name → `com.unity.cinemachine`), or add to `Packages/manifest.json` dependencies:

```json
"com.unity.cinemachine": "3.1.2"
```

(Use the latest 3.x the registry offers for Unity 6.3.5; must be 3.x, not 2.x.)

- [ ] **Step 2: Add the assembly reference**

In `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef`, add `"Unity.Cinemachine"` to the `references` array (alongside `Unity.2D.PixelPerfect` etc.).

- [ ] **Step 3: Verify the API surface exists (no invented APIs)**

After the domain reload, confirm these CM 3.x types resolve (create a throwaway script or check in the Package Manager API, then delete): `Unity.Cinemachine.CinemachineBrain`, `CinemachineCamera`, `CinemachineImpulseSource`, `CinemachineImpulseListener`, `CinemachineConfiner2D`. Confirm `CinemachineCamera.Lens.OrthographicSize`, `CinemachineCamera.Follow`, `CinemachineCamera.Priority`, and `CinemachineImpulseSource.GenerateImpulseWithForce(float)` exist. **If any name differs in the installed version, adjust the code in later tasks accordingly.**

Run: check `mcp__unityMCP__read_console` (or the Editor console) — expect **no compile errors** after the package + asmdef change.

- [ ] **Step 4: Commit**

```bash
git add Packages/manifest.json Packages/packages-lock.json Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef
git commit -m "build: add Cinemachine 3.x package + asmdef reference (Plan 0)"
```

---

## Task 2: Pure camera-bounds math (TDD)

The right-edge clamp + no-backtrack logic is the only unit-testable core of this plan. Extract it as pure functions so the Cinemachine wiring stays thin.

**Files:**
- Create: `Assets/_neon/Scripts/Camera/NeonCameraBounds.cs`
- Test: `NeonCameraBoundsTests.cs` in the `BrainlessLabs.Neon.Tests.EditMode` assembly folder (the folder whose `.asmdef` is named `BrainlessLabs.Neon.Tests.EditMode`; place under a `Camera/` subfolder).

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using UnityEngine;
using BrainlessLabs.Neon;

namespace BrainlessLabs.Neon.Tests.EditMode
{
    public class NeonCameraBoundsTests
    {
        [Test]
        public void RightEdge_advances_left_bound_monotonically()
        {
            // rightBoundX=20, camHalfW=8, previously reached left=5
            var rect = NeonCameraBounds.ConfinerRect(
                rightBoundX: 20f, camHalfWidth: 8f, camHalfHeight: 4.5f,
                centerY: 0f, previousLeftX: 5f);
            // left edge never retreats below previousLeftX
            Assert.GreaterOrEqual(rect.xMin, 5f);
            // right edge sits at the bound
            Assert.AreEqual(20f, rect.xMax, 0.001f);
        }

        [Test]
        public void RightEdge_is_at_least_one_view_wide()
        {
            // A degenerate bound narrower than the view must widen to >= 2*halfWidth
            var rect = NeonCameraBounds.ConfinerRect(
                rightBoundX: 3f, camHalfWidth: 8f, camHalfHeight: 4.5f,
                centerY: 0f, previousLeftX: 0f);
            Assert.GreaterOrEqual(rect.width, 16f - 0.001f);
        }

        [Test]
        public void Height_spans_two_half_heights_around_centerY()
        {
            var rect = NeonCameraBounds.ConfinerRect(
                rightBoundX: 20f, camHalfWidth: 8f, camHalfHeight: 4.5f,
                centerY: 2f, previousLeftX: 0f);
            Assert.AreEqual(2f - 4.5f, rect.yMin, 0.001f);
            Assert.AreEqual(2f + 4.5f, rect.yMax, 0.001f);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run the EditMode suite (via `mcp__unityMCP__run_tests` with mode `EditMode`, or Test Runner → EditMode). Expected: FAIL — `NeonCameraBounds` does not exist / does not compile.

- [ ] **Step 3: Write the minimal implementation**

```csharp
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Pure geometry for the Cinemachine confiner that replaces the old
    /// CameraFollow right-edge clamp + no-backtrack behavior. No Unity side effects.
    /// </summary>
    public static class NeonCameraBounds
    {
        /// <summary>
        /// Build the confiner rect for a right-edge arena lock. The left edge never
        /// retreats (no backtracking), and the rect is always at least one camera
        /// view wide/tall so Cinemachine's Confiner2D doesn't lock the camera solid.
        /// </summary>
        public static Rect ConfinerRect(
            float rightBoundX, float camHalfWidth, float camHalfHeight,
            float centerY, float previousLeftX)
        {
            float minWidth = camHalfWidth * 2f;
            float left = previousLeftX;
            float right = Mathf.Max(rightBoundX, left + minWidth);
            float bottom = centerY - camHalfHeight;
            return new Rect(left, bottom, right - left, camHalfHeight * 2f);
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run the EditMode suite. Expected: the 3 `NeonCameraBoundsTests` PASS; full suite still green (138 baseline + 3).

- [ ] **Step 5: Commit**

```bash
git add Assets/_neon/Scripts/Camera/NeonCameraBounds.cs <test-file-path>
git commit -m "feat: NeonCameraBounds pure confiner-rect math + tests (Plan 0)"
```

---

## Task 3: Confiner driver (drives the bounding shape from a right-bound)

**Files:**
- Create: `Assets/_neon/Scripts/Camera/NeonCameraConfinerDriver.cs`

- [ ] **Step 1: Write the component**

```csharp
using UnityEngine;
using Unity.Cinemachine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Owns the PolygonCollider2D used by a CinemachineConfiner2D and rebuilds it
    /// from a right-edge arena bound (replaces the old CameraFollow.levelBound clamp).
    /// Left edge advances monotonically = no backtracking. Lives on the base vcam.
    /// </summary>
    [RequireComponent(typeof(PolygonCollider2D))]
    public class NeonCameraConfinerDriver : MonoBehaviour
    {
        [SerializeField] private CinemachineConfiner2D _confiner;
        [SerializeField] private CinemachineCamera _vcam;

        private PolygonCollider2D _shape;
        private float _leftX;
        private bool _initialized;

        void Awake()
        {
            _shape = GetComponent<PolygonCollider2D>();
            if (_confiner == null) _confiner = GetComponent<CinemachineConfiner2D>();
            if (_vcam == null) _vcam = GetComponent<CinemachineCamera>();
        }

        /// <summary>Set the arena right-edge world X. Call from Level per wave.</summary>
        public void SetRightBound(float rightBoundX)
        {
            float halfHeight = _vcam != null ? _vcam.Lens.OrthographicSize : 5f;
            float halfWidth = halfHeight * ((float)Screen.width / Screen.height);
            float centerY = transform.position.y;
            if (!_initialized) { _leftX = rightBoundX - halfWidth * 2f; _initialized = true; }

            Rect r = NeonCameraBounds.ConfinerRect(rightBoundX, halfWidth, halfHeight, centerY, _leftX);
            _leftX = r.xMin;

            _shape.pathCount = 1;
            _shape.SetPath(0, new[]
            {
                new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin),
                new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax),
            });
            if (_confiner != null) _confiner.InvalidateBoundingShapeCache();
        }
    }
}
```

- [ ] **Step 2: Compile check**

Run `mcp__unityMCP__read_console`. Expected: no compile errors. (If `InvalidateBoundingShapeCache` differs in the installed CM version — Task 1 — use the confirmed method name.)

- [ ] **Step 3: Commit**

```bash
git add Assets/_neon/Scripts/Camera/NeonCameraConfinerDriver.cs
git commit -m "feat: NeonCameraConfinerDriver — per-wave arena bound via Confiner2D (Plan 0)"
```

---

## Task 4: Reimplement CameraShake on a Cinemachine impulse (API preserved)

**Files:**
- Modify: `Assets/_neon/Scripts/Camera/CameraShake.cs` (full rewrite of the body; public methods unchanged)

- [ ] **Step 1: Rewrite the class**

```csharp
using UnityEngine;
using Unity.Cinemachine;

namespace BrainlessLabs.Neon {

    /// <summary>
    /// Camera shake, now backed by a Cinemachine impulse (was: CameraFollow.additionalYOffset bob).
    /// Public API (ShowCamShake) is unchanged so FeedbackSystem / UnitActions / DoCamShake are untouched.
    /// Lives on the Main Camera; a CinemachineImpulseListener on the base vcam consumes the impulse.
    /// </summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CameraShake : MonoBehaviour {

        // Serialized fields kept for scene/inspector compatibility (curve now unused by the impulse path).
        public AnimationCurve CameraShakeAnimation;
        public float intensity = .15f;
        public float duration = .3f;

        private CinemachineImpulseSource _source;

        void Awake() {
            _source = GetComponent<CinemachineImpulseSource>();
        }

        //use default settings
        public void ShowCamShake() {
            ShowCamShake(intensity, duration);
        }

        //use custom settings
        public void ShowCamShake(float _intensity, float _duration) {
            if (_source == null) return;
            _source.ImpulseDefinition.ImpulseDuration = Mathf.Max(0.01f, _duration);
            _source.GenerateImpulseWithForce(_intensity);
        }
    }
}
```

Note: the `AnimationCurve`/`intensity`/`duration` fields stay so existing scene serialization doesn't error; the impulse *shape* is configured on the `CinemachineImpulseSource` component (Task 6). The `[RequireComponent(CameraFollow)]` and `additionalYOffset` coupling are gone.

- [ ] **Step 2: Compile check**

Run `mcp__unityMCP__read_console`. Expected: no errors. `FeedbackSystem`, `UnitActions`, `DoCamShake` still compile against the unchanged `ShowCamShake` signatures.

- [ ] **Step 3: Commit**

```bash
git add Assets/_neon/Scripts/Camera/CameraShake.cs
git commit -m "refactor: CameraShake backed by Cinemachine impulse (API preserved) (Plan 0)"
```

---

## Task 5: Runtime vcam target binder

The player spawns at runtime, so the base vcam's `Follow` can't be wired in the scene.

**Files:**
- Create: `Assets/_neon/Scripts/Camera/CinemachineTargetBinder.cs`

- [ ] **Step 1: Write the component**

```csharp
using UnityEngine;
using Unity.Cinemachine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Binds a CinemachineCamera's Follow target to the runtime-spawned Player.
    /// Retries until the Player tag exists (Level spawns the player after boot).
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public class CinemachineTargetBinder : MonoBehaviour
    {
        [SerializeField] private string _targetTag = "Player";
        private CinemachineCamera _vcam;

        void Awake() => _vcam = GetComponent<CinemachineCamera>();

        void Update()
        {
            if (_vcam.Follow != null) return;
            var target = GameObject.FindGameObjectWithTag(_targetTag);
            if (target != null) _vcam.Follow = target.transform;
        }
    }
}
```

- [ ] **Step 2: Compile check**

Run `mcp__unityMCP__read_console`. Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/_neon/Scripts/Camera/CinemachineTargetBinder.cs
git commit -m "feat: CinemachineTargetBinder — bind vcam Follow to spawned player (Plan 0)"
```

---

## Task 6: Scene surgery — build the Cinemachine rig on 03_Level1

This is editor/scene work (verified in play mode, not unit-tested). Use the Unity MCP tools (`mcp__unityMCP__manage_gameobject`, `manage_scene`) or do it by hand in the editor. **Boot via Recipe 4 (DI bootstrap) to play-test — do not press Play directly in the level scene.**

**Files:**
- Modify: `Assets/_neon/Scenes/Game/03_Level1.unity`

- [ ] **Step 1: Convert the Main Camera**
  - Set the `Camera` component `Projection = Orthographic`.
  - Add `CinemachineBrain`.
  - Add `CinemachineImpulseSource` (its `ImpulseDefinition` = the shake shape: short duration ~0.2s, a 6-D or dissipating shape; leave defaults for now — tuned in Task 8).
  - Add the (reimplemented) `CameraShake` (now requires `CinemachineImpulseSource` — already added).
  - **Remove** `PixelPerfectCamera` and `CameraFollow` from the Main Camera.

- [ ] **Step 2: Create the base vcam** — a new GameObject `CM Base Camera` with:
  - `CinemachineCamera`: `Lens.ModeOverride = Orthographic`, `Lens.OrthographicSize = 8` (placeholder zoomed-out baseline; tuned in Task 8), `Priority = 10`.
  - A body/position component for 2D framing (e.g. `CinemachinePositionComposer`) with a little dead zone + damping.
  - `PolygonCollider2D` (bounding shape) + `CinemachineConfiner2D` (BoundingShape2D = that collider) + `NeonCameraConfinerDriver` (wire its `_confiner` + `_vcam`).
  - `CinemachineImpulseListener` (so shake reaches the camera).
  - `CinemachineTargetBinder`.

- [ ] **Step 3: Compile + boot verification**

Run `mcp__unityMCP__read_console` (no errors). Boot the game via Recipe 4 and load Level 01. Verify **in the Game view** (not MCP screenshots — they miss immediate-mode draws, per spec §10):
  - The avatar + enemies are noticeably **smaller** than before (baseline zoomed out).
  - The camera **follows** the player smoothly.
  - Landing a hit produces a **camera shake** (impulse path works).

- [ ] **Step 4: Commit**

```bash
git add Assets/_neon/Scenes/Game/03_Level1.unity
git commit -m "feat: Cinemachine ortho rig on 03_Level1 (brain, base vcam, confiner, impulse) (Plan 0)"
```

---

## Task 7: Route the per-wave arena lock through the confiner

**Files:**
- Modify: `Assets/_neon/Scripts/Level/Level.cs` (method `SetCameraBoundFromProgression`, ~lines 78–98)

- [ ] **Step 1: Read the current method**

Confirm the body matches the ground-truth summary (creates `_dynamicCameraBound`, sets `Camera.main.GetComponent<CameraFollow>().levelBound`).

- [ ] **Step 2: Replace the CameraFollow wiring with the confiner driver**

Change the tail of `SetCameraBoundFromProgression` from setting `camFollow.levelBound` to driving `NeonCameraConfinerDriver`. Keep computing `worldX` exactly as before; feed it as the right bound:

```csharp
// after computing worldX (the arena right-edge world X):
_dynamicCameraBound.transform.position = new Vector3(worldX, transform.position.y, 0f);

var confiner = Object.FindFirstObjectByType<NeonCameraConfinerDriver>();
if (confiner != null)
    confiner.SetRightBound(worldX);
```

(Leave the `_dynamicCameraBound` creation/positioning in place — it's still a useful editor gizmo. Remove only the `Camera.main.GetComponent<CameraFollow>()` block.)

- [ ] **Step 3: Compile check**

Run `mcp__unityMCP__read_console`. Expected: no errors. (`CameraFollow` type still exists for the legacy `WaveManager`; we only stopped *using* it here.)

- [ ] **Step 4: Boot verification**

Boot via Recipe 4, play Level 01 to a wave that has `HasCameraBound` (wave B locks at 0.28 per spec §7). Verify the camera **stops advancing at the arena bound** and does not backtrack.

- [ ] **Step 5: Commit**

```bash
git add Assets/_neon/Scripts/Level/Level.cs
git commit -m "feat: per-wave arena lock via NeonCameraConfinerDriver (Plan 0)"
```

---

## Task 8: Tune the zoomed-out baseline

**Files:**
- Modify: `Assets/_neon/Scenes/Game/03_Level1.unity` (base vcam lens + impulse source shape)

- [ ] **Step 1: Set the baseline lens size**

With the game booted and Level 01 loaded, adjust `CM Base Camera` → `Lens.OrthographicSize` at the target build resolution until the avatar + enemies read at the intended on-screen size (this is the non-negotiable "not too big" goal). Record the chosen value in this plan's changelog note. Verify pixel art is acceptable with point filtering (PPv2 in Plan B will further soften).

- [ ] **Step 2: Tune the shake**

Trigger hits; adjust the `CinemachineImpulseSource.ImpulseDefinition` (duration/amplitude/dissipation) so the shake feels close to the old bob but reads at the new zoom. Confirm `FeedbackSystem`'s per-verb intensities still feel right (they pass `profile.ShakeIntensity`/`ShakeSeconds` unchanged).

- [ ] **Step 3: Commit**

```bash
git add Assets/_neon/Scenes/Game/03_Level1.unity
git commit -m "tune: camera baseline ortho size + impulse shake shape (Plan 0)"
```

---

## Task 9: Per-zone zoom capability (trigger → vcam priority)

Delivers the *mechanism* for real per-zone zoom; Plan C authors the actual zone vcams for Level 01.

**Files:**
- Create: `Assets/_neon/Scripts/Camera/ZoneCameraTrigger.cs`

- [ ] **Step 1: Write the trigger component**

```csharp
using UnityEngine;
using Unity.Cinemachine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// When the player enters this 2D trigger, promote the assigned zone vcam above the
    /// base vcam so the CinemachineBrain blends to the zone's framing/zoom. On exit, demote.
    /// Level 01 places one per corridor/plaza zone (authored in Plan C).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ZoneCameraTrigger : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _zoneVcam;
        [SerializeField] private int _activePriority = 20;
        [SerializeField] private int _inactivePriority = 5;
        [SerializeField] private string _playerTag = "Player";

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        void Start()
        {
            if (_zoneVcam != null) _zoneVcam.Priority = _inactivePriority;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_zoneVcam != null && other.CompareTag(_playerTag))
                _zoneVcam.Priority = _activePriority;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (_zoneVcam != null && other.CompareTag(_playerTag))
                _zoneVcam.Priority = _inactivePriority;
        }
    }
}
```

(If `CinemachineCamera.Priority` is a `PrioritySettings` struct in the installed CM version — Task 1 — assign via the confirmed form, e.g. `_zoneVcam.Priority = new PrioritySettings { Value = _activePriority };`.)

- [ ] **Step 2: Compile check**

Run `mcp__unityMCP__read_console`. Expected: no errors.

- [ ] **Step 3: Smoke-test the capability**

Temporarily add one extra `CinemachineCamera` (tighter `OrthographicSize`, e.g. 5) + a `ZoneCameraTrigger` box near the player spawn in 03_Level1. Boot via Recipe 4; walk the player into the box and confirm the camera **blends to the tighter zoom**, and back out on exit. Then **remove the temporary vcam + trigger** (Plan C authors the real ones) — or leave a single disabled example prefab. Note what you did.

- [ ] **Step 4: Commit**

```bash
git add Assets/_neon/Scripts/Camera/ZoneCameraTrigger.cs
git commit -m "feat: ZoneCameraTrigger — per-zone vcam priority blend capability (Plan 0)"
```

---

## Task 10: Green-build sweep & CameraFollow decommission

**Files:**
- Verify across: `Assets/_neon/Scripts/**`, `03_Level1.unity`

- [ ] **Step 1: Confirm no live references to the removed camera components**

Grep to confirm the only remaining `CameraFollow` users are the `[Obsolete]` `WaveManager` and the editor (`CameraFollowEditor`), which are fine (null-guarded / editor-only):

Run: `rg -n "GetComponent<CameraFollow>|CameraFollow " Assets/_neon/Scripts`
Expected: hits only in `WaveManager.cs` (legacy, null-guarded), `CameraFollowEditor.cs`, `CameraFollow.cs`, `CameraShake` no longer references it.

- [ ] **Step 2: Full EditMode suite**

Run the full EditMode suite (`mcp__unityMCP__run_tests`, mode EditMode). Expected: **all green** — 138 baseline + 3 new (`NeonCameraBoundsTests`) = 141, no regressions.

- [ ] **Step 3: Full boot play-test (the Plan 0 acceptance run)**

Boot via Recipe 4, play Level 01 start→first arena. Confirm all of:
  - Baseline zoom reads (avatar/enemies small enough — the non-negotiable).
  - Camera follows the player; no jitter/NaN.
  - Shake fires on hits (impulse).
  - Arena lock stops the camera at a wave bound; no backtracking.
  - No console errors.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "chore: Plan 0 green-build sweep — camera rig verified, CameraFollow off the live camera"
```

---

## Self-Review

**1. Spec coverage (§4 Plan 0):**
- "drop PixelPerfectCamera → Cinemachine ortho rig" → Tasks 1, 6. ✓
- "zoomed-out baseline" → Task 8. ✓
- "per-zone vcams + blends" → Task 9 (capability; authoring in Plan C, noted). ✓
- "Confiner2D bounds; per-wave arena locks map onto confiner swaps" → Tasks 2, 3, 7. ✓
- "Impulse shake, migrating CameraShake" → Task 4 (API-preserving), 6, 8. ✓
- "single-player → plain Follow, no TargetGroup" → Task 5, 6. ✓
- "crispness managed ourselves" → Task 8 note (point filtering; PPv2 softens). ✓

**2. Placeholder scan:** No "TBD/handle later". Editor/scene steps give concrete components + values; the two version-sensitive spots (CM `Priority` type, `InvalidateBoundingShapeCache`, extension API) are called out with a Task-1 confirmation gate rather than guessed silently — this is honest handling of a not-yet-installed third-party API, not a placeholder.

**3. Type consistency:** `NeonCameraBounds.ConfinerRect(...)` signature matches between Task 2 test, Task 2 impl, and Task 3 caller. `NeonCameraConfinerDriver.SetRightBound(float)` matches between Task 3 def and Task 7 caller. `CameraShake.ShowCamShake()` / `ShowCamShake(float,float)` preserved exactly for the three untouched callers.

**Known non-unit-tested surface (honest flag):** Tasks 6/8/9 are Unity scene + Cinemachine config, verified in play mode (Recipe 4), not by unit tests — camera behavior isn't meaningfully unit-testable. The pure math (Task 2) is the extracted testable core. This matches the spec's "runtime is ground truth" posture.

---

## Execution Handoff

**Plan complete and saved to `docs/superpowers/plans/2026-07-05-feel-and-level-plan0-camera.md`.** Two execution options:

1. **Subagent-Driven (recommended)** — a fresh subagent per task, review between tasks, fast iteration. Note: Tasks 6/8/9 need a live Unity Editor (via the Unity MCP) to do scene work + play-test — the executing agent must have that.
2. **Inline Execution** — execute tasks in this session with checkpoints for review.

Which approach?
