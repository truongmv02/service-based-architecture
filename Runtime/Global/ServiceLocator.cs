using System;

namespace TMV.Service
{
    /// <summary>
    /// Optional static entry point. Games that prefer explicit DI can skip this entirely.
    /// </summary>
    public static class ServiceLocator
    {
        #region Fields

        private static readonly ServiceLocatorScope _scope = new();

        #endregion

        #region Properties

        /// <summary>Direct access to the underlying container — for scope injection or testing.</summary>
        public static IServiceContainer Container => _scope;

        #endregion

        #region Public Methods

        /// <inheritdoc cref="IServiceContainer.Add{T}(T)"/>
        public static void Add<T>(T service) where T : class
            => _scope.Add(service);

        /// <inheritdoc cref="IServiceContainer.Add{T}(Func{IServiceContainer,T},ServiceLifetime)"/>
        public static void Add<T>(
            Func<IServiceContainer, T> factory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class
            => _scope.Add(factory, lifetime);

        /// <inheritdoc cref="IServiceContainer.Replace{T}(T)"/>
        public static void Replace<T>(T service) where T : class
            => _scope.Replace(service);

        /// <inheritdoc cref="IServiceContainer.Replace{T}(Func{IServiceContainer,T},ServiceLifetime)"/>
        public static void Replace<T>(
            Func<IServiceContainer, T> factory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class
            => _scope.Replace(factory, lifetime);

        /// <inheritdoc cref="IServiceContainer.Remove{T}"/>
        public static void Remove<T>() where T : class => _scope.Remove<T>();

        /// <inheritdoc cref="IServiceContainer.Clear"/>
        public static void Clear() => _scope.Clear();

        /// <inheritdoc cref="IServiceContainer.Get{T}"/>
        public static T Get<T>() where T : class => _scope.Get<T>();

        /// <inheritdoc cref="IServiceContainer.TryGet{T}"/>
        public static bool TryGet<T>(out T service) where T : class
            => _scope.TryGet(out service);

        /// <inheritdoc cref="IServiceContainer.IsExists{T}"/>
        public static bool IsExists<T>() where T : class => _scope.IsExists<T>();

        /// <summary>Reset to a fresh container. Call in [TearDown] when using the static facade in tests.</summary>
        public static void ResetForTesting() => _scope.Reset();

        #endregion
    }
}
