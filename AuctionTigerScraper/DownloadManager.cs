using PuppeteerSharp;
using System.IO;
using System.Threading.Tasks;
namespace AuctionTigerScraper
{
    public class DownloadManager
    {
        private readonly string _downloadDirectory;
        public DownloadManager(string downloadDirectory)
        {
            _downloadDirectory = downloadDirectory;
        }
        public async Task SetupPageAsync(Page page)
        {
            await page.Client.SendAsync("Page.setDownloadBehavior", new
            {
                behavior = "allow",
                downloadPath = _downloadDirectory,
            });
        }
        public void CleanDownloadDirectory()
        {
            foreach (string file in Directory.EnumerateFiles(_downloadDirectory))
            {
                File.Delete(file);
            }
        }
        public async Task<string> WaitForDownload(Page page, string downloadLink)
        {
            Task<Response> response = GetResponseWithFile(page);
            await page.ClickAsync($"a[href='{downloadLink}']");
            Response responseData = await response;
            string contentDisposition = responseData.Headers["Content-Disposition"];
            string fileName = contentDisposition.Split(";")[1].Split("=")[1].Trim('\"');
            string filePath = Path.Combine(_downloadDirectory, fileName);
            await WaitForFile(filePath);
            return filePath;
        }
        private static async Task<Response> GetResponseWithFile(Page page)
        {
            Response response = await page.WaitForResponseAsync(r => r.Headers.ContainsKey("Content-Disposition"),
                new WaitForOptions
                {
                    Timeout = 10000
                });
            return response;
        }
        private static async ValueTask WaitForFile(string filePath)
        {
            while (!File.Exists(filePath))
            {
                await Task.Delay(100);
            }
        }
    }
}

