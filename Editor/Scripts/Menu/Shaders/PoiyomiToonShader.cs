using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/Shaders/Poiyomi Toon Shader", false, 102)]
        private static void importPTS()
        {
            ToolCatalog.Install("poiyomi");
        }
    }
}
