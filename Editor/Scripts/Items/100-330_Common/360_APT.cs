using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/Avatar Performance Tools", false, 360)]
        private static void importAPT()
        {
            client = new HttpClient(GitHubHeaders: true);
            jsonNode = JSON.Parse(Task.Run(() => HttpClient.DownloadString(client, "https://api.github.com/repos/Thryrallo/VRC-Avatar-Performance-Tools/releases/latest")).Result)["assets"];
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
                assets: new string[] { "../Packages/de.thryrallo.vrc.avatar-performance-tools" },
                packages: new string[] { "de.thryrallo.vrc.avatar-performance-tools" });
        }
    }
}
