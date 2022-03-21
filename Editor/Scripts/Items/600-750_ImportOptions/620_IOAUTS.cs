using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Customize Essentials/Avatar Essentials/Unity-Chan Toon Shader 2.0", false, 620)]
    private static void IOAUTS()
    {
        if (IOA[2] == '0')
        {
            IOA = IOA.Substring(0, 2) + "1" + IOA.Substring(3);
            Menu.SetChecked("kebinImports/Customize Essentials/Avatar Essentials/Unity-Chan Toon Shader 2.0", true);
        }
        else
        {
            IOA = IOA.Substring(0, 2) + "0" + IOA.Substring(3);
            Menu.SetChecked("kebinImports/Customize Essentials/Avatar Essentials/Unity-Chan Toon Shader 2.0", false);
        }
        EditorPrefs.SetString("kebinImports.IOA", IOA);
    }
}
