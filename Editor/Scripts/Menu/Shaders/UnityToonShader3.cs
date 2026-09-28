using System;
using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        // Unity Toon Shader (UTS3) is an official Unity package on the Unity registry, so it is added through the
        // Package Manager rather than downloaded. Versions after 0.12 need Unity 6, so the newest version the running
        // editor supports is looked up on the registry first.
        [MenuItem("kebinImports/Shaders/Unity Toon Shader 3", false, 100)]
        private static void importUTS3()
        {
            ToolCatalog.Install("uts3");
        }

        // "2022.3.22f1" or "2021.3" -> 2022.3 / 2021.3 (major.minor only).
        private static Version ParseUnityVersion(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            string[] parts = text.Split('.');
            int major, minor = 0;
            if (parts.Length == 0 || !int.TryParse(parts[0], out major)) return null;
            if (parts.Length > 1) int.TryParse(parts[1], out minor);
            return new Version(major, minor);
        }
        // "0.12.0-preview" -> 0.12.0 (pre-release suffix ignored).
        private static Version ParsePackageVersion(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            int dash = text.IndexOf('-');
            if (dash >= 0) text = text.Substring(0, dash);
            Version v;
            return Version.TryParse(text.Contains(".") ? text : text + ".0", out v) ? v : null;
        }
    }
}
