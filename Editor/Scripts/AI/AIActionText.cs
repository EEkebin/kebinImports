using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports
    {
        // Turns kebinAI's tool calls into plain sentences for the person using it: the confirmation dialog, the chat
        // log and error lines. The model itself still gets the full technical detail; people never see JSON.
        internal static class AIActionText
        {
            // "Install lilToon." — used in the confirmation dialog.
            public static string Describe(AIToolCall call) => Sentence(call, false);
            // "Installed lilToon." — used in the chat log after the action ran.
            public static string Done(AIToolCall call) => Sentence(call, true);
            // "Couldn't install lilToon." — used when the action failed.
            public static string Failed(AIToolCall call)
            {
                string s = Sentence(call, false).TrimEnd('.');
                return "Couldn't " + char.ToLowerInvariant(s[0]) + s.Substring(1) + ". kebinAI will take that into account.";
            }

            private static string Sentence(AIToolCall call, bool past)
            {
                JSONNode a = call.Arguments ?? new JSONObject();
                Func<string, string, string> v = (present, done) => past ? done : present;
                switch (call.Name)
                {
                    // ---- looking around (never confirmed, shown in the log)
                    case "get_project_info": return v("Look at the project", "Looked at the project") + ".";
                    case "get_selection": return v("Check what you have selected", "Checked what you have selected") + ".";
                    case "get_hierarchy": return v("Look at the scene", "Looked at the scene") + (Has(a, "root") ? " under " + Obj(a["root"]) : "") + ".";
                    case "find_objects": return v("Search the scene", "Searched the scene") + (Has(a, "component_type") ? " for " + Component(a["component_type"].Value) + "s" : Has(a, "name_contains") ? " for objects named \"" + a["name_contains"].Value + "\"" : "") + ".";
                    case "get_components": return v("Look at the components on ", "Looked at the components on ") + Obj(a["object"]) + ".";
                    case "get_component_properties": return v("Read the ", "Read the ") + Component(a["component"].Value) + " settings on " + Obj(a["object"]) + ".";
                    case "list_assets": return v("Search the project's files", "Searched the project's files") + ".";
                    case "get_material": return v("Look at the material ", "Looked at the material ") + Asset(a["path"]) + ".";
                    case "list_shaders": return v("List the installed shaders", "Listed the installed shaders") + ".";
                    case "get_shader_errors": return v("Check the shader ", "Checked the shader ") + Asset(a["shader"]) + " for errors.";
                    case "read_file": return v("Read the file ", "Read the file ") + FileName(a["path"]) + ".";
                    case "find_in_files": return v("Search the project's files", "Searched the project's files") + ".";
                    case "get_console_log": return v("Read the Console", "Read the Console") + ".";
                    case "list_tools": return v("Check which tools are installed", "Checked which tools are installed") + ".";
                    case "get_essentials": return v("Look at your Essentials lists", "Looked at your Essentials lists") + ".";
                    case "run_doctor": return v("Run the Project Doctor", "Ran the Project Doctor") + ".";
                    case "get_avatar_info": return v("Look over ", "Looked over ") + (Has(a, "object") ? Obj(a["object"]) : "your avatar") + ".";
                    case "get_mesh_bones": return v("Check which bones ", "Checked which bones ") + Obj(a["object"]) + " uses.";
                    case "get_blendshapes": return v("Look at the blendshapes", "Looked at the blendshapes") + (Has(a, "object") ? " on " + Obj(a["object"]) : "") + ".";
                    case "select_objects": return v("Show you ", "Showed you ") + (a["objects"].Count == 1 ? Obj(a["objects"][0]) : "the objects") + ".";
                    case "find_component_types": return v("Look up component types", "Looked up component types") + ".";
                    case "get_asset_properties": return v("Read ", "Read ") + Asset(a["path"]) + ".";

                    // ---- changes (confirmed before they happen)
                    case "set_component_property":
                        return v("Set ", "Set ") + Nice(a["property_path"].Value) + " to " + Value(a["value"]) + " on the " + Component(a["component"].Value) + " of " + Obj(a["object"]) + ".";
                    case "add_component": { string c = Component(a["type"].Value); string an = c.Length > 0 && "AEIOUaeiou".IndexOf(c[0]) >= 0 ? "an " : "a "; return v("Add ", "Added ") + an + c + " to " + Obj(a["object"]) + "."; }
                    case "remove_component": return v("Remove the ", "Removed the ") + Component(a["component"].Value) + " from " + Obj(a["object"]) + ".";
                    case "set_active": return (a["active"].AsBool ? v("Turn on ", "Turned on ") : v("Turn off ", "Turned off ")) + Obj(a["object"]) + ".";
                    case "create_gameobject": return v("Create a new object called \"", "Created a new object called \"") + a["name"].Value + "\"" + (Has(a, "parent") ? " under " + Obj(a["parent"]) : "") + ".";
                    case "set_material": return v("Change the material ", "Changed the material ") + Asset(a["path"]) + MaterialChanges(a) + ".";
                    case "create_material": return v("Create a new material ", "Created a new material ") + Asset(a["path"]) + " using " + a["shader"].Value + ".";
                    case "write_file": return (File.Exists(Path.Combine(ProjectPath, a["path"].Value)) ? v("Rewrite the file ", "Rewrote the file ") : v("Create the file ", "Created the file ")) + FileName(a["path"]) + ".";
                    case "run_menu_item": return v("Click the menu item ", "Clicked the menu item ") + "\"" + a["path"].Value.Replace("/", " > ") + "\".";
                    case "save_all": return v("Save the scene and all changed files", "Saved the scene and all changed files") + ".";
                    case "undo": return v("Undo the last change", "Undid the last change") + ".";
                    case "install_tool": return (a["update"].AsBool ? v("Update ", "Updated ") : v("Install ", "Installed ")) + ToolName(a["name"].Value) + ".";
                    case "remove_tool": return v("Uninstall ", "Uninstalled ") + ToolName(a["name"].Value) + ".";
                    case "add_listing_to_creator_companion": return v("Add ", "Added ") + ToolName(a["name"].Value) + v("'s download source to the VRChat Creator Companion", "'s download source to the VRChat Creator Companion") + ".";
                    case "set_essential": return (a["enabled"].AsBool ? v("Add ", "Added ") : v("Remove ", "Removed ")) + a["name"].Value + (a["enabled"].AsBool ? " to" : " from") + " your " + (a["list"].Value.StartsWith("w", StringComparison.OrdinalIgnoreCase) ? "World" : "Avatar") + " Essentials.";
                    case "run_essentials": return v("Install your ", "Installed your ") + (a["list"].Value.StartsWith("w", StringComparison.OrdinalIgnoreCase) ? "World" : "Avatar") + " Essentials.";
                    case "fix_materials":
                        return a["from_shader"].Value == "Hidden/InternalErrorShader"
                            ? v("Fix every pink material by switching it to ", "Fixed pink materials by switching them to ") + a["to_shader"].Value + "."
                            : v("Switch every material using ", "Switched every material using ") + a["from_shader"].Value + " to " + a["to_shader"].Value + ".";
                    case "remove_missing_scripts": return v("Remove broken (missing) scripts from ", "Removed broken (missing) scripts from ") + (Has(a, "object") ? Obj(a["object"]) : "the selected objects") + ".";
                    case "fix_scripting_define_symbols": return v("Reset the project's scripting define symbols so tools can set them up again", "Reset the project's scripting define symbols") + ".";
                    case "doctor_fix": return DoctorFix(a["id"].Value, past);
                    case "assign_material": return v("Put the material ", "Put the material ") + Asset(a["material"]) + " on " + Obj(a["object"]) + (Has(a, "slot") && a["slot"].AsInt > 0 ? " (slot " + a["slot"].AsInt + ")" : "") + ".";
                    case "set_blendshape": return v("Set the blendshape \"", "Set the blendshape \"") + a["name"].Value + "\" on " + Obj(a["object"]) + " to " + Value(a["value"]) + ".";
                    case "set_transform": return TransformChange(a, past);
                    case "modify_gameobject":
                    {
                        List<string> parts = new List<string>();
                        if (Has(a, "name")) parts.Add(v("rename it to \"", "renamed it to \"") + a["name"].Value + "\"");
                        if (a.HasKey("parent")) parts.Add(Has(a, "parent") ? v("move it under ", "moved it under ") + Obj(a["parent"]) : v("move it to the top of the scene", "moved it to the top of the scene"));
                        if (Has(a, "tag")) parts.Add(v("set its tag to ", "set its tag to ") + a["tag"].Value);
                        if (Has(a, "layer")) parts.Add(v("put it on the layer ", "put it on the layer ") + a["layer"].Value);
                        return v("Change ", "Changed ") + Obj(a["object"]) + (parts.Count > 0 ? ": " + string.Join(", ", parts) : "") + ".";
                    }
                    case "delete_gameobject": return v("Delete ", "Deleted ") + Obj(a["object"]) + v(" and everything under it", " and everything under it") + ".";
                    case "duplicate_gameobject": return v("Duplicate ", "Duplicated ") + Obj(a["object"]) + ".";
                    case "instantiate_prefab": return v("Place ", "Placed ") + Asset(a["path"]) + v(" in the scene", " in the scene") + (Has(a, "parent") ? " under " + Obj(a["parent"]) : "") + ".";
                    case "set_asset_property": return v("Set ", "Set ") + Nice(a["property_path"].Value) + " to " + Value(a["value"]) + " in " + Asset(a["path"]) + ".";
                    case "create_asset": return v("Create ", "Created ") + Asset(a["path"]) + " (" + Nice(a["type"].Value) + ").";
                }
                return v("Use the tool ", "Used the tool ") + Nice(call.Name) + ".";
            }

            // ---- pieces
            private static bool Has(JSONNode a, string key) => a.HasKey(key) && !a[key].IsNull && a[key].Value != "";
            private static string Component(string typeName)
            {
                string t = (typeName ?? "").Split('.').Last();
                switch (t)
                {
                    case "VRCPhysBone": return "PhysBone";
                    case "VRCPhysBoneCollider": return "PhysBone Collider";
                    case "VRCAvatarDescriptor": return "Avatar Descriptor";
                    case "VRCContactSender": return "Contact Sender";
                    case "VRCContactReceiver": return "Contact Receiver";
                    case "SkinnedMeshRenderer": return "Skinned Mesh Renderer";
                }
                string nice = ObjectNames.NicifyVariableName(t);
                return nice.StartsWith("VRC ") ? nice.Substring(4) : nice;
            }
            private static string FileName(JSONNode path) => "\"" + Path.GetFileName((path == null ? "" : path.Value).TrimEnd('/')) + "\"";
            private static string CleanLabel(string description, string propertyName)
            {
                string d = description ?? "";
                int cut = d.IndexOf("--", StringComparison.Ordinal); if (cut >= 0) d = d.Substring(0, cut);
                cut = d.IndexOf('{'); if (cut >= 0) d = d.Substring(0, cut);
                d = d.Trim();
                if (d.Length > 1 && d[0] == 's' && char.IsUpper(d[1]) && !d.Contains(" ")) d = ObjectNames.NicifyVariableName(d.Substring(1));
                return string.IsNullOrEmpty(d) ? Nice(propertyName) : d;
            }
            private static string Nice(string identifier) => string.IsNullOrEmpty(identifier) ? "" : ObjectNames.NicifyVariableName(identifier.Split('.').Last());
            private static string Obj(JSONNode path)
            {
                string p = path == null ? "" : path.Value;
                if (string.IsNullOrEmpty(p)) return "the selected object";
                return "\"" + p.TrimEnd('/').Split('/').Last() + "\"";
            }
            private static string Asset(JSONNode path)
            {
                string p = path == null ? "" : path.Value;
                return "\"" + Path.GetFileNameWithoutExtension(p.TrimEnd('/')) + "\"";
            }
            private static string ToolName(string nameOrKey)
            {
                Tool t = ToolCatalog.Find(nameOrKey);
                return t != null ? t.Name : nameOrKey;
            }
            private static string MaterialChanges(JSONNode a)
            {
                List<string> parts = new List<string>();
                if (Has(a, "shader")) parts.Add("switch it to the " + a["shader"].Value + " shader");
                if (a.HasKey("properties") && a["properties"].IsObject)
                {
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(a["path"].Value);
                    foreach (KeyValuePair<string, JSONNode> kv in a["properties"].AsObject)
                    {
                        string label = Nice(kv.Key);
                        if (m != null && m.shader != null)
                        {
                            int i = m.shader.FindPropertyIndex(kv.Key);
                            if (i >= 0) label = CleanLabel(m.shader.GetPropertyDescription(i), kv.Key);
                        }
                        parts.Add("set " + label + " to " + Value(kv.Value));
                    }
                }
                if (a.HasKey("enable_keywords") && a["enable_keywords"].Count > 0) parts.Add("turn on " + a["enable_keywords"].Count + " shader option(s)");
                if (a.HasKey("disable_keywords") && a["disable_keywords"].Count > 0) parts.Add("turn off " + a["disable_keywords"].Count + " shader option(s)");
                if (a.HasKey("render_queue")) parts.Add("set its render queue to " + a["render_queue"].Value);
                if (parts.Count == 0) return "";
                if (parts.Count > 4) parts = parts.Take(4).Concat(new[] { "and " + (parts.Count - 4) + " more change(s)" }).ToList();
                return ": " + string.Join(", ", parts);
            }
            // A value in words: numbers as numbers, true/false as on/off, colors as #RRGGBB, vectors as (x, y, z), references by name.
            private static string TransformChange(JSONNode a, bool past)
            {
                Func<string, string, string> v = (present, done) => past ? done : present;
                string obj = Obj(a["object"]);
                if (Has(a, "scale_by"))
                {
                    float f = a["scale_by"].AsFloat;
                    string how = f >= 1 ? Num(a["scale_by"]) + " times bigger" : Num(a["scale_by"]) + " times its size";
                    return v("Make ", "Made ") + obj + " " + how + ".";
                }
                List<string> parts = new List<string>();
                if (a.HasKey("position")) parts.Add(v("move", "moved"));
                if (a.HasKey("rotation")) parts.Add(v("rotate", "rotated"));
                if (a.HasKey("scale")) parts.Add(v("resize", "resized"));
                string verbs = parts.Count == 0 ? v("change", "changed") : string.Join(" and ", parts);
                return char.ToUpperInvariant(verbs[0]) + verbs.Substring(1) + " " + obj + ".";
            }
            private static string Value(JSONNode v)
            {
                if (v == null || v.IsNull) return "nothing";
                if (v.IsBoolean) return v.AsBool ? "on" : "off";
                if (v.IsNumber) return v.AsDouble.ToString("0.###", CultureInfo.InvariantCulture);
                if (v.IsString)
                {
                    string s = v.Value;
                    if (s.StartsWith("Assets/") || s.StartsWith("Packages/")) return "\"" + Path.GetFileNameWithoutExtension(s) + "\"";
                    return s.Contains("/") ? "\"" + s.Split('/').Last() + "\"" : "\"" + s + "\"";
                }
                if (v.IsObject)
                {
                    if (v.HasKey("r") && v.HasKey("g") && v.HasKey("b"))
                    {
                        Color c = new Color(v["r"].AsFloat, v["g"].AsFloat, v["b"].AsFloat, v.HasKey("a") ? v["a"].AsFloat : 1f);
                        return "the color #" + ColorUtility.ToHtmlStringRGB(c) + (c.a < 0.999f ? " (" + Mathf.RoundToInt(c.a * 100) + "% opaque)" : "");
                    }
                    if (v.HasKey("x") && v.HasKey("y"))
                    {
                        List<string> n = new List<string> { Num(v["x"]), Num(v["y"]) };
                        if (v.HasKey("z")) n.Add(Num(v["z"]));
                        if (v.HasKey("w")) n.Add(Num(v["w"]));
                        return "(" + string.Join(", ", n) + ")";
                    }
                    if (v.HasKey("object")) return Obj(v["object"]);
                    return "new values";
                }
                if (v.IsArray) return v.Count == 0 ? "an empty list" : "a list of " + v.Count;
                return v.Value;
            }
            private static string Num(JSONNode n) => n.AsDouble.ToString("0.###", CultureInfo.InvariantCulture);
            private static string DoctorFix(string id, bool past)
            {
                ProjectDoctor.Finding f = ProjectDoctor.Last != null ? ProjectDoctor.Last.Findings.FirstOrDefault(x => x.Id == id) : null;
                if (f == null || string.IsNullOrEmpty(f.FixLabel)) return (past ? "Applied a Project Doctor fix" : "Apply a Project Doctor fix") + ".";
                return (past ? "Fixed: " : "") + f.FixLabel.TrimEnd('.') + (past ? "" : ", to fix: " + f.Title) + ".";
            }
        }
    }
}
