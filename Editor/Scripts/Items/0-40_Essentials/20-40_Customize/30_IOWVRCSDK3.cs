using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Customize/World Essentials/VRChat SDK 3", false, 30)]
    private static void IOWVRCSDK3()
    {
        if (IOW[0] == '0')
        {
            IOW = "1" + IOW.Substring(1);
            Menu.SetChecked("kebinImports/Customize Essentials/World Essentials/VRChat SDK 3", true);
        }
        else
        {
            IOW = "0" + IOW.Substring(1);
            Menu.SetChecked("kebinImports/Customize Essentials/World Essentials/VRChat SDK 3", false);
        }
        EditorPrefs.SetString("kebinImports.IOW", IOW);
    }
}
