using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using kebinClient;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/VRWorld Toolkit", false, 200)]
    private static void importVRWT()
    {
        Selection.activeGameObject = null;
        client = new ModHttpClient(true);

        // Clean Up Old Files / Delete Pre-existing
        bool refreshRequired = false;
        if (Directory.Exists(Application.dataPath + @"/VRWorldToolkit"))
        {
            Directory.Delete(Application.dataPath + @"/VRWorldToolkit", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/VRWorldToolkit.meta"))
        {
            File.Delete(Application.dataPath + @"/VRWorldToolkit.meta");
            refreshRequired = true;
        }
        if (refreshRequired == true)
            FSDSHandler(true);

        // Import Asset
        string repoLink = "https://api.github.com/repos/oneVR/VRWorldToolkit/releases/latest";
        ImportAsset(JSON.Parse(Task.Run(() => ModHttpClient.DownloadString(client,repoLink)).Result)["assets"][0]["browser_download_url"]);
    }
}
