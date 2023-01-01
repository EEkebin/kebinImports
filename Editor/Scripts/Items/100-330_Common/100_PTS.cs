using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/PoiyomiToonShader", false, 100)]
        private static void importPTS()
        {
            client = new HttpClient(GitHubHeaders: true);
            foreach (string dir in Directory.GetDirectories(Application.dataPath, "OptimizedShaders", SearchOption.AllDirectories))
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
            string downloadURL = JSON.Parse(Task.Run(() => HttpClient.DownloadString(client, "https://api.github.com/repos/poiyomi/PoiyomiToonShader/releases/latest")).Result)["assets"][0]["browser_download_url"];
            ImportAsset(
                pathOrURL: downloadURL,
                assets: new string[] { "_PoiyomiShaders", "csc.rsp", "msc.rsp", "mcs.rsp", "../Thry" });
        }
    }
}
