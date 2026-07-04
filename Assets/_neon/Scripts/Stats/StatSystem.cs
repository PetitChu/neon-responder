namespace BrainlessLabs.Neon
{
    public sealed class StatSystem : IStatSystem
    {
        public StatSheet Player { get; } = new();
        public StatSheet Run { get; } = new();
    }
}
