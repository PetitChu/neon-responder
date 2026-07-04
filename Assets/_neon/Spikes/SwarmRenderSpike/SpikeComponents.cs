using Unity.Entities;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.SwarmRenderSpike
{
    public struct SpikePosition : IComponentData
    {
        public float2 Value;
    }

    public struct SpikeVelocity : IComponentData
    {
        public float2 Value;
    }

    /// <summary>Hot chaff — rendered via pooled SpriteRenderer proxies.</summary>
    public struct HotAgentTag : IComponentData
    {
    }

    /// <summary>Ambient vibe props — rendered via Graphics.RenderMeshInstanced.</summary>
    public struct AmbientAgentTag : IComponentData
    {
    }

    /// <summary>Singleton: the belt rect agents bounce inside.</summary>
    public struct SpikeBounds : IComponentData
    {
        public float2 Min;
        public float2 Max;
    }
}
