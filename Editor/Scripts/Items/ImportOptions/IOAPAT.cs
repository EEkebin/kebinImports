using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Customize Essentials/Avatar Essentials/Pumkin's Avatar Tools", false, 111)]
    private static void IOAPAT()
    {
        if (IOA[5] == '0')
        {
            IOA = IOA.Substring(0, 5) + "1" + IOA.Substring(6);
            Menu.SetChecked("kebinImports/Customize Essentials/Avatar Essentials/Pumkin's Avatar Tools", true);
        }
        else
        {
            IOA = IOA.Substring(0, 5) + "0" + IOA.Substring(6);
            Menu.SetChecked("kebinImports/Customize Essentials/Avatar Essentials/Pumkin's Avatar Tools", false);
        }
        EditorPrefs.SetString("kebinImports.IOA", IOA);
    }
}