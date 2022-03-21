using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using kebinClient;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/UdonSharp", false, 210)]
    private static void importUS()
    {
        Selection.activeGameObject = null;
        client = new ModHttpClient(true);

        // Clean Up Old Files / Delete Pre-existing
        bool refreshRequired = false;
        if (Directory.Exists(Application.dataPath + @"/UdonSharp"))
        {
            Directory.Delete(Application.dataPath + @"/UdonSharp", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/UdonSharp.meta"))
        {
            File.Delete(Application.dataPath + @"/UdonSharp.meta");
            refreshRequired = true;
        }
        if (Directory.Exists(Application.dataPath + @"/Gizmos"))
        {
            Directory.Delete(Application.dataPath + @"/Gizmos", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/Gizmos.meta"))
        {
            File.Delete(Application.dataPath + @"/Gizmos.meta");
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/VRCSDK/Dependencies/Managed/Microsoft.CodeAnalysis.CSharp.dll"))
        {
            File.Delete(Application.dataPath + @"/VRCSDK/Dependencies/Managed/Microsoft.CodeAnalysis.CSharp.dll.meta");
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/VRCSDK/Dependencies/Managed/Microsoft.CodeAnalysis.dll"))
        {
            File.Delete(Application.dataPath + @"/VRCSDK/Dependencies/Managed/Microsoft.CodeAnalysis.dll.meta");
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/VRCSDK/Dependencies/Managed/System.Reflection.Metadata.dll"))
        {
            File.Delete(Application.dataPath + @"/VRCSDK/Dependencies/Managed/System.Reflection.Metadata.dll.meta");
            refreshRequired = true;
        }
        if (refreshRequired == true)
            FSDSHandler(true);

        // Import Asset
        string repoLink = "https://api.github.com/repos/MerlinVR/UdonSharp/releases/latest";
        ImportAsset(JSON.Parse(Task.Run(() => ModHttpClient.DownloadString(client,repoLink)).Result)["assets"][0]["browser_download_url"]);
    }
}
