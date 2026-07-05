using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    public sealed class OverchargeFinisher : IOverchargeFinisher, IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 46; // after SpecialSystem (45)

        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly IEntitiesService _entities;
        private readonly ISwarmBridge _bridge;
        private readonly IInputService _input;
        private readonly IEconomySystem _economy;
        private readonly SpecialConfig _config;
        private readonly ModifierSource _freezeSource = ModifierSource.Create("finisher-freeze");

        private bool _freezing;
        private float _freezeReleaseAt; // unscaled time
        private bool _lastReady;

        public bool IsReady => _economy.IsOverchargeFull;

        public OverchargeFinisher(IGameplayClock clock, IGameplaySignals signals, IEntitiesService entities,
            ISwarmBridge bridge, IInputService input, IEconomySystem economy, SpecialConfig config)
        {
            _clock = clock;
            _signals = signals;
            _entities = entities;
            _bridge = bridge;
            _input = input;
            _economy = economy;
            _config = config;
            _clock.Register(this, TICK_ORDER);
            _lastReady = IsReady;
        }

        public void Dispose()
        {
            _clock.Unregister(this);
            _clock.ClearScale(_freezeSource);
        }

        public void Tick(float deltaTime)
        {
            // Ready-edge → HUD.
            bool ready = IsReady;
            if (ready != _lastReady) { _lastReady = ready; _signals.Publish(new OverchargeReadyChanged(ready)); }

            // Unscaled freeze release (scaled dt is 0 during the freeze).
            if (_freezing && Time.unscaledTime >= _freezeReleaseAt) ReleaseFreeze();

            if (_input.FinisherKeyDown(1)) TryFire();
        }

        private void TryFire()
        {
            if (!_economy.TryConsumeOvercharge()) return;

            var player = _entities.GetFirstByType(UNITTYPE.PLAYER).GameObject;
            Vector2 origin = player != null ? (Vector2)player.transform.position : Vector2.zero;

            int cleared = _bridge.FinishAllChaff(origin, _config.FinisherRadius);

            _clock.SetScale(_freezeSource, 0f);      // freeze-frame
            _freezing = true;
            _freezeReleaseAt = Time.unscaledTime + _config.FinisherFreezeSeconds;

            _signals.Publish(new OverchargeFinisherFired(origin, cleared));
            _signals.Publish(new Callout("OVERCHARGE", origin));
            _signals.Publish(new OverchargeReadyChanged(false));
            _lastReady = false;
        }

        private void ReleaseFreeze()
        {
            _freezing = false;
            _clock.ClearScale(_freezeSource);
        }

        // Test seam: the freeze release is unscaled-time-gated (Time.unscaledTime),
        // which doesn't advance deterministically in EditMode — release directly.
        public void ReleaseFreezeForTest() => ReleaseFreeze();
    }
}
