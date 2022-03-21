using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Customize Essentials/Avatar Essentials/Dynamic Bone", false, 640)]
    private static void IOADB()
    {
        if (IOA[4] == '0')
        {
            IOA = IOA.Substring(0, 4) + "1" + IOA.Substring(5);
            Menu.SetChecked("kebinImports/Customize Essentials/Avatar Essentials/Dynamic Bone", true);
        }
        else
        {
            IOA = IOA.Substring(0, 4) + "0" + IOA.Substring(5);
            Menu.SetChecked("kebinImports/Customize Essentials/Avatar Essentials/Dynamic Bone", false);
        }
        EditorPrefs.SetString("kebinImports.IOA", IOA);
    }
}
