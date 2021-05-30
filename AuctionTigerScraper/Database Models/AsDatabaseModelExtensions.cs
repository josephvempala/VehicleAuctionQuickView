using MongoDB.Bson;
using System.Collections.Generic;
using System.Linq;

namespace AuctionTigerScraper.Database_Models
{
    public static class AsDatabaseModelExtensions
    {
        public static VehicleModel AsDatabaseModel(this Vehicle vehicle)
        {
            return new VehicleModel
            {
                Id = ObjectId.GenerateNewId(),
                AuctionName = vehicle.Auction.Name,
                Name = vehicle.Name,
                Year = vehicle.Year,
                RegistrationNumber = vehicle.RegistrationNumber.ToString(),
                Fuel = vehicle.Fuel,
                Address = vehicle.Address,
                OwnershipStatus = vehicle.OwnershipStatus,
                Remarks = vehicle.Remarks,
                Reference = vehicle.Reference,
                PicturesDirectory = vehicle.PicturesDirectory,
                PicturesCount = vehicle.PicturesCount,
                isActive = true
            };
        }
        public static AuctionModel AsDatabaseModel(this Auction auction, IEnumerable<VehicleModel> vehicles)
        {
            return new AuctionModel
            {
                Id = ObjectId.GenerateNewId(),
                Name = auction.Name,
                Link = auction.Link,
                Description = auction.Description,
                DocumentData = auction.DocumentData,
                Vehicles = vehicles.Select(item => item.Id).ToArray()
            };
        }
    }
}

