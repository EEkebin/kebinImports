using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        private static class RemoveMissingScriptsTool
        {
            [MenuItem("kebinImports/Remove Missing Scripts", false, 601)]
            private static void RMS()
            {
                if (!hideWarnings && (EditorUtility.DisplayDialog(
                    "WARNING!",
                    "WARNING!\n\nThis will recursively remove all missing scripts that are in the selected asset. This includes the scripts that are not present in the project yet and are showing up as missing.\n\nAre you sure you want to do this?",
                    "Yes",
                    "No"
                ) == false)) return;
                int compCount, goCount;
                RemoveFrom(Selection.gameObjects, out compCount, out goCount);
                Debug.Log("[kebinImports] Removed " + compCount + " missing script(s) from " + goCount + " GameObject(s).");
            }
            // Shared by the menu item and the AI assistant.
            public static void RemoveFrom(IEnumerable<GameObject> roots, out int compCount, out int goCount)
            {
                var deeperSelection = roots.SelectMany(go => go.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject);
                var prefabs = new HashSet<Object>();
                compCount = 0;
                goCount = 0;
                foreach (var go in deeperSelection)
                {
                    int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                    if (count > 0)
                    {
                        if (PrefabUtility.IsPartOfAnyPrefab(go))
                        {
                            RecursivePrefabSource(go, prefabs, ref compCount, ref goCount);
                            count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                            if (count == 0) continue;
                        }
                        Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                        compCount += count;
                        goCount++;
                    }
                }
            }
            private static void RecursivePrefabSource(GameObject instance, HashSet<Object> prefabs, ref int compCount, ref int goCount)
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
                if (source == null || !prefabs.Add(source)) return;
                RecursivePrefabSource(source, prefabs, ref compCount, ref goCount);
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(source);
                if (count > 0)
                {
                    Undo.RegisterCompleteObjectUndo(source, "Remove missing scripts");
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(source);
                    compCount += count;
                    goCount++;
                }
            }
        }
    }
}
