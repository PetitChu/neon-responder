using Unity.Entities;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>
    /// Swarm density tiers (spec §5.2): Chaff = hot, hittable, finish-ready-capable;
    /// Ambient = pure-vibe props.
    /// </summary>
    public enum SwarmTier : byte
    {
        Chaff = 0,
        Ambient = 1
    }

    /// <summary>
    /// Marks an entity as a swarm agent. The full component set
    /// (BeltPosition, Velocity, Health, HotFlag, FinishReadyTag, EngageIntent)
    /// lands with the swarm systems in Milestone 1 (Plan 2).
    /// </summary>
    public struct SwarmAgent : IComponentData
    {
        public SwarmTier Tier;
    }
}
