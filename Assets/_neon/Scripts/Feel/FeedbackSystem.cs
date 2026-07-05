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
    /// flourish, and the whiff record-scratch + red flash. Scene MonoBehaviour on
    /// UNSCALED time (hitstop release can't use the frozen gameplay clock).
    /// Injected clock is used only to set/clear scale sources — timing is unscaled.
    /// </summary>
    public class FeedbackSystem : MonoBehaviour
    {
        [SerializeField] private CanvasGroup whiffFlash; // full-screen red vignette (uGUI), alpha 0 at rest

        [Inject] private IGameplaySignals _signals;
        [Inject] private IGameplayClock _clock;
        [Inject] private IAudioService _audio;

        private FeelConfig _config;
        private CameraShake _cameraShake;
        private readonly ModifierSource _hitstopSource = ModifierSource.Create("hitstop");
        private Coroutine _hitstopRoutine;
        private Coroutine _whiffRoutine;
        private IDisposable _finishSub;
        private IDisposable _tierSub;

        void Start()
        {
            if (_signals == null || _clock == null) { enabled = false; return; } // scene w/o DI
            _config = FeelConfig.FromSettings();
            _cameraShake = Camera.main != null ? Camera.main.GetComponent<CameraShake>() : null;
            if (whiffFlash != null) whiffFlash.alpha = 0f;

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
            if (_whiffRoutine != null) StopCoroutine(_whiffRoutine);
            _whiffRoutine = StartCoroutine(WhiffFlashRoutine());
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

        private IEnumerator WhiffFlashRoutine()
        {
            if (whiffFlash == null) yield break;
            float t = 0f;
            while (t < _config.WhiffFlashSeconds)
            {
                whiffFlash.alpha = Mathf.Lerp(0.6f, 0f, t / _config.WhiffFlashSeconds);
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            whiffFlash.alpha = 0f;
            _whiffRoutine = null;
        }

        private Vector2 PlayerPos()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? (Vector2)player.transform.position : Vector2.zero;
        }
    }
}
