using UnityEngine;

namespace SphereOne
{
    public class SphereOneLogger
    {
        public static string _prefix = "SphereOneSDK: ";

        readonly bool _enableLogging;

        public SphereOneLogger(bool enableLogging)
        {
            _enableLogging = enableLogging;
        }

        public void Log(string message)
        {
            if (_enableLogging)
            {
                Debug.Log(_prefix + message);
            }
        }

        public void LogWarning(string message)
        {
            Debug.LogWarning(_prefix + message);
        }

        public void LogError(string message)
        {
            Debug.LogError(_prefix + message);
        }
    }
}