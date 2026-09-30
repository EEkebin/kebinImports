using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports
    {
        private class AIAssistantWindow : EditorWindow
        {
            private AIAgent agent;
            private AISettings settings;
            private string input = "";
            private Vector2 transcriptScroll;
            private bool showSettings;
            private bool allowAllThisSession;
            private List<string> models;
            private Task<List<string>> modelsTask;
            private string modelsError;
            private GUIStyle wrap, titleStyle, toolTitle, box;
            private bool scrollToBottom;
            private bool refocusInput;

            [MenuItem("kebinImports/Ask kebinAI", false, 21)]
            private static void ShowWindow()
            {
                AIAssistantWindow window = GetWindow<AIAssistantWindow>("kebinAI");
                SizeWindow(window, 440, 480, false);
                window.Show();
            }

            private void OnEnable()
            {
                settings = AISettings.Load();
                agent = new AIAgent { Settings = settings, Approve = Approve };
                agent.Changed += OnAgentChanged;
                agent.Restore();
                if (string.IsNullOrEmpty(settings.Model)) showSettings = true;
                EditorApplication.update += OnUpdate;
                scrollToBottom = true;
            }
            private void OnDisable()
            {
                EditorApplication.update -= OnUpdate;
                if (agent != null) { agent.Changed -= OnAgentChanged; agent.Persist(); }
                settings.Save();
            }
            private void OnUpdate()
            {
                agent.Tick();
                if (modelsTask != null && modelsTask.IsCompleted)
                {
                    if (modelsTask.IsFaulted) modelsError = (modelsTask.Exception?.InnerExceptions.Count > 0 ? modelsTask.Exception.InnerExceptions[0] : modelsTask.Exception)?.Message;
                    else { models = modelsTask.Result; modelsError = models.Count == 0 ? "The server returned no models." : null; }
                    modelsTask = null;
                    Repaint();
                }
                if (agent.IsBusy) Repaint();
            }
            private void OnAgentChanged()
            {
                scrollToBottom = true;
                Repaint();
            }

            private bool Approve(AITool tool, AIToolCall call)
            {
                if (!settings.AskBeforeChanges || allowAllThisSession) return true;
                string what = AIActionText.Describe(call);
                int choice = EditorUtility.DisplayDialogComplex("kebinAI", "kebinAI wants to:\n\n" + what + "\n\nYou can undo this afterwards with Ctrl+Z" + (tool.Name == "install_tool" || tool.Name == "remove_tool" || tool.Name == "run_essentials" ? " (installs are undone by removing the tool again)." : "."), "Allow", "Don't allow", "Allow everything this session");
                if (choice == 2) allowAllThisSession = true;
                return choice != 1;
            }

            private void InitStyles()
            {
                if (wrap != null && titleStyle != null) return;
                wrap = new GUIStyle(EditorStyles.label) { wordWrap = true, richText = false };
                titleStyle = new GUIStyle(EditorStyles.boldLabel);
                toolTitle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
                box = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(8, 8, 6, 6) };
            }

            private void OnGUI()
            {
                SizeWindow(this, 440, 480, false);
                logicalWidth = BeginScaledGUI(this).width;
                InitStyles();
                DrawToolbar();
                if (showSettings) DrawSettings();
                DrawTranscript();
                DrawInput();
                EndScaledGUI();
            }
            private float logicalWidth;

            private void DrawToolbar()
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                AIKnownModel known = settings.PresetInfo.Known(settings.Model);
                string modelLabel = string.IsNullOrEmpty(settings.Model) ? "no model" : (known != null ? known.Label : settings.Model);
                GUILayout.Label(settings.PresetInfo.Name + "  ·  " + modelLabel, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("New chat", EditorStyles.toolbarButton)) { if (!agent.IsBusy || EditorUtility.DisplayDialog("kebinAI", "Cancel the running request and start a new chat?", "Yes", "No")) { agent.Clear(); input = ""; } }
                showSettings = GUILayout.Toggle(showSettings, "Settings", EditorStyles.toolbarButton);
                EditorGUILayout.EndHorizontal();
            }

            private void DrawSettings()
            {
                EditorGUILayout.BeginVertical(box);
                EditorGUI.BeginChangeCheck();
                string[] names = new string[AIPresets.Length];
                for (int i = 0; i < names.Length; i++) names[i] = AIPresets[i].Name;
                int preset = EditorGUILayout.Popup("Provider", settings.Preset, names);
                if (EditorGUI.EndChangeCheck() && preset != settings.Preset)
                {
                    settings.Save();
                    settings.Preset = preset;
                    settings.LoadPresetValues();
                    models = null;
                    modelsError = null;
                }
                if (!string.IsNullOrEmpty(settings.PresetInfo.Hint)) EditorGUILayout.HelpBox(settings.PresetInfo.Hint, MessageType.None);
                settings.BaseUrl = EditorGUILayout.TextField("Base URL", settings.BaseUrl);
                if (settings.PresetInfo.NeedsKey || settings.Protocol == AIProtocol.Anthropic || settings.ApiKey.Length > 0 || settings.Preset == AIPresets.Length - 1)
                {
                    settings.ApiKey = EditorGUILayout.PasswordField("API key", settings.ApiKey);
                }
                EditorGUILayout.BeginHorizontal();
                settings.Model = EditorGUILayout.TextField("Model", settings.Model);
                List<string> ids = new List<string>();
                List<string> labels = new List<string>();
                // Known models first (with their Free tags), then whatever the server listed that we did not know about.
                foreach (AIKnownModel km in settings.PresetInfo.KnownModels)
                {
                    ids.Add(km.Id);
                    labels.Add(km.Label + "   (" + km.Id + ")");
                }
                if (models != null)
                {
                    foreach (string id in models)
                    {
                        if (ids.Contains(id)) continue;
                        ids.Add(id);
                        labels.Add(id);
                    }
                }
                if (ids.Count > 0)
                {
                    int current = ids.IndexOf(settings.Model);
                    int picked = EditorGUILayout.Popup(current, labels.ToArray(), GUILayout.Width(22));
                    if (picked >= 0 && picked != current) settings.Model = ids[picked];
                }
                using (new EditorGUI.DisabledScope(modelsTask != null))
                {
                    if (GUILayout.Button(modelsTask != null ? "…" : "Models", GUILayout.Width(60)))
                    {
                        AISettings snapshot = settings.Clone();
                        modelsError = null;
                        modelsTask = Task.Run(() => AIProvider.ListModelsAsync(snapshot, CancellationToken.None));
                    }
                }
                EditorGUILayout.EndHorizontal();
                if (!string.IsNullOrEmpty(modelsError)) EditorGUILayout.HelpBox(modelsError, MessageType.Warning);
                settings.MaxTokens = EditorGUILayout.IntField(new GUIContent("Max tokens", "Longest reply the model may write per turn."), settings.MaxTokens);
                settings.MaxSteps = EditorGUILayout.IntSlider(new GUIContent("Max tool steps", "How many tool calls one message may trigger before the assistant stops and waits for you."), settings.MaxSteps, 1, 100);
                settings.AskBeforeChanges = EditorGUILayout.ToggleLeft(new GUIContent("Ask before the assistant changes anything", "Shows a confirmation for every tool that modifies the scene, assets or files. Reading is never confirmed."), settings.AskBeforeChanges);
                if (allowAllThisSession && GUILayout.Button("Ask again this session")) allowAllThisSession = false;
                EditorGUILayout.LabelField("The API key is stored in EditorPrefs on this machine only.", EditorStyles.miniLabel);
                if (GUILayout.Button("Save settings")) { settings.Save(); showSettings = false; }
                EditorGUILayout.EndVertical();
            }

            private void DrawTranscript()
            {
                transcriptScroll = EditorGUILayout.BeginScrollView(transcriptScroll, GUILayout.ExpandHeight(true));
                float width = logicalWidth - 44;
                if (agent.Transcript.Count == 0)
                {
                    EditorGUILayout.Space(8);
                    EditorGUILayout.LabelField("Ask for anything you would do by hand in the editor. Examples:", wrap);
                    EditorGUILayout.LabelField("• \"Set the pull of every PhysBone on the selected avatar to 0.2\"", wrap);
                    EditorGUILayout.LabelField("• \"Switch all materials under Assets/MyAvatar to lilToon and keep their main textures\"", wrap);
                    EditorGUILayout.LabelField("• \"Write a simple unlit shader that fades by distance and put it on the Wing material\"", wrap);
                    EditorGUILayout.LabelField("• \"Why is my avatar pink? Fix it.\"", wrap);
                }
                foreach (AITranscriptEntry entry in agent.Transcript)
                {
                    EditorGUILayout.BeginVertical(box);
                    Color old = GUI.color;
                    if (entry.Kind == "error") GUI.color = new Color(1f, 0.6f, 0.6f);
                    else if (entry.Kind == "tool") GUI.color = new Color(0.8f, 0.85f, 1f);
                    else if (entry.Kind == "info") GUI.color = new Color(0.85f, 0.85f, 0.85f);
                    EditorGUILayout.BeginHorizontal();
                    if (entry.Kind == "tool" || entry.Kind == "info") EditorGUILayout.LabelField(entry.Title, toolTitle);
                    else EditorGUILayout.LabelField(entry.Title, titleStyle);
                    GUI.color = old;
                    if (entry.Kind == "assistant" && GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.Width(44))) EditorGUIUtility.systemCopyBuffer = entry.Text;
                    EditorGUILayout.EndHorizontal();
                    if (entry.Kind == "tool")
                    {
                        // Actions are a single plain sentence (the title); the technical result is only for kebinAI.
                    }
                    else
                    {
                        float height = wrap.CalcHeight(new GUIContent(entry.Text), width);
                        EditorGUILayout.SelectableLabel(entry.Text, wrap, GUILayout.Height(height), GUILayout.ExpandWidth(true));
                    }
                    EditorGUILayout.EndVertical();
                }
                if (agent.IsBusy)
                {
                    EditorGUILayout.BeginVertical(box);
                    EditorGUILayout.LabelField(agent.Status, EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                }
                if (scrollToBottom && Event.current.type == EventType.Repaint)
                {
                    transcriptScroll.y = float.MaxValue;
                    scrollToBottom = false;
                }
                EditorGUILayout.EndScrollView();
            }

            private void DrawInput()
            {
                // Enter sends; Shift+Enter makes a new line. Unity delivers Enter as two key events (the key, then the
                // newline character), so both are swallowed before the text box sees them.
                Event e = Event.current;
                bool typing = GUI.GetNameOfFocusedControl() == "kebinImports.ai.input";
                if (typing && e.type == EventType.KeyDown && !e.shift && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.character == '\n'))
                {
                    bool isKey = e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter;
                    e.Use();
                    if (isKey && !agent.IsBusy) SendInput();
                }
                EditorGUILayout.BeginVertical(box);
                GUI.SetNextControlName("kebinImports.ai.input");
                input = EditorGUILayout.TextArea(input, GUILayout.MinHeight(56), GUILayout.MaxHeight(140));
                if (refocusInput && Event.current.type == EventType.Repaint)
                {
                    refocusInput = false;
                    EditorGUI.FocusTextInControl("kebinImports.ai.input");
                }
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Enter to send  ·  Shift+Enter for a new line", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (agent.IsBusy)
                {
                    if (GUILayout.Button("Stop", GUILayout.Width(80))) agent.Cancel();
                }
                else
                {
                    using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(input)))
                    {
                        if (GUILayout.Button("Send", GUILayout.Width(80))) SendInput();
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            private void SendInput()
            {
                if (string.IsNullOrWhiteSpace(input) || agent.IsBusy) return;
                settings.Save();
                agent.Settings = settings;
                string text = input;
                input = "";
                // Drop focus so the text box forgets the sent text, then take it back so the next message can be typed right away.
                GUI.FocusControl(null);
                refocusInput = true;
                agent.Send(text);
            }
        }
    }
}
