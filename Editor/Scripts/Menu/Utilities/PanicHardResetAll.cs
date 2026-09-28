using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/PANIC HARD RESET ALL", false, 620)]
        private static void PHRA()
        {
            int selection = EditorUtility.DisplayDialogComplex(
                "WARNING!",
                "WARNING!\n\nThis will most likely fix any errors you have. It is a Hard Reset to the UnityEditor Appliation's Editor Preferences while also Fixing Scripting Define Symbols. It is meant to be used when you are in a PANIC and cannot figure out issues with the project. However, it does require a restart of Unity to do so.\n\nTHIS MAY MAKE UNITY HUB AND UNITY TO FORGET WHERE THE PROJECT IS, HOWEVER DATA HAS NOT BEEN LOST!!!\n\nDo you wish to continue?",
                "Reopen Project",
                "Cancel",
                "Close Project");
            if (selection == 1) return;
            FSDSHandler(true);
            try
            {
#if UNITY_EDITOR_WIN
                // Per-project editor state lives under HKCU\Software\Unity\UnityEditor. reg.exe is used instead of
                // Microsoft.Win32.Registry so this also compiles under the .NET Standard API compatibility level.
                using (Process reg = Process.Start(new ProcessStartInfo("reg.exe", "delete \"HKCU\\Software\\Unity\\UnityEditor\" /f")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                }))
                {
                    reg.WaitForExit();
                }
#endif
                Lightmapping.ClearDiskCache();
                Caching.ClearCache();
                EditorPrefs.DeleteAll();
                if (selection == 0)
                {
                    Process.Start(EditorApplication.applicationPath, "-projectPath \"" + ProjectPath + "\"");
                }
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[kebinImports] Error: Attempted to PANIC HARD RESET ALL, but failed miserably. Seek help and advice from the Discord Server.\n" + e.Message);
            }
        }
    }
}
