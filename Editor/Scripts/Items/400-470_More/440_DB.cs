using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/More/Legacy/Dynamic Bone", false, 440)]
        private static void importDB()
        {
            Selection.activeGameObject = null;
            if (!(Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Unity/Asset Store-5.x/Will Hong"))
                || !(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Unity/Asset Store-5.x/Will Hong", "*.unitypackage", SearchOption.AllDirectories).Length > 0))
            {
                int selection = EditorUtility.DisplayDialogComplex(
                    "Missing Paid Asset!",
                    "Dynamic Bone was not found to have been downloaded previously.\n\nPlease either browse for Dynamic Bone or purchase and download the latest version using the Unity Asset Store.",
                    "Purchase",
                    "Close",
                    "Browse"
                );
                switch (selection)
                {
                    case 0:
                        Application.OpenURL("https://assetstore.unity.com/packages/tools/animation/dynamic-bone-16743");
                        return;
                    case 1:
                        return;
                    case 2:
                        string path = EditorUtility.OpenFilePanel("Browse for Dynamic Bone", "", "unitypackage");
                        if (path != null && path != String.Empty && path != "" && File.Exists(path))
                        {
                            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Unity/Asset Store-5.x/Will Hong/ScriptingAnimation";
                            string name = "Dynamic Bone.unitypackage";
                            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                            string copyTo = dir + @"/" + name;
                            File.Copy(path, copyTo, true);
                        }
                        else return;
                        break;
                }
            }
            string downloadURL = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Unity/Asset Store-5.x/Will Hong", @"*.unitypackage", SearchOption.AllDirectories)[0];
            ImportAsset(
                pathOrURL: downloadURL,
                assets: new string[] { "DynamicBone" },
                official: true);
        }
    }
}
