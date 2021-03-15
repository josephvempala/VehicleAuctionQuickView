﻿using System.IO;
using System.Threading.Tasks;
using PuppeteerSharp;
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
            foreach (var file in Directory.EnumerateFiles(_downloadDirectory))
            {
                File.Delete(file);
            }
        }
        public async Task<string> WaitForDownload(Page page, string downloadLink)
        {
            Task<Response> response = GetResponseWithFile(page);
            await page.ClickAsync($"a[href='{downloadLink}']");
            var responseData = await response;
            var contentDisposition = responseData.Headers["Content-Disposition"];
            var fileName = contentDisposition.Split(";")[1].Split("=")[1].Trim('\"');
            var filePath = Path.Combine(_downloadDirectory, fileName);
            await WaitForFile(filePath);
            return filePath;
        }
        private static async Task<Response> GetResponseWithFile(Page page)
        {
            var response = await page.WaitForResponseAsync(r => r.Headers.ContainsKey("Content-Disposition"),
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

