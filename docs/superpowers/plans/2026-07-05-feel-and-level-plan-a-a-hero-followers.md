# Feel & Level — Plan A.a: Hero-Follower Crowd Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn Plan A's player-seeking crowd into **hero-anchored squads** — every chaff is a follower of a hero enemy up to that hero's cap; a dead hero orphans its followers (they seek the player) until a freed slot re-adopts them; heroes refill from orphans first, then fresh spawns, bounded by the density ceiling.

**Architecture:** Layers on Plan A (executed first). `SwarmBridge.Tick` pushes a **hero table** into the sim each tick (id = `TrackedEntity.Id`, position, cap) exactly as it already pushes `PlayerPosition` — so hero identity crosses the DOTS/Mono line as an int, and a hero leaving `IEntitiesService` auto-orphans its followers. A new `SwarmAssignmentSystem` re-adopts orphans into open slots (before spawn); `SwarmSpawnSystem` becomes hero-demand-driven (spawn only to fill remaining open slots, ≤ the `ChaffCap` ceiling); `SwarmSteeringSystem` targets each chaff's assigned hero (or the player when orphaned), reusing Plan A's `SwarmSteering` math unchanged. The only testable-in-isolation core is the assignment math (`HeroAssignment`), unit-tested like Plan A's helpers. `ISwarmBridge` interface + all consumers stay untouched.

**Tech Stack:** Unity 6.3.5, Unity Entities/DOTS, `Unity.Mathematics`, `Unity.Collections`, C#, Unity Test Framework (EditMode). Source spec: `docs/superpowers/specs/2026-07-05-feel-and-level-pass-design.md` §5a. Branch: `claude/feel-and-level-pass`.

**Prerequisite:** **Plan A is executed and merged/committed** (player-seeking crowd, lanes removed, `SwarmSteering`/`SwarmDensity` helpers exist, `BeltPosition.LaneIndex` gone). This plan modifies files Plan A produced.

**Scope boundary:** Delivers the follower machinery + a smoke test using the existing generic enemy (`UnitDefinition_NmeOne`) as a stand-in hero. **Plan C** authors the real roster caps (thug/elite/mini-boss `MaxFollowers`) and runs the full multi-hero verification.

---

## Ground truth (verified in code)

- `SwarmBridge.Tick()` (`Assets/_neon/Scripts/Swarm/SwarmBridge.cs`) already reads `_entities.GetFirstByType(UNITTYPE.PLAYER)` and writes `SwarmWorldState`. It has `IEntitiesService _entities`. `TryInitialize()` creates `_controlEntity` with `SwarmWorldState` + `SwarmDamageCommand` + `SwarmKillCommand` + `SwarmEventRecord`.
- `IEntitiesService` exposes `GetByType(UNITTYPE) → IReadOnlyList<TrackedEntity>`; `TrackedEntity` has `int Id`, `GameObject GameObject`, `UnitDefinitionAsset Definition` (confirmed via `Fakes.cs` `FakeEntitiesService`/`TrackedEntity`). `Id` is stable while the entity is registered; `Unregister` removes it (so a dead hero's `Id` vanishes from `GetByType`).
- `UnitDefinitionAsset` (`Assets/_neon/Scripts/Units/UnitDefinitionAsset.cs`): `_unitId`, `_displayName`, `_unitType`, `_prefab`, `_portrait`, `_maxHealth`. **No follower field yet.**
- After Plan A: `SwarmSpawnSystem` spawns chaff to a flat ceiling (hero-agnostic); chaff archetype = `(SwarmAgent, BeltPosition, SwarmVelocity, SwarmHealth, FinishReadyTag)`. `SwarmSteeringSystem` chaff branch seeks `world.PlayerPosition` via `SwarmSteering.ComputeDesiredVelocity`. `SwarmDensity.ResolveChaffCap(...)` writes `SwarmWorldState.ChaffCap` in the bridge.
- Chaff query = `WithAll<BeltPosition, SwarmHealth>()`.

## File structure (what changes)

- **Modify:** `Assets/_neon/Scripts/Units/UnitDefinitionAsset.cs` — add `MaxFollowers`.
- **Create:** `Assets/_neon/Scripts/Simulation/FollowerComponents.cs` — `HeroSlot` (buffer), `FollowerState` (component).
- **Create:** `Assets/_neon/Scripts/Simulation/HeroAssignment.cs` — pure static assignment math.
- **Create:** `Assets/_neon/Scripts/Simulation/SwarmAssignmentSystem.cs` — re-adopt orphans into open slots.
- **Modify:** `Assets/_neon/Scripts/Swarm/SwarmBridge.cs` — add `HeroSlot` to the control entity; push the hero table each tick.
- **Modify:** `Assets/_neon/Scripts/Simulation/SwarmSpawnSystem.cs` — chaff archetype gains `FollowerState`; spawn becomes hero-demand-driven.
- **Modify:** `Assets/_neon/Scripts/Simulation/SwarmSteeringSystem.cs` — chaff target = assigned hero (or player when orphaned).
- **Create (test):** `Assets/_neon/Tests/EditMode/Simulation/HeroAssignmentTests.cs`.

---

## Task 1: Add `MaxFollowers` to `UnitDefinitionAsset`

**Files:**
- Modify: `Assets/_neon/Scripts/Units/UnitDefinitionAsset.cs`

- [ ] **Step 1: Add the serialized field + accessor**

Add alongside the existing serialized fields (match the file's existing `_maxHealth`/accessor style):

```csharp
[Tooltip("Max chaff followers this enemy anchors as a squad (0 = commands no followers). Plan A.a.")]
[SerializeField] private int _maxFollowers = 0;
public int MaxFollowers => _maxFollowers;
```

(If the file exposes fields directly rather than via properties, match that convention instead.)

- [ ] **Step 2: Compile check**

`mcp__unityMCP__read_console`. Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/_neon/Scripts/Units/UnitDefinitionAsset.cs
git commit -m "feat: UnitDefinitionAsset.MaxFollowers — hero-squad cap (Plan A.a)"
```

---

## Task 2: Follower components + assignment math (TDD)

**Files:**
- Create: `Assets/_neon/Scripts/Simulation/FollowerComponents.cs`
- Create: `Assets/_neon/Scripts/Simulation/HeroAssignment.cs`
- Test: `Assets/_neon/Tests/EditMode/Simulation/HeroAssignmentTests.cs`

- [ ] **Step 1: Add the ECS data**

```csharp
using Unity.Entities;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>One hero enemy pushed into the sim by SwarmBridge each tick.</summary>
    public struct HeroSlot : IBufferElementData
    {
        public int Id;         // = TrackedEntity.Id (stable while the hero is alive/registered)
        public float2 Position;
        public int Cap;        // UnitDefinitionAsset.MaxFollowers
    }

    /// <summary>A chaff's squad assignment. HeroId = -1 means orphan (hero dead / unassigned).</summary>
    public struct FollowerState : IComponentData
    {
        public const int Orphan = -1;
        public int HeroId;
    }
}
```

- [ ] **Step 2: Write the failing test**

```csharp
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BrainlessLabs.Neon.Simulation;

namespace BrainlessLabs.Neon.Tests
{
    public class HeroAssignmentTests
    {
        static NativeArray<HeroSlot> Heroes(params (int id, float2 pos, int cap)[] hs)
        {
            var a = new NativeArray<HeroSlot>(hs.Length, Allocator.Temp);
            for (int i = 0; i < hs.Length; i++) a[i] = new HeroSlot { Id = hs[i].id, Position = hs[i].pos, Cap = hs[i].cap };
            return a;
        }

        [Test]
        public void PicksNearestHeroWithAnOpenSlot()
        {
            var heroes = Heroes((1, new float2(1, 0), 4), (2, new float2(5, 0), 4));
            var counts = new NativeArray<int>(new[] { 0, 0 }, Allocator.Temp);
            try { Assert.AreEqual(0, HeroAssignment.PickNearestOpenHero(new float2(0, 0), heroes, counts)); }
            finally { heroes.Dispose(); counts.Dispose(); }
        }

        [Test]
        public void SkipsFullHeroesEvenIfNearer()
        {
            var heroes = Heroes((1, new float2(1, 0), 4), (2, new float2(5, 0), 4));
            var counts = new NativeArray<int>(new[] { 4, 0 }, Allocator.Temp); // hero 1 full
            try { Assert.AreEqual(1, HeroAssignment.PickNearestOpenHero(new float2(0, 0), heroes, counts)); }
            finally { heroes.Dispose(); counts.Dispose(); }
        }

        [Test]
        public void ReturnsMinusOneWhenAllFull()
        {
            var heroes = Heroes((1, new float2(1, 0), 2));
            var counts = new NativeArray<int>(new[] { 2 }, Allocator.Temp);
            try { Assert.AreEqual(-1, HeroAssignment.PickNearestOpenHero(new float2(0, 0), heroes, counts)); }
            finally { heroes.Dispose(); counts.Dispose(); }
        }

        [Test]
        public void IndexOfHero_finds_and_misses()
        {
            var heroes = Heroes((7, float2.zero, 3), (9, new float2(2, 0), 3));
            try
            {
                Assert.AreEqual(1, HeroAssignment.IndexOfHero(9, heroes));
                Assert.AreEqual(-1, HeroAssignment.IndexOfHero(42, heroes));
            }
            finally { heroes.Dispose(); }
        }
    }
}
```

- [ ] **Step 3: Run to verify it fails**

Run EditMode. Expected: FAIL — `HeroAssignment` undefined.

- [ ] **Step 4: Implement the helper**

```csharp
using Unity.Collections;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>Pure, Burst-compatible squad assignment: nearest hero with a free slot, and id lookup.</summary>
    public static class HeroAssignment
    {
        public static int PickNearestOpenHero(float2 pos, in NativeArray<HeroSlot> heroes, in NativeArray<int> followerCounts)
        {
            int best = -1;
            float bestSq = float.MaxValue;
            for (int i = 0; i < heroes.Length; i++)
            {
                if (heroes[i].Cap - followerCounts[i] <= 0) continue;
                float d = math.lengthsq(heroes[i].Position - pos);
                if (d < bestSq) { bestSq = d; best = i; }
            }
            return best;
        }

        public static int IndexOfHero(int heroId, in NativeArray<HeroSlot> heroes)
        {
            for (int i = 0; i < heroes.Length; i++)
                if (heroes[i].Id == heroId) return i;
            return -1;
        }
    }
}
```

- [ ] **Step 5: Run to verify it passes**

Run EditMode. Expected: 4 `HeroAssignmentTests` PASS; suite green (Plan A's 151 + 4 = 155).

- [ ] **Step 6: Commit**

```bash
git add Assets/_neon/Scripts/Simulation/FollowerComponents.cs Assets/_neon/Scripts/Simulation/HeroAssignment.cs Assets/_neon/Tests/EditMode/Simulation/HeroAssignmentTests.cs
git commit -m "feat: HeroSlot/FollowerState + HeroAssignment pure math + tests (Plan A.a)"
```

---

## Task 3: Bridge pushes the hero table into the sim

**Files:**
- Modify: `Assets/_neon/Scripts/Swarm/SwarmBridge.cs`

- [ ] **Step 1: Add `HeroSlot` to the control-entity archetype**

In `TryInitialize`, extend the `CreateEntity` call:

```csharp
_controlEntity = entityManager.CreateEntity(
    typeof(SwarmWorldState), typeof(SwarmDamageCommand),
    typeof(SwarmKillCommand), typeof(SwarmEventRecord), typeof(HeroSlot));
```

- [ ] **Step 2: Write the hero table each tick**

In `Tick()`, after the player-position block and before/near the cap write, rebuild the hero buffer from the enemy roster:

```csharp
var heroes = entityManager.GetBuffer<HeroSlot>(_controlEntity);
heroes.Clear();
var enemies = _entities.GetByType(UNITTYPE.ENEMY);
for (int i = 0; i < enemies.Count; i++)
{
    var go = enemies[i].GameObject;
    if (go == null) continue;
    int cap = enemies[i].Definition != null ? enemies[i].Definition.MaxFollowers : 0;
    heroes.Add(new HeroSlot
    {
        Id = enemies[i].Id,
        Position = new float2(go.transform.position.x, go.transform.position.y),
        Cap = cap,
    });
}
```

(Add `using BrainlessLabs.Neon.Simulation;` if not already present — it is.)

- [ ] **Step 3: Compile check**

`mcp__unityMCP__read_console`. Expected: no errors. (Confirm `GetBuffer<HeroSlot>` re-fetch after the earlier `GetComponentData`/`SetComponentData` in `Tick` — buffers are fetched fresh, fine.)

- [ ] **Step 4: Play-verify the table populates**

Boot via Recipe 4, Level 01. Temporarily log `heroes.Length` or inspect via a quick probe — confirm it matches the number of live enemies as waves spawn/die. No errors.

- [ ] **Step 5: Commit**

```bash
git add Assets/_neon/Scripts/Swarm/SwarmBridge.cs
git commit -m "feat: SwarmBridge pushes live hero table (id/pos/cap) into the sim (Plan A.a)"
```

---

## Task 4: Chaff carry `FollowerState`; spawn becomes hero-demand-driven

**Files:**
- Modify: `Assets/_neon/Scripts/Simulation/SwarmSpawnSystem.cs`

- [ ] **Step 1: Add `FollowerState` to the chaff archetype**

In `OnCreate`:

```csharp
_chaffArchetype = state.EntityManager.CreateArchetype(
    typeof(SwarmAgent), typeof(BeltPosition), typeof(SwarmVelocity),
    typeof(SwarmHealth), typeof(FinishReadyTag), typeof(FollowerState));
```

- [ ] **Step 2: Replace the flood with hero-demand spawning**

Rewrite `FloodChaff` so chaff only spawn to fill open hero slots, assigned to that hero, bounded by the `ChaffCap` ceiling. Runs **after** `SwarmAssignmentSystem` (Task 5) has re-adopted orphans, so only genuinely-open slots remain:

```csharp
private void FloodChaff(ref SystemState state, in SwarmWorldState world)
{
    if (!SystemAPI.TryGetSingletonEntity<SwarmWorldState>(out var control)) return;
    var heroes = state.EntityManager.GetBuffer<HeroSlot>(control);
    if (heroes.Length == 0) return; // no heroes → no chaff (all chaff are followers)

    int total = _chaffQuery.CalculateEntityCount();
    if (total >= world.ChaffCap) { _spawnAccumulator = 0f; return; }

    // Follower count per hero (index-aligned with `heroes`).
    var counts = new Unity.Collections.NativeArray<int>(heroes.Length, Unity.Collections.Allocator.Temp);
    foreach (var follower in SystemAPI.Query<RefRO<FollowerState>>().WithAll<SwarmHealth>())
    {
        int idx = HeroAssignment.IndexOfHero(follower.ValueRO.HeroId, heroes.AsNativeArray());
        if (idx >= 0) counts[idx]++;
    }

    _spawnAccumulator += world.SpawnRatePerSecond * SystemAPI.Time.DeltaTime;
    while (_spawnAccumulator >= 1f && total < world.ChaffCap)
    {
        // pick any hero with an open slot (nearest-to-belt-end is fine; assignment tunes the rest)
        int heroIdx = -1;
        for (int i = 0; i < heroes.Length; i++)
            if (heroes[i].Cap - counts[i] > 0) { heroIdx = i; break; }
        if (heroIdx < 0) break; // no demand

        _spawnAccumulator -= 1f;
        total++; counts[heroIdx]++;

        float spawnX = _spawnFromLeft ? world.BeltMin.x : world.BeltMax.x;
        float spawnY = _random.NextFloat(world.BeltMin.y, world.BeltMax.y);
        _spawnFromLeft = !_spawnFromLeft;

        var entity = state.EntityManager.CreateEntity(_chaffArchetype);
        state.EntityManager.SetComponentData(entity, new SwarmAgent { Tier = SwarmTier.Chaff });
        state.EntityManager.SetComponentData(entity, new BeltPosition { Value = new float2(spawnX, spawnY) });
        state.EntityManager.SetComponentData(entity, new SwarmVelocity { Value = float2.zero });
        state.EntityManager.SetComponentData(entity, new SwarmHealth { Current = world.ChaffMaxHealth, Max = world.ChaffMaxHealth });
        state.EntityManager.SetComponentData(entity, new FollowerState { HeroId = heroes[heroIdx].Id });
        state.EntityManager.SetComponentEnabled<FinishReadyTag>(entity, false);
    }
    counts.Dispose();
    if (total >= world.ChaffCap) _spawnAccumulator = 0f;
}
```

(`_chaffQuery` already exists in the system. If `heroes.AsNativeArray()` inside the query loop trips the safety system, snapshot the buffer to a temp `NativeArray<HeroSlot>` before the loop and pass that.)

- [ ] **Step 3: Compile check**

`mcp__unityMCP__read_console`. Expected: no errors.

- [ ] **Step 4: (Verify after Task 6 — spawning is meaningless until steering + assignment are in.)**

- [ ] **Step 5: Commit**

```bash
git add Assets/_neon/Scripts/Simulation/SwarmSpawnSystem.cs
git commit -m "feat: hero-demand chaff spawning (fill open hero slots, ceiling-bounded) (Plan A.a)"
```

---

## Task 5: `SwarmAssignmentSystem` — re-adopt orphans into open slots

**Files:**
- Create: `Assets/_neon/Scripts/Simulation/SwarmAssignmentSystem.cs`

- [ ] **Step 1: Write the system (runs before spawn, so adoption beats fresh spawns)**

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>
    /// Re-adopts orphan chaff (HeroId not in the live hero table) into the nearest hero with a
    /// free slot. Runs before SwarmSpawnSystem so orphans fill slots before fresh chaff spawn
    /// ("adopt before spawn"). Also demotes followers whose hero has died to orphan (-1).
    /// </summary>
    [BurstCompile]
    [UpdateBefore(typeof(SwarmSpawnSystem))]
    public partial struct SwarmAssignmentSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<SwarmWorldState>();

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var world = SystemAPI.GetSingleton<SwarmWorldState>();
            if (world.Enabled == 0) return;
            if (!SystemAPI.TryGetSingletonEntity<SwarmWorldState>(out var control)) return;

            var heroesBuf = state.EntityManager.GetBuffer<HeroSlot>(control);
            using var heroes = heroesBuf.AsNativeArray().ToArray(Allocator.Temp); // stable snapshot
            using var counts = new NativeArray<int>(heroes.Length, Allocator.Temp);

            // 1) tally current valid followers; demote dead-hero followers to orphan.
            foreach (var (follower, _) in
                     SystemAPI.Query<RefRW<FollowerState>, RefRO<SwarmHealth>>().WithEntityAccess())
            {
                int idx = HeroAssignment.IndexOfHero(follower.ValueRO.HeroId, heroes);
                if (idx >= 0) counts[idx]++;
                else follower.ValueRW.HeroId = FollowerState.Orphan;
            }

            // 2) re-adopt orphans into nearest hero with a free slot.
            foreach (var (follower, position) in
                     SystemAPI.Query<RefRW<FollowerState>, RefRO<BeltPosition>>().WithAll<SwarmHealth>())
            {
                if (follower.ValueRO.HeroId != FollowerState.Orphan) continue;
                int idx = HeroAssignment.PickNearestOpenHero(position.ValueRO.Value, heroes, counts);
                if (idx < 0) continue; // still orphan → seeks player (steering)
                follower.ValueRW.HeroId = heroes[idx].Id;
                counts[idx]++;
            }
        }
    }
}
```

(Confirm `heroesBuf.AsNativeArray().ToArray(Allocator.Temp)` against the installed Entities version; the snapshot avoids aliasing the buffer while writing `FollowerState`. If `WithEntityAccess()` isn't needed, drop it.)

- [ ] **Step 2: Compile check**

`mcp__unityMCP__read_console`. Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/_neon/Scripts/Simulation/SwarmAssignmentSystem.cs
git commit -m "feat: SwarmAssignmentSystem — orphan re-adoption + dead-hero demotion (Plan A.a)"
```

---

## Task 6: Steering targets the assigned hero (or player when orphaned)

**Files:**
- Modify: `Assets/_neon/Scripts/Simulation/SwarmSteeringSystem.cs` (chaff branch from Plan A)

- [ ] **Step 1: Look up the hero table and target per-chaff**

In the chaff branch, replace the single `playerTarget` with a per-agent target. Snapshot the hero table once, then per chaff: if its `HeroId` is present → target that hero's position; else (orphan) → target the player. Reuse `SwarmSteering` unchanged.

```csharp
// after gathering `crowd` (Plan A) and before the chaff foreach:
var control = SystemAPI.GetSingletonEntity<SwarmWorldState>();
using var heroes = state.EntityManager.GetBuffer<HeroSlot>(control).AsNativeArray().ToArray(Unity.Collections.Allocator.Temp);
var playerPos = new float2(world.PlayerPosition.x, world.PlayerPosition.y);

foreach (var (position, velocity, follower) in
         SystemAPI.Query<RefRW<BeltPosition>, RefRW<SwarmVelocity>, RefRO<FollowerState>>()
             .WithAll<SwarmHealth>())
{
    int heroIdx = HeroAssignment.IndexOfHero(follower.ValueRO.HeroId, heroes);
    float2 target = heroIdx >= 0 ? heroes[heroIdx].Position : playerPos; // orphan → player

    float2 desired = SwarmSteering.ComputeDesiredVelocity(
        position.ValueRO.Value, target, crowd,
        world.ChaffMoveSpeed, SEP_RADIUS, SEP_WEIGHT, COH_RADIUS, COH_WEIGHT, STOP_RADIUS);

    var newVelocity = math.lerp(velocity.ValueRO.Value, desired, STEER_LERP);
    var newPosition = math.clamp(position.ValueRO.Value + newVelocity * deltaTime, world.BeltMin, world.BeltMax);
    velocity.ValueRW.Value = newVelocity;
    position.ValueRW.Value = newPosition;
}
```

- [ ] **Step 2: Compile check**

`mcp__unityMCP__read_console`. Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/_neon/Scripts/Simulation/SwarmSteeringSystem.cs
git commit -m "feat: chaff steer toward assigned hero, orphans toward player (Plan A.a)"
```

---

## Task 7: Smoke test with a stand-in hero + green sweep (acceptance)

**Files:**
- Temp-edit: `Assets/_neon/Units/UnitDefinition_NmeOne.asset` (or wherever it lives) — set `MaxFollowers` for the smoke test.

- [ ] **Step 1: Full EditMode suite**

Run EditMode. Expected: **green** — Plan A's 151 + 4 (`HeroAssignmentTests`) = 155, no regressions.

- [ ] **Step 2: Set a stand-in hero cap**

Set `UnitDefinition_NmeOne.MaxFollowers = 6` (temporary — Plan C authors real roster caps). Confirm Level 01's `SwarmDensityBlock` has swarm enabled + a `ChaffCap`/curve ceiling above the expected squad total.

- [ ] **Step 3: Acceptance play-test (Recipe 4 boot, Level 01)** — confirm all:
  - Chaff **group around the spawned enemies** (squads), not around the player, keeping separation.
  - Each enemy holds **≤ its cap** followers; the crowd count tracks `heroes × cap` (bounded by the ceiling), not a flat flood.
  - **Kill an enemy** → its followers become **orphans that come at the player**; when another enemy has a free slot, orphans **get re-adopted** and rejoin a squad.
  - A dead follower is **replaced** (orphan adopted first, else a fresh spawn) while the ceiling allows.
  - Finish-Ready tint + verb-finish still work (bridge/interface unchanged); no console errors; healthy frame rate at the cap.

- [ ] **Step 4: Revert the temp cap if it shouldn't ship**

Either leave `UnitDefinition_NmeOne.MaxFollowers = 6` as a sane default or reset to 0 — note the choice for Plan C. Commit the sweep.

```bash
git add -A
git commit -m "chore: Plan A.a green sweep — hero-follower squads + orphan/re-adopt verified"
```

---

## Self-Review

**1. Spec coverage (§5a):**
- "every chaff is a follower of a hero, per-hero cap (`MaxFollowers`)" → Tasks 1, 4. ✓
- "assignment = nearest hero with a free slot" → Task 2 (`PickNearestOpenHero`), used in Tasks 4–5. ✓
- "adopt orphan before spawning fresh" → Task 5 runs `[UpdateBefore(SwarmSpawnSystem)]`; spawn (Task 4) fills only post-adoption open slots. ✓
- "hero death orphans followers; orphans seek player until re-adopted" → Task 5 demotes dead-hero followers to `-1`; Task 6 targets player for orphans; Task 5 re-adopts. ✓
- "`ChaffCap` is the ceiling, hero-demand-driven population" → Task 4 (`total >= world.ChaffCap` guard; spawn only for open slots). ✓
- "bridge pushes hero table (id/pos/cap), interface untouched" → Task 3. ✓
- "reuse `SwarmSteering` unchanged, only target differs" → Task 6. ✓

**2. Placeholder scan:** No "TBD/handle later". Version-sensitive Entities spots (`TryGetSingletonEntity`, `GetBuffer` inside `Tick`, `AsNativeArray().ToArray`, `WithEntityAccess`, buffer snapshotting while writing `FollowerState`) each carry a concrete confirm/fallback note.

**3. Type consistency:** `HeroSlot{ Id, Position, Cap }` and `FollowerState{ HeroId, Orphan=-1 }` (Task 2) match every consumer (bridge Task 3, spawn Task 4, assignment Task 5, steering Task 6). `HeroAssignment.PickNearestOpenHero(float2, NativeArray<HeroSlot>, NativeArray<int>)` and `IndexOfHero(int, NativeArray<HeroSlot>)` match Task-2 test/impl and Task-4/5/6 callers. `SwarmSteering.ComputeDesiredVelocity(...)` reused with the same signature Plan A defined.

**Ordering guarantee:** `SwarmAssignmentSystem [UpdateBefore(SwarmSpawnSystem)]`; Plan A's `SwarmSteeringSystem [UpdateAfter(SwarmSpawnSystem)]`. So per frame: **assign/re-adopt → spawn-to-fill → steer**. Correct for "adopt before spawn" and fresh targets.

**Known non-unit-tested surface:** the three ECS systems + bridge are play-mode-verified (Recipe 4) — no ECS World test harness exists. `HeroAssignment` is the extracted testable core. Full multi-hero-tier verification lands in Plan C with the real roster.

**Dependency flag:** Plan A.a is meaningless without heroes on screen. The smoke test uses `UnitDefinition_NmeOne` as a stand-in; the *real* acceptance (thug/elite/mini-boss caps, mixed squads, mini-boss big squad) happens in Plan C.

---

## Execution Handoff

**Plan complete and saved to `docs/superpowers/plans/2026-07-05-feel-and-level-plan-a-a-hero-followers.md`.** Runs after Plan A. Two execution options:

1. **Subagent-Driven (recommended)** — fresh subagent per task, review between tasks. Tasks 3–7 need a live Unity Editor over the MCP for compile + play-test.
2. **Inline Execution** — run tasks here with checkpoints.

Which approach — or review the plan yourself first?
