using UnityEngine;

namespace TMV.Service
{
    /// <summary>
    /// Internal debug logger. Active only in Editor and Development builds.
    /// All messages are prefixed with [TMV.Service] for easy console filtering.
    /// </summary>
    internal static class ServiceLog
    {
        #region Fields

        private const string Tag = "[TMV.Service]";

        #endregion

        #region Internal Methods

        internal static void Info(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"{Tag} {message}");
#endif
        }

        internal static void Warning(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"{Tag} {message}");
#endif
        }

        internal static void Error(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError($"{Tag} {message}");
#endif
        }

        #endregion
    }
}
