using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Object = UnityEngine.Object;

namespace kebinImports
{
    public partial class kebinImports
    {
        // Previews an AssetBundle (such as a VRChat .vrca avatar, .vrcw world or .vrcp file) by placing its objects in the open
        // scene. It only previews: nothing from the bundle is saved into the project.
        private static readonly string[] BundleExtensions = { ".vrca", ".vrcw", ".vrcp", ".assetbundle", ".unity3d", ".bundle", "" };
        private const string PreviewSuffix = " (preview)";
        private const string PreviewPathKey = "kebinImports.previewBundle";
        private const string PreviewReloadKey = "kebinImports.previewReload";

        // The bundle being previewed; kept for the session so the Project Doctor and a reload after an install can find it.
        internal static string PreviewBundlePath => SessionState.GetString(PreviewPathKey, "");

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
            PreviewBundle(Path.GetFullPath(Path.Combine(ProjectPath, path)), true);
        }

        internal static bool PreviewBundle(string fullPath, bool interactive)
        {
            string fileName = Path.GetFileName(fullPath);
            // Check which Unity built the file before handing it to Unity, which otherwise logs red errors for bundles
            // it can't open.
            string builtWith = BundleUnityVersion(fullPath);
            if (builtWith != null && builtWith != Application.unityVersion)
            {
                if (interactive)
                {
                    EditorUtility.DisplayDialog("kebinImports",
                        fileName + " can't be loaded: it was built with Unity " + builtWith + ", which isn't supported.\n\n" +
                        "Only AssetBundles built with Unity " + Application.unityVersion + " can be loaded in this project.", "Ok");
                }
                return false;
            }
            UnloadPreviewBundles();
            AssetBundle bundle;
            try
            {
                EditorUtility.DisplayProgressBar("kebinImports", "Loading " + fileName + "…", 0.5f);
                bundle = AssetBundle.LoadFromFile(fullPath);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            if (bundle == null)
            {
                if (interactive) EditorUtility.DisplayDialog("kebinImports", fileName + " couldn't be loaded. It may not be an AssetBundle, or it was made for a different version of Unity.", "Ok");
                return false;
            }
            if (bundle.isStreamedSceneAssetBundle)
            {
                bundle.Unload(true);
                if (interactive) EditorUtility.DisplayDialog("kebinImports", fileName + " contains a world scene, which can't be previewed this way.", "Ok");
                return false;
            }
            GameObject[] prefabs = bundle.LoadAllAssets<GameObject>();
            if (prefabs.Length == 0)
            {
                bundle.Unload(true);
                if (interactive) EditorUtility.DisplayDialog("kebinImports", fileName + " has no objects to place in the scene.", "Ok");
                return false;
            }
            foreach (GameObject prefab in prefabs)
            {
                GameObject instance = Object.Instantiate(prefab);
                instance.name = prefab.name + PreviewSuffix;
                // Mark it so it is never saved with the scene and can be found again after a script reload.
                instance.hideFlags = HideFlags.DontSave;
            }
            SessionState.SetString(PreviewPathKey, fullPath);
            // Frame it without selecting it: the VRChat SDK's avatar descriptor inspector throws errors for a descriptor
            // that came out of a bundle.
            Renderer[] renderers = PreviewRoots().SelectMany(go => go.GetComponentsInChildren<Renderer>(true)).ToArray();
            if (renderers.Length > 0 && SceneView.lastActiveSceneView != null)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer rd in renderers) bounds.Encapsulate(rd.bounds);
                SceneView.lastActiveSceneView.Frame(bounds, false);
            }
            Debug.Log("[kebinImports] Previewing " + fileName + ": " + string.Join(", ", prefabs.Select(p => p.name)) + ". The preview is not saved with the scene; loading another bundle replaces it. If it looks wrong, the Project Doctor can help.");
            return true;
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

        // The preview's top-level objects in the open scenes. They survive script reloads, so they are found by their
        // marking rather than kept in a list.
        internal static List<GameObject> PreviewRoots()
        {
            List<GameObject> roots = new List<GameObject>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                roots.AddRange(scene.GetRootGameObjects().Where(IsPreviewRoot));
            }
            return roots;
        }
        internal static bool IsPreviewRoot(GameObject go) => go != null && go.transform.parent == null && go.hideFlags == HideFlags.DontSave && go.name.EndsWith(PreviewSuffix);

        // Removes the previous preview, including one left behind by a script reload (when Unity kept the bundle loaded,
        // which would otherwise make the same file refuse to load again).
        private static void UnloadPreviewBundles()
        {
            foreach (GameObject go in PreviewRoots()) Object.DestroyImmediate(go);
            foreach (AssetBundle loaded in AssetBundle.GetAllLoadedAssetBundles().ToList()) loaded.Unload(true);
        }

        // Asks for the preview to be loaded again after the next script reload, for fixes that install something the
        // preview needs (a shader, or the VRChat SDK for its components): a bundle only picks those up when it is loaded.
        internal static void ReloadPreviewAfterInstall() => SessionState.SetBool(PreviewReloadKey, true);

        [InitializeOnLoadMethod]
        private static void ReloadPreviewIfAsked()
        {
            if (!SessionState.GetBool(PreviewReloadKey, false)) return;
            EditorApplication.delayCall += ReloadPreviewWhenReady;
        }
        private static void ReloadPreviewWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += ReloadPreviewWhenReady; return; }
            SessionState.EraseBool(PreviewReloadKey);
            string path = PreviewBundlePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            if (!PreviewBundle(path, false)) return;
            int fixedCount = ProjectDoctor.UseProjectShadersForPreview();
            Debug.Log("[kebinImports] Reloaded the preview of " + Path.GetFileName(path) + (fixedCount > 0 ? " and switched " + fixedCount + (fixedCount == 1 ? " material" : " materials") + " to this project's shaders." : "."));
        }
    }
}
