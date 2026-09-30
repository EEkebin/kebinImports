using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/Avatar Tools/VRCQuestTools", false, 208)]
        private static void importVRCQT()
        {
            ToolCatalog.Install("vrcquesttools");
        }
    }
}
