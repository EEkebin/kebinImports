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
            jsonNode = JSON.Parse(Task.Run(() => HttpClient.DownloadString(client, "https://api.github.com/repos/lilxyzw/lilToon/releases/latest")).Result)["assets"];
            string downloadURL = "";
            for (int i = 0; i < jsonNode.Count; i++)
            {
                if (jsonNode[i]["browser_download_url"].ToString().Trim('\"').ToLower().EndsWith(".unitypackage"))
                {
                    downloadURL = jsonNode[i]["browser_download_url"].ToString().Trim('\"');
                    break;
                }
                if (downloadURL != "")
                {
                    break;
                }
            }
            ImportAsset(
                pathOrURL: downloadURL,
                assets: new string[] { "lilToon", "lilToonSetting", "csc.rsp", "msc.rsp", "mcs.rsp", "../Packages/lilToon", "../Packages/jp.lilxyzw.liltoon" },
                packages: new string[] { "jp.lilxyzw.liltoon" });
        }
    }
}
