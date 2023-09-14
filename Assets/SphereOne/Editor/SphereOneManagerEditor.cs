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
        SerializedProperty _sphereOneApiUrl;
        SerializedProperty _apiKey;
        SerializedProperty _enableLogging;

        // Slideout
        SerializedProperty _backgroundFilter;

        // Popup
        SerializedProperty _clientId;
        SerializedProperty _redirectUrl;

        bool _popupSettingsGroup = true;
        bool _slideoutSettingsGroup = true;
        #endregion

        void OnEnable()
        {
            _loginMode = serializedObject.FindProperty("_loginMode");
            _environment = serializedObject.FindProperty("_environment");
            _sphereOneApiUrl = serializedObject.FindProperty("_sphereOneApiUrl");
            _apiKey = serializedObject.FindProperty("_apiKey");
            _enableLogging = serializedObject.FindProperty("_enableLogging");

            _backgroundFilter = serializedObject.FindProperty("_backgroundFilter");

            _clientId = serializedObject.FindProperty("_clientId");
            _redirectUrl = serializedObject.FindProperty("_redirectUrl");
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
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            EditorGUILayout.LabelField("MacOS Build");
#endif

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_environment);
            EditorGUILayout.PropertyField(_sphereOneApiUrl);
            _sphereOneApiUrl.stringValue = _sphereOneApiUrl.stringValue.TrimEnd('/').Trim();
            EditorGUILayout.PropertyField(_apiKey);
            _apiKey.stringValue = _apiKey.stringValue.Trim();
            EditorGUILayout.PropertyField(_enableLogging);

            EditorGUILayout.Space(8);

            if (_manager.LoginMode == LoginBehavior.POPUP)
            {
                _popupSettingsGroup = EditorGUILayout.BeginFoldoutHeaderGroup(_popupSettingsGroup, "Popup Auth Setup");
                if (_popupSettingsGroup)
                {
#if UNITY_WEBGL
                    EditorGUILayout.PropertyField(_redirectUrl);
                    _redirectUrl.stringValue = _redirectUrl.stringValue.TrimEnd('/').Trim();
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