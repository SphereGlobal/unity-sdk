using TMPro;
using UnityEngine;

namespace SphereOne
{
    public class SphereOneLogger
    {
        [SerializeField] TMP_Text _debug;

        public static string _prefix = "SphereOneSDK: ";

        readonly bool _enableLogging;

        public SphereOneLogger(bool enableLogging, TMP_Text debugText)
        {
            _enableLogging = enableLogging;
            _debug = debugText;
        }

        public void Log(string message)
        {
            if (_enableLogging)
            {
                WriteToScreen(message);
                Debug.Log(_prefix + message);
            }
        }

        public void LogWarning(string message)
        {
            WriteToScreen(message);
            Debug.LogWarning(_prefix + message);
        }

        public void LogError(string message)
        {
            WriteToScreen(message);
            Debug.LogError(_prefix + message);
        }

        void WriteToScreen(string message)
        {
            if (_debug == null) return;

            _debug.text = message;
        }
    }
}