using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/More/Legacy/Yukio's Fur Shader", false, 450)]
        private static void importYFS()
        {
            client = new HttpClient(GitHubHeaders: true);
            ImportAsset(
                pathOrURL: "https://drive.google.com/uc?export=download&id=1PAjNYDTE6HJLOzETtCHwjylAz34606cT",
                assets: new string[] { "Yukio's Shaders" });
        }
    }
}
