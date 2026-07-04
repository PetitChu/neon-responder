using Unity.Entities;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>2D belt position (x = along the belt, y = depth lane band).</summary>
    public struct BeltPosition : IComponentData
    {
        public float2 Value;
        public int LaneIndex;
    }

    public struct SwarmVelocity : IComponentData
    {
        public float2 Value;
    }

    /// <summary>Chaff only (F2: sim-owned health). Ambient agents have no health.</summary>
    public struct SwarmHealth : IComponentData
    {
        public int Current;
        public int Max;
    }

    /// <summary>Enabled when a chaff agent is at or under the Finish-Ready threshold.</summary>
    public struct FinishReadyTag : IComponentData, IEnableableComponent
    {
    }

    /// <summary>
    /// Control singleton written by the SwarmBridge every gameplay tick.
    /// The sim reads it; it never reads Mono state directly.
    /// </summary>
    public struct SwarmWorldState : IComponentData
    {
        public float2 PlayerPosition;
        public float PlayerFacingSign;
        public int ChaffCap;
        public int AmbientCap;
        public float SpawnRatePerSecond;
        public int ChaffMaxHealth;
        public float ChaffMoveSpeed;
        public float2 BeltMin;
        public float2 BeltMax;
        public float FinishReadyThreshold;
        public byte Enabled;
    }

    /// <summary>
    /// Damage command from the Mono side. IsChip = 1 for auto-engage chip, which
    /// pushes toward Finish-Ready but NEVER kills (spec §5.1 — floored at 1 HP by
    /// SwarmDamageSystem); IsChip = 0 for verb damage, which may kill.
    /// </summary>
    public struct SwarmDamageCommand : IBufferElementData
    {
        public Entity Target;
        public int Amount;
        public byte IsChip;
    }

    /// <summary>Instant-kill command (finishing verb hit on a Finish-Ready chaff).</summary>
    public struct SwarmKillCommand : IBufferElementData
    {
        public Entity Target;
    }

    /// <summary>Events out of the sim, drained by the bridge each tick.</summary>
    public struct SwarmEventRecord : IBufferElementData
    {
        public const byte KIND_CHAFF_DIED = 0;

        public byte Kind;
        public float2 Position;
    }
}
