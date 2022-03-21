using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using kebinClient;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Unity-Chan Toon Shader 2.0", false, 110)]
    private static void importUTS()
    {
        Selection.activeGameObject = null;
        client = new ModHttpClient(true);

        // Clean Up Old Files / Delete Pre-existing
        bool refreshRequired = false;
        if (Directory.Exists(Application.dataPath + @"/Toon"))
        {
            Directory.Delete(Application.dataPath + @"/Toon", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/Toon.meta"))
        {
            File.Delete(Application.dataPath + @"/Toon.meta");
            refreshRequired = true;
        }
        if (refreshRequired == true)
            FSDSHandler(true);

        // Import Asset
        string repoLink = "https://api.github.com/repos/unity3d-jp/UnityChanToonShaderVer2_Project/releases";
        jsonNode = JSON.Parse(Task.Run(() => ModHttpClient.DownloadString(client,repoLink)).Result);
        string package = "";
        for (int i = 0; i < jsonNode.Count; i++)
        {
            for (int j = 0; j < jsonNode[i]["assets"].Count; j++)
            {
                if (jsonNode[i]["assets"][j]["browser_download_url"].ToString().Trim('\"').ToLower().Contains("shaderonly") && jsonNode[i]["assets"][j]["browser_download_url"].ToString().Trim('\"').ToLower().EndsWith(".unitypackage"))
                {
                    package = jsonNode[i]["assets"][j]["browser_download_url"].ToString().Trim('\"');
                    break;
                }
            }
            if (package != "") break;
        }
        ImportAsset(package);
    }
}
