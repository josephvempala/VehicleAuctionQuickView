using System.Text.Json;
namespace AuctionTigerScraper
{
    public class Vehicle
    {
        public string Name { get; set; }
        public int Year { get; set; }
        public Registration RegistrationNumber { get; set; }
        public string Fuel { get; set; }
        public string Address { get; set; }
        public int OwnershipStatus { get; set; }
        public string Remarks { get; set; }
        public string Reference { get; set; }
        internal Auction Auction { get; set; }

        public Vehicle(Auction auction, string carname, string number, string fuel, string year, string address, string status="", string remarks="", string reference="")
        {
            Auction = auction;
            Name = carname;
            RegistrationNumber = new Registration(number);
            Fuel = fuel;
            _ = int.TryParse(year, out int _year);
            Year = _year;
            Address = address;
            _ = int.TryParse(status, out int _status);
            OwnershipStatus = _status;
            Remarks = remarks;
            Reference = reference;
        }
    }
}
