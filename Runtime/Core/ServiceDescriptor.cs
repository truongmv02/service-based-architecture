using System;

namespace TMV.Service
{
    // Internal record — not exposed outside the assembly.
    internal sealed class ServiceDescriptor
    {
        /// <summary>Whether this registration produces one shared instance or a new one per resolve.</summary>
        internal ServiceLifetime Lifetime { get; }

        /// <summary>Cached singleton instance; null until first resolve (or until seeded via constructor).</summary>
        internal object CachedInstance { get; set; }

        /// <summary>Delegate that constructs a new instance on demand.</summary>
        internal Func<IServiceContainer, object> Factory { get; }

        internal ServiceDescriptor(
            Func<IServiceContainer, object> factory,
            ServiceLifetime lifetime,
            object seed = null)
        {
            Factory = factory;
            Lifetime = lifetime;
            CachedInstance = seed;
        }
    }
}
