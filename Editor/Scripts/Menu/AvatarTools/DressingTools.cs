using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/Avatar Tools/DressingTools", false, 202)]
        private static void importDT()
        {
            ToolCatalog.Install("dressingtools");
        }
    }
}
