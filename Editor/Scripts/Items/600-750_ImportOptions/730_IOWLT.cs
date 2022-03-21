using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Customize Essentials/World Essentials/lilToon", false, 730)]
    private static void IOWLT()
    {
        if (IOW[3] == '0')
        {
            IOW = IOW.Substring(0, 3) + "1" + IOW.Substring(4);
            Menu.SetChecked("kebinImports/Customize Essentials/World Essentials/lilToon", true);
        }
        else
        {
            IOW = IOW.Substring(0, 3) + "0" + IOW.Substring(4);
            Menu.SetChecked("kebinImports/Customize Essentials/World Essentials/lilToon", false);
        }
        EditorPrefs.SetString("kebinImports.IOW", IOW);
    }
}
