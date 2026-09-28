using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        private class CustomizeWindow : EditorWindow
        {
            private const float Width = 520, Height = 380;
            private Vector2 avatarScroll, worldScroll;

            [MenuItem("kebinImports/Customize Essentials", false, 2)]
            private static void ShowWindow()
            {
                EditorApplication.update -= CustomizeWindow.ShowWindow;
                CustomizeWindow window = EditorWindow.GetWindow<CustomizeWindow>(true, "kebinImports - Customize Essentials");
                SizeWindow(window, Width, Height, true);
                window.Show();
            }
            private void OnGUI()
            {
                SizeWindow(this, Width, Height, true);
                BeginScaledGUI(this);
                GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 16 };
                GUILayout.BeginHorizontal();
                DrawColumn("Avatar Essentials", headerStyle, AvatarEssentialDefs, avatarEssentials, ref avatarScroll, "vrcsdk-avatars");
                DrawColumn("World Essentials", headerStyle, WorldEssentialDefs, worldEssentials, ref worldScroll, "vrcsdk-worlds");
                GUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Essentials install in this order. The VRChat SDK is managed by the Creator Companion in its projects.", EditorStyles.miniLabel);
                EndScaledGUI();
            }
            private void DrawColumn(string title, GUIStyle headerStyle, Essential[] defs, bool[] enabled, ref Vector2 scroll, string sdkKey)
            {
                GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width((Width - 24) / 2f), GUILayout.ExpandHeight(true));
                GUILayout.Label(title, headerStyle);
                if (GUILayout.Button("Toggle All"))
                {
                    bool allTrue = true;
                    for (int i = 0; i < enabled.Length; i++) if (!enabled[i]) { allTrue = false; break; }
                    for (int i = 0; i < enabled.Length; i++) enabled[i] = !allTrue;
                }
                scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
                string group = null;
                for (int i = 0; i < defs.Length; i++)
                {
                    if (defs[i].Group != group)
                    {
                        group = defs[i].Group;
                        if (i > 0) GUILayout.Space(6);
                        GUILayout.Label(group, EditorStyles.miniBoldLabel);
                    }
                    bool sdk = defs[i].Key == sdkKey;
                    using (new EditorGUI.DisabledScope(sdk && isVRCCreatorCompanion))
                    {
                        Tool t = ToolCatalog.Get(defs[i].Key);
                        string label = defs[i].Name + (t != null && t.Paid ? "  (paid)" : "");
                        enabled[i] = GUILayout.Toggle(enabled[i], label);
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            private void OnDisable() { SaveEssentials(); }
            private void OnLostFocus() { SaveEssentials(); }
        }
    }
}
