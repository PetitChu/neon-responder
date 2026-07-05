using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Picks the SINGLE highest-priority Finish-Ready target across both worlds
    /// (R7 one-prompt rule): nearest to the player. Publishes the prompt + the
    /// "+N ready" count; only publishes on change.
    /// </summary>
    public sealed class FinishReadySelector : IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 20; // reserved band (IGameplayClock doc)
        private const float REPUBLISH_POSITION_SQR = 0.04f;

        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly IEntitiesService _entities;
        private readonly ISwarmBridge _bridge;

        private bool _lastHasTarget;
        private Vector2 _lastPosition;
        private int _lastCount = -1;

        public FinishReadySelector(IGameplayClock clock, IGameplaySignals signals,
            IEntitiesService entities, ISwarmBridge bridge)
        {
            _clock = clock;
            _signals = signals;
            _entities = entities;
            _bridge = bridge;
            _clock.Register(this, TICK_ORDER);
        }

        public void Tick(float deltaTime)
        {
            var player = _entities.GetFirstByType(UNITTYPE.PLAYER).GameObject;
            if (player == null)
            {
                PublishIfChanged(false, Vector2.zero, 0);
                return;
            }
            Vector2 origin = player.transform.position;

            GameObject bestHero = null;
            float bestHeroSqrDistance = float.MaxValue;
            int heroReadyCount = 0;
            var enemies = _entities.GetByType(UNITTYPE.ENEMY);
            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i].GameObject;
                if (go == null) continue;
                var marker = go.GetComponent<FinishReadyMarker>();
                if (marker == null || !marker.IsReady) continue;

                heroReadyCount++;
                float sqrDistance = ((Vector2)go.transform.position - origin).sqrMagnitude;
                if (sqrDistance < bestHeroSqrDistance)
                {
                    bestHeroSqrDistance = sqrDistance;
                    bestHero = go;
                }
            }

            bool hasChaff = _bridge.TryGetNearestFinishReady(origin, out var chaffTarget);
            float chaffSqrDistance = hasChaff ? (chaffTarget.Position - origin).sqrMagnitude : float.MaxValue;
            int readyCount = heroReadyCount + _bridge.CountFinishReady();

            if (bestHero == null && !hasChaff)
            {
                PublishIfChanged(false, Vector2.zero, readyCount);
                return;
            }

            Vector2 targetPosition = chaffSqrDistance <= bestHeroSqrDistance
                ? chaffTarget.Position
                : (Vector2)bestHero.transform.position;
            PublishIfChanged(true, targetPosition, readyCount);
        }

        public void Dispose()
        {
            _clock.Unregister(this);
        }

        private void PublishIfChanged(bool hasTarget, Vector2 position, int count)
        {
            bool changed = hasTarget != _lastHasTarget
                || count != _lastCount
                || (hasTarget && (position - _lastPosition).sqrMagnitude > REPUBLISH_POSITION_SQR);
            if (!changed) return;

            _lastHasTarget = hasTarget;
            _lastPosition = position;
            _lastCount = count;
            // Single-verb M1: the prompt verb is always PUNCH (tiered challenges are M2).
            _signals.Publish(new FinishReadyPromptChanged(hasTarget, position, ATTACKTYPE.PUNCH, count));
        }
    }
}
