using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/Avatar Tools/Avatar Performance Tools", false, 205)]
        private static void importAPT()
        {
            ToolCatalog.Install("apt");
        }
    }
}
