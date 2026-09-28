using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/VRChat SDK/SDK 3 Avatars", true, 400)]
        private static bool ValidateVRCSDK3a()
        {
            return !isVRCCreatorCompanion;
        }
        // Installed from VRChat's official VPM listing (with com.vrchat.base), so the project ends up laid out the way
        // the Creator Companion expects. Creator Companion projects get the SDK from the Creator Companion instead.
        [MenuItem("kebinImports/VRChat SDK/SDK 3 Avatars", false, 400)]
        private static void importVRCSDK3a()
        {
            ToolCatalog.Install("vrcsdk-avatars");
        }
    }
}
