using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        // Legacy tools stay in the menu but are greyed out unless "Show legacy items" is on in kebinImports > Settings.
        private static bool showLegacy = false;
        private static bool ShowLegacy => showLegacy;

        [MenuItem("kebinImports/Legacy/Dynamic Bone (paid)", true, 500)]
        private static bool ValidateDB() => ShowLegacy;
        [MenuItem("kebinImports/Legacy/Arktoon Shader", true, 501)]
        private static bool ValidateAS() => ShowLegacy;
        [MenuItem("kebinImports/Legacy/Cubed's Unity Shaders", true, 502)]
        private static bool ValidateCUS() => ShowLegacy;
        [MenuItem("kebinImports/Legacy/Yukio's Fur Shader", true, 503)]
        private static bool ValidateYFS() => ShowLegacy;
    }
}
