using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace kebinImports
{
    public partial class kebinImports
    {
        // Once a day, fetches the listings in the background and notes which installed tools have a newer version.
        internal static class UpdateChecker
        {
            internal class Update
            {
                public Tool Tool;
                public string InstalledVersion;
                public string LatestVersion;
            }
            public static List<Update> Available = new List<Update>();
            public static DateTime LastCheck = DateTime.MinValue;
            public static bool Checking { get; private set; }
            public static event Action Changed;

            private const string LastCheckKey = "kebinImports.lastUpdateCheck";
            private static Task<Dictionary<string, string>> fetch;

            [InitializeOnLoadMethod]
            private static void Init()
            {
                if (Application.isBatchMode) return;
                long ticks;
                if (long.TryParse(EditorPrefs.GetString(LastCheckKey, "0"), out ticks)) LastCheck = new DateTime(ticks, DateTimeKind.Utc);
                Restore();
                if (DateTime.UtcNow - LastCheck > TimeSpan.FromHours(24)) EditorApplication.delayCall += () => Check();
            }

            // Downloads the listing JSON on a worker thread, then evaluates on the main thread.
            public static void Check()
            {
                if (Checking) return;
                Checking = true;
                List<string> urls = Vpm.AllListingUrls().ToList();
                fetch = Task.Run(() =>
                {
                    Dictionary<string, string> result = new Dictionary<string, string>();
                    foreach (string url in urls)
                    {
                        try
                        {
                            HttpClient c = new HttpClient();
                            result[url] = HttpClient.DownloadString(c, url).Result;
                        }
                        catch (Exception) { }
                    }
                    return result;
                });
                EditorApplication.update += Poll;
            }
            private static void Poll()
            {
                if (fetch == null || !fetch.IsCompleted) return;
                EditorApplication.update -= Poll;
                try
                {
                    if (!fetch.IsFaulted)
                    {
                        foreach (KeyValuePair<string, string> kv in fetch.Result) Vpm.Prime(kv.Key, kv.Value);
                        foreach (string url in Vpm.AllListingUrls()) if (!fetch.Result.ContainsKey(url)) Vpm.MarkFailed(url);
                    }
                    Evaluate();
                }
                finally
                {
                    fetch = null;
                    Checking = false;
                    LastCheck = DateTime.UtcNow;
                    EditorPrefs.SetString(LastCheckKey, LastCheck.Ticks.ToString());
                    Changed?.Invoke();
                }
            }
            private static void Evaluate()
            {
                Available = ToolCatalog.AllStatuses(true).Where(s => s.UpdateAvailable).Select(s => new Update { Tool = s.Tool, InstalledVersion = s.InstalledVersion, LatestVersion = s.LatestVersion }).ToList();
                SessionState.SetString("kebinImports.updates", string.Join("\n", Available.Select(u => u.Tool.Key + "|" + u.InstalledVersion + "|" + u.LatestVersion)));
                if (Available.Count > 0) Debug.Log("[kebinImports] Updates available: " + string.Join(", ", Available.Select(u => u.Tool.Name + " " + u.InstalledVersion + " -> " + u.LatestVersion)) + ". Open kebinImports > Project Doctor to update.");
            }
            private static void Restore()
            {
                string saved = SessionState.GetString("kebinImports.updates", "");
                if (string.IsNullOrEmpty(saved)) return;
                Available = new List<Update>();
                foreach (string line in saved.Split('\n'))
                {
                    string[] parts = line.Split('|');
                    Tool t = parts.Length == 3 ? ToolCatalog.Get(parts[0]) : null;
                    if (t != null) Available.Add(new Update { Tool = t, InstalledVersion = parts[1], LatestVersion = parts[2] });
                }
            }
        }
    }
}
