using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/Avatar Tools/ComboGestureExpressions", false, 204)]
        private static void importCGE()
        {
            ToolCatalog.Install("cge");
        }
    }
}
