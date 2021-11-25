using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace kebinClient
{
    public class ModHttpClient : HttpClient
    {
        private static HttpClientHandler handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            PreAuthenticate = true
        };
        public ModHttpClient(bool GH = false) : base(handler)
        {
            this.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.246");
            if (GH)
                this.DefaultRequestHeaders.Add("Authorization", "token " + Encoding.UTF8.GetString(Convert.FromBase64String("Z2hwX1RhWHA0UlFzRHdQR1RuUmhOYUtmYW1yOEgxWXI2SDRXTGdqNg==")));
        }
        public static async Task DownloadFile(ModHttpClient client, string link, string fileName_Extension)
        {
            var response = await client.GetAsync(link);
            using (var fs = File.Create(fileName_Extension))
            {
                await response.Content.CopyToAsync(fs);
            }
        }
        public static async Task<string> DownloadString(ModHttpClient client, string link)
        {
            var response = await client.GetStringAsync(link);
            return response;
        }
    }
}