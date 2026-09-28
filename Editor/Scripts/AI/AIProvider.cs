using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace kebinImports
{
    public partial class kebinImports
    {
        // Two wire protocols cover every backend we care about:
        //  - OpenAI-compatible chat completions: Ollama, LM Studio, llama.cpp server, OpenCode Zen, OpenAI, vLLM, ...
        //  - Anthropic Messages API: Claude, called directly (Unity's Mono runtime cannot load the NuGet SDK).
        internal enum AIProtocol { OpenAICompatible, Anthropic }

        // A model we know about up front, so it can be picked from the dropdown before (or without) listing the server.
        internal class AIKnownModel
        {
            public string Id;
            public string Label;
            public bool Free;
            public AIKnownModel(string id, string label, bool free = false) { Id = id; Label = label; Free = free; }
        }

        internal class AIPreset
        {
            public string Name;
            public AIProtocol Protocol = AIProtocol.OpenAICompatible;
            public string BaseUrl;
            public string DefaultModel = "";
            public bool NeedsKey;
            public string Hint = "";
            public AIKnownModel[] KnownModels = new AIKnownModel[0];

            public AIKnownModel Known(string id) => Array.Find(KnownModels, m => m.Id == id);
        }

        internal static readonly AIPreset[] AIPresets =
        {
            new AIPreset { Name = "Ollama (local)", BaseUrl = "http://localhost:11434/v1", Hint = "Run 'ollama serve', then pull a model that supports tool calling (for example 'ollama pull qwen3'). Click Models to list what is installed." },
            new AIPreset { Name = "LM Studio (local)", BaseUrl = "http://localhost:1234/v1", Hint = "Start the server on LM Studio's Developer tab and load a model that supports tool calling." },
            new AIPreset { Name = "llama.cpp server (local)", BaseUrl = "http://localhost:8080/v1", Hint = "Start llama-server with --jinja so the model can call tools." },
            new AIPreset
            {
                Name = "OpenCode Zen", BaseUrl = "https://opencode.ai/zen/v1", DefaultModel = "big-pickle", NeedsKey = true,
                Hint = "Sign in at opencode.ai/zen and copy your API key. The models marked Free cost nothing; the rest bill your Zen balance.",
                // The free tier as OpenCode lists it. Click Models to see everything Zen offers.
                KnownModels = new[]
                {
                    new AIKnownModel("big-pickle", "Big Pickle", true),
                    new AIKnownModel("space-bunny-free", "Space Bunny", true),
                    new AIKnownModel("mimo-v2.6-flash-free", "MiMo-V2.6-Flash", true),
                    new AIKnownModel("mimo-v2.5-free", "MiMo-V2.5", true),
                    new AIKnownModel("muse-spark-1.3-contributor-free", "Muse Spark 1.3", true),
                    new AIKnownModel("ling-3.0-flash-fin-free", "Ling 3.0 Flash Fin", true),
                    new AIKnownModel("nemotron-3.5-lightning-free", "Nemotron 3.5 Lightning", true),
                    new AIKnownModel("nemotron-3-ultra-free", "Nemotron 3 Ultra", true),
                    new AIKnownModel("deepseek-v4-flash-free", "DeepSeek V4 Flash", true),
                    new AIKnownModel("claude-opus-5-5", "Claude Opus 5.5"),
                    new AIKnownModel("claude-sonnet-5", "Claude Sonnet 5"),
                    new AIKnownModel("gpt-5.5", "GPT-5.5"),
                },
            },
            new AIPreset { Name = "OpenAI", BaseUrl = "https://api.openai.com/v1", NeedsKey = true, Hint = "Any chat model that supports function calling. Click Models to list them." },
            new AIPreset
            {
                Name = "Anthropic (Claude)", Protocol = AIProtocol.Anthropic, BaseUrl = "https://api.anthropic.com", DefaultModel = "claude-opus-5", NeedsKey = true,
                Hint = "API key from console.anthropic.com.",
                KnownModels = new[]
                {
                    new AIKnownModel("claude-opus-5", "Claude Opus 5"),
                    new AIKnownModel("claude-sonnet-5", "Claude Sonnet 5"),
                    new AIKnownModel("claude-haiku-4-5", "Claude Haiku 4.5"),
                    new AIKnownModel("claude-fable-5-1", "Claude Fable 5.1"),
                },
            },
            new AIPreset { Name = "Custom (OpenAI-compatible)", BaseUrl = "http://localhost:8000/v1", Hint = "Any server that implements POST /chat/completions with tools (vLLM, text-generation-webui, Jan, ...)." },
        };

        internal class AISettings
        {
            private const string Prefix = "kebinImports.ai.";
            public int Preset;
            public string BaseUrl = "";
            public string ApiKey = "";
            public string Model = "";
            public bool AskBeforeChanges = true;
            public int MaxSteps = 30;
            public int MaxTokens = 8192;

            public AIPreset PresetInfo => AIPresets[Mathf.Clamp(Preset, 0, AIPresets.Length - 1)];
            public AIProtocol Protocol => PresetInfo.Protocol;

            public static AISettings Load()
            {
                AISettings s = new AISettings();
                s.Preset = Mathf.Clamp(EditorPrefs.GetInt(Prefix + "preset", 0), 0, AIPresets.Length - 1);
                s.AskBeforeChanges = EditorPrefs.GetBool(Prefix + "askBeforeChanges", true);
                s.MaxSteps = EditorPrefs.GetInt(Prefix + "maxSteps", 30);
                s.MaxTokens = EditorPrefs.GetInt(Prefix + "maxTokens", 8192);
                EditorPrefs.DeleteKey(Prefix + "customInstructions"); // setting removed in 2026.9.26
                s.LoadPresetValues();
                return s;
            }
            // Base URL, key and model are remembered per preset so switching back and forth loses nothing.
            public void LoadPresetValues()
            {
                string p = Prefix + Preset + ".";
                BaseUrl = EditorPrefs.GetString(p + "baseUrl", PresetInfo.BaseUrl);
                ApiKey = Unprotect(EditorPrefs.GetString(p + "apiKey", ""));
                Model = EditorPrefs.GetString(p + "model", PresetInfo.DefaultModel);
            }
            public void Save()
            {
                EditorPrefs.SetInt(Prefix + "preset", Preset);
                EditorPrefs.SetBool(Prefix + "askBeforeChanges", AskBeforeChanges);
                EditorPrefs.SetInt(Prefix + "maxSteps", MaxSteps);
                EditorPrefs.SetInt(Prefix + "maxTokens", MaxTokens);
                string p = Prefix + Preset + ".";
                EditorPrefs.SetString(p + "baseUrl", BaseUrl ?? "");
                EditorPrefs.SetString(p + "apiKey", Protect(ApiKey ?? ""));
                EditorPrefs.SetString(p + "model", Model ?? "");
            }
            public AISettings Clone() => (AISettings)MemberwiseClone();

            // API keys are stored encrypted with the Windows Data Protection API, bound to the current Windows user, so
            // the registry never holds them in plain text. Elsewhere (or if decryption fails) the value is stored as is.
            private const string ProtectedPrefix = "dpapi:";
            private static string Protect(string value)
            {
                if (string.IsNullOrEmpty(value)) return "";
#if UNITY_EDITOR_WIN
                try
                {
                    byte[] data = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);
                    return ProtectedPrefix + Convert.ToBase64String(data);
                }
                catch (Exception) { }
#endif
                return value;
            }
            private static string Unprotect(string stored)
            {
                if (string.IsNullOrEmpty(stored) || !stored.StartsWith(ProtectedPrefix)) return stored ?? "";
                try
                {
                    byte[] data = ProtectedData.Unprotect(Convert.FromBase64String(stored.Substring(ProtectedPrefix.Length)), null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(data);
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        internal class AIToolCall
        {
            public string Id;
            public string Name;
            public JSONNode Arguments;
        }

        // Provider-neutral conversation entry. Role is "user", "assistant" or "tool".
        internal class AIMessage
        {
            public string Role;
            public string Text = "";
            public List<AIToolCall> ToolCalls = new List<AIToolCall>();
            public string ToolCallId;
            public string ToolName;
            public bool IsError;
            public string StopReason;
            // Anthropic assistant content is replayed verbatim so thinking blocks stay attached to their turn.
            public JSONNode RawContent;

            public JSONNode ToJson()
            {
                JSONObject o = new JSONObject();
                o["role"] = Role;
                o["text"] = Text ?? "";
                if (ToolCalls.Count > 0)
                {
                    JSONArray calls = new JSONArray();
                    foreach (AIToolCall c in ToolCalls)
                    {
                        JSONObject jc = new JSONObject();
                        jc["id"] = c.Id;
                        jc["name"] = c.Name;
                        jc["arguments"] = c.Arguments ?? new JSONObject();
                        calls.Add(jc);
                    }
                    o["toolCalls"] = calls;
                }
                if (ToolCallId != null) o["toolCallId"] = ToolCallId;
                if (ToolName != null) o["toolName"] = ToolName;
                if (IsError) o["isError"] = true;
                if (StopReason != null) o["stopReason"] = StopReason;
                if (RawContent != null) o["rawContent"] = RawContent;
                return o;
            }
            public static AIMessage FromJson(JSONNode o)
            {
                AIMessage m = new AIMessage { Role = o["role"].Value, Text = o["text"].Value };
                if (o.HasKey("toolCalls"))
                {
                    foreach (JSONNode jc in o["toolCalls"].Children)
                    {
                        m.ToolCalls.Add(new AIToolCall { Id = jc["id"].Value, Name = jc["name"].Value, Arguments = jc["arguments"] });
                    }
                }
                if (o.HasKey("toolCallId")) m.ToolCallId = o["toolCallId"].Value;
                if (o.HasKey("toolName")) m.ToolName = o["toolName"].Value;
                m.IsError = o["isError"].AsBool;
                if (o.HasKey("stopReason")) m.StopReason = o["stopReason"].Value;
                if (o.HasKey("rawContent")) m.RawContent = o["rawContent"];
                return m;
            }
        }

        // Every await uses ConfigureAwait(false): callers may block on these tasks from the editor main thread, and
        // Unity installs a SynchronizationContext there that would otherwise deadlock the continuation.
        internal static class AIProvider
        {
            private static readonly System.Net.Http.HttpClient http;

            static AIProvider()
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                http.DefaultRequestHeaders.Add("User-Agent", "kebinImports-AI (+https://github.com/EEkebin/kebinImports)");
            }

            public static Task<AIMessage> CompleteAsync(AISettings settings, string system, List<AIMessage> history, List<AITool> tools, CancellationToken ct)
            {
                if (string.IsNullOrWhiteSpace(settings.Model)) throw new InvalidOperationException("No model selected. Open the settings (gear) and pick a model.");
                if (settings.PresetInfo.NeedsKey && string.IsNullOrWhiteSpace(settings.ApiKey)) throw new InvalidOperationException(settings.PresetInfo.Name + " needs an API key. Open the settings (gear) and enter it.");
                return settings.Protocol == AIProtocol.Anthropic
                    ? CompleteAnthropicAsync(settings, system, history, tools, ct)
                    : CompleteOpenAIAsync(settings, system, history, tools, ct);
            }

            public static async Task<List<string>> ListModelsAsync(AISettings settings, CancellationToken ct)
            {
                string url = settings.Protocol == AIProtocol.Anthropic
                    ? Endpoint(settings.BaseUrl, "/v1/models", "/v1")
                    : Endpoint(settings.BaseUrl, "/models", null);
                JSONNode node = await SendAsync(settings, HttpMethod.Get, url, null, ct).ConfigureAwait(false);
                List<string> models = new List<string>();
                JSONNode data = node.HasKey("data") ? node["data"] : (node.HasKey("models") ? node["models"] : node);
                foreach (JSONNode m in data.Children)
                {
                    string id = m.HasKey("id") ? m["id"].Value : (m.HasKey("name") ? m["name"].Value : m.Value);
                    if (!string.IsNullOrEmpty(id)) models.Add(id);
                }
                models.Sort(StringComparer.OrdinalIgnoreCase);
                return models;
            }

            // ---------------------------------------------------------------- OpenAI-compatible chat completions
            private static async Task<AIMessage> CompleteOpenAIAsync(AISettings settings, string system, List<AIMessage> history, List<AITool> tools, CancellationToken ct)
            {
                JSONObject body = new JSONObject();
                body["model"] = settings.Model;
                body["stream"] = false;
                JSONArray messages = new JSONArray();
                JSONObject sys = new JSONObject();
                sys["role"] = "system";
                sys["content"] = system;
                messages.Add(sys);
                foreach (AIMessage m in history)
                {
                    JSONObject jm = new JSONObject();
                    switch (m.Role)
                    {
                        case "user":
                            jm["role"] = "user";
                            jm["content"] = m.Text ?? "";
                            break;
                        case "assistant":
                            jm["role"] = "assistant";
                            jm["content"] = m.Text ?? "";
                            if (m.ToolCalls.Count > 0)
                            {
                                JSONArray calls = new JSONArray();
                                foreach (AIToolCall c in m.ToolCalls)
                                {
                                    JSONObject jc = new JSONObject();
                                    jc["id"] = c.Id;
                                    jc["type"] = "function";
                                    JSONObject fn = new JSONObject();
                                    fn["name"] = c.Name;
                                    fn["arguments"] = (c.Arguments ?? new JSONObject()).ToString();
                                    jc["function"] = fn;
                                    calls.Add(jc);
                                }
                                jm["tool_calls"] = calls;
                            }
                            break;
                        case "tool":
                            jm["role"] = "tool";
                            jm["tool_call_id"] = m.ToolCallId ?? "";
                            jm["name"] = m.ToolName ?? "";
                            jm["content"] = m.Text ?? "";
                            break;
                        default:
                            continue;
                    }
                    messages.Add(jm);
                }
                body["messages"] = messages;
                if (tools != null && tools.Count > 0)
                {
                    JSONArray jtools = new JSONArray();
                    foreach (AITool t in tools)
                    {
                        JSONObject jt = new JSONObject();
                        jt["type"] = "function";
                        JSONObject fn = new JSONObject();
                        fn["name"] = t.Name;
                        fn["description"] = t.Description;
                        fn["parameters"] = t.Schema;
                        jt["function"] = fn;
                        jtools.Add(jt);
                    }
                    body["tools"] = jtools;
                    body["tool_choice"] = "auto";
                }
                if (settings.MaxTokens > 0) body["max_tokens"] = settings.MaxTokens;

                JSONNode node = await SendAsync(settings, HttpMethod.Post, Endpoint(settings.BaseUrl, "/chat/completions", null), body, ct).ConfigureAwait(false);
                if (node.HasKey("error") && !node["error"].IsNull) throw new Exception("Server error: " + node["error"].ToString());
                JSONNode choice = node["choices"][0];
                JSONNode msg = choice["message"];
                AIMessage result = new AIMessage { Role = "assistant", StopReason = choice["finish_reason"].Value };
                JSONNode content = msg["content"];
                if (content.IsString) result.Text = content.Value;
                else if (content.IsArray)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (JSONNode part in content.Children) if (part["type"].Value == "text") sb.Append(part["text"].Value);
                    result.Text = sb.ToString();
                }
                if (msg.HasKey("tool_calls") && msg["tool_calls"].IsArray)
                {
                    foreach (JSONNode jc in msg["tool_calls"].Children)
                    {
                        AIToolCall call = new AIToolCall();
                        call.Id = jc["id"].Value;
                        if (string.IsNullOrEmpty(call.Id)) call.Id = "call_" + Guid.NewGuid().ToString("N").Substring(0, 12);
                        call.Name = jc["function"]["name"].Value;
                        JSONNode args = jc["function"]["arguments"];
                        // Most servers send the arguments as a JSON string; a few send the object itself.
                        if (args.IsString) call.Arguments = JSON.Parse(args.Value) ?? new JSONObject();
                        else if (args.IsObject) call.Arguments = args;
                        else call.Arguments = new JSONObject();
                        result.ToolCalls.Add(call);
                    }
                }
                if (result.StopReason == "length") result.Text += "\n\n[The reply was cut off by the token limit. Raise Max tokens in the settings if this keeps happening.]";
                return result;
            }

            // ---------------------------------------------------------------- Anthropic Messages API
            private static async Task<AIMessage> CompleteAnthropicAsync(AISettings settings, string system, List<AIMessage> history, List<AITool> tools, CancellationToken ct)
            {
                JSONObject body = new JSONObject();
                body["model"] = settings.Model;
                body["max_tokens"] = Math.Max(settings.MaxTokens, 4096);
                body["system"] = system;
                JSONArray messages = new JSONArray();
                JSONArray pendingResults = null;
                foreach (AIMessage m in history)
                {
                    if (m.Role == "tool")
                    {
                        // Every tool result for one assistant turn goes back in a single user message.
                        if (pendingResults == null) pendingResults = new JSONArray();
                        JSONObject r = new JSONObject();
                        r["type"] = "tool_result";
                        r["tool_use_id"] = m.ToolCallId ?? "";
                        r["content"] = m.Text ?? "";
                        if (m.IsError) r["is_error"] = true;
                        pendingResults.Add(r);
                        continue;
                    }
                    if (pendingResults != null)
                    {
                        JSONObject u = new JSONObject();
                        u["role"] = "user";
                        u["content"] = pendingResults;
                        messages.Add(u);
                        pendingResults = null;
                    }
                    JSONObject jm = new JSONObject();
                    if (m.Role == "user")
                    {
                        jm["role"] = "user";
                        JSONArray blocks = new JSONArray();
                        JSONObject t = new JSONObject();
                        t["type"] = "text";
                        t["text"] = string.IsNullOrEmpty(m.Text) ? "(empty)" : m.Text;
                        blocks.Add(t);
                        jm["content"] = blocks;
                    }
                    else if (m.Role == "assistant")
                    {
                        jm["role"] = "assistant";
                        if (m.RawContent != null && m.RawContent.IsArray && m.RawContent.Count > 0)
                        {
                            jm["content"] = m.RawContent;
                        }
                        else
                        {
                            JSONArray blocks = new JSONArray();
                            if (!string.IsNullOrEmpty(m.Text))
                            {
                                JSONObject t = new JSONObject();
                                t["type"] = "text";
                                t["text"] = m.Text;
                                blocks.Add(t);
                            }
                            foreach (AIToolCall c in m.ToolCalls)
                            {
                                JSONObject tu = new JSONObject();
                                tu["type"] = "tool_use";
                                tu["id"] = c.Id;
                                tu["name"] = c.Name;
                                tu["input"] = c.Arguments ?? new JSONObject();
                                blocks.Add(tu);
                            }
                            if (blocks.Count == 0)
                            {
                                JSONObject t = new JSONObject();
                                t["type"] = "text";
                                t["text"] = "(no response)";
                                blocks.Add(t);
                            }
                            jm["content"] = blocks;
                        }
                    }
                    else continue;
                    messages.Add(jm);
                }
                if (pendingResults != null)
                {
                    JSONObject u = new JSONObject();
                    u["role"] = "user";
                    u["content"] = pendingResults;
                    messages.Add(u);
                }
                body["messages"] = messages;
                if (tools != null && tools.Count > 0)
                {
                    JSONArray jtools = new JSONArray();
                    foreach (AITool t in tools)
                    {
                        JSONObject jt = new JSONObject();
                        jt["name"] = t.Name;
                        jt["description"] = t.Description;
                        jt["input_schema"] = t.Schema;
                        jtools.Add(jt);
                    }
                    body["tools"] = jtools;
                }

                JSONNode node = await SendAsync(settings, HttpMethod.Post, Endpoint(settings.BaseUrl, "/v1/messages", "/v1"), body, ct).ConfigureAwait(false);
                AIMessage result = new AIMessage { Role = "assistant", StopReason = node["stop_reason"].Value, RawContent = node["content"] };
                StringBuilder text = new StringBuilder();
                foreach (JSONNode block in node["content"].Children)
                {
                    string type = block["type"].Value;
                    if (type == "text") text.Append(block["text"].Value);
                    else if (type == "tool_use") result.ToolCalls.Add(new AIToolCall { Id = block["id"].Value, Name = block["name"].Value, Arguments = block["input"].IsObject ? block["input"] : new JSONObject() });
                }
                if (result.StopReason == "refusal")
                {
                    string why = node.HasKey("stop_details") && !node["stop_details"].IsNull ? node["stop_details"]["explanation"].Value : "";
                    text.Append("\n\n[The model declined this request" + (string.IsNullOrEmpty(why) ? "." : ": " + why + "]"));
                }
                else if (result.StopReason == "max_tokens")
                {
                    text.Append("\n\n[The reply was cut off by the token limit. Raise Max tokens in the settings if this keeps happening.]");
                }
                result.Text = text.ToString();
                return result;
            }

            // ---------------------------------------------------------------- HTTP
            private static async Task<JSONNode> SendAsync(AISettings settings, HttpMethod method, string url, JSONNode body, CancellationToken ct)
            {
                using (HttpRequestMessage request = new HttpRequestMessage(method, url))
                {
                    if (settings.Protocol == AIProtocol.Anthropic)
                    {
                        request.Headers.Add("x-api-key", settings.ApiKey ?? "");
                        request.Headers.Add("anthropic-version", "2023-06-01");
                    }
                    else if (!string.IsNullOrEmpty(settings.ApiKey))
                    {
                        request.Headers.Add("Authorization", "Bearer " + settings.ApiKey);
                    }
                    if (body != null) request.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
                    HttpResponseMessage response;
                    try
                    {
                        response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct).ConfigureAwait(false);
                    }
                    catch (HttpRequestException e)
                    {
                        Debug.LogWarning("[kebinImports] kebinAI could not reach " + url + ": " + (e.InnerException ?? e).Message);
                        bool local = url.Contains("localhost") || url.Contains("127.0.0.1");
                        throw new Exception(local
                            ? "kebinAI couldn't reach the AI running on this computer. Make sure it is started (for example, open Ollama or LM Studio) and try again."
                            : "kebinAI couldn't reach " + new Uri(url).Host + ". Check your internet connection and try again.");
                    }
                    using (response)
                    {
                        string text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode)
                        {
                            Debug.LogWarning("[kebinImports] kebinAI request failed (HTTP " + (int)response.StatusCode + ") at " + url + ": " + Truncate(text, 800));
                            throw new Exception(FriendlyHttpError((int)response.StatusCode, settings));
                        }
                        JSONNode node = JSON.Parse(text);
                        if (node == null)
                        {
                            Debug.LogWarning("[kebinImports] kebinAI got an unexpected answer from " + url + ": " + Truncate(text, 300));
                            throw new Exception("The AI service sent back something kebinAI didn't understand. Check the address in kebinAI's Settings.");
                        }
                        return node;
                    }
                }
            }

            private static string FriendlyHttpError(int status, AISettings settings)
            {
                switch (status)
                {
                    case 401:
                    case 403: return "The API key was not accepted. Open kebinAI's Settings and check the key for " + settings.PresetInfo.Name + ".";
                    case 402: return "Your " + settings.PresetInfo.Name + " account is out of credit. Add credit, or pick a model marked [Free].";
                    case 404: return "The AI service doesn't know the model \"" + settings.Model + "\". Pick another model in kebinAI's Settings (the Models button lists them).";
                    case 408:
                    case 504: return "The AI service took too long to answer. Try again in a moment.";
                    case 413: return "The conversation got too long for this model. Click New chat and try again.";
                    case 429: return "The AI service says you're sending too many requests, or you've hit your limit. Wait a minute and try again.";
                    default:
                        if (status >= 500) return "The AI service is having problems right now (error " + status + "). Try again in a moment.";
                        return "The AI service refused the request (error " + status + "). The details are in Unity's Console.";
                }
            }
            // Joins the base URL with a path, tolerating a base that already ends with the version segment.
            private static string Endpoint(string baseUrl, string path, string versionSegment)
            {
                string b = (baseUrl ?? "").Trim().TrimEnd('/');
                if (versionSegment != null && b.EndsWith(versionSegment, StringComparison.OrdinalIgnoreCase) && path.StartsWith(versionSegment + "/"))
                {
                    path = path.Substring(versionSegment.Length);
                }
                return b + path;
            }

            public static string Truncate(string s, int max)
            {
                if (s == null) return "";
                return s.Length <= max ? s : s.Substring(0, max) + "… (" + (s.Length - max) + " more characters)";
            }
        }
    }
}
