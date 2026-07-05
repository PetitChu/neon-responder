using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Hold-the-zone-under-fire (spec §5.4). Fills while the player stands within
    /// radius; fill rate = (1/duration) × ObjectiveFillRateScale (a Run stat, so
    /// Objective protocols / Priority Override speed it). Pure logic — no collider.
    /// </summary>
    public sealed class RebootNodeObjective : IObjective
    {
        private readonly IStatSystem _stats;
        private readonly float _radiusSq;
        private readonly float _baseRatePerSecond;

        public Vector2 Position { get; }
        public float Normalized { get; private set; }
        public bool IsComplete { get; private set; }
        public bool PlayerInZone { get; private set; }

        public RebootNodeObjective(IStatSystem stats, Vector2 position, float radius, float durationSeconds)
        {
            _stats = stats;
            Position = position;
            _radiusSq = radius * radius;
            _baseRatePerSecond = 1f / Mathf.Max(0.01f, durationSeconds);

            // Seed the tunable base once (idempotent — protocols modify via modifiers).
            if (_stats.Run.GetBase(StatId.ObjectiveFillRateScale) <= 0f)
            {
                _stats.Run.SetBase(StatId.ObjectiveFillRateScale, 1f);
            }
        }

        public bool Tick(float deltaTime, Vector2 playerPosition)
        {
            if (IsComplete) return false;

            PlayerInZone = (playerPosition - Position).sqrMagnitude <= _radiusSq;
            if (!PlayerInZone) return false;

            float rate = _baseRatePerSecond * Mathf.Max(0f, _stats.Run.GetValue(StatId.ObjectiveFillRateScale));
            Normalized = Mathf.Clamp01(Normalized + rate * deltaTime);

            if (Normalized >= 1f)
            {
                IsComplete = true;
                return true;
            }
            return false;
        }
    }
}
