# Feel & Level — Plan A: Swarm Rework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the 3-lane chaff spawn/hold with a real crowd (seek + separation + light cohesion), move ambient agents onto authored walkway paths, and ramp density by level progression — so the swarm reads as a mob to wade through, not marching columns.

**Architecture:** All changes are Layer-1 internal. The `ISwarmBridge` interface and every consumer above it (AutoEngage, engagement systems, render rig) stay untouched. Steering, path-sampling, and density-cap math are extracted as **pure Burst-compatible static functions** (unit-tested with plain arrays, no ECS World needed — the suite has no ECS test harness); the `[BurstCompile] ISystem`s and the runtime bake call those functions and are verified in play mode. The per-zone density ramp is a small addition to `SwarmBridge.Tick`'s existing cap computation (it already writes the cap each tick and already reads the player position).

**Tech Stack:** Unity 6.3.5, Unity Entities/DOTS (`Unity.Entities`, `Unity.Mathematics`, `Unity.Collections`, `[BurstCompile]`), C#, Unity Test Framework (EditMode). Source spec: `docs/superpowers/specs/2026-07-05-feel-and-level-pass-design.md` §5. Branch: `claude/feel-and-level-pass` (Plan 0 already merged).

**Scope boundary:** This plan delivers the crowd behavior, the walkway-path *capability* + a smoke-test path, and the density-curve *mechanism*. It does **not** author Level 01's real walkway paths or final density values — that's Plan C. Chaff render (`SwarmRenderRig`) sprite fields are already `[SerializeField]`-exposed; real art is Plan D.

---

## Ground truth (verified in code)

- `SwarmSpawnSystem` (`Assets/_neon/Scripts/Simulation/SwarmSpawnSystem.cs`): `LANE_COUNT = 3`; `FloodChaff` picks `lane = random(0,3)`, `laneY = lerp(BeltMin.y, BeltMax.y, (lane+0.5)/3)`, spawns at alternating belt ends, sets `BeltPosition{ Value, LaneIndex }`. `SeedAmbient` scatters `AmbientCap` agents at random belt positions once, gives them a random wander velocity.
- `SwarmSteeringSystem` (`.../SwarmSteeringSystem.cs`): `[BurstCompile] ISystem`. Chaff branch seeks `(PlayerPosition.x, laneY)` where `laneY` comes from `LaneIndex`; adds `(entity.Index % 7 - 3) * 0.15f` jitter; `STOP_RADIUS = 0.9f`, `STEER_LERP = 0.08f`; clamps to `BeltMin/BeltMax`. Ambient branch bounce-wanders off belt bounds.
- `SwarmComponents.cs`: `BeltPosition{ float2 Value; int LaneIndex; }`, `SwarmVelocity{ float2 Value }`, `SwarmHealth{ int Current, Max }`, `FinishReadyTag : IEnableableComponent`, `SwarmWorldState` singleton (fields incl. `PlayerPosition`, `ChaffCap`, `AmbientCap`, `SpawnRatePerSecond`, `ChaffMoveSpeed`, `BeltMin`, `BeltMax`, `Enabled`), plus damage/kill/event buffers. `SwarmAgent{ SwarmTier Tier }`, `SwarmTier{ Chaff, Ambient }` in `SwarmAgent.cs`.
- `SwarmBridge.cs` (`Assets/_neon/Scripts/Swarm/SwarmBridge.cs`): `Tick()` each gameplay tick reads player pos and sets `state.ChaffCap = min(150, round(_config.ChaffCap * nastiness))`, `state.SpawnRatePerSecond = _config.SpawnRatePerSecond * nastiness`. Creates the control entity in `TryInitialize()`. **The interface `ISwarmBridge` is not modified by this plan.**
- `SwarmConfig.cs`: `readonly struct` built by `SwarmConfig.From(LevelConfigurationAsset, Level)`; `BeltMin = (level.LevelStartX, block.BeltYMin)`, `BeltMax = (level.LevelStartX + level.LevelLength, block.BeltYMax)`.
- `SwarmDensityBlock` (in `LevelConfigurationAsset.cs`): `EnableSwarm`, `ChaffCap`, `AmbientCap`, `ChaffSpawnRatePerSecond`, `ChaffMoveSpeed`, `BeltYMin`, `BeltYMax`.
- `SwarmRenderRig.cs`: chaff = pooled `SpriteRenderer` proxies keyed by entity (reads `BeltPosition.Value`, not `LaneIndex`); ambient = `Graphics.DrawMeshInstanced` from `BeltPosition.Value`. Sprite/material fields already `[SerializeField]`. **Reads only `BeltPosition.Value` — safe across the `LaneIndex` removal.**
- Tests: `Assets/_neon/Tests/EditMode/*`, `namespace BrainlessLabs.Neon.Tests`, plain NUnit, no ECS World. `Fakes.cs` has `FakeSwarmBridge` (interface unchanged ⇒ fakes stay valid).

## File structure (what changes)

- **Create:** `Assets/_neon/Scripts/Simulation/SwarmSteering.cs` — pure static crowd-steering math.
- **Create:** `Assets/_neon/Scripts/Simulation/WalkwayPathSampler.cs` — pure static polyline sampler.
- **Create:** `Assets/_neon/Scripts/Simulation/SwarmDensity.cs` — pure static progression→cap resolver.
- **Create:** `Assets/_neon/Scripts/Simulation/WalkwayComponents.cs` — `WalkwayPoint`, `WalkwayPathRange` (buffers), `AmbientPathState`, `WalkwayPathsTag`.
- **Create:** `Assets/_neon/Scripts/Swarm/WalkwayPathAuthoring.cs` — scene MonoBehaviour: gizmo-editable paths, runtime bake to the singleton.
- **Modify:** `SwarmComponents.cs` — drop `BeltPosition.LaneIndex`.
- **Modify:** `SwarmSpawnSystem.cs` — remove lanes; random-Y chaff spawn; add `AmbientPathState` to ambient + assign a path.
- **Modify:** `SwarmSteeringSystem.cs` — chaff = seek+separation+cohesion via `SwarmSteering`; ambient = path-follow via `WalkwayPathSampler`.
- **Modify:** `LevelConfigurationAsset.cs` (`SwarmDensityBlock`) — add `ChaffCapCurve`, `AmbientCapCurve` (`AnimationCurve`).
- **Modify:** `SwarmConfig.cs` — carry the curves.
- **Modify:** `SwarmBridge.cs` — sample progression (player-X vs belt) → curve → cap, in the existing `Tick` cap math.
- **Create (tests):** `SwarmSteeringTests.cs`, `WalkwayPathSamplerTests.cs`, `SwarmDensityTests.cs` under `Assets/_neon/Tests/EditMode/Simulation/`.
- **Modify:** the EditMode test asmdef (`Assets/_neon/Tests/EditMode/`) — add `Unity.Mathematics` + `Unity.Collections` references.

---

## Task 1: Crowd-steering math (TDD)

**Files:**
- Modify: the EditMode test asmdef in `Assets/_neon/Tests/EditMode/` — add `"Unity.Mathematics"` and `"Unity.Collections"` to `references`.
- Create: `Assets/_neon/Scripts/Simulation/SwarmSteering.cs`
- Test: `Assets/_neon/Tests/EditMode/Simulation/SwarmSteeringTests.cs`

- [ ] **Step 1: Add math/collections refs to the test asmdef, then write the failing test**

```csharp
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BrainlessLabs.Neon.Simulation;

namespace BrainlessLabs.Neon.Tests
{
    public class SwarmSteeringTests
    {
        // radius/weight knobs mirror the system consts (Task 2).
        const float MoveSpeed = 2f, SepR = 0.6f, SepW = 1f, CohR = 1.5f, CohW = 0.15f, StopR = 0.9f;

        static float2 Compute(float2 self, float2 target, float2[] crowd)
        {
            var arr = new NativeArray<float2>(crowd, Allocator.Temp);
            try { return SwarmSteering.ComputeDesiredVelocity(self, target, arr, MoveSpeed, SepR, SepW, CohR, CohW, StopR); }
            finally { arr.Dispose(); }
        }

        [Test]
        public void LoneAgent_far_from_target_moves_toward_it_at_move_speed()
        {
            var v = Compute(new float2(0, 0), new float2(10, 0), new[] { new float2(0, 0) });
            Assert.Greater(v.x, 0f);
            Assert.AreEqual(MoveSpeed, math.length(v), 0.01f);
        }

        [Test]
        public void LoneAgent_within_stop_radius_holds()
        {
            var v = Compute(new float2(0, 0), new float2(0.5f, 0), new[] { new float2(0, 0) });
            Assert.Less(math.length(v), 0.01f);
        }

        [Test]
        public void Close_neighbor_pushes_agent_away()
        {
            // neighbor to the right, target straight up → separation must add a leftward push
            var v = Compute(new float2(0, 0), new float2(0, 10), new[] { new float2(0, 0), new float2(0.2f, 0) });
            Assert.Less(v.x, 0f);
        }

        [Test]
        public void Cohesion_pulls_toward_a_cluster_outside_separation_range()
        {
            // neighbors at ~1.2 (outside SepR 0.6, inside CohR 1.5), target within stop radius → only cohesion acts
            var v = Compute(new float2(0, 0), new float2(0.1f, 0),
                new[] { new float2(0, 0), new float2(1.2f, 0), new float2(1.2f, 0.2f) });
            Assert.Greater(v.x, 0f);
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run EditMode (`mcp__unityMCP__run_tests` mode EditMode, or Test Runner). Expected: FAIL — `SwarmSteering` undefined.

- [ ] **Step 3: Implement the pure helper**

```csharp
using Unity.Collections;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>
    /// Pure, Burst-compatible crowd steering: seek target + separation + light cohesion.
    /// No alignment (alignment reintroduces the marching-column read). Scans the crowd
    /// array O(n) per call; the caller skips the agent itself via distance epsilon.
    /// </summary>
    public static class SwarmSteering
    {
        public static float2 ComputeDesiredVelocity(
            float2 self, float2 target, in NativeArray<float2> crowd,
            float moveSpeed, float separationRadius, float separationWeight,
            float cohesionRadius, float cohesionWeight, float stopRadius)
        {
            float2 separation = float2.zero;
            float2 cohesionCenter = float2.zero;
            int cohesionCount = 0;
            float sepSq = separationRadius * separationRadius;
            float cohSq = cohesionRadius * cohesionRadius;

            for (int i = 0; i < crowd.Length; i++)
            {
                float2 offset = self - crowd[i];
                float distSq = math.lengthsq(offset);
                if (distSq < 1e-6f) continue; // the agent itself / coincident
                if (distSq < sepSq)
                    separation += math.normalizesafe(offset) * (1f - math.sqrt(distSq) / separationRadius);
                if (distSq < cohSq) { cohesionCenter += crowd[i]; cohesionCount++; }
            }

            float2 toTarget = target - self;
            float2 seek = math.length(toTarget) > stopRadius ? math.normalizesafe(toTarget) : float2.zero;

            float2 cohesion = cohesionCount > 0
                ? math.normalizesafe(cohesionCenter / cohesionCount - self)
                : float2.zero;

            float2 steer = seek + separation * separationWeight + cohesion * cohesionWeight;
            float len = math.length(steer);
            if (len < 1e-5f) return float2.zero;
            return steer / len * moveSpeed * math.min(1f, len);
        }
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run EditMode. Expected: 4 `SwarmSteeringTests` PASS; full suite green (141 baseline + 4).

- [ ] **Step 5: Commit**

```bash
git add Assets/_neon/Scripts/Simulation/SwarmSteering.cs Assets/_neon/Tests/EditMode/Simulation/SwarmSteeringTests.cs <test-asmdef>
git commit -m "feat: SwarmSteering pure crowd math (seek+separation+cohesion) + tests (Plan A)"
```

---

## Task 2: Rewire SwarmSteeringSystem chaff branch to the crowd math

**Files:**
- Modify: `Assets/_neon/Scripts/Simulation/SwarmSteeringSystem.cs` (chaff branch, ~lines 31–55)

- [ ] **Step 1: Replace the chaff loop**

Add tuning consts and gather all chaff positions once, then steer via the helper. Replace the chaff `foreach` block with:

```csharp
// --- tuning (in-editor tunable; mirror SwarmSteeringTests) ---
const float SEP_RADIUS = 0.6f, SEP_WEIGHT = 1f, COH_RADIUS = 1.5f, COH_WEIGHT = 0.15f;

// Gather the crowd (chaff = has SwarmHealth) once for O(n) neighbor scans per agent.
var crowdQuery = SystemAPI.QueryBuilder()
    .WithAll<BeltPosition, SwarmHealth>().Build();
using var crowdPos = crowdQuery.ToComponentDataArray<BeltPosition>(Unity.Collections.Allocator.Temp);
using var crowd = new Unity.Collections.NativeArray<float2>(crowdPos.Length, Unity.Collections.Allocator.Temp);
for (int i = 0; i < crowdPos.Length; i++) crowd[i] = crowdPos[i].Value;

var playerTarget = new float2(world.PlayerPosition.x, world.PlayerPosition.y);

foreach (var (position, velocity) in
         SystemAPI.Query<RefRW<BeltPosition>, RefRW<SwarmVelocity>>()
             .WithAll<SwarmHealth>())
{
    float2 desired = SwarmSteering.ComputeDesiredVelocity(
        position.ValueRO.Value, playerTarget, crowd,
        world.ChaffMoveSpeed, SEP_RADIUS, SEP_WEIGHT, COH_RADIUS, COH_WEIGHT, STOP_RADIUS);

    var newVelocity = math.lerp(velocity.ValueRO.Value, desired, STEER_LERP);
    var newPosition = math.clamp(position.ValueRO.Value + newVelocity * deltaTime, world.BeltMin, world.BeltMax);
    velocity.ValueRW.Value = newVelocity;
    position.ValueRW.Value = newPosition;
}
```

Delete the old `laneY`/`LaneIndex`/`jitter` lines. Remove the now-unused `.WithEntityAccess()` from this query. Keep `STOP_RADIUS`/`STEER_LERP` consts.

- [ ] **Step 2: Compile check**

`mcp__unityMCP__read_console`. Expected: no errors. (If the installed Entities version rejects `SystemAPI.QueryBuilder().Build()` inside `OnUpdate` under Burst, gather via a cached `EntityQuery` created in `OnCreate` instead — confirm against the version.)

- [ ] **Step 3: Play-verify**

Boot via Recipe 4, load Level 01 (swarm enabled). Confirm in Game view: chaff **no longer spawn/hold in 3 horizontal lines**; they converge on the player as a crowd and **keep spacing** (no stacking on one point). No console errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/_neon/Scripts/Simulation/SwarmSteeringSystem.cs
git commit -m "feat: chaff crowd steering (seek+separation+cohesion), lanes removed from steering (Plan A)"
```

---

## Task 3: Remove lanes from SwarmSpawnSystem

**Files:**
- Modify: `Assets/_neon/Scripts/Simulation/SwarmSpawnSystem.cs`

- [ ] **Step 1: Remove `LANE_COUNT` and lane-Y; spawn chaff at random depth**

In `FloodChaff`, replace the lane block with a random Y across the belt band:

```csharp
float spawnX = _spawnFromLeft ? world.BeltMin.x : world.BeltMax.x;
float spawnY = _random.NextFloat(world.BeltMin.y, world.BeltMax.y);
_spawnFromLeft = !_spawnFromLeft;

var entity = state.EntityManager.CreateEntity(_chaffArchetype);
state.EntityManager.SetComponentData(entity, new SwarmAgent { Tier = SwarmTier.Chaff });
state.EntityManager.SetComponentData(entity, new BeltPosition { Value = new float2(spawnX, spawnY) });
state.EntityManager.SetComponentData(entity, new SwarmVelocity { Value = float2.zero });
state.EntityManager.SetComponentData(entity, new SwarmHealth { Current = world.ChaffMaxHealth, Max = world.ChaffMaxHealth });
state.EntityManager.SetComponentEnabled<FinishReadyTag>(entity, false);
```

Delete the `private const int LANE_COUNT = 3;` line and the `int lane = ...; float laneY = ...;` lines. (`BeltPosition` no longer sets `LaneIndex` — removed in Task 4.)

- [ ] **Step 2: Compile check**

`mcp__unityMCP__read_console`. Expected: no errors (LaneIndex still exists on the struct until Task 4; omitting it from the initializer is fine).

- [ ] **Step 3: Play-verify**

Boot + Level 01. Chaff enter from both ends at varied depths (not 3 rows). No errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/_neon/Scripts/Simulation/SwarmSpawnSystem.cs
git commit -m "feat: chaff spawn at random belt depth, lanes removed from spawn (Plan A)"
```

---

## Task 4: Drop the vestigial `BeltPosition.LaneIndex`

**Files:**
- Modify: `Assets/_neon/Scripts/Simulation/SwarmComponents.cs`

- [ ] **Step 1: Remove the field**

```csharp
public struct BeltPosition : IComponentData
{
    public float2 Value;
}
```

- [ ] **Step 2: Confirm no remaining readers/writers**

Run: `rg -n "LaneIndex" Assets/_neon/Scripts`
Expected: **no matches** (spawn + steering both cleaned in Tasks 2–3; render rig and bridge only read `.Value`).

- [ ] **Step 3: Compile + smoke**

`mcp__unityMCP__read_console` — no errors. Boot + Level 01 — swarm still renders/moves.

- [ ] **Step 4: Commit**

```bash
git add Assets/_neon/Scripts/Simulation/SwarmComponents.cs
git commit -m "refactor: drop vestigial BeltPosition.LaneIndex (Plan A)"
```

---

## Task 5: Walkway polyline sampler (TDD)

**Files:**
- Create: `Assets/_neon/Scripts/Simulation/WalkwayPathSampler.cs`
- Test: `Assets/_neon/Tests/EditMode/Simulation/WalkwayPathSamplerTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BrainlessLabs.Neon.Simulation;

namespace BrainlessLabs.Neon.Tests
{
    public class WalkwayPathSamplerTests
    {
        static (float2 pos, float2 dir) Sample(float2[] pts, float dist)
        {
            var arr = new NativeArray<float2>(pts, Allocator.Temp);
            try { WalkwayPathSampler.Sample(arr, 0, arr.Length, dist, out var p, out var d); return (p, d); }
            finally { arr.Dispose(); }
        }

        [Test]
        public void Midway_along_a_straight_segment()
        {
            var (p, d) = Sample(new[] { new float2(0, 0), new float2(10, 0) }, 3f);
            Assert.AreEqual(3f, p.x, 0.001f); Assert.AreEqual(0f, p.y, 0.001f);
            Assert.AreEqual(1f, d.x, 0.001f);
        }

        [Test]
        public void Distance_past_the_end_wraps_around()
        {
            var (p, _) = Sample(new[] { new float2(0, 0), new float2(10, 0) }, 13f); // total 10 → 3
            Assert.AreEqual(3f, p.x, 0.001f);
        }

        [Test]
        public void Crosses_into_the_second_segment_of_an_L()
        {
            var (p, d) = Sample(new[] { new float2(0, 0), new float2(10, 0), new float2(10, 10) }, 15f);
            Assert.AreEqual(10f, p.x, 0.001f); Assert.AreEqual(5f, p.y, 0.001f);
            Assert.AreEqual(1f, d.y, 0.001f);
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run EditMode. Expected: FAIL — `WalkwayPathSampler` undefined.

- [ ] **Step 3: Implement the sampler**

```csharp
using Unity.Collections;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>
    /// Pure, Burst-compatible looping polyline sampler. Points live in a shared buffer;
    /// (start,count) selects one path. Distance wraps around the path's total length.
    /// </summary>
    public static class WalkwayPathSampler
    {
        public static void Sample(in NativeArray<float2> points, int start, int count,
            float distance, out float2 position, out float2 direction)
        {
            if (count < 2)
            {
                position = count == 1 ? points[start] : float2.zero;
                direction = new float2(1f, 0f);
                return;
            }

            float total = 0f;
            for (int i = 0; i < count; i++)
                total += math.distance(points[start + i], points[start + (i + 1) % count]);

            float d = total > 1e-5f ? math.fmod(math.fmod(distance, total) + total, total) : 0f;

            for (int i = 0; i < count; i++)
            {
                float2 a = points[start + i];
                float2 b = points[start + (i + 1) % count];
                float seg = math.distance(a, b);
                if (d <= seg || i == count - 1)
                {
                    float t = seg > 1e-5f ? d / seg : 0f;
                    position = math.lerp(a, b, t);
                    direction = math.normalizesafe(b - a, new float2(1f, 0f));
                    return;
                }
                d -= seg;
            }
            position = points[start]; direction = new float2(1f, 0f);
        }
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run EditMode. Expected: 3 `WalkwayPathSamplerTests` PASS; suite green.

- [ ] **Step 5: Commit**

```bash
git add Assets/_neon/Scripts/Simulation/WalkwayPathSampler.cs Assets/_neon/Tests/EditMode/Simulation/WalkwayPathSamplerTests.cs
git commit -m "feat: WalkwayPathSampler looping polyline math + tests (Plan A)"
```

---

## Task 6: Walkway components + scene authoring & runtime bake

**Files:**
- Create: `Assets/_neon/Scripts/Simulation/WalkwayComponents.cs`
- Create: `Assets/_neon/Scripts/Swarm/WalkwayPathAuthoring.cs`

- [ ] **Step 1: Add the ECS data**

```csharp
using Unity.Entities;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>Singleton marker holding the baked walkway geometry buffers.</summary>
    public struct WalkwayPathsTag : IComponentData { }

    /// <summary>Flat list of all path waypoints; ranges select individual paths.</summary>
    public struct WalkwayPoint : IBufferElementData { public float2 Value; }

    /// <summary>One (start,count) window into the WalkwayPoint buffer per authored path.</summary>
    public struct WalkwayPathRange : IBufferElementData { public int Start; public int Count; }

    /// <summary>Per-ambient-agent path assignment + progress.</summary>
    public struct AmbientPathState : IComponentData
    {
        public int PathId;      // index into the WalkwayPathRange buffer; -1 = no path
        public float Distance;  // arc length along the path
        public float Speed;
        public float Lateral;   // signed perpendicular offset so agents don't overlap the centerline
    }
}
```

- [ ] **Step 2: Add the authoring MonoBehaviour**

```csharp
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using BrainlessLabs.Neon.Simulation;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Scene authoring for ambient walkway paths. Each element of `Paths` is a parent
    /// Transform whose ORDERED CHILDREN are the waypoints. Draw gizmos for editing; at
    /// runtime, bake all paths into a single WalkwayPathsTag singleton the sim reads.
    /// Plan A ships the capability + a smoke path; Plan C authors Level 01's real paths.
    /// </summary>
    public class WalkwayPathAuthoring : MonoBehaviour
    {
        [Tooltip("Each entry = one path; its child transforms (in order) are the waypoints.")]
        public Transform[] Paths;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || Paths == null || Paths.Length == 0) return;

            var em = world.EntityManager;
            var entity = em.CreateEntity(typeof(WalkwayPathsTag));
            var pointBuf = em.AddBuffer<WalkwayPoint>(entity);
            var rangeBuf = em.AddBuffer<WalkwayPathRange>(entity);

            foreach (var root in Paths)
            {
                if (root == null || root.childCount < 2) continue;
                int start = pointBuf.Length;
                for (int i = 0; i < root.childCount; i++)
                {
                    var p = root.GetChild(i).position;
                    pointBuf.Add(new WalkwayPoint { Value = new float2(p.x, p.y) });
                }
                rangeBuf.Add(new WalkwayPathRange { Start = start, Count = root.childCount });
            }
        }

        private void OnDrawGizmos()
        {
            if (Paths == null) return;
            Gizmos.color = Color.cyan;
            foreach (var root in Paths)
            {
                if (root == null || root.childCount < 2) continue;
                for (int i = 0; i < root.childCount; i++)
                {
                    var a = root.GetChild(i).position;
                    var b = root.GetChild((i + 1) % root.childCount).position;
                    Gizmos.DrawSphere(a, 0.12f);
                    Gizmos.DrawLine(a, b);
                }
            }
        }
    }
}
```

- [ ] **Step 3: Compile check**

`mcp__unityMCP__read_console`. Expected: no errors. (Confirm `AddBuffer`/`IBufferElementData` usage against the installed Entities version.)

- [ ] **Step 4: Commit**

```bash
git add Assets/_neon/Scripts/Simulation/WalkwayComponents.cs Assets/_neon/Scripts/Swarm/WalkwayPathAuthoring.cs
git commit -m "feat: walkway path components + scene authoring/bake (Plan A)"
```

---

## Task 7: Ambient agents follow walkway paths

**Files:**
- Modify: `Assets/_neon/Scripts/Simulation/SwarmSpawnSystem.cs` (ambient archetype + `SeedAmbient`)
- Modify: `Assets/_neon/Scripts/Simulation/SwarmSteeringSystem.cs` (ambient branch)

- [ ] **Step 1: Add `AmbientPathState` to the ambient archetype and assign a path on seed**

In `OnCreate`, add `AmbientPathState` to `_ambientArchetype`:

```csharp
_ambientArchetype = state.EntityManager.CreateArchetype(
    typeof(SwarmAgent), typeof(BeltPosition), typeof(SwarmVelocity), typeof(AmbientPathState));
```

In `SeedAmbient`, look up how many paths exist and assign one round-robin (fallback `PathId = -1` if none):

```csharp
int pathCount = 0;
if (SystemAPI.TryGetSingletonEntity<WalkwayPathsTag>(out var pathsEntity))
    pathCount = state.EntityManager.GetBuffer<WalkwayPathRange>(pathsEntity).Length;

for (int i = 0; i < missing; i++)
{
    var entity = state.EntityManager.CreateEntity(_ambientArchetype);
    var position = _random.NextFloat2(world.BeltMin, world.BeltMax);
    state.EntityManager.SetComponentData(entity, new SwarmAgent { Tier = SwarmTier.Ambient });
    state.EntityManager.SetComponentData(entity, new BeltPosition { Value = position });
    state.EntityManager.SetComponentData(entity, new SwarmVelocity { Value = float2.zero });
    state.EntityManager.SetComponentData(entity, new AmbientPathState
    {
        PathId = pathCount > 0 ? (int)(_random.NextUInt() % (uint)pathCount) : -1,
        Distance = _random.NextFloat(0f, 50f),
        Speed = _random.NextFloat(0.4f, 1.1f),
        Lateral = _random.NextFloat(-0.6f, 0.6f),
    });
}
```

- [ ] **Step 2: Replace the ambient bounce-wander with path-follow**

In `SwarmSteeringSystem.OnUpdate`, replace the ambient `foreach` (the `.WithNone<SwarmHealth>()` block) with:

```csharp
if (SystemAPI.TryGetSingletonEntity<WalkwayPathsTag>(out var pathsEntity))
{
    var points = SystemAPI.GetBuffer<WalkwayPoint>(pathsEntity);
    var ranges = SystemAPI.GetBuffer<WalkwayPathRange>(pathsEntity);
    if (points.Length >= 2 && ranges.Length > 0)
    {
        var pointArr = points.AsNativeArray().Reinterpret<float2>(); // WalkwayPoint is one float2
        foreach (var (position, path) in
                 SystemAPI.Query<RefRW<BeltPosition>, RefRW<AmbientPathState>>())
        {
            if (path.ValueRO.PathId < 0 || path.ValueRO.PathId >= ranges.Length) continue;
            var range = ranges[path.ValueRO.PathId];

            path.ValueRW.Distance += path.ValueRO.Speed * deltaTime;
            WalkwayPathSampler.Sample(pointArr, range.Start, range.Count,
                path.ValueRO.Distance, out float2 pos, out float2 dir);

            float2 perp = new float2(-dir.y, dir.x);
            position.ValueRW.Value = pos + perp * path.ValueRO.Lateral;
        }
    }
}
```

Delete the old ambient bounce-wander loop. (If `Reinterpret<float2>` is rejected by the installed version — `WalkwayPoint` is a single `float2` so it's layout-compatible — instead build a temp `NativeArray<float2>` from the buffer, as in Task 2.)

- [ ] **Step 3: Compile check**

`mcp__unityMCP__read_console`. Expected: no errors.

- [ ] **Step 4: Smoke-test the capability**

Temporarily add a `WalkwayPathAuthoring` to 03_Level1 with one `Paths[0]` root holding 3–4 child waypoint transforms tracing a sidewalk. Boot via Recipe 4; confirm ambient agents **walk along the path** (offset laterally, looping) instead of bounce-wandering. Then **remove the temporary authoring object** (Plan C authors the real paths) or leave it disabled as an example; note what you did.

- [ ] **Step 5: Commit**

```bash
git add Assets/_neon/Scripts/Simulation/SwarmSpawnSystem.cs Assets/_neon/Scripts/Simulation/SwarmSteeringSystem.cs
git commit -m "feat: ambient agents follow authored walkway paths (Plan A)"
```

---

## Task 8: Per-zone density curve

**Files:**
- Create: `Assets/_neon/Scripts/Simulation/SwarmDensity.cs`
- Test: `Assets/_neon/Tests/EditMode/Simulation/SwarmDensityTests.cs`
- Modify: `LevelConfigurationAsset.cs` (`SwarmDensityBlock`), `SwarmConfig.cs`, `SwarmBridge.cs`

- [ ] **Step 1: Write the failing test for the cap resolver**

```csharp
using NUnit.Framework;
using UnityEngine;
using BrainlessLabs.Neon;

namespace BrainlessLabs.Neon.Tests
{
    public class SwarmDensityTests
    {
        [Test]
        public void No_curve_falls_back_to_flat_cap_times_nastiness()
        {
            int cap = SwarmDensity.ResolveChaffCap(flatCap: 100, curve: null, progression: 0.5f, nastiness: 1.2f);
            Assert.AreEqual(120, cap);
        }

        [Test]
        public void Curve_defines_absolute_cap_over_progression()
        {
            var curve = new AnimationCurve(new Keyframe(0f, 20f), new Keyframe(1f, 150f));
            int lo = SwarmDensity.ResolveChaffCap(999, curve, 0f, 1f);
            int hi = SwarmDensity.ResolveChaffCap(999, curve, 1f, 1f);
            Assert.AreEqual(20, lo);
            Assert.AreEqual(150, hi);
        }

        [Test]
        public void Result_is_clamped_to_the_proxy_pool_ceiling()
        {
            var curve = new AnimationCurve(new Keyframe(0f, 150f), new Keyframe(1f, 150f));
            Assert.AreEqual(150, SwarmDensity.ResolveChaffCap(999, curve, 1f, 2f)); // 150*2 → clamp 150
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run EditMode. Expected: FAIL — `SwarmDensity` undefined.

- [ ] **Step 3: Implement the resolver**

```csharp
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>Resolves the live chaff cap from a progression curve (or a flat fallback),
    /// scaled by the Signal nastiness, clamped to the 150 proxy-pool ceiling.</summary>
    public static class SwarmDensity
    {
        public const int ProxyPoolCeiling = 150;

        public static int ResolveChaffCap(int flatCap, AnimationCurve curve, float progression, float nastiness)
        {
            float baseCap = (curve != null && curve.length > 0)
                ? curve.Evaluate(Mathf.Clamp01(progression))
                : flatCap;
            return Mathf.Min(ProxyPoolCeiling, Mathf.RoundToInt(baseCap * nastiness));
        }
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run EditMode. Expected: 3 `SwarmDensityTests` PASS; suite green.

- [ ] **Step 5: Add the curves to the config**

In `SwarmDensityBlock` (`LevelConfigurationAsset.cs`), add:

```csharp
[Tooltip("Optional: chaff cap over level progression 0→1 (spec §5 rising sawtooth). Empty = use ChaffCap flat.")]
public AnimationCurve ChaffCapCurve = new();
```

In `SwarmConfig` add a readonly `AnimationCurve ChaffCapCurve` field, set it in the constructor, and pass `block.ChaffCapCurve` in `SwarmConfig.From`.

- [ ] **Step 6: Sample progression in the bridge**

In `SwarmBridge.Tick`, replace the `state.ChaffCap = ...` line with a progression-driven resolve (player-X vs belt extents):

```csharp
float span = _config.BeltMax.x - _config.BeltMin.x;
float progression = span > 1e-3f
    ? Mathf.Clamp01(((state.PlayerPosition.x) - _config.BeltMin.x) / span)
    : 0f;
state.ChaffCap = SwarmDensity.ResolveChaffCap(_config.ChaffCap, _config.ChaffCapCurve, progression, nastiness);
state.SpawnRatePerSecond = _config.SpawnRatePerSecond * nastiness;
```

- [ ] **Step 7: Compile + play-verify**

`mcp__unityMCP__read_console` — no errors. On Level 01's `SwarmDensityBlock`, set a temporary `ChaffCapCurve` (20 → 120 → 150). Boot via Recipe 4; walk the player forward and confirm the live chaff count **ramps up with progression** (watch `CountHot()` / the `[Swarm] Chaff cap reached` log, or the Game view). Note: Plan C sets the final curve values.

- [ ] **Step 8: Commit**

```bash
git add Assets/_neon/Scripts/Simulation/SwarmDensity.cs Assets/_neon/Tests/EditMode/Simulation/SwarmDensityTests.cs Assets/_neon/Scripts/Spawner/LevelConfigurationAsset.cs Assets/_neon/Scripts/Swarm/SwarmConfig.cs Assets/_neon/Scripts/Swarm/SwarmBridge.cs
git commit -m "feat: per-zone chaff density curve sampled by progression (Plan A)"
```

---

## Task 9: Confirm sprite-swap plumbing (hand-off to Plan D)

**Files:**
- Verify: `Assets/_neon/Scripts/Swarm/SwarmRenderRig.cs`

- [ ] **Step 1: Confirm the fields survive the rework**

Confirm `_chaffSprite` (`Sprite`), `_hotColor`, `_finishReadyColor`, `_ambientMaterial` (`Material`), `_ambientSize` are still `[SerializeField]` and that the rig reads only `BeltPosition.Value` (unaffected by `LaneIndex` removal). No code change expected — this task documents the seam Plan D fills.

- [ ] **Step 2: Play-verify render still works**

Boot + Level 01: chaff proxies + ambient instanced quads still render at the new crowd positions and along the walkway. Verify ambient via `UnityStats.instancedBatches` or the Game view (not MCP screenshots — they miss `Graphics.DrawMeshInstanced`).

- [ ] **Step 3: (No commit unless a guard was added.)**

---

## Task 10: Green sweep & play-test gate (Plan A acceptance)

- [ ] **Step 1: Full EditMode suite**

Run EditMode (`mcp__unityMCP__run_tests`). Expected: **all green** — 141 (post-Plan-0) + 10 new (`SwarmSteeringTests` 4, `WalkwayPathSamplerTests` 3, `SwarmDensityTests` 3) = 151, no regressions.

- [ ] **Step 2: Acceptance play-test (Recipe 4 boot, Level 01, swarm enabled)** — confirm all:
  - Chaff read as a **crowd** converging on the player, keeping spacing — **no 3-lane columns**.
  - Ambient agents **walk the authored path** (smoke path from Task 7), not bounce-wander.
  - Chaff density **ramps with progression** (temporary curve).
  - Finish-Ready tint still shows on ready chaff; verb hits still finish them (bridge unchanged).
  - No console errors; frame rate healthy at cap (spec headroom ~197 FPS).

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "chore: Plan A green-build sweep — crowd + walkway paths + density curve verified"
```

---

## Self-Review

**1. Spec coverage (§5 Plan A):**
- "Kill lanes (`LANE_COUNT`/`LaneIndex`, lane-Y hold + jitter)" → Tasks 2, 3, 4. ✓
- "seek + separation + light cohesion, spatial hash cheap at 150" → Tasks 1, 2 (O(n) scan per agent; note that a uniform-grid hash is the drop-in if the cap rises above ~150 — O(n²) is fine at 150 given the headroom). ✓ (deviation noted below)
- "new WalkwayPath authoring + baker + ambient path-follow" → Tasks 5, 6, 7. ✓
- "progression-driven AnimationCurve density on `SwarmDensityBlock`; `SwarmConfig.From` carries it" → Task 8 (sampled in `SwarmBridge.Tick` where progression is known — see note). ✓
- "sprite-swap plumbing exposed for Plan D" → Task 9 (already exposed; confirmed). ✓
- "ISwarmBridge + consumers untouched" → interface unchanged; only `SwarmBridge.Tick`'s internal cap math extended. ✓

**2. Placeholder scan:** No "TBD/handle later". Version-sensitive Entities API spots (`QueryBuilder().Build()` under Burst, `Reinterpret<float2>`, `TryGetSingletonEntity`, `AddBuffer`) are each flagged with a concrete fallback rather than guessed silently.

**3. Type consistency:** `SwarmSteering.ComputeDesiredVelocity(float2, float2, NativeArray<float2>, float×6)` matches Task-1 test/impl and Task-2 caller. `WalkwayPathSampler.Sample(NativeArray<float2>, int, int, float, out float2, out float2)` matches Task-5 test/impl and Task-7 caller. `AmbientPathState` fields (`PathId/Distance/Speed/Lateral`) match Task-6 def and Task-7 seed+steer. `SwarmDensity.ResolveChaffCap(int, AnimationCurve, float, float)` matches Task-8 test/impl and bridge caller. `SwarmConfig.ChaffCapCurve` added in Task 8 before the bridge reads it.

**Deviations flagged:**
- **Neighbor gather is O(n²)** (per-agent scan of the crowd array), not a spatial hash. At the 150 cap with ~197 FPS headroom this is comfortably fine; a uniform-grid hash is the documented optimization if the cap ever rises. Chosen for correctness + testability simplicity over premature optimization (YAGNI).
- **Density curve sampled in `SwarmBridge.Tick`, not `SwarmConfig.From`** — the spec said `From`, but `From` runs once and can't ramp; the bridge already rewrites the cap every tick and already has the player position, so that's the correct, minimal home. Interface + consumers untouched.

**Known non-unit-tested surface:** the ECS system rewrites, the runtime bake, and rendering are verified in play mode (Recipe 4) — DOTS systems aren't unit-testable without a World harness (which the suite doesn't have). The three pure helpers are the extracted testable cores.

---

## Execution Handoff

**Plan complete and saved to `docs/superpowers/plans/2026-07-05-feel-and-level-plan-a-swarm.md`.** Two execution options:

1. **Subagent-Driven (recommended)** — a fresh subagent per task, review between tasks. Tasks 2/3/6/7/8/9/10 need a live Unity Editor over the MCP for compile + play-test.
2. **Inline Execution** — run tasks here with checkpoints.

Which approach — or review the plan yourself first?
