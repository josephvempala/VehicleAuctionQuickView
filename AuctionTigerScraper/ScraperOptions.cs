using System.Collections.Generic;

namespace AuctionTigerScraper
{
    public class ScraperOptions
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public IEnumerable<string> DesirableVehicles { get; set; }
    }
}
