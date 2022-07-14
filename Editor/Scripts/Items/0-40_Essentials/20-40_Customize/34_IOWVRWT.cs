using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Customize/World Essentials/VRWorld Toolkit", false, 34)]
    private static void IOWVRWT()
    {
        if (IOW[4] == '0')
        {
            IOW = IOW.Substring(0, 4) + "1" + IOW.Substring(5);
            Menu.SetChecked("kebinImports/Customize Essentials/World Essentials/VRWorld Toolkit", true);
        }
        else
        {
            IOW = IOW.Substring(0, 4) + "0" + IOW.Substring(5);
            Menu.SetChecked("kebinImports/Customize Essentials/World Essentials/VRWorld Toolkit", false);
        }
        EditorPrefs.SetString("kebinImports.IOW", IOW);
    }
}
