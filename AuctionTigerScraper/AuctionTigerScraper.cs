using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AuctionTigerScraper
{
    public class AuctionTigerScraper : IAsyncDisposable, IAuctionScraper
    {
        private Browser browser;
        private Page page;
        private Action<Vehicle[], Vehicle[]> AuctionsChanged;
        private List<Auction> Auctions = new();

        private IEnumerable<Vehicle> GetVehicles(IEnumerable<Auction> auctions)
        {
            List<Vehicle> Vehicles = new();
            List<Auction> ToBeRemoved = new();
            foreach (Auction Auction in auctions)
            {
                MatchCollection matches = null;
                Auction.Vehicles = new List<Vehicle>();
                try
                {
                    matches = Regex.Matches(Regex.Replace(Auction.Description, @"\n", " "), @"(?>(?<AucName>.+?)?(?>[\(]?(\d{1,2})[\)]?) ?(?<CarName>.+?)(?<Number>(?>[A-Z]{2}[- ]*?[A-Z\d]{1,2}[- ]*?[A-Z\d]{1,2}[- ]*?[\d]{4} ))(?<Fuel>(?>Diesel|Petrol|CNG|0)).*?(?<Year>\d{4}) (?<Address>.+?)(?:(?>Ownership( status)?)) ?(?<Status>(?>\d))? ?(?:(?>Remark))?(?<Remarks>.+?)?(?<Reference>(?>MOT\d{8})))", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(1000));
                    foreach (Match match in matches)
                    {
                        if (match.Groups["AucName"].Success is true && match.Groups["AucName"].Value is not " ")
                        {
                            Auction.Name = match.Groups["AucName"].Value;
                        }
                        Vehicle vehicle = new Vehicle(Auction, match.Groups["CarName"].Value, match.Groups["Number"].Value, match.Groups["Fuel"].Value, match.Groups["Year"].Value, match.Groups["Address"].Value, match.Groups["Status"].Value, match.Groups["Remarks"].Value, match.Groups["Reference"].Value);
                        Auction.Vehicles.Add(vehicle);
                        Vehicles.Add(vehicle);
                    }
                }
                catch
                {
                    ToBeRemoved.Add(Auction);
                    continue;
                }
            }
            ToBeRemoved.ForEach(item => { Auctions.Remove(item); });
            return Vehicles;
        }

        private async Task LoginAsync(string username, string password)
        {
            await page.EvaluateExpressionAsync("loginWindow();");
            await page.TypeAsync("#j_username", username);
            await page.TypeAsync("#j_password", password);
            await page.ClickAsync("button.pull-left.blue-button-small");
        }

        private async Task<List<Auction>> GetAuctionsInfoAsync()
        {
            try
            {
                await page.WaitForSelectorAsync(".details-box");
            }
            catch (Exception e)
            {
                throw new Exception("Unable to Log-In", e);
            }
            List<Auction> auctions = await page.EvaluateExpressionAsync<List<Auction>>(@"Array.from(document.getElementsByClassName('details-box')).map(box=>{return{Link:box.firstElementChild.firstElementChild.href,Description:box.firstElementChild.firstElementChild.text}});");
            return auctions;
        }

        private async Task GetAllAuctionDocuments(IEnumerable<Auction> auctions)
        {
            List<Task> documentTasks = new List<Task>();
            foreach (Auction Auction in auctions)
            {
                documentTasks.Add(GetDocuments(Auction, auctions));
            }
            await Task.WhenAll(documentTasks);
        }

        private async Task GetDocuments(Auction auction, IEnumerable<Auction> auctions)
        {
            using (Page page = await browser.NewPageAsync())
            {
                await page.GoToAsync(auction.Link);
                try
                {
                    await page.WaitForSelectorAsync("#auctionNotice > div > table > tbody > tr:nth-child(3) > td:nth-child(7) > a", new WaitForSelectorOptions { Timeout = 1000 });
                }
                catch
                {
                    Auctions.Remove(auction);
                    auction.DocumentData = null;
                    return;
                }
                auction.DocumentData = await page.EvaluateExpressionAsync<DocumentData[]>("Array.from(Array.from(document.querySelectorAll('.table-border'))[1].querySelectorAll('tr')).slice(2).map((i)=>{return{DocName:i.outerText,DownloadLink:i.lastElementChild.firstElementChild.href.slice(37)}});");
            }
        }

        private async Task DownloadPicturesAsync(IEnumerable<Vehicle> vehicles)
        {
            using (Page page = await browser.NewPageAsync())
            {
                await page.GoToAsync("https://icicilombard.procuretiger.com/");
                foreach (Vehicle vehicle in vehicles)
                {
                    string downloadsPath = Path.Combine(Path.GetTempPath(), vehicle.RegistrationNumber.ToString());
                    if (vehicle.PicturesLink is null or "" || vehicle.PicturesDirectory is not null)
                    {
                        continue;
                    }
                    if (Directory.Exists(downloadsPath))
                    {
                        vehicle.PicturesDirectory = downloadsPath;
                        continue;
                    }
                    DownloadManager downloadManager = new(Path.GetTempPath());
                    await downloadManager.SetupPageAsync(page);
                    try
                    {
                        await page.EvaluateExpressionAsync($"var butn = document.createElement('A'); butn.href = '{vehicle.PicturesLink}'; butn.text='test'; document.body.appendChild(butn);");
                        string download = await downloadManager.WaitForDownload(page, vehicle.PicturesLink);
                        ZipFile.ExtractToDirectory(download, downloadsPath);
                    }
                    catch (Exception e)
                    {
                        vehicle.PicturesDirectory = null;
                        continue;
                    }
                    vehicle.PicturesCount = Directory.EnumerateFiles(downloadsPath, "*.*", SearchOption.AllDirectories).Select((link, index) => { File.Move(link, downloadsPath + "\\" + index + ".jpg"); return string.Empty; }).Count();
                    vehicle.PicturesDirectory = downloadsPath;
                }
            }
        }

        private async Task UpdateVehicleInfo(IEnumerable<Auction> newAuctions, IEnumerable<Auction> expiredAuctions)
        {
            Vehicle[] newVehicles = GetVehicles(newAuctions).ToArray();
            Vehicle[] expiredVehicles = GetVehicles(expiredAuctions).ToArray();
            foreach (Vehicle vehicle in newVehicles)
            {
                foreach (DocumentData document in vehicle.Auction.DocumentData)
                {
                    Match match = null;
                    try
                    {
                        match = Regex.Match(document.DocName, @$"{vehicle.Reference}|{vehicle.RegistrationNumber.State}[- ]*?{vehicle.RegistrationNumber.RTO}[- ]*?{vehicle.RegistrationNumber.Series}[- ]*?{vehicle.RegistrationNumber.Number}|{vehicle.Name}");
                    }
                    catch
                    {
                        continue;
                    }
                    if (match.Success)
                    {
                        vehicle.PicturesLink = document.DownloadLink;
                    }
                }
            }
            await DownloadPicturesAsync(newVehicles);
            AuctionsChanged?.Invoke(newVehicles, expiredVehicles);
        }

        public async Task<bool> CheckForChangesAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;
            await page.ReloadAsync();
            List<Auction> currentAuctions = await GetAuctionsInfoAsync();
            IEnumerable<Auction> expiredAuctions = Auctions.Where(item => !currentAuctions.Exists(x => x.Equals(item))).ToList();
            IEnumerable<Auction> newAuctions = currentAuctions.Where(item => !Auctions.Exists(x => x.Equals(item))).ToList();
            if (newAuctions.Count() > 0 || expiredAuctions.Count() > 0)
            {
                Auctions = currentAuctions;
                await GetAllAuctionDocuments(Auctions);
                await UpdateVehicleInfo(newAuctions, expiredAuctions);
            }
            return newAuctions.Count() > 0 || expiredAuctions.Count() > 0;
        }

        public void ListenForChanges(Action<Vehicle[], Vehicle[]> toBeCalledOnChanges)
        {
            AuctionsChanged += toBeCalledOnChanges;
        }

        public void StopListeningForChanges(Action<Vehicle[], Vehicle[]> toBeRemoved)
        {
            AuctionsChanged -= toBeRemoved;
        }

        public async Task InitializeScraperAsync(ScraperOptions scraperOptions, CancellationToken cancellationToken)
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new string[] { "--no-sandbox" },
            });
            page = await browser.NewPageAsync();
            await page.GoToAsync("https://icicilombard.procuretiger.com/");
            await LoginAsync(scraperOptions.Username, scraperOptions.Password);
        }

        ~AuctionTigerScraper()
        {
            page.Dispose();
            browser.Dispose();
        }
            
        public async ValueTask DisposeAsync()
        {
            await page.CloseAsync();
            await browser.CloseAsync();
        }
    }
}
