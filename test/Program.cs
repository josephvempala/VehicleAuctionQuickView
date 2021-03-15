using System;
using PuppeteerSharp;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = false
            });
            var page = await browser.NewPageAsync();
            await page.GoToAsync("https://www.w3schools.com/html/html_tables.asp");
            var temp = await page.EvaluateExpressionAsync<Dictionary<string,string>>(@"(()=>{
                                                            let dict={};
                                                            Array.from(document.querySelector('#customers').querySelectorAll('td')).map(i => { dict[i.outerText] = i.outerText});
                                                            return dict;
                                                        })()");

        }
    }
}
