namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Run flow phases (spec §5.4). Mirrors the UnityHFSM state names in RunService
    /// so the flow is assertable in EditMode without reaching into the FSM internals.
    /// </summary>
    public enum RunPhase
    {
        None = 0,
        EncounterIntro = 1,
        EncounterActive = 2,
        EncounterComplete = 3,
        Shop = 4,
        BossStub = 5,
        RunWon = 6,
        RunLost = 7
    }
}
