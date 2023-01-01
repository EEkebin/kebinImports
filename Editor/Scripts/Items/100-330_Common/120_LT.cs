using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/lilToon", false, 100)]
        private static void importLT()
        {
            client = new HttpClient(GitHubHeaders: true);
            string downloadURL = JSON.Parse(Task.Run(() => HttpClient.DownloadString(client, "https://api.github.com/repos/lilxyzw/lilToon/releases/latest")).Result)["assets"][0]["browser_download_url"];
            ImportAsset(
                pathOrURL: downloadURL,
                assets: new string[] { "lilToon", "lilToonSetting", "csc.rsp", "msc.rsp", "mcs.rsp", "../Packages/lilToon", "../Packages/jp.lilxyzw.liltoon" },
                packages: new string[] { "jp.lilxyzw.liltoon" });
        }
    }
}
