using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/Muscle Animation Editor", false, 320)]
        private static void importMAE()
        {
            Selection.activeGameObject = null;
            if (!(Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Unity/Asset Store-5.x/Pavo Studio"))
                || !(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Unity/Asset Store-5.x/Pavo Studio", "*.unitypackage", SearchOption.AllDirectories).Length > 0))
            {
                int selection = EditorUtility.DisplayDialogComplex(
                    "Missing Paid Asset!",
                    "Muscle Animation Editor was not found to have been downloaded previously.\n\nPlease either browse for Muscle Animation Editor or purchase and download the latest version using the Unity Asset Store.",
                    "Purchase",
                    "Close",
                    "Browse"
                );
                switch (selection)
                {
                    case 0:
                        Application.OpenURL("https://assetstore.unity.com/packages/tools/animation/muscle-animation-editor-32233");
                        return;
                    case 1:
                        return;
                    case 2:
                        string path = EditorUtility.OpenFilePanel("Browse for Muscle Animation Editor", "", "unitypackage");
                        if (path != null && path != String.Empty && path != "" && File.Exists(path))
                        {
                            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Unity/Asset Store-5.x/Pavo Studio/ScriptingAnimation";
                            string name = "Muscle Animation Editor.unitypackage";
                            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                            string copyTo = dir + @"/" + name;
                            File.Copy(path, copyTo, true);
                        }
                        else return;
                        break;
                }
            }
            string downloadURL = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Unity/Asset Store-5.x/Pavo Studio", @"*.unitypackage", SearchOption.AllDirectories)[0];
            ImportAsset(
                pathOrURL: downloadURL,
                assets: new string[] { "Muscle Animation Editor" },
                official: true);
        }
    }
}
