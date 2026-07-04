using System.Collections.Generic;
using VContainer.Unity;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Registered as a VContainer entry point (ITickable) so the DI player loop
    /// drives Advance() once per frame with Time.deltaTime. Tests call Advance()
    /// directly. Do not register/unregister tickables from inside Tick.
    /// </summary>
    public sealed class GameplayClock : IGameplayClock, ITickable
    {
        private struct Entry
        {
            public int Order;
            public int Sequence;
            public IGameplayTickable Tickable;
        }

        private readonly List<Entry> _entries = new();
        private readonly Dictionary<ModifierSource, float> _scales = new();
        private int _nextSequence;
        private bool _orderDirty;
        private bool _loggedFirstTick;

        public float GameplayTime { get; private set; }
        public float DeltaTime { get; private set; }

        public float EffectiveScale
        {
            get
            {
                float scale = 1f;
                foreach (var value in _scales.Values)
                {
                    scale *= value;
                }
                return scale;
            }
        }

        void ITickable.Tick()
        {
            if (!_loggedFirstTick)
            {
                UnityEngine.Debug.Log("[Gameplay] GameplayClock ticking.");
                _loggedFirstTick = true;
            }
            Advance(UnityEngine.Time.deltaTime);
        }

        /// <summary>Advance gameplay time by an unscaled delta and run the ordered tick.</summary>
        public void Advance(float unscaledDeltaTime)
        {
            DeltaTime = unscaledDeltaTime * EffectiveScale;
            GameplayTime += DeltaTime;

            if (_orderDirty)
            {
                _entries.Sort(static (a, b) =>
                    a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Sequence.CompareTo(b.Sequence));
                _orderDirty = false;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].Tickable.Tick(DeltaTime);
            }
        }

        public void Register(IGameplayTickable tickable, int order)
        {
            _entries.Add(new Entry { Order = order, Sequence = _nextSequence++, Tickable = tickable });
            _orderDirty = true;
        }

        public void Unregister(IGameplayTickable tickable)
        {
            _entries.RemoveAll(e => ReferenceEquals(e.Tickable, tickable));
        }

        public void SetScale(ModifierSource source, float scale)
        {
            _scales[source] = scale;
        }

        public void ClearScale(ModifierSource source)
        {
            _scales.Remove(source);
        }
    }
}
