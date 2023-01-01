using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        private static HttpClient client = null;
        private static JSONNode jsonNode = null;
        private static bool hideWarnings = false, showSplash = true, isVRCCreatorCompanion = false;
        private static string installedPath;
        private static void ClearLog()
        {
            EditorApplication.update -= ClearLog;
            Assembly assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            System.Type type = assembly.GetType("UnityEditor.LogEntries");
            MethodInfo method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
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
        private static void RemoveAssets(string[] assets)
        {
            if (assets != null && assets.Length > 0)
            {
                foreach (string asset in assets)
                {
                    if (Directory.Exists(Application.dataPath + "/" + asset))
                    {
                        Directory.Delete(Application.dataPath + "/" + asset, true);
                    }
                    if (File.Exists(Application.dataPath + "/" + asset))
                    {
                        File.Delete(Application.dataPath + "/" + asset);
                    }
                    if (File.Exists(Application.dataPath + "/" + asset + ".meta"))
                    {
                        File.Delete(Application.dataPath + "/" + asset + ".meta");
                    }
                }
            }
            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        private static void RemovePackages(string[] packages)
        {
            if (packages != null && packages.Length > 0)
            {
                foreach (string package in packages)
                {
                    UnityEditor.PackageManager.Requests.RemoveRequest removeRequest = UnityEditor.PackageManager.Client.Remove(package);
                    while (!removeRequest.IsCompleted)
                    {
                        // Wait until the removal is completed
                    }

                    if (removeRequest.Status == UnityEditor.PackageManager.StatusCode.Success)
                    {
                        Debug.Log($"Successfully removed package: {package}");
                    }
                    else
                    {
                        Debug.LogError($"Failed to remove package: {package}");
                    }
                }
                string manifest = File.ReadAllText(Application.dataPath + "/../Packages/manifest.json");
                string lockfile = File.ReadAllText(Application.dataPath + "/../Packages/packages-lock.json");
                jsonNode = JSON.Parse(manifest);
                foreach (string package in packages)
                {
                    if (jsonNode["dependencies"].AsObject.HasKey(package))
                    {
                        jsonNode["dependencies"].AsObject.Remove(package);
                    }
                }
                File.WriteAllText(Application.dataPath + "/../Packages/manifest.json", jsonNode.ToString());
                jsonNode = JSON.Parse(lockfile);
                foreach (string package in packages)
                {
                    if (jsonNode["dependencies"].AsObject.HasKey(package))
                    {
                        jsonNode["dependencies"].AsObject.Remove(package);
                    }
                }
                File.WriteAllText(Application.dataPath + "/../Packages/packages-lock.json", jsonNode.ToString());
            }
            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        private static void ImportAsset(string pathOrURL, string[] assets = null, string[] packages = null, bool official = false, bool zippedFiles = false, bool zippedAssetPackage = false, string name = null)
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
            Selection.activeGameObject = null;
            EditorApplication.LockReloadAssemblies();
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "");
            RemoveAssets(assets);
            RemovePackages(packages);
            string path = string.Empty;
            bool file = false;
            if (!pathOrURL.StartsWith("http") && File.Exists(pathOrURL))
            {
                path = pathOrURL;
                file = true;
            }
            else
            {
                path = Application.persistentDataPath + "/" + pathOrURL.Substring(pathOrURL.LastIndexOf("/") + 1);
                Task.Run(() => HttpClient.DownloadFile(client, pathOrURL, @path)).Wait();
            }
            if (!zippedFiles && !zippedAssetPackage)
            {
                AssetDatabase.ImportPackage(@path, false);
            }
            else if (zippedFiles && !zippedAssetPackage)
            {
                ZipFile.ExtractToDirectory(@path, Application.dataPath + "/" + name);
            }
            else if (!zippedFiles && zippedAssetPackage)
            {
                ZipFile.ExtractToDirectory(@path, Application.persistentDataPath + "/Extracted");
                foreach (string unitypackage in Directory.GetFiles(Application.persistentDataPath + "/Extracted", "*.unitypackage", SearchOption.AllDirectories))
                {
                    AssetDatabase.ImportPackage(unitypackage, false);
                }
            }
            if (!official && !file && File.Exists(@path))
            {
                File.Delete(@path);
                if (Directory.Exists(Application.persistentDataPath + "/Extracted"))
                {
                    Directory.Delete(Application.persistentDataPath + "/Extracted", true);
                }
            }
            AssetDatabase.SaveAssets();
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "");
            AssetDatabase.Refresh();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            EditorApplication.UnlockReloadAssemblies();
            EditorApplication.update += ClearLog;
        }
        private static void UpdateSelf()
        {
            EditorApplication.update -= UpdateSelf;
            client = new HttpClient(GitHubHeaders: true);
            jsonNode = JSON.Parse(Task.Run(() => HttpClient.DownloadString(client, "https://api.github.com/repos/EEkebin/kebinImports/releases/latest")).Result);
            string path = Application.persistentDataPath + "/kebinImports.unitypackage";
            if (File.Exists(@path))
            {
                File.Delete(@path);
            }
            if (Directory.Exists(Application.dataPath + "/kebinImports"))
            {
                installedPath = Application.dataPath + "/kebinImports";
            }
            else
            {
                installedPath = Application.dataPath + "/../Packages/dev.kebin.kebinimports";
            }
            Version currVersion = new Version(), newVersion = new Version();
            Version.TryParse(JSON.Parse(File.ReadAllText(installedPath + "/package.json"))["version"], out currVersion);
            Version.TryParse(jsonNode["tag_name"].ToString().Trim('\"').Substring(1), out newVersion);
            if (currVersion < newVersion)
            {
                SettingsWindow settingsWindow = EditorWindow.GetWindow<SettingsWindow>(true, "kebinImports");
                settingsWindow.Close();
                Task.Run(() => HttpClient.DownloadFile(client, "https://github.com/EEkebin/kebinImports/releases/latest/download/kebinImports.unitypackage", @path)).Wait();
                Directory.Delete(installedPath, true);
                ImportAsset(
                    pathOrURL: path);
                if (File.Exists(@path))
                {
                    File.Delete(@path);
                }
            }
        }
        private static void UpdateSettings()
        {
            EditorApplication.update -= UpdateSettings;
            hideWarnings = EditorPrefs.GetBool("hideWarnings", false);
            showSplash = EditorPrefs.GetBool("showSplash", true);
            avatarEssentials = Str2BoolArr(EditorPrefs.GetString("avatarEssentials", "1000000000"));
            worldEssentials = Str2BoolArr(EditorPrefs.GetString("worldEssentials", "1000"));
            if (avatarEssentials.Length != 7 && worldEssentials.Length != 4)
            {
                EditorPrefs.SetString("avatarEssentials", "1000000000");
                EditorPrefs.SetString("worldEssentials", "1000");
                avatarEssentials = Str2BoolArr(EditorPrefs.GetString("avatarEssentials", "1000000000"));
                worldEssentials = Str2BoolArr(EditorPrefs.GetString("worldEssentials", "1000"));
            }
            if (Directory.Exists(Application.dataPath + "/../Packages/com.vrchat.base"))
            {
                isVRCCreatorCompanion = true;
            }
            else
            {
                isVRCCreatorCompanion = false;
            }
        }
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.update += UpdateSettings;
            if (showSplash)
            {
                EditorApplication.update += SettingsWindow.ShowWindow;
            }
            EditorApplication.update += UpdateSelf;
            EditorApplication.update += ClearLog;
        }
    }
}
