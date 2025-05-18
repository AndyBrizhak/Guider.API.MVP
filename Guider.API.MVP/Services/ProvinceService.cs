
namespace Guider.API.MVP.Services
{
    using Guider.API.MVP.Models;
    using MongoDB.Driver;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Text.Json;
    using MongoDB.Bson;
    using Guider.API.MVP.Data;
    using Microsoft.Extensions.Options;
    using System.Text.Json.Nodes;
    using System.IO;

    public class ProvinceService
    {
        private readonly IMongoCollection<BsonDocument> _provinceCollection;

        public ProvinceService(IOptions<MongoDbSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _provinceCollection = database.GetCollection<BsonDocument>(
                mongoSettings.Value.Collections["Provinces"]);
        }

        public async Task<List<JsonDocument>> GetAllAsync(Dictionary<string, string> filter = null)
        {
            try
            {
                FilterDefinition<BsonDocument> filterDefinition = Builders<BsonDocument>.Filter.Empty;

                // Применяем фильтры, если они переданы
                if (filter != null && filter.Count > 0)
                {
                    var filterBuilder = Builders<BsonDocument>.Filter;
                    var filters = new List<FilterDefinition<BsonDocument>>();

                    // Обработка общего поискового запроса
                    if (filter.TryGetValue("q", out string q) && !string.IsNullOrEmpty(q))
                    {
                        // Создаем фильтр для поиска по нескольким полям
                        var nameFilter = filterBuilder.Regex("name", new BsonRegularExpression(q, "i"));
                        var urlFilter = filterBuilder.Regex("url", new BsonRegularExpression(q, "i"));

                        // Объединяем в один фильтр OR
                        filters.Add(filterBuilder.Or(nameFilter, urlFilter));
                    }

                    // Фильтр по названию провинции
                    if (filter.TryGetValue("name", out string name) && !string.IsNullOrEmpty(name))
                    {
                        filters.Add(filterBuilder.Regex("name", new BsonRegularExpression(name, "i")));
                    }

                    // Фильтр по URL слагу
                    if (filter.TryGetValue("url", out string url) && !string.IsNullOrEmpty(url))
                    {
                        filters.Add(filterBuilder.Regex("url", new BsonRegularExpression(url, "i")));
                    }

                    // Если есть фильтры, применяем их
                    if (filters.Count > 0)
                    {
                        filterDefinition = filterBuilder.And(filters);
                    }
                }

                var documents = await _provinceCollection.Find(filterDefinition).ToListAsync();
                var jsonDocuments = new List<JsonDocument>();

                foreach (var document in documents)
                {
                    jsonDocuments.Add(JsonDocument.Parse(document.ToJson()));
                }

                return jsonDocuments;
            }
            catch (Exception ex)
            {
                return new List<JsonDocument>
                {
                    JsonDocument.Parse($"{{\"error\": \"An error occurred: {ex.Message}\"}}")
                };
            }
        }

        public async Task<JsonDocument> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return JsonDocument.Parse("{\"error\": \"Invalid ID format.\"}");
            }

            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
            var document = await _provinceCollection.Find(filter).FirstOrDefaultAsync();

            if (document == null)
            {
                return JsonDocument.Parse("{\"error\": \"Document with the specified ID does not exist.\"}");
            }

            return JsonDocument.Parse(document.ToJson());
        }

        public async Task<JsonDocument> CreateAsync(JsonDocument province)
        {
            var bsonDocument = BsonDocument.Parse(province.RootElement.ToString());
            await _provinceCollection.InsertOneAsync(bsonDocument);

            // Return the created document with the generated ID
            return JsonDocument.Parse(bsonDocument.ToJson());
        }

        public async Task<JsonDocument> UpdateAsync(string id, JsonDocument updatedProvince)
        {
            // Validate the ID format
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return JsonDocument.Parse("{\"error\": \"Invalid ID format.\"}");
            }

            // Check if the document exists in the collection
            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
            var existingDocument = await _provinceCollection.Find(filter).FirstOrDefaultAsync();

            if (existingDocument == null)
            {
                return JsonDocument.Parse("{\"error\": \"Document with the specified ID does not exist.\"}");
            }

            // Parse and prepare the updated document
            var updatedDoc = updatedProvince.RootElement.ToString();
            var bsonDocument = BsonDocument.Parse(updatedDoc);

            // Ensure the _id field is set correctly
            if (bsonDocument.Contains("_id"))
            {
                bsonDocument.Remove("_id");
            }
            bsonDocument.Add("_id", objectId);

            // Perform the update
            await _provinceCollection.ReplaceOneAsync(filter, bsonDocument);

            // Return the updated document
            return JsonDocument.Parse(bsonDocument.ToJson());
        }

        public async Task<bool> DeleteAsync(string id)
        {
            // Validate the ID format
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return false;
            }

            // Check if the document exists in the collection
            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
            var existingDocument = await _provinceCollection.Find(filter).FirstOrDefaultAsync();

            if (existingDocument == null)
            {
                return false;
            }

            // Perform the deletion
            var result = await _provinceCollection.DeleteOneAsync(filter);

            return result.DeletedCount > 0;
        }
    }
}