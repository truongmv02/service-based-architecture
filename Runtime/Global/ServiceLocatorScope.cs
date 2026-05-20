using System;

namespace TMV.Service
{
    /// <summary>
    /// Wraps <see cref="ServiceContainer"/> so <see cref="ServiceLocator"/> can swap
    /// the underlying container for testing without changing the public API surface.
    /// </summary>
    internal sealed class ServiceLocatorScope : IServiceContainer
    {
        #region Fields

        private IServiceContainer _container = new ServiceContainer();

        #endregion

        #region Public Methods

        /// <summary>Dispose the current container and replace it with a fresh one.</summary>
        internal void Reset()
        {
            _container.Dispose();
            _container = new ServiceContainer();
        }

        /// <inheritdoc/>
        public void Add<T>(T service) where T : class
            => _container.Add(service);

        /// <inheritdoc/>
        public void Add<T>(Func<IServiceContainer, T> factory, ServiceLifetime lifetime) where T : class
            => _container.Add(factory, lifetime);

        /// <inheritdoc/>
        public void Replace<T>(T service) where T : class
            => _container.Replace(service);

        /// <inheritdoc/>
        public void Replace<T>(Func<IServiceContainer, T> factory, ServiceLifetime lifetime) where T : class
            => _container.Replace(factory, lifetime);

        /// <inheritdoc/>
        public void Remove<T>() where T : class => _container.Remove<T>();

        /// <inheritdoc/>
        public void Clear() => _container.Clear();

        /// <inheritdoc/>
        public T Get<T>() where T : class => _container.Get<T>();

        /// <inheritdoc/>
        public bool TryGet<T>(out T service) where T : class
            => _container.TryGet(out service);

        /// <inheritdoc/>
        public bool IsExists<T>() where T : class => _container.IsExists<T>();

        /// <inheritdoc/>
        public void Dispose() => _container.Dispose();

        #endregion
    }
}
