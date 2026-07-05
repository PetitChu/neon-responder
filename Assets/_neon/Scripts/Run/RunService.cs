using System;
using UnityHFSM;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// UnityHFSM run flow (spec §5.4). Event-driven: intro timer + objective fill run
    /// in each state's onLogic (fed the clock delta via a per-tick field), while
    /// completion / shop-continue / death fire RequestStateChange. The Phase mirror
    /// is set in each state's onEnter and published — the assertable surface.
    /// </summary>
    public sealed class RunService : IRunService, IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 40; // after Momentum (30); run flow sits atop engagement

        private const string INTRO = "EncounterIntro";
        private const string ACTIVE = "EncounterActive";
        private const string COMPLETE = "EncounterComplete";
        private const string SHOP = "Shop";
        private const string BOSS = "BossStub";
        private const string WON = "RunWon";
        private const string LOST = "RunLost";

        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly IStatSystem _stats;
        private readonly IEntitiesService _entities;
        private readonly ISignalSystem _signal;
        private readonly RunConfig _config;
        private readonly ModifierSource _shopPauseSource = ModifierSource.Create("run-shop-pause");

        private StateMachine _fsm;
        private Action<int> _triggerWave;
        private RebootNodeObjective _objective;
        private float _phaseElapsed;
        private float _tickDelta;
        private bool _started;

        public RunPhase Phase { get; private set; } = RunPhase.None;
        public int EncounterIndex { get; private set; }

        public RunService(IGameplayClock clock, IGameplaySignals signals, IStatSystem stats,
            IEntitiesService entities, ISignalSystem signal, RunConfig config)
        {
            _clock = clock;
            _signals = signals;
            _stats = stats;
            _entities = entities;
            _signal = signal;
            _config = config;

            HealthSystem.onUnitDeath += HandleUnitDeath;
            _clock.Register(this, TICK_ORDER);
            BuildFsm();
        }

        public void Dispose()
        {
            HealthSystem.onUnitDeath -= HandleUnitDeath;
            _clock.Unregister(this);
            _clock.ClearScale(_shopPauseSource);
        }

        public void BeginRun(Action<int> triggerWave)
        {
            _triggerWave = triggerWave;
            _started = true;
            _fsm.Init(); // enters INTRO
        }

        public void ContinueFromShop()
        {
            if (Phase != RunPhase.Shop) return;
            _clock.ClearScale(_shopPauseSource);
            EncounterIndex++;
            _fsm.RequestStateChange(INTRO);
        }

        public void Tick(float deltaTime)
        {
            if (!_started) return;
            _tickDelta = deltaTime;
            _fsm.OnLogic();
        }

        /// <summary>Public so EditMode tests can drive it (static event can't be raised externally).</summary>
        public void HandleUnitDeath(GameObject unit)
        {
            if (!_started || unit == null || !unit.CompareTag("Player")) return;
            if (Phase == RunPhase.RunWon || Phase == RunPhase.RunLost) return;
            _fsm.RequestStateChange(LOST, forceInstantly: true);
        }

        private void BuildFsm()
        {
            _fsm = new StateMachine();

            _fsm.AddState(INTRO, new State(
                onEnter: _ => { SetPhase(RunPhase.EncounterIntro); _phaseElapsed = 0f; },
                onLogic: _ =>
                {
                    _phaseElapsed += _tickDelta;
                    if (_phaseElapsed >= _config.EncounterIntroSeconds) _fsm.RequestStateChange(ACTIVE);
                }));

            _fsm.AddState(ACTIVE, new State(
                onEnter: _ =>
                {
                    SetPhase(RunPhase.EncounterActive);
                    _triggerWave?.Invoke(EncounterIndex);
                    var node = NodeForCurrentEncounter();
                    _objective = new RebootNodeObjective(_stats, node, _config.NodeRadius, _config.RebootDurationSeconds);
                },
                onLogic: _ =>
                {
                    if (_objective == null) return;
                    Vector2 playerPos = PlayerPosition();
                    bool done = _objective.Tick(_tickDelta, playerPos);
                    _signals.Publish(new ObjectiveProgress(_objective.Normalized, _objective.Position, _objective.PlayerInZone));
                    if (done) _fsm.RequestStateChange(COMPLETE);
                }));

            _fsm.AddState(COMPLETE, new State(
                onEnter: _ =>
                {
                    SetPhase(RunPhase.EncounterComplete);
                    _signals.Publish(new ObjectiveCompleted(EncounterIndex));
                    _signal.Raise(_config.SignalPerObjective);
                    _fsm.RequestStateChange(_signal.IsDawn ? BOSS : SHOP);
                }));

            _fsm.AddState(SHOP, new State(
                onEnter: _ =>
                {
                    SetPhase(RunPhase.Shop);
                    _clock.SetScale(_shopPauseSource, _config.ShopPauseScale);
                }));

            _fsm.AddState(BOSS, new State(
                onEnter: _ =>
                {
                    SetPhase(RunPhase.BossStub);
                    Debug.Log("[Run] Boss stub — skipping to dawn (MVP).");
                    _fsm.RequestStateChange(WON);
                }));

            _fsm.AddState(WON, new State(onEnter: _ =>
            {
                SetPhase(RunPhase.RunWon);
                _signals.Publish(new RunEnded(true));
            }));

            _fsm.AddState(LOST, new State(onEnter: _ =>
            {
                SetPhase(RunPhase.RunLost);
                _clock.ClearScale(_shopPauseSource); // in case we died looking at the shop
                _signals.Publish(new RunEnded(false));
            }));

            _fsm.SetStartState(INTRO);
        }

        private Vector2 NodeForCurrentEncounter()
        {
            var positions = _config.NodePositions;
            if (positions == null || positions.Length == 0) return Vector2.zero;
            return positions[Mathf.Clamp(EncounterIndex, 0, positions.Length - 1)];
        }

        private Vector2 PlayerPosition()
        {
            var player = _entities.GetFirstByType(UNITTYPE.PLAYER).GameObject;
            return player != null ? (Vector2)player.transform.position : Vector2.zero;
        }

        private void SetPhase(RunPhase phase)
        {
            var previous = Phase;
            Phase = phase;
            _signals.Publish(new RunPhaseChanged(previous, phase, EncounterIndex, _config.EncounterCount));
        }
    }
}
