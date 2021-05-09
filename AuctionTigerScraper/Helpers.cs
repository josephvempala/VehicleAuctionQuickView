namespace AuctionTigerScraper
{
    internal static class Helpers
    {
        public static bool CompareDescription(this Auction currentAuction, Auction newAuction)
        {
            if (currentAuction.Description.GetHashCode() != newAuction.Description.GetHashCode())
                return false;
            return true;
        }
    }
}
