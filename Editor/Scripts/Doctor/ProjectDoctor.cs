using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports
    {
        // Inspects the project for the things that break VRChat projects in practice and offers a fix for each.
        internal static partial class ProjectDoctor
        {
            internal class Finding
            {
                public string Id;
                public string Severity; // error | warning | info
                public string Title;
                public string Detail;
                public string FixLabel;
                public Action Fix;
                public bool Recompiles;
                public bool Safe; // can be applied by "Fix all" without asking
                public JSONNode ToJson()
                {
                    JSONObject o = new JSONObject();
                    o["id"] = Id;
                    o["severity"] = Severity;
                    o["title"] = Title;
                    o["detail"] = Detail ?? "";
                    if (Fix != null) { o["fix"] = FixLabel; o["recompiles"] = Recompiles; }
                    return o;
                }
            }
            internal class Report
            {
                public DateTime Time;
                public List<Finding> Findings = new List<Finding>();
                public int Errors => Findings.Count(f => f.Severity == "error");
                public int Warnings => Findings.Count(f => f.Severity == "warning");
                public JSONNode ToJson()
                {
                    JSONObject o = new JSONObject();
                    o["checkedAt"] = Time.ToString("s");
                    o["summary"] = Findings.Count == 0 ? "No problems found." : Errors + " error(s), " + Warnings + " warning(s), " + (Findings.Count - Errors - Warnings) + " note(s).";
                    JSONArray arr = new JSONArray();
                    foreach (Finding f in Findings) arr.Add(f.ToJson());
                    o["findings"] = arr;
                    return o;
                }
            }

            public static Report Last { get; private set; }
            // Fixes applied since the last check, so the chat log can still say what a fix did after it's gone from the report.
            public static readonly Dictionary<string, Finding> RecentlyApplied = new Dictionary<string, Finding>();

            // ---------------------------------------------------------------- wording
            // The name a person knows a package by: the tool's name, else the package's own display name, else its id.
            private static string Display(string packageId)
            {
                switch (packageId)
                {
                    case "nadena.dev.ndmf": return "NDMF";
                    case "com.vrchat.base": return "the VRChat SDK";
                    case "com.vrchat.core.vpm-resolver": return "the Creator Companion's package resolver";
                }
                Tool t = ToolCatalog.Tools.FirstOrDefault(x => x.VpmPackage == packageId || x.UnityPackage == packageId);
                if (t != null) return t.Name;
                JSONNode m;
                if (Vpm.InstalledManifests().TryGetValue(packageId, out m) && m.HasKey("displayName") && m["displayName"].Value != "") return m["displayName"].Value;
                foreach (VpmListing l in Vpm.AllListings())
                {
                    VpmPackageVersion v = l.Versions(packageId).FirstOrDefault();
                    if (v != null && v.DisplayName != packageId) return v.DisplayName;
                }
                return packageId;
            }
            // ">=1.14.7 <2.0.0-a" -> "version 1.14.7 or newer"; "^3.2.0" -> "version 3.2.0 or newer"; "1.2.3" -> "version 1.2.3".
            private static string RangeText(string range)
            {
                if (string.IsNullOrWhiteSpace(range) || range.Trim() == "*") return "any version";
                foreach (string part in range.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string v = part.TrimStart('>', '=', '^', '~', 'v');
                    if (part.StartsWith("<")) continue;
                    if (part.StartsWith(">") || part.StartsWith("^") || part.StartsWith("~")) return "version " + v + " or newer";
                    return "version " + v;
                }
                return "a compatible version";
            }
            private static string Plural(int n, string one, string many) => n + " " + (n == 1 ? one : many);

            public static Report Run()
            {
                Report r = new Report { Time = DateTime.Now };
                try { CheckUnityVersion(r); } catch (Exception e) { Note(r, "unity-version", e); }
                try { CheckLegacyKebinImports(r); } catch (Exception e) { Note(r, "legacy-self", e); }
                try { CheckTools(r); } catch (Exception e) { Note(r, "tools", e); }
                try { CheckManifest(r); } catch (Exception e) { Note(r, "manifest", e); }
                try { CheckCompilation(r); } catch (Exception e) { Note(r, "compile", e); }
                try { CheckMaterials(r); } catch (Exception e) { Note(r, "materials", e); }
                try { CheckShaders(r); } catch (Exception e) { Note(r, "shaders", e); }
                try { CheckMissingScripts(r); } catch (Exception e) { Note(r, "missing-scripts", e); }
                try { CheckBundlePreview(r); } catch (Exception e) { Note(r, "bundle-preview", e); }
                r.Findings = r.Findings.OrderBy(f => f.Severity == "error" ? 0 : f.Severity == "warning" ? 1 : 2).ToList();
                Last = r;
                return r;
            }
            private static void Note(Report r, string check, Exception e)
            {
                Debug.LogWarning("[kebinImports] Project Doctor: the " + check + " check failed: " + e);
                r.Findings.Add(new Finding { Id = "check-failed-" + check, Severity = "info", Title = "Part of the check couldn't run", Detail = "The Doctor couldn't finish one of its checks. The details are in Unity's Console." });
            }
            public static string Fix(string id)
            {
                Report r = Last ?? Run();
                Finding f = r.Findings.FirstOrDefault(x => x.Id == id);
                if (f == null) throw new ArgumentException("No finding with id '" + id + "'. Run the doctor again; ids: " + string.Join(", ", r.Findings.Select(x => x.Id)));
                if (f.Fix == null) return "'" + f.Title + "' has no automatic fix. " + f.Detail;
                f.Fix();
                RecentlyApplied[f.Id] = f;
                r.Findings.Remove(f);
                return "Applied: " + f.FixLabel + " (" + f.Title + ")." + (f.Recompiles ? " Unity will recompile; tell the user and end your reply." : "");
            }

            // ---------------------------------------------------------------- checks
            private static void CheckUnityVersion(Report r)
            {
                string wanted = VRChatSdkUnityVersionCached();
                if (string.IsNullOrEmpty(wanted)) return;
                if (Application.unityVersion != wanted)
                {
                    r.Findings.Add(new Finding { Id = "unity-version", Severity = "warning", Title = "This project is open in Unity " + Application.unityVersion + ", but VRChat needs Unity " + wanted, Detail = "Uploads may fail or the VRChat SDK may misbehave. Install Unity " + wanted + " with Unity Hub (or let the Creator Companion do it) and open the project with that version." });
                }
            }
            private static string cachedSdkUnityVersion;
            private static string VRChatSdkUnityVersionCached()
            {
                if (cachedSdkUnityVersion != null) return cachedSdkUnityVersion;
                try
                {
                    client = new HttpClient();
                    // Wait at most 5 seconds: a slow connection shouldn't freeze the Doctor. Without an answer the check is skipped.
                    System.Threading.Tasks.Task<string> fetch = System.Threading.Tasks.Task.Run(() => HttpClient.DownloadString(client, "https://api.vrchat.cloud/api/1/config"));
                    cachedSdkUnityVersion = fetch.Wait(TimeSpan.FromSeconds(5)) ? JSON.Parse(fetch.Result)["sdkUnityVersion"].Value : "";
                }
                catch (Exception) { cachedSdkUnityVersion = ""; }
                return cachedSdkUnityVersion;
            }
            private static void CheckLegacyKebinImports(Report r)
            {
                string legacy = Path.Combine(Application.dataPath, "kebinImports");
                if (Directory.Exists(legacy) && !installedPath.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
                {
                    r.Findings.Add(new Finding { Id = "legacy-kebinimports", Severity = "error", Title = "An old copy of kebinImports is still in the project", Detail = "It's in Assets/kebinImports. Two copies clash and stop the project's scripts from compiling.", FixLabel = "Remove the old copy", Recompiles = true, Safe = true, Fix = () => { DeleteDirectory(legacy); AssetDatabase.Refresh(); } });
                }
            }
            private static void CheckTools(Report r)
            {
                foreach (ToolCatalog.Status s in ToolCatalog.AllStatuses(true))
                {
                    Tool t = s.Tool;
                    if (s.LegacyCopies.Count > 0 && s.Installed && (t.IsPackage || !string.IsNullOrEmpty(t.UnityPackage)))
                    {
                        r.Findings.Add(new Finding { Id = "duplicate-" + t.Key, Severity = "error", Title = t.Name + " is installed twice", Detail = "There is a current copy in " + s.Location + " and an old copy in " + string.Join(", ", s.LegacyCopies) + ". Two copies clash: they cause script errors and pink materials. Removing the old copy keeps the current one.", FixLabel = "Remove the old copy", Recompiles = true, Safe = true, Fix = () => ToolCatalog.CleanLegacy(t) });
                    }
                    else if (s.LegacyCopies.Count > 0 && !s.Installed && t.IsPackage)
                    {
                        r.Findings.Add(new Finding { Id = "legacy-" + t.Key, Severity = "warning", Title = t.Name + " was installed the old way", Detail = "It lives in " + string.Join(", ", s.LegacyCopies) + ", so the Creator Companion can't keep it up to date. Reinstalling it the new way fixes that, and your materials keep their look.", FixLabel = "Reinstall the new way", Recompiles = true, Fix = () => ToolCatalog.Install(t) });
                    }
                    if (s.UpdateAvailable)
                    {
                        r.Findings.Add(new Finding { Id = "outdated-" + t.Key, Severity = "warning", Title = t.Name + " " + s.InstalledVersion + " can be updated to " + s.LatestVersion, Detail = "A newer version is available.", FixLabel = "Update to " + s.LatestVersion, Recompiles = true, Fix = () => ToolCatalog.Install(t, true) });
                    }
                }
            }
            private static void CheckManifest(Report r)
            {
                if (!File.Exists(Path.Combine(PackagesPath, "vpm-manifest.json"))) return;
                foreach (string id in Vpm.UnmanagedPackages())
                {
                    string pid = id;
                    r.Findings.Add(new Finding { Id = "unmanaged-" + pid, Severity = "warning", Title = "The Creator Companion doesn't know " + Display(pid) + " is installed", Detail = "So it can't update it, and it might remove it the next time it sets up this project. Registering it fixes both.", FixLabel = "Register it with the Creator Companion", Safe = true, Fix = () => Vpm.AdoptIntoManifest(pid) });
                }
                Dictionary<string, JSONNode> manifests = Vpm.InstalledManifests();
                Dictionary<string, string> installed = Vpm.InstalledPackages();
                foreach (KeyValuePair<string, JSONNode> kv in manifests)
                {
                    if (!kv.Value.HasKey("vpmDependencies")) continue;
                    foreach (KeyValuePair<string, JSONNode> dep in kv.Value["vpmDependencies"].AsObject)
                    {
                        string depId = dep.Key, range = dep.Value.Value;
                        string have;
                        bool ok = installed.TryGetValue(depId, out have) && (SemVer.TryParse(have) == null || SemVerRange.Satisfies(SemVer.TryParse(have), range));
                        if (ok) continue;
                        if (depId.StartsWith("com.vrchat.") && isVRCCreatorCompanion && installed.ContainsKey(depId)) continue;
                        r.Findings.Add(new Finding { Id = "missing-dep-" + kv.Key + "-" + depId, Severity = "error", Title = Display(kv.Key) + " needs " + Display(depId) + " " + RangeText(range) + (installed.ContainsKey(depId) ? " (you have " + have + ")" : ", which isn't installed"), Detail = Display(kv.Key) + " won't work until " + Display(depId) + " is installed.", FixLabel = installed.ContainsKey(depId) ? "Update " + Display(depId) : "Install " + Display(depId), Recompiles = true, Fix = () => Vpm.Install(depId, range) });
                    }
                }
                foreach (string orphan in Vpm.Orphans())
                {
                    string oid = orphan;
                    if (ToolCatalog.Tools.Any(t => t.VpmPackage == oid)) continue; // a tool the user may well want; not an orphan in spirit
                    r.Findings.Add(new Finding { Id = "orphan-" + oid, Severity = "info", Title = Display(oid) + " is installed, but nothing uses it any more", Detail = "It was probably needed by a tool you removed. It's safe to remove.", FixLabel = "Remove " + Display(oid), Recompiles = true, Fix = () => Vpm.Remove(oid) });
                }
            }
            private static void CheckCompilation(Report r)
            {
                if (!EditorUtility.scriptCompilationFailed) return;
                List<string> errors = AIConsoleLog.Recent(8, true).Where(l => l.Contains("error CS")).ToList();
                r.Findings.Add(new Finding { Id = "compile-errors", Severity = "error", Title = "Unity can't compile the project's scripts", Detail = "Until this is fixed, tools and menus in the project may not work. Resetting the scripting define symbols fixes the most common cause. If that doesn't help, Ask kebinAI can read the errors and explain them." + (errors.Count > 0 ? "\n\nUnity's first error: " + errors[0] : ""), FixLabel = "Reset define symbols", Recompiles = true, Fix = () => FSDSHandler(true) });
            }
            // Materials the open scenes actually render (prefab instances included); materials that only sit in the
            // project are not this avatar's problem.
            private static List<Material> SceneMaterials()
            {
                HashSet<Material> set = new HashSet<Material>();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        if (IsPreviewRoot(root)) continue; // an AssetBundle preview has its own check
                        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                        {
                            foreach (Material m in renderer.sharedMaterials) if (m != null) set.Add(m);
                        }
                    }
                }
                return set.ToList();
            }
            private static void CheckMaterials(Report r)
            {
                // Group pink scene materials by the tool their missing shader belongs to (GUID table, then content fingerprint).
                Dictionary<string, List<KeyValuePair<string, string>>> byTool = new Dictionary<string, List<KeyValuePair<string, string>>>();
                List<string> unknown = new List<string>();
                foreach (Material m in SceneMaterials())
                {
                    if (m.shader != null && m.shader.name != "Hidden/InternalErrorShader") continue;
                    string path = AssetDatabase.GetAssetPath(m);
                    if (string.IsNullOrEmpty(path)) { unknown.Add(m.name + " (material embedded in the scene)"); continue; }
                    string yaml;
                    try { yaml = File.ReadAllText(Path.Combine(ProjectPath, path)); } catch (Exception) { yaml = ""; }
                    string shaderGuid;
                    KnownSignatures.Match match = KnownSignatures.IdentifyMaterial(yaml, out shaderGuid);
                    if (match != null && ToolCatalog.Get(match.ToolKey) != null)
                    {
                        if (!byTool.ContainsKey(match.ToolKey)) byTool[match.ToolKey] = new List<KeyValuePair<string, string>>();
                        byTool[match.ToolKey].Add(new KeyValuePair<string, string>(path, match.Name));
                    }
                    else unknown.Add(path);
                }
                foreach (KeyValuePair<string, List<KeyValuePair<string, string>>> group in byTool)
                {
                    Tool t = ToolCatalog.Get(group.Key);
                    List<KeyValuePair<string, string>> mats = group.Value;
                    string files = string.Join("\n", mats.Take(12).Select(x => x.Key)) + (mats.Count > 12 ? "\n… " + (mats.Count - 12) + " more" : "");
                    ToolCatalog.Status status = ToolCatalog.GetStatus(t, false);
                    if (!status.Installed)
                    {
                        r.Findings.Add(new Finding { Id = "materials-need-" + t.Key, Severity = "error", Title = Plural(mats.Count, "material in the scene uses ", "materials in the scene use ").Replace(" uses  ", " uses ") + t.Name + ", which isn't installed", Detail = files + "\nInstalling " + t.Name + " makes " + (mats.Count == 1 ? "it" : "them") + " look right again.", FixLabel = "Install " + t.Name, Recompiles = true, Fix = () => ToolCatalog.Install(t) });
                    }
                    else
                    {
                        r.Findings.Add(new Finding { Id = "materials-repoint-" + t.Key, Severity = "error", Title = Plural(mats.Count, "material in the scene lost its ", "materials in the scene lost their ") + t.Name + " shader", Detail = files + "\n" + t.Name + " is installed, but these materials still point to an older copy of it that's gone.", FixLabel = "Reconnect them to " + t.Name, Safe = true, Fix = () => RepointMaterials(mats) });
                    }
                }
                if (unknown.Count == 0) return;
                r.Findings.Add(new Finding { Id = "pink-materials", Severity = "error", Title = Plural(unknown.Count, "material in the scene is pink", "materials in the scene are pink") + " (the shader is missing)", Detail = string.Join("\n", unknown.Take(15)) + (unknown.Count > 15 ? "\n… and " + (unknown.Count - 15) + " more" : "") + "\nkebinImports doesn't recognise the shader they used; it was probably bought or custom. Reinstall it if you have it, or use Fix Materials to switch them to another shader.", FixLabel = "Open Fix Materials", Fix = () => EditorApplication.ExecuteMenuItem("kebinImports/Fix Materials") });
            }
            private static void RepointMaterials(List<KeyValuePair<string, string>> mats)
            {
                int done = 0;
                List<string> missing = new List<string>();
                foreach (KeyValuePair<string, string> kv in mats)
                {
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(kv.Key);
                    Shader s = Shader.Find(kv.Value);
                    if (m == null) continue;
                    if (s == null) { if (!missing.Contains(kv.Value)) missing.Add(kv.Value); continue; }
                    Undo.RecordObject(m, "Re-point material shader");
                    m.shader = s;
                    EditorUtility.SetDirty(m);
                    done++;
                }
                AssetDatabase.SaveAssets();
                Debug.Log("[kebinImports] Re-pointed " + done + " material(s)." + (missing.Count > 0 ? " No shader named: " + string.Join(", ", missing) : ""));
            }
            private static void CheckShaders(Report r)
            {
                // Only shaders the open scenes use.
                List<string> bad = new List<string>();
                foreach (Shader shader in SceneMaterials().Select(m => m.shader).Where(sh => sh != null).Distinct())
                {
                    if (!ShaderUtil.ShaderHasError(shader)) continue;
                    string path = AssetDatabase.GetAssetPath(shader);
                    if (string.IsNullOrEmpty(path) || path.StartsWith("Resources/unity_builtin")) continue;
                    bad.Add(shader.name + " (" + path + ")");
                }
                if (bad.Count == 0) return;
                r.Findings.Add(new Finding { Id = "shader-errors", Severity = "warning", Title = Plural(bad.Count, "shader used in the scene has errors", "shaders used in the scene have errors"), Detail = string.Join("\n", bad.Take(15)) + (bad.Count > 15 ? "\n… and " + (bad.Count - 15) + " more" : "") + "\nMaterials using them may look wrong. Updating the shader's tool usually fixes it; Ask kebinAI can also look into it." });
            }
            private static void CheckMissingScripts(Report r)
            {
                // What is missing, from the files: every MonoBehaviour whose script GUID has no asset, in the saved open
                // scenes and in the prefabs those scenes actually use. Prefabs that merely sit in the project are ignored:
                // an unused prefab with Dynamic Bones on it is not a problem for this avatar.
                Dictionary<string, bool> exists = new Dictionary<string, bool>();
                Func<string, bool> guidExists = g => { bool e; if (!exists.TryGetValue(g, out e)) { e = !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(g)); exists[g] = e; } return e; };
                List<string> files = new List<string>();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene sc = SceneManager.GetSceneAt(i);
                    if (!sc.isLoaded) continue;
                    if (!string.IsNullOrEmpty(sc.path)) files.Add(sc.path);
                    foreach (GameObject root in sc.GetRootGameObjects())
                    {
                        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                        {
                            if (!PrefabUtility.IsPartOfPrefabInstance(t.gameObject)) continue;
                            // Walk the whole variant/nesting chain so a missing script in a base prefab is found too.
                            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                            int guard = 0;
                            while (source != null && guard++ < 16)
                            {
                                string path = AssetDatabase.GetAssetPath(source);
                                if (!string.IsNullOrEmpty(path) && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) && !files.Contains(path)) files.Add(path);
                                source = PrefabUtility.GetCorrespondingObjectFromSource(source);
                            }
                        }
                    }
                }
                Dictionary<string, HashSet<string>> filesByTool = new Dictionary<string, HashSet<string>>();
                Dictionary<string, int> countByTool = new Dictionary<string, int>();
                Dictionary<string, string> nameByTool = new Dictionary<string, string>();
                Dictionary<string, HashSet<string>> unknownFiles = new Dictionary<string, HashSet<string>>();
                int unknownCount = 0;
                foreach (string file in files.Distinct())
                {
                    string yaml;
                    try { yaml = File.ReadAllText(Path.Combine(ProjectPath, file)); } catch (Exception) { continue; }
                    if (yaml.IndexOf("m_Script:", StringComparison.Ordinal) < 0) continue;
                    foreach (KeyValuePair<string, KnownSignatures.Match> hit in KnownSignatures.MissingScriptsIn(yaml, guidExists))
                    {
                        if (hit.Value != null && ToolCatalog.Get(hit.Value.ToolKey) != null)
                        {
                            string key = hit.Value.ToolKey;
                            if (!filesByTool.ContainsKey(key)) { filesByTool[key] = new HashSet<string>(); countByTool[key] = 0; }
                            filesByTool[key].Add(file);
                            countByTool[key]++;
                            nameByTool[key] = hit.Value.Name;
                        }
                        else
                        {
                            if (!unknownFiles.ContainsKey(hit.Key)) unknownFiles[hit.Key] = new HashSet<string>();
                            unknownFiles[hit.Key].Add(file);
                            unknownCount++;
                        }
                    }
                }
                foreach (KeyValuePair<string, HashSet<string>> group in filesByTool)
                {
                    Tool t = ToolCatalog.Get(group.Key);
                    ToolCatalog.Status status = ToolCatalog.GetStatus(t, false);
                    string detail = Plural(countByTool[group.Key], "component", "components") + " (such as " + UnityEditor.ObjectNames.NicifyVariableName(nameByTool[group.Key]) + ") in:\n" + string.Join("\n", group.Value.Take(12)) + (group.Value.Count > 12 ? "\n… and " + (group.Value.Count - 12) + " more" : "");
                    if (t.Key == "dynamic-bone") detail += "\nVRChat replaced Dynamic Bone with PhysBones. Install Dynamic Bone (you must own it) and use the SDK's converter, or strip the components with Remove Missing Scripts.";
                    if (t.Key.StartsWith("vrcsdk") && isVRCCreatorCompanion) detail += "\nThis is a Creator Companion project: update or repair the VRChat SDK from the Creator Companion.";
                    if (status.Installed)
                    {
                        r.Findings.Add(new Finding { Id = "scripts-stale-" + t.Key, Severity = "warning", Title = "Some objects use " + t.Name + " components from an older copy", Detail = detail + "\n" + t.Name + " is installed, but these components were made with an older copy that works differently, so Unity can't connect them. Add them again by hand, or strip the broken ones with Remove Missing Scripts." });
                    }
                    else
                    {
                        r.Findings.Add(new Finding { Id = "scripts-need-" + t.Key, Severity = "error", Title = "Objects use " + t.Name + " components, but " + t.Name + " is not installed", Detail = detail, FixLabel = t.Paid ? "Install " + t.Name + " (needs your purchased copy)" : "Install " + t.Name, Recompiles = true, Fix = () => ToolCatalog.Install(t) });
                    }
                }
                if (unknownCount > 0)
                {
                    string detail = "In: " + string.Join(", ", unknownFiles.SelectMany(kv => kv.Value).Distinct().Take(8)) + (unknownFiles.SelectMany(kv => kv.Value).Distinct().Count() > 8 ? " …" : "");
                    r.Findings.Add(new Finding { Id = "scripts-unknown", Severity = "info", Title = Plural(unknownCount, "broken component", "broken components") + " from a tool kebinImports doesn't recognise", Detail = detail + "\nThey came from something that isn't installed any more. Reinstall it if you still need it, or strip them with Remove Missing Scripts." });
                }
                // Live count in the open scenes (covers unsaved objects too) with the generic fix.
                int count = 0;
                List<GameObject> roots = new List<GameObject>();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        if (IsPreviewRoot(root)) continue;
                        roots.Add(root);
                        foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    }
                }
                if (count == 0) return;
                r.Findings.Add(new Finding { Id = "missing-scripts", Severity = "warning", Title = Plural(count, "broken component", "broken components") + " in the scene", Detail = "These are components whose script no longer exists (Unity shows them as \"Missing Script\"). If a problem above names the tool they came from, install it instead. Otherwise it's safe to remove them.", FixLabel = "Remove the broken components", Safe = false, Fix = () =>
                {
                    List<GameObject> now = new List<GameObject>();
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        Scene scene = SceneManager.GetSceneAt(i);
                        if (scene.isLoaded) now.AddRange(scene.GetRootGameObjects().Where(g => !IsPreviewRoot(g)));
                    }
                    int c, g2;
                    RemoveMissingScriptsTool.RemoveFrom(now, out c, out g2);
                    Debug.Log("[kebinImports] Removed " + Plural(c, "broken component", "broken components") + " from " + Plural(g2, "object", "objects") + ".");
                } });
            }
        }

        private class ProjectDoctorWindow : EditorWindow
        {
            private Vector2 scroll;
            private GUIStyle wrap, titleStyle, fixButton;
            private bool running;
            private float logicalWidth;

            [MenuItem("kebinImports/Project Doctor", false, 20)]
            private static void ShowWindow()
            {
                ProjectDoctorWindow w = GetWindow<ProjectDoctorWindow>(true, "kebinImports - Project Doctor");
                SizeWindow(w, 600, 440, true);
                w.Show();
                if (ProjectDoctor.Last == null) w.RunDoctor();
            }
            private static string Summary(ProjectDoctor.Report r)
            {
                List<string> parts = new List<string>();
                if (r.Errors > 0) parts.Add(r.Errors + (r.Errors == 1 ? " problem" : " problems"));
                if (r.Warnings > 0) parts.Add(r.Warnings + (r.Warnings == 1 ? " warning" : " warnings"));
                int notes = r.Findings.Count - r.Errors - r.Warnings;
                if (notes > 0) parts.Add(notes + (notes == 1 ? " note" : " notes"));
                return string.Join(", ", parts);
            }
            // Runs an action after this GUI frame, so the report never changes while the window is being laid out.
            private void Later(Action action)
            {
                EditorApplication.delayCall += () => { action(); Repaint(); };
            }
            private void RunDoctor()
            {
                running = true;
                try { ProjectDoctor.Run(); }
                finally { running = false; Repaint(); }
            }
            private void OnGUI()
            {
                SizeWindow(this, 600, 440, true);
                logicalWidth = BeginScaledGUI(this).width;
                try { DrawContents(); }
                finally { EndScaledGUI(); }
            }
            private void DrawContents()
            {
                if (wrap == null)
                {
                    wrap = new GUIStyle(EditorStyles.label) { wordWrap = true };
                    titleStyle = new GUIStyle(EditorStyles.boldLabel) { wordWrap = true };
                    fixButton = new GUIStyle(GUI.skin.button) { wordWrap = true };
                }
                // A big, full-width button at the top: the first thing you see and the thing you use most.
                GUILayout.Space(6);
                GUIStyle checkStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
                if (GUILayout.Button(running ? "Checking…" : "Check the project again", checkStyle, GUILayout.Height(34), GUILayout.ExpandWidth(true))) Later(RunDoctor);
                GUILayout.Space(4);
                ProjectDoctor.Report r = ProjectDoctor.Last;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(r == null ? "" : r.Findings.Count == 0 ? "No problems found." : Summary(r) + "  ·  checked at " + r.Time.ToString("HH:mm"), EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (r != null && r.Findings.Any(f => f.Fix != null && f.Safe) && GUILayout.Button("Fix all safe", GUILayout.Width(110)))
                {
                    List<ProjectDoctor.Finding> safe = r.Findings.Where(f => f.Fix != null && f.Safe).ToList();
                    Later(() =>
                    {
                        foreach (ProjectDoctor.Finding f in safe) { try { ProjectDoctor.Fix(f.Id); } catch (Exception e) { Debug.LogError("[kebinImports] " + f.Title + ": " + e.Message); } }
                        RunDoctor();
                    });
                }
                EditorGUILayout.EndHorizontal();
                if (r == null) { EditorGUILayout.HelpBox("Click \"Check the project again\" to scan the project.", MessageType.None); return; }
                // Vertical scrollbar only; rows are sized to the window so nothing ever runs off the right edge.
                scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView, GUILayout.ExpandHeight(true));
                float rowWidth = logicalWidth - 34;           // window minus scrollbar and box padding
                float buttonWidth = Mathf.Clamp(rowWidth * 0.32f, 130f, 220f);
                float titleWidth = rowWidth - 64 - buttonWidth - 24;
                if (r.Findings.Count == 0) EditorGUILayout.HelpBox("Everything kebinImports knows how to check looks fine.", MessageType.Info);
                foreach (ProjectDoctor.Finding f in r.Findings.ToList())
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(rowWidth));
                    EditorGUILayout.BeginHorizontal();
                    Color old = GUI.color;
                    GUI.color = f.Severity == "error" ? new Color(1f, 0.55f, 0.55f) : f.Severity == "warning" ? new Color(1f, 0.85f, 0.5f) : new Color(0.8f, 0.85f, 1f);
                    GUILayout.Label(f.Severity == "error" ? "PROBLEM" : f.Severity == "warning" ? "WARNING" : "NOTE", EditorStyles.miniBoldLabel, GUILayout.Width(64));
                    GUI.color = old;
                    GUILayout.Label(f.Title, titleStyle, GUILayout.Width(titleWidth));
                    GUILayout.FlexibleSpace();
                    if (f.Fix != null && GUILayout.Button(f.FixLabel, fixButton, GUILayout.Width(buttonWidth)))
                    {
                        ProjectDoctor.Finding finding = f;
                        Later(() =>
                        {
                            if ((finding.Recompiles || !finding.Safe) && !EditorUtility.DisplayDialog("kebinImports", finding.FixLabel + "?\n\n" + finding.Title + "." + (finding.Recompiles ? "\n\nUnity will recompile scripts afterwards." : "\n\nYou can undo this with Ctrl+Z."), "Do it", "Cancel")) return;
                            try { Debug.Log("[kebinImports] " + ProjectDoctor.Fix(finding.Id)); }
                            catch (Exception e) { EditorUtility.DisplayDialog("kebinImports", "That fix didn't work.\n\n" + FriendlyError(e, "Project Doctor fix " + finding.Id + " failed"), "Ok"); }
                            RunDoctor();
                        });
                    }
                    EditorGUILayout.EndHorizontal();
                    if (!string.IsNullOrEmpty(f.Detail)) EditorGUILayout.LabelField(f.Detail, wrap, GUILayout.Width(rowWidth - 16));
                    EditorGUILayout.EndVertical();
                }
                GUILayout.EndScrollView();
            }
        }
    }
}
