using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuctionTigerScraper
{
    internal static class Helpers
    {
        public static bool CompareDescription(this List<Auction> currentAuctions, List<Auction> newAuctions)
        {
            if (currentAuctions.Count != newAuctions.Count)
                return false;
            for(int i=0;i<currentAuctions.Count;i++)
            {
                if (currentAuctions[i].Description.GetHashCode() != newAuctions[i].Description.GetHashCode())
                    return false;
            }
            return true;
        }
    }
}
