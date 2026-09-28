using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using SimpleJSON;
using Object = UnityEngine.Object;

namespace kebinImports
{
    public partial class kebinImports
    {
        internal class AITool
        {
            public string Name;
            public string Description;
            public JSONNode Schema;
            // Mutating tools change the scene, assets or files and go through the approval prompt.
            public bool Mutating;
            public Func<JSONNode, string> Run;
        }

        // Everything the assistant can do in the editor. Every handler runs on the main thread and returns text
        // for the model; anything it changes is recorded with Undo where Unity supports it.
        internal static class AITools
        {
            public static readonly List<AITool> All = new List<AITool>();
            private const int MaxTextResult = 24000;

            public static AITool Find(string name) => All.FirstOrDefault(t => t.Name == name);

            public static string Execute(AIToolCall call, Func<AITool, AIToolCall, bool> approve, out bool isError)
            {
                AITool tool = Find(call.Name);
                if (tool == null)
                {
                    isError = true;
                    return "Unknown tool '" + call.Name + "'. Available tools: " + string.Join(", ", All.Select(t => t.Name));
                }
                if (tool.Mutating && approve != null && !approve(tool, call))
                {
                    isError = true;
                    return "The user declined this action. Ask them how they would like to proceed.";
                }
                try
                {
                    string result = tool.Run(call.Arguments ?? new JSONObject()) ?? "ok";
                    isError = false;
                    return result.Length > MaxTextResult ? result.Substring(0, MaxTextResult) + "\n… (truncated, narrow the request)" : result;
                }
                catch (Exception e)
                {
                    isError = true;
                    return "Error: " + e.Message;
                }
            }

            // ---------------------------------------------------------------- registration
            static AITools()
            {
                Register("get_project_info", "Unity version, project folders, installed packages, open scenes and whether this is a VRChat Creator Companion project.", false,
                    Schema(), _ => GetProjectInfo());
                Register("get_selection", "The objects currently selected in the editor: scene paths and asset paths.", false,
                    Schema(), _ => GetSelection());
                Register("get_hierarchy", "The scene hierarchy as an indented tree with component names. Use root to start at a specific object.", false,
                    Schema("root", "string", "Scene path of the object to start from (Root/Child). Omit for the whole scene.", "depth", "integer", "How many levels to show (default 3)."),
                    a => GetHierarchy(a));
                Register("find_objects", "Find scene objects (including inactive ones) by name substring and/or component type.", false,
                    Schema("name_contains", "string", "Case-insensitive substring of the object name.", "component_type", "string", "Component type name, e.g. VRCPhysBone, SkinnedMeshRenderer.", "limit", "integer", "Maximum results (default 50)."),
                    a => FindObjects(a));
                Register("get_components", "List the components on a scene object with their index and enabled state.", false,
                    Schema("object*", "string", "Scene path of the object (Root/Child)."),
                    a => GetComponents(a));
                Register("get_component_properties", "Read every serialized property of a component as JSON. Property paths returned here are what set_component_property expects. Works for any component, including VRChat SDK ones such as VRCPhysBone.", false,
                    Schema("object*", "string", "Scene path of the object.", "component*", "string", "Component type name, e.g. VRCPhysBone or Transform.", "index", "integer", "Which one when several components of that type exist (default 0).", "filter", "string", "Only include properties whose path contains this text."),
                    a => GetComponentProperties(a));
                Register("set_component_property", "Set one serialized property on a component. Read the component first to learn property paths and value shapes. Values: numbers, booleans, strings, enum names, {\"r\",\"g\",\"b\",\"a\"} colors, {\"x\",\"y\",\"z\",\"w\"} vectors, object references as an asset path (Assets/...) or scene path (Root/Child, optionally {\"object\":path,\"component\":type}), arrays as JSON arrays, nested objects as JSON objects.", true,
                    Schema("object*", "string", "Scene path of the object.", "component*", "string", "Component type name.", "index", "integer", "Which one when several exist (default 0).", "property_path*", "string", "Serialized property path, e.g. m_LocalPosition or pull or colliders.", "value*", "any", "The new value."),
                    a => SetComponentProperty(a));
                Register("add_component", "Add a component to a scene object by type name.", true,
                    Schema("object*", "string", "Scene path of the object.", "type*", "string", "Component type name, e.g. VRCPhysBone."),
                    a => AddComponent(a));
                Register("remove_component", "Remove a component from a scene object.", true,
                    Schema("object*", "string", "Scene path of the object.", "component*", "string", "Component type name.", "index", "integer", "Which one when several exist (default 0)."),
                    a => RemoveComponent(a));
                Register("set_active", "Enable or disable a scene object.", true,
                    Schema("object*", "string", "Scene path of the object.", "active*", "boolean", "true to enable, false to disable."),
                    a => SetActive(a));
                Register("create_gameobject", "Create an empty scene object, optionally under a parent.", true,
                    Schema("name*", "string", "Name of the new object.", "parent", "string", "Scene path of the parent."),
                    a => CreateGameObject(a));
                Register("list_assets", "Search project assets with an AssetDatabase filter such as 't:Material', 't:Shader', 't:Texture2D', 't:Prefab' or a name, optionally inside a folder.", false,
                    Schema("filter*", "string", "AssetDatabase search filter, e.g. 't:Material Body' or 'MyAvatar t:Prefab'.", "folder", "string", "Folder to search in, e.g. Assets/MyAvatar.", "limit", "integer", "Maximum results (default 100)."),
                    a => ListAssets(a));
                Register("get_material", "Read a material: shader, render queue, keywords and property values. Shaders like Poiyomi have hundreds of properties, so use filter.", false,
                    Schema("path*", "string", "Asset path of the material.", "filter", "string", "Only properties whose name or description contains this text.", "limit", "integer", "Maximum properties to return (default 150)."),
                    a => GetMaterial(a));
                Register("set_material", "Change a material: shader, properties, keywords or render queue. Read it first so property names are right. Texture values are asset paths; colors are {\"r\",\"g\",\"b\",\"a\"} in 0-1 or \"#RRGGBBAA\".", true,
                    Schema("path*", "string", "Asset path of the material.", "shader", "string", "New shader name, e.g. .poiyomi/Poiyomi Toon.", "properties", "object", "Map of property name to value.", "enable_keywords", "array:string", "Shader keywords to enable.", "disable_keywords", "array:string", "Shader keywords to disable.", "render_queue", "integer", "Render queue, or -1 for the shader default."),
                    a => SetMaterial(a));
                Register("create_material", "Create a new material asset with a shader.", true,
                    Schema("path*", "string", "Asset path ending in .mat, e.g. Assets/Materials/New.mat.", "shader*", "string", "Shader name."),
                    a => CreateMaterial(a));
                Register("list_shaders", "List shader names available in the project, with whether they have compile errors.", false,
                    Schema("filter", "string", "Case-insensitive substring to match.", "limit", "integer", "Maximum results (default 200)."),
                    a => ListShaders(a));
                Register("get_shader_errors", "Compile errors and warnings for a shader, by shader name or asset path.", false,
                    Schema("shader*", "string", "Shader name (e.g. Standard) or asset path (Assets/.../Foo.shader)."),
                    a => GetShaderErrors(a));
                Register("read_file", "Read a text file in the project (shaders, scripts, JSON, ...). Paths are relative to the project folder.", false,
                    Schema("path*", "string", "Project-relative path, e.g. Assets/Shaders/Fur.shader.", "start_line", "integer", "First line to return (1-based, default 1).", "max_lines", "integer", "Maximum lines to return (default 400)."),
                    a => ReadFile(a));
                Register("write_file", "Create or overwrite a text file under Assets/ or Packages/. Use it for shaders, scripts and config files. Unity reimports the file afterwards; check get_shader_errors or get_console_log for problems.", true,
                    Schema("path*", "string", "Project-relative path under Assets/ or Packages/.", "content*", "string", "The complete file contents."),
                    a => WriteFile(a));
                Register("find_in_files", "Search text files under Assets/ and Packages/ with a regular expression.", false,
                    Schema("pattern*", "string", ".NET regular expression.", "folder", "string", "Folder to search (default Assets).", "extensions", "array:string", "File extensions to include, e.g. [\".shader\", \".cs\"] (default: shader, cginc, hlsl, cs, json, asmdef, txt, md).", "limit", "integer", "Maximum matching lines (default 100)."),
                    a => FindInFiles(a));
                Register("get_console_log", "Recent Unity console messages, newest last. Use it after changes to look for errors.", false,
                    Schema("count", "integer", "How many messages (default 40).", "errors_only", "boolean", "Only errors and exceptions."),
                    a => GetConsoleLog(a));
                Register("run_menu_item", "Run a Unity editor menu item by its path, e.g. 'kebinImports/Fix Materials' or 'Assets/Refresh'.", true,
                    Schema("path*", "string", "Menu path."),
                    a => RunMenuItem(a));
                Register("save_all", "Save all open scenes and modified assets to disk.", true,
                    Schema(), _ => SaveAll());
                Register("undo", "Undo the last editor change (same as Ctrl+Z).", true,
                    Schema(), _ => { Undo.PerformUndo(); return "Undid the last change."; });
                // kebinImports' own capabilities: the importer, Essentials and utilities.
                AIKebinTools.Register(Register, Schema);
            }
            public static GameObject FindSceneObjectPublic(string path) => FindSceneObject(path);

            private static void Register(string name, string description, bool mutating, JSONNode schema, Func<JSONNode, string> run)
            {
                All.Add(new AITool { Name = name, Description = description, Mutating = mutating, Schema = schema, Run = run });
            }

            // Builds a JSON schema from (name, type, description) triples. A trailing * on the name marks it required.
            // Types: string, integer, number, boolean, object, any, array:<type>.
            private static JSONNode Schema(params string[] def)
            {
                JSONObject schema = new JSONObject();
                schema["type"] = "object";
                JSONObject props = new JSONObject();
                JSONArray required = new JSONArray();
                for (int i = 0; i + 2 < def.Length; i += 3)
                {
                    string name = def[i];
                    bool req = name.EndsWith("*");
                    if (req) name = name.Substring(0, name.Length - 1);
                    JSONObject p = new JSONObject();
                    string type = def[i + 1];
                    if (type.StartsWith("array:"))
                    {
                        p["type"] = "array";
                        JSONObject items = new JSONObject();
                        items["type"] = type.Substring(6);
                        p["items"] = items;
                    }
                    else if (type == "object")
                    {
                        p["type"] = "object";
                        p["additionalProperties"] = true;
                    }
                    else if (type != "any")
                    {
                        p["type"] = type;
                    }
                    p["description"] = def[i + 2];
                    props[name] = p;
                    if (req) required.Add(name);
                }
                schema["properties"] = props;
                if (required.Count > 0) schema["required"] = required;
                return schema;
            }

            // ---------------------------------------------------------------- argument helpers
            private static string Str(JSONNode a, string key, string fallback = null)
            {
                if (!a.HasKey(key) || a[key].IsNull) return fallback;
                return a[key].IsString ? a[key].Value : a[key].ToString();
            }
            private static int Int(JSONNode a, string key, int fallback)
            {
                if (!a.HasKey(key) || a[key].IsNull) return fallback;
                if (a[key].IsNumber) return a[key].AsInt;
                int v;
                return int.TryParse(a[key].Value, out v) ? v : fallback;
            }
            private static string Require(JSONNode a, string key)
            {
                string v = Str(a, key);
                if (string.IsNullOrEmpty(v)) throw new ArgumentException("Missing argument '" + key + "'.");
                return v;
            }

            // ---------------------------------------------------------------- scene helpers
            private static IEnumerable<GameObject> SceneRoots()
            {
                PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage != null)
                {
                    yield return stage.prefabContentsRoot;
                    yield break;
                }
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;
                    foreach (GameObject go in scene.GetRootGameObjects()) yield return go;
                }
            }
            private static IEnumerable<GameObject> AllSceneObjects()
            {
                foreach (GameObject root in SceneRoots())
                {
                    foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) yield return t.gameObject;
                }
            }
            public static string PathOf(Transform t)
            {
                if (t == null) return "";
                string path = t.name;
                while (t.parent != null)
                {
                    t = t.parent;
                    path = t.name + "/" + path;
                }
                return path;
            }
            private static GameObject FindSceneObject(string path)
            {
                if (string.IsNullOrEmpty(path)) throw new ArgumentException("An object path is required.");
                path = path.Trim().TrimStart('/');
                string[] parts = path.Split('/');
                foreach (GameObject root in SceneRoots())
                {
                    if (root.name != parts[0]) continue;
                    Transform t = root.transform;
                    bool ok = true;
                    for (int i = 1; i < parts.Length && ok; i++)
                    {
                        Transform next = null;
                        foreach (Transform child in t)
                        {
                            if (child.name == parts[i]) { next = child; break; }
                        }
                        if (next == null) ok = false; else t = next;
                    }
                    if (ok) return t.gameObject;
                }
                // Fall back to a unique name match anywhere in the scene.
                List<GameObject> byName = AllSceneObjects().Where(g => string.Equals(g.name, parts[parts.Length - 1], StringComparison.OrdinalIgnoreCase)).ToList();
                if (byName.Count == 1) return byName[0];
                if (byName.Count > 1) throw new ArgumentException("'" + path + "' is ambiguous; candidates: " + string.Join(", ", byName.Take(10).Select(g => PathOf(g.transform))));
                throw new ArgumentException("No scene object at '" + path + "'. Use get_hierarchy or find_objects to look it up.");
            }
            private static bool TypeMatches(Type type, string name)
            {
                for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
                {
                    if (string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase) || string.Equals(t.FullName, name, StringComparison.OrdinalIgnoreCase)) return true;
                }
                return false;
            }
            private static Component FindComponent(GameObject go, string typeName, int index)
            {
                List<Component> matches = go.GetComponents<Component>().Where(c => c != null && c.GetType().Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) || c != null && c.GetType().FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matches.Count == 0) matches = go.GetComponents<Component>().Where(c => c != null && TypeMatches(c.GetType(), typeName)).ToList();
                if (matches.Count == 0) throw new ArgumentException("No " + typeName + " on '" + PathOf(go.transform) + "'. Components: " + string.Join(", ", go.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name)));
                if (index < 0 || index >= matches.Count) throw new ArgumentException("Index " + index + " is out of range; there are " + matches.Count + " " + typeName + " components.");
                return matches[index];
            }
            private static Type FindType(string name, Type mustBe)
            {
                Type exact = null, byName = null;
                foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try { types = asm.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
                    foreach (Type t in types)
                    {
                        if (mustBe != null && !mustBe.IsAssignableFrom(t)) continue;
                        if (t.IsAbstract || t.IsGenericTypeDefinition) continue;
                        if (t.FullName == name) { exact = t; break; }
                        if (t.Name == name || t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) byName = byName ?? t;
                    }
                    if (exact != null) break;
                }
                return exact ?? byName;
            }

            // ---------------------------------------------------------------- project
            private static string GetProjectInfo()
            {
                JSONObject o = new JSONObject();
                o["unityVersion"] = Application.unityVersion;
                o["projectPath"] = ProjectPath;
                o["projectName"] = Path.GetFileName(ProjectPath);
                o["creatorCompanionProject"] = isVRCCreatorCompanion;
                o["renderPipeline"] = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null ? "Built-in" : UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name;
                JSONArray scenes = new JSONArray();
                for (int i = 0; i < SceneManager.sceneCount; i++) scenes.Add(SceneManager.GetSceneAt(i).path);
                o["openScenes"] = scenes;
                JSONArray packages = new JSONArray();
                if (Directory.Exists(PackagesPath))
                {
                    foreach (string dir in Directory.GetDirectories(PackagesPath))
                    {
                        string pj = Path.Combine(dir, "package.json");
                        if (!File.Exists(pj)) continue;
                        JSONNode p = JSON.Parse(File.ReadAllText(pj));
                        packages.Add(Path.GetFileName(dir) + (p != null && p.HasKey("version") ? " " + p["version"].Value : ""));
                    }
                }
                o["embeddedPackages"] = packages;
                JSONArray topFolders = new JSONArray();
                foreach (string dir in Directory.GetDirectories(Application.dataPath)) topFolders.Add("Assets/" + Path.GetFileName(dir));
                o["assetFolders"] = topFolders;
                return o.ToString(2);
            }
            private static string GetSelection()
            {
                JSONArray arr = new JSONArray();
                foreach (Object obj in Selection.objects)
                {
                    JSONObject o = new JSONObject();
                    o["name"] = obj.name;
                    o["type"] = obj.GetType().Name;
                    GameObject go = obj as GameObject;
                    string assetPath = AssetDatabase.GetAssetPath(obj);
                    if (go != null && string.IsNullOrEmpty(assetPath)) o["scenePath"] = PathOf(go.transform);
                    if (!string.IsNullOrEmpty(assetPath)) o["assetPath"] = assetPath;
                    arr.Add(o);
                }
                return arr.Count == 0 ? "Nothing is selected." : arr.ToString(2);
            }
            private static string GetHierarchy(JSONNode a)
            {
                int depth = Int(a, "depth", 3);
                string root = Str(a, "root");
                StringBuilder sb = new StringBuilder();
                int lines = 0;
                IEnumerable<GameObject> roots = string.IsNullOrEmpty(root) ? SceneRoots() : new[] { FindSceneObject(root) };
                foreach (GameObject go in roots) AppendHierarchy(sb, go.transform, 0, depth, ref lines);
                if (lines == 0) sb.Append("The scene is empty.");
                return sb.ToString();
            }
            private static void AppendHierarchy(StringBuilder sb, Transform t, int level, int maxDepth, ref int lines)
            {
                if (lines >= 400) { if (lines == 400) sb.AppendLine("… (more objects; use root/depth to narrow)"); lines++; return; }
                string comps = string.Join(", ", t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name));
                sb.Append(' ', level * 2).Append(t.name);
                if (comps.Length > 0) sb.Append(" [").Append(comps).Append(']');
                if (!t.gameObject.activeSelf) sb.Append(" (inactive)");
                if (level >= maxDepth && t.childCount > 0) sb.Append(" (+" + t.childCount + " children)");
                sb.AppendLine();
                lines++;
                if (level >= maxDepth) return;
                foreach (Transform child in t) AppendHierarchy(sb, child, level + 1, maxDepth, ref lines);
            }
            private static string FindObjects(JSONNode a)
            {
                string name = Str(a, "name_contains");
                string type = Str(a, "component_type");
                int limit = Int(a, "limit", 50);
                StringBuilder sb = new StringBuilder();
                int count = 0;
                foreach (GameObject go in AllSceneObjects())
                {
                    if (!string.IsNullOrEmpty(name) && go.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (!string.IsNullOrEmpty(type) && !go.GetComponents<Component>().Any(c => c != null && TypeMatches(c.GetType(), type))) continue;
                    sb.AppendLine(PathOf(go.transform) + (go.activeInHierarchy ? "" : " (inactive)"));
                    if (++count >= limit) { sb.AppendLine("… (limit reached)"); break; }
                }
                return count == 0 ? "No matching objects." : sb.ToString();
            }
            private static string GetComponents(JSONNode a)
            {
                GameObject go = FindSceneObject(Require(a, "object"));
                Component[] comps = go.GetComponents<Component>();
                StringBuilder sb = new StringBuilder();
                Dictionary<string, int> perType = new Dictionary<string, int>();
                for (int i = 0; i < comps.Length; i++)
                {
                    if (comps[i] == null) { sb.AppendLine("(missing script)"); continue; }
                    string tn = comps[i].GetType().Name;
                    int idx; perType.TryGetValue(tn, out idx); perType[tn] = idx + 1;
                    Behaviour b = comps[i] as Behaviour;
                    sb.AppendLine(tn + " index=" + idx + (b != null && !b.enabled ? " (disabled)" : "") + "  [" + comps[i].GetType().FullName + "]");
                }
                return sb.ToString();
            }

            // ---------------------------------------------------------------- serialized properties
            private static string GetComponentProperties(JSONNode a)
            {
                GameObject go = FindSceneObject(Require(a, "object"));
                Component c = FindComponent(go, Require(a, "component"), Int(a, "index", 0));
                string filter = Str(a, "filter");
                SerializedObject so = new SerializedObject(c);
                JSONObject o = new JSONObject();
                SerializedProperty it = so.GetIterator();
                bool enter = true;
                int count = 0;
                while (it.NextVisible(enter))
                {
                    enter = false;
                    if (it.name == "m_Script") continue;
                    if (!string.IsNullOrEmpty(filter) && it.propertyPath.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 && it.displayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    o[it.propertyPath] = PropToJson(it, 3);
                    if (++count >= 300) { o["…"] = "more properties omitted; use filter"; break; }
                }
                return o.ToString(2);
            }
            private static JSONNode PropToJson(SerializedProperty p, int depth)
            {
                switch (p.propertyType)
                {
                    case SerializedPropertyType.Integer: return p.longValue;
                    case SerializedPropertyType.Boolean: return p.boolValue;
                    case SerializedPropertyType.Float: return p.doubleValue;
                    case SerializedPropertyType.String: return p.stringValue;
                    case SerializedPropertyType.Color: return ColorToJson(p.colorValue);
                    case SerializedPropertyType.ObjectReference: return RefToJson(p.objectReferenceValue);
                    case SerializedPropertyType.LayerMask: return p.intValue;
                    case SerializedPropertyType.Enum:
                    {
                        JSONObject e = new JSONObject();
                        string[] names = p.enumDisplayNames;
                        e["enum"] = p.enumValueIndex >= 0 && p.enumValueIndex < names.Length ? names[p.enumValueIndex] : p.enumValueIndex.ToString();
                        JSONArray opts = new JSONArray();
                        foreach (string n in names) opts.Add(n);
                        e["options"] = opts;
                        return e;
                    }
                    case SerializedPropertyType.Vector2: return VecToJson(p.vector2Value);
                    case SerializedPropertyType.Vector3: return VecToJson(p.vector3Value);
                    case SerializedPropertyType.Vector4: return VecToJson(p.vector4Value);
                    case SerializedPropertyType.Quaternion: { Vector4 v = new Vector4(p.quaternionValue.x, p.quaternionValue.y, p.quaternionValue.z, p.quaternionValue.w); return VecToJson(v); }
                    case SerializedPropertyType.Rect: { JSONObject r = new JSONObject(); r["x"] = p.rectValue.x; r["y"] = p.rectValue.y; r["width"] = p.rectValue.width; r["height"] = p.rectValue.height; return r; }
                    case SerializedPropertyType.Bounds: { JSONObject b = new JSONObject(); b["center"] = VecToJson(p.boundsValue.center); b["size"] = VecToJson(p.boundsValue.size); return b; }
                    case SerializedPropertyType.ArraySize: return p.intValue;
                    case SerializedPropertyType.Character: return p.intValue;
                    case SerializedPropertyType.AnimationCurve: return "AnimationCurve(" + p.animationCurveValue.length + " keys)";
                    case SerializedPropertyType.Gradient: return "Gradient";
                    case SerializedPropertyType.ManagedReference: return "(managed reference)";
                    case SerializedPropertyType.Generic:
                    {
                        if (depth <= 0) return "(…)";
                        if (p.isArray)
                        {
                            JSONArray arr = new JSONArray();
                            int n = Math.Min(p.arraySize, 20);
                            for (int i = 0; i < n; i++) arr.Add(PropToJson(p.GetArrayElementAtIndex(i), depth - 1));
                            if (p.arraySize > n) arr.Add("… " + (p.arraySize - n) + " more elements");
                            return arr;
                        }
                        JSONObject obj = new JSONObject();
                        SerializedProperty end = p.GetEndProperty();
                        SerializedProperty child = p.Copy();
                        if (child.NextVisible(true))
                        {
                            while (!SerializedProperty.EqualContents(child, end))
                            {
                                obj[child.name] = PropToJson(child, depth - 1);
                                if (!child.NextVisible(false)) break;
                            }
                        }
                        return obj;
                    }
                    default: return "(" + p.propertyType + ")";
                }
            }
            private static JSONNode RefToJson(Object obj)
            {
                if (obj == null) return JSONNull.CreateOrGet();
                string assetPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(assetPath)) return assetPath + (AssetDatabase.IsMainAsset(obj) ? "" : " (" + obj.GetType().Name + " '" + obj.name + "')");
                GameObject go = obj as GameObject;
                Component comp = obj as Component;
                if (go != null) return PathOf(go.transform);
                if (comp != null) return PathOf(comp.transform) + " (" + comp.GetType().Name + ")";
                return obj.GetType().Name + " '" + obj.name + "'";
            }
            private static JSONNode ColorToJson(Color c) { JSONObject o = new JSONObject(); o["r"] = c.r; o["g"] = c.g; o["b"] = c.b; o["a"] = c.a; return o; }
            private static JSONNode VecToJson(Vector4 v) { JSONObject o = new JSONObject(); o["x"] = v.x; o["y"] = v.y; o["z"] = v.z; o["w"] = v.w; return o; }
            private static JSONNode VecToJson(Vector3 v) { JSONObject o = new JSONObject(); o["x"] = v.x; o["y"] = v.y; o["z"] = v.z; return o; }
            private static JSONNode VecToJson(Vector2 v) { JSONObject o = new JSONObject(); o["x"] = v.x; o["y"] = v.y; return o; }

            private static float Num(JSONNode v, string key, float fallback)
            {
                if (v == null || !v.IsObject || !v.HasKey(key)) return fallback;
                return v[key].AsFloat;
            }
            private static Vector4 ParseVector(JSONNode v, Vector4 current)
            {
                if (v.IsArray)
                {
                    Vector4 r = current;
                    if (v.Count > 0) r.x = v[0].AsFloat;
                    if (v.Count > 1) r.y = v[1].AsFloat;
                    if (v.Count > 2) r.z = v[2].AsFloat;
                    if (v.Count > 3) r.w = v[3].AsFloat;
                    return r;
                }
                return new Vector4(Num(v, "x", current.x), Num(v, "y", current.y), Num(v, "z", current.z), Num(v, "w", current.w));
            }
            private static Color ParseColor(JSONNode v, Color current)
            {
                if (v.IsString)
                {
                    Color c;
                    if (ColorUtility.TryParseHtmlString(v.Value, out c)) return c;
                    throw new ArgumentException("Cannot parse color '" + v.Value + "'. Use #RRGGBB, #RRGGBBAA or {\"r\",\"g\",\"b\",\"a\"}.");
                }
                if (v.IsArray) { Vector4 x = ParseVector(v, new Vector4(current.r, current.g, current.b, current.a)); return new Color(x.x, x.y, x.z, v.Count > 3 ? x.w : current.a); }
                return new Color(Num(v, "r", current.r), Num(v, "g", current.g), Num(v, "b", current.b), Num(v, "a", current.a));
            }
            private static Object ResolveReference(JSONNode v, string expectedTypeName)
            {
                if (v == null || v.IsNull || (v.IsString && v.Value == "")) return null;
                string assetPath = null, objectPath = null, componentType = null;
                if (v.IsObject)
                {
                    assetPath = Str(v, "asset") ?? Str(v, "path");
                    objectPath = Str(v, "object");
                    componentType = Str(v, "component");
                    if (assetPath == null && objectPath == null) throw new ArgumentException("Object references need an asset path string, a scene path string, or {\"object\":...,\"component\":...}.");
                }
                else
                {
                    string s = v.Value;
                    if (s.StartsWith("Assets/") || s.StartsWith("Packages/")) assetPath = s; else objectPath = s;
                }
                if (assetPath != null)
                {
                    Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                    if (all == null || all.Length == 0) throw new ArgumentException("No asset at '" + assetPath + "'.");
                    Object typed = all.FirstOrDefault(o => o != null && TypeMatches(o.GetType(), expectedTypeName));
                    return typed ?? AssetDatabase.LoadMainAssetAtPath(assetPath);
                }
                GameObject go = FindSceneObject(objectPath);
                string wanted = componentType ?? expectedTypeName;
                if (string.IsNullOrEmpty(wanted) || wanted == "GameObject" || wanted == "Object") return go;
                Component comp = go.GetComponents<Component>().FirstOrDefault(c => c != null && TypeMatches(c.GetType(), wanted));
                if (comp == null) throw new ArgumentException("'" + objectPath + "' has no " + wanted + " component.");
                return comp;
            }
            private static string ExpectedRefType(SerializedProperty p)
            {
                // p.type looks like "PPtr<$Transform>" for object references.
                string t = p.type ?? "";
                int a = t.IndexOf('$'), b = t.IndexOf('>');
                return a >= 0 && b > a ? t.Substring(a + 1, b - a - 1) : "";
            }
            private static void SetPropFromJson(SerializedProperty p, JSONNode v)
            {
                switch (p.propertyType)
                {
                    case SerializedPropertyType.Integer: p.longValue = v.IsNumber ? (long)v.AsDouble : long.Parse(v.Value); break;
                    case SerializedPropertyType.Boolean: p.boolValue = v.IsBoolean ? v.AsBool : bool.Parse(v.Value); break;
                    case SerializedPropertyType.Float: p.doubleValue = v.IsNumber ? v.AsDouble : double.Parse(v.Value, System.Globalization.CultureInfo.InvariantCulture); break;
                    case SerializedPropertyType.String: p.stringValue = v.IsString ? v.Value : v.ToString(); break;
                    case SerializedPropertyType.Color: p.colorValue = ParseColor(v, p.colorValue); break;
                    case SerializedPropertyType.ObjectReference: p.objectReferenceValue = ResolveReference(v, ExpectedRefType(p)); break;
                    case SerializedPropertyType.LayerMask: p.intValue = v.IsNumber ? v.AsInt : LayerMask.NameToLayer(v.Value); break;
                    case SerializedPropertyType.Enum:
                    {
                        if (v.IsNumber) { p.enumValueIndex = v.AsInt; break; }
                        int idx = Array.FindIndex(p.enumDisplayNames, n => n.Equals(v.Value, StringComparison.OrdinalIgnoreCase));
                        if (idx < 0) idx = Array.FindIndex(p.enumNames, n => n.Equals(v.Value, StringComparison.OrdinalIgnoreCase));
                        if (idx < 0) throw new ArgumentException("'" + v.Value + "' is not one of: " + string.Join(", ", p.enumDisplayNames));
                        p.enumValueIndex = idx;
                        break;
                    }
                    case SerializedPropertyType.Vector2: { Vector4 x = ParseVector(v, p.vector2Value); p.vector2Value = new Vector2(x.x, x.y); break; }
                    case SerializedPropertyType.Vector3: { Vector4 x = ParseVector(v, p.vector3Value); p.vector3Value = new Vector3(x.x, x.y, x.z); break; }
                    case SerializedPropertyType.Vector4: p.vector4Value = ParseVector(v, p.vector4Value); break;
                    case SerializedPropertyType.Quaternion: { Quaternion q = p.quaternionValue; Vector4 x = ParseVector(v, new Vector4(q.x, q.y, q.z, q.w)); p.quaternionValue = new Quaternion(x.x, x.y, x.z, x.w); break; }
                    case SerializedPropertyType.Rect: { Rect r = p.rectValue; p.rectValue = new Rect(Num(v, "x", r.x), Num(v, "y", r.y), Num(v, "width", r.width), Num(v, "height", r.height)); break; }
                    case SerializedPropertyType.Bounds: { Bounds b = p.boundsValue; b.center = ParseVector(v["center"], b.center); b.size = ParseVector(v["size"], b.size); p.boundsValue = b; break; }
                    case SerializedPropertyType.ArraySize: p.intValue = v.AsInt; break;
                    case SerializedPropertyType.Character: p.intValue = v.IsNumber ? v.AsInt : (v.Value.Length > 0 ? v.Value[0] : 0); break;
                    case SerializedPropertyType.Generic:
                    {
                        if (p.isArray)
                        {
                            if (!v.IsArray) throw new ArgumentException(p.propertyPath + " is an array; pass a JSON array.");
                            p.arraySize = v.Count;
                            for (int i = 0; i < v.Count; i++) SetPropFromJson(p.GetArrayElementAtIndex(i), v[i]);
                            break;
                        }
                        if (!v.IsObject) throw new ArgumentException(p.propertyPath + " is a struct; pass a JSON object with its fields.");
                        foreach (KeyValuePair<string, JSONNode> kv in v.AsObject)
                        {
                            SerializedProperty child = p.FindPropertyRelative(kv.Key);
                            if (child == null) throw new ArgumentException(p.propertyPath + " has no field '" + kv.Key + "'.");
                            SetPropFromJson(child, kv.Value);
                        }
                        break;
                    }
                    default: throw new ArgumentException("Properties of type " + p.propertyType + " cannot be set through this tool.");
                }
            }
            private static SerializedProperty FindPropertyLoose(SerializedObject so, string path)
            {
                SerializedProperty p = so.FindProperty(path);
                if (p != null) return p;
                // Accept display names ("Local Position") and case differences.
                SerializedProperty it = so.GetIterator();
                bool enter = true;
                while (it.NextVisible(enter))
                {
                    enter = false;
                    if (it.propertyPath.Equals(path, StringComparison.OrdinalIgnoreCase) || it.displayName.Equals(path, StringComparison.OrdinalIgnoreCase) || it.name.Equals(path, StringComparison.OrdinalIgnoreCase)) return it.Copy();
                }
                return null;
            }
            private static string SetComponentProperty(JSONNode a)
            {
                GameObject go = FindSceneObject(Require(a, "object"));
                Component c = FindComponent(go, Require(a, "component"), Int(a, "index", 0));
                string path = Require(a, "property_path");
                if (!a.HasKey("value")) throw new ArgumentException("Missing argument 'value'.");
                SerializedObject so = new SerializedObject(c);
                SerializedProperty p = FindPropertyLoose(so, path);
                if (p == null) throw new ArgumentException("No property '" + path + "' on " + c.GetType().Name + ". Use get_component_properties to list them.");
                SetPropFromJson(p, a["value"]);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(c);
                so.Update();
                SerializedProperty check = so.FindProperty(p.propertyPath);
                return "Set " + c.GetType().Name + "." + p.propertyPath + " on '" + PathOf(go.transform) + "' to " + (check != null ? PropToJson(check, 2).ToString() : "(new value)");
            }
            private static string AddComponent(JSONNode a)
            {
                GameObject go = FindSceneObject(Require(a, "object"));
                string typeName = Require(a, "type");
                Type type = FindType(typeName, typeof(Component));
                if (type == null) throw new ArgumentException("No component type named '" + typeName + "' is loaded. Is the package that provides it imported?");
                Component c = Undo.AddComponent(go, type);
                if (c == null) throw new InvalidOperationException("Unity refused to add " + type.Name + " (see the console).");
                return "Added " + type.FullName + " to '" + PathOf(go.transform) + "'.";
            }
            private static string RemoveComponent(JSONNode a)
            {
                GameObject go = FindSceneObject(Require(a, "object"));
                Component c = FindComponent(go, Require(a, "component"), Int(a, "index", 0));
                if (c is Transform) throw new ArgumentException("Transform cannot be removed.");
                string name = c.GetType().Name;
                Undo.DestroyObjectImmediate(c);
                return "Removed " + name + " from '" + PathOf(go.transform) + "'.";
            }
            private static string SetActive(JSONNode a)
            {
                GameObject go = FindSceneObject(Require(a, "object"));
                bool active = a["active"].AsBool;
                Undo.RecordObject(go, "Set active");
                go.SetActive(active);
                EditorUtility.SetDirty(go);
                return "'" + PathOf(go.transform) + "' is now " + (active ? "active" : "inactive") + ".";
            }
            private static string CreateGameObject(JSONNode a)
            {
                GameObject go = new GameObject(Require(a, "name"));
                Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
                string parent = Str(a, "parent");
                if (!string.IsNullOrEmpty(parent)) Undo.SetTransformParent(go.transform, FindSceneObject(parent).transform, "Parent " + go.name);
                Selection.activeGameObject = go;
                return "Created '" + PathOf(go.transform) + "'.";
            }

            // ---------------------------------------------------------------- assets, materials, shaders
            private static string ListAssets(JSONNode a)
            {
                string filter = Require(a, "filter");
                string folder = Str(a, "folder");
                int limit = Int(a, "limit", 100);
                string[] guids = string.IsNullOrEmpty(folder) ? AssetDatabase.FindAssets(filter) : AssetDatabase.FindAssets(filter, new[] { folder.TrimEnd('/') });
                StringBuilder sb = new StringBuilder();
                int n = 0;
                foreach (string guid in guids)
                {
                    sb.AppendLine(AssetDatabase.GUIDToAssetPath(guid));
                    if (++n >= limit) { sb.AppendLine("… (" + (guids.Length - n) + " more; narrow the filter)"); break; }
                }
                return n == 0 ? "No assets match '" + filter + "'." : sb.ToString();
            }
            private static Material LoadMaterial(string path)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) throw new ArgumentException("No material at '" + path + "'.");
                return m;
            }
            private static string GetMaterial(JSONNode a)
            {
                Material m = LoadMaterial(Require(a, "path"));
                string filter = Str(a, "filter");
                int limit = Int(a, "limit", 150);
                Shader shader = m.shader;
                JSONObject o = new JSONObject();
                o["path"] = AssetDatabase.GetAssetPath(m);
                o["shader"] = shader != null ? shader.name : null;
                o["renderQueue"] = m.renderQueue;
                JSONArray kw = new JSONArray();
                foreach (string k in m.shaderKeywords) kw.Add(k);
                o["keywords"] = kw;
                JSONObject props = new JSONObject();
                int total = shader != null ? shader.GetPropertyCount() : 0, shown = 0;
                for (int i = 0; i < total; i++)
                {
                    string name = shader.GetPropertyName(i);
                    string desc = shader.GetPropertyDescription(i);
                    if (!string.IsNullOrEmpty(filter) && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 && desc.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (shown >= limit) { props["…"] = (total - i) + " more properties; use filter"; break; }
                    JSONObject p = new JSONObject();
                    UnityEngine.Rendering.ShaderPropertyType type = shader.GetPropertyType(i);
                    p["type"] = type.ToString();
                    if (desc != name) p["label"] = desc;
                    switch (type)
                    {
                        case UnityEngine.Rendering.ShaderPropertyType.Color: p["value"] = ColorToJson(m.GetColor(name)); break;
                        case UnityEngine.Rendering.ShaderPropertyType.Vector: p["value"] = VecToJson(m.GetVector(name)); break;
                        case UnityEngine.Rendering.ShaderPropertyType.Float: p["value"] = m.GetFloat(name); break;
                        case UnityEngine.Rendering.ShaderPropertyType.Range: { p["value"] = m.GetFloat(name); Vector2 r = shader.GetPropertyRangeLimits(i); p["min"] = r.x; p["max"] = r.y; break; }
                        case UnityEngine.Rendering.ShaderPropertyType.Int: p["value"] = m.GetInteger(name); break;
                        case UnityEngine.Rendering.ShaderPropertyType.Texture:
                        {
                            Texture tex = m.GetTexture(name);
                            p["value"] = tex != null ? (JSONNode)(AssetDatabase.GetAssetPath(tex)) : JSONNull.CreateOrGet();
                            p["tiling"] = VecToJson(m.GetTextureScale(name));
                            p["offset"] = VecToJson(m.GetTextureOffset(name));
                            break;
                        }
                    }
                    props[name] = p;
                    shown++;
                }
                o["propertyCount"] = total;
                o["properties"] = props;
                return o.ToString(2);
            }
            private static string SetMaterial(JSONNode a)
            {
                Material m = LoadMaterial(Require(a, "path"));
                Undo.RecordObject(m, "AI set material");
                List<string> changes = new List<string>();
                string shaderName = Str(a, "shader");
                if (!string.IsNullOrEmpty(shaderName))
                {
                    Shader s = Shader.Find(shaderName);
                    if (s == null) throw new ArgumentException("No shader named '" + shaderName + "'. Use list_shaders.");
                    m.shader = s;
                    changes.Add("shader=" + shaderName);
                }
                if (a.HasKey("properties") && a["properties"].IsObject)
                {
                    foreach (KeyValuePair<string, JSONNode> kv in a["properties"].AsObject)
                    {
                        int idx = m.shader.FindPropertyIndex(kv.Key);
                        if (idx < 0) throw new ArgumentException("Shader '" + m.shader.name + "' has no property '" + kv.Key + "'. Read the material to see its properties.");
                        switch (m.shader.GetPropertyType(idx))
                        {
                            case UnityEngine.Rendering.ShaderPropertyType.Color: m.SetColor(kv.Key, ParseColor(kv.Value, m.GetColor(kv.Key))); break;
                            case UnityEngine.Rendering.ShaderPropertyType.Vector: m.SetVector(kv.Key, ParseVector(kv.Value, m.GetVector(kv.Key))); break;
                            case UnityEngine.Rendering.ShaderPropertyType.Float:
                            case UnityEngine.Rendering.ShaderPropertyType.Range: m.SetFloat(kv.Key, kv.Value.AsFloat); break;
                            case UnityEngine.Rendering.ShaderPropertyType.Int: m.SetInteger(kv.Key, kv.Value.AsInt); break;
                            case UnityEngine.Rendering.ShaderPropertyType.Texture:
                            {
                                if (kv.Value.IsNull || kv.Value.Value == "") { m.SetTexture(kv.Key, null); break; }
                                Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(kv.Value.Value);
                                if (tex == null) throw new ArgumentException("No texture at '" + kv.Value.Value + "'.");
                                m.SetTexture(kv.Key, tex);
                                break;
                            }
                        }
                        changes.Add(kv.Key);
                    }
                }
                if (a.HasKey("enable_keywords")) foreach (JSONNode k in a["enable_keywords"].Children) { m.EnableKeyword(k.Value); changes.Add("+" + k.Value); }
                if (a.HasKey("disable_keywords")) foreach (JSONNode k in a["disable_keywords"].Children) { m.DisableKeyword(k.Value); changes.Add("-" + k.Value); }
                string note = "";
                if (a.HasKey("render_queue"))
                {
                    int wanted = a["render_queue"].AsInt;
                    m.renderQueue = wanted;
                    // Shaders with a rendering mode (Unity's Standard shader) re-apply the mode's queue on the next
                    // property access, so probe once and report honestly instead of claiming a value that will not stick.
                    m.HasProperty("_Color");
                    if (wanted >= 0 && m.renderQueue != wanted)
                    {
                        note = " Note: the render queue snapped back to " + m.renderQueue + " because this shader derives it from its rendering mode. For Standard, set _Mode instead (0 Opaque, 1 Cutout, 2 Fade, 3 Transparent) and the matching queue follows.";
                    }
                    changes.Add("renderQueue=" + m.renderQueue);
                }
                EditorUtility.SetDirty(m);
                return changes.Count == 0 ? "Nothing to change." : "Updated " + AssetDatabase.GetAssetPath(m) + ": " + string.Join(", ", changes) + note;
            }
            private static string CreateMaterial(JSONNode a)
            {
                string path = Require(a, "path");
                if (!path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) path += ".mat";
                EnsureAssetPath(path);
                Shader s = Shader.Find(Require(a, "shader"));
                if (s == null) throw new ArgumentException("No shader named '" + a["shader"].Value + "'. Use list_shaders.");
                Material m = new Material(s);
                string dir = Path.GetDirectoryName(path).Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(dir)) Directory.CreateDirectory(Path.Combine(ProjectPath, dir));
                AssetDatabase.CreateAsset(m, path);
                AssetDatabase.SaveAssets();
                return "Created material " + path + " with shader " + s.name + ".";
            }
            private static string ListShaders(JSONNode a)
            {
                string filter = Str(a, "filter");
                int limit = Int(a, "limit", 200);
                StringBuilder sb = new StringBuilder();
                int n = 0, total = 0;
                foreach (ShaderInfo info in ShaderUtil.GetAllShaderInfo().OrderBy(i => i.name))
                {
                    if (!string.IsNullOrEmpty(filter) && info.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    total++;
                    if (n >= limit) continue;
                    sb.AppendLine(info.name + (info.hasErrors ? "  (HAS ERRORS)" : "") + (info.supported ? "" : "  (unsupported)"));
                    n++;
                }
                if (total > n) sb.AppendLine("… " + (total - n) + " more; use filter");
                return n == 0 ? "No shaders match." : sb.ToString();
            }
            private static string GetShaderErrors(JSONNode a)
            {
                string name = Require(a, "shader");
                Shader s = name.StartsWith("Assets/") || name.StartsWith("Packages/") ? AssetDatabase.LoadAssetAtPath<Shader>(name) : Shader.Find(name);
                if (s == null) throw new ArgumentException("No shader '" + name + "'.");
                ShaderMessage[] messages = ShaderUtil.GetShaderMessages(s);
                if (messages.Length == 0) return s.name + ": no errors or warnings" + (ShaderUtil.ShaderHasError(s) ? " reported, but the shader is flagged as having errors; reimport it and check the console." : ".");
                StringBuilder sb = new StringBuilder();
                foreach (ShaderMessage msg in messages)
                {
                    sb.AppendLine(msg.severity + " line " + msg.line + (string.IsNullOrEmpty(msg.file) ? "" : " in " + msg.file) + ": " + msg.message + (string.IsNullOrEmpty(msg.messageDetails) ? "" : "\n    " + msg.messageDetails));
                }
                return sb.ToString();
            }

            // ---------------------------------------------------------------- files
            private static string ResolveProjectPath(string relative, bool forWrite)
            {
                if (string.IsNullOrEmpty(relative)) throw new ArgumentException("A path is required.");
                string full = Path.GetFullPath(Path.Combine(ProjectPath, relative.Replace('\\', '/')));
                string root = Path.GetFullPath(ProjectPath).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
                if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Paths must stay inside the project folder.");
                string rel = full.Substring(root.Length).Replace('\\', '/');
                string relSlash = rel.TrimEnd('/') + "/";
                foreach (string blocked in new[] { "Library/", "Temp/", "Logs/", ".git/", "obj/" })
                {
                    if (relSlash.StartsWith(blocked, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("That folder is off limits.");
                }
                if (forWrite && !(relSlash.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) || relSlash.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Files can only be written under Assets/ or Packages/.");
                return full;
            }
            private static void EnsureAssetPath(string path)
            {
                if (!(path.StartsWith("Assets/") || path.StartsWith("Packages/"))) throw new ArgumentException("Asset paths must start with Assets/ or Packages/.");
            }
            private static string ReadFile(JSONNode a)
            {
                string full = ResolveProjectPath(Require(a, "path"), false);
                if (!File.Exists(full)) throw new ArgumentException("No file at '" + a["path"].Value + "'.");
                string[] lines = File.ReadAllLines(full);
                int start = Math.Max(1, Int(a, "start_line", 1));
                int max = Math.Max(1, Int(a, "max_lines", 400));
                StringBuilder sb = new StringBuilder();
                for (int i = start - 1; i < lines.Length && i < start - 1 + max; i++) sb.Append(i + 1).Append(": ").AppendLine(lines[i]);
                if (start - 1 + max < lines.Length) sb.AppendLine("… (" + (lines.Length - (start - 1 + max)) + " more lines)");
                return sb.Length == 0 ? "(empty file)" : sb.ToString();
            }
            private static string WriteFile(JSONNode a)
            {
                string rel = Require(a, "path");
                string full = ResolveProjectPath(rel, true);
                if (!a.HasKey("content")) throw new ArgumentException("Missing argument 'content'.");
                bool existed = File.Exists(full);
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllText(full, a["content"].Value, new UTF8Encoding(false));
                string assetPath = full.Substring(Path.GetFullPath(ProjectPath).TrimEnd('\\', '/').Length + 1).Replace('\\', '/');
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                return (existed ? "Overwrote " : "Created ") + assetPath + " (" + a["content"].Value.Length + " characters). Unity is reimporting it; scripts trigger a recompile.";
            }
            private static string FindInFiles(JSONNode a)
            {
                Regex regex = new Regex(Require(a, "pattern"), RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
                string folder = Str(a, "folder", "Assets");
                int limit = Int(a, "limit", 100);
                HashSet<string> exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".shader", ".cginc", ".hlsl", ".cs", ".json", ".asmdef", ".txt", ".md" };
                if (a.HasKey("extensions") && a["extensions"].IsArray && a["extensions"].Count > 0)
                {
                    exts.Clear();
                    foreach (JSONNode e in a["extensions"].Children) exts.Add(e.Value.StartsWith(".") ? e.Value : "." + e.Value);
                }
                string root = ResolveProjectPath(folder, false);
                if (!Directory.Exists(root)) throw new ArgumentException("No folder '" + folder + "'.");
                StringBuilder sb = new StringBuilder();
                int n = 0;
                foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (!exts.Contains(Path.GetExtension(file))) continue;
                    string rel = file.Substring(Path.GetFullPath(ProjectPath).TrimEnd('\\', '/').Length + 1).Replace('\\', '/');
                    int lineNo = 0;
                    foreach (string line in File.ReadLines(file))
                    {
                        lineNo++;
                        if (!regex.IsMatch(line)) continue;
                        sb.AppendLine(rel + ":" + lineNo + ": " + line.Trim());
                        if (++n >= limit) return sb.AppendLine("… (limit reached)").ToString();
                    }
                }
                return n == 0 ? "No matches." : sb.ToString();
            }

            // ---------------------------------------------------------------- console & editor
            private static string GetConsoleLog(JSONNode a)
            {
                int count = Int(a, "count", 40);
                bool errorsOnly = a["errors_only"].AsBool;
                List<string> lines = AIConsoleLog.Recent(count, errorsOnly);
                return lines.Count == 0 ? "No console messages recorded" + (errorsOnly ? " (no errors)." : " since the editor started.") : string.Join("\n", lines);
            }
            private static string RunMenuItem(JSONNode a)
            {
                string path = Require(a, "path");
                return EditorApplication.ExecuteMenuItem(path) ? "Ran '" + path + "'." : "Menu item '" + path + "' was not found or is disabled.";
            }
            private static string SaveAll()
            {
                AssetDatabase.SaveAssets();
                bool scenes = EditorSceneManager.SaveOpenScenes();
                return scenes ? "Saved all assets and open scenes." : "Saved assets; saving scenes was cancelled or failed.";
            }
        }

        // Keeps the last few hundred console messages so the assistant can read errors after it changes something.
        internal static class AIConsoleLog
        {
            private static readonly List<string> entries = new List<string>();
            private static readonly List<bool> isError = new List<bool>();
            private const int Capacity = 400;

            [InitializeOnLoadMethod]
            private static void Hook()
            {
                Application.logMessageReceived -= OnLog;
                Application.logMessageReceived += OnLog;
            }
            private static void OnLog(string condition, string stackTrace, LogType type)
            {
                string firstFrame = "";
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    int nl = stackTrace.IndexOf('\n');
                    firstFrame = "  @ " + (nl > 0 ? stackTrace.Substring(0, nl) : stackTrace).Trim();
                }
                entries.Add("[" + type + "] " + condition.Trim() + firstFrame);
                isError.Add(type == LogType.Error || type == LogType.Exception || type == LogType.Assert);
                if (entries.Count > Capacity) { entries.RemoveAt(0); isError.RemoveAt(0); }
            }
            public static List<string> Recent(int count, bool errorsOnly)
            {
                List<string> result = new List<string>();
                for (int i = entries.Count - 1; i >= 0 && result.Count < count; i--)
                {
                    if (errorsOnly && !isError[i]) continue;
                    result.Add(entries[i]);
                }
                result.Reverse();
                return result;
            }
        }
    }
}
