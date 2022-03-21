using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using kebinClient;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Pumkin's Avatar Tools", false, 310)]
    private static void importPAT()
    {
        Selection.activeGameObject = null;
        client = new ModHttpClient(true);

        // Clean Up Old Files / Delete Pre-existing
        bool refreshRequired = false;
        if (Directory.Exists(Application.dataPath + @"/PumkinsAvatarTools"))
        {
            Directory.Delete(Application.dataPath + @"/PumkinsAvatarTools", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/PumkinsAvatarTools.meta"))
        {
            File.Delete(Application.dataPath + @"/PumkinsAvatarTools.meta");
            refreshRequired = true;
        }
        if (refreshRequired == true)
            FSDSHandler(true);

        // Import Asset
        string repoLink = "https://api.github.com/repos/rurre/PumkinsAvatarTools/releases/latest";
        ImportAsset(JSON.Parse(Task.Run(() => ModHttpClient.DownloadString(client,repoLink)).Result)["assets"][0]["browser_download_url"]);
    }
}
