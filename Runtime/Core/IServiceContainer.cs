using System;

namespace TMV.Service
{
    /// <summary>
    /// Defines a service registry that maps interface types to implementations,
    /// supporting singleton and transient lifetimes.
    /// </summary>
    public interface IServiceContainer : IDisposable
    {
        /// <summary>
        /// Register a pre-built singleton instance.
        /// Throws if <typeparamref name="T"/> is already registered.
        /// </summary>
        void Add<T>(T service) where T : class;

        /// <summary>
        /// Register a factory with the given lifetime.
        /// Throws if <typeparamref name="T"/> is already registered.
        /// </summary>
        void Add<T>(
            Func<IServiceContainer, T> factory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class;

        /// <summary>
        /// Swap an existing registration with a new instance, disposing the old one
        /// if it implements <see cref="IService"/>.
        /// Throws if <typeparamref name="T"/> is not registered.
        /// </summary>
        void Replace<T>(T service) where T : class;

        /// <summary>
        /// Swap an existing factory registration, disposing the old cached instance
        /// if it implements <see cref="IService"/>.
        /// Throws if <typeparamref name="T"/> is not registered.
        /// </summary>
        void Replace<T>(
            Func<IServiceContainer, T> factory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class;

        /// <summary>
        /// Remove and dispose the service registered as <typeparamref name="T"/>.
        /// Throws if not registered.
        /// </summary>
        void Remove<T>() where T : class;

        /// <summary>Remove and dispose all registered services.</summary>
        void Clear();

        /// <summary>
        /// Resolve <typeparamref name="T"/>.
        /// Throws <see cref="InvalidOperationException"/> if not registered.
        /// </summary>
        T Get<T>() where T : class;

        /// <summary>
        /// Try to resolve <typeparamref name="T"/>.
        /// Returns false without throwing if not registered.
        /// </summary>
        bool TryGet<T>(out T service) where T : class;

        /// <summary>
        /// Returns true if <typeparamref name="T"/> is registered,
        /// regardless of whether its instance has been created yet.
        /// </summary>
        bool IsExists<T>() where T : class;
    }
}
