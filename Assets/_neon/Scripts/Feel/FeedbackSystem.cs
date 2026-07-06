using System;
using System.Collections;
using R3;
using UnityEngine;
using VContainer;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Spec §5.5 feedback: a pure CONSUMER of the combat seams + signals. Applies
    /// per-verb hitstop (clock scale dip) + CameraShake, a finish beat, the tier-up
    /// flourish, and the whiff record-scratch + fullscreen desaturate (WhiffPostFx).
    /// Scene MonoBehaviour on UNSCALED time (hitstop release can't use the frozen
    /// gameplay clock). Injected clock is used only to set/clear scale sources.
    /// </summary>
    public class FeedbackSystem : MonoBehaviour
    {
        [Inject] private IGameplaySignals _signals;
        [Inject] private IGameplayClock _clock;
        [Inject] private IAudioService _audio;

        private FeelConfig _config;
        private CameraShake _cameraShake;
        private WhiffPostFx _whiffPostFx;
        private readonly ModifierSource _hitstopSource = ModifierSource.Create("hitstop");
        private Coroutine _hitstopRoutine;
        private IDisposable _finishSub;
        private IDisposable _tierSub;

        void Start()
        {
            if (_signals == null || _clock == null) { enabled = false; return; } // scene w/o DI
            _config = FeelConfig.FromSettings();
            _cameraShake = Camera.main != null ? Camera.main.GetComponent<CameraShake>() : null;
            _whiffPostFx = UnityEngine.Object.FindFirstObjectByType<WhiffPostFx>();

            _finishSub = _signals.On<EnemyFinished>().Subscribe(f => Play(_config.Finish, f.Position));
            _tierSub = _signals.On<MomentumTierChanged>().Subscribe(OnTier);
        }

        void OnEnable()
        {
            UnitActions.onUnitDealDamage += OnUnitDealDamage;
            UnitActions.onVerbWhiffed += OnVerbWhiffed;
        }

        void OnDisable()
        {
            UnitActions.onUnitDealDamage -= OnUnitDealDamage;
            UnitActions.onVerbWhiffed -= OnVerbWhiffed;
        }

        void OnDestroy()
        {
            _finishSub?.Dispose();
            _tierSub?.Dispose();
            _clock?.ClearScale(_hitstopSource);
        }

        private void OnUnitDealDamage(GameObject recipient, AttackData attackData)
        {
            if (attackData?.inflictor == null || !attackData.inflictor.CompareTag("Player")) return;
            var pos = recipient != null ? (Vector2)recipient.transform.position : Vector2.zero;
            Play(_config.ProfileForVerb(attackData.attackType), pos);
        }

        private void OnTier(MomentumTierChanged e)
        {
            if ((int)e.Current <= (int)e.Previous) return; // flourish only on tier UP
            Play(_config.TierUp, PlayerPos());
            _audio?.PlaySFX("MomentumTierUp", PlayerPos());
        }

        private void OnVerbWhiffed(UnitActions unit, ATTACKTYPE attackType)
        {
            if (unit == null || !unit.isPlayer) return;
            _audio?.PlaySFX("Whiff", PlayerPos()); // record-scratch
            _whiffPostFx?.Pulse(_config.WhiffFlashSeconds);
        }

        private void Play(HitProfile profile, Vector2 position)
        {
            if (_cameraShake != null && profile.ShakeIntensity > 0f)
                _cameraShake.ShowCamShake(profile.ShakeIntensity, profile.ShakeSeconds);

            if (profile.HitstopSeconds > 0f && profile.HitstopScale < 1f)
            {
                if (_hitstopRoutine != null) StopCoroutine(_hitstopRoutine);
                _hitstopRoutine = StartCoroutine(HitstopRoutine(profile.HitstopScale, profile.HitstopSeconds));
            }
        }

        private IEnumerator HitstopRoutine(float scale, float seconds)
        {
            _clock.SetScale(_hitstopSource, scale);
            yield return new WaitForSecondsRealtime(seconds); // UNSCALED — scaled time is dilated during hitstop
            _clock.ClearScale(_hitstopSource);
            _hitstopRoutine = null;
        }

        private Vector2 PlayerPos()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? (Vector2)player.transform.position : Vector2.zero;
        }
    }
}
