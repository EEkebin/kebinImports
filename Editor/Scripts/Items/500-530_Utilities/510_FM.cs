using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        private class FixMaterialsWindow : UnityEditor.EditorWindow
        {
            private static List<string> shaders;
            private static int _fromchoice = 0, _tochoice = 0;
            public static bool _deb = false;
            [MenuItem("kebinImports/Fix Materials", false, 510)]
            private static void ShowWindow()
            {
                EditorApplication.update -= FixMaterialsWindow.ShowWindow;
                FixMaterialsWindow window = EditorWindow.GetWindow<FixMaterialsWindow>(true, "kebinImports - Fix Materials");
                window.maxSize = new Vector2(400, 200);
                window.minSize = new Vector2(400, 200);
                FixMaterialsWindow._deb = false;
                window.Show();
            }
            private void OnGUI()
            {
                shaders = new List<string>();
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
                HashSet<string> excludedPaths = new HashSet<string>()
            {
                "Assets/_PoiyomiShaders/",
                "Assets/Toon/",
                "Assets/Yukio's Shaders/",
                "Assets/arktoon Shaders/",
                "Assets/Mochie/",
                "Assets/ReroShaders/",
                "Assets/Cubed's Unity Shaders/",
                "Assets/VRCSDK/",
                "Assets/VRChat Examples/",
                "Assets/Udon/",
                "Assets/SerializedUdonPrograms/",
                "Assets/lilToon/",
                "Assets/lilToonSetting/",
                "Assets/Hai/",
                "Assets/chocopoi/"
            };
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!excludedPaths.Contains(path))
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
}
