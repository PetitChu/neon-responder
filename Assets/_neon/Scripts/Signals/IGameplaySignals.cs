using R3;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Typed gameplay event bus. One struct per event; publishers never know
    /// their subscribers. HUD/feedback layers are pure consumers of this bus.
    /// </summary>
    public interface IGameplaySignals
    {
        /// <summary>Publish a signal to all current subscribers of T.</summary>
        void Publish<T>(T signal) where T : struct;

        /// <summary>The observable stream of T signals. Subscribe with R3 operators.</summary>
        Observable<T> On<T>() where T : struct;
    }
}
