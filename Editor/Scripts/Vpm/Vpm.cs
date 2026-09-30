using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using Debug = UnityEngine.Debug;

namespace kebinImports
{
    public partial class kebinImports
    {
        // A small VPM client: reads the same repository listings the VRChat Creator Companion reads, resolves
        // versions and dependencies, verifies downloads, installs packages under Packages/ and keeps vpm-manifest.json
        // in sync so the Creator Companion and ALCOM recognise everything it installs.
        internal static class Vpm
        {
            public const string OfficialListing = "https://packages.vrchat.com/official?download";
            public const string CuratedListing = "https://packages.vrchat.com/curated?download";

            // Listings kebinImports always consults, in addition to whatever the user added to the Creator Companion.
            public static readonly string[] DefaultListings =
            {
                OfficialListing,
                CuratedListing,
                "https://poiyomi.github.io/vpm/index.json",
                "https://lilxyzw.github.io/vpm-repos/vpm.json",
                "https://vpm.nadena.dev/vpm.json",
                "https://vcc.vrcfury.com",
                "https://vpm.chocopoi.com/index.json",
                "https://vpm.thry.dev/index.json",
                "https://hai-vr.github.io/vpm-listing/index.json",
                "https://rurre.github.io/vpm/index.json",
                "https://kurotu.github.io/vpm-repos/vpm.json",
            };

            private static readonly TimeSpan ListingMaxAge = TimeSpan.FromHours(6);
            private static readonly Dictionary<string, VpmListing> memoryCache = new Dictionary<string, VpmListing>();
            private static string ListingCachePath => Path.Combine(DownloadPath, "listings");
            private static string CreatorCompanionPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VRChatCreatorCompanion");

            // ---------------------------------------------------------------- listings
            // Listings that could not be fetched this session are not retried until a forced refresh, so an offline
            // editor or a blocked host costs one short timeout, not one per tool.
            private static readonly HashSet<string> failedListings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public static void MarkFailed(string url) { failedListings.Add(url); }

            // A package id from a listing or a zip, validated before it becomes a folder under Packages/.
            public static string PackageFolder(string packageId)
            {
                if (string.IsNullOrEmpty(packageId) || !Regex.IsMatch(packageId, "^[A-Za-z0-9][A-Za-z0-9._-]*$") || packageId.Contains(".."))
                    throw new InvalidDataException("Refusing to use '" + packageId + "' as a package folder name.");
                string root = Path.GetFullPath(PackagesPath).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
                string full = Path.GetFullPath(Path.Combine(PackagesPath, packageId));
                if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Package folder for '" + packageId + "' would be outside Packages/.");
                return full;
            }

            public static VpmListing GetListing(string url, bool forceRefresh = false)
            {
                VpmListing cached;
                if (!forceRefresh && memoryCache.TryGetValue(url, out cached)) return cached;
                if (!forceRefresh && failedListings.Contains(url) && ReadCachedListing(url) == null) throw new Exception("Listing " + url + " is unreachable (skipped for this session).");
                Directory.CreateDirectory(ListingCachePath);
                string file = Path.Combine(ListingCachePath, Sha256Hex(Encoding.UTF8.GetBytes(url)).Substring(0, 16) + ".json");
                string json = null;
                bool fresh = File.Exists(file) && DateTime.UtcNow - File.GetLastWriteTimeUtc(file) < ListingMaxAge;
                if (!forceRefresh && fresh) json = File.ReadAllText(file);
                if (json == null)
                {
                    try
                    {
                        if (failedListings.Contains(url) && !forceRefresh) throw new Exception("skipped for this session");
                        HttpClient listingClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                        json = Task.Run(() => HttpClient.DownloadString(listingClient, url)).Result;
                        File.WriteAllText(file, json);
                        failedListings.Remove(url);
                    }
                    catch (Exception e)
                    {
                        if (failedListings.Add(url)) Debug.LogWarning("[kebinImports] Could not download the listing " + url + ": " + (e.InnerException ?? e).Message);
                        if (File.Exists(file)) json = File.ReadAllText(file);
                        else json = ReadCreatorCompanionCache(url);
                        if (json == null) throw new Exception("Listing " + url + " is unreachable and not cached.");
                    }
                }
                VpmListing listing = VpmListing.Parse(json, url);
                memoryCache[url] = listing;
                return listing;
            }
            private static string ReadCachedListing(string url)
            {
                string file = Path.Combine(ListingCachePath, Sha256Hex(Encoding.UTF8.GetBytes(url)).Substring(0, 16) + ".json");
                return File.Exists(file) ? File.ReadAllText(file) : null;
            }
            // Seeds the caches with listing JSON fetched elsewhere (the background update check).
            public static void Prime(string url, string json)
            {
                try
                {
                    memoryCache[url] = VpmListing.Parse(json, url);
                    Directory.CreateDirectory(ListingCachePath);
                    File.WriteAllText(Path.Combine(ListingCachePath, Sha256Hex(Encoding.UTF8.GetBytes(url)).Substring(0, 16) + ".json"), json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[kebinImports] Ignoring listing " + url + ": " + e.Message);
                }
            }
            // The Creator Companion keeps every listing it has seen under its Repos folder; use them offline.
            private static string ReadCreatorCompanionCache(string url)
            {
                string repos = Path.Combine(CreatorCompanionPath, "Repos");
                if (!Directory.Exists(repos)) return null;
                foreach (string file in Directory.GetFiles(repos, "*.json"))
                {
                    try
                    {
                        string text = File.ReadAllText(file);
                        JSONNode node = JSON.Parse(StripBom(text));
                        JSONNode repo = node != null && node.HasKey("repo") ? node["repo"] : node;
                        if (repo != null && repo["url"].Value.TrimEnd('/') == url.TrimEnd('/')) return repo.ToString();
                    }
                    catch (Exception) { }
                }
                return null;
            }
            public static List<string> CreatorCompanionUserListings()
            {
                List<string> urls = new List<string>();
                string settings = Path.Combine(CreatorCompanionPath, "settings.json");
                if (!File.Exists(settings)) return urls;
                try
                {
                    JSONNode node = JSON.Parse(StripBom(File.ReadAllText(settings)));
                    foreach (JSONNode repo in node["userRepos"].Children)
                    {
                        string url = repo["url"].Value;
                        if (!string.IsNullOrEmpty(url)) urls.Add(url);
                    }
                }
                catch (Exception) { }
                return urls;
            }
            public static IEnumerable<string> AllListingUrls()
            {
                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string url in DefaultListings.Concat(CreatorCompanionUserListings()))
                {
                    if (seen.Add(url.TrimEnd('/'))) yield return url;
                }
            }
            public static IEnumerable<VpmListing> AllListings(bool forceRefresh = false)
            {
                foreach (string url in AllListingUrls())
                {
                    VpmListing listing = null;
                    try { listing = GetListing(url, forceRefresh); }
                    catch (Exception e) { Debug.LogWarning("[kebinImports] Skipping listing " + url + ": " + e.Message); }
                    if (listing != null) yield return listing;
                }
            }

            // ---------------------------------------------------------------- project state
            public static Dictionary<string, string> InstalledPackages()
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                if (!Directory.Exists(PackagesPath)) return result;
                foreach (string dir in Directory.GetDirectories(PackagesPath))
                {
                    string pj = Path.Combine(dir, "package.json");
                    if (!File.Exists(pj)) continue;
                    JSONNode p = JSON.Parse(StripBom(File.ReadAllText(pj)));
                    if (p == null || !p.HasKey("name")) continue;
                    result[p["name"].Value] = p.HasKey("version") ? p["version"].Value : "";
                }
                return result;
            }
            public static Dictionary<string, JSONNode> InstalledManifests()
            {
                Dictionary<string, JSONNode> result = new Dictionary<string, JSONNode>();
                if (!Directory.Exists(PackagesPath)) return result;
                foreach (string dir in Directory.GetDirectories(PackagesPath))
                {
                    string pj = Path.Combine(dir, "package.json");
                    if (!File.Exists(pj)) continue;
                    JSONNode p = JSON.Parse(StripBom(File.ReadAllText(pj)));
                    if (p != null && p.HasKey("name")) result[p["name"].Value] = p;
                }
                return result;
            }
            public static Version EditorVersion()
            {
                string[] parts = Application.unityVersion.Split('.');
                int major, minor;
                int.TryParse(parts[0], out major);
                int.TryParse(parts.Length > 1 ? parts[1] : "0", out minor);
                return new Version(major, minor);
            }

            // ---------------------------------------------------------------- resolution
            public static VpmPackageVersion FindLatest(string packageId, string range = null, bool allowPrerelease = false)
            {
                VpmPackageVersion best = null;
                foreach (VpmListing listing in AllListings())
                {
                    foreach (VpmPackageVersion v in listing.Versions(packageId))
                    {
                        if (!v.SupportsEditor(EditorVersion())) continue;
                        if (!allowPrerelease && v.SemVer.IsPrerelease && !RangeRequiresPrerelease(range)) continue;
                        if (!SemVerRange.Satisfies(v.SemVer, range)) continue;
                        if (best == null || v.SemVer.CompareTo(best.SemVer) > 0) best = v;
                    }
                }
                return best;
            }
            // Pre-releases are only wanted when a lower bound itself names one (">=1.2.0-beta.1"); an upper bound such as
            // "<2.0.0-a" only excludes pre-releases of 2.0.0 and must not pull in betas.
            private static bool RangeRequiresPrerelease(string range)
            {
                if (string.IsNullOrEmpty(range)) return false;
                foreach (string part in range.Split(new[] { ' ', ',', '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (part.StartsWith("<")) continue;
                    if (part.Contains("-")) return true;
                }
                return false;
            }

            public static bool IsKnownPackage(string packageId) => AllListings().Any(l => l.Versions(packageId).Any());

            // Everything that has to be installed for packageId, dependencies first. Already satisfied packages are skipped.
            public static List<VpmPackageVersion> Plan(string packageId, string range = null, bool allowPrerelease = false, bool update = false)
            {
                List<VpmPackageVersion> plan = new List<VpmPackageVersion>();
                Dictionary<string, string> installed = InstalledPackages();
                HashSet<string> visiting = new HashSet<string>();
                PlanInto(packageId, range, allowPrerelease, update, installed, plan, visiting, true);
                return plan;
            }
            private static void PlanInto(string packageId, string range, bool allowPrerelease, bool update, Dictionary<string, string> installed, List<VpmPackageVersion> plan, HashSet<string> visiting, bool isRoot)
            {
                if (plan.Any(p => p.Id == packageId) || !visiting.Add(packageId)) return;
                string installedVersion;
                bool have = installed.TryGetValue(packageId, out installedVersion);
                if (have && !(isRoot && update))
                {
                    SemVer current = SemVer.TryParse(installedVersion);
                    if (current != null && SemVerRange.Satisfies(current, range)) return; // already fine
                }
                VpmPackageVersion pick = FindLatest(packageId, range, allowPrerelease);
                if (pick == null)
                {
                    if (have) return; // installed but not in any listing (user package); leave it alone
                    throw new Exception("PLAIN:There's no version of " + packageId + " that works with Unity " + EditorVersion() + ".");
                }
                if (have && SemVer.TryParse(installedVersion) != null && pick.SemVer.CompareTo(SemVer.TryParse(installedVersion)) <= 0 && !(isRoot && update)) return;
                foreach (KeyValuePair<string, string> dep in pick.Dependencies)
                {
                    PlanInto(dep.Key, dep.Value, allowPrerelease, false, installed, plan, visiting, false);
                }
                plan.Add(pick);
            }

            // ---------------------------------------------------------------- install / remove
            public static List<VpmPackageVersion> Install(string packageId, string range = null, bool allowPrerelease = false, bool update = false)
            {
                List<VpmPackageVersion> plan = Plan(packageId, range, allowPrerelease, update);
                if (plan.Count == 0)
                {
                    Debug.Log("[kebinImports] " + packageId + " is already installed and up to date.");
                    return plan;
                }
                if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
                Selection.activeGameObject = null;
                EditorApplication.LockReloadAssemblies();
                List<MaterialRecord> materials = new List<MaterialRecord>();
                try
                {
                    Directory.CreateDirectory(DownloadPath);
                    int i = 0;
                    foreach (VpmPackageVersion v in plan)
                    {
                        EditorUtility.DisplayProgressBar("kebinImports", "Downloading " + v.Id + " " + v.Version + "…", (float)i / plan.Count);
                        string zip = Path.Combine(DownloadPath, v.Id + "-" + v.Version + ".zip");
                        client = new HttpClient();
                        Task.Run(() => HttpClient.DownloadFile(client, v.Url, zip)).Wait();
                        if (!string.IsNullOrEmpty(v.ZipSha256))
                        {
                            string actual = Sha256Hex(File.ReadAllBytes(zip));
                            if (!string.Equals(actual, v.ZipSha256, StringComparison.OrdinalIgnoreCase))
                            {
                                File.Delete(zip);
                                throw new Exception("PLAIN:The download of " + (string.IsNullOrEmpty(v.DisplayName) ? v.Id : v.DisplayName) + " arrived damaged, so it wasn't installed. Please try again.");
                            }
                        }
                        EditorUtility.DisplayProgressBar("kebinImports", "Installing " + v.Id + " " + v.Version + "…", (float)(i + 0.5f) / plan.Count);
                        string target = PackageFolder(v.Id);
                        materials.AddRange(SnapshotMaterials(v.Id));
                        DeleteDirectory(target);
                        try
                        {
                            ZipFile.ExtractToDirectory(zip, target);
                        }
                        catch (Exception)
                        {
                            DeleteDirectory(target);
                            throw;
                        }
                        finally
                        {
                            File.Delete(zip);
                        }
                        // Only what the user asked for is a top-level dependency; everything installed is locked.
                        WriteManifestEntry(v, v.Id == packageId);
                        Debug.Log("[kebinImports] Installed " + v.Id + " " + v.Version + " from " + v.Listing.Name);
                        i++;
                    }
                    // The VRChat SDK ships test scripts that need Unity's Test Framework. Creator Companion projects and
                    // Unity Hub's templates have it, but a project without it would stop compiling.
                    if (plan.Any(p => p.Id == "com.vrchat.base")) EnsureUnityPackage("com.unity.test-framework", "1.1.33");
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                    AssetDatabase.SaveAssets();
                    EditorApplication.UnlockReloadAssemblies();
                    AssetDatabase.Refresh();
                    RestoreMaterials(materials);
                    // A package folder that just appeared under Packages/ is only registered once the Package Manager
                    // re-resolves; until then its shaders and scripts do not exist as far as Unity is concerned.
                    UnityEditor.PackageManager.Client.Resolve();
                    UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                }
                return plan;
            }

            public static void Remove(string packageId)
            {
                string target = PackageFolder(packageId);
                if (!Directory.Exists(target)) throw new Exception(packageId + " is not installed under Packages/.");
                EditorApplication.LockReloadAssemblies();
                try
                {
                    DeleteDirectory(target);
                    RemoveManifestEntry(packageId);
                    RemoveJsonDependencies(Path.Combine(PackagesPath, "manifest.json"), new[] { packageId });
                    RemoveJsonDependencies(Path.Combine(PackagesPath, "packages-lock.json"), new[] { packageId });
                    Debug.Log("[kebinImports] Removed " + packageId);
                }
                finally
                {
                    EditorApplication.UnlockReloadAssemblies();
                    AssetDatabase.Refresh();
                    UnityEditor.PackageManager.Client.Resolve();
                    UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                }
            }

            // Packages nothing else depends on any more (candidates for removal after removing something).
            public static List<string> Orphans()
            {
                Dictionary<string, JSONNode> manifests = InstalledManifests();
                JSONNode vpm = ReadVpmManifest();
                HashSet<string> wanted = new HashSet<string>();
                if (vpm != null) foreach (KeyValuePair<string, JSONNode> kv in vpm["dependencies"].AsObject) wanted.Add(kv.Key);
                HashSet<string> needed = new HashSet<string>();
                foreach (JSONNode m in manifests.Values)
                {
                    if (!m.HasKey("vpmDependencies")) continue;
                    foreach (KeyValuePair<string, JSONNode> dep in m["vpmDependencies"].AsObject) needed.Add(dep.Key);
                }
                return manifests.Keys.Where(id => !wanted.Contains(id) && !needed.Contains(id) && id != "dev.kebin.kebinimports").ToList();
            }

            // Adds a Unity registry package to Packages/manifest.json unless the project already has it.
            private static void EnsureUnityPackage(string id, string version)
            {
                string file = Path.Combine(PackagesPath, "manifest.json");
                if (!File.Exists(file) || Directory.Exists(Path.Combine(PackagesPath, id))) return;
                JSONNode node = JSON.Parse(File.ReadAllText(file));
                if (node == null) return;
                if (!node.HasKey("dependencies")) node["dependencies"] = new JSONObject();
                if (node["dependencies"].AsObject.HasKey(id)) return;
                node["dependencies"][id] = version;
                File.WriteAllText(file, node.ToString(2));
                Debug.Log("[kebinImports] Added " + id + " " + version + ", which the VRChat SDK needs.");
            }

            // ---------------------------------------------------------------- vpm-manifest.json
            private static string VpmManifestPath => Path.Combine(PackagesPath, "vpm-manifest.json");
            private static JSONNode ReadVpmManifest()
            {
                if (!File.Exists(VpmManifestPath)) return null;
                JSONNode node = JSON.Parse(StripBom(File.ReadAllText(VpmManifestPath)));
                if (node == null) return null;
                if (!node.HasKey("dependencies")) node["dependencies"] = new JSONObject();
                if (!node.HasKey("locked")) node["locked"] = new JSONObject();
                return node;
            }
            private static void WriteManifestEntry(VpmPackageVersion v, bool isRoot)
            {
                JSONNode manifest = ReadVpmManifest();
                if (manifest == null)
                {
                    // A plain Unity project: create the manifest so the Creator Companion can adopt the project later.
                    manifest = new JSONObject();
                    manifest["dependencies"] = new JSONObject();
                    manifest["locked"] = new JSONObject();
                }
                if (isRoot || manifest["dependencies"].HasKey(v.Id))
                {
                    JSONObject dep = new JSONObject();
                    dep["version"] = v.Version;
                    manifest["dependencies"][v.Id] = dep;
                }
                JSONObject locked = new JSONObject();
                locked["version"] = v.Version;
                JSONObject deps = new JSONObject();
                foreach (KeyValuePair<string, string> d in v.Dependencies) deps[d.Key] = d.Value;
                locked["dependencies"] = deps;
                manifest["locked"][v.Id] = locked;
                File.WriteAllText(VpmManifestPath, manifest.ToString(2));
            }
            private static void RemoveManifestEntry(string packageId)
            {
                JSONNode manifest = ReadVpmManifest();
                if (manifest == null) return;
                bool changed = false;
                if (manifest["dependencies"].HasKey(packageId)) { manifest["dependencies"].Remove(packageId); changed = true; }
                if (manifest["locked"].HasKey(packageId)) { manifest["locked"].Remove(packageId); changed = true; }
                if (changed) File.WriteAllText(VpmManifestPath, manifest.ToString(2));
            }
            // Packages present under Packages/ that vpm-manifest.json does not list (the Creator Companion calls these unmanaged).
            public static List<string> UnmanagedPackages()
            {
                JSONNode manifest = ReadVpmManifest();
                if (manifest == null) return new List<string>();
                return InstalledPackages().Keys.Where(id => !manifest["dependencies"].HasKey(id) && !manifest["locked"].HasKey(id) && id != "dev.kebin.kebinimports" && IsKnownPackage(id)).ToList();
            }
            public static void AdoptIntoManifest(string packageId)
            {
                Dictionary<string, JSONNode> manifests = InstalledManifests();
                JSONNode m;
                if (!manifests.TryGetValue(packageId, out m)) throw new Exception(packageId + " is not installed.");
                VpmPackageVersion v = new VpmPackageVersion(m, new VpmListing { Name = "installed" });
                WriteManifestEntry(v, true);
            }

            // ---------------------------------------------------------------- material migration
            private class MaterialRecord { public string Path; public string ShaderName; }
            // Materials whose shader lives inside the package about to be replaced, so they can be re-pointed by name afterwards.
            private static List<MaterialRecord> SnapshotMaterials(string packageId)
            {
                List<MaterialRecord> records = new List<MaterialRecord>();
                string prefix = "Packages/" + packageId + "/";
                if (!Directory.Exists(Path.Combine(PackagesPath, packageId))) return records;
                foreach (string guid in AssetDatabase.FindAssets("t:Material"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.StartsWith("Packages/")) continue;
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (m == null || m.shader == null) continue;
                    string shaderPath = AssetDatabase.GetAssetPath(m.shader);
                    if (shaderPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) records.Add(new MaterialRecord { Path = path, ShaderName = m.shader.name });
                }
                return records;
            }
            private static void RestoreMaterials(List<MaterialRecord> records)
            {
                int fixedCount = 0;
                foreach (MaterialRecord r in records)
                {
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(r.Path);
                    if (m == null) continue;
                    if (m.shader != null && m.shader.name == r.ShaderName) continue;
                    Shader s = Shader.Find(r.ShaderName);
                    if (s == null) continue;
                    m.shader = s;
                    EditorUtility.SetDirty(m);
                    fixedCount++;
                }
                if (fixedCount > 0)
                {
                    AssetDatabase.SaveAssets();
                    Debug.Log("[kebinImports] Re-pointed " + fixedCount + " material(s) to their shaders after the package update.");
                }
            }

            // ---------------------------------------------------------------- Creator Companion handoff
            public static bool IsCreatorCompanionInstalled()
            {
#if UNITY_EDITOR_WIN
                try
                {
                    using (Process reg = Process.Start(new ProcessStartInfo("reg.exe", "query HKCR\\vcc\\shell\\open\\command") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true }))
                    {
                        string output = reg.StandardOutput.ReadToEnd();
                        reg.WaitForExit();
                        return reg.ExitCode == 0 && output.IndexOf("REG_SZ", StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                }
                catch (Exception) { return false; }
#else
                return Directory.Exists(CreatorCompanionPath);
#endif
            }
            // Asks the Creator Companion (or ALCOM) to add a listing; it shows its own confirmation.
            public static bool RegisterListingInCreatorCompanion(string url)
            {
                if (!IsCreatorCompanionInstalled()) return false;
                Application.OpenURL("vcc://vpm/addRepo?url=" + Uri.EscapeDataString(url));
                return true;
            }

            // ---------------------------------------------------------------- helpers
            public static string StripBom(string s) => s != null && s.Length > 0 && s[0] == '﻿' ? s.Substring(1) : s;
            private static string Sha256Hex(byte[] bytes)
            {
                using (SHA256 sha = SHA256.Create())
                {
                    return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        internal class VpmListing
        {
            public string Name = "";
            public string Id = "";
            public string Url = "";
            public Dictionary<string, List<VpmPackageVersion>> Packages = new Dictionary<string, List<VpmPackageVersion>>();

            public static VpmListing Parse(string json, string url)
            {
                JSONNode node = JSON.Parse(Vpm.StripBom(json));
                if (node == null || !node.HasKey("packages")) throw new Exception("Not a VPM listing: " + url);
                VpmListing listing = new VpmListing { Name = node["name"].Value, Id = node["id"].Value, Url = string.IsNullOrEmpty(node["url"].Value) ? url : node["url"].Value };
                foreach (KeyValuePair<string, JSONNode> pkg in node["packages"].AsObject)
                {
                    List<VpmPackageVersion> versions = new List<VpmPackageVersion>();
                    foreach (KeyValuePair<string, JSONNode> ver in pkg.Value["versions"].AsObject)
                    {
                        JSONNode manifest = ver.Value;
                        if (!manifest.HasKey("name")) manifest["name"] = pkg.Key;
                        if (!manifest.HasKey("version")) manifest["version"] = ver.Key;
                        VpmPackageVersion v = new VpmPackageVersion(manifest, listing);
                        if (v.SemVer != null && !string.IsNullOrEmpty(v.Url)) versions.Add(v);
                    }
                    listing.Packages[pkg.Key] = versions;
                }
                return listing;
            }
            public IEnumerable<VpmPackageVersion> Versions(string packageId)
            {
                List<VpmPackageVersion> list;
                return Packages.TryGetValue(packageId, out list) ? list : Enumerable.Empty<VpmPackageVersion>();
            }
        }

        internal class VpmPackageVersion
        {
            public string Id;
            public string Version;
            public string DisplayName;
            public string Url;
            public string ZipSha256;
            public string Unity;
            public SemVer SemVer;
            public Dictionary<string, string> Dependencies = new Dictionary<string, string>();
            public JSONNode Manifest;
            public VpmListing Listing;

            public VpmPackageVersion(JSONNode manifest, VpmListing listing)
            {
                Manifest = manifest;
                Listing = listing;
                Id = manifest["name"].Value;
                Version = manifest["version"].Value;
                DisplayName = manifest.HasKey("displayName") ? manifest["displayName"].Value : Id;
                Url = manifest["url"].Value;
                ZipSha256 = manifest.HasKey("zipSHA256") ? manifest["zipSHA256"].Value : null;
                Unity = manifest.HasKey("unity") ? manifest["unity"].Value : null;
                SemVer = SemVer.TryParse(Version);
                if (manifest.HasKey("vpmDependencies") && manifest["vpmDependencies"].IsObject)
                {
                    foreach (KeyValuePair<string, JSONNode> dep in manifest["vpmDependencies"].AsObject) Dependencies[dep.Key] = dep.Value.Value;
                }
            }
            public bool SupportsEditor(Version editor)
            {
                if (string.IsNullOrEmpty(Unity)) return true;
                string[] parts = Unity.Split('.');
                int major, minor = 0;
                if (!int.TryParse(parts[0], out major)) return true;
                if (parts.Length > 1) int.TryParse(parts[1], out minor);
                return new Version(major, minor) <= editor;
            }
        }

        // Semantic versions as VPM uses them: major.minor.patch with an optional pre-release tag.
        internal class SemVer : IComparable<SemVer>
        {
            public int Major, Minor, Patch;
            public string Prerelease = "";
            public bool IsPrerelease => Prerelease.Length > 0;
            private static readonly Regex Pattern = new Regex(@"^v?(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:-([0-9A-Za-z.\-]+))?(?:\+[0-9A-Za-z.\-]+)?$");

            public static SemVer TryParse(string text)
            {
                if (string.IsNullOrEmpty(text)) return null;
                Match m = Pattern.Match(text.Trim());
                if (!m.Success) return null;
                return new SemVer
                {
                    Major = int.Parse(m.Groups[1].Value),
                    Minor = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0,
                    Patch = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0,
                    Prerelease = m.Groups[4].Success ? m.Groups[4].Value : ""
                };
            }
            public int CompareTo(SemVer other)
            {
                if (Major != other.Major) return Major.CompareTo(other.Major);
                if (Minor != other.Minor) return Minor.CompareTo(other.Minor);
                if (Patch != other.Patch) return Patch.CompareTo(other.Patch);
                if (IsPrerelease != other.IsPrerelease) return IsPrerelease ? -1 : 1; // 1.0.0-rc < 1.0.0
                if (!IsPrerelease) return 0;
                string[] a = Prerelease.Split('.'), b = other.Prerelease.Split('.');
                for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
                {
                    if (i >= a.Length) return -1;
                    if (i >= b.Length) return 1;
                    int na, nb;
                    bool ia = int.TryParse(a[i], out na), ib = int.TryParse(b[i], out nb);
                    int c = ia && ib ? na.CompareTo(nb) : (ia != ib ? (ia ? -1 : 1) : string.CompareOrdinal(a[i], b[i]));
                    if (c != 0) return c;
                }
                return 0;
            }
            public override string ToString() => Major + "." + Minor + "." + Patch + (IsPrerelease ? "-" + Prerelease : "");
        }

        // The subset of npm-style version ranges that VPM manifests use: exact, ^, ~, >= > <= <, x-wildcards, * and
        // space-separated combinations ("&&"), plus "||" alternatives.
        internal static class SemVerRange
        {
            public static bool Satisfies(SemVer v, string range)
            {
                if (v == null) return false;
                if (string.IsNullOrWhiteSpace(range) || range.Trim() == "*" || range.Trim().ToLowerInvariant() == "x") return true;
                foreach (string alternative in range.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    bool all = true;
                    foreach (string part in alternative.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!SatisfiesOne(v, part)) { all = false; break; }
                    }
                    if (all) return true;
                }
                return false;
            }
            private static bool SatisfiesOne(SemVer v, string part)
            {
                string op = "";
                string rest = part;
                foreach (string candidate in new[] { ">=", "<=", ">", "<", "=", "^", "~" })
                {
                    if (rest.StartsWith(candidate)) { op = candidate; rest = rest.Substring(candidate.Length); break; }
                }
                rest = rest.Trim().TrimStart('v');
                if (rest == "*" || rest == "x" || rest == "") return true;
                // x-wildcards: 1.x / 1.2.x behave like ^1 / ~1.2
                string[] pieces = rest.Split('.');
                bool wildcard = pieces.Any(p => p == "x" || p == "X" || p == "*");
                if (wildcard && (op == ">=" || op == ">" || op == "<=" || op == "<" || op == "="))
                {
                    // ">=1.x" and friends: compare against the wildcard's lowest version.
                    rest = string.Join(".", pieces.Select(p => p == "x" || p == "X" || p == "*" ? "0" : p));
                    pieces = rest.Split('.');
                    wildcard = false;
                }
                if (wildcard)
                {
                    int maj = int.Parse(pieces[0]);
                    if (pieces.Length < 3 || pieces[1] == "x" || pieces[1] == "*") return v.Major == maj && !v.IsPrerelease;
                    int min = int.Parse(pieces[1]);
                    return v.Major == maj && v.Minor == min;
                }
                SemVer target = SemVer.TryParse(rest);
                if (target == null) return true; // unknown syntax: do not block installs
                int c = v.CompareTo(target);
                switch (op)
                {
                    case ">=": return c >= 0;
                    case ">": return c > 0;
                    case "<=": return c <= 0;
                    case "<": return c < 0;
                    case "^":
                    {
                        if (c < 0) return false;
                        if (target.Major > 0) return v.Major == target.Major;
                        if (target.Minor > 0) return v.Major == 0 && v.Minor == target.Minor;
                        return v.Major == 0 && v.Minor == 0 && v.Patch == target.Patch;
                    }
                    case "~":
                    {
                        if (c < 0) return false;
                        return v.Major == target.Major && (pieces.Length < 2 || v.Minor == target.Minor);
                    }
                    default:
                    {
                        // A bare version: exact unless fewer than three parts were given (1.2 means 1.2.x).
                        if (pieces.Length >= 3) return c == 0;
                        if (pieces.Length == 2) return v.Major == target.Major && v.Minor == target.Minor;
                        return v.Major == target.Major;
                    }
                }
            }
        }
    }
}
