namespace Guider.API.MVP.Services
{
    using Guider.API.MVP.Models;
    using Microsoft.Extensions.Options;
    using MongoDB.Bson;
    using MongoDB.Driver;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    public class PlaceService
    {
        private readonly IMongoCollection<BsonDocument> _placeCollection;

        public PlaceService(IOptions<MongoDbSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _placeCollection = database.GetCollection<BsonDocument>(mongoSettings.Value.CollectionName);
        }

        public async Task<List<BsonDocument>> GetAllAsync() =>
            await _placeCollection.Find(_ => true).ToListAsync();

        public async Task<BsonDocument?> GetByIdAsync(string id) =>
        await _placeCollection.Find(b => b["_id"] == ObjectId.Parse(id)).FirstOrDefaultAsync();

        public async Task CreateAsync(BsonDocument place) =>
            await _placeCollection.InsertOneAsync(place);

        //public async Task UpdateAsync(string id, BsonDocument updatedPlace)
        //{
        //    var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id));
        //    return await _placeCollection.ReplaceOneAsync(filter, updatedPlace);
        //}

        //public async Task DeleteAsync(string id)
        //{
        //    var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id));
        //    return await _placeCollection.DeleteOneAsync(filter);
        //}

        public async Task<string> GetPlacesNearbyAsync(decimal lat, decimal lng, int maxDistanceMeters)
        {
            var pipeline = new[]
            {
            new BsonDocument("$geoNear", new BsonDocument
            {
                { "near", new BsonDocument
                    {
                        { "type", "Point" },
                        { "coordinates", new BsonArray { lng, lat } }
                    }
                },
                { "distanceField", "distance" },
                { "maxDistance", maxDistanceMeters },
                { "spherical", true }
            }),
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 1 },
                { "distance", 1 },
                { "name", 1 },
                { "img_link", new BsonDocument
                    {
                        { "$arrayElemAt", new BsonArray { "$img_link", 0 } } // Первая ссылка на изображение
                    }
                }
            })
        };

            var result = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return result.ToJson();
        }
    }
}
