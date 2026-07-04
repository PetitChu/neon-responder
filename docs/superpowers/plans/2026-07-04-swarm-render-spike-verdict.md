# Swarm render spike — verdict

- **Date:** 2026-07-04
- **Machine:** AMD Ryzen 9 9900X3D (12-core) / AMD Radeon RX 7900 XTX / 93 GB RAM / Windows 11 / Unity 6000.3.5f2
- **Population:** 150 hot proxies + 100 instanced ambient, all moved by SpikeMoveSystem every frame
- **Smoothed FPS @60s (editor):** 332–347 (two independent untouched 60s+ windows; ~430 fps raw frame rate when fully idle)
- **Lowest observed FPS:** 21–25 — but see Notes: exactly 4 frames out of 27,690 (~0.014%) dipped below 60 in the characterization window, never two in a row, and each dip coincides 1:1 with a gen-0 GC collection (the spike HUD's `OnGUI` allocates per frame — harness artifact, not the render path)
- **Batches / SetPass (Stats overlay):** 4 batches / 8 SetPass calls total; ambient = **1 instanced batch carrying all 100 instances** (`UnityStats.instancedBatches=1`, `instancedBatchedDrawCalls=100`)
- **HotProxyPool.LateUpdate cost (ms):** ~0.051 avg / 0.503 max (whole `LateBehaviourUpdate` phase, which in this scene is essentially only the proxy sync — the proxy-sync tax Plan 2 needs)
- **AmbientInstancedRenderer.Update cost (ms):** ~0.031 avg / 0.458 max (whole `BehaviourUpdate` phase = ambient matrix build + draw call + FPS HUD)
- **Standalone build FPS (only if editor was 45–60):** not needed — editor sits >5× above the bar

## Verdict (pick one)

- [x] **PASS** (≥60 FPS sustained): Plan 2 proceeds on the hybrid design as specced —
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

Two findings Plan 2 must inherit:

1. **The project runs the built-in render pipeline, not URP.** No SRP asset is assigned
   in Graphics or Quality settings (`m_CustomRenderPipeline: {fileID: 0}` everywhere);
   URP 17 is installed as a package but inactive. Consequences hit the spike directly:
   - `Graphics.RenderMeshInstanced` (RenderParams API) silently draws nothing —
     switched to `Graphics.DrawMeshInstanced` (classic built-in-RP API).
   - The plan's "URP Unlit" material renders nothing under built-in.
   - `Sprites/Default` **also fails silently under manual instancing**: its instancing
     path reads per-instance color/flip arrays that only the SpriteRenderer batcher
     populates, so `DrawMeshInstanced` output is fully transparent. The spike ships a
     30-line throwaway unlit instanced shader (`SpikeAmbientInstanced.shader`) instead.
   Plan 2's SwarmBridge ambient path must use `DrawMeshInstanced` + an
   instancing-safe shader (or revisit whether URP should actually be activated —
   a separate decision for Sebastien, not one the spike makes).

2. **The MCP editor screenshot pipeline does not capture immediate-mode draws**
   (`Graphics.Draw*` queues are missed by its camera re-render). Runtime verification of
   instanced drawing must use `UnityStats.instancedBatches` /
   `ScreenCapture.CaptureScreenshot` (back buffer), not the MCP screenshot tool.

Other observations:

- Burst compiled `SpikeMoveSystem` with no warnings; 250-agent sim cost is noise
  (`BehaviourUpdate`/`LateBehaviourUpdate` both < 0.06 ms avg — the ECS system runs in
  the player loop outside these markers and the total frame sits at ~2.3–3 ms).
- GC hitches trace to the spike harness (`OnGUI` GUIStyle/string allocs), 4 gen-0
  collections per minute. The real HUD (Plan 2) should not use `OnGUI`.
- Headroom: at 250 agents the frame budget is ~5× the 60 FPS bar in-editor; the
  spec's density targets have comfortable room before the proxy-sync loop needs the
  MARGINAL-path optimizations.
