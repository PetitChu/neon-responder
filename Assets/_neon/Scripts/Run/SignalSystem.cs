using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    public sealed class SignalSystem : ISignalSystem, IDisposable
    {
        private readonly IGameplaySignals _signals;
        private readonly IStatSystem _stats;
        private readonly float _maxSpawnNastinessBonus;
        private readonly ModifierSource _source = ModifierSource.Create("signal");

        public float Value { get; private set; }
        public float Dawn { get; }
        public bool IsDawn => Value >= Dawn;

        public SignalSystem(IGameplaySignals signals, IStatSystem stats, float dawnValue, float maxSpawnNastinessBonus)
        {
            _signals = signals;
            _stats = stats;
            Dawn = Mathf.Max(0.0001f, dawnValue);
            _maxSpawnNastinessBonus = maxSpawnNastinessBonus;

            _stats.Run.SetBase(StatId.SpawnNastiness, 1f);
            ApplyNastiness();
        }

        public void Raise(float amount) => SetValue(Value + amount);
        public void Lower(float amount) => SetValue(Value - amount);

        public void Dispose()
        {
            _stats.Run.RemoveBySource(_source);
        }

        private void SetValue(float value)
        {
            Value = Mathf.Clamp(value, 0f, Dawn);
            ApplyNastiness();
            _signals.Publish(new SignalChanged(Value, Dawn));
        }

        private void ApplyNastiness()
        {
            // Signal is a modifier source like Momentum, but PctAdd (never Mult —
            // Momentum stays the only global multiplier, protocol doc §8.1).
            float fraction = Value / Dawn; // 0..1
            _stats.Run.RemoveBySource(_source);
            _stats.Run.AddModifier(StatId.SpawnNastiness, StatOp.PctAdd, fraction * _maxSpawnNastinessBonus, _source);
        }
    }
}
