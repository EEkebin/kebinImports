using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using kebinClient;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/More/Xiexes Unity Shaders", false, 500)]
    private static void importXUS()
    {
        Selection.activeGameObject = null;
        client = new ModHttpClient(true);

        // Clean Up Old Files / Delete Pre-existing
        bool refreshRequired = false;
        if (Directory.Exists(Application.dataPath + @"/Xiexes-Unity-Shaders"))
        {
            Directory.Delete(Application.dataPath + @"/Xiexes-Unity-Shaders", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/Xiexes-Unity-Shaders.meta"))
        {
            File.Delete(Application.dataPath + @"/Xiexes-Unity-Shaders.meta");
            refreshRequired = true;
        }
        if (refreshRequired == true)
            FSDSHandler(true);

        // Import Asset
        string repoLink = "https://api.github.com/repos/Xiexe/Xiexes-Unity-Shaders/releases/latest";

        ImportAsset(JSON.Parse(Task.Run(() => ModHttpClient.DownloadString(client,repoLink)).Result)["zipball_url"], true, "Xiexes-Unity-Shaders");
    }
}
