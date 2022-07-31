using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Customize/Avatar Essentials/lilToon", false, 23)]
    private static void IOALT()
    {
        if (IOA[3] == '0')
        {
            IOA = IOA.Substring(0, 3) + "1" + IOA.Substring(4);
            Menu.SetChecked("kebinImports/Customize/Avatar Essentials/lilToon", true);
        }
        else
        {
            IOA = IOA.Substring(0, 3) + "0" + IOA.Substring(4);
            Menu.SetChecked("kebinImports/Customize/Avatar Essentials/lilToon", false);
        }
        EditorPrefs.SetString("kebinImports.IOA", IOA);
    }
}
