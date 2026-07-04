namespace BrainlessLabs.Neon
{
    /// <summary>
    /// A system that participates in the ordered gameplay tick.
    /// Receives the gameplay-scaled delta time.
    /// </summary>
    public interface IGameplayTickable
    {
        void Tick(float deltaTime);
    }
}
