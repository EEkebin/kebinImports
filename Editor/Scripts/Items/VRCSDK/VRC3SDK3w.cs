using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using SimpleJSON;
using kebinClient;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/VRChat SDK 3/Worlds", false, 13)]
    private static void importVRCSDK3w()
    {
        Selection.activeGameObject = null;
        client = new ModHttpClient();

        // Check If Running The Latest Unity Version Supported By VRChat
        jsonNode = JSON.Parse(Task.Run(() => ModHttpClient.DownloadString(client, "https://api.vrchat.cloud/api/1/config")).Result);
        if (Application.unityVersion != jsonNode["sdkUnityVersion"])
        {
            EditorUtility.DisplayDialog("Error caught.", "kebinImports saved you from making a fucky wucky!\n\nPlease use Unity " + jsonNode["sdkUnityVersion"] + " to import the VRChat SDK!", "Ok");
            return;
        }

        // Clean Up Old Files / Delete Pre-existing
        bool refreshRequired = false;
        if (Directory.Exists(Application.dataPath + @"/VRCSDK"))
        {
            Directory.Delete(Application.dataPath + @"/VRCSDK", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/VRCSDK.meta"))
        {
            File.Delete(Application.dataPath + @"/VRCSDK.meta");
            refreshRequired = true;
        }
        if (Directory.Exists(Application.dataPath + @"/Udon"))
        {
            Directory.Delete(Application.dataPath + @"/Udon", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/Udon.meta"))
        {
            File.Delete(Application.dataPath + @"/Udon.meta");
            refreshRequired = true;
        }
        if (Directory.Exists(Application.dataPath + @"/VRChat Examples"))
        {
            Directory.Delete(Application.dataPath + @"/VRChat Examples", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/VRChat Examples.meta"))
        {
            File.Delete(Application.dataPath + @"/VRChat Examples.meta");
            refreshRequired = true;
        }
        if (Directory.Exists(Application.dataPath + @"/SerializedUdonPrograms"))
        {
            Directory.Delete(Application.dataPath + @"/SerializedUdonPrograms", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/SerializedUdonPrograms.meta"))
        {
            File.Delete(Application.dataPath + @"/SerializedUdonPrograms.meta");
            refreshRequired = true;
        }
        if (Directory.Exists(Application.dataPath + @"/../Packages/com.vrchat.vrcsdk3"))
        {
            Directory.Delete(Application.dataPath + @"/../Packages/com.vrchat.vrcsdk3", true);
            refreshRequired = true;
        }
        if (Client.Remove("com.unity.cinemachine").Error != null)
        {
            refreshRequired = true;
        }
        if (Client.Remove("com.unity.xr.oculus.standalone").Error != null)
        {
            refreshRequired = true;
        }
        if (Client.Remove("com.unity.xr.openvr.standalone").Error != null)
        {
            refreshRequired = true;
        }
        if (Client.Remove("com.unity.postprocessing").Error != null)
        {
            refreshRequired = true;
        }
        if (refreshRequired == true)
            FSDSHandler(true);

        // Import Asset
        string package = "https://vrchat.com/download/sdk3-worlds";
        ImportAsset(package);
    }
}