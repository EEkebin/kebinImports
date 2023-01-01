using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/More/Xiexes Unity Shaders", false, 400)]
        private static void importXUS()
        {
            client = new HttpClient(GitHubHeaders: true);
            string downloadURL = JSON.Parse(Task.Run(() => HttpClient.DownloadString(client, "https://api.github.com/repos/Xiexe/Xiexes-Unity-Shaders/releases/latest")).Result)["assets"][0]["browser_download_url"];
            ImportAsset(
                pathOrURL: downloadURL,
                assets: new string[] { "Xiexes-Unity-Shaders" },
                zippedFiles: true,
                name: "Xiexes-Unity-Shaders");
        }
    }
}
