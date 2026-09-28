using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/VRChat SDK/SDK 2 (Unity 2019)", true, 402)]
        private static bool ValidateVRCSDK2()
        {
            return !isVRCCreatorCompanion && Application.unityVersion.StartsWith("2019");
        }
        [MenuItem("kebinImports/VRChat SDK/SDK 2 (Unity 2019)", false, 402)]
        private static void importVRCSDK2()
        {
            if (!VRChatSdkSupportsThisUnity()) return;
            ImportAsset(
                pathOrURL: "https://vrchat.com/download/sdk2",
                assets: new string[] { "VRCSDK", "Udon", "VRChat Examples", "SerializedUdonPrograms", "../Packages/com.vrchat.vrcsdk3" },
                packages: new string[] { "com.unity.burst", "com.unity.mathematics", "com.unity.xr.oculus.standalone", "com.unity.xr.openvr.standalone", "com.unity.cinemachine", "com.unity.postprocessing", "com.unity.package-manager-ui", "com.vrchat.vrcsdk3" });
        }
    }
}
