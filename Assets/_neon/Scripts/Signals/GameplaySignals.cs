using System;
using System.Collections.Generic;
using R3;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// R3-backed implementation: lazily creates one Subject per signal type.
    /// Registered as a singleton; VContainer disposes it with its scope.
    /// </summary>
    public sealed class GameplaySignals : IGameplaySignals, IDisposable
    {
        private readonly Dictionary<Type, object> _subjects = new();

        public void Publish<T>(T signal) where T : struct
        {
            GetSubject<T>().OnNext(signal);
        }

        public Observable<T> On<T>() where T : struct
        {
            return GetSubject<T>();
        }

        public void Dispose()
        {
            foreach (var subject in _subjects.Values)
            {
                ((IDisposable)subject).Dispose();
            }
            _subjects.Clear();
        }

        private Subject<T> GetSubject<T>() where T : struct
        {
            if (_subjects.TryGetValue(typeof(T), out var existing))
            {
                return (Subject<T>)existing;
            }

            var subject = new Subject<T>();
            _subjects[typeof(T)] = subject;
            return subject;
        }
    }
}
