using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/Pumkin's Avatar Tools", false, 310)]
        private static void importPAT()
        {
            client = new HttpClient(GitHubHeaders: true);
            string downloadURL = JSON.Parse(Task.Run(() => HttpClient.DownloadString(client, "https://api.github.com/repos/rurre/PumkinsAvatarTools/releases/latest")).Result)["assets"][0]["browser_download_url"];
            ImportAsset(
                pathOrURL: downloadURL,
                assets: new string[] { "PumkinsAvatarTools" });
        }
    }
}
