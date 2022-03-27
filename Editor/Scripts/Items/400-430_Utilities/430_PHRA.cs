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
                "WARNING!\n\nThis will most likely fix any errors you have. It is a Hard Reset to the UnityEditor Appliation's Editor Preferences while also Fixing Scripting Define Symbols. It is meant to be used when you are in a PANIC and cannot figure out issues with the project. However, it does require a restart of Unity to do so.\n\nTHIS MAY MAKE UNITY HUB AND UNITY TO FORGET WHERE THE PROJECT IS, HOWEVER DATA HAS NOT BEEN LOST!!!\n\nPlease relaunch Unity a few seconds after it closes.\n\nDo you wish to continue?",
                "Yes",
                "No"
            ) == false) return;
        }
        FSDSHandler(true);
        try
        {
            RegistryKey unity = Registry.CurrentUser.OpenSubKey("Software\\Unity\\UnityEditor", true);
            RegistryKey unitytech = Registry.CurrentUser.OpenSubKey("Software\\Unity Technologies\\Unity Editor 5.x", true);
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
            if (unitytech != null)
            {
                var values = unitytech.GetValueNames();
                foreach (string value in values)
                {
                    unitytech.DeleteValue(value);
                }
            }
            EditorApplication.Exit(0);
        }
        catch (System.Exception)
        {
            Debug.LogError("[kebinImports] Error: Attempted to PANIC HARD RESET ALL, but failed miserably. Seek help and advice from the Discord Server.");
        }
    }
}
