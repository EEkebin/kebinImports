using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using kebinClient;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Extra Shaders/Legacy/Cubed's Unity Shaders", false, 560)]
    private static void importCUS()
    {
        Selection.activeGameObject = null;
        client = new ModHttpClient(true);

        // Clean Up Old Files / Delete Pre-existing
        bool refreshRequired = false;
        if (Directory.Exists(Application.dataPath + @"/Cubed's Unity Shaders"))
        {
            Directory.Delete(Application.dataPath + @"/Cubed's Unity Shaders", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/Cubed's Unity Shaders.meta"))
        {
            File.Delete(Application.dataPath + @"/Cubed's Unity Shaders.meta");
            refreshRequired = true;
        }
        if (refreshRequired == true)
            FSDSHandler(true);

        // Import Asset
        string repoLink = "https://api.github.com/repos/cubedparadox/Cubeds-Unity-Shaders/releases/latest";
        ImportAsset(JSON.Parse(Task.Run(() => ModHttpClient.DownloadString(client,repoLink)).Result)["assets"][0]["browser_download_url"]);
    }
}
