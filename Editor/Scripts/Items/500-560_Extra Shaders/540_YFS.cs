using System.IO;
using UnityEngine;
using UnityEditor;
using kebinClient;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Extra Shaders/Legacy/Yukio's Fur Shader", false, 540)]
    private static void importYFS()
    {
        Selection.activeGameObject = null;
        client = new ModHttpClient();

        // Clean Up Old Files / Delete Pre-existing
        bool refreshRequired = false;
        if (Directory.Exists(Application.dataPath + @"/Yukio's Shaders"))
        {
            Directory.Delete(Application.dataPath + @"/Yukio's Shaders", true);
            refreshRequired = true;
        }
        if (File.Exists(Application.dataPath + @"/Yukio's Shaders.meta"))
        {
            File.Delete(Application.dataPath + @"/Yukio's Shaders.meta");
            refreshRequired = true;
        }
        if (refreshRequired == true)
            FSDSHandler(true);

        // Import Asset
        string package = "https://drive.google.com/uc?export=download&id=1PAjNYDTE6HJLOzETtCHwjylAz34606cT";
        ImportAsset(package);
    }
}
