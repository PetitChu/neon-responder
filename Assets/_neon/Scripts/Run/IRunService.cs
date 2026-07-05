using System;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The run flow (spec §5.4): sequences encounter phases in the one belt arena.
    /// Lives in the Level scope; built on UnityHFSM but distinct from the boot FSM.
    /// </summary>
    public interface IRunService
    {
        RunPhase Phase { get; }
        int EncounterIndex { get; }

        /// <summary>Start the run. <paramref name="triggerWave"/> = SpawnerService.TriggerWave (per-encounter hero wave).</summary>
        void BeginRun(Action<int> triggerWave);

        /// <summary>Leave the shop → next encounter (called by the shop UI's Continue).</summary>
        void ContinueFromShop();
    }
}
