using PuppeteerSharp;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System;

namespace AuctionTigerScraper
{
    public class AuctionScraper
    {
        #region private
        private string username;
        private string password;
        private bool currentlyChecking = false;
        private Browser browser;
        private Page page;
        public List<Auction> Auctions;
        public List<Vehicle> Vehicles = new();
        private async Task DeserializeVehicleInfo()
        {
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
                            var vehicle = new Vehicle(Guid.NewGuid(), Auction, match.Groups["CarName"].Value, match.Groups["Number"].Value, match.Groups["Fuel"].Value, match.Groups["Year"].Value, match.Groups["Address"].Value, Auction.Id, match.Groups["Status"].Value, match.Groups["Remarks"].Value, match.Groups["Reference"].Value);
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
        }
        private async Task LoginAsync()
        {
            await page.EvaluateExpressionAsync("loginWindow();");
            await page.TypeAsync("#j_username", username);
            await page.TypeAsync("#j_password", password);
            await page.ClickAsync("button.pull-left.blue-button-small");
        }
        private async Task<List<Auction>> GetAuctionsInfoAsync()
        {
            await CheckSessionExpire();
            await page.WaitForSelectorAsync(".details-box");
            var auctions = await page.EvaluateExpressionAsync<List<Auction>>(@"Array.from(document.getElementsByClassName('details-box')).map(box=>{return{Link:box.firstElementChild.firstElementChild.href,Description:box.firstElementChild.firstElementChild.text}});");
            auctions.ForEach(item => { item.Id = Guid.NewGuid(); });
            return auctions;
        }
        private async Task<List<string>> GetDesirableVehiclesPicturesAsync(IEnumerable<Vehicle> DesirableVehicles)
        {
            await CheckSessionExpire();
            var tempdir = Path.GetTempPath();
            DownloadManager downloadManager = new DownloadManager(tempdir);
            List<string> downloads = new List<string>();
            foreach(Vehicle vehicle in DesirableVehicles)
            {
                downloads.Add(await DownloadPictures(vehicle, downloadManager));
            }
            return downloads;
        }
        private async Task GetAllAuctionDocuments()
        {
            List<Task> documentTasks = new List<Task>();
            foreach(var Auction in Auctions)
            {
                documentTasks.Add(GetDocuments(Auction));
            }
            await Task.WhenAll(documentTasks);
        }
        private async Task GetDocuments(Auction auction)
        {
            var page = await browser.NewPageAsync();
            await page.GoToAsync(auction.Link);
            await page.WaitForSelectorAsync("#auctionNotice > div > table > tbody > tr:nth-child(3) > td:nth-child(7) > a");
            auction.DocumentData = await page.EvaluateExpressionAsync<DocumentData[]>("Array.from(Array.from(document.querySelectorAll('.table-border'))[1].querySelectorAll('tr')).slice(2).map((i)=>{return{DocName:i.outerText,DownloadLink:i.lastElementChild.firstElementChild.href.slice(37)}});");
            await page.CloseAsync();
        }
        private async Task<string> DownloadPictures(Vehicle vehicle, DownloadManager downloadManager)
        {
            Page temp = await browser.NewPageAsync();
            await temp.GoToAsync(vehicle.Auction.Link);
            if (vehicle.Auction.DocumentData is null)
            {
                return string.Empty;
            }
            foreach (DocumentData document in vehicle.Auction.DocumentData)
            {
                var match = Regex.Match(document.DocName, @$"{vehicle.Reference}|{vehicle.RegistrationNumber.State}[- ]*?{vehicle.RegistrationNumber.RTO}[- ]*?{vehicle.RegistrationNumber.Series}[- ]*?{vehicle.RegistrationNumber.Number}|{vehicle.Name}");
                if (match.Success)
                {
                    await downloadManager.SetupPageAsync(temp);
                    string download = await downloadManager.WaitForDownload(temp, document.DownloadLink);
                    await temp.CloseAsync();
                    return download;
                }
            }
            return null;
        }
        private async Task CheckSessionExpire()
        {
            if (await page.EvaluateExpressionAsync(@"document.querySelector('h1.pull-left.grid_6')")!= null)
            if(await page.EvaluateExpressionAsync<string>(@"document.querySelector('h1.pull-left.grid_6').innerText;")=="Session expired")
            {
                await LoginAsync();
            }
        }
        private async Task WatchForAuctions(Page page)
        {
            while(true)
            {
                await Task.Delay(new TimeSpan(0, 5, 0));
                await CheckForChanges();
            }
        }
        #endregion
        #region public
        public async Task CheckForChanges()
        {
            if (currentlyChecking)
                return;
            currentlyChecking = true;
            await page.ReloadAsync();
            await CheckSessionExpire();
            var newAuctions = await GetAuctionsInfoAsync();
            if (!newAuctions.CompareDescription(Auctions))
            {
                Auctions = newAuctions;
                Vehicles = new();
                await Task.WhenAll(GetAllAuctionDocuments(), DeserializeVehicleInfo());
            }
            currentlyChecking = false;
        }
        public async Task InitializeScraperAsync(string username, string password)
        {
            this.username = username;
            this.password = password;
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = false
            });
            page = await browser.NewPageAsync();
            await page.GoToAsync("https://icicilombard.procuretiger.com/");
            await LoginAsync();
            Auctions = await GetAuctionsInfoAsync();
            WatchForAuctions(page);
            await Task.WhenAll(GetAllAuctionDocuments(), DeserializeVehicleInfo());
        }
        public async Task<List<Vehicle>> GetDesirableVehiclesAsync(IEnumerable<string> DesirableVehicleNames)
        {
            await CheckSessionExpire();
            List<Vehicle> desirablevehicles = new List<Vehicle>();
            foreach (Vehicle vehicle in Vehicles)
            {
                foreach (string car in DesirableVehicleNames)
                    if (vehicle.Name.ToUpper().Contains(car.ToUpper()))
                    {
                        desirablevehicles.Add(vehicle);
                    }
            }
            return desirablevehicles;
        }
        #endregion
    }
}
