using System.IO;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

/* 
    This controls kebinImports Settings Window
*/

public partial class kebinImports : MonoBehaviour
{
    private class settingsWindow : UnityEditor.EditorWindow
    {

        private static GUIStyle kebinSplash, discordStyle, versionStyle;
        private static Vector2 scrollPosition;

        [MenuItem("kebinImports/Settings", false, 820)]
        public static void showWindow()
        {
            EditorApplication.update -= settingsWindow.showWindow;
            if (EditorApplication.isPlaying) return;
            settingsWindow window = (settingsWindow)EditorWindow.GetWindow(typeof(settingsWindow), true, "kebinImports");
            window.minSize = new Vector2(400, 500);
            window.maxSize = new Vector2(400, 500);
            window.Show();
        }
        void OnGUI()
        {
            kebinSplash = new GUIStyle
            {
                normal = {
                    background = Resources.Load("kebinSplash") as Texture2D,
                    textColor = Color.white
                },
                fixedHeight = 200
            };
            Rect banner = new Rect(0, 0, Screen.width, 200);
            Rect checkBoxes = new Rect(0, banner.height + 10, Screen.width, 20);
            Rect changelog = new Rect(0, checkBoxes.y + checkBoxes.height + 10, Screen.width, 240);

            GUILayout.BeginArea(banner);
            GUILayout.Box("", kebinSplash);
            discordStyle = new GUIStyle(GUI.skin.button);
            discordStyle.normal.textColor = Color.white;
            discordStyle.hover.textColor = new Color32(115, 138, 219, 255);
            discordStyle.fontSize = 20;
            GUI.backgroundColor = Color.gray;
            if (GUI.Button(new Rect(210, 155, 150, 40), "DISCORD", discordStyle))
                Application.OpenURL(JSON.Parse(File.ReadAllText(Application.dataPath + @"/kebinImports/Editor/Scripts/info.json"))["discord"]);
            GUILayout.EndArea();

            GUILayout.BeginArea(checkBoxes);
            hideWarnings = EditorGUILayout.Toggle("Hide Warnings: ", hideWarnings);
            GUILayout.EndArea();

            GUILayout.BeginArea(changelog);
            GUILayout.Label("Changelog:");
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, true);
            EditorGUILayout.LabelField(File.ReadAllText(Application.dataPath + @"/kebinImports/CHANGELOG"), EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            versionStyle = new GUIStyle(GUI.skin.label);
            versionStyle.normal.textColor = Color.green;
            GUILayout.Label("Installed Version: " + JSON.Parse(File.ReadAllText(Application.dataPath + @"/kebinImports/Editor/Scripts/info.json"))["version"], versionStyle);
            GUILayout.FlexibleSpace();
            showSplash = GUILayout.Toggle(showSplash, "Show at Startup");
            GUILayout.EndHorizontal();
        }

        void OnLostFocus()
        {
            EditorPrefs.SetBool("kebinImports.hideWarnings", hideWarnings);
            EditorPrefs.SetBool("kebinImports.showSplash", showSplash);
        }

        void OnDestroy()
        {
            EditorPrefs.SetBool("kebinImports.hideWarnings", hideWarnings);
            EditorPrefs.SetBool("kebinImports.showSplash", showSplash);
        }
    }
}
