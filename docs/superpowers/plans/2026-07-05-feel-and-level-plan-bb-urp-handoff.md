# Plan B.b handoff — URP 2D migration (Feel & Level pass)

> **For the planner:** write `docs/superpowers/plans/2026-07-05-feel-and-level-plan-bb-urp.md`.
> Plan chain becomes `[0 ∥ A ∥ B] → B.b → C → [D ∥ E]` — B.b MUST land before Plan C authors
> level art (every BiRP-authored asset after this point is rework).

**Decision (Sebastien, 2026-07-05):** migrate the project from built-in RP to **URP with the 2D
Renderer**, and **remove PPv2 entirely**. Wanted end-state capabilities driving this: 2D lights
(neon pools, rim light), wet-ground reflection / rain render features, and Entities Graphics for
the ECS swarm track (requires an SRP). BiRP + PPv2 are both maintenance-mode dead ends. Plan B's
*architecture* (whiff volume-weight pulse, FeedbackSystem seam, pure `WhiffFx` curve) ports ~1:1;
its *stack* (PPv2 package, profiles, components) does not and gets cleaned up.

## Ground truth (branch `claude/feel-and-level-pass`, tip `5b0a274`, pushed)

- **Plan B (PPv2) is live and verified** — commits `40040c4` (package + asmdef), `36994b9` (base
  volume), `5820bf3` (whiff desaturate), `5b0a274` (green sweep). 161/161 EditMode tests.
- **Scene `03_Level1`:** `PostProcessLayer` on MainCamera (volume layer = `PostProcessing` layer 10,
  trigger = self) · `Post Global` GO (PostProcessVolume prio 0, `NeonPostBase.asset`: bloom 2.5 /
  threshold 0.9 / knee 0.5, LDR grade sat+10 con+10, vignette 0.25) · `Post Whiff` GO (prio 10,
  weight 0, `NeonPostWhiff.asset`: saturation −100) + `WhiffPostFx`.
- **Code:** `WhiffFx` (pure curve, 5 tests — keep verbatim) · `WhiffPostFx` (pulses
  `PostProcessVolume.weight` 1→0 on unscaled time) · `FeedbackSystem` (only knows
  `_whiffPostFx?.Pulse(_config.WhiffFlashSeconds)`; record-scratch SFX independent of pipeline).
- **URP 17.3.0 is already in the manifest** (installed, unassigned — Graphics/Quality pipeline
  slots are all None = BiRP today). `com.unity.feature.2d` present.
- **PPv2 footprint to remove:** `com.unity.postprocessing` 3.4.0 in manifest ·
  `Unity.Postprocessing.Runtime` ref in `BrainlessLabs.Neon.asmdef` · `NeonPostBase/NeonPostWhiff`
  assets · scene components above · auto-added `UNITY_POST_PROCESSING_STACK_V2` defines across
  platforms in ProjectSettings (verify they auto-remove with the package).
- **Swarm/ambient rendering:** custom `Neon/InstancedUnlit` shader + `Graphics.DrawMeshInstanced`
  (verify via `UnityStats.instancedBatches` — instanced draws are INVISIBLE in screenshots).
- **Settings quirks found 2026-07-05:** BiRP graphics-tier HDR is FALSE (starves bloom; becomes
  irrelevant — the URP asset owns HDR post-migration, turn it ON there) · Quality "Ultra" has
  2x MSAA (turn off — useless for 2D, conflicts with HDR targets).
- **Perf baselines @150 chaff (editor, same probe):** PPv2 stack on, live fight = **189 FPS** ·
  post off = **224 FPS** · M1 pre-post gate = ~197 FPS. Density-probe recipe (force cap 150:
  boxed `SwarmBridge._config` ChaffCap=150 / rate=40 / **null ChaffCapCurve**, bump
  `UnitDefinitionAsset._maxFollowers`, RESTORE after — asset mutations survive play-exit) is in
  the Plan B section of project memory.

## Proposed scope (planner refines)

**IN:**
1. URP pipeline asset + **Renderer2D** data; assign as Default Render Pipeline (leave per-Quality
   overrides None); **HDR on**, MSAA off.
2. Material/shader migration: sprites → `Sprite-Lit-Default` (so 2D lights *work* later);
   rewrite `Neon/InstancedUnlit` as a URP-compatible instanced unlit (keep the
   `DrawMeshInstanced` path — Entities Graphics adoption is **not** B.b); pink-material sweep of
   template/addon materials in shipping scenes.
3. Port post to the URP Volume system: global volume (Bloom / ColorAdjustments / Vignette at
   parity values) + whiff volume; `WhiffPostFx` re-typed to `UnityEngine.Rendering.Volume`
   (same weight-pulse design); `WhiffFx` + tests unchanged; `FeedbackSystem` unchanged; enable
   post-processing on the camera's `UniversalAdditionalCameraData`. Reuse the `PostProcessing`
   layer for URP volume masks.
4. Remove PPv2 completely (footprint list above).
5. Lighting **foundation only**: Renderer2D + one global 2D light at intensity 1 so the scene
   renders at identical brightness. NO light authoring, NO rain/reflections (Plan C/D/mood).
6. Gates: 161/161 EditMode · Recipe-4 play-verify (boot, HUD/finish-prompt crisp, whiff pulse
   end-to-end, no console errors) · perf gate @150 chaff vs the three baselines above ·
   `instancedBatches ≥ 1` · other scenes (menu etc.) still boot (smoke only).

**OUT:** 2D light authoring, rain / wet-ground reflections, Entities Graphics, PixelPerfect
anything (removed in Plan 0), balancing/feel changes.

## Execution constraints (hard-won this session)

- Must DI-bootstrap to play-test (Recipe 4, `neon-recipes`); never write assets/scripts mid-play
  (compile defers until play-exit and blocks scene MCP tools); screenshots via
  `ScreenCapture.CaptureScreenshot` to the scratchpad, never into `Assets/`.
- Scope test runs to assembly `BrainlessLabs.Neon.Tests.EditMode` (full-project runs fail on
  third-party DTT tests).
- Whiff verification: raise `UnitActions.onVerbWhiffed` DEFERRED (EditorApplication.update
  one-shot ≥0.5s after the bridge call) and track `Volume.weight` max — a bridge-call stall frame
  eats a 0.25s pulse invisibly.
- `aura-unity` MCP is bound to another project — use `mcp-for-unity` `execute_code` for probes.
- Idle player dies to wave-1 mobs in minutes (faster at forced density) — front-load play probes.
