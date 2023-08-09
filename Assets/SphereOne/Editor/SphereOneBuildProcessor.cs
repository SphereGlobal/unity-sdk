using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SearchService;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SphereOne
{
    class SphereOneBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            var manager = Object.FindObjectsOfType<SphereOneManager>();

            if (manager.Length == 0)
                return;

            if (manager.Length > 1)
                throw new Exception("SphereOneSDK: Only 1 SphereOneManager is allowed.");

            if (manager[0].Environment == Environment.EDITOR)
                throw new Exception("SphereOneSDK: Environment EDITOR cannot be used in production builds. You must switch to PRODUCTION before building.");

            manager[0].ValidateSetup();
        }
    }
}