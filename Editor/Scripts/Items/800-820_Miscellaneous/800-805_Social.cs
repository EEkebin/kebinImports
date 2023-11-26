using System.IO;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        [MenuItem("kebinImports/Social/kebin.dev", false, 800)]
        private static void showSite()
        {
            Application.OpenURL(JSON.Parse(File.ReadAllText(installedPath + @"/package.json"))["author"]["url2"]);
        }
    }
}
