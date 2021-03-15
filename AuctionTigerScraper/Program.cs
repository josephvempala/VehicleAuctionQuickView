using PuppeteerSharp;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System;

namespace AuctionTigerScraper
{
    public class AuctionScraper
    {
        #region private
        private string username;
        private string password;
        private Browser browser;
        private Page page;
        private List<Auction> Auctions;
        private List<Vehicle> DesirableVehicles;
        private async Task DeserializeVehicleInfo()
        {
            await Task.Run(() =>
            {
                if (Auctions is not null)
                {
                    foreach (Auction Auction in Auctions)
                    {
                        MatchCollection matches = null;
                        Auction.Vehicles = new List<Vehicle>();
                        try
                        {
                            matches = Regex.Matches(Regex.Replace(Auction.Description, @"\n", " "), @"(?>(?<AucName>.+?)?(?>[\(]?(\d{1,2})[\)]?) ?(?<CarName>.+?)(?<Number>(?>[A-Z]{2}[- ]*?[A-Z\d]{1,2}[- ]*?[A-Z\d]{1,2}[- ]*?[\d]{4} ))(?<Fuel>(?>Diesel|Petrol|CNG|0)).*?(?<Year>\d{4}) (?<Address>.+?)(?:(?>Ownership( status)?)) ?(?<Status>(?>\d))? ?(?:(?>Remark))?(?<Remarks>.+?)?(?<Reference>(?>MOT\d{8})))", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100));
                        }
                        catch
                        {
                            Auctions.Remove(Auction);
                            continue;
                        }
                        foreach (Match match in matches)
                        {
                            if (match.Groups["AucName"].Success is true && match.Groups["AucName"].Value is not " ")
                            {
                                Auction.Name = match.Groups["AucName"].Value;
                            }
                            Auction.Vehicles.Add(new Vehicle(Auction, match.Groups["CarName"].Value, match.Groups["Number"].Value, match.Groups["Fuel"].Value, match.Groups["Year"].Value, match.Groups["Address"].Value, match.Groups["Status"].Value, match.Groups["Remarks"].Value, match.Groups["Reference"].Value));
                        }
                    }
                }
            });
        }
        private async Task LoginAsync()
        {
            await page.EvaluateExpressionAsync("loginWindow();");
            await page.TypeAsync("#j_username", username);
            await page.TypeAsync("#j_password", password);
            await page.ClickAsync("button.pull-left.blue-button-small");
        }
        private async Task LoadAuctionsInfoAsync()
        {
            await CheckSessionExpire();
            await page.WaitForSelectorAsync(".details-box");
            Auctions = await page.EvaluateExpressionAsync<List<Auction>>(@"Array.from(document.getElementsByClassName('details-box')).map(box=>{return{Link:box.firstElementChild.firstElementChild.href,Description:box.firstElementChild.firstElementChild.text}});");
        }
        private async Task FindDesirableVehiclesAsync(IEnumerable<string> DesirableVehicleNames)
        {
            await CheckSessionExpire();
            await Task.Run(() =>
            {
                List<Vehicle> desirablevehicles = new List<Vehicle>();
                foreach(Auction auction in Auctions)
                foreach (Vehicle vehicle in auction.Vehicles)
                {
                    foreach (string car in DesirableVehicleNames)
                        if (vehicle.Name.ToUpper().Contains(car.ToUpper()))
                        {
                            desirablevehicles.Add(vehicle);
                        }
                }
                DesirableVehicles = desirablevehicles;
            });
        }
        private async Task<List<string>> GetDesirableVehiclesPicturesAsync()
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
        #endregion
        #region public
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
            await LoadAuctionsInfoAsync();
            await Task.WhenAll(GetAllAuctionDocuments(), DeserializeVehicleInfo());
        }
        #endregion
        public static async Task Main(string[] args)
        {
            AuctionScraper p = new AuctionScraper();
            await p.InitializeScraperAsync("Karthikeyacarz@gmail.com", "Subhash@123");
            await p.FindDesirableVehiclesAsync(new string[] {"kia"});
            await p.browser.CloseAsync();
        }
    }
}
