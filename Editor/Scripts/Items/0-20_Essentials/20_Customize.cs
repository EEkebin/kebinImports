using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        private class CustomizeWindow : EditorWindow
        {
            [MenuItem("kebinImports/Customize Essentials", false, 20)]
            private static void ShowWindow()
            {
                EditorApplication.update -= CustomizeWindow.ShowWindow;
                CustomizeWindow window = EditorWindow.GetWindow<CustomizeWindow>(true, "kebinImports - Customize Essentials");
                window.minSize = new Vector2(400, 300);
                window.maxSize = new Vector2(400, 300);
                window.Show();
            }
            private void OnGUI()
            {
                GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 18
                };
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Avatar Essentials", headerStyle);
                GUILayout.Space(5);
                if (GUILayout.Button("Toggle All"))
                {
                    bool allTrue = true;
                    for (int i = 0; i < avatarEssentials.Length; i++)
                    {
                        if (!avatarEssentials[i])
                        {
                            allTrue = false;
                            break;
                        }
                    }
                    for (int i = 0; i < avatarEssentials.Length; i++)
                    {
                        avatarEssentials[i] = !allTrue;
                    }
                }
                GUI.enabled = !isVRCCreatorCompanion;
                avatarEssentials[0] = GUILayout.Toggle(avatarEssentials[0], AENames[0]);
                GUI.enabled = true;
                for (int i = 1; i < avatarEssentials.Length; i++)
                {
                    avatarEssentials[i] = GUILayout.Toggle(avatarEssentials[i], AENames[i]);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("World Essentials", headerStyle);
                GUILayout.Space(5);
                if (GUILayout.Button("Toggle All"))
                {
                    bool allTrue = true;
                    for (int i = 0; i < worldEssentials.Length; i++)
                    {
                        if (!worldEssentials[i])
                        {
                            allTrue = false;
                            break;
                        }
                    }
                    for (int i = 0; i < worldEssentials.Length; i++)
                    {
                        worldEssentials[i] = !allTrue;
                    }
                }
                GUI.enabled = !isVRCCreatorCompanion;
                worldEssentials[0] = GUILayout.Toggle(worldEssentials[0], WENames[0]);
                GUI.enabled = true;
                for (int i = 1; i < worldEssentials.Length; i++)
                {
                    worldEssentials[i] = GUILayout.Toggle(worldEssentials[i], WENames[i]);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
            private void OnDisable()
            {
                EditorPrefs.SetString("avatarEssentials", BoolArr2Str(avatarEssentials));
                EditorPrefs.SetString("worldEssentials", BoolArr2Str(worldEssentials));
            }
            private void OnLostFocus()
            {
                EditorPrefs.SetString("avatarEssentials", BoolArr2Str(avatarEssentials));
                EditorPrefs.SetString("worldEssentials", BoolArr2Str(worldEssentials));
            }
        }
    }
}
