using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Customize/Avatar Essentials/Pumkin's Avatar Tools", false, 25)]
    private static void IOAPAT()
    {
        if (IOA[5] == '0')
        {
            IOA = IOA.Substring(0, 5) + "1" + IOA.Substring(6);
            Menu.SetChecked("kebinImports/Customize/Avatar Essentials/Pumkin's Avatar Tools", true);
        }
        else
        {
            IOA = IOA.Substring(0, 5) + "0" + IOA.Substring(6);
            Menu.SetChecked("kebinImports/Customize/Avatar Essentials/Pumkin's Avatar Tools", false);
        }
        EditorPrefs.SetString("kebinImports.IOA", IOA);
    }
}
