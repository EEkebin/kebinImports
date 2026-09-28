using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/Shaders/Mochie's Unity Shaders", false, 121)]
        private static void importMUS()
        {
            ToolCatalog.Install("mochie");
        }
    }
}
