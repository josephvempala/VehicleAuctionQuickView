using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuctionTigerScraper
{
    public class ScraperOptions
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public IEnumerable<string> DesirableVehicles {get;set;}
    }
}
