using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports
    {
        private const string GitHubApi = "https://api.github.com/repos/";
        private static HttpClient client = null;
        private static JSONNode jsonNode = null;
        private static bool hideWarnings = false, showSplash = true, isVRCCreatorCompanion = false;
        private static string installedPath;
        private static string ProjectPath => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string PackagesPath => Path.Combine(ProjectPath, "Packages");
        private static string DownloadPath => Path.Combine(Application.persistentDataPath, "kebinImports");
        private static string ExtractedPath => Path.Combine(DownloadPath, "Extracted");

        private static void ClearLog()
        {
            EditorApplication.update -= ClearLog;
            try
            {
                Assembly assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
                System.Type type = assembly.GetType("UnityEditor.LogEntries");
                MethodInfo method = type?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                method?.Invoke(null, null);
            }
            catch (Exception)
            {
                // Clearing the console is cosmetic; never let it break an import.
            }
        }
        private static bool[] Str2BoolArr(string str)
        {
            bool[] boolArr = new bool[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                boolArr[i] = str[i] == '1';
            }
            return boolArr;
        }
        private static string BoolArr2Str(bool[] boolArr)
        {
            string str = string.Empty;
            foreach (bool b in boolArr)
            {
                str += b ? "1" : "0";
            }
            return str;
        }
        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            if (File.Exists(path + ".meta"))
            {
                File.Delete(path + ".meta");
            }
        }
        private static void RemoveAssets(string[] assets)
        {
            if (assets != null && assets.Length > 0)
            {
                foreach (string asset in assets)
                {
                    string path = Path.Combine(Application.dataPath, asset);
                    DeleteDirectory(path);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    if (File.Exists(path + ".meta"))
                    {
                        File.Delete(path + ".meta");
                    }
                }
            }
            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        private static void RemoveJsonDependencies(string file, string[] packages)
        {
            if (!File.Exists(file)) return;
            JSONNode node = JSON.Parse(File.ReadAllText(file));
            if (node == null || !node.HasKey("dependencies")) return;
            bool changed = false;
            foreach (string package in packages)
            {
                if (node["dependencies"].AsObject.HasKey(package))
                {
                    node["dependencies"].AsObject.Remove(package);
                    changed = true;
                }
            }
            if (changed)
            {
                File.WriteAllText(file, node.ToString(2));
            }
        }
        private static void RemovePackages(string[] packages)
        {
            if (packages != null && packages.Length > 0)
            {
                foreach (string package in packages)
                {
                    DeleteDirectory(Path.Combine(PackagesPath, package));
                    UnityEditor.PackageManager.Requests.RemoveRequest removeRequest = UnityEditor.PackageManager.Client.Remove(package);
                    WaitForPackageManager(removeRequest, "removing " + package);
                    if (removeRequest.Status == UnityEditor.PackageManager.StatusCode.Success)
                    {
                        Debug.Log($"[kebinImports] Removed package: {package}");
                    }
                }
                RemoveJsonDependencies(Path.Combine(PackagesPath, "manifest.json"), packages);
                RemoveJsonDependencies(Path.Combine(PackagesPath, "packages-lock.json"), packages);
            }
            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        // What to tell a person when an install goes wrong: one plain sentence. The technical details go to the Console.
        internal static string FriendlyError(Exception e, string what)
        {
            Exception inner = e;
            while ((inner is AggregateException || inner is System.Reflection.TargetInvocationException) && inner.InnerException != null) inner = inner.InnerException;
            Debug.LogWarning("[kebinImports] " + what + ": " + inner);
            string m = inner.Message ?? "";
            if (inner is System.Net.Http.HttpRequestException || inner is System.Net.WebException || m.Contains("Response status code") || m.Contains("unreachable"))
                return "The download couldn't be reached. Check your internet connection and try again. If it keeps happening, the tool may have moved its downloads.";
            if (inner is TimeoutException || inner is TaskCanceledException) return "This took too long and was stopped. Check your internet connection and try again.";
            if (inner is UnauthorizedAccessException || inner is IOException) return "A file couldn't be written. If Unity or another program has the project's files open (a file explorer window, an antivirus scan), close it and try again.";
            if (m.StartsWith("PLAIN:")) return m.Substring(6);
            return "Something went wrong. The details are in Unity's Console.";
        }
        // Waits for a Package Manager request, giving up after a few minutes instead of hanging the editor forever.
        internal static void WaitForPackageManager(UnityEditor.PackageManager.Requests.Request request, string what)
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            while (!request.IsCompleted)
            {
                if (sw.Elapsed.TotalSeconds > 180) throw new TimeoutException("The Package Manager did not finish " + what + " within 3 minutes.");
                Thread.Sleep(20);
            }
        }
        // Adds a package from the Unity registry (or any id the Package Manager accepts) and waits for the result.
        private static void AddUnityPackage(string id, string displayName)
        {
            UnityEditor.PackageManager.Requests.AddRequest request = UnityEditor.PackageManager.Client.Add(id);
            EditorUtility.DisplayProgressBar("kebinImports", "Adding " + id + "…", 0.5f);
            try
            {
                WaitForPackageManager(request, "adding " + id);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
            {
                Debug.Log("[kebinImports] Added " + request.Result.name + " " + request.Result.version);
            }
            else
            {
                string message = request.Error != null ? request.Error.message : "unknown error";
                Debug.LogError("[kebinImports] Could not add " + id + ": " + message);
                EditorUtility.DisplayDialog("kebinImports", "Could not add " + displayName + ":\n\n" + message, "Ok");
            }
        }
        // ---------------------------------------------------------------- UI scale
        // 0 = automatic (from the monitor's resolution); otherwise a multiplier applied to every kebinImports window.
        // The menu bar itself is drawn by the OS and follows Windows display scaling / Unity's UI Scaling preference.
        private static float uiScaleSetting = 0f;
        internal static float UiScale
        {
            get
            {
                float s = uiScaleSetting > 0f ? uiScaleSetting : AutoUiScale();
                return Mathf.Clamp(s, 1f, 3f);
            }
        }
        // 1080p at 100% Windows scaling is the baseline; Unity already applies Windows DPI scaling through pixelsPerPoint.
        internal static float AutoUiScale()
        {
            float s = Screen.currentResolution.height / 1080f / EditorGUIUtility.pixelsPerPoint;
            return Mathf.Max(1f, Mathf.Round(s * 4f) / 4f);
        }
        internal static void SetUiScale(float value)
        {
            uiScaleSetting = value;
            EditorPrefs.SetFloat("kebinImports.uiScale", value);
        }
        // Call first in OnGUI; everything drawn until EndScaledGUI is scaled by UiScale. Returns the logical window rect.
        internal static Rect BeginScaledGUI(EditorWindow window)
        {
            float s = UiScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            Rect logical = new Rect(0f, 0f, window.position.width / s, window.position.height / s);
            GUILayout.BeginArea(logical);
            return logical;
        }
        internal static void EndScaledGUI()
        {
            GUILayout.EndArea();
            GUI.matrix = Matrix4x4.identity;
        }
        // Sizes a window in logical units so it grows with the UI scale; safe to call every OnGUI.
        internal static void SizeWindow(EditorWindow window, float width, float height, bool fixedSize)
        {
            Vector2 size = new Vector2(Mathf.Round(width * UiScale), Mathf.Round(height * UiScale));
            if (fixedSize)
            {
                if (window.minSize != size || window.maxSize != size) { window.minSize = size; window.maxSize = size; }
            }
            else if (window.minSize != size) window.minSize = size;
        }
        private static void ClearScriptingDefineSymbols()
        {
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, string.Empty);
        }
        private static string FileNameFromUrl(string url, string fallback)
        {
            string name = url;
            int query = name.IndexOfAny(new[] { '?', '#' });
            if (query >= 0) name = name.Substring(0, query);
            name = name.Substring(name.LastIndexOf('/') + 1);
            name = Uri.UnescapeDataString(name);
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            if (string.IsNullOrEmpty(name) || !Path.HasExtension(name))
            {
                name = string.IsNullOrEmpty(fallback) ? "download.bin" : fallback + ".bin";
            }
            return name;
        }
        private enum ArchiveKind { Unknown, UnityPackage, Zip }
        // .unitypackage files are gzip streams, VPM/source releases are zip archives. Sniff the bytes instead of trusting the URL.
        private static ArchiveKind DetectArchiveKind(string path)
        {
            byte[] header = new byte[4];
            using (FileStream fs = File.OpenRead(path))
            {
                if (fs.Read(header, 0, header.Length) < 2) return ArchiveKind.Unknown;
            }
            if (header[0] == 0x1F && header[1] == 0x8B) return ArchiveKind.UnityPackage;
            if (header[0] == 0x50 && header[1] == 0x4B) return ArchiveKind.Zip;
            return ArchiveKind.Unknown;
        }
        // True while a non-interactive AssetDatabase.ImportPackage is still pending on the next editor tick.
        private static bool deferredImport = false;
        private static readonly MethodInfo importPackageImmediately = typeof(AssetDatabase).GetMethod("ImportPackageImmediately", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { typeof(string) }, null);
        // AssetDatabase.ImportPackage(path, false) only queues the import for the next editor tick. Unity's own package
        // window finishes the job synchronously through ImportPackageImmediately, so use that when it is available.
        private static void ImportUnityPackage(string unitypackage)
        {
            if (importPackageImmediately != null)
            {
                importPackageImmediately.Invoke(null, new object[] { unitypackage });
            }
            else
            {
                deferredImport = true;
                AssetDatabase.ImportPackage(unitypackage, false);
            }
        }
        private static void ImportUnityPackages(string directory)
        {
            foreach (string unitypackage in Directory.GetFiles(directory, "*.unitypackage", SearchOption.AllDirectories))
            {
                ImportUnityPackage(unitypackage);
            }
        }
        // Downloads are kept until the next import in case a queued package import is still reading them.
        private static void CleanDownloads(string keep)
        {
            if (!Directory.Exists(DownloadPath)) return;
            string keepFullPath = !string.IsNullOrEmpty(keep) && File.Exists(keep) ? Path.GetFullPath(keep) : null;
            foreach (string file in Directory.GetFiles(DownloadPath))
            {
                if (keepFullPath == null || !string.Equals(Path.GetFullPath(file), keepFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(file);
                }
            }
            DeleteDirectory(ExtractedPath);
        }
        // Installs a VPM/UPM package zip (package.json at the root) as an embedded package under Packages/<name>.
        private static void InstallPackageZip(string zipPath, string packageJson)
        {
            JSONNode package = JSON.Parse(packageJson);
            string name = package["name"].Value;
            string version = package["version"].Value;
            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidDataException("package.json inside " + zipPath + " has no name.");
            }
            string target = Vpm.PackageFolder(name);
            DeleteDirectory(target);
            try
            {
                ZipFile.ExtractToDirectory(zipPath, target);
            }
            catch (Exception)
            {
                // Never leave a half-extracted package behind; Unity would fail to resolve it on every reload.
                DeleteDirectory(target);
                throw;
            }
            UpdateVpmManifest(name, version);
            JSONNode dependencies = package["vpmDependencies"];
            if (dependencies != null && dependencies.IsObject)
            {
                foreach (KeyValuePair<string, JSONNode> dependency in dependencies.AsObject)
                {
                    if (!Directory.Exists(Path.Combine(PackagesPath, dependency.Key)))
                    {
                        Debug.LogWarning($"[kebinImports] {name} depends on {dependency.Key} {dependency.Value.Value}, which is not installed. Install it through the VRChat Creator Companion or kebinImports.");
                    }
                }
            }
            Debug.Log($"[kebinImports] Installed {name} {version} to Packages/{name}");
        }
        // Keeps the Creator Companion's manifest in sync so its resolver does not roll the package back on the next project open.
        private static void UpdateVpmManifest(string name, string version)
        {
            string manifestPath = Path.Combine(PackagesPath, "vpm-manifest.json");
            if (!File.Exists(manifestPath) || string.IsNullOrEmpty(version)) return;
            JSONNode manifest = JSON.Parse(File.ReadAllText(manifestPath));
            if (manifest == null) return;
            bool changed = false;
            if (manifest.HasKey("dependencies") && manifest["dependencies"].HasKey(name))
            {
                manifest["dependencies"][name]["version"] = version;
                changed = true;
            }
            if (manifest.HasKey("locked") && manifest["locked"].HasKey(name))
            {
                manifest["locked"][name]["version"] = version;
                changed = true;
            }
            if (changed)
            {
                File.WriteAllText(manifestPath, manifest.ToString(2));
            }
        }
        private static void ImportArchive(string path, string name)
        {
            switch (DetectArchiveKind(path))
            {
                case ArchiveKind.UnityPackage:
                    ImportUnityPackage(path);
                    break;
                case ArchiveKind.Zip:
                    string packageJson = null;
                    bool containsUnityPackage = false;
                    using (ZipArchive archive = ZipFile.OpenRead(path))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            if (entry.FullName == "package.json")
                            {
                                using (StreamReader reader = new StreamReader(entry.Open()))
                                {
                                    packageJson = reader.ReadToEnd();
                                }
                            }
                            else if (entry.FullName.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
                            {
                                containsUnityPackage = true;
                            }
                        }
                    }
                    if (packageJson != null)
                    {
                        InstallPackageZip(path, packageJson);
                    }
                    else if (containsUnityPackage)
                    {
                        DeleteDirectory(ExtractedPath);
                        ZipFile.ExtractToDirectory(path, ExtractedPath);
                        ImportUnityPackages(ExtractedPath);
                        DeleteDirectory(ExtractedPath);
                    }
                    else
                    {
                        string folder = string.IsNullOrEmpty(name) ? Path.GetFileNameWithoutExtension(path) : name;
                        ZipFile.ExtractToDirectory(path, Path.Combine(Application.dataPath, folder));
                    }
                    break;
                default:
                    throw new InvalidDataException("PLAIN:The download wasn't the file kebinImports expected. The tool may have changed where it publishes its downloads.");
            }
        }
        private static void ImportAsset(string pathOrURL, string[] assets = null, string[] packages = null, bool official = false, bool zippedFiles = false, bool zippedAssetPackage = false, string name = null)
        {
            if (string.IsNullOrEmpty(pathOrURL))
            {
                EditorUtility.DisplayDialog("kebinImports", "No download was found for " + (name ?? "this asset") + ". The release may have moved. Please report this on the Discord.", "Ok");
                return;
            }
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
            Selection.activeGameObject = null;
            EditorApplication.LockReloadAssemblies();
            string path = string.Empty;
            bool file = false;
            deferredImport = false;
            try
            {
                CleanDownloads(keep: pathOrURL);
                // Download and check the new copy first; the old one is only removed once the new one is in hand.
                if (!pathOrURL.StartsWith("http", StringComparison.OrdinalIgnoreCase) && File.Exists(pathOrURL))
                {
                    path = pathOrURL;
                    file = true;
                }
                else
                {
                    Directory.CreateDirectory(DownloadPath);
                    path = Path.Combine(DownloadPath, FileNameFromUrl(pathOrURL, name));
                    if (client == null) client = new HttpClient();
                    EditorUtility.DisplayProgressBar("kebinImports", "Downloading " + Path.GetFileName(path) + "...", 0.5f);
                    try
                    {
                        Task.Run(() => HttpClient.DownloadFile(client, pathOrURL, path)).Wait();
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                    }
                }
                if (DetectArchiveKind(path) == ArchiveKind.Unknown)
                {
                    throw new InvalidDataException("PLAIN:The download wasn't the file kebinImports expected. The tool may have changed where it publishes its downloads.");
                }
                RemoveAssets(assets);
                RemovePackages(packages);
                ImportArchive(path, name);
            }
            catch (Exception e)
            {
                Exception inner = e is AggregateException aggregate ? aggregate.InnerException : e;
                Debug.LogError("[kebinImports] Import failed: " + inner.Message + "\n" + pathOrURL);
                EditorUtility.DisplayDialog("kebinImports", "Couldn't install " + (name ?? "this") + ".\n\n" + FriendlyError(inner, "Import of " + pathOrURL + " failed"), "Ok");
            }
            finally
            {
                if (!official && !file && !deferredImport && File.Exists(path))
                {
                    File.Delete(path);
                }
                if (!deferredImport)
                {
                    DeleteDirectory(ExtractedPath);
                }
                AssetDatabase.SaveAssets();
                EditorApplication.UnlockReloadAssemblies();
                AssetDatabase.Refresh();
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                EditorApplication.update += ClearLog;
            }
        }
        private static JSONNode GetLatestRelease(string repository)
        {
            client = new HttpClient(GitHubHeaders: true);
            return JSON.Parse(Task.Run(() => HttpClient.DownloadString(client, GitHubApi + repository + "/releases/latest")).Result);
        }
        // Picks the download for a GitHub release. Creator Companion projects get the VPM zip (installed under Packages/),
        // everything else gets the .unitypackage, with sensible fallbacks when a project only ships one of the two.
        private static string PickReleaseAsset(JSONNode assets, bool preferPackage = true)
        {
            if (assets == null || assets.Count == 0) return null;
            bool isVpmRelease = false;
            for (int i = 0; i < assets.Count; i++)
            {
                if (assets[i]["name"].Value == "package.json") isVpmRelease = true;
            }
            string unitypackage = null, urpUnitypackage = null, zip = null;
            for (int i = 0; i < assets.Count; i++)
            {
                string url = assets[i]["browser_download_url"].Value;
                string lower = assets[i]["name"].Value.ToLowerInvariant();
                if (lower.EndsWith(".unitypackage"))
                {
                    // Some shaders (Poiyomi) also ship a URP build; VRChat projects use the built-in render pipeline.
                    if (lower.Contains("urp")) { if (urpUnitypackage == null) urpUnitypackage = url; }
                    else if (unitypackage == null) unitypackage = url;
                }
                if (zip == null && lower.EndsWith(".zip")) zip = url;
            }
            if (preferPackage && isVRCCreatorCompanion && isVpmRelease && zip != null) return zip;
            return unitypackage ?? zip ?? urpUnitypackage ?? assets[0]["browser_download_url"].Value;
        }
        // Returns null instead of throwing when GitHub is unreachable or rate limited, so ImportAsset can show a dialog
        // and an Essentials batch carries on with the next item instead of aborting with an unhandled exception.
        private static string GetLatestReleaseAsset(string repository, bool preferPackage = true)
        {
            try
            {
                return PickReleaseAsset(GetLatestRelease(repository)["assets"], preferPackage);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[kebinImports] Could not look up the latest release of " + repository + ": " + (e.InnerException ?? e).Message);
                return null;
            }
        }
        // Checks VRChat's config endpoint for the Unity version the current SDK expects. Returns true when the check
        // passes, false (after telling the user) when it fails or the endpoint cannot be reached.
        private static bool VRChatSdkSupportsThisUnity()
        {
            string sdkUnityVersion;
            try
            {
                client = new HttpClient();
                sdkUnityVersion = JSON.Parse(Task.Run(() => HttpClient.DownloadString(client, "https://api.vrchat.cloud/api/1/config")).Result)["sdkUnityVersion"].Value;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[kebinImports] Could not reach the VRChat API: " + (e.InnerException ?? e).Message);
                EditorUtility.DisplayDialog("kebinImports", "Could not reach the VRChat API to check which Unity version the SDK needs. Please check your connection and try again.", "Ok");
                return false;
            }
            if (Application.unityVersion != sdkUnityVersion)
            {
                EditorUtility.DisplayDialog("Error caught.", "kebinImports saved you from making a fucky wucky!\n\nPlease use Unity " + sdkUnityVersion + " to import the VRChat SDK!", "Ok");
                return false;
            }
            return true;
        }
        private static string ResolveInstalledPath()
        {
            UnityEditor.PackageManager.PackageInfo info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(kebinImports).Assembly);
            if (info != null && Directory.Exists(info.resolvedPath))
            {
                return info.resolvedPath;
            }
            string legacy = Path.Combine(Application.dataPath, "kebinImports");
            if (Directory.Exists(legacy))
            {
                return legacy;
            }
            return Path.Combine(PackagesPath, "dev.kebin.kebinimports");
        }
        private static JSONNode PackageJson => JSON.Parse(File.ReadAllText(Path.Combine(installedPath, "package.json")));
        private static void UpdateSelf()
        {
            EditorApplication.update -= UpdateSelf;
            // Package installs (Creator Companion / Package Manager) are updated by their package manager, not by us.
            if (Application.isBatchMode || !installedPath.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                jsonNode = GetLatestRelease("EEkebin/kebinImports");
                string path = Path.Combine(DownloadPath, "kebinImports.unitypackage");
                Version currVersion, newVersion;
                if (!Version.TryParse(PackageJson["version"].Value, out currVersion)) return;
                if (!Version.TryParse(jsonNode["tag_name"].Value.TrimStart('v', 'V'), out newVersion)) return;
                if (currVersion < newVersion)
                {
                    if (EditorWindow.HasOpenInstances<SettingsWindow>())
                    {
                        EditorWindow.GetWindow<SettingsWindow>().Close();
                    }
                    Directory.CreateDirectory(DownloadPath);
                    Task.Run(() => HttpClient.DownloadFile(client, "https://github.com/EEkebin/kebinImports/releases/latest/download/kebinImports.unitypackage", path)).Wait();
                    DeleteDirectory(installedPath);
                    ImportAsset(pathOrURL: path);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[kebinImports] Update check skipped: " + (e.InnerException ?? e).Message);
            }
        }
        private static void UpdateSettings()
        {
            EditorApplication.update -= UpdateSettings;
            hideWarnings = EditorPrefs.GetBool("kebinImports.hideWarnings", false);
            showSplash = EditorPrefs.GetBool("kebinImports.showSplash", true);
            showLegacy = EditorPrefs.GetBool("kebinImports.showLegacy", false);
            uiScaleSetting = EditorPrefs.GetFloat("kebinImports.uiScale", 0f);
            // Only the VRChat SDK is on by default; everything else (including VRCFury) is opt-in through Customize Essentials.
            LoadEssentials();
            // Every Creator Companion / ALCOM project has the VPM resolver package. vpm-manifest.json alone is not enough:
            // kebinImports creates it in plain projects too when it installs a VPM package.
            isVRCCreatorCompanion = Directory.Exists(Path.Combine(PackagesPath, "com.vrchat.core.vpm-resolver"));
        }
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            installedPath = ResolveInstalledPath();
            UpdateSettings();
            if (Application.isBatchMode) return;
            if (showSplash)
            {
                EditorApplication.update += SettingsWindow.ShowWindow;
            }
            EditorApplication.update += UpdateSelf;
            EditorApplication.update += ClearLog;
        }
    }
}
