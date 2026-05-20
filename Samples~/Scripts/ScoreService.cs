namespace TMV.Service.Samples
{
    /// <summary>In-memory score service. Accumulates points for the current session.</summary>
    public class ScoreService : IScoreService
    {
        #region Properties

        /// <inheritdoc/>
        public int Score { get; private set; }

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public void Add(int points) => Score += points;

        #endregion
    }
}
