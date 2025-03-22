namespace Guider.API.MVP.Services
{
    using Guider.API.MVP.Models;
    using MongoDB.Bson;
    using MongoDB.Driver;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    public class PlaceService
    {
        private readonly IMongoCollection<BsonDocument> _placeCollection;

        public PlaceService(IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase("guider"); 
            _placeCollection = database.GetCollection<BsonDocument>("places_clear");
        }

        public async Task<List<BsonDocument>> GetAllAsync() =>
            await _placeCollection.Find(_ => true).ToListAsync();

        public async Task<BsonDocument?> GetByIdAsync(string id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(id));
            return await _placeCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(BsonDocument place) =>
            await _placeCollection.InsertOneAsync(place);

        public async Task UpdateAsync(string id, BsonDocument updatedPlace)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(id));
            await _placeCollection.ReplaceOneAsync(filter, updatedPlace);
        }

        public async Task DeleteAsync(string id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(id));
            await _placeCollection.DeleteOneAsync(filter);
        }
    }
}
