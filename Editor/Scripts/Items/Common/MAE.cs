using System;
using System.IO;
using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Muscle Animation Editor", false, 53)]
    private static void importMAE()
    {
        Selection.activeGameObject = null;
        if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Unity/Asset Store-5.x/Pavo Studio") && Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Unity/Asset Store-5.x/Pavo Studio", "*.unitypackage", SearchOption.AllDirectories).Length > 0)
        {

            // Clean Up Old Files / Delete Pre-existing
            bool refreshRequired = false;
            if (Directory.Exists(Application.dataPath + @"/Muscle Animation Editor"))
            {
                Directory.Delete(Application.dataPath + @"/Muscle Animation Editor", true);
                refreshRequired = true;
            }
            if (File.Exists(Application.dataPath + @"/Muscle Animation Editor.meta"))
            {
                File.Delete(Application.dataPath + @"/Muscle Animation Editor.meta");
                refreshRequired = true;
            }
            if (refreshRequired == true)
                FSDSHandler(true);
        }
        else
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
                    if (path != null && path != String.Empty && path != "")
                    {
                        string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Unity/Asset Store-5.x/Pavo Studio/ScriptingAnimation";
                        string name = "Muscle Animation Editor.unitypackage";
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        string copyTo = dir + @"/" + name;
                        File.Copy(path, copyTo, true);
                    }
                    break;
            }
        }

        // Import Asset
        AssetDatabase.ImportPackage(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Unity/Asset Store-5.x/Pavo Studio", @"*.unitypackage", SearchOption.AllDirectories)[0], false);
    }
}