#if UNITY_IOS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.iOS.Xcode;
using UnityEditor.Callbacks;
using UnityEditor;
using System.IO;

public class BuildPostscript : MonoBehaviour {

    [PostProcessBuildAttribute(1)]
    public static void OnPostprocessBuild(BuildTarget buildTarget, string path) 
    {
        if (buildTarget == BuildTarget.iOS )
        {
            string projPath = PBXProject.GetPBXProjectPath(path);
            PBXProject proj = new PBXProject();

            proj.ReadFromString(File.ReadAllText(projPath));

            // Get plist
            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            // Get root
            PlistElementDict rootDict = plist.root;

            // Create URL types 
            string identifier = "com.UnitySafariViewController";
            string scheme = "UnitySafariViewControllerScheme";

            PlistElementArray urlTypesArray = rootDict.CreateArray("CFBundleURLTypes");
            PlistElementDict dict = urlTypesArray.AddDict();
            dict.SetString("CFBundleURLName", identifier);
            PlistElementArray schemesArray = dict.CreateArray("CFBundleURLSchemes");
            schemesArray.AddString(scheme);
            File.WriteAllText(plistPath, plist.WriteToString());

            File.WriteAllText(projPath, proj.WriteToString());

        }
    }
}
#endif