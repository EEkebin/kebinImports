using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/Unity-Chan Toon Shader 2.0", false, 110)]
        private static void importUTS()
        {
            client = new HttpClient(GitHubHeaders: true);
            jsonNode = JSON.Parse(Task.Run(() => HttpClient.DownloadString(client, "https://api.github.com/repos/unity3d-jp/UnityChanToonShaderVer2_Project/releases")).Result);
            string downloadURL = "";
            for (int i = 0; i < jsonNode.Count; i++)
            {
                for (int j = 0; j < jsonNode[i]["assets"].Count; j++)
                {
                    if (jsonNode[i]["assets"][j]["browser_download_url"].ToString().Trim('\"').ToLower().Contains("shaderonly") && jsonNode[i]["assets"][j]["browser_download_url"].ToString().Trim('\"').ToLower().EndsWith(".unitypackage"))
                    {
                        downloadURL = jsonNode[i]["assets"][j]["browser_download_url"].ToString().Trim('\"');
                        break;
                    }
                }
                if (downloadURL != "")
                {
                    break;
                }
            }
            ImportAsset(
                pathOrURL: downloadURL,
                assets: new string[] { "Toon" });
        }
    }
}
