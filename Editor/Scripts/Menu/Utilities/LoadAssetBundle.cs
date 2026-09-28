using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace kebinImports
{
    public partial class kebinImports
    {
        // Previews an AssetBundle (such as a VRChat .vrca avatar, .vrcw world or .vrcp file) by placing its objects in the open
        // scene. It only previews: nothing from the bundle is saved into the project.
        private static readonly string[] BundleExtensions = { ".vrca", ".vrcw", ".vrcp", ".assetbundle", ".unity3d", ".bundle", "" };
        private static readonly List<GameObject> previewObjects = new List<GameObject>();

        [MenuItem("Assets/kebinImports/Load AssetBundle", true)]
        private static bool LoadAssetBundleValidate()
        {
            if (!Selection.activeObject) return false;
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path) || Directory.Exists(path)) return false;
            return BundleExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());
        }

        [MenuItem("Assets/kebinImports/Load AssetBundle", false)]
        private static void LoadAssetBundle()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            string fullPath = Path.GetFullPath(Path.Combine(ProjectPath, path));
            // Check which Unity built the file before handing it to Unity, which otherwise logs red errors for bundles
            // it can't open.
            string builtWith = BundleUnityVersion(fullPath);
            if (builtWith != null && builtWith != Application.unityVersion)
            {
                EditorUtility.DisplayDialog("kebinImports",
                    Path.GetFileName(path) + " can't be loaded: it was built with Unity " + builtWith + ", which isn't supported.\n\n" +
                    "Only AssetBundles built with Unity " + Application.unityVersion + " can be loaded in this project.", "Ok");
                return;
            }
            UnloadPreviewBundles();
            AssetBundle bundle;
            try
            {
                EditorUtility.DisplayProgressBar("kebinImports", "Loading " + Path.GetFileName(path) + "…", 0.5f);
                bundle = AssetBundle.LoadFromFile(fullPath);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            if (bundle == null)
            {
                EditorUtility.DisplayDialog("kebinImports", Path.GetFileName(path) + " couldn't be loaded. It may not be an AssetBundle, or it was made for a different version of Unity.", "Ok");
                return;
            }
            GameObject[] prefabs = bundle.LoadAllAssets<GameObject>();
            if (prefabs.Length == 0)
            {
                string scenes = bundle.isStreamedSceneAssetBundle ? " It contains a world scene, which can't be previewed this way." : "";
                bundle.Unload(true);
                EditorUtility.DisplayDialog("kebinImports", Path.GetFileName(path) + " has no objects to place in the scene." + scenes, "Ok");
                return;
            }
            foreach (GameObject prefab in prefabs)
            {
                GameObject instance = Object.Instantiate(prefab);
                instance.name = prefab.name + " (preview)";
                // Mark it so it is never saved with the scene and can be found again after a script reload.
                instance.hideFlags = HideFlags.DontSave;
                previewObjects.Add(instance);
            }
            Selection.activeGameObject = previewObjects.Last();
            SceneView.FrameLastActiveSceneView();
            Debug.Log("[kebinImports] Previewing " + Path.GetFileName(path) + ": " + string.Join(", ", prefabs.Select(p => p.name)) + ". The preview is not saved with the scene; loading another bundle replaces it.");
        }

        // Reads the Unity version stored in an AssetBundle's header ("UnityFS", format, "5.x.x", then the engine version).
        // Returns null when the file is not a UnityFS bundle.
        private static string BundleUnityVersion(string file)
        {
            try
            {
                byte[] head = new byte[128];
                int read;
                using (FileStream fs = File.OpenRead(file)) read = fs.Read(head, 0, head.Length);
                string text = System.Text.Encoding.ASCII.GetString(head, 0, read);
                if (!text.StartsWith("UnityFS\0")) return null;
                string[] parts = text.Substring(12).Split('\0'); // skip "UnityFS\0" and the 4-byte format number
                return parts.Length > 1 && parts[1].Length > 0 ? parts[1] : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
        // Removes the previous preview, including one left behind by a script reload (when this class's lists were reset
        // but Unity kept the bundle loaded, which would otherwise make the same file refuse to load again).
        private static void UnloadPreviewBundles()
        {
            foreach (GameObject go in previewObjects) if (go != null) Object.DestroyImmediate(go);
            previewObjects.Clear();
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go != null && go.transform.parent == null && go.hideFlags == HideFlags.DontSave && go.name.EndsWith(" (preview)") && go.scene.IsValid()) Object.DestroyImmediate(go);
            }
            foreach (AssetBundle loaded in AssetBundle.GetAllLoadedAssetBundles().ToList()) loaded.Unload(true);
        }
    }
}
