using System.IO;
using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        private static AssetBundle assetBundle = null;
        private static GameObject _gameObject = null;
        [MenuItem("Assets/kebinImports/Load AssetBundle", true)]
        private static bool LoadAssetBundleValidate()
        {
            if (Selection.activeObject)
            {
                string path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (Path.HasExtension(path))
                {
                    return true;
                }
            }
            return false;
        }
        [MenuItem("Assets/kebinImports/Load AssetBundle", false)]
        private static void LoadAssetBundle()
        {
            if (assetBundle)
            {
                assetBundle.Unload(true);
                DestroyImmediate(assetBundle);
                DestroyImmediate(_gameObject);
            }
            assetBundle = AssetBundle.LoadFromFile(AssetDatabase.GetAssetPath(Selection.activeObject));
            foreach (Object obj in assetBundle.LoadAllAssets())
            {
                _gameObject = (GameObject)Instantiate(obj);
            }
            EditorApplication.update += ClearLog;
        }
    }
}
