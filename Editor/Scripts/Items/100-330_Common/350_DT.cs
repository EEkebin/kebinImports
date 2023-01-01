using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/DressingTools", false, 350)]
        private static void importDT()
        {
            if (Directory.Exists(Application.dataPath + @"/chocopoi"))
            {
                if (Directory.Exists(Application.dataPath + @"/chocopoi/DressingTools"))
                {
                    Directory.Delete(Application.dataPath + @"/chocopoi/DressingTools", true);
                }
                if (File.Exists(Application.dataPath + @"/chocopoi/DressingTools.meta"))
                {
                    File.Delete(Application.dataPath + @"/chocopoi/DressingTools.meta");
                }
                if (Directory.GetFiles(Application.dataPath + @"/chocopoi").Length == 0 && Directory.GetDirectories(Application.dataPath + @"/chocopoi").Length == 0)
                {
                    Directory.Delete(Application.dataPath + @"/chocopoi", true);
                }
            }
            client = new HttpClient(GitHubHeaders: true);
            string downloadURL = JSON.Parse(Task.Run(() => HttpClient.DownloadString(client, "https://api.github.com/repos/poi-vrc/DressingTools/releases/latest")).Result)["assets"][0]["browser_download_url"];
            ImportAsset(
                pathOrURL: downloadURL);
        }
    }
}
