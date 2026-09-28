using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        // Tools from VRChat's curated listing.
        [MenuItem("kebinImports/Avatar Tools/Gesture Manager", false, 206)]
        private static void importGestureManager() { ToolCatalog.Install("gesture-manager"); }

        [MenuItem("kebinImports/Avatar Tools/Av3Emulator", false, 207)]
        private static void importAv3Emulator() { ToolCatalog.Install("av3emulator"); }

        [MenuItem("kebinImports/World Tools/VRWorld Toolkit", false, 300)]
        private static void importVRWorldToolkit() { ToolCatalog.Install("vrworld-toolkit"); }

        [MenuItem("kebinImports/World Tools/AudioLink", false, 301)]
        private static void importAudioLink() { ToolCatalog.Install("audiolink"); }
    }
}
