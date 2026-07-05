using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    public sealed class ProgressionSystem : IProgressionSystem, IDisposable
    {
        private readonly IGameplaySignals _signals;
        private readonly IGameplayClock _clock;
        private readonly IProtocolService _protocols;
        private readonly GrowthConfig _config;
        private readonly ModifierSource _slowMoSource = ModifierSource.Create("levelup-slowmo");
        private readonly IDisposable _xpSubscription;

        private int _totalXp;
        private int _levelStartXp;
        private int _nextLevelAtXp;
        private int _pendingOffers;
        private IReadOnlyList<ProtocolDefinitionAsset> _currentChoices;

        public int Level { get; private set; } = 1;
        public bool AwaitingChoice { get; private set; }

        public ProgressionSystem(IGameplaySignals signals, IGameplayClock clock,
            IProtocolService protocols, GrowthConfig config)
        {
            _signals = signals;
            _clock = clock;
            _protocols = protocols;
            _config = config;
            _nextLevelAtXp = XpCostForLevel(1);
            _xpSubscription = _signals.On<XpGained>().Subscribe(e => OnXp(e.TotalXp));
        }

        public void Dispose()
        {
            _xpSubscription?.Dispose();
            _clock.ClearScale(_slowMoSource);
        }

        public void Choose(int index)
        {
            if (!AwaitingChoice || _currentChoices == null || _currentChoices.Count == 0) return;

            int clampedIndex = Mathf.Clamp(index, 0, _currentChoices.Count - 1);
            _protocols.Acquire(_currentChoices[clampedIndex]);

            AwaitingChoice = false;
            _currentChoices = null;
            _clock.ClearScale(_slowMoSource);
            TryOfferNext();
        }

        private void OnXp(int totalXp)
        {
            _totalXp = totalXp;
            while (_totalXp >= _nextLevelAtXp)
            {
                Level++;
                _levelStartXp = _nextLevelAtXp;
                _nextLevelAtXp += XpCostForLevel(Level);
                _pendingOffers++;
                _signals.Publish(new PlayerLevelChanged(Level));
            }

            _signals.Publish(new XpProgressChanged(Level, _totalXp - _levelStartXp, _nextLevelAtXp - _levelStartXp));
            TryOfferNext();
        }

        private void TryOfferNext()
        {
            while (!AwaitingChoice && _pendingOffers > 0)
            {
                _pendingOffers--;
                var choices = _protocols.RollChoices(3);
                if (choices.Count == 0) continue; // pool dry: the level is banked, no draft

                _currentChoices = choices;
                AwaitingChoice = true;
                _clock.SetScale(_slowMoSource, _config.LevelUpSlowMoScale);

                var choicesArray = new ProtocolDefinitionAsset[choices.Count];
                for (int i = 0; i < choices.Count; i++) choicesArray[i] = choices[i];
                _signals.Publish(new LevelUpChoicesReady(Level, choicesArray));
            }
        }

        // XP required to clear level N (protocol doc §8.3: ceil(10 × N^1.35)).
        private int XpCostForLevel(int level)
        {
            return Mathf.CeilToInt(_config.XpCostBase * Mathf.Pow(level, _config.XpCostExponent));
        }
    }
}
