namespace TMV.Service
{
    /// <summary>
    /// Opt-in marker. Services that implement IService are automatically
    /// disposed when removed or when the container is cleared.
    /// </summary>
    public interface IService : System.IDisposable { }
}
