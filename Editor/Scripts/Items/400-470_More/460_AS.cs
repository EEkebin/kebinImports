using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/More/Legacy/Arktoon Shader", false, 460)]
        private static void importAS()
        {
            client = new HttpClient(GitHubHeaders: true);
            ImportAsset(
                pathOrURL: "https://web.archive.org/web/20210122100312/https://github-production-release-asset-2e65be.s3.amazonaws.com/143599529/ac33b000-05a6-11ea-9dfd-ee78d44c6c7f?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=AKIAIWNJYAX4CSVEH53A%2F20210122%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20210122T100311Z&X-Amz-Expires=300&X-Amz-Signature=8cf28b3a9693c75d341a5004ae7661ff2d8ef89e5c16ec146d3a828d4ad4ab72&X-Amz-SignedHeaders=host&actor_id=0&key_id=0&repo_id=143599529&response-content-disposition=attachment%3B%20filename%3Darktoon-shaders-1.0.2.6.unitypackage&response-content-type=application%2Foctet-stream",
                assets: new string[] { "arktoon Shaders" });
        }
    }
}
