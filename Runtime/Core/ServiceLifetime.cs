namespace TMV.Service
{
    /// <summary>
    /// Controls how many instances the container creates for a registered service.
    /// </summary>
    public enum ServiceLifetime
    {
        /// One instance per container — created on first Resolve, reused after.
        Singleton,

        /// New instance on every Resolve call.
        Transient
    }
}
