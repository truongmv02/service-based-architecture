namespace TMV.Service.Samples
{
    public interface IAudioService
    {
        /// <summary>Play a named audio clip.</summary>
        void Play(string clipName);

        /// <summary>Name of the last clip that was played, null if none.</summary>
        string LastPlayed { get; }
    }
}
