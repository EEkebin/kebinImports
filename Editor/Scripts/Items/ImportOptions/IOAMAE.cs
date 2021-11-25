using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Customize Essentials/Avatar Essentials/Muscle Animation Editor", false, 112)]
    private static void IOAMAE()
    {
        if (IOA[6] == '0')
        {
            IOA = IOA.Substring(0, 6) + "1";
            Menu.SetChecked("kebinImports/Customize Essentials/Avatar Essentials/Muscle Animation Editor", true);
        }
        else
        {
            IOA = IOA.Substring(0, 6) + "0";
            Menu.SetChecked("kebinImports/Customize Essentials/Avatar Essentials/Muscle Animation Editor", false);
        }
        EditorPrefs.SetString("kebinImports.IOA", IOA);
    }
}