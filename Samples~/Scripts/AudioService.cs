namespace TMV.Service.Samples
{
    /// <summary>
    /// Stub audio service. Implements IService so the container auto-disposes it on Clear.
    /// </summary>
    public class AudioService : IAudioService, IService
    {
        #region Properties

        public string LastPlayed { get; private set; }

        #endregion

        #region Public Methods

        public void Play(string clipName) => LastPlayed = clipName;

        /// <summary>Release audio resources — called automatically by the container.</summary>
        public void Dispose() { }

        #endregion
    }
}
