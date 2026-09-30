using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports
    {
        // What kebinImports itself can do, exposed to the assistant as tools and described in its system prompt:
        // the tool catalog (install, update, remove, Creator Companion handoff), the Essentials lists, the utilities
        // and the Project Doctor.
        internal static class AIKebinTools
        {
            private static readonly string[] UtilityMenus = { "kebinImports/Fix Materials", "kebinImports/Remove Missing Scripts", "kebinImports/Fix Scripting Define Symbols", "kebinImports/PANIC HARD RESET ALL", "kebinImports/Customize Essentials", "kebinImports/Avatar Essentials", "kebinImports/World Essentials", "kebinImports/Ask kebinAI", "kebinImports/Settings", "kebinImports/Project Doctor" };

            // ---------------------------------------------------------------- registration
            public static void Register(Action<string, string, bool, JSONNode, Func<JSONNode, string>> register, Func<string[], JSONNode> schema)
            {
                register("list_tools", "Every asset kebinImports can install (shaders, avatar and world tools, SDKs) with whether it is installed, the installed and latest versions, and leftover copies from old installs. Call this before recommending, installing or updating anything.", false,
                    schema(new string[0]), _ => ListTools());
                register("install_tool", "Install or update one of kebinImports' tools by name (see list_tools). Uses the tool's official VPM listing with dependency resolution, the Unity registry, or its GitHub release, and removes leftovers of older install methods. Unity recompiles afterwards: after calling this, finish your reply and let the user continue when the import is done.", true,
                    schema(new[] { "name*", "string", "Tool name or key as list_tools shows it, e.g. Poiyomi Toon Shader.", "update", "boolean", "true to update an installed tool to the latest version." }), a => InstallTool(a["name"].Value, a["update"].AsBool));
                register("remove_tool", "Uninstall one of kebinImports' tools: removes its package (or Assets folder), its vpm-manifest entry and leftovers. Unity recompiles afterwards.", true,
                    schema(new[] { "name*", "string", "Tool name or key as list_tools shows it." }), a => RemoveTool(a["name"].Value));
                register("add_listing_to_creator_companion", "Register a tool's official VPM listing in the VRChat Creator Companion / ALCOM (opens a vcc:// link; the app asks the user to confirm) so it can manage updates for that tool from then on.", true,
                    schema(new[] { "name*", "string", "Tool name or key as list_tools shows it." }), a => AddListing(a["name"].Value));
                register("get_essentials", "The Avatar Essentials and World Essentials lists: the tools kebinImports installs in one go, and which are switched on.", false,
                    schema(new string[0]), _ => GetEssentials());
                register("set_essential", "Switch one entry of the Avatar or World Essentials list on or off.", true,
                    schema(new[] { "list*", "string", "avatar or world", "name*", "string", "Entry name as get_essentials shows it.", "enabled*", "boolean", "true to include it." }), a => SetEssential(a["list"].Value, a["name"].Value, a["enabled"].AsBool));
                register("run_essentials", "Install every switched-on entry of the Avatar or World Essentials list. Unity recompiles afterwards; finish your reply after calling this.", true,
                    schema(new[] { "list*", "string", "avatar or world" }), a => RunEssentials(a["list"].Value));
                register("fix_materials", "kebinImports' Fix Materials: change every material using one shader to another. Use from_shader Hidden/InternalErrorShader to repair pink/broken materials (missing shader).", true,
                    schema(new[] { "from_shader*", "string", "Shader name to replace, or Hidden/InternalErrorShader for broken materials.", "to_shader*", "string", "Shader name to switch to, e.g. .poiyomi/Poiyomi Toon or lilToon." }), a => FixMaterials(a["from_shader"].Value, a["to_shader"].Value));
                register("remove_missing_scripts", "kebinImports' Remove Missing Scripts: strips components whose script is missing from an object and all its children, including prefab sources.", true,
                    schema(new[] { "object", "string", "Scene path of the root object. Omit to use the current selection." }), a => RemoveMissingScripts(a.HasKey("object") ? a["object"].Value : null));
                register("fix_scripting_define_symbols", "kebinImports' Fix Scripting Define Symbols: clears the project's scripting defines so the VRChat SDK, Poiyomi and other tools re-add their own. Fixes many 'missing define' compile errors. Unity recompiles afterwards.", true,
                    schema(new string[0]), _ => { FSDSHandler(true); return "Cleared the scripting define symbols; Unity is recompiling."; });
                register("run_doctor", "kebinImports' Project Doctor: scans the project for duplicate or leftover installs, outdated tools, unmanaged packages, missing dependencies, pink materials, missing scripts, shader errors, Unity version problems, and a Load AssetBundle preview that is pink or missing components. Returns findings with ids you can pass to doctor_fix.", false,
                    schema(new string[0]), _ => ProjectDoctor.Run().ToJson().ToString(2));
                register("doctor_fix", "Apply the fix for one Project Doctor finding by its id (from run_doctor). Some fixes make Unity recompile; finish your reply after those.", true,
                    schema(new[] { "id*", "string", "Finding id from run_doctor." }), a => ProjectDoctor.Fix(a["id"].Value));
            }

            // ---------------------------------------------------------------- prompt text
            public static string DescribeForPrompt()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("kebinImports (the package you live in) installs and maintains the tools VRChat creators use, from the tools' official VPM listings with dependency resolution, and it cleans up leftovers of older installs. Prefer its tools over run_menu_item for these jobs:");
                sb.AppendLine("- list_tools / install_tool / remove_tool / add_listing_to_creator_companion: the importer. Installed state and versions come from list_tools; do not guess.");
                sb.AppendLine("- run_doctor / doctor_fix: the Project Doctor. Whenever the user describes a problem (errors, pink materials, something not working, an upload failing), run_doctor FIRST, before anything else. Look for findings related to their problem, explain them in plain words, and offer the matching fixes: ask the user which ones they want, and only call doctor_fix after they agree. If nothing in the report is related, say so briefly and carry on investigating.");
                sb.AppendLine("- get_essentials / set_essential / run_essentials: the Avatar Essentials and World Essentials batch lists (menu: kebinImports > Avatar Essentials, World Essentials, Customize Essentials).");
                sb.AppendLine("- fix_materials (menu: Fix Materials), remove_missing_scripts (menu: Remove Missing Scripts), fix_scripting_define_symbols (menu: Fix Scripting Define Symbols).");
                sb.AppendLine("- Never run 'kebinImports/PANIC HARD RESET ALL'; it wipes editor preferences and restarts Unity. Tell the user about it instead when everything else failed.");
                sb.AppendLine("Installed right now: " + InstalledSummary());
                List<string> updates = UpdateChecker.Available.Select(u => u.Tool.Name + " " + u.InstalledVersion + " -> " + u.LatestVersion).ToList();
                if (updates.Count > 0) sb.AppendLine("Updates available: " + string.Join(", ", updates) + ".");
                List<string> other = MenuPaths().Where(p => !ToolCatalog.Tools.Any(t => t.MenuPath == p) && !UtilityMenus.Contains(p) && !p.StartsWith("kebinImports/Social") && !p.StartsWith("kebinImports/Donate")).ToList();
                if (other.Count > 0) sb.AppendLine("Other kebinImports menu items (run_menu_item): " + string.Join(", ", other));
                return sb.ToString();
            }

            // ---------------------------------------------------------------- tools
            private static string ListTools()
            {
                JSONArray arr = new JSONArray();
                foreach (ToolCatalog.Status s in ToolCatalog.AllStatuses(true))
                {
                    JSONObject o = new JSONObject();
                    o["name"] = s.Tool.Name;
                    o["key"] = s.Tool.Key;
                    o["description"] = s.Tool.Description;
                    o["installed"] = s.Installed;
                    if (s.Installed) { o["location"] = s.Location; if (s.InstalledVersion != null) o["installedVersion"] = s.InstalledVersion; }
                    if (s.LatestVersion != null) o["latestVersion"] = s.LatestVersion;
                    if (s.UpdateAvailable) o["updateAvailable"] = true;
                    if (s.LegacyCopies.Count > 0) { JSONArray l = new JSONArray(); foreach (string c in s.LegacyCopies) l.Add(c); o["leftoverCopies"] = l; }
                    o["source"] = s.Tool.IsPackage ? "vpm" : (!string.IsNullOrEmpty(s.Tool.UnityPackage) ? "unity registry" : (!string.IsNullOrEmpty(s.Tool.GitHubRepo) ? "github release" : "custom"));
                    if (s.Tool.Paid) o["paid"] = true;
                    if (s.Tool.Legacy) o["legacy"] = true;
                    arr.Add(o);
                }
                return arr.ToString(2);
            }
            private static string InstalledSummary()
            {
                List<string> parts = new List<string>();
                foreach (ToolCatalog.Status s in ToolCatalog.AllStatuses(false))
                {
                    if (s.Installed) parts.Add(s.Tool.Name + (string.IsNullOrEmpty(s.InstalledVersion) ? "" : " " + s.InstalledVersion));
                }
                return parts.Count == 0 ? "none of them." : string.Join(", ", parts) + ".";
            }
            private static Tool FindTool(string name)
            {
                Tool t = ToolCatalog.Find(name);
                if (t == null) throw new ArgumentException("kebinImports does not know a tool called '" + name + "'. Known: " + string.Join(", ", ToolCatalog.Tools.Select(x => x.Name)));
                return t;
            }
            private static string InstallTool(string name, bool update)
            {
                Tool t = FindTool(name);
                if (t.Key == "yukio") return "Yukio's Fur Shader cannot be installed: its only download link is dead. Suggest Xiexe's Unity Shaders (fur) or Poiyomi's fur feature instead.";
                if (t.Key.StartsWith("vrcsdk") && isVRCCreatorCompanion) return "This is a Creator Companion project; the VRChat SDK is managed by the Creator Companion. Ask the user to update it there.";
                ToolCatalog.Install(t, update);
                ToolCatalog.Status s = ToolCatalog.GetStatus(t, false);
                return (s.Installed ? t.Name + " is now present at " + s.Location + (s.InstalledVersion != null ? " (" + s.InstalledVersion + ")" : "") + "." : "The install of " + t.Name + " did not leave it installed; check the console (it may have been cancelled).") + " Unity will recompile; tell the user and end your reply.";
            }
            private static string RemoveTool(string name)
            {
                Tool t = FindTool(name);
                ToolCatalog.Status before = ToolCatalog.GetStatus(t, false);
                if (!before.Installed && before.LegacyCopies.Count == 0) return t.Name + " is not installed.";
                ToolCatalog.Remove(t);
                List<string> orphans = Vpm.Orphans();
                return "Removed " + t.Name + "." + (orphans.Count > 0 ? " Packages nothing depends on any more: " + string.Join(", ", orphans) + " (ask the user before removing them)." : "") + " Unity will recompile; tell the user and end your reply.";
            }
            private static string AddListing(string name)
            {
                Tool t = FindTool(name);
                string url = ToolCatalog.ListingUrlFor(t);
                if (url == null) return t.Name + " is not distributed through a VPM listing.";
                if (!Vpm.RegisterListingInCreatorCompanion(url)) return "The VRChat Creator Companion (or ALCOM) is not installed on this machine, so the listing could not be registered. The listing URL is " + url + ".";
                return "Asked the Creator Companion to add " + url + " (" + t.Name + "'s listing). The user has to confirm in the Creator Companion window.";
            }
            private static string GetEssentials()
            {
                JSONObject o = new JSONObject();
                JSONArray avatar = new JSONArray(), world = new JSONArray();
                for (int i = 0; i < AENames.Length; i++) { JSONObject e = new JSONObject(); e["name"] = AENames[i]; e["enabled"] = avatarEssentials[i]; avatar.Add(e); }
                for (int i = 0; i < WENames.Length; i++) { JSONObject e = new JSONObject(); e["name"] = WENames[i]; e["enabled"] = worldEssentials[i]; world.Add(e); }
                o["avatar"] = avatar;
                o["world"] = world;
                return o.ToString(2);
            }
            private static string SetEssential(string list, string name, bool enabled)
            {
                bool avatar = ListIsAvatar(list);
                string[] names = avatar ? AENames : WENames;
                bool[] values = avatar ? avatarEssentials : worldEssentials;
                int idx = Array.FindIndex(names, n => n.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (idx < 0) idx = Array.FindIndex(names, n => n.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
                if (idx < 0) throw new ArgumentException("No entry '" + name + "' in the " + (avatar ? "avatar" : "world") + " essentials. Entries: " + string.Join(", ", names));
                values[idx] = enabled;
                SaveEssentials();
                return names[idx] + " is now " + (enabled ? "enabled" : "disabled") + " in the " + (avatar ? "Avatar" : "World") + " Essentials.";
            }
            private static string RunEssentials(string list)
            {
                bool avatar = ListIsAvatar(list);
                string[] names = avatar ? AENames : WENames;
                bool[] values = avatar ? avatarEssentials : worldEssentials;
                List<string> enabled = names.Where((n, i) => values[i]).ToList();
                if (enabled.Count == 0) return "Nothing is enabled in the " + (avatar ? "Avatar" : "World") + " Essentials; enable entries with set_essential first.";
                if (avatar) importAvatarEssentials(); else importWorldEssentials();
                return "Ran the " + (avatar ? "Avatar" : "World") + " Essentials: " + string.Join(", ", enabled) + ". Unity will recompile; tell the user and end your reply.";
            }
            private static bool ListIsAvatar(string list)
            {
                if (string.IsNullOrEmpty(list)) throw new ArgumentException("list must be 'avatar' or 'world'.");
                if (list.StartsWith("a", StringComparison.OrdinalIgnoreCase)) return true;
                if (list.StartsWith("w", StringComparison.OrdinalIgnoreCase)) return false;
                throw new ArgumentException("list must be 'avatar' or 'world'.");
            }
            private static string FixMaterials(string from, string to)
            {
                if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) throw new ArgumentException("from_shader and to_shader are required.");
                if (Shader.Find(to) == null) throw new ArgumentException("No shader named '" + to + "'. Use list_shaders.");
                int changed = fixMaterials(from, to);
                return "Fix Materials changed " + changed + (changed == 1 ? " material" : " materials") + " from '" + from + "' to '" + to + "'.";
            }
            private static string RemoveMissingScripts(string objectPath)
            {
                IEnumerable<GameObject> roots;
                if (string.IsNullOrEmpty(objectPath))
                {
                    if (Selection.gameObjects.Length == 0) throw new ArgumentException("Nothing is selected; pass an object path.");
                    roots = Selection.gameObjects;
                }
                else roots = new[] { AITools.FindSceneObjectPublic(objectPath) };
                int comps, gos;
                RemoveMissingScriptsTool.RemoveFrom(roots, out comps, out gos);
                return "Removed " + comps + (comps == 1 ? " missing script component" : " missing script components") + " from " + gos + (gos == 1 ? " object." : " objects.");
            }

            // ---------------------------------------------------------------- menu reflection
            private static IEnumerable<MethodInfo> MenuMethods()
            {
                foreach (Type type in typeof(kebinImports).Assembly.GetTypes())
                {
                    foreach (MethodInfo m in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (m.GetCustomAttribute<MenuItem>() != null) yield return m;
                    }
                }
            }
            private static List<string> MenuPaths()
            {
                return MenuMethods().Select(m => m.GetCustomAttribute<MenuItem>()).Where(a => !a.validate && a.menuItem.StartsWith("kebinImports/")).Select(a => a.menuItem).Distinct().OrderBy(p => p).ToList();
            }
        }
    }
}
