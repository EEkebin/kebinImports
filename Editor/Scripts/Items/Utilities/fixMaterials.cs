using System.IO;
using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Fix All Materials/Poiyomi Toon Shader", false, 66)]
    private static void FAMPTS()
    {
        fixAllMaterials(".poiyomi/• Poiyomi Toon •");
    }
    [MenuItem("kebinImports/Fix All Materials/Poiyomi Toon Shader", true, 66)]
    private static bool ValidateFAMPTS()
    {
        return Directory.Exists(Application.dataPath + @"/_PoiyomiShaders");
    }
    [MenuItem("kebinImports/Fix All Materials/lilToon", false, 67)]
    private static void FAMLT()
    {
        fixAllMaterials("_lil/lilToonMulti");
    }
    [MenuItem("kebinImports/Fix All Materials/lilToon", true, 67)]
    private static bool ValidateFAMLT()
    {
        return Directory.Exists(Application.dataPath + @"/lilToon");
    }
    private static void fixAllMaterials(string shaderName)
    {
        string[] guids = AssetDatabase.FindAssets("t: material");
        if (guids.Length >= 1)
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/_PoiyomiShaders/")
                    && !path.StartsWith("Assets/Toon/")
                    && !path.StartsWith("Assets/Yukio's Shaders/")
                    && !path.StartsWith("Assets/arktoon Shaders/")
                    && !path.StartsWith("Assets/Mochie/")
                    && !path.StartsWith("Assets/ReroShaders/")
                    && !path.StartsWith("Assets/Cubed's Unity Shaders/")
                    && !path.StartsWith("Assets/VRCSDK/")
                    && !path.StartsWith("Assets/VRChat Examples/")
                    && !path.StartsWith("Assets/Udon/")
                    && !path.StartsWith("Assets/SerializedUdonPrograms/")
                    && !path.StartsWith("Assets/lilToon")
                    && !path.StartsWith("Assets/lilToonSetting"))
                {
                    Material temp = (Material)AssetDatabase.LoadAssetAtPath(path, typeof(Material));
                    if (temp != null && (temp.shader.name == "" || temp.shader.name == "Hidden/InternalErrorShader" || temp.shader.name == null))
                    {
                        temp.shader = Shader.Find(shaderName);
                    }
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
    [MenuItem("kebinImports/Fix All Materials/Standard", false, 80)]
    private static void FAMStandard()
    {
        fixAllMaterials("Standard");
    }
}