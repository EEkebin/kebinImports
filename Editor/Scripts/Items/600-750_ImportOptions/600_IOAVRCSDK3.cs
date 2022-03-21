using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Customize Essentials/Avatar Essentials/VRChat SDK 3", false, 600)]
    private static void IOAVRCSDK3()
    {
        if (IOA[0] == '0')
        {
            IOA = "1" + IOA.Substring(1);
            Menu.SetChecked("kebinImports/Customize Essentials/Avatar Essentials/VRChat SDK 3", true);
        }
        else
        {
            IOA = "0" + IOA.Substring(1);
            Menu.SetChecked("kebinImports/Customize Essentials/Avatar Essentials/VRChat SDK 3", false);
        }
        EditorPrefs.SetString("kebinImports.IOA", IOA);
    }
}
