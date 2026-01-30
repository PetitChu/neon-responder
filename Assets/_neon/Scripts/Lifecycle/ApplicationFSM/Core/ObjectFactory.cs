using VContainer;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Factory for resolving objects from the DI container.
    /// </summary>
    public class ObjectFactory
    {
        private readonly IObjectResolver _resolver;

        public ObjectFactory(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public T Resolve<T>() => _resolver.Resolve<T>();
    }
}
