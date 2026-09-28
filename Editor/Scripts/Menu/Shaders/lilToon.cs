using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/Shaders/lilToon", false, 101)]
        private static void importLT()
        {
            ToolCatalog.Install("liltoon");
        }
    }
}
