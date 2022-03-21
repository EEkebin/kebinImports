using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using kebinClient;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/ComboGestureExpressions", false, 330)]
    private static void importCGE()
    {
        Selection.activeGameObject = null;
        client = new ModHttpClient(true);

        // Clean Up Old Files / Delete Pre-existing
        bool refreshRequired = false;
        if (Directory.Exists(Application.dataPath + @"/Hai"))
        {
            Directory.Delete(Application.dataPath + @"/Hai", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/Hai.meta"))
        {
            File.Delete(Application.dataPath + @"/Hai.meta");
            refreshRequired = true;
        }
        if (refreshRequired == true)
            FSDSHandler(true);

        // Import Asset
        string repoLink = "https://api.github.com/repos/hai-vr/combo-gesture-expressions-av3/releases/latest";
        ImportAsset(JSON.Parse(Task.Run(() => ModHttpClient.DownloadString(client,repoLink)).Result)["assets"][0]["browser_download_url"]);
    }
}
