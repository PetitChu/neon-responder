using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Gameplay-specific queries over <see cref="IEntitiesService"/>, replacing the legacy
    /// static EnemyManager helpers. Kept as extension methods (not on the service interface)
    /// so IEntitiesService stays a thin, DOTS-facing registry while combat code gets the
    /// convenience queries it needs. All methods are null-safe on the receiver so call sites
    /// in scenes without a DI scope degrade gracefully instead of throwing.
    /// </summary>
    public static class EntitiesQueries
    {
        /// <summary>
        /// Number of tracked enemies currently attacking the player
        /// (in an <see cref="EnemyAttack"/> or <see cref="EnemyMoveToTargetAndAttack"/> state).
        /// </summary>
        public static int GetEnemyAttackerCount(this IEntitiesService entities)
        {
            if (entities == null) return 0;

            int count = 0;
            var enemies = entities.GetByType(UNITTYPE.ENEMY);
            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i].GameObject;
                if (go == null) continue;

                var stateMachine = go.GetComponent<UnitStateMachine>();
                var state = stateMachine != null ? stateMachine.GetCurrentState() : null;
                if (state is EnemyAttack || state is EnemyMoveToTargetAndAttack) count++;
            }
            return count;
        }

        /// <summary>
        /// Disables AI and forces every tracked enemy to idle (e.g. when the player dies).
        /// </summary>
        public static void DisableAllEnemyAI(this IEntitiesService entities)
        {
            if (entities == null) return;

            var enemies = entities.GetByType(UNITTYPE.ENEMY);
            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i].GameObject;
                if (go == null) continue;

                var behaviour = go.GetComponent<EnemyBehaviour>();
                if (behaviour != null) behaviour.AI_Active = false;

                go.GetComponent<UnitStateMachine>()?.SetState(new EnemyIdle());
            }
        }

        /// <summary>
        /// Returns a living tracked enemy within <paramref name="range"/> of <paramref name="position"/>
        /// that is currently knocked down on the ground, or null if none.
        /// </summary>
        public static GameObject GetNearbyDownedEnemy(this IEntitiesService entities, Vector2 position, float range)
        {
            if (entities == null) return null;

            var enemies = entities.GetByType(UNITTYPE.ENEMY);
            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i].GameObject;
                if (go == null) continue;
                if (Vector2.Distance(position, go.transform.position) >= range) continue;

                if (go.GetComponent<HealthSystem>()?.isDead == true) continue;
                if (go.GetComponent<UnitStateMachine>()?.GetCurrentState() is UnitKnockDownGrounded) return go;
            }
            return null;
        }

        /// <summary>
        /// True if any tracked enemy has spotted the player.
        /// </summary>
        public static bool AnyEnemyDetectedPlayer(this IEntitiesService entities)
        {
            if (entities == null) return false;

            var enemies = entities.GetByType(UNITTYPE.ENEMY);
            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i].GameObject;
                if (go != null && go.GetComponent<UnitActions>()?.targetSpotted == true) return true;
            }
            return false;
        }

        /// <summary>
        /// Nearest living tracked enemy inside the facing arc (auto-engage targeting,
        /// hero-tier side of the bridge query). facingSign: +1 right, -1 left.
        /// </summary>
        public static GameObject GetNearestEnemyInArc(this IEntitiesService entities,
            Vector2 origin, float facingSign, float arcDegrees, float range)
        {
            if (entities == null) return null;

            float cosHalfArc = Mathf.Cos(Mathf.Min(arcDegrees, 360f) * 0.5f * Mathf.Deg2Rad);
            var facing = new Vector2(Mathf.Sign(facingSign == 0f ? 1f : facingSign), 0f);
            float bestSqrDistance = range * range;
            GameObject best = null;

            var enemies = entities.GetByType(UNITTYPE.ENEMY);
            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i].GameObject;
                if (go == null) continue;
                if (go.GetComponent<HealthSystem>()?.isDead == true) continue;

                Vector2 toEnemy = (Vector2)go.transform.position - origin;
                float sqrDistance = toEnemy.sqrMagnitude;
                if (sqrDistance > bestSqrDistance || sqrDistance < 1e-6f) continue;
                if (arcDegrees < 360f && Vector2.Dot(toEnemy.normalized, facing) < cosHalfArc) continue;

                bestSqrDistance = sqrDistance;
                best = go;
            }
            return best;
        }
    }
}
