using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public partial class kebinImports : MonoBehaviour
{
    [MenuItem("kebinImports/Fix Materials", false, 510)]
    private static void fixMaterials()
    {
        EditorApplication.update -= settingsWindow.showWindow;
        kebin_fixMaterials window = EditorWindow.GetWindow<kebin_fixMaterials>(true, "Fix Materials - kebinImports");
        window.maxSize = new Vector2(400, 200);
        window.minSize = new Vector2(400, 200);
        kebin_fixMaterials._deb = false;
        window.Show();
    }
    public class kebin_fixMaterials : UnityEditor.EditorWindow
    {
        Rect windowRect = new Rect(0, 0, 400, 400);
        List<string> shaders;
        int _fromchoice = 0, _tochoice = 0;
        public static bool _deb = false;
        void OnGUI()
        {
            shaders = new List<string>();
            // Create a DropDown MenuItem for each shaders and with /
            foreach (ShaderInfo shaderinfo in ShaderUtil.GetAllShaderInfo())
            {
                shaders.Add(shaderinfo.name);
            }
            GUILayout.Space(5);
            _fromchoice = EditorGUILayout.Popup("From:", _fromchoice, shaders.ToArray());
            GUIStyle buttonStyle = new GUIStyle("button");
            buttonStyle.fontSize = 28;
            if (GUILayout.Button("⇅", buttonStyle))
            {
                int temp = _tochoice;
                _tochoice = _fromchoice;
                _fromchoice = temp;
            }
            _tochoice = EditorGUILayout.Popup("To:", _tochoice, shaders.ToArray());
            if (_deb == false)
            {
                _deb = true;
                if (Directory.Exists(Application.dataPath + @"/_PoiyomiShaders"))
                {
                    _tochoice = shaders.FindLastIndex(x => x.Contains("Poiyomi Toon"));
                }
                else if (Directory.Exists(Application.dataPath + @"/lilToon"))
                {
                    _tochoice = shaders.FindIndex(x => x.Equals("lilToon"));
                }
                else
                {
                    _tochoice = shaders.FindIndex(x => x.Equals("Standard"));
                }
            }
            GUILayout.Space(15);
            EditorStyles.label.wordWrap = true;
            EditorGUILayout.LabelField("If you're suffering from pink or broken materials in your project/avatar/world, then please leave the From: shader as Hidden/InternalErrorShader and change To: shader to whatever you want to fix all materials to.");
            GUILayout.Space(10);
            if (GUILayout.Button("Fix All Materials"))
            {
                fixMaterials(shaders[_fromchoice], shaders[_tochoice]);
            }
        }
    }
    private static void fixMaterials(string shaderFrom, string shaderTo)
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
                    && !path.StartsWith("Assets/lilToonSetting")
                    && !path.StartsWith("Assets/Hai"))
                {
                    Material temp = (Material)AssetDatabase.LoadAssetAtPath(path, typeof(Material));
                    if (temp != null && shaderFrom != "Hidden/InternalErrorShader" && temp.shader.name == shaderFrom)
                    {
                        temp.shader = Shader.Find(shaderTo);
                    }
                    else if (temp != null && shaderFrom == "Hidden/InternalErrorShader" && (temp.shader.name == "" || temp.shader.name == "Hidden/InternalErrorShader" || temp.shader.name == null))
                    {
                        temp.shader = Shader.Find(shaderTo);
                    }
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
