using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/VRChat SDK/SDK 3 Worlds", true, 401)]
        private static bool ValidateVRCSDK3w()
        {
            return !isVRCCreatorCompanion;
        }
        // Installed from VRChat's official VPM listing (with com.vrchat.base), so the project ends up laid out the way
        // the Creator Companion expects. Creator Companion projects get the SDK from the Creator Companion instead.
        [MenuItem("kebinImports/VRChat SDK/SDK 3 Worlds", false, 401)]
        private static void importVRCSDK3w()
        {
            ToolCatalog.Install("vrcsdk-worlds");
        }
    }
}
