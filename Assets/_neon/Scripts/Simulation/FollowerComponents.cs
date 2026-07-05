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
