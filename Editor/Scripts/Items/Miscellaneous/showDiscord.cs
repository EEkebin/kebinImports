using System.IO;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Discord", false, 150)]
    private static void showDiscord()
    {
        Application.OpenURL(JSON.Parse(File.ReadAllText(Application.dataPath + @"/kebinImports/Editor/Scripts/info.json"))["discord"]);
    }
}