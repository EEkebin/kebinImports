using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports
    {
        // One tool the importer knows about. Installation prefers VPM (from the tool's official listing), then the
        // Unity registry, then a GitHub release; a few tools need custom handling (paid assets, dead links).
        internal class Tool
        {
            public string Key;
            public string Name;
            public string MenuPath;
            public string Description;
            public string VpmPackage;
            public string UnityPackage;
            public string GitHubRepo;
            public Action Custom;
            // Folders (relative to Assets/, or ../Packages/<x>) left behind by older install methods; removed after a successful install.
            public string[] LegacyAssets = new string[0];
            // Folders that mean "installed" for tools without a package id.
            public string[] InstalledFolders = new string[0];
            public bool Paid;
            public bool Legacy;
            public bool Avatar;
            public bool World;
            // "avatars" or "worlds": the tool only compiles with that VRChat SDK in the project.
            public string NeedsSdk;

            public bool IsPackage => !string.IsNullOrEmpty(VpmPackage);
        }

        internal static class ToolCatalog
        {
            public static readonly List<Tool> Tools = new List<Tool>
            {
                new Tool { Key = "vrcsdk-avatars", Name = "VRChat SDK 3 - Avatars", MenuPath = "kebinImports/VRChat SDK/SDK 3 Avatars", Description = "VRChat avatar SDK from VRChat's official listing", VpmPackage = "com.vrchat.avatars", LegacyAssets = new[] { "../Packages/com.vrchat.vrcsdk3" }, Avatar = true },
                new Tool { Key = "vrcsdk-worlds", Name = "VRChat SDK 3 - Worlds", MenuPath = "kebinImports/VRChat SDK/SDK 3 Worlds", Description = "VRChat world SDK from VRChat's official listing", VpmPackage = "com.vrchat.worlds", LegacyAssets = new[] { "../Packages/com.vrchat.vrcsdk3" }, World = true },
                new Tool { Key = "uts3", Name = "Unity Toon Shader 3", MenuPath = "kebinImports/Shaders/Unity Toon Shader 3", Description = "Unity's official toon shader (com.unity.toonshader) from the Unity registry", UnityPackage = "com.unity.toonshader", Avatar = true, World = true },
                new Tool { Key = "liltoon", Name = "lilToon", MenuPath = "kebinImports/Shaders/lilToon", Description = "lilToon shader", VpmPackage = "jp.lilxyzw.liltoon", GitHubRepo = "lilxyzw/lilToon", LegacyAssets = new[] { "lilToon", "lilToonSetting", "../Packages/lilToon" }, Avatar = true, World = true },
                new Tool { Key = "poiyomi", Name = "Poiyomi Toon Shader", MenuPath = "kebinImports/Shaders/Poiyomi Toon Shader", Description = "Poiyomi Toon, the most used VRChat avatar shader", VpmPackage = "com.poiyomi.toon", GitHubRepo = "poiyomi/PoiyomiToonShader", LegacyAssets = new[] { "_PoiyomiShaders", "../Thry" }, Avatar = true, World = true },
                new Tool { Key = "uts2", Name = "Unity-Chan Toon Shader 2.0", MenuPath = "kebinImports/Shaders/Unity-Chan Toon Shader 2.0", Description = "UTS2 toon shader (shader-only package from GitHub)", Custom = importUTS, InstalledFolders = new[] { "Assets/Toon" }, Avatar = true, World = true },
                new Tool { Key = "gesture-manager", NeedsSdk = "avatars", Name = "Gesture Manager", MenuPath = "kebinImports/Avatar Tools/Gesture Manager", Description = "BlackStartx's Gesture Manager: test expressions, gestures and menus in the editor (VRChat curated)", VpmPackage = "vrchat.blackstartx.gesture-manager", Avatar = true },
                new Tool { Key = "av3emulator", NeedsSdk = "avatars", Name = "Av3Emulator", MenuPath = "kebinImports/Avatar Tools/Av3Emulator", Description = "Lyuma's Av3Emulator: run the avatar's animator and OSC logic in play mode (VRChat curated)", VpmPackage = "lyuma.av3emulator", Avatar = true },
                new Tool { Key = "pumkin", Name = "Pumkin's Avatar Tools", MenuPath = "kebinImports/Avatar Tools/Pumkin's Avatar Tools", Description = "Avatar editing utilities (copy components, thumbnails, PhysBone tools)", VpmPackage = "io.github.rurre.pumkinsavatartools", GitHubRepo = "rurre/PumkinsAvatarTools", LegacyAssets = new[] { "PumkinsAvatarTools" }, Avatar = true },
                new Tool { Key = "mae", Name = "Muscle Animation Editor", MenuPath = "kebinImports/Avatar Tools/Muscle Animation Editor (paid)", Description = "Paid Asset Store tool for editing humanoid animations; the user must own it", Custom = importMAE, InstalledFolders = new[] { "Assets/Muscle Animation Editor" }, Paid = true, Avatar = true },
                new Tool { Key = "cge", NeedsSdk = "avatars", Name = "ComboGestureExpressions", MenuPath = "kebinImports/Avatar Tools/ComboGestureExpressions", Description = "Hai's facial expression / gesture animator tool", VpmPackage = "dev.hai-vr.cge", GitHubRepo = "hai-vr/combo-gesture-expressions-av3", LegacyAssets = new[] { "Hai/AnimationViewer", "Hai/ComboGesture", "Hai/VisualExpressionsEditor" }, Avatar = true },
                new Tool { Key = "modular-avatar", NeedsSdk = "avatars", Name = "Modular Avatar", MenuPath = "kebinImports/Avatar Tools/Modular Avatar", Description = "Modular Avatar (installs NDMF too): non-destructive outfit and menu merging", VpmPackage = "nadena.dev.modular-avatar", GitHubRepo = "bdunderscore/modular-avatar", LegacyAssets = new[] { "../Packages/Modular Avatar" }, Avatar = true },
                new Tool { Key = "dressingtools", Name = "DressingTools", MenuPath = "kebinImports/Avatar Tools/DressingTools", Description = "chocopoi's outfit dressing tool", VpmPackage = "com.chocopoi.vrc.dressingtools", GitHubRepo = "poi-vrc/DressingTools", LegacyAssets = new[] { "chocopoi/DressingTools" }, Avatar = true },
                new Tool { Key = "apt", NeedsSdk = "avatars", Name = "Avatar Performance Tools", MenuPath = "kebinImports/Avatar Tools/Avatar Performance Tools", Description = "Thry's avatar performance analysis", VpmPackage = "de.thryrallo.vrc.avatar-performance-tools", GitHubRepo = "Thryrallo/VRC-Avatar-Performance-Tools", Avatar = true },
                new Tool { Key = "vrcfury", NeedsSdk = "avatars", Name = "VRCFury", MenuPath = "kebinImports/Avatar Tools/VRCFury", Description = "VRCFury non-destructive avatar building (toggles, armature link, ...)", VpmPackage = "com.vrcfury.vrcfury", GitHubRepo = "VRCFury/VRCFury", LegacyAssets = new[] { "VRCFury" }, Avatar = true },
                new Tool { Key = "vrworld-toolkit", NeedsSdk = "worlds", Name = "VRWorld Toolkit", MenuPath = "kebinImports/World Tools/VRWorld Toolkit", Description = "World Debugger, post-processing setup and other world building helpers (VRChat curated)", VpmPackage = "dev.onevr.vrworldtoolkit", World = true },
                new Tool { Key = "audiolink", NeedsSdk = "worlds", Name = "AudioLink", MenuPath = "kebinImports/World Tools/AudioLink", Description = "Audio-reactive shaders and Udon for worlds (VRChat curated)", VpmPackage = "com.llealloo.audiolink", World = true },
                new Tool { Key = "xiexe", Name = "Xiexe's Unity Shaders", MenuPath = "kebinImports/Shaders/Xiexe's Unity Shaders", Description = "XSToon shaders (includes fur)", GitHubRepo = "Xiexe/Xiexes-Unity-Shaders", LegacyAssets = new[] { "Xiexes-Unity-Shaders" }, InstalledFolders = new[] { "Assets/Xiexes-Unity-Shaders" } },
                new Tool { Key = "mochie", Name = "Mochie's Unity Shaders", MenuPath = "kebinImports/Shaders/Mochie's Unity Shaders", Description = "Mochie's shaders", GitHubRepo = "MochiesCode/Mochies-Unity-Shaders", LegacyAssets = new[] { "Mochie" }, InstalledFolders = new[] { "Assets/Mochie" } },
                new Tool { Key = "rero", Name = "reroStandard Shaders", MenuPath = "kebinImports/Shaders/reroStandard Shaders", Description = "reroStandard shader", GitHubRepo = "RetroGEO/reroStandard", LegacyAssets = new[] { "ReroShaders" }, InstalledFolders = new[] { "Assets/ReroShaders" } },
                new Tool { Key = "dynamic-bone", Name = "Dynamic Bone", MenuPath = "kebinImports/Legacy/Dynamic Bone (paid)", Description = "Paid legacy bone physics; VRChat converts it to PhysBones. The user must own it", Custom = importDB, InstalledFolders = new[] { "Assets/DynamicBone" }, Paid = true, Legacy = true, Avatar = true },
                new Tool { Key = "arktoon", Name = "Arktoon Shader", MenuPath = "kebinImports/Legacy/Arktoon Shader", Description = "Legacy arktoon shader (archived download)", Custom = importAS, InstalledFolders = new[] { "Assets/arktoon Shaders" }, Legacy = true },
                new Tool { Key = "cubed", Name = "Cubed's Unity Shaders", MenuPath = "kebinImports/Legacy/Cubed's Unity Shaders", Description = "Legacy Cubed's shaders", GitHubRepo = "cubedparadox/Cubeds-Unity-Shaders", LegacyAssets = new[] { "Cubed's Unity Shaders" }, InstalledFolders = new[] { "Assets/Cubed's Unity Shaders" }, Legacy = true },
                new Tool { Key = "yukio", Name = "Yukio's Fur Shader", MenuPath = "kebinImports/Legacy/Yukio's Fur Shader", Description = "Legacy fur shader; its only download is gone", Custom = importYFS, InstalledFolders = new[] { "Assets/Yukio's Shaders" }, Legacy = true },
                new Tool { Key = "vrcsdk2", Name = "VRChat SDK 2", MenuPath = "kebinImports/VRChat SDK/SDK 2 (Unity 2019)", Description = "Legacy SDK2 (Unity 2019 only)", Custom = importVRCSDK2, Legacy = true },
            };

            public static Tool Get(string key) => Tools.FirstOrDefault(t => t.Key == key);
            public static Tool Find(string nameOrKey)
            {
                if (string.IsNullOrEmpty(nameOrKey)) return null;
                return Tools.FirstOrDefault(t => t.Key.Equals(nameOrKey, StringComparison.OrdinalIgnoreCase) || t.Name.Equals(nameOrKey, StringComparison.OrdinalIgnoreCase))
                    ?? Tools.FirstOrDefault(t => t.VpmPackage != null && t.VpmPackage.Equals(nameOrKey, StringComparison.OrdinalIgnoreCase))
                    ?? Tools.FirstOrDefault(t => t.Name.IndexOf(nameOrKey, StringComparison.OrdinalIgnoreCase) >= 0 || t.MenuPath.IndexOf(nameOrKey, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // ---------------------------------------------------------------- install / update / remove
            public static void Install(string key, bool update = false)
            {
                Tool t = Get(key);
                if (t == null) throw new ArgumentException("Unknown tool '" + key + "'.");
                Install(t, update);
            }
            public static void Install(Tool t, bool update = false)
            {
                if (t.Custom != null) { t.Custom(); return; }
                if (!OfferSdkFor(t)) return;
                if (t.IsPackage)
                {
                    try
                    {
                        List<VpmPackageVersion> installed = Vpm.Install(t.VpmPackage, null, false, update);
                        CleanLegacy(t);
                        if (installed.Count > 0) Debug.Log("[kebinImports] " + t.Name + ": " + string.Join(", ", installed.Select(p => p.Id + " " + p.Version)));
                        return;
                    }
                    catch (Exception e)
                    {
                        if (string.IsNullOrEmpty(t.GitHubRepo))
                        {
                            EditorUtility.DisplayDialog("kebinImports", "Couldn't install " + t.Name + ".\n\n" + FriendlyError(e, "Install of " + t.Name + " failed"), "Ok");
                            throw;
                        }
                        Debug.LogWarning("[kebinImports] VPM install of " + t.Name + " failed (" + (e.InnerException ?? e).Message + "); falling back to the GitHub release.");
                    }
                }
                if (!string.IsNullOrEmpty(t.UnityPackage))
                {
                    AddUnityPackage(t.UnityPackage + UnityRegistryVersionSuffix(t.UnityPackage), t.Name);
                    return;
                }
                if (!string.IsNullOrEmpty(t.GitHubRepo))
                {
                    ImportAsset(
                        pathOrURL: GetLatestReleaseAsset(t.GitHubRepo),
                        assets: t.LegacyAssets.Concat(t.IsPackage ? new[] { "../Packages/" + t.VpmPackage } : new string[0]).ToArray(),
                        packages: t.IsPackage ? new[] { t.VpmPackage } : null,
                        name: t.Name);
                    return;
                }
                throw new InvalidOperationException(t.Name + " has no install method.");
            }
            // Tools such as Modular Avatar only compile when the VRChat SDK is present (they rely on libraries it ships).
            // Installing one into a project without the SDK would break compilation, so offer to install the SDK first.
            // Returns false when the user cancels.
            private static bool OfferSdkFor(Tool t)
            {
                if (string.IsNullOrEmpty(t.NeedsSdk) || isVRCCreatorCompanion) return true;
                bool hasSdk = System.IO.Directory.Exists(System.IO.Path.Combine(PackagesPath, "com.vrchat.base"))
                    || System.IO.Directory.Exists(System.IO.Path.Combine(Application.dataPath, "VRCSDK"));
                if (hasSdk) return true;
                string sdkName = t.NeedsSdk == "worlds" ? "VRChat SDK 3 - Worlds" : "VRChat SDK 3 - Avatars";
                if (Application.isBatchMode) { Debug.LogWarning("[kebinImports] " + t.Name + " needs the " + sdkName + ", which is not installed."); return true; }
                int choice = EditorUtility.DisplayDialogComplex("kebinImports", t.Name + " needs the " + sdkName + ", which is not in this project. Without it the project will not compile.\n\nInstall the SDK first?", "Install both", "Cancel", "Install " + t.Name + " only");
                if (choice == 1) return false;
                if (choice == 0) Install(t.NeedsSdk == "worlds" ? "vrcsdk-worlds" : "vrcsdk-avatars");
                return true;
            }
            public static void Remove(Tool t)
            {
                if (t.IsPackage && Vpm.InstalledPackages().ContainsKey(t.VpmPackage))
                {
                    Vpm.Remove(t.VpmPackage);
                }
                else if (!string.IsNullOrEmpty(t.UnityPackage))
                {
                    UnityEditor.PackageManager.Requests.RemoveRequest request = UnityEditor.PackageManager.Client.Remove(t.UnityPackage);
                    WaitForPackageManager(request, "removing " + t.UnityPackage);
                    if (request.Status != UnityEditor.PackageManager.StatusCode.Success) throw new Exception("PLAIN:Unity's Package Manager couldn't remove " + t.Name + ". The details are in Unity's Console.");
                }
                CleanLegacy(t);
                foreach (string folder in t.InstalledFolders)
                {
                    string full = Path.Combine(ProjectPath, folder);
                    if (Directory.Exists(full)) { DeleteDirectory(full); }
                }
                AssetDatabase.Refresh();
            }
            // Removes leftovers of older install methods (unitypackage folders under Assets/, renamed package folders).
            public static List<string> CleanLegacy(Tool t)
            {
                List<string> removed = new List<string>();
                foreach (string asset in t.LegacyAssets)
                {
                    string path = Path.GetFullPath(Path.Combine(Application.dataPath, asset));
                    if (!Directory.Exists(path) && !File.Exists(path)) continue;
                    DeleteDirectory(path);
                    if (File.Exists(path)) File.Delete(path);
                    if (File.Exists(path + ".meta")) File.Delete(path + ".meta");
                    removed.Add(asset);
                    PruneEmptyParents(path);
                }
                if (removed.Count > 0)
                {
                    AssetDatabase.Refresh();
                    Debug.Log("[kebinImports] Removed old copies of " + t.Name + ": " + string.Join(", ", removed));
                }
                return removed;
            }
            private static void PruneEmptyParents(string path)
            {
                string dir = Path.GetDirectoryName(path);
                string assets = Path.GetFullPath(Application.dataPath);
                while (dir != null && dir.Length > assets.Length && Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Directory.Delete(dir);
                    if (File.Exists(dir + ".meta")) File.Delete(dir + ".meta");
                    dir = Path.GetDirectoryName(dir);
                }
            }

            // ---------------------------------------------------------------- state
            public class Status
            {
                public Tool Tool;
                public bool Installed;
                public string Location;
                public string InstalledVersion;
                public string LatestVersion;
                public bool UpdateAvailable => Installed && !string.IsNullOrEmpty(LatestVersion) && !string.IsNullOrEmpty(InstalledVersion) && SemVer.TryParse(LatestVersion) != null && SemVer.TryParse(InstalledVersion) != null && SemVer.TryParse(LatestVersion).CompareTo(SemVer.TryParse(InstalledVersion)) > 0;
                public List<string> LegacyCopies = new List<string>();
            }
            public static Status GetStatus(Tool t, bool includeLatest)
            {
                Status s = new Status { Tool = t };
                Dictionary<string, string> installed = Vpm.InstalledPackages();
                if (t.IsPackage && installed.ContainsKey(t.VpmPackage))
                {
                    s.Installed = true;
                    s.Location = "Packages/" + t.VpmPackage;
                    s.InstalledVersion = installed[t.VpmPackage];
                }
                else if (!string.IsNullOrEmpty(t.UnityPackage))
                {
                    string manifest = Path.Combine(PackagesPath, "manifest.json");
                    if (File.Exists(manifest))
                    {
                        JSONNode m = JSON.Parse(Vpm.StripBom(File.ReadAllText(manifest)));
                        if (m != null && m["dependencies"].HasKey(t.UnityPackage)) { s.Installed = true; s.Location = "Package Manager"; s.InstalledVersion = m["dependencies"][t.UnityPackage].Value; }
                    }
                }
                if (!s.Installed)
                {
                    foreach (string folder in t.InstalledFolders)
                    {
                        if (Directory.Exists(Path.Combine(ProjectPath, folder))) { s.Installed = true; s.Location = folder; break; }
                    }
                }
                foreach (string asset in t.LegacyAssets)
                {
                    string path = Path.GetFullPath(Path.Combine(Application.dataPath, asset));
                    if (Directory.Exists(path)) s.LegacyCopies.Add(asset.StartsWith("../") ? asset.Substring(3) : "Assets/" + asset);
                }
                if (includeLatest)
                {
                    try
                    {
                        if (t.IsPackage) { VpmPackageVersion latest = Vpm.FindLatest(t.VpmPackage); if (latest != null) s.LatestVersion = latest.Version; }
                        else if (!string.IsNullOrEmpty(t.UnityPackage)) s.LatestVersion = UnityRegistryLatest(t.UnityPackage);
                    }
                    catch (Exception) { }
                }
                return s;
            }
            public static List<Status> AllStatuses(bool includeLatest) => Tools.Select(t => GetStatus(t, includeLatest)).ToList();

            // ---------------------------------------------------------------- Unity registry
            private static readonly Dictionary<string, string> unityLatestCache = new Dictionary<string, string>();
            public static string UnityRegistryLatest(string packageId)
            {
                string cached;
                if (unityLatestCache.TryGetValue(packageId, out cached)) return cached;
                string version = null;
                try
                {
                    client = new HttpClient();
                    JSONNode registry = JSON.Parse(Task.Run(() => HttpClient.DownloadString(client, "https://packages.unity.com/" + packageId)).Result);
                    Version editor = Vpm.EditorVersion();
                    SemVer best = null;
                    foreach (KeyValuePair<string, JSONNode> kv in registry["versions"].AsObject)
                    {
                        Version required = ParseUnityVersion(kv.Value["unity"].Value);
                        if (required != null && required > editor) continue;
                        SemVer candidate = SemVer.TryParse(kv.Key);
                        if (candidate == null) continue;
                        if (best == null || candidate.CompareTo(best) > 0) { best = candidate; version = kv.Key; }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[kebinImports] Could not read the Unity registry for " + packageId + ": " + (e.InnerException ?? e).Message);
                }
                unityLatestCache[packageId] = version;
                return version;
            }
            private static string UnityRegistryVersionSuffix(string packageId)
            {
                string v = UnityRegistryLatest(packageId);
                return string.IsNullOrEmpty(v) ? "" : "@" + v;
            }

            // ---------------------------------------------------------------- Creator Companion
            // The listing that provides a tool, so it can be registered in the Creator Companion for future updates.
            public static string ListingUrlFor(Tool t)
            {
                if (!t.IsPackage) return null;
                foreach (VpmListing listing in Vpm.AllListings())
                {
                    if (listing.Versions(t.VpmPackage).Any()) return listing.Url;
                }
                return null;
            }
        }
    }
}
