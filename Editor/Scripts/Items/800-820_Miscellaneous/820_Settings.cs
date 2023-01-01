using System.IO;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports : MonoBehaviour
    {
        private class SettingsWindow : EditorWindow
        {
            private static GUIStyle kebinSplash, discordStyle, versionStyle;
            private static Vector2 scrollPosition;
            [MenuItem("kebinImports/Settings", false, 820)]
            public static void ShowWindow()
            {
                EditorApplication.update -= SettingsWindow.ShowWindow;
                if (EditorApplication.isPlaying) return;
                SettingsWindow window = EditorWindow.GetWindow<SettingsWindow>(true, "kebinImports");
                window.minSize = new Vector2(400, 500);
                window.maxSize = new Vector2(400, 500);
                window.Show();
            }
            private void OnGUI()
            {
                kebinSplash = new GUIStyle
                {
                    normal = { background = Resources.Load<Texture2D>("kebinSplash") },
                    fixedHeight = 200,
                    fixedWidth = 400
                };
                Rect bannerRect = new Rect(0, 0, Screen.width, 200);
                Rect checkBoxesRect = new Rect(5, bannerRect.height + 10, Screen.width, 20);
                Rect changelogRect = new Rect(0, checkBoxesRect.y + checkBoxesRect.height + 10, Screen.width, 240);
                DrawBanner(bannerRect);
                DrawCheckBoxes(checkBoxesRect);
                DrawChangelog(changelogRect);
                DrawVersionInfo();
                this.Repaint();
            }
            private void DrawBanner(Rect bannerRect)
            {
                GUILayout.BeginArea(bannerRect);
                GUILayout.Box("", kebinSplash);
                discordStyle = new GUIStyle(GUI.skin.button);
                discordStyle.hover.textColor = new Color32(114, 137, 218, 255);
                discordStyle.fontSize = 20;
                if (GUI.Button(new Rect(205, 155, 150, 40), "DISCORD", discordStyle))
                {
                    Application.OpenURL(JSON.Parse(File.ReadAllText(installedPath + @"/package.json"))["author"]["discord"]);
                }
                GUILayout.EndArea();
            }
            private void DrawCheckBoxes(Rect checkBoxesRect)
            {
                GUILayout.BeginArea(checkBoxesRect);
                hideWarnings = GUILayout.Toggle(hideWarnings, "Hide Warnings");
                GUILayout.EndArea();
            }
            private void DrawChangelog(Rect changelogRect)
            {
                GUILayout.BeginArea(changelogRect);
                EditorGUILayout.LabelField("Changelog:", EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, true);
                string changelogText = File.ReadAllText(installedPath + @"/CHANGELOG.md");
                changelogText = changelogText.Substring(changelogText.IndexOf("\n") + 1);
                EditorGUILayout.LabelField(changelogText, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndScrollView();
                GUILayout.EndArea();
            }
            private void DrawVersionInfo()
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                versionStyle = new GUIStyle(GUI.skin.label);
                versionStyle.normal.textColor = Color.green;
                EditorGUILayout.LabelField("Installed Version: " + JSON.Parse(File.ReadAllText(installedPath + @"/package.json"))["version"], versionStyle);
                GUILayout.FlexibleSpace();
                showSplash = GUILayout.Toggle(showSplash, "Show at Startup");
                GUILayout.EndHorizontal();
            }
            private void OnLostFocus()
            {
                EditorPrefs.SetBool("kebinImports.hideWarnings", hideWarnings);
                EditorPrefs.SetBool("kebinImports.showSplash", showSplash);
            }
            private void OnDisable()
            {
                EditorPrefs.SetBool("kebinImports.hideWarnings", hideWarnings);
                EditorPrefs.SetBool("kebinImports.showSplash", showSplash);
            }
        }
    }
}
