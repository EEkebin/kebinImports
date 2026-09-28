using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace kebinImports
{
    public class HttpClient : System.Net.Http.HttpClient
    {
        private static readonly HttpClientHandler handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            PreAuthenticate = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        public HttpClient(bool GitHubHeaders = false) : base(handler, false)
        {
            // Unity's Mono only negotiates TLS 1.2+ if it is explicitly enabled on older runtimes.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            Timeout = TimeSpan.FromMinutes(5);
            DefaultRequestHeaders.Add("User-Agent", "kebinImports (+https://github.com/EEkebin/kebinImports)");
            if (GitHubHeaders)
            {
                // Unauthenticated requests to the public GitHub API are limited to 60/hour, which is plenty for a manual importer.
                DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
                DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            }
        }
        public static async Task DownloadFile(HttpClient client, string link, string fileNameExtension)
        {
            using (HttpResponseMessage response = await client.GetAsync(link, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                using (FileStream fs = File.Create(fileNameExtension))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }
        }
        public static async Task<string> DownloadString(HttpClient client, string link)
        {
            using (HttpResponseMessage response = await client.GetAsync(link))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
