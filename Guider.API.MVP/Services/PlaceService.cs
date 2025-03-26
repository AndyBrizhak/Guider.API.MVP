namespace Guider.API.MVP.Services
{
    using Guider.API.MVP.Models;
    using Microsoft.AspNetCore.Mvc;
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
        

        // Гео с выводом отсортированного списка с id, distance, name, img_link
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
                },
                {
                    "web", 1
                }
            })
        };

            var result = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return result.ToJson();
        }

        public async Task<string> GetNearbyPlacesAsyncCenter(decimal latitude, decimal longitude, int radiusMeters, int limit)
        {
            var pipeline = new[]
            {
            new BsonDocument("$geoNear", new BsonDocument
            {
                ///*{ "near", new BsonDocument { { "type", "Point" }, { "coordinates", new BsonArray { longitude, latitude } }*/ } },
                { "near", new BsonArray { longitude, latitude } },  // Массив координат
                { "distanceField", "distance" },
                { "maxDistance", radiusMeters },
                { "spherical", true },
                //{ "limit", limit }
            })
            ,
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 1 },
                { "category", 1 },
                { "name", 1 },
                { "location.coordinates", 1 },
                {
                    "web", 1
                }

            }),
            new BsonDocument("$limit", limit) // Ограничение результата
            };

            var results = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            if (results.Count == 0)
            {
                return "[]"; // Возвращаем пустой массив
            }
            return results.ToJson();  // Возвращаем JSON-строку
        }

        

        public async Task<string> GetPlacesNearbyByCategoryByTagsAsyncAsync(decimal lat, decimal lng, int maxDistanceMeters, string? category = null, List<string>? filterTags = null)
        {
            var geoNearStage = new BsonDocument("$geoNear", new BsonDocument
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
        });

            var projectStage = new BsonDocument("$project", new BsonDocument
        {
            { "_id", 1 },
            { "distance", 1 },
            { "name", 1 },
            { "img_link", new BsonDocument { { "$arrayElemAt", new BsonArray { "$img_link", 0 } } } },
            { "web", 1 },
            { "category", 1 },
            { "tags", 1 }
        });

            var pipeline = new[] { geoNearStage, projectStage };

            var results = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            if (!string.IsNullOrEmpty(category))
            {
                results = results.Where(doc => System.Text.RegularExpressions.Regex.IsMatch(doc["category"].AsString, category, System.Text.RegularExpressions.RegexOptions.IgnoreCase)).ToList();
            }

            if (filterTags != null && filterTags.Any())
            {
                results = results.Where(doc =>
                    doc.Contains("tags") && doc["tags"].IsBsonArray &&
                    doc["tags"].AsBsonArray.Any(tag => filterTags.Any(filterTag =>
                        System.Text.RegularExpressions.Regex.IsMatch(tag.AsString, filterTag, System.Text.RegularExpressions.RegexOptions.IgnoreCase)))).ToList();
            }

            return results.ToJson();
        }
    }
}

