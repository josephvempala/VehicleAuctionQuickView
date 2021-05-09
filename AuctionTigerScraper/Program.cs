using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AuctionTigerScraper
{
    public class AuctionScraper
    {
        #region private
        private bool currentlyChecking = false;
        private Browser browser;
        private Page page;
        public List<Auction> Auctions = new();
        public List<Vehicle> Vehicles;
        public IEnumerable<string> DesirableVehicleNames { get; private set; }

        private async Task<IEnumerable<Vehicle>> GetVehicles()
        {
            List<Vehicle> Vehicles = new();
            await Task.Run(() =>
            {
                if (Auctions is null)
                {
                    return;
                }
                List<Auction> ToBeRemoved = new();
                foreach (Auction Auction in Auctions)
                {
                    MatchCollection matches = null;
                    Auction.Vehicles = new List<Vehicle>();
                    matches = Regex.Matches(Regex.Replace(Auction.Description, @"\n", " "), @"(?>(?<AucName>.+?)?(?>[\(]?(\d{1,2})[\)]?) ?(?<CarName>.+?)(?<Number>(?>[A-Z]{2}[- ]*?[A-Z\d]{1,2}[- ]*?[A-Z\d]{1,2}[- ]*?[\d]{4} ))(?<Fuel>(?>Diesel|Petrol|CNG|0)).*?(?<Year>\d{4}) (?<Address>.+?)(?:(?>Ownership( status)?)) ?(?<Status>(?>\d))? ?(?:(?>Remark))?(?<Remarks>.+?)?(?<Reference>(?>MOT\d{8})))", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(1000));
                    try
                    {
                        foreach (Match match in matches)
                        {
                            if (match.Groups["AucName"].Success is true && match.Groups["AucName"].Value is not " ")
                            {
                                Auction.Name = match.Groups["AucName"].Value;
                            }
                            Vehicle vehicle = new Vehicle(Guid.NewGuid(), Auction, match.Groups["CarName"].Value, match.Groups["Number"].Value, match.Groups["Fuel"].Value, match.Groups["Year"].Value, match.Groups["Address"].Value, match.Groups["Status"].Value, match.Groups["Remarks"].Value, match.Groups["Reference"].Value);
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
            });
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
            await page.WaitForSelectorAsync(".details-box");
            List<Auction> auctions = await page.EvaluateExpressionAsync<List<Auction>>(@"Array.from(document.getElementsByClassName('details-box')).map(box=>{return{Link:box.firstElementChild.firstElementChild.href,Description:box.firstElementChild.firstElementChild.text}});");
            return auctions;
        }
        private async Task GetAllAuctionDocuments(IEnumerable<Auction> auctions)
        {
            List<Task> documentTasks = new List<Task>();
            foreach (Auction Auction in auctions)
            {
                documentTasks.Add(GetDocuments(Auction));
            }
            await Task.WhenAll(documentTasks);
        }
        private async Task GetDocuments(Auction auction)
        {
            Page page = await browser.NewPageAsync();
            await page.GoToAsync(auction.Link);
            try
            {
                await page.WaitForSelectorAsync("#auctionNotice > div > table > tbody > tr:nth-child(3) > td:nth-child(7) > a");
            }
            catch
            {
                auction.DocumentData = null;
                return;
            }
            auction.DocumentData = await page.EvaluateExpressionAsync<DocumentData[]>("Array.from(Array.from(document.querySelectorAll('.table-border'))[1].querySelectorAll('tr')).slice(2).map((i)=>{return{DocName:i.outerText,DownloadLink:i.lastElementChild.firstElementChild.href.slice(37)}});");
            await page.CloseAsync();
        }
        private async Task StartWatchchingForAuctions(Page page)
        {
            while (true)
            {
                await CheckForChangesAsync();
                await Task.Delay(new TimeSpan(0, 20, 0));
            }
        }
        #endregion
        #region public
        public async Task CheckForChangesAsync()
        {
            bool isNew = false;
            if (currentlyChecking)
                return;
            currentlyChecking = true;
            await page.ReloadAsync();
            List<Auction> newAuctions = await GetAuctionsInfoAsync();
            if (newAuctions.Count != Auctions.Count)
            {
                isNew = true;
                newAuctions.RemoveAll(item => Auctions.Contains(item));
                if (newAuctions.Count > 0)
                    await GetAllAuctionDocuments(newAuctions);
                Auctions.AddRange(newAuctions);
            }
            List<Task> temp = new();
            Auction tempAuction = new();
            for (int i = 0; i < newAuctions.Count; i++)
            {
                if (!newAuctions[i].Equals(Auctions[i]))
                {
                    isNew = true;
                    temp.Add(GetDocuments(newAuctions[i]));
                    Auctions[i] = newAuctions[i];
                }
            }
            if (isNew)
            {
                await Task.WhenAll(Task.WhenAll(temp));
                List<Vehicle> vehicles = (List<Vehicle>)await GetVehicles();
                foreach (Vehicle vehicle in vehicles)
                {
                    foreach (DocumentData document in vehicle.Auction.DocumentData)
                    {
                        Match match = Regex.Match(document.DocName, @$"{vehicle.Reference}|{vehicle.RegistrationNumber.State}[- ]*?{vehicle.RegistrationNumber.RTO}[- ]*?{vehicle.RegistrationNumber.Series}[- ]*?{vehicle.RegistrationNumber.Number}|{vehicle.Name}");
                        if (match.Success)
                        {
                            vehicle.PicturesLink = document.DownloadLink;
                        }
                    }
                }
                Vehicles = vehicles;
                await DownloadPicturesAsync(await GetDesirableVehiclesAsync(DesirableVehicleNames));
            }
            currentlyChecking = false;
        }
        public async Task InitializeScraperAsync(ScraperOptions scraperOptions)
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = false
            });
            page = await browser.NewPageAsync();
            await page.GoToAsync("https://icicilombard.procuretiger.com/");
            await LoginAsync(scraperOptions.Username, scraperOptions.Password);
            DesirableVehicleNames = scraperOptions.DesirableVehicles;
            await CheckForChangesAsync();
            StartWatchchingForAuctions(page);
        }
        public async Task DownloadPicturesAsync(IEnumerable<Vehicle> vehicles)
        {
            Page page = await browser.NewPageAsync();
            await page.GoToAsync("https://icicilombard.procuretiger.com/");
            foreach (Vehicle vehicle in vehicles)
            {
                DownloadManager downloadManager = new(Path.GetTempPath());
                if (vehicle.PicturesLink is null or "")
                {
                    continue;
                }
                await downloadManager.SetupPageAsync(page);
                await page.EvaluateExpressionAsync($"var butn = document.createElement('A'); butn.href = '{vehicle.PicturesLink}'; butn.text='test'; document.body.appendChild(butn);");
                string download = await downloadManager.WaitForDownload(page, vehicle.PicturesLink);
                string downloadsPath = Path.Combine(Path.GetTempPath(), vehicle.Id.ToString());
                ZipFile.ExtractToDirectory(download, downloadsPath);
                vehicle.Pictures = Directory.EnumerateFiles(downloadsPath, "*.*", SearchOption.AllDirectories);
            }
            await page.CloseAsync();
        }
        public async Task<IEnumerable<Vehicle>> GetDesirableVehiclesAsync(IEnumerable<string> DesirableVehicleNames)
        {
            List<Vehicle> desirablevehicles = new List<Vehicle>();
            await Task.Run(() =>
            {
                foreach (Vehicle vehicle in Vehicles)
                {
                    foreach (string car in DesirableVehicleNames)
                        if (vehicle.Name.ToUpper().Contains(car.ToUpper()))
                        {
                            desirablevehicles.Add(vehicle);
                        }
                }
            });
            return desirablevehicles;
        }
        #endregion
    }
}
