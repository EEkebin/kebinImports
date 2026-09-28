using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        // The Project Doctor's check of an AssetBundle preview (Load AssetBundle). A bundle carries compiled copies of its
        // shaders; when it was built for another platform (a Quest avatar, say) those copies can't draw here and the
        // preview is pink. The shader names survive, so the fix is to use this project's shader of the same name, and to
        // install the tool it comes from when the project doesn't have it. Components can't be identified this way (a
        // bundle doesn't record which tool a script came from), but a VRChat bundle can only carry VRChat's own.
        internal static partial class ProjectDoctor
        {
            private static void CheckBundlePreview(Report r)
            {
                List<GameObject> roots = PreviewRoots();
                if (roots.Count == 0) return;
                string path = PreviewBundlePath;
                string bundle = string.IsNullOrEmpty(path) ? "The previewed AssetBundle" : Path.GetFileName(path);
                string ext = Path.GetExtension(path ?? "").ToLowerInvariant();
                Tool sdk = ext == ".vrcw" ? ToolCatalog.Get("vrcsdk-worlds") : ext == ".vrca" || ext == ".vrcp" ? ToolCatalog.Get("vrcsdk-avatars") : null;
                bool sdkMissing = sdk != null && !ToolCatalog.GetStatus(sdk, false).Installed;
                const string why = " was probably built for Quest (Android), so the shaders packed inside it can't draw on this PC.";

                // Materials
                List<Material> pink = PreviewMaterials(roots).Where(RendersPink).ToList();
                List<Material> haveShader = new List<Material>();
                Dictionary<string, List<Material>> needTool = new Dictionary<string, List<Material>>();
                List<Material> unknown = new List<Material>();
                foreach (Material m in pink)
                {
                    if (ProjectShaderFor(m) != null) { haveShader.Add(m); continue; }
                    KnownSignatures.Match match = KnownSignatures.ByShaderName(m.shader.name) ?? KnownSignatures.ByMaterial(m);
                    Tool t = match == null ? null : ToolCatalog.Get(match.ToolKey);
                    if (t != null && !ToolCatalog.GetStatus(t, false).Installed)
                    {
                        if (!needTool.ContainsKey(t.Key)) needTool[t.Key] = new List<Material>();
                        needTool[t.Key].Add(m);
                    }
                    else unknown.Add(m);
                }
                if (haveShader.Count > 0)
                {
                    r.Findings.Add(new Finding
                    {
                        Id = "preview-shaders",
                        Severity = "warning",
                        Title = Plural(haveShader.Count, "material in the preview is pink, but this project has its shader", "materials in the preview are pink, but this project has their shaders"),
                        Detail = bundle + why + " This project has the same shaders, so the preview can use those instead. Only the preview changes; nothing is saved.",
                        FixLabel = "Use this project's shaders",
                        Safe = true,
                        Fix = () => UseProjectShadersForPreview(),
                    });
                }
                foreach (KeyValuePair<string, List<Material>> group in needTool)
                {
                    Tool t = ToolCatalog.Get(group.Key);
                    if (sdkMissing && t == sdk) continue; // covered by the SDK problem below
                    r.Findings.Add(new Finding
                    {
                        Id = "preview-needs-" + t.Key,
                        Severity = "warning",
                        Title = Plural(group.Value.Count, "material in the preview uses ", "materials in the preview use ") + t.Name + ", which isn't installed",
                        Detail = bundle + why + " Installing " + t.Name + " lets the preview use this project's copy instead. The preview reloads by itself afterwards.",
                        FixLabel = t.Paid ? "Install " + t.Name + " (needs your purchased copy)" : "Install " + t.Name,
                        Recompiles = true,
                        Fix = () => { ReloadPreviewAfterInstall(); ToolCatalog.Install(t); },
                    });
                }
                if (unknown.Count > 0)
                {
                    List<string> names = unknown.Select(m => KnownSignatures.UnlockedShaderName(m.shader.name)).Distinct().ToList();
                    r.Findings.Add(new Finding
                    {
                        Id = "preview-unknown-shaders",
                        Severity = "info",
                        Title = Plural(unknown.Count, "material in the preview uses a shader", "materials in the preview use shaders") + " kebinImports doesn't recognise",
                        Detail = bundle + why + " The " + (names.Count == 1 ? "shader it wants is " : "shaders it wants are ") + string.Join(", ", names.Take(8)) + (names.Count > 8 ? " and " + (names.Count - 8) + " more" : "") + ". If you have " + (names.Count == 1 ? "it" : "them") + ", add " + (names.Count == 1 ? "it" : "them") + " to the project and check again. Or show these materials with Unity's Standard shader for now: colours and main textures carry over, but the look won't match exactly.",
                        FixLabel = "Show them with Standard",
                        Fix = () => { Shader standard = Shader.Find("Standard"); foreach (Material m in unknown) if (m != null) m.shader = standard; },
                    });
                }

                // Components
                int missing = roots.Sum(root => root.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
                if (missing == 0) return;
                if (sdkMissing)
                {
                    int sdkMaterials = needTool.ContainsKey(sdk.Key) ? needTool[sdk.Key].Count : 0;
                    r.Findings.Add(new Finding
                    {
                        Id = "preview-needs-sdk",
                        Severity = "warning",
                        Title = Plural(missing, "component in the preview doesn't work", "components in the preview don't work") + " because the VRChat SDK isn't installed",
                        Detail = "VRChat " + (sdk.Key == "vrcsdk-worlds" ? "worlds" : "avatars") + " only carry VRChat's own components, such as " + (sdk.Key == "vrcsdk-worlds" ? "Udon behaviours" : "the avatar descriptor and PhysBones") + ", and those come from the VRChat SDK." + (sdkMaterials > 0 ? " " + Plural(sdkMaterials, "material uses a VRChat shader", "materials use VRChat shaders") + " from it too." : "") + " Installing " + sdk.Name + " brings them back. The preview reloads by itself afterwards.",
                        FixLabel = "Install the VRChat SDK",
                        Recompiles = true,
                        Fix = () => { ReloadPreviewAfterInstall(); ToolCatalog.Install(sdk); },
                    });
                    return;
                }
                bool canReload = !string.IsNullOrEmpty(path) && File.Exists(path);
                r.Findings.Add(new Finding
                {
                    Id = "preview-unknown-scripts",
                    Severity = "info",
                    Title = Plural(missing, "component in the preview comes", "components in the preview come") + " from a tool that isn't in this project",
                    Detail = "AssetBundles don't record which tool a component came from, so kebinImports can't tell which one. The rest of the preview still works." + (canReload ? " If you installed a tool after loading the preview, reloading it can bring them back." : ""),
                    FixLabel = canReload ? "Reload the preview" : null,
                    Fix = canReload ? (Action)(() => PreviewBundle(path, true)) : null,
                });
            }

            // Switches every pink preview material whose shader this project has to the project's copy. Returns how many.
            internal static int UseProjectShadersForPreview()
            {
                int count = 0;
                foreach (Material m in PreviewMaterials(PreviewRoots()).Where(RendersPink))
                {
                    Shader s = ProjectShaderFor(m);
                    if (s == null) continue;
                    m.shader = s; // keeps the material's properties; the material only lives in memory
                    count++;
                }
                return count;
            }

            private static List<Material> PreviewMaterials(List<GameObject> roots)
            {
                return roots.SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).SelectMany(rd => rd.sharedMaterials).Where(m => m != null && m.shader != null).Distinct().ToList();
            }

            // This project's shader for a preview material. Shader.Find only sees the project's shaders, never the
            // bundle's own copies, so any hit is one that draws here.
            private static Shader ProjectShaderFor(Material m)
            {
                string name = m.shader.name;
                KnownSignatures.Match match = KnownSignatures.ByShaderName(name);
                foreach (string candidate in new[] { name, KnownSignatures.UnlockedShaderName(name), match != null ? match.Name : null })
                {
                    if (string.IsNullOrEmpty(candidate)) continue;
                    Shader s = Shader.Find(candidate);
                    if (s != null && s != m.shader && s.isSupported) return s;
                }
                return null;
            }

            // Whether the material draws as Unity's magenta error shader. A bundle shader built for another platform still
            // reports itself as supported, so the only reliable test is to draw it once and look.
            private static readonly Dictionary<int, bool> pinkCache = new Dictionary<int, bool>();
            private static bool RendersPink(Material m)
            {
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return !m.shader.isSupported;
                int key = m.GetInstanceID() ^ (m.shader.GetInstanceID() * 397);
                bool pink;
                if (pinkCache.TryGetValue(key, out pink)) return pink;
                RenderTexture rt = RenderTexture.GetTemporary(8, 8, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                RenderTexture previous = RenderTexture.active;
                Texture2D read = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                try
                {
                    RenderTexture.active = rt;
                    GL.Clear(true, true, Color.black);
                    if (m.SetPass(0))
                    {
                        GL.PushMatrix();
                        GL.LoadOrtho();
                        GL.Begin(GL.QUADS);
                        GL.Vertex3(0, 0, 0.5f); GL.Vertex3(0, 1, 0.5f); GL.Vertex3(1, 1, 0.5f); GL.Vertex3(1, 0, 0.5f);
                        GL.End();
                        GL.PopMatrix();
                    }
                    read.ReadPixels(new Rect(0, 0, 8, 8), 0, 0);
                    read.Apply();
                    Color32[] px = read.GetPixels32();
                    pink = px.Count(p => p.r > 240 && p.g < 15 && p.b > 240) > px.Length / 2;
                }
                finally
                {
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(rt);
                    UnityEngine.Object.DestroyImmediate(read);
                }
                pinkCache[key] = pink;
                return pink;
            }
        }
    }
}
