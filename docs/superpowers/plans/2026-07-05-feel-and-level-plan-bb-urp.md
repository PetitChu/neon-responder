# Feel & Level — Plan B.b: URP 2D Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate the project from the built-in render pipeline to **URP with the 2D Renderer**, port Plan B's post-processing to the URP Volume system at visual parity, and remove PPv2 entirely — so Plan C authors level art against the final pipeline (2D lights, render features, and an SRP for the ECS-graphics track later) instead of a dead-end BiRP stack.

**Architecture:** Assign a URP Pipeline Asset (Renderer2D) as the Default Render Pipeline (per-Quality slots stay None so the default applies everywhere). Custom instanced ambient shader `Neon/InstancedUnlit` is rewritten URP-compatible **keeping the `Graphics.DrawMeshInstanced` path** (Entities Graphics is explicitly out). Sprites move to `Sprite-Lit-Default` so 2D lights work later; a single global `Light2D` at intensity 1 holds brightness at parity (no light *authoring* here). Post ports to URP Volumes: a global volume (Bloom / ColorAdjustments / Vignette at Plan-B parity) + a whiff volume; `WhiffPostFx` is re-typed from PPv2's `PostProcessVolume` to `UnityEngine.Rendering.Volume` — **same weight-pulse design** — while `WhiffFx` (pure curve + its tests) and `FeedbackSystem` are untouched. Then PPv2 is removed wholesale.

**Tech Stack:** Unity 6.3.5, **URP 17.3.0** (2D Renderer; already in the manifest), `com.unity.feature.2d`, `Light2D`, URP Volume framework (`UnityEngine.Rendering` / `UnityEngine.Rendering.Universal`), C#, HLSL, Unity Test Framework (EditMode). Source: handoff `docs/superpowers/plans/2026-07-05-feel-and-level-plan-bb-urp-handoff.md`; spec `…-feel-and-level-pass-design.md` §6 (PPv2 decision now superseded by this plan). Branch: `claude/feel-and-level-pass` (Plans 0/A/A.a/B merged; B tip `5b0a274`, 161/161 tests).

**Scope boundary (from handoff):** **IN** — URP asset+Renderer2D, HDR on / MSAA off, shader+sprite material migration, URP post at parity, remove PPv2, lighting *foundation only* (one global light for parity). **OUT** — 2D light authoring, rain / wet-ground reflections, Entities Graphics, PixelPerfect (already gone in Plan 0), any balance/feel change. B.b must land **before Plan C**.

**No new tests:** this is a pipeline/config/shader migration — not unit-testable. `WhiffFx`'s 5 tests stay verbatim and green (161 total); everything else is play-verified per Recipe 4. This mirrors Plan 0/B's scene-work posture.

---

## Ground truth (from handoff, branch tip `5b0a274`)

- **Plan B (PPv2) is live:** `PostProcessLayer` on MainCamera (volume layer = `PostProcessing`, layer 10, trigger self); `Post Global` GO (`PostProcessVolume` prio 0, `NeonPostBase.asset`: bloom 2.5 / threshold 0.9 / knee 0.5, LDR grade sat +10 con +10, vignette 0.25); `Post Whiff` GO (prio 10, weight 0, `NeonPostWhiff.asset`: saturation −100) + `WhiffPostFx`.
- **Code to preserve:** `WhiffFx` (pure curve, 5 tests — verbatim); `WhiffPostFx` (weight-pulse 1→0 on unscaled time); `FeedbackSystem` (only calls `_whiffPostFx?.Pulse(_config.WhiffFlashSeconds)`; record-scratch SFX is pipeline-independent).
- **URP 17.3.0 already in manifest** (installed, unassigned — Graphics/Quality pipeline slots all None = BiRP today). `com.unity.feature.2d` present.
- **PPv2 footprint to remove:** `com.unity.postprocessing` 3.4.0 (manifest); `Unity.Postprocessing.Runtime` ref in `BrainlessLabs.Neon.asmdef`; `NeonPostBase`/`NeonPostWhiff` assets; the scene components above; auto-added `UNITY_POST_PROCESSING_STACK_V2` scripting defines (verify they auto-remove with the package).
- **Swarm/ambient rendering:** `Neon/InstancedUnlit` (`Assets/_neon/Shaders/NeonInstancedUnlit.shader`) + `Graphics.DrawMeshInstanced` in `SwarmRenderRig` (verify via `UnityStats.instancedBatches` — instanced draws are invisible in screenshots). Chaff proxies are `SpriteRenderer`s tinted by `_hotColor`/`_finishReadyColor`.
- **Settings quirks:** BiRP graphics-tier HDR is FALSE (irrelevant post-migration — the URP asset owns HDR; turn it **on** there); Quality "Ultra" has 2× MSAA (turn **off** — useless for 2D, conflicts with HDR targets).
- **Perf baselines @150 chaff (editor, same probe):** PPv2 on + live fight = **189 FPS**; post off = **224 FPS**; M1 pre-post = ~197 FPS.

## Execution constraints (hard-won — from handoff, obey during execution)

- **DI-bootstrap to play-test** (Recipe 4, `neon-recipes`) — never Play directly in a level scene.
- **Never write assets/scripts mid-play** — compile defers to play-exit and blocks scene MCP tools.
- Screenshots via `ScreenCapture.CaptureScreenshot` to the scratchpad — never into `Assets/`; never trust the MCP screenshot tool for instanced draws.
- Scope test runs to assembly `BrainlessLabs.Neon.Tests.EditMode` (full-project runs fail on third-party DTT tests).
- **Whiff verify:** raise `UnitActions.onVerbWhiffed` **deferred** (an `EditorApplication.update` one-shot ≥0.5 s after the bridge call) and track `Volume.weight` max — a bridge-call stall frame eats a 0.25 s pulse invisibly.
- Use **`mcp-for-unity` `execute_code`** for probes (`aura-unity` MCP is bound to another project).
- **Front-load play probes** — an idle player dies to wave-1 mobs within minutes (faster at forced density).

## File structure (what changes)

- **Create:** `Assets/_neon/Rendering/NeonURP-Pipeline.asset` (UniversalRenderPipelineAsset) + `Assets/_neon/Rendering/NeonURP-Renderer2D.asset` (Renderer2DData).
- **Modify:** `ProjectSettings/GraphicsSettings.asset` (Default Render Pipeline) + `QualitySettings.asset` (MSAA off; leave per-Quality RP None).
- **Rewrite (in place):** `Assets/_neon/Shaders/NeonInstancedUnlit.shader` → URP-compatible instanced unlit (same shader name `Neon/InstancedUnlit`, so `SwarmAmbient.mat` needs no rewiring).
- **Modify:** `Assets/_neon/Scripts/Swarm/SwarmRenderRig.cs` — assign a `Sprite-Lit-Default` material to chaff proxies (new serialized `_chaffMaterial`).
- **Create:** `Assets/_neon/Rendering/NeonVolumeBase.asset` + `NeonVolumeWhiff.asset` (URP `VolumeProfile`s).
- **Modify:** `Assets/_neon/Scripts/Feel/WhiffPostFx.cs` — `PostProcessVolume` → `UnityEngine.Rendering.Volume`.
- **Modify:** `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef` — drop `Unity.Postprocessing.Runtime`; ensure `Unity.RenderPipelines.Core.Runtime` (for `Volume`) is referenced.
- **Modify:** `Assets/_neon/Scenes/Game/03_Level1.unity` — Renderer2D global `Light2D`; URP `Volume` GOs replace PPv2 volumes; camera `UniversalAdditionalCameraData` post enabled; remove `PostProcessLayer`.
- **Delete:** `Packages` PPv2 entry; `NeonPostBase.asset`/`NeonPostWhiff.asset`.
- **Unchanged:** `WhiffFx.cs` + `WhiffFxTests.cs`, `FeedbackSystem.cs` whiff call site.

---

## Task 1: Create + assign the URP 2D pipeline

**Files:**
- Create: `Assets/_neon/Rendering/NeonURP-Pipeline.asset`, `Assets/_neon/Rendering/NeonURP-Renderer2D.asset`
- Modify: `ProjectSettings/GraphicsSettings.asset`, `ProjectSettings/QualitySettings.asset`

- [ ] **Step 1: Create the pipeline + 2D renderer**

Assets → Create → Rendering → **URP Universal Renderer** is the 3D one — instead create **URP → 2D Renderer (Renderer2D Data)** → `NeonURP-Renderer2D.asset`, and a **URP Pipeline Asset** → `NeonURP-Pipeline.asset`, and set the pipeline's Renderer list to the Renderer2D. On the pipeline asset: **HDR = on**, and leave scaling/shadows at 2D-sane defaults.

- [ ] **Step 2: Assign as Default Render Pipeline**

`ProjectSettings/GraphicsSettings` → Scriptable Render Pipeline Settings = `NeonURP-Pipeline`. **Leave every Quality level's Render Pipeline Asset = None** (so the graphics default applies uniformly — per the handoff).

- [ ] **Step 3: Quality — MSAA off**

`QualitySettings` → for all levels (esp. "Ultra"): **Anti Aliasing = Disabled** (2× MSAA is useless for 2D and conflicts with HDR targets).

- [ ] **Step 4: Compile + expect broken materials**

`mcp-for-unity` `read_console`. Expected: no *compile* errors. The scene will render with **magenta/pink** materials (BiRP shaders don't run under URP) — that is expected and fixed in Tasks 2–3. Confirm the game boots via Recipe 4 (DI intact) even if pink.

- [ ] **Step 5: Commit**

```bash
git add Assets/_neon/Rendering/NeonURP-Pipeline.asset* Assets/_neon/Rendering/NeonURP-Renderer2D.asset* ProjectSettings/GraphicsSettings.asset ProjectSettings/QualitySettings.asset
git commit -m "build: assign URP 2D pipeline as default (HDR on, MSAA off) (Plan B.b)"
```

---

## Task 2: Migrate the instanced ambient shader to URP

**Files:**
- Rewrite: `Assets/_neon/Shaders/NeonInstancedUnlit.shader` (keep the name `Neon/InstancedUnlit`)

- [ ] **Step 1: Rewrite the shader URP-compatible (instancing preserved)**

```hlsl
Shader "Neon/InstancedUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return tex * _Color;
            }
            ENDHLSL
        }
    }
}
```

(`_Color` is a per-material uniform — `SwarmRenderRig.DrawAmbient` passes only matrices to `DrawMeshInstanced`, no per-instance color array, so this matches usage. `DrawMeshInstanced` feeds `unity_ObjectToWorld` per instance, which `TransformObjectToHClip` consumes via the instancing macros.)

- [ ] **Step 2: Compile check**

`read_console`. Expected: shader compiles; `SwarmAmbient.mat` (uses `Neon/InstancedUnlit`) is no longer pink in the inspector preview.

- [ ] **Step 3: Play-verify the instanced draw (the critical one)**

Boot via Recipe 4, Level 01, swarm on. Confirm **ambient quads render** and `UnityEditor.UnityStats.instancedBatches ≥ 1` (probe via `mcp-for-unity` `execute_code`; do **not** use the MCP screenshot tool). If ambient is invisible: the shader/instancing path is wrong — do not proceed until `instancedBatches ≥ 1`.

- [ ] **Step 4: Commit**

```bash
git add Assets/_neon/Shaders/NeonInstancedUnlit.shader
git commit -m "feat: URP-compatible instanced unlit shader (DrawMeshInstanced preserved) (Plan B.b)"
```

---

## Task 3: Sprites → Sprite-Lit-Default + global 2D light (parity)

**Files:**
- Modify: `Assets/_neon/Scripts/Swarm/SwarmRenderRig.cs`
- Modify: `Assets/_neon/Scenes/Game/03_Level1.unity` (+ pink-material sweep across shipping scenes)

- [ ] **Step 1: Give chaff proxies a lit material**

Add a serialized material field and assign it to each proxy in `Start` (proxies currently take the default sprite material):

```csharp
[SerializeField] private Material _chaffMaterial; // assign Sprite-Lit-Default so 2D lights reach chaff later

// in the proxy-creation loop, after AddComponent<SpriteRenderer>():
if (_chaffMaterial != null) spriteRenderer.material = _chaffMaterial;
```

(`spriteRenderer.color` tinting — `_hotColor`/`_finishReadyColor` — still applies with `Sprite-Lit-Default`.)

- [ ] **Step 2: Sweep sprite materials in shipping scenes**

In `03_Level1` (and any other shipping scene showing pink), set `SpriteRenderer` materials to **`Sprite-Lit-Default`** (Unity default; sprites with the built-in default already fall back, but explicitly set the ones rendering pink). Assign `_chaffMaterial = Sprite-Lit-Default` on the `SwarmRenderRig`. Note any addon/template materials that need a URP shader.

- [ ] **Step 3: Add the parity global light**

Add a `Light2D` (Global, intensity 1, white) to `03_Level1` (Renderer2D must be active). With lit sprites + a global light at 1, brightness matches BiRP unlit. **No other lights** (authoring is Plan C/mood).

- [ ] **Step 4: Play-verify parity**

Boot via Recipe 4. Sprites (player, chaff, environment) render at ~pre-migration brightness, **no pink**. Chaff tint (hot / finish-ready) still shows. No console errors.

- [ ] **Step 5: Commit**

```bash
git add Assets/_neon/Scripts/Swarm/SwarmRenderRig.cs Assets/_neon/Scenes/Game/03_Level1.unity
git commit -m "feat: sprites → Sprite-Lit-Default + global Light2D for URP parity (Plan B.b)"
```

---

## Task 4: Port post-processing to URP Volumes (parity + whiff)

**Files:**
- Create: `Assets/_neon/Rendering/NeonVolumeBase.asset`, `Assets/_neon/Rendering/NeonVolumeWhiff.asset` (VolumeProfiles)
- Modify: `Assets/_neon/Scripts/Feel/WhiffPostFx.cs`, `Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef`
- Modify: `03_Level1.unity`

- [ ] **Step 1: Enable post on the camera**

On the Plan-0 Main Camera, its `UniversalAdditionalCameraData`: **Post Processing = on**; **Volume Mask = `PostProcessing`** layer (reuse Plan B's layer 10). Cinemachine still drives the transform; URP handles post per-camera.

- [ ] **Step 2: Base volume profile (parity with `NeonPostBase`)**

Create `NeonVolumeBase.asset` (VolumeProfile) with URP overrides:
- **Bloom** — intensity 2.5, threshold 0.9 (URP has no "knee"; set scatter ~0.5 to approximate; HDR is on so bloom has range).
- **Color Adjustments** — saturation +10, contrast +10.
- **Vignette** — intensity 0.25.

Add `GameObject "Post Global"` (reuse/replace the PPv2 one) on the `PostProcessing` layer with a URP `Volume`: `Is Global = true`, `Priority = 0`, profile = `NeonVolumeBase`.

- [ ] **Step 3: Whiff volume profile + re-type WhiffPostFx**

Create `NeonVolumeWhiff.asset` with a **Color Adjustments** override, **saturation = −100**. Add `GameObject "Post Whiff"` on the `PostProcessing` layer: URP `Volume` `Is Global = true`, `Priority = 10`, `Weight = 0`, profile = `NeonVolumeWhiff`.

Re-type `WhiffPostFx` (design unchanged — only the volume type):

```csharp
using UnityEngine;
using UnityEngine.Rendering; // Volume (Core.Runtime)

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Fullscreen whiff desaturate. Owns a high-priority global URP Volume whose profile grades
    /// saturation to grey; Pulse() drives its weight 1→0 (WhiffFx curve) on unscaled time.
    /// (URP port of the Plan B PPv2 component — same weight-pulse design.)
    /// </summary>
    public class WhiffPostFx : MonoBehaviour
    {
        [SerializeField] private Volume _whiffVolume; // isGlobal, priority > base, ColorAdjustments saturation -100, weight 0 at rest

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
            _elapsed += Time.unscaledDeltaTime;
            _whiffVolume.weight = WhiffFx.WeightAt(_elapsed, _duration);
            if (_elapsed >= _duration) { _whiffVolume.weight = 0f; _active = false; }
        }
    }
}
```

Wire the `Post Whiff` `Volume` into `WhiffPostFx._whiffVolume`. `WhiffFx`, its tests, and `FeedbackSystem` are **unchanged**.

- [ ] **Step 4: asmdef refs**

In `BrainlessLabs.Neon.asmdef`: remove `"Unity.Postprocessing.Runtime"`. Ensure `"Unity.RenderPipelines.Core.Runtime"` is referenced (provides `UnityEngine.Rendering.Volume`). Add `"Unity.RenderPipelines.Universal.Runtime"` only if any runtime code touches URP override types (the profiles are authored as assets, so likely not needed — add only if compile demands).

- [ ] **Step 5: Compile + play-verify post + whiff**

`read_console` — no errors. Boot via Recipe 4: bloom/grade/vignette read like the Plan-B BiRP look; HUD/finish-prompt crisp. Whiff a verb — confirm the desaturate pulse fires (raise `UnitActions.onVerbWhiffed` **deferred** and track `Volume.weight` max, per the constraint). SFX still plays.

- [ ] **Step 6: Commit**

```bash
git add Assets/_neon/Rendering/NeonVolumeBase.asset* Assets/_neon/Rendering/NeonVolumeWhiff.asset* Assets/_neon/Scripts/Feel/WhiffPostFx.cs Assets/_neon/Scripts/BrainlessLabs.Neon.asmdef Assets/_neon/Scenes/Game/03_Level1.unity
git commit -m "feat: URP Volume post (bloom/grade/vignette) + whiff desaturate re-typed to URP Volume (Plan B.b)"
```

---

## Task 5: Remove PPv2 completely

**Files:**
- Modify: `Packages/manifest.json`; Delete: `NeonPostBase.asset`, `NeonPostWhiff.asset`; Modify: `03_Level1.unity`, ProjectSettings defines

- [ ] **Step 1: Strip the scene of PPv2 components**

Remove the `PostProcessLayer` from the Main Camera and delete any leftover PPv2 `PostProcessVolume` components/GOs (the URP `Volume` GOs from Task 4 replace them). Confirm nothing in the scene references `NeonPostBase`/`NeonPostWhiff`.

- [ ] **Step 2: Delete the PPv2 profile assets**

Delete `Assets/_neon/Rendering/NeonPostBase.asset` + `.meta` and `NeonPostWhiff.asset` + `.meta`.

- [ ] **Step 3: Remove the package**

Remove `"com.unity.postprocessing": "3.4.0"` from `Packages/manifest.json`. Let the resolver update `packages-lock.json`.

- [ ] **Step 4: Verify the scripting defines cleared**

Confirm `UNITY_POST_PROCESSING_STACK_V2` is gone from `ProjectSettings/ProjectSettings.asset` scripting-define lists across platforms (should auto-remove with the package; strip manually if any linger).

- [ ] **Step 5: Compile clean**

`read_console`. Expected: **no errors, no "PostProcessing" namespace references** anywhere. (`rg -n "UnityEngine.Rendering.PostProcessing|PostProcessVolume|PostProcessLayer|Postprocessing.Runtime" Assets/_neon` → no hits.)

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "chore: remove PPv2 entirely — package, assets, scene components, defines (Plan B.b)"
```

---

## Task 6: Gates & acceptance

- [ ] **Step 1: EditMode suite**

Run EditMode scoped to `BrainlessLabs.Neon.Tests.EditMode`. Expected: **161/161 green** (`WhiffFx` tests unchanged; no new tests in B.b).

- [ ] **Step 2: Perf gate @150 chaff**

Force cap 150 (density-probe recipe: box `SwarmBridge._config` ChaffCap=150 / rate=40 / null ChaffCapCurve, bump `UnitDefinitionAsset._maxFollowers`, **restore after**). Measure live-fight FPS with URP post on, via the same editor probe. Compare to baselines: PPv2 was **189 FPS** on / **224** off / ~**197** M1. Expected: URP 2D + Volume post lands in the same ballpark (≥ ~180 on-post). Record the number; investigate if materially below.

- [ ] **Step 3: Instanced draw + acceptance play-test (Recipe 4)** — confirm all:
  - `instancedBatches ≥ 1` (ambient draws under URP).
  - No pink materials in `03_Level1`; sprites at parity brightness; chaff tints show.
  - Bloom/grade/vignette read like Plan B; HUD + finish prompt crisp.
  - Whiff = record-scratch + fullscreen desaturate pulse (deferred-probe verified); no red flash.
  - No console errors.

- [ ] **Step 4: Other-scene smoke**

Boot the menu / level-select / other shipping scenes (via the normal flow) — confirm they load and render (no pink, no errors). Full art-parity of non-Level-01 scenes is not required, but they must not be broken.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore: Plan B.b green sweep — URP 2D live, PPv2 gone, parity + perf verified"
```

---

## Self-Review

**1. Handoff scope coverage:**
- URP asset + Renderer2D, default RP, HDR on / MSAA off → Task 1. ✓
- Shader migration (`Neon/InstancedUnlit` URP, DrawMeshInstanced kept) → Task 2. ✓
- Sprites → Sprite-Lit-Default + pink sweep → Task 3. ✓
- Lighting foundation (one global `Light2D`, parity, no authoring) → Task 3. ✓
- URP post at parity + whiff re-type (`WhiffFx`/`FeedbackSystem` untouched) → Task 4. ✓
- Remove PPv2 completely (package, asmdef, assets, scene, defines) → Task 5. ✓
- Gates (161/161, Recipe-4, perf vs baselines, `instancedBatches ≥ 1`, other scenes smoke) → Task 6. ✓
- OUT items (light authoring, rain/reflections, Entities Graphics, PixelPerfect, balance) — none touched. ✓

**2. Placeholder scan:** No "TBD/handle later". URP-version-sensitive spots (2D Renderer creation menu path, Bloom "scatter" vs BiRP "knee", `UniversalAdditionalCameraData` field names, whether the Universal.Runtime asmdef ref is needed) each carry a concrete confirm/approximate note. The shader is the highest risk and has an explicit `instancedBatches ≥ 1` gate before proceeding.

**3. Consistency:** shader keeps the name `Neon/InstancedUnlit` so `SwarmAmbient.mat` needs no rewiring. `WhiffPostFx.Pulse(float)` signature preserved → `FeedbackSystem` untouched. `WhiffFx.WeightAt` reused verbatim. Volume type `UnityEngine.Rendering.Volume` matches the Core.Runtime asmdef ref added in Task 4.

**Ordering guarantee:** URP asset (1) → fix rendering so nothing's pink (2 shader, 3 sprites) → stand up URP post (4) → only then remove PPv2 (5), so there's never a frame with no working post/pipeline. Perf/acceptance last (6).

**Known non-unit-tested surface:** the entire migration is play-verified (Recipe 4) + perf-probed — pipeline/shader/config isn't unit-testable. `WhiffFx` remains the sole testable core, unchanged.

---

## Execution Handoff

**Plan complete and saved to `docs/superpowers/plans/2026-07-05-feel-and-level-plan-bb-urp.md`.** Runs after Plan B, before Plan C. Two execution options:

1. **Subagent-Driven (recommended)** — fresh subagent per task; **every task** needs a live Unity Editor over `mcp-for-unity` (pipeline/shader/scene work + play-test). Task 2 (shader) is the highest-risk gate — verify `instancedBatches ≥ 1` before moving on.
2. **Inline Execution** — here with checkpoints.

Which approach — or review the plan yourself first?
