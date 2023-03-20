using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        private static bool[] avatarEssentials;
        private static readonly string[] AENames = new string[]
        {
            "VRChat SDK 3",
            "Dynamic Bone",
            "Poiyomi Toon Shader",
            "Unity-Chan Toon Shader 2.0",
            "lilToon",
            "Muscle Animation Editor",
            "Pumkin's Avatar Tools",
            "ComboGestureExpressions",
            "Modular Avatar",
            "DressingTools",
            "Avatar Performance Tools"
        };
        private static readonly Dictionary<int, Action> AEImports = new Dictionary<int, Action>
        {
            { 0, () => { if(!isVRCCreatorCompanion) importVRCSDK3a(); } },
            { 1, importDB },
            { 2, importPTS },
            { 3, importUTS },
            { 4, importLT },
            { 5, importMAE },
            { 6, importPAT },
            { 7, importCGE },
            { 8, importMA },
            { 9, importDT },
            { 10, importAPT }
        };
        [MenuItem("kebinImports/Avatar Essentials", false, 0)]
        private static void importAvatarEssentials()
        {
            foreach (var index in avatarEssentials.Select((value, i) => new { value, i }))
            {
                if (index.value)
                {
                    AEImports[index.i]();
                }
            }
        }
    }
}
