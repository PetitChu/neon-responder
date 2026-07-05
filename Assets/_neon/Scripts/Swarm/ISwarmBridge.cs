using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The single seam between the DOTS swarm and the MonoBehaviour world
    /// (spec §5.2): targeting queries, damage/finish commands. Bridge-only
    /// spatial truth for chaff (F4) — chaff have no colliders.
    /// </summary>
    public interface ISwarmBridge
    {
        /// <summary>Nearest living chaff inside the facing arc, or false.</summary>
        bool TryGetNearestHot(Vector2 origin, float facingSign, float arcDegrees, float range, out TargetRef target);

        /// <summary>Nearest Finish-Ready chaff to the origin (arc-free — the selector wants proximity), or false.</summary>
        bool TryGetNearestFinishReady(Vector2 origin, out TargetRef target);

        int CountHot();
        int CountFinishReady();

        /// <summary>Auto-engage chip on a chaff target (queued into the sim).</summary>
        void ApplyChip(in TargetRef target, int damage);

        /// <summary>
        /// A verb hitbox sweep against chaff. Finish-Ready chaff die as a FINISH
        /// (publishes EnemyFinished); others take verb damage. Returns true if any
        /// chaff was hit (feeds the whiff decision).
        /// </summary>
        bool ApplyVerbHit(Bounds hitBounds, AttackData attackData);

        /// <summary>Radial chaff damage (e.g. Concussive Finish). Non-finish damage — may kill.</summary>
        void ApplyAreaDamage(Vector2 center, float radius, int damage);

        /// <summary>Drop in-radius chaff to the Finish-Ready threshold (no kill) — Siren Pulse. Returns count.</summary>
        int MassFinishReady(Vector2 center, float radius);

        /// <summary>Kill in-radius chaff AS finishes (each publishes EnemyFinished) — the Overcharge finisher. Returns count.</summary>
        int FinishAllChaff(Vector2 center, float radius);
    }
}
