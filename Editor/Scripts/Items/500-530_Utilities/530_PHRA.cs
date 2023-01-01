using Microsoft.Win32;
using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/PANIC HARD RESET ALL", false, 530)]
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
                RegistryKey unity = Registry.CurrentUser.OpenSubKey("Software\\Unity\\UnityEditor", true);
                if (unity != null)
                {
                    var values = unity.GetValueNames();
                    foreach (string value in values)
                    {
                        unity.DeleteValue(value);
                    }
                    var keys = unity.GetSubKeyNames();
                    foreach (string key in keys)
                    {
                        unity.DeleteSubKeyTree(key);
                    }
                }
                Lightmapping.ClearDiskCache();
                Caching.ClearCache();
                EditorPrefs.DeleteAll();
                if (selection == 0)
                {
                    System.Diagnostics.Process.Start(EditorApplication.applicationPath, "-projectPath \"" + System.IO.Path.GetDirectoryName(Application.dataPath) + "\"");
                }
                EditorApplication.Exit(0);
            }
            catch (System.Exception)
            {
                Debug.LogError("[kebinImports] Error: Attempted to PANIC HARD RESET ALL, but failed miserably. Seek help and advice from the Discord Server.");
            }
        }
    }
}
