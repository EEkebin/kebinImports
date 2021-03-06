using System.IO;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Donate/PayPal", false, 810)]
    private static void donatePayPal()
    {
        Application.OpenURL(JSON.Parse(File.ReadAllText(Application.dataPath + @"/kebinImports/Editor/Scripts/info.json"))["paypal"]);
    }

    [MenuItem("kebinImports/Donate/Cash App", false, 815)]
    private static void donateCashApp()
    {
        Application.OpenURL(JSON.Parse(File.ReadAllText(Application.dataPath + @"/kebinImports/Editor/Scripts/info.json"))["cashapp"]);
    }
}
