using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        private class FixMaterialsWindow : UnityEditor.EditorWindow
        {
            private static List<string> shaders;
            private static int _fromchoice = 0, _tochoice = 0;
            public static bool _deb = false;
            [MenuItem("kebinImports/Fix Materials", false, 600)]
            private static void ShowWindow()
            {
                EditorApplication.update -= FixMaterialsWindow.ShowWindow;
                FixMaterialsWindow window = EditorWindow.GetWindow<FixMaterialsWindow>(true, "kebinImports - Fix Materials");
                SizeWindow(window, 400, 200, true);
                FixMaterialsWindow._deb = false;
                window.Show();
            }
            private static GUIStyle wrapLabel;
            private void OnGUI()
            {
                SizeWindow(this, 400, 200, true);
                BeginScaledGUI(this);
                try { DrawContents(); }
                finally { EndScaledGUI(); }
            }
            private void DrawContents()
            {
                shaders = new List<string>();
                foreach (ShaderInfo shaderinfo in ShaderUtil.GetAllShaderInfo()) shaders.Add(shaderinfo.name);
                if (shaders.Count == 0) { EditorGUILayout.HelpBox("No shaders found.", MessageType.None); return; }
                if (_deb == false)
                {
                    // Defaults: repair broken materials, onto the first installed shader of the usual choices.
                    _deb = true;
                    _fromchoice = Mathf.Max(0, shaders.IndexOf("Hidden/InternalErrorShader"));
                    string[] preferred = { "Toon (Built-in)", "lilToon", ".poiyomi/Poiyomi Toon", "Standard" };
                    _tochoice = -1;
                    foreach (string name in preferred) { _tochoice = shaders.IndexOf(name); if (_tochoice >= 0) break; }
                    if (_tochoice < 0) _tochoice = shaders.FindIndex(x => x.Contains("Poiyomi Toon"));
                }
                _fromchoice = Mathf.Clamp(_fromchoice, 0, shaders.Count - 1);
                _tochoice = Mathf.Clamp(_tochoice, 0, shaders.Count - 1);
                GUILayout.Space(5);
                _fromchoice = EditorGUILayout.Popup("From:", _fromchoice, shaders.ToArray());
                GUIStyle buttonStyle = new GUIStyle("button") { fontSize = 28 };
                if (GUILayout.Button("⇅", buttonStyle))
                {
                    int temp = _tochoice;
                    _tochoice = _fromchoice;
                    _fromchoice = temp;
                }
                _tochoice = EditorGUILayout.Popup("To:", _tochoice, shaders.ToArray());
                GUILayout.Space(15);
                if (wrapLabel == null) wrapLabel = new GUIStyle(EditorStyles.label) { wordWrap = true };
                EditorGUILayout.LabelField("For pink or broken materials, leave From: as Hidden/InternalErrorShader and pick the shader to move them to. Materials inside Packages/ are never changed.", wrapLabel);
                GUILayout.Space(10);
                if (GUILayout.Button("Fix All Materials"))
                {
                    string from = shaders[_fromchoice], to = shaders[_tochoice];
                    EditorApplication.delayCall += () =>
                    {
                        int changed = fixMaterials(from, to);
                        if (changed < 0) return;
                        EditorUtility.DisplayDialog("kebinImports", changed == 0
                            ? "No materials in Assets/ use " + (from == "Hidden/InternalErrorShader" ? "a missing shader" : "\"" + from + "\"") + ", so nothing was changed."
                            : "Switched " + changed + (changed == 1 ? " material" : " materials") + " to \"" + to + "\".", "Ok");
                    };
                }
            }
        }
        // Returns how many materials changed, or -1 when the target shader doesn't exist.
        private static int fixMaterials(string shaderFrom, string shaderTo)
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
                Shader target = Shader.Find(shaderTo);
                if (target == null)
                {
                    EditorUtility.DisplayDialog("kebinImports", "The shader \"" + shaderTo + "\" could not be found, so no materials were changed.", "Ok");
                    return -1;
                }
                int fixedCount = 0;
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase)) continue; // never rewrite package materials
                    bool excluded = false;
                    foreach (string excludedPath in excludedPaths)
                    {
                        if (path.StartsWith(excludedPath, System.StringComparison.OrdinalIgnoreCase)) { excluded = true; break; }
                    }
                    if (excluded) continue;
                    Material temp = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (temp == null) continue;
                    string currentShader = temp.shader != null ? temp.shader.name : "";
                    bool matches = shaderFrom == "Hidden/InternalErrorShader"
                        ? currentShader == "" || currentShader == "Hidden/InternalErrorShader"
                        : currentShader == shaderFrom;
                    if (matches)
                    {
                        temp.shader = target;
                        EditorUtility.SetDirty(temp);
                        fixedCount++;
                    }
                }
                AssetDatabase.SaveAssets();
                Debug.Log("[kebinImports] Fix Materials changed " + fixedCount + (fixedCount == 1 ? " material" : " materials") + " from \"" + shaderFrom + "\" to \"" + shaderTo + "\".");
                return fixedCount;
            }
            return 0;
        }
    }
}
