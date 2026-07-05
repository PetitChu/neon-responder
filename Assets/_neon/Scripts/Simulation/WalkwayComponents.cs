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
