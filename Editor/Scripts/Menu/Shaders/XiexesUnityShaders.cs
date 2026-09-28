using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/Shaders/Xiexe's Unity Shaders", false, 120)]
        private static void importXUS()
        {
            ToolCatalog.Install("xiexe");
        }
    }
}
