using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/VRChat SDK 3/Worlds", true, 51)]
        private static bool ValidateVRCSDK3w()
        {
            return !isVRCCreatorCompanion;
        }
        [MenuItem("kebinImports/VRChat SDK 3/Worlds", false, 51)]
        private static void importVRCSDK3w()
        {
            client = new HttpClient();
            jsonNode = JSON.Parse(Task.Run(() => HttpClient.DownloadString(client, "https://api.vrchat.cloud/api/1/config")).Result);
            if (Application.unityVersion != jsonNode["sdkUnityVersion"])
            {
                EditorUtility.DisplayDialog("Error caught.", "kebinImports saved you from making a fucky wucky!\n\nPlease use Unity " + jsonNode["sdkUnityVersion"] + " to import the VRChat SDK!", "Ok");
                return;
            }
            ImportAsset(
                pathOrURL: "https://vrchat.com/download/sdk3-worlds",
                assets: new string[] { "VRCSDK", "Udon", "VRChat Examples", "SerializedUdonPrograms", "../Packages/com.vrchat.vrcsdk3" },
                packages: new string[] { "com.unity.burst", "com.unity.mathematics", "com.unity.xr.oculus.standalone", "com.unity.xr.openvr.standalone", "com.unity.cinemachine", "com.unity.postprocessing", "com.unity.package-manager-ui", "com.vrchat.vrcsdk3" });
        }
    }
}
