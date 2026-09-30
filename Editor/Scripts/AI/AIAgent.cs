using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports
    {
        internal class AITranscriptEntry
        {
            public string Kind; // user | assistant | tool | error | info
            public string Title;
            public string Text;
        }

        // The agent loop: send the conversation, run whatever tools the model asks for on the main thread, send the
        // results back, repeat until the model answers in plain text or the step limit is hit. HTTP happens on a
        // worker task; Tick() is driven from EditorApplication.update by the window (or a blocking loop in tests).
        internal class AIAgent
        {
            public readonly List<AIMessage> History = new List<AIMessage>();
            public readonly List<AITranscriptEntry> Transcript = new List<AITranscriptEntry>();
            public AISettings Settings;
            public Func<AITool, AIToolCall, bool> Approve;
            public bool IsBusy { get; private set; }
            public string Status { get; private set; } = "";
            public event Action Changed;

            private Task<AIMessage> pending;
            private CancellationTokenSource cts;
            private int steps;
            private const string SessionKey = "kebinImports.ai.session";

            public void Send(string userText)
            {
                if (IsBusy || string.IsNullOrWhiteSpace(userText)) return;
                History.Add(new AIMessage { Role = "user", Text = userText.Trim() });
                Transcript.Add(new AITranscriptEntry { Kind = "user", Title = "You", Text = userText.Trim() });
                steps = 0;
                IsBusy = true;
                StartRequest();
            }

            private void StartRequest()
            {
                cts = new CancellationTokenSource();
                CancellationToken token = cts.Token;
                AISettings settings = Settings.Clone();
                List<AIMessage> history = History.ToList();
                string system = BuildSystemPrompt();
                List<AITool> tools = AITools.All;
                Status = "Waiting for " + settings.Model + "…";
                pending = Task.Run(() => AIProvider.CompleteAsync(settings, system, history, tools, token), token);
                Changed?.Invoke();
            }

            public void Tick()
            {
                if (pending == null || !pending.IsCompleted) return;
                Task<AIMessage> task = pending;
                pending = null;
                if (task.IsCanceled) { Finish(); return; }
                if (task.IsFaulted)
                {
                    Exception e = task.Exception != null ? task.Exception.InnerExceptions.FirstOrDefault() ?? task.Exception : null;
                    Transcript.Add(new AITranscriptEntry { Kind = "error", Title = "Something went wrong", Text = e != null ? e.Message : "kebinAI couldn't get an answer. Please try again." });
                    Finish();
                    return;
                }
                AIMessage reply = task.Result;
                History.Add(reply);
                if (!string.IsNullOrWhiteSpace(reply.Text)) Transcript.Add(new AITranscriptEntry { Kind = "assistant", Title = "kebinAI", Text = PlainText(reply.Text) });
                if (reply.ToolCalls.Count == 0) { Finish(); return; }
                if (steps >= Settings.MaxSteps)
                {
                    Transcript.Add(new AITranscriptEntry { Kind = "error", Title = "Paused", Text = "kebinAI has done " + Settings.MaxSteps + " steps for this message and paused to check in. Send another message to let it continue." });
                    // Tell the model why its calls were not run, so the history stays consistent.
                    foreach (AIToolCall call in reply.ToolCalls) History.Add(new AIMessage { Role = "tool", ToolCallId = call.Id, ToolName = call.Name, Text = "Not run: the step limit was reached.", IsError = true });
                    Finish();
                    return;
                }
                foreach (AIToolCall call in reply.ToolCalls)
                {
                    Status = AIActionText.Describe(call).TrimEnd('.') + "…";
                    Changed?.Invoke();
                    bool isError;
                    string result = AITools.Execute(call, Approve, out isError);
                    History.Add(new AIMessage { Role = "tool", ToolCallId = call.Id, ToolName = call.Name, Text = result, IsError = isError });
                    Transcript.Add(new AITranscriptEntry
                    {
                        Kind = isError ? "info" : "tool",
                        Title = isError ? AIActionText.Failed(call) : AIActionText.Done(call),
                        Text = ""
                    });
                    steps++;
                }
                StartRequest();
            }

            private void Finish()
            {
                IsBusy = false;
                Status = "";
                Persist();
                Changed?.Invoke();
            }

            public void Cancel()
            {
                if (!IsBusy) return;
                cts?.Cancel();
                pending = null;
                Transcript.Add(new AITranscriptEntry { Kind = "info", Title = "Stopped", Text = "You stopped kebinAI." });
                // Leave the history consistent: any assistant turn with unanswered tool calls gets error results.
                AIMessage last = History.LastOrDefault();
                if (last != null && last.Role == "assistant" && last.ToolCalls.Count > 0 && !History.Any(m => m.Role == "tool" && m.ToolCallId == last.ToolCalls[0].Id))
                {
                    foreach (AIToolCall call in last.ToolCalls) History.Add(new AIMessage { Role = "tool", ToolCallId = call.Id, ToolName = call.Name, Text = "Cancelled by the user.", IsError = true });
                }
                Finish();
            }

            public void Clear()
            {
                Cancel();
                History.Clear();
                Transcript.Clear();
                Persist();
                Changed?.Invoke();
            }

            // Runs a whole exchange synchronously. Used by headless tests; the window uses Tick() from the editor loop.
            public void RunBlocking(string userText, int timeoutSeconds)
            {
                Send(userText);
                Stopwatch sw = Stopwatch.StartNew();
                while (IsBusy && sw.Elapsed.TotalSeconds < timeoutSeconds)
                {
                    Task<AIMessage> p = pending;
                    if (p != null) { try { p.Wait(500); } catch (AggregateException) { } }
                    Tick();
                }
                if (IsBusy) { Cancel(); Transcript.Add(new AITranscriptEntry { Kind = "error", Title = "Timeout", Text = "No answer within " + timeoutSeconds + " seconds." }); }
            }

            // Models write Markdown even when asked not to; the window shows plain text, so drop the markup.
            private static string PlainText(string text)
            {
                string t = text.Trim();
                t = System.Text.RegularExpressions.Regex.Replace(t, @"(\*\*|__)(.+?)\1", "$2");
                t = System.Text.RegularExpressions.Regex.Replace(t, @"`([^`\n]+)`", "$1");
                t = System.Text.RegularExpressions.Regex.Replace(t, @"(?m)^#{1,6}\s+", "");
                t = System.Text.RegularExpressions.Regex.Replace(t, @"(?m)^(\s*)[-*]\s+", "$1• ");
                return t;
            }

            // ---------------------------------------------------------------- prompt
            private string BuildSystemPrompt()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("You are kebinAI, the AI built into kebinImports, working inside the Unity Editor for a VRChat avatar/world creator. You can inspect and change the open scene, materials, shaders, components (including VRChat SDK components such as VRCPhysBone) and project files through the tools you are given.");
                sb.AppendLine();
                sb.AppendLine("How to work:");
                sb.AppendLine("- Look before you change. Read a component's properties or a material's properties first so you use real property paths, enum names and value shapes. Never guess shader property names.");
                sb.AppendLine("- Object references can be an asset path (Assets/... or Packages/...) or a scene path (Root/Child/Grandchild). Scene paths come from get_hierarchy, find_objects or get_selection.");
                sb.AppendLine("- Prefer small, targeted changes. When something is ambiguous or destructive, ask the user instead of guessing.");
                sb.AppendLine("- After editing a shader or script, check get_shader_errors or get_console_log and fix problems you caused.");
                sb.AppendLine("- Every change is recorded in Unity's undo history; the user can press Ctrl+Z.");
                sb.AppendLine("- The person you are talking to is a VRChat creator, not a programmer. Always answer in plain, friendly English. Never show JSON, code, tool names, file GUIDs, property paths such as m_LocalPosition, or package ids such as jp.lilxyzw.liltoon; use the names people see in Unity (Local Position, lilToon). Only show code if they ask for it.");
                sb.AppendLine("- When you are done, say what you changed in a few short sentences. Plain text, no markdown tables.");
                sb.AppendLine("- Installing a tool or clearing define symbols makes Unity recompile and reload scripts, which ends this conversation turn. Do those last, then tell the user what to do next.");
                sb.AppendLine("- Do what the user asks with the tools; don't tell them to do it by hand unless no tool can.");
                sb.AppendLine();
                sb.AppendLine("VRChat avatars:");
                sb.AppendLine("- When the user talks about their avatar, call get_avatar_info first. An avatar's root is its top object (it has the Animator); the VRChat avatar descriptor (VRCAvatarDescriptor) and pipeline manager go on that root, never on a mesh or bone.");
                sb.AppendLine("- Before adding a PhysBone, check whether that part already has one (get_avatar_info lists them, get_mesh_bones says ALREADY moved). If it does, tell the user and offer to change its settings instead of adding another.");
                sb.AppendLine("- A part whose mesh only follows the humanoid skeleton has no bones of its own and can't jiggle; bones have to be added and weight-painted in a 3D program such as Blender. Say so instead of creating empty bones.");
                sb.AppendLine("- PhysBones (VRCPhysBone) make hair, ears, tails, skirts, breasts and clothes move. They go on the first bone of that part's bone chain in the Armature, not on the mesh. Find the bones with get_mesh_bones on the part's mesh, or find_objects by bone name. Left/right bones are usually named with _L/_R, .L/.R or Left/Right; breast bones are often called Breast, Bust or Chest_L/R.");
                sb.AppendLine("- Useful PhysBone settings: pull, spring (momentum), stiffness, gravity (-1 to 1, positive pulls down), gravityFalloff, immobile, radius, maxAngleX, colliders, rootTransform.");
                sb.AppendLine("- To make an avatar bigger or smaller, use set_transform with scale_by on the avatar root; it keeps the view position at the eyes.");
                sb.AppendLine("- To put a material on a mesh use assign_material (it keeps the mesh's other material slots). Face expressions and body shapes are blendshapes: get_blendshapes without an object searches every mesh.");
                sb.AppendLine("- Other VRChat components: VRCPhysBoneCollider, VRCContactSender, VRCContactReceiver, VRCParentConstraint and friends. find_component_types lists every addable component.");
                sb.AppendLine();
                sb.Append(AIKebinTools.DescribeForPrompt());
                sb.AppendLine();
                sb.AppendLine("Current context:");
                if (ProjectDoctor.Last != null)
                {
                    List<string> found = ProjectDoctor.Last.Findings.Select(f => "[" + f.Id + "] " + f.Title).ToList();
                    sb.AppendLine("- The Project Doctor last checked the project at " + ProjectDoctor.Last.Time.ToString("HH:mm") + (found.Count == 0 ? " and found nothing wrong." : " and found: " + string.Join("; ", found.Take(12)) + ". Run it again for current details."));
                }
                sb.AppendLine("- Unity " + Application.unityVersion + ", project '" + System.IO.Path.GetFileName(ProjectPath) + "', " + (isVRCCreatorCompanion ? "a VRChat Creator Companion project" : "not a Creator Companion project") + ".");
                string scenes = string.Join(", ", Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i).name).Where(n => !string.IsNullOrEmpty(n)));
                sb.AppendLine("- Open scene(s): " + (scenes.Length > 0 ? scenes : "(untitled)") + ".");
                sb.AppendLine("- Avatars in the scene: " + AITools.DescribeAvatarsForPrompt() + ".");
                string selection = string.Join(", ", Selection.gameObjects.Take(8).Select(g => AITools.PathOf(g.transform)));
                if (Selection.objects.Length > 0 && selection.Length == 0) selection = string.Join(", ", Selection.objects.Take(8).Select(o => AssetDatabase.GetAssetPath(o)).Where(p => !string.IsNullOrEmpty(p)));
                sb.AppendLine("- Selected: " + (selection.Length > 0 ? selection : "nothing") + ".");
                return sb.ToString();
            }

            // ---------------------------------------------------------------- persistence across domain reloads
            public void Persist()
            {
                JSONObject o = new JSONObject();
                JSONArray h = new JSONArray();
                foreach (AIMessage m in History) h.Add(m.ToJson());
                o["history"] = h;
                JSONArray t = new JSONArray();
                foreach (AITranscriptEntry e in Transcript)
                {
                    JSONObject je = new JSONObject();
                    je["kind"] = e.Kind;
                    je["title"] = e.Title ?? "";
                    je["text"] = e.Text ?? "";
                    t.Add(je);
                }
                o["transcript"] = t;
                SessionState.SetString(SessionKey, o.ToString());
            }
            public void Restore()
            {
                string json = SessionState.GetString(SessionKey, "");
                if (string.IsNullOrEmpty(json)) return;
                JSONNode o = JSON.Parse(json);
                if (o == null) return;
                History.Clear();
                Transcript.Clear();
                foreach (JSONNode m in o["history"].Children) History.Add(AIMessage.FromJson(m));
                foreach (JSONNode e in o["transcript"].Children) Transcript.Add(new AITranscriptEntry { Kind = e["kind"].Value, Title = e["title"].Value, Text = e["text"].Value });
                // A reload in the middle of a tool step (write_file that recompiled) leaves dangling tool calls; answer them.
                AIMessage last = History.LastOrDefault();
                if (last != null && last.Role == "assistant" && last.ToolCalls.Count > 0)
                {
                    foreach (AIToolCall call in last.ToolCalls) History.Add(new AIMessage { Role = "tool", ToolCallId = call.Id, ToolName = call.Name, Text = "The editor reloaded scripts before this result could be delivered. Check the state and continue.", IsError = true });
                    Transcript.Add(new AITranscriptEntry { Kind = "info", Title = "Unity reloaded", Text = "Unity reloaded its scripts while kebinAI was working. Send a message to let it continue." });
                }
            }
        }
    }
}
