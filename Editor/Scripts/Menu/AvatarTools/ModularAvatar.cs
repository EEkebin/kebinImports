using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/Avatar Tools/Modular Avatar", false, 200)]
        private static void importMA()
        {
            ToolCatalog.Install("modular-avatar");
        }
    }
}
