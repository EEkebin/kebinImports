using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/Modular Avatar", false, 340)]
        private static void importMA()
        {
            client = new HttpClient(GitHubHeaders: true);
            string downloadURL = JSON.Parse(Task.Run(() => HttpClient.DownloadString(client, "https://api.github.com/repos/bdunderscore/modular-avatar/releases/latest")).Result)["assets"][0]["browser_download_url"];
            ImportAsset(
                pathOrURL: downloadURL,
                assets: new string[] { "../Packages/Modular Avatar", "../Packages/nadena.dev.modular-avatar" },
                packages: new string[] { "nadena.dev.modular-avatar" });
        }
    }
}
