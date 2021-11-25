using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using kebinClient;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Poiyomi Toon Shader", false, 25)]
    private static void importPTS()
    {
        Selection.activeGameObject = null;
        client = new ModHttpClient(true);

        // Clean Up Old Files / Delete Pre-existing
        bool refreshRequired = false;
        foreach (string dir in Directory.GetDirectories(Application.dataPath, "OptimizedShaders", SearchOption.AllDirectories))
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
            refreshRequired = true;
        }
        if (Directory.Exists(Application.dataPath + @"/_PoiyomiShaders"))
        {
            Directory.Delete(Application.dataPath + @"/_PoiyomiShaders", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/_PoiyomiShaders.meta"))
        {
            File.Delete(Application.dataPath + @"/_PoiyomiShaders.meta");
            refreshRequired = true;
        }
        if (Directory.Exists(Application.dataPath + @"/../Thry"))
        {
            Directory.Delete(Application.dataPath + @"/../Thry", true);
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
        string repoLink = "https://api.github.com/repos/poiyomi/PoiyomiToonShader/releases/latest";
        ImportAsset(JSON.Parse(Task.Run(() => ModHttpClient.DownloadString(client, repoLink)).Result)["assets"][0]["browser_download_url"]);
    }
}