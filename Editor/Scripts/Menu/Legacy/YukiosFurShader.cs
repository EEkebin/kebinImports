using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        // The only known download of Yukio's Fur Shader (a Google Drive link) is gone and no mirror exists.
        [MenuItem("kebinImports/Legacy/Yukio's Fur Shader", false, 503)]
        private static void importYFS()
        {
            if (EditorUtility.DisplayDialog("kebinImports", "Yukio's Fur Shader is no longer available: its download link is dead and no archive of it exists.\n\nFor fur, use Xiexe's Unity Shaders (XSFur) or Poiyomi Toon's fur feature.", "Install Xiexe's Unity Shaders", "Close"))
            {
                ToolCatalog.Install("xiexe");
            }
        }
    }
}
