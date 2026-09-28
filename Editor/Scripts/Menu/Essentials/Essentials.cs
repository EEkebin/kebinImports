using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        // An entry of the Avatar or World Essentials list: the tool catalog key it installs and its display name.
        internal class Essential
        {
            public string Key;
            public string Name;
            public string Group;
            public bool DefaultOn;
            public Essential(string group, string key, string name, bool defaultOn = false) { Group = group; Key = key; Name = name; DefaultOn = defaultOn; }
        }

        // Order here is the order in Customize Essentials and the order things are installed in.
        internal static readonly Essential[] AvatarEssentialDefs =
        {
            new Essential("SDK", "vrcsdk-avatars", "VRChat SDK 3", true),
            new Essential("Shaders", "uts3", "Unity Toon Shader 3"),
            new Essential("Shaders", "liltoon", "lilToon"),
            new Essential("Shaders", "poiyomi", "Poiyomi Toon Shader"),
            new Essential("Shaders", "uts2", "Unity-Chan Toon Shader 2.0"),
            new Essential("Avatar tools", "modular-avatar", "Modular Avatar"),
            new Essential("Avatar tools", "vrcfury", "VRCFury"),
            new Essential("Avatar tools", "dressingtools", "DressingTools"),
            new Essential("Avatar tools", "pumkin", "Pumkin's Avatar Tools"),
            new Essential("Avatar tools", "cge", "ComboGestureExpressions"),
            new Essential("Avatar tools", "apt", "Avatar Performance Tools"),
            new Essential("Avatar tools", "gesture-manager", "Gesture Manager"),
            new Essential("Avatar tools", "av3emulator", "Av3Emulator"),
            new Essential("Avatar tools", "mae", "Muscle Animation Editor"),
            new Essential("Legacy", "dynamic-bone", "Dynamic Bone"),
        };
        internal static readonly Essential[] WorldEssentialDefs =
        {
            new Essential("SDK", "vrcsdk-worlds", "VRChat SDK 3", true),
            new Essential("Shaders", "uts3", "Unity Toon Shader 3"),
            new Essential("Shaders", "liltoon", "lilToon"),
            new Essential("Shaders", "poiyomi", "Poiyomi Toon Shader"),
            new Essential("Shaders", "uts2", "Unity-Chan Toon Shader 2.0"),
            new Essential("World tools", "vrworld-toolkit", "VRWorld Toolkit"),
            new Essential("World tools", "audiolink", "AudioLink"),
        };
        private static string[] AENames => AvatarEssentialDefs.Select(e => e.Name).ToArray();
        private static string[] WENames => WorldEssentialDefs.Select(e => e.Name).ToArray();
        private static bool[] avatarEssentials;
        private static bool[] worldEssentials;

        // Toggles are stored by tool key ("kebinImports.avatarEssentials.v2"), so reordering the list never scrambles
        // them. The first version stored them by position; that format is migrated once.
        private static readonly string[] LegacyAvatarOrder = { "vrcsdk-avatars", "dynamic-bone", "poiyomi", "uts2", "liltoon", "mae", "pumkin", "cge", "modular-avatar", "dressingtools", "apt", "vrcfury", "uts3", "gesture-manager", "av3emulator" };
        private static readonly string[] LegacyWorldOrder = { "vrcsdk-worlds", "poiyomi", "uts2", "liltoon", "uts3", "vrworld-toolkit", "audiolink" };

        private static void LoadEssentials()
        {
            avatarEssentials = LoadEssentialSet("kebinImports.avatarEssentials", AvatarEssentialDefs, LegacyAvatarOrder);
            worldEssentials = LoadEssentialSet("kebinImports.worldEssentials", WorldEssentialDefs, LegacyWorldOrder);
        }
        private static bool[] LoadEssentialSet(string prefKey, Essential[] defs, string[] legacyOrder)
        {
            HashSet<string> enabled;
            if (EditorPrefs.HasKey(prefKey + ".v2"))
            {
                enabled = new HashSet<string>(EditorPrefs.GetString(prefKey + ".v2", "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }
            else if (EditorPrefs.HasKey(prefKey))
            {
                string old = EditorPrefs.GetString(prefKey, "");
                enabled = new HashSet<string>();
                for (int i = 0; i < old.Length && i < legacyOrder.Length; i++) if (old[i] == '1') enabled.Add(legacyOrder[i]);
            }
            else
            {
                enabled = new HashSet<string>(defs.Where(d => d.DefaultOn).Select(d => d.Key));
            }
            return defs.Select(d => enabled.Contains(d.Key)).ToArray();
        }
        private static void SaveEssentials()
        {
            EditorPrefs.SetString("kebinImports.avatarEssentials.v2", string.Join(",", AvatarEssentialDefs.Where((d, i) => avatarEssentials[i]).Select(d => d.Key)));
            EditorPrefs.SetString("kebinImports.worldEssentials.v2", string.Join(",", WorldEssentialDefs.Where((d, i) => worldEssentials[i]).Select(d => d.Key)));
        }
        private static void RunEssentialList(Essential[] defs, bool[] enabled)
        {
            for (int i = 0; i < defs.Length; i++)
            {
                if (!enabled[i]) continue;
                if (defs[i].Key.StartsWith("vrcsdk") && isVRCCreatorCompanion) continue; // the Creator Companion owns the SDK
                try { ToolCatalog.Install(defs[i].Key); }
                catch (Exception e) { Debug.LogError("[kebinImports] " + defs[i].Name + ": " + (e.InnerException ?? e).Message); }
            }
        }

        [MenuItem("kebinImports/Avatar Essentials", false, 0)]
        private static void importAvatarEssentials() { RunEssentialList(AvatarEssentialDefs, avatarEssentials); }

        [MenuItem("kebinImports/World Essentials", false, 1)]
        private static void importWorldEssentials() { RunEssentialList(WorldEssentialDefs, worldEssentials); }
    }
}
