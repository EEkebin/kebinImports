using System.IO;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports
    {
        [MenuItem("kebinImports/Donate/PayPal", false, 820)]
        private static void donatePayPal()
        {
            Application.OpenURL(JSON.Parse(File.ReadAllText(installedPath + @"/package.json"))["author"]["donate"]["paypal"]);
        }
        [MenuItem("kebinImports/Donate/Cash App", false, 821)]
        private static void donateCashApp()
        {
            Application.OpenURL(JSON.Parse(File.ReadAllText(installedPath + @"/package.json"))["author"]["donate"]["cashapp"]);
        }
    }
}
