using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/World Essentials", false, 1)]
    private static void importWorldEssentials()
    {
        if (IOW[0] == '1') importVRCSDK3w();
        if (IOW[1] == '1') importPTS();
        if (IOW[2] == '1') importUTS();
        if (IOW[3] == '1') importLT();
        if (IOW[4] == '1') importVRWT();
        if (IOW[5] == '1') importUS();
    }
}