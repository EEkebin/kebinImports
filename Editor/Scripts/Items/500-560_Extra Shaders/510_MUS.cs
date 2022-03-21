using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using kebinClient;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Extra Shaders/Mochies Unity Shaders", false, 510)]
    private static void importMUS()
    {
        Selection.activeGameObject = null;
        client = new ModHttpClient(true);

        // Clean Up Old Files / Delete Pre-existing
        bool refreshRequired = false;
        if (Directory.Exists(Application.dataPath + @"/Mochie"))
        {
            Directory.Delete(Application.dataPath + @"/Mochie", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/Mochie.meta"))
        {
            File.Delete(Application.dataPath + @"/Mochie.meta");
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/csc.rsp"))
        {
            File.Delete(Application.dataPath + @"/csc.rsp");
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/csc.rsp.meta"))
        {
            File.Delete(Application.dataPath + @"/csc.rsp.meta");
            refreshRequired = true;
        }
        if (refreshRequired == true)
            FSDSHandler(true);

        // Import Asset
        string repoLink = "https://api.github.com/repos/MochiesCode/Mochies-Unity-Shaders/releases/latest";
        ImportAsset(JSON.Parse(Task.Run(() => ModHttpClient.DownloadString(client,repoLink)).Result)["assets"][0]["browser_download_url"]);
    }
}
