using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AuctionTigerScraper.Database_Models
{
    public record AuctionModel
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public DocumentData[] DocumentData { get; set; }
        public ObjectId[] Vehicles { get; set; }
    }
}
