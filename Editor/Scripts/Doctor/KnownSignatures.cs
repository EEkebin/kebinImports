using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace kebinImports
{
    public partial class kebinImports
    {
        // Identifies what a missing shader or missing script belonged to. Two layers: exact GUIDs harvested from the
        // tools' packages (KnownSignatures.Guids.cs), and content fingerprints for cases the GUID table cannot cover:
        // paid assets we cannot harvest, locked/generated shader copies, and older versions with different GUIDs.
        internal static partial class KnownSignatures
        {
            internal class Match
            {
                public string ToolKey;
                public string Kind;   // script | assembly | shader
                public string Name;   // class name / assembly / shader name
                public string How;    // guid | fingerprint
            }

            private class Fingerprint
            {
                public string ToolKey;
                public string Kind;
                public string Name;
                public string[] AllOf; // every marker must appear in the serialized block
            }

            // Serialized field names (scripts) and material property names (shaders) that only one tool uses.
            private static readonly Fingerprint[] Fingerprints =
            {
                new Fingerprint { ToolKey = "dynamic-bone", Kind = "script", Name = "DynamicBone", AllOf = new[] { "m_Damping", "m_Elasticity", "m_Stiffness", "m_Inert" } },
                new Fingerprint { ToolKey = "dynamic-bone", Kind = "script", Name = "DynamicBoneCollider", AllOf = new[] { "m_Center", "m_Radius", "m_Bound", "m_Direction" } },
                new Fingerprint { ToolKey = "vrcsdk-avatars", Kind = "script", Name = "VRCPhysBone", AllOf = new[] { "rootTransform", "integrationType", "pull", "spring" } },
                new Fingerprint { ToolKey = "vrcsdk-avatars", Kind = "script", Name = "VRCAvatarDescriptor", AllOf = new[] { "ViewPosition", "lipSync", "baseAnimationLayers" } },
                new Fingerprint { ToolKey = "vrcsdk-avatars", Kind = "script", Name = "VRCContactReceiver", AllOf = new[] { "collisionTags", "receiverType", "parameter" } },
                new Fingerprint { ToolKey = "vrcfury", Kind = "script", Name = "VRCFury", AllOf = new[] { "VF.Model" } },
                new Fingerprint { ToolKey = "modular-avatar", Kind = "script", Name = "Modular Avatar", AllOf = new[] { "nadena.dev.modular_avatar" } },
                new Fingerprint { ToolKey = "cge", Kind = "script", Name = "ComboGestureExpressions", AllOf = new[] { "comboLayers", "activityStageName" } },
                new Fingerprint { ToolKey = "liltoon", Kind = "shader", Name = "lilToon", AllOf = new[] { "_lilToonVersion" } },
                new Fingerprint { ToolKey = "poiyomi", Kind = "shader", Name = ".poiyomi/Poiyomi Toon", AllOf = new[] { "_ShaderOptimizerEnabled", "_MainColorAdjustToggle" } },
                new Fingerprint { ToolKey = "poiyomi", Kind = "shader", Name = ".poiyomi/Poiyomi Toon", AllOf = new[] { "_PoiyomiVersion" } },
                new Fingerprint { ToolKey = "uts2", Kind = "shader", Name = "UnityChanToonShader/Toon_DoubleShadeWithFeather", AllOf = new[] { "_1st_ShadeColor", "_2nd_ShadeColor", "_Set_1st_ShadePosition" } },
                new Fingerprint { ToolKey = "uts3", Kind = "shader", Name = "Toon (Built-in)", AllOf = new[] { "_1st_ShadeColor", "_utsVersionX" } },
                new Fingerprint { ToolKey = "xiexe", Kind = "shader", Name = "Xiexe/Toon", AllOf = new[] { "_RampColor", "_ShadowSharpness", "_OcclusionMap" } },
                new Fingerprint { ToolKey = "mochie", Kind = "shader", Name = "Mochie/Standard", AllOf = new[] { "_MochieVersion" } },
                new Fingerprint { ToolKey = "arktoon", Kind = "shader", Name = "arktoon/Opaque", AllOf = new[] { "_ShadowStrength", "_ShadowborderBlur", "_ShadowPlanBDefaultShadowMix" } },
                new Fingerprint { ToolKey = "cubed", Kind = "shader", Name = "Cubed's Unity Shaders/Flat Lit Toon", AllOf = new[] { "_EmissionColor", "_ShadowMask", "_Shadow" , "_LightingType" } },
                new Fingerprint { ToolKey = "audiolink", Kind = "shader", Name = "AudioLink", AllOf = new[] { "_AudioTexture" } },
            };

            private static readonly Regex ScriptRef = new Regex(@"m_Script:\s*\{fileID:\s*-?\d+,\s*guid:\s*([0-9a-fA-F]{32})", RegexOptions.Compiled);
            private static readonly Regex ShaderRef = new Regex(@"m_Shader:\s*\{fileID:\s*-?\d+,\s*guid:\s*([0-9a-fA-F]{32})", RegexOptions.Compiled);

            public static Match ByGuid(string guid)
            {
                string[] row;
                if (string.IsNullOrEmpty(guid) || !Guids.TryGetValue(guid.ToLowerInvariant(), out row)) return null;
                return new Match { ToolKey = row[0], Kind = row[1], Name = row[2], How = "guid" };
            }
            public static Match ByContent(string serializedBlock, string kind)
            {
                if (string.IsNullOrEmpty(serializedBlock)) return null;
                foreach (Fingerprint f in Fingerprints)
                {
                    if (f.Kind != kind) continue;
                    if (f.AllOf.All(marker => serializedBlock.IndexOf(marker, StringComparison.Ordinal) >= 0)) return new Match { ToolKey = f.ToolKey, Kind = f.Kind, Name = f.Name, How = "fingerprint" };
                }
                return null;
            }
            // For a material file's text: the shader GUID it references and the best identification of it.
            public static Match IdentifyMaterial(string materialYaml, out string shaderGuid)
            {
                System.Text.RegularExpressions.Match m = ShaderRef.Match(materialYaml ?? "");
                shaderGuid = m.Success ? m.Groups[1].Value.ToLowerInvariant() : null;
                return ByGuid(shaderGuid) ?? ByContent(materialYaml, "shader");
            }
            // For a scene or prefab file's text: every MonoBehaviour block whose script GUID has no asset, identified.
            public static List<KeyValuePair<string, Match>> MissingScriptsIn(string yaml, Func<string, bool> guidExists)
            {
                List<KeyValuePair<string, Match>> result = new List<KeyValuePair<string, Match>>();
                if (string.IsNullOrEmpty(yaml)) return result;
                string[] blocks = yaml.Split(new[] { "\n--- " }, StringSplitOptions.None);
                foreach (string block in blocks)
                {
                    if (!block.StartsWith("!u!114 ") && !block.Contains("\nMonoBehaviour:")) continue;
                    System.Text.RegularExpressions.Match m = ScriptRef.Match(block);
                    if (!m.Success) continue;
                    string guid = m.Groups[1].Value.ToLowerInvariant();
                    if (guidExists(guid)) continue;
                    result.Add(new KeyValuePair<string, Match>(guid, ByGuid(guid) ?? ByContent(block, "script")));
                }
                return result;
            }
        }
    }
}
