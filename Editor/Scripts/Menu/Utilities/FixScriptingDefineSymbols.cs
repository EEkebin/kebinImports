using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/Fix Scripting Define Symbols", false, 602)]
        private static void FSDS()
        {
            FSDSHandler();
        }
        private static void FSDSHandler(bool forced = false)
        {
            if (!forced && hideWarnings == false)
            {
                if (EditorUtility.DisplayDialog(
                    "WARNING!",
                    "WARNING!\n\nThis will remove and allow other assets to reimport their Script Define Variables automatically!\n\nInfo: Usually, this is to fix issues with the VRChat SDK, Poiyomi Toon Shader, and Pumkins Avatar Tools interconnectivity.\n\nThese can also be found under:\nFile >> Build Settings >> Player Settings... >> Other Settings >> Scripting Define Symbols\n\nAre you sure you want to do this?",
                    "Yes",
                    "No"
                ) == false) return;
            }
            ClearScriptingDefineSymbols();
            AssetDatabase.Refresh();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            EditorApplication.update += ClearLog;
        }
    }
}
