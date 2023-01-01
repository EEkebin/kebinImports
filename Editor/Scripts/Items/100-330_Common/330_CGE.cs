using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/ComboGestureExpressions", false, 330)]
        private static void importCGE()
        {
            if (Directory.Exists(Application.dataPath + @"/Hai"))
            {
                if (Directory.Exists(Application.dataPath + @"/Hai/AnimationViewer"))
                {
                    Directory.Delete(Application.dataPath + @"/Hai/AnimationViewer", true);
                }
                if (File.Exists(Application.dataPath + @"/Hai/AnimationViewer.meta"))
                {
                    File.Delete(Application.dataPath + @"/Hai/AnimationViewer.meta");
                }
                if (Directory.Exists(Application.dataPath + @"/Hai/ComboGesture"))
                {
                    Directory.Delete(Application.dataPath + @"/Hai/ComboGesture", true);
                }
                if (File.Exists(Application.dataPath + @"/Hai/ComboGesture.meta"))
                {
                    File.Delete(Application.dataPath + @"/Hai/ComboGesture.meta");
                }
                if (Directory.Exists(Application.dataPath + @"/Hai/VisualExpressionsEditor"))
                {
                    Directory.Delete(Application.dataPath + @"/Hai/VisualExpressionsEditor", true);
                }
                if (File.Exists(Application.dataPath + @"/Hai/VisualExpressionsEditor.meta"))
                {
                    File.Delete(Application.dataPath + @"/Hai/VisualExpressionsEditor.meta");
                }
                if (Directory.GetFiles(Application.dataPath + @"/Hai").Length == 0 && Directory.GetDirectories(Application.dataPath + @"/Hai").Length == 0)
                {
                    Directory.Delete(Application.dataPath + @"/Hai", true);
                }
            }
            if (File.Exists(Application.dataPath + @"/Hai.meta"))
            {
                File.Delete(Application.dataPath + @"/Hai.meta");
            }
            client = new HttpClient(GitHubHeaders: true);
            string downloadURL = JSON.Parse(Task.Run(() => HttpClient.DownloadString(client, "https://api.github.com/repos/hai-vr/combo-gesture-expressions-av3/releases/latest")).Result)["assets"][0]["browser_download_url"];
            ImportAsset(
                pathOrURL: downloadURL,
                zippedAssetPackage: true);
        }
    }
}
