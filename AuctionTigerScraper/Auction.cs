using System.Collections.Generic;

namespace AuctionTigerScraper
{
    public class Auction
    {
        public string Name { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public DocumentData[] DocumentData { get; set; }
        public List<Vehicle> Vehicles { get; set; }
    }
}
