# Neon Responder — Night Shift

**Neon Responder: Night Shift** is a **PC (Steam) 2D side-scroll beat-'em-up** — Unity 6,
single-player, **NOT VR** (ignore any Quest/XR framing that leaks in from the "Jungle XRKit"
Notion space this game lives under). It's a hard genre fusion: beat-'em-up feel + idle-survivor
swarm density + roguelite "Protocol" stack + a contextual **"Act" verb** combat layer. Design-target
core loop: *auto-engage → Finish-Ready → contextual Act verb → Momentum up → build fires harder /
earns faster → more Finish-Ready.*

## Design ground-truth vs implementation truth

- **GDD = design intent** (the authority for *what the game should be*):
  <https://app.notion.com/p/Neon_Responder_GDD-3935c0654b6b80beaa94f61de4bbf47d>
- **The `neon-*` skills = implementation truth** (the authority for *how the code works today*).
  Skills reference the GDD; they don't duplicate it.
- **Much of the GDD is not built yet.** Always check the status tag before assuming a system exists.

## Status tags (used across the skills)

- `[BUILT]` — in code on `master` now.
- `[IN-MIGRATION → DOTS]` — exists as plain C#, deliberately DOTS-shaped for a parallel ECS refactor
  (e.g. `EntitiesService`, `SpawnerService`). It runs today; its internals are converging on ECS.
- `[GDD-TARGET · not built]` — design intent only (auto-engage, Finish-Ready, Momentum, Protocols,
  the Signal). Do **not** wire against these or describe them as existing.

## Non-negotiable rules

1. **Must DI-bootstrap to run.** Pressing Play directly in a level scene breaks DI — `Level.Start`
   can't resolve services (no parent app scope). Boot through the ApplicationFSM: enable editor
   bootstrap in `BootstrapSettingsAsset`. → `neon-troubleshooting`, `neon-recipes` (Recipe 4).
2. **Resolve via VContainer DI only.** Constructor injection (non-MonoBehaviour) or `[Inject]` fields
   (scene/spawned MonoBehaviours). **There is no static `Services` locator** — `Services.cs` was
   deleted; never reintroduce singletons / `Instance` accessors.
3. **Register services in an FSM state** — `GameServicesState.RegisterTypes` for gameplay services,
   platform services in their own states — **not** in `ApplicationLifetimeScope`.
4. **Combat design stays engine-agnostic.** Combat runs as a MonoBehaviour FSM today (custom
   `UnitStateMachine`); a DOTS/ECS *combat* track is being developed in parallel. Describe combat as
   player-intent verbs, never MonoBehaviour-vs-ECS. → `neon-combat`.
5. **No legacy.** Don't use `WaveManager` (`[Obsolete]`) or rebuild `04_Level2` / `05_Level3`. New
   levels use `Level` + `LevelConfigurationAsset` + `SpawnerService`; only `03_Level1` is migrated.
6. **Single-player.** No networking / multiplayer assumptions.
7. **Runtime is ground truth.** Verify spawn / wave / AI / combat behavior by play-testing
   (`neon-recipes` Recipe 4), not by reading code. Front-load a concrete run before claiming a fix.
8. **Never invent APIs.** Confirm a type / member / field exists in code before calling it.

## Assemblies & namespaces

| asmdef / namespace | Scope |
|---|---|
| `BrainlessLabs.Neon` | Runtime core: services, gameplay, settings, entities, combat |
| `BrainlessLabs.Neon.Lifecycle` | App boot / FSM (UnityHFSM) + boot services (Steam, Unity Services, health) |
| `BrainlessLabs.Neon.Editor` | Editor-only: bootstrap, drawers, custom inspectors, project settings |

Namespaces match the asmdef `rootNamespace` — folder depth does **not** add sub-namespaces.
Reference direction: **Lifecycle → Neon**, **Editor → Neon**. Runtime never references Editor;
`Neon` never references `Lifecycle`. All game code lives under `Assets/_neon/`.

## Skills — where to look

Project skills live in `.claude/skills/neon-*/`. Read the relevant one before working:

| Skill | Read when |
|---|---|
| `neon-architecture` | Designing a feature, understanding boot / DI / scopes / service roster |
| `neon-conventions` | Writing code or reviewing for consistency (folders, asmdefs, naming, service + settings patterns) |
| `neon-combat` | Working on combat, unit states, or the "Act" verb design (engine-agnostic; MonoBehaviour now, DOTS target) |
| `neon-game-systems` | Working on gameplay systems, or checking what exists vs planned (Entities/Spawner/Level/Health/UI vs GDD targets) |
| `neon-recipes` | Implementing something — add a service / unit state / level / settings asset, or play-test a level |
| `neon-troubleshooting` | Debugging — DI-bootstrap failures, the `AI_Active` spawn gap, scene-migration status, editor-bootstrap flakiness |
