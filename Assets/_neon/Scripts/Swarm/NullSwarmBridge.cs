using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>Session default for scenes without a swarm (menus, training hall).</summary>
    public sealed class NullSwarmBridge : ISwarmBridge
    {
        public bool TryGetNearestHot(Vector2 origin, float facingSign, float arcDegrees, float range, out TargetRef target)
        {
            target = default;
            return false;
        }

        public bool TryGetNearestFinishReady(Vector2 origin, out TargetRef target)
        {
            target = default;
            return false;
        }

        public int CountHot() => 0;
        public int CountFinishReady() => 0;

        public void ApplyChip(in TargetRef target, int damage)
        {
        }

        public bool ApplyVerbHit(Bounds hitBounds, AttackData attackData) => false;

        public void ApplyAreaDamage(Vector2 center, float radius, int damage)
        {
        }

        public int MassFinishReady(Vector2 center, float radius) => 0;
        public int FinishAllChaff(Vector2 center, float radius) => 0;
    }
}
