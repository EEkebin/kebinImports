using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Customize/World Essentials/Poiyomi Toon Shader", false, 31)]
    private static void IOWPTS()
    {
        if (IOW[1] == '0')
        {
            IOW = IOW.Substring(0, 1) + "1" + IOW.Substring(2);
            Menu.SetChecked("kebinImports/Customize Essentials/World Essentials/Poiyomi Toon Shader", true);
        }
        else
        {
            IOW = IOW.Substring(0, 1) + "0" + IOW.Substring(2);
            Menu.SetChecked("kebinImports/Customize Essentials/World Essentials/Poiyomi Toon Shader", false);
        }
        EditorPrefs.SetString("kebinImports.IOW", IOW);
    }
}
