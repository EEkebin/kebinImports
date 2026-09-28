using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/Avatar Tools/VRCFury", false, 201)]
        private static void importVRCF()
        {
            ToolCatalog.Install("vrcfury");
        }
    }
}
