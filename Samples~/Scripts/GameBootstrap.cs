using UnityEngine;

namespace TMV.Service.Samples
{
    /// <summary>
    /// Registers sample services on Awake and clears the container on destroy.
    /// Attach to the first GameObject in the scene.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        #region Unity Callbacks

        private void Awake()
        {
            ServiceLocator.Add<IAudioService>(new AudioService());
            ServiceLocator.Add<IScoreService>(new ScoreService());
        }

        private void OnDestroy() => ServiceLocator.Clear();

        #endregion
    }
}
