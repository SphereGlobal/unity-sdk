using UnityEditor;
using UnityEngine;

namespace SphereOne
{
    [CustomEditor(typeof(SphereOneManager))]
    public class SphereOneManagerEditor : Editor
    {
        #region SerializedProperties
        SerializedProperty _loginMode;
        SerializedProperty _environment;
        SerializedProperty _apiKey;
        SerializedProperty _enableLogging;
        SerializedProperty _debugText;

        // Slideout
        SerializedProperty _backgroundFilter;

        // Popup
        SerializedProperty _clientId;
        SerializedProperty _redirectUrl;
        SerializedProperty _scheme;
        #endregion

        bool _popupSettingsGroup = true;
        bool _slideoutSettingsGroup = true;

        void OnEnable()
        {
            _loginMode = serializedObject.FindProperty("_loginMode");
            _environment = serializedObject.FindProperty("_environment");
            _apiKey = serializedObject.FindProperty("_apiKey");
            _enableLogging = serializedObject.FindProperty("_enableLogging");
            _debugText = serializedObject.FindProperty("_debugText");

            _backgroundFilter = serializedObject.FindProperty("_backgroundFilter");

            _clientId = serializedObject.FindProperty("_clientId");
            _redirectUrl = serializedObject.FindProperty("_redirectUrl");
            _scheme = serializedObject.FindProperty("_scheme");
        }

        public override void OnInspectorGUI()
        {
            SphereOneManager _manager = (SphereOneManager)target;

            serializedObject.Update();

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("SphereOneSDK", EditorStyles.boldLabel);

#if UNITY_WEBGL
            EditorGUILayout.LabelField("WebGL Build");
            EditorGUILayout.PropertyField(_loginMode);
#elif UNITY_IOS
            EditorGUILayout.LabelField("iOS Build");
#elif UNITY_ANDROID
            EditorGUILayout.LabelField("Android Build");
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            EditorGUILayout.LabelField("MacOS Standalone Build");
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            EditorGUILayout.LabelField("Windows Standalone Build");
#else
            EditorGUILayout.LabelField("Platform not supported");
#endif

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_environment);
            // EditorGUILayout.PropertyField(_sphereOneApiUrl);
            // _sphereOneApiUrl.stringValue = _sphereOneApiUrl.stringValue.TrimEnd('/').Trim();
            EditorGUILayout.PropertyField(_apiKey);
            _apiKey.stringValue = _apiKey.stringValue.Trim();
            EditorGUILayout.PropertyField(_enableLogging);
            EditorGUILayout.PropertyField(_debugText);

            EditorGUILayout.Space(8);

            if (_manager.LoginMode == LoginBehavior.POPUP)
            {
                _popupSettingsGroup = EditorGUILayout.BeginFoldoutHeaderGroup(_popupSettingsGroup, "Popup Auth Setup");
                if (_popupSettingsGroup)
                {
#if UNITY_WEBGL
                    EditorGUILayout.PropertyField(_redirectUrl);
                    _redirectUrl.stringValue = _redirectUrl.stringValue.TrimEnd('/').Trim();
#elif UNITY_IOS || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_ANDROID
                    EditorGUILayout.PropertyField(_scheme);
                    _scheme.stringValue = _scheme.stringValue.Trim();
                    _scheme.stringValue = SphereOneUtils.ReplaceWhitespace(_scheme.stringValue, "");
                    _scheme.stringValue = SphereOneUtils.RemoveSpecialCharacters(_scheme.stringValue);
#endif

                    EditorGUILayout.PropertyField(_clientId);
                    _clientId.stringValue = _clientId.stringValue.Trim();
                }
            }
            else if (_manager.LoginMode == LoginBehavior.SLIDEOUT)
            {
                _slideoutSettingsGroup = EditorGUILayout.BeginFoldoutHeaderGroup(_slideoutSettingsGroup, "Slideout Auth Setup");
                if (_slideoutSettingsGroup)
                {
                    EditorGUILayout.PropertyField(_backgroundFilter);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("Sphere One/Add SphereOneManager to Scene")]
        static void InstantiateManager()
        {
            if (FindObjectOfType<SphereOneManager>() != null)
            {
                Debug.Log("SphereOneManager already in scene.");
                return;
            }

            Object pPrefab = Resources.Load("SphereOneManager");
            PrefabUtility.InstantiatePrefab(pPrefab);
        }

        [MenuItem("Sphere One/Documentation")]
        static void OpenDocumentation()
        {
            Application.OpenURL("https://docs.sphereone.xyz/docs/getting-started-unitysdk");
        }
    }
}