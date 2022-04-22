using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using kebinClient;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/More/reroStandard Shaders", false, 520)]
    private static void importRSS()
    {
        Selection.activeGameObject = null;
        client = new ModHttpClient(true);

        // Clean Up Old Files / Delete Pre-existing
        bool refreshRequired = false;
        if (Directory.Exists(Application.dataPath + @"/ReroShaders"))
        {
            Directory.Delete(Application.dataPath + @"/ReroShaders", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/ReroShaders.meta"))
        {
            File.Delete(Application.dataPath + @"/ReroShaders.meta");
            refreshRequired = true;
        }
        if (refreshRequired == true)
            FSDSHandler(true);

        // Import Asset
        string repoLink = "https://api.github.com/repos/RetroGEO/reroStandard/releases/latest";
        ImportAsset(JSON.Parse(Task.Run(() => ModHttpClient.DownloadString(client,repoLink)).Result)["assets"][0]["browser_download_url"]);
    }
}
