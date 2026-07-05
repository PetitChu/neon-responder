namespace BrainlessLabs.Neon
{
    /// <summary>The eight GDD families (protocol doc §1).</summary>
    public enum ProtocolFamily
    {
        AutoGear = 0,
        Momentum = 1,
        Execution = 2,
        Brawler = 3,
        Scavenger = 4,
        Specials = 5,
        Defense = 6,
        Objective = 7
    }

    /// <summary>Rarity drives draft weight (protocol doc §2/§8.2).</summary>
    public enum ProtocolRarity
    {
        Stock = 0,
        Tuned = 1,
        Prototype = 2,
        Blacksite = 3
    }

    /// <summary>Which M0 stat sheet a protocol modifier lands on.</summary>
    public enum StatSheetTarget
    {
        Player = 0,
        Run = 1
    }
}
