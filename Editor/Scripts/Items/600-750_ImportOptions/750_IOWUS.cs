using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Customize Essentials/World Essentials/UdonSharp", false, 750)]
    private static void IOWUS()
    {
        if (IOW[5] == '0')
        {
            IOW = IOW.Substring(0, 5) + "1";
            Menu.SetChecked("kebinImports/Customize Essentials/World Essentials/UdonSharp", true);
        }
        else
        {
            IOW = IOW.Substring(0, 5) + "0";
            Menu.SetChecked("kebinImports/Customize Essentials/World Essentials/UdonSharp", false);
        }
        EditorPrefs.SetString("kebinImports.IOW", IOW);
    }
}
