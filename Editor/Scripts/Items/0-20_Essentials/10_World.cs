using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        private static bool[] worldEssentials;
        private static readonly string[] WENames = new string[]
        {
            "VRChat SDK 3",
            "Poiyomi Toon Shader",
            "Unity-Chan Toon Shader 2.0",
            "lilToon"
        };
        private static readonly Dictionary<int, Action> WEImports = new Dictionary<int, Action>
        {
            { 0, () => { if(!isVRCCreatorCompanion) importVRCSDK3w(); } },
            { 1, importPTS },
            { 2, importUTS },
            { 3, importLT }
        };
        [MenuItem("kebinImports/World Essentials", false, 10)]
        private static void importWorldEssentials()
        {
            foreach (var index in worldEssentials.Select((value, i) => new { value, i }))
            {
                if (index.value)
                {
                    WEImports[index.i]();
                }
            }
        }
    }
}
