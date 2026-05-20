using UnityEngine;

namespace TMV.Service.Samples
{
    /// <summary>
    /// Immediate-mode GUI that demonstrates Resolve, Replace, and auto-dispose.
    /// No Canvas setup required.
    /// </summary>
    public class SampleRunner : MonoBehaviour
    {
        #region Fields

        private string _log = "Press a button.";

        private readonly GUIStyle _boxStyle = new();

        #endregion

        #region Unity Callbacks

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 440, 320));

            GUILayout.Label("TMV.Service — Sample", GUI.skin.box);
            GUILayout.Space(8);

            if (ServiceLocator.TryGet<IScoreService>(out var score))
                GUILayout.Label($"Score: {score.Score}");

            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                GUILayout.Label($"Last played: {audio.LastPlayed ?? "—"}");

            GUILayout.Space(8);

            if (GUILayout.Button("Add 10 points"))
            {
                ServiceLocator.Get<IScoreService>().Add(10);
                _log = "Added 10 points.";
            }

            if (GUILayout.Button("Play 'sfx_coin'"))
            {
                ServiceLocator.Get<IAudioService>().Play("sfx_coin");
                _log = "Played sfx_coin.";
            }

            if (GUILayout.Button("Replace AudioService (resets LastPlayed)"))
            {
                // Replace disposes the old instance (IService) and swaps in a fresh one.
                ServiceLocator.Replace<IAudioService>(new AudioService());
                _log = "AudioService replaced — LastPlayed reset.";
            }

            GUILayout.Space(8);
            GUILayout.Label($"Log: {_log}", GUI.skin.box);

            GUILayout.EndArea();
        }

        #endregion
    }
}
