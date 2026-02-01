using VContainer;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Centralized access to application services resolved through VContainer DI.
    /// Populated during GameServicesState initialization.
    /// Consumers use Services.Audio, Services.Input, etc. through interfaces.
    /// </summary>
    public static class Services
    {
        public static IInputService Input { get; private set; }
        public static IAudioService Audio { get; private set; }
        public static IScenesService Scenes { get; private set; }
        public static IEntitiesService Entities { get; private set; }

        public static void Initialize(IObjectResolver container)
        {
            Input = container.Resolve<IInputService>();
            Audio = container.Resolve<IAudioService>();
            Scenes = container.Resolve<IScenesService>();
            Entities = container.Resolve<IEntitiesService>();
        }

        public static void Dispose()
        {
            Input = null;
            Audio = null;
            Scenes = null;
            Entities = null;
        }
    }
}
