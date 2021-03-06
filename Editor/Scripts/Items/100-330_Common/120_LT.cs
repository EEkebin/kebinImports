using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using kebinClient;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/lilToon", false, 120)]
    private static void importLT()
    {
        Selection.activeGameObject = null;
        client = new ModHttpClient(true);

        // Clean Up Old Files / Delete Pre-existing
        bool refreshRequired = false;
        if (Directory.Exists(Application.dataPath + @"/lilToon"))
        {
            Directory.Delete(Application.dataPath + @"/lilToon", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/lilToon.meta"))
        {
            File.Delete(Application.dataPath + @"/lilToon.meta");
            refreshRequired = true;
        }
        if (Directory.Exists(Application.dataPath + @"/lilToonSetting"))
        {
            Directory.Delete(Application.dataPath + @"/lilToonSetting", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/lilToonSetting.meta"))
        {
            File.Delete(Application.dataPath + @"/lilToonSetting.meta");
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
        if (File.Exists(Application.dataPath + @"/msc.rsp"))
        {
            File.Delete(Application.dataPath + @"/msc.rsp");
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/msc.rsp.meta"))
        {
            File.Delete(Application.dataPath + @"/msc.rsp.meta");
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/mcs.rsp"))
        {
            File.Delete(Application.dataPath + @"/mcs.rsp");
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/mcs.rsp.meta"))
        {
            File.Delete(Application.dataPath + @"/mcs.rsp.meta");
            refreshRequired = true;
        }
        if (refreshRequired == true)
            FSDSHandler(true);

        // Import Asset
        string repoLink = "https://api.github.com/repos/lilxyzw/lilToon/releases/latest";
        ImportAsset(JSON.Parse(Task.Run(() => ModHttpClient.DownloadString(client,repoLink)).Result)["assets"][0]["browser_download_url"]);
    }
}
