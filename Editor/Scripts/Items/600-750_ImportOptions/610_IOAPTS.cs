using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Customize Essentials/Avatar Essentials/Poiyomi Toon Shader", false, 610)]
    private static void IOAPTS()
    {
        if (IOA[1] == '0')
        {
            IOA = IOA.Substring(0, 1) + "1" + IOA.Substring(2);
            Menu.SetChecked("kebinImports/Customize Essentials/Avatar Essentials/Poiyomi Toon Shader", true);
        }
        else
        {
            IOA = IOA.Substring(0, 1) + "0" + IOA.Substring(2);
            Menu.SetChecked("kebinImports/Customize Essentials/Avatar Essentials/Poiyomi Toon Shader", false);
        }
        EditorPrefs.SetString("kebinImports.IOA", IOA);
    }
}
