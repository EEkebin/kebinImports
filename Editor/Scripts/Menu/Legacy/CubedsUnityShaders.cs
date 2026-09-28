using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/Legacy/Cubed's Unity Shaders", false, 502)]
        private static void importCUS()
        {
            ToolCatalog.Install("cubed");
        }
    }
}
