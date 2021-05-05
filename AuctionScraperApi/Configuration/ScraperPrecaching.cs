using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuctionScraperApi.Configuration
{
    public class ScraperPrecaching
    {
        public IEnumerable<string> DesirableVehicles { get; set; }
    }
}
