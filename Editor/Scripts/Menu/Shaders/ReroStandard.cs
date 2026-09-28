using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/Shaders/reroStandard Shaders", false, 122)]
        private static void importRSS()
        {
            ToolCatalog.Install("rero");
        }
    }
}
