using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuctionTigerScraper
{
    public class Vehicle
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Year { get; set; }
        public Registration RegistrationNumber { get; set; }
        public string Fuel { get; set; }
        public string Address { get; set; }
        public int OwnershipStatus { get; set; }
        public string Remarks { get; set; }
        public string Reference { get; set; }
        internal Auction Auction { get; set; }
        internal string PicturesLink { get; set; }
        internal IEnumerable<string> Pictures { get; set; }

        public Vehicle(Guid id, Auction auction, string carname, string number, string fuel, string year, string address,  string status="", string remarks="", string reference="")
        {
            Id = id;
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
        public string GetPictureAsync(int id)
        {
            try
            {
                return Pictures.ElementAt(id);
            }
            catch
            {
                return null;
            }
        }
    }
}
