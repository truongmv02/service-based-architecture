namespace TMV.Service.Samples
{
    public interface IScoreService
    {
        /// <summary>Current accumulated score.</summary>
        int Score { get; }

        /// <summary>Add points to the current score.</summary>
        void Add(int points);
    }
}
