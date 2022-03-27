using Microsoft.Win32;
using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/PANIC HARD RESET ALL", false, 430)]
    private static void PHRA()
    {
        if (hideWarnings == false)
        {
            if (EditorUtility.DisplayDialog(
                "WARNING!",
                "WARNING!\n\nThis will most likely fix any errors you have. It is a Hard Reset to the UnityEditor Appliation's Editor Preferences while also Fixing Scripting Define Symbols. It is meant to be used when you are in a PANIC and cannot figure out issues with the project. However, it does require a restart of Unity to do so. Please relaunch Unity a few seconds after it closes.\n\nDo you wish to continue?",
                "Yes",
                "No"
            ) == false) return;
        }
        FSDSHandler(true);
        RegistryKey unity = Registry.CurrentUser.OpenSubKey("Software\\Unity Technologies\\Unity Editor 5.x", true);
        if (unity != null)
        {
            var keys = unity.GetValueNames();
            foreach (string key in keys)
            {
                unity.DeleteValue(key);
            }
        }
        EditorApplication.Exit(0);
    }
}
