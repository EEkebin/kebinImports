using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace kebinImports
{
    public class HttpClient : System.Net.Http.HttpClient
    {
        private static HttpClientHandler handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            PreAuthenticate = true
        };
        public HttpClient(bool GitHubHeaders = false) : base(handler)
        {
            this.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.246");
            if (GitHubHeaders)
                this.DefaultRequestHeaders.Add("Authorization", "token " + Encoding.UTF8.GetString(Convert.FromBase64String("Z2hwX1RhWHA0UlFzRHdQR1RuUmhOYUtmYW1yOEgxWXI2SDRXTGdqNg==")));
        }
        public static async Task DownloadFile(HttpClient client, string link, string fileNameExtension)
        {
            var response = await client.GetAsync(link);
            using (var fs = File.Create(fileNameExtension))
            {
                await response.Content.CopyToAsync(fs);
            }
        }
        public static async Task<string> DownloadString(HttpClient client, string link)
        {
            var response = await client.GetStringAsync(link);
            return response;
        }
    }
}
