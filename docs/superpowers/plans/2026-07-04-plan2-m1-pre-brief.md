# Plan 2 (M1 remainder) — pre-plan brief

Groundwork for writing Plan 2 (spec §7 M1 minus the completed spike). Everything below is
verified against real code or runtime; the **forks** section needs Sebastien's answers
before the plan is written.

## What the spike verdict locks in

(Full data: `2026-07-04-swarm-render-spike-verdict.md`.)

- **PASS** → Plan 2 proceeds on the specced hybrid: DOTS sim + pooled `SpriteRenderer`
  proxies for hot chaff + instanced ambient. Huge headroom (330+ FPS at 250 agents;
  proxy-sync tax ~0.05 ms).
- The ambient render path must be **`Graphics.DrawMeshInstanced` + an instancing-safe
  unlit shader** (pattern: `SpikeAmbientInstanced.shader`). The project runs the
  **built-in render pipeline** — `RenderMeshInstanced`, URP shaders, and `Sprites/Default`
  under manual instancing all fail silently.
- The real bridge needs **stable entity↔proxy mapping** (spike used index-order mapping,
  fine for perf only).
- Don't build the real HUD on `OnGUI` (GC hitches measured in the spike harness).

## Verified integration points (file:line, checked 2026-07-04)

| Seam | Where | Note |
|---|---|---|
| Verb hit resolution | `UnitActions.CheckForHit` (`Units/UnitActions.cs:87`) → `GetObjectsHit` (`:148`) → `HealthSystem.SubstractHealth` (`:108`) | The FinishResolver observation point the spec calls "confirmed at implementation". Additive hook only; verbs unchanged. |
| AI gate / spawn gap | `EnemyBehaviour.AI_Active` (`Units/EnemyBehaviour.cs:8`, gate at `:40`) | `SpawnerService.SpawnUnit` never sets it — spawned enemies stay inert unless the scene/prefab pre-set it. The M1 fix point is in `SpawnerService` after `InjectGameObject`. |
| Hero-tier registry | `EntitiesService` / `EntitiesQueries` | Stays Mono-only per spec §5.2; chaff never registers. `EntitiesQueries.DisableAllEnemyAI` (`Entities/EntitiesQueries.cs:50`) already flips `AI_Active` off on player death — the on-switch should mirror this. |
| Wave data | `LevelConfigurationAsset` | Gains a swarm-density block (data extension). |
| Clock tick order | `IGameplayClock` reserved bands | AutoEngage 0 → FinishReady 10 → Selector 20 → MomentumDecay 30 (already documented in `IGameplayClock.cs`). |

## ⚠ Blocker found (runtime-verified): scene scopes can't see the spine

`ScenesService` is constructed in **`GameServicesState`'s** scope, and its
`LifetimeScope.EnqueueParent(_lifetimeScope)` (`Scenes/ScenesService.cs:44`) parents every
loaded scene's scope **there**. The spine (`IStatSystem`, `IGameplaySignals`,
`IGameplayClock`) is registered one child deeper, in `GameplayServicesState` — so scene
scopes cannot resolve it.

Runtime probe (booted build, scene scope container): `IEntitiesService=OK`,
`IStatSystem=FAIL(VContainerException)`, `IGameplayClock=FAIL(VContainerException)`.

Everything M1 puts in the level scene (`SwarmBridge`, HUD, auto-engage on the player)
needs the spine. **Fork F1 below.**

## Forks needing Sebastien (answer before Plan 2 is written)

- **F1 — spine visibility fix.** (a) Move the `ScenesService` registration from
  `GameServicesState` to `GameplayServicesState` — one-line move; the deepest session
  scope becomes the scene parent, scenes see spine + everything above; `GameState`
  (child of GameplayServicesState) still resolves it. **Recommended.**
  (b) Register the spine in `GameServicesState` instead and drop the new state — undoes
  spec §4.3's separation. (c) Something else (e.g. an explicit scene-parent provider).
- **F2 — chaff damage model.** Spec §5.2 gives DOTS chaff their own `Health` component
  (sim owns truth; auto-engage chip + verb hits write via `EntityCommandBuffer`). Confirm:
  chaff do NOT use `HealthSystem`, and the single-verb chaff finish resolves entirely
  bridge-side (Mono `HealthSystem` path stays hero-tier only). This is the spec reading —
  flag if you want chaff on proxy-side `HealthSystem` instead.
- **F3 — M1 HUD scope.** Spec M1 says "minimal HUD (Momentum meter + Finish-Ready glow +
  verb prompt)". uGUI under the existing `UIManager` canvas (consumer of
  `IGameplaySignals` only), or defer meter styling and ship debug-bar minimal? Cost mainly
  in sign authoring.
- **F4 — auto-engage arc/targeting source.** `AutoEngageSystem` targets "nearest hot enemy
  in facing arc" spanning both worlds via `SwarmBridge.NearestFinishReadyInArc` /
  `HotCountInArc`. OK to make the bridge the ONLY spatial query layer (no physics overlap
  for chaff), per spec? Affects how verb hitboxes hit chaff (`QueryEntitiesInBox`).
- **F5 — branch base.** Plan 2 executes on top of `claude/neon-engine-base` (unmerged) or
  on a fresh branch off master after merging? (Branch-fate question still open.)

## Not in Plan 2 (already deferred by the plan series)

Economy/Protocols/progression (M2), run FSM/objective/Signal (M3), actives/feel (M4),
legacy menu-flow migration (separate small task; MainMenu itself was fixed 2026-07-04).
