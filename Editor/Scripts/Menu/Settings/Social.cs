using System.IO;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/Social/kebin.dev", false, 810)]
        private static void showSite()
        {
            Application.OpenURL(JSON.Parse(File.ReadAllText(installedPath + @"/package.json"))["author"]["url2"]);
        }
        [MenuItem("kebinImports/Social/Discord", false, 811)]
        private static void showDiscord()
        {
            Application.OpenURL(JSON.Parse(File.ReadAllText(installedPath + @"/package.json"))["author"]["discord"]);
        }
    }
}
