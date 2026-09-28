using System.IO;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports
    {
        private class SettingsWindow : EditorWindow
        {
            private static GUIStyle kebinSplash, discordStyle, versionStyle;
            private static Vector2 scrollPosition;
            // Read once per window instead of on every repaint (OnGUI runs many times per second while the window is open).
            private Texture2D splashTexture;
            private string discordUrl = "", installedVersion = "", changelogText = "";
            private void OnEnable()
            {
                splashTexture = Resources.Load<Texture2D>("kebinSplash");
                try
                {
                    JSONNode package = JSON.Parse(File.ReadAllText(Path.Combine(installedPath, "package.json")));
                    discordUrl = package["author"]["discord"].Value;
                    installedVersion = package["version"].Value;
                    changelogText = File.ReadAllText(Path.Combine(installedPath, "CHANGELOG.md"));
                    changelogText = changelogText.Substring(changelogText.IndexOf("\n") + 1);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[kebinImports] Could not read package files from " + installedPath + ": " + e.Message);
                }
            }
            [MenuItem("kebinImports/Settings", false, 800)]
            public static void ShowWindow()
            {
                EditorApplication.update -= SettingsWindow.ShowWindow;
                if (EditorApplication.isPlaying) return;
                SettingsWindow window = EditorWindow.GetWindow<SettingsWindow>(true, "kebinImports");
                SizeWindow(window, 400, 500, true);
                window.Show();
            }
            private void OnGUI()
            {
                SizeWindow(this, 400, 500, true);
                Rect logical = BeginScaledGUI(this);
                kebinSplash = new GUIStyle
                {
                    normal = { background = splashTexture },
                    fixedHeight = 200,
                    fixedWidth = 400
                };
                Rect bannerRect = new Rect(0, 0, logical.width, 200);
                Rect checkBoxesRect = new Rect(5, bannerRect.height + 8, logical.width - 10, 22);
                Rect scaleRect = new Rect(5, checkBoxesRect.yMax + 4, logical.width - 10, 22);
                Rect changelogRect = new Rect(0, scaleRect.yMax + 6, logical.width, 210);
                DrawBanner(bannerRect);
                DrawCheckBoxes(checkBoxesRect);
                DrawUiScale(scaleRect);
                DrawChangelog(changelogRect);
                DrawVersionInfo();
                EndScaledGUI();
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
                    if (!string.IsNullOrEmpty(discordUrl)) Application.OpenURL(discordUrl);
                }
                GUILayout.EndArea();
            }
            private void DrawCheckBoxes(Rect checkBoxesRect)
            {
                GUILayout.BeginArea(checkBoxesRect);
                GUILayout.BeginHorizontal();
                hideWarnings = GUILayout.Toggle(hideWarnings, "Hide Warnings");
                showLegacy = GUILayout.Toggle(showLegacy, new GUIContent("Show legacy items", "Enables the outdated tools under kebinImports > Legacy."));
                GUILayout.FlexibleSpace();
                if (UpdateChecker.Available.Count > 0)
                {
                    GUIStyle updates = new GUIStyle(GUI.skin.button);
                    updates.normal.textColor = new Color32(255, 200, 80, 255);
                    if (GUILayout.Button(UpdateChecker.Available.Count + " update" + (UpdateChecker.Available.Count == 1 ? "" : "s") + " available", updates))
                    {
                        EditorApplication.ExecuteMenuItem("kebinImports/Project Doctor");
                    }
                }
                else if (GUILayout.Button(UpdateChecker.Checking ? "Checking…" : "Project Doctor"))
                {
                    EditorApplication.ExecuteMenuItem("kebinImports/Project Doctor");
                }
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
            private void DrawUiScale(Rect rect)
            {
                GUILayout.BeginArea(rect);
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("UI scale", "Size of every kebinImports window and its text. Auto picks a value from your monitor's resolution. The Unity menu bar follows Windows display scaling or Edit > Preferences > UI Scaling instead."), GUILayout.Width(56));
                bool auto = uiScaleSetting <= 0f;
                bool newAuto = GUILayout.Toggle(auto, "Auto", GUILayout.Width(52));
                if (newAuto != auto) SetUiScale(newAuto ? 0f : UiScale);
                using (new EditorGUI.DisabledScope(newAuto))
                {
                    float shown = newAuto ? AutoUiScale() : uiScaleSetting;
                    float picked = GUILayout.HorizontalSlider(shown, 1f, 2.5f);
                    picked = Mathf.Round(picked * 20f) / 20f;
                    if (!newAuto && Mathf.Abs(picked - uiScaleSetting) > 0.01f) SetUiScale(picked);
                    GUILayout.Label(Mathf.RoundToInt(shown * 100f) + "%", GUILayout.Width(40));
                }
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
            private void DrawChangelog(Rect changelogRect)
            {
                GUILayout.BeginArea(changelogRect);
                EditorGUILayout.LabelField("Changelog:", EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, true);
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
                EditorGUILayout.LabelField("Installed Version: " + installedVersion, versionStyle);
                GUILayout.FlexibleSpace();
                showSplash = GUILayout.Toggle(showSplash, "Show at Startup");
                GUILayout.EndHorizontal();
            }
            private void OnLostFocus() { SavePrefs(); }
            private void OnDisable() { SavePrefs(); }
            private static void SavePrefs()
            {
                EditorPrefs.SetBool("kebinImports.hideWarnings", hideWarnings);
                EditorPrefs.SetBool("kebinImports.showSplash", showSplash);
                EditorPrefs.SetBool("kebinImports.showLegacy", showLegacy);
            }
        }
    }
}
