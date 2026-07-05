using System;
using R3;
using UnityEngine;
using VContainer;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Spec §5.5 "audio layering by Momentum tier + Signal" (F3: track-swap + stingers).
    /// A combined 0–3 "heat" band = max(Momentum tier, Signal band) picks the music bed;
    /// tier-up/finisher/node-restored fire one-shot stingers. Pure consumer.
    /// The music config currently ships ONE gameplay track ("Level 1") — with a single
    /// band entry the director never swaps (documented F3 fallback); add tracks to
    /// _bandTracks in the scene when they exist.
    /// </summary>
    public class SignalMusicDirector : MonoBehaviour
    {
        [SerializeField] private string[] _bandTracks = { "Level 1" };
        [SerializeField] private float _crossfadeSeconds = 1.5f;

        [Inject] private IGameplaySignals _signals;
        [Inject] private IAudioService _audio;

        private int _momentumBand;
        private int _signalBand;
        private int _currentBand = -1;
        private readonly System.Collections.Generic.List<IDisposable> _subs = new();

        void Start()
        {
            if (_signals == null || _audio == null) { enabled = false; return; }

            _subs.Add(_signals.On<MomentumTierChanged>().Subscribe(e => { _momentumBand = (int)e.Current; Reevaluate(); }));
            _subs.Add(_signals.On<SignalChanged>().Subscribe(e =>
            {
                _signalBand = e.Dawn > 0f ? Mathf.Clamp(Mathf.RoundToInt(e.Value / e.Dawn * 3f), 0, 3) : 0;
                Reevaluate();
            }));
            _subs.Add(_signals.On<OverchargeFinisherFired>().Subscribe(_ => _audio.PlaySFX("FinisherStinger")));
            _subs.Add(_signals.On<ObjectiveCompleted>().Subscribe(_ => _audio.PlaySFX("NodeRestoredStinger")));

            Reevaluate(); // set the opening bed
        }

        void OnDestroy()
        {
            foreach (var sub in _subs) sub.Dispose();
            _subs.Clear();
        }

        private void Reevaluate()
        {
            if (_bandTracks == null || _bandTracks.Length == 0) return;
            int heat = Mathf.Max(_momentumBand, _signalBand);
            int band = Mathf.Clamp(heat, 0, _bandTracks.Length - 1);
            if (band == _currentBand) return;
            _currentBand = band;
            _audio.CrossfadeMusic(_bandTracks[band], _crossfadeSeconds);
        }
    }
}
