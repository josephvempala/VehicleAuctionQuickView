using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AuctionTigerScraper.Database_Models
{
    public record VehicleModel
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public int Year { get; set; }
        public string RegistrationNumber { get; set; }
        public string Fuel { get; set; }
        public string Address { get; set; }
        public int OwnershipStatus { get; set; }
        public string Remarks { get; set; }
        public string Reference { get; set; }
        public string PicturesDirectory { get; set; }
        public int PicturesCount { get; set; }
        public string AuctionName { get; set; }
        public bool isActive { get; set; }
    }
}
