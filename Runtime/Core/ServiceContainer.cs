using System;
using System.Collections.Generic;
using System.Threading;

namespace TMV.Service
{
    /// <summary>
    /// Concrete service container. Thread-safe. No static state.
    /// Create one per scope (app session, level, test case).
    /// </summary>
    public sealed class ServiceContainer : IServiceContainer
    {
        #region Fields

        private readonly Dictionary<Type, ServiceDescriptor> _descriptors = new();

        // ReaderWriterLockSlim allows concurrent reads without blocking each other.
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

        private bool _disposed;

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public void Add<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            WriteDescriptor(
                typeof(T),
                new ServiceDescriptor(_ => service, ServiceLifetime.Singleton, service),
                allowOverwrite: false);
        }

        /// <inheritdoc/>
        public void Add<T>(
            Func<IServiceContainer, T> factory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            WriteDescriptor(
                typeof(T),
                new ServiceDescriptor(c => factory(c), lifetime),
                allowOverwrite: false);
        }

        /// <inheritdoc/>
        public void Replace<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            WriteDescriptor(
                typeof(T),
                new ServiceDescriptor(_ => service, ServiceLifetime.Singleton, service),
                allowOverwrite: true);
        }

        /// <inheritdoc/>
        public void Replace<T>(
            Func<IServiceContainer, T> factory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            WriteDescriptor(
                typeof(T),
                new ServiceDescriptor(c => factory(c), lifetime),
                allowOverwrite: true);
        }

        /// <inheritdoc/>
        public void Remove<T>() where T : class
        {
            _lock.EnterWriteLock();
            try
            {
                var type = typeof(T);
                if (!_descriptors.TryGetValue(type, out var descriptor))
                {
                    ServiceLog.Warning($"ServiceContainer.Remove<{type.Name}>: not registered");
                    throw new InvalidOperationException($"{type.Name} is not added.");
                }
                DisposeDescriptor(descriptor);
                _descriptors.Remove(type);
            }
            finally { _lock.ExitWriteLock(); }
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                foreach (var descriptor in _descriptors.Values)
                    DisposeDescriptor(descriptor);
                _descriptors.Clear();
            }
            finally { _lock.ExitWriteLock(); }
        }

        /// <inheritdoc/>
        public T Get<T>() where T : class
        {
            if (!TryGet<T>(out var service))
            {
                ServiceLog.Warning($"ServiceContainer.Get<{typeof(T).Name}>: not registered — did you call Add?");
                throw new InvalidOperationException($"{typeof(T).Name} is not added.");
            }
            return service;
        }

        /// <inheritdoc/>
        public bool TryGet<T>(out T service) where T : class
        {
            var type = typeof(T);

            // Fast path: singleton already cached — read lock only, does not block concurrent reads.
            _lock.EnterReadLock();
            try
            {
                if (_descriptors.TryGetValue(type, out var d) &&
                    d.Lifetime == ServiceLifetime.Singleton && d.CachedInstance != null)
                {
                    service = (T)d.CachedInstance;
                    return true;
                }
            }
            finally { _lock.ExitReadLock(); }

            return TryGetSlowPath<T>(type, out service);
        }

        /// <inheritdoc/>
        public bool IsExists<T>() where T : class
        {
            _lock.EnterReadLock();
            try { return _descriptors.ContainsKey(typeof(T)); }
            finally { _lock.ExitReadLock(); }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Clear();
            _lock.Dispose();
        }

        #endregion

        #region Private/Protected Methods

        private void WriteDescriptor(Type type, ServiceDescriptor descriptor, bool allowOverwrite)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_descriptors.TryGetValue(type, out var existing))
                {
                    if (!allowOverwrite)
                    {
                        ServiceLog.Warning($"ServiceContainer.Add<{type.Name}>: already registered — use Replace instead");
                        throw new InvalidOperationException($"{type.Name} is already added. Use Replace.");
                    }
                    DisposeDescriptor(existing);
                }
                else if (allowOverwrite)
                {
                    ServiceLog.Warning($"ServiceContainer.Replace<{type.Name}>: not registered — use Add first");
                    throw new InvalidOperationException($"{type.Name} is not added. Use Add.");
                }
                _descriptors[type] = descriptor;
            }
            finally { _lock.ExitWriteLock(); }
        }

        private bool TryGetSlowPath<T>(Type type, out T service) where T : class
        {
            ServiceDescriptor descriptor;

            _lock.EnterWriteLock();
            try
            {
                if (!_descriptors.TryGetValue(type, out descriptor))
                {
                    service = null;
                    return false;
                }

                // Double-check after acquiring write lock.
                if (descriptor.Lifetime == ServiceLifetime.Singleton && descriptor.CachedInstance != null)
                {
                    service = (T)descriptor.CachedInstance;
                    return true;
                }
            }
            finally { _lock.ExitWriteLock(); }

            // Factory is invoked outside the lock — prevents LockRecursionException when the
            // factory resolves other services from this same container.
            var instance = (T)descriptor.Factory(this);

            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                _lock.EnterWriteLock();
                try
                {
                    // A concurrent get may have already cached an instance; discard ours.
                    if (descriptor.CachedInstance == null)
                        descriptor.CachedInstance = instance;
                    else
                    {
                        if (instance is IService s) s.Dispose();
                        instance = (T)descriptor.CachedInstance;
                    }
                }
                finally { _lock.ExitWriteLock(); }
            }

            service = instance;
            return true;
        }

        private static void DisposeDescriptor(ServiceDescriptor descriptor)
        {
            if (descriptor.CachedInstance is IService s)
                s.Dispose();
        }

        #endregion
    }
}
