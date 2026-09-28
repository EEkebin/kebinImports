using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/Avatar Tools/Pumkin's Avatar Tools", false, 203)]
        private static void importPAT()
        {
            ToolCatalog.Install("pumkin");
        }
    }
}
