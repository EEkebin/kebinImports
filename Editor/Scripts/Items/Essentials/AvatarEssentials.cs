using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Avatar Essentials", false, 0)]
    private static void importAvatarEssentials()
    {
        Debug.Log("importAvatarEssentials is not yet implemented!");
        if (IOA[0] == '1') importVRCSDK3a();
        if (IOA[1] == '1') importPTS();
        if (IOA[2] == '1') importUTS();
        if (IOA[3] == '1') importLT();
        if (IOA[4] == '1') importDB();
        if (IOA[5] == '1') importPAT();
        if (IOA[6] == '1') importMAE();
    }
}