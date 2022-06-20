using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using kebinClient;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/ComboGestureExpressions", false, 330)]
    private static void importCGE()
    {
        Selection.activeGameObject = null;
        client = new ModHttpClient(true);

        // Clean Up Old Files / Delete Pre-existing
        bool refreshRequired = false;
        if (Directory.Exists(Application.dataPath + @"/Hai"))
        {
            if (Directory.Exists(Application.dataPath + @"/Hai/AnimationViewer"))
            {
                Directory.Delete(Application.dataPath + @"/Hai/AnimationViewer", true);
                refreshRequired = true;
            }
            if (File.Exists(Application.dataPath + @"/Hai/AnimationViewer.meta"))
            {
                File.Delete(Application.dataPath + @"/Hai/AnimationViewer.meta");
                refreshRequired = true;
            }
            if (Directory.Exists(Application.dataPath + @"/Hai/ComboGesture"))
            {
                Directory.Delete(Application.dataPath + @"/Hai/ComboGesture", true);
                refreshRequired = true;
            }
            if (File.Exists(Application.dataPath + @"/Hai/ComboGesture.meta"))
            {
                File.Delete(Application.dataPath + @"/Hai/ComboGesture.meta");
                refreshRequired = true;
            }
            if (Directory.Exists(Application.dataPath + @"/Hai/VisualExpressionsEditor"))
            {
                Directory.Delete(Application.dataPath + @"/Hai/VisualExpressionsEditor", true);
                refreshRequired = true;
            }
            if (File.Exists(Application.dataPath + @"/Hai/VisualExpressionsEditor.meta"))
            {
                File.Delete(Application.dataPath + @"/Hai/VisualExpressionsEditor.meta");
                refreshRequired = true;
            }
            if (Directory.GetFiles(Application.dataPath + @"/Hai").Length == 0 && Directory.GetDirectories(Application.dataPath + @"/Hai").Length == 0)
            {
                Directory.Delete(Application.dataPath + @"/Hai", true);
            }
        }
        if (File.Exists(Application.dataPath + @"/Hai.meta"))
        {
            File.Delete(Application.dataPath + @"/Hai.meta");
            refreshRequired = true;
        }
        if (refreshRequired == true)
            FSDSHandler(true);

        // Import Asset
        string repoLink = "https://api.github.com/repos/hai-vr/combo-gesture-expressions-av3/releases/latest";
        ImportAsset(JSON.Parse(Task.Run(() => ModHttpClient.DownloadString(client, repoLink)).Result)["assets"][0]["browser_download_url"], false, true);
    }
}
