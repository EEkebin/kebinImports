using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using SimpleJSON;
using kebinClient;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/VRChat SDK 3/Avatars", false, 50)]
    private static void importVRCSDK3a()
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
        if (Client.Remove("com.unity.xr.oculus.standalone").Error != null)
        {
            refreshRequired = true;
        }
        if (Client.Remove("com.unity.xr.openvr.standalone").Error != null)
        {
            refreshRequired = true;
        }
        if (refreshRequired == true)
            FSDSHandler(true);

        // Import Asset
        string package = "https://vrchat.com/download/sdk3-avatars";
        ImportAsset(package);
    }
}
