using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/More/Mochies Unity Shaders", false, 410)]
        private static void importMUS()
        {
            client = new HttpClient(GitHubHeaders: true);
            string downloadURL = JSON.Parse(Task.Run(() => HttpClient.DownloadString(client, "https://api.github.com/repos/MochiesCode/Mochies-Unity-Shaders/releases/latest")).Result)["assets"][0]["browser_download_url"];
            ImportAsset(
                pathOrURL: downloadURL,
                assets: new string[] { "Mochie", "csc.rsp" });
        }
    }
}
