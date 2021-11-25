using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Customize Essentials/World Essentials/Unity-Chan Toon Shader 2.0", false, 115)]
    private static void IOWUTS()
    {
        if (IOW[2] == '0')
        {
            IOW = IOW.Substring(0, 2) + "1" + IOW.Substring(3);
            Menu.SetChecked("kebinImports/Customize Essentials/World Essentials/Unity-Chan Toon Shader 2.0", true);
        }
        else
        {
            IOW = IOW.Substring(0, 2) + "0" + IOW.Substring(3);
            Menu.SetChecked("kebinImports/Customize Essentials/World Essentials/Unity-Chan Toon Shader 2.0", false);
        }
        EditorPrefs.SetString("kebinImports.IOW", IOW);
    }
}