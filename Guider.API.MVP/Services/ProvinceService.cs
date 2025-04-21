namespace Guider.API.MVP.Services
{
    using Guider.API.MVP.Models;
    using MongoDB.Driver;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Text.Json;
    using MongoDB.Bson;
    using Guider.API.MVP.Data;
    using Microsoft.Extensions.Options;
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

        public async Task<List<JsonDocument>> GetAllAsync()
        {
            try
            {
                var documents = await _provinceCollection.Find(_ => true).ToListAsync();
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

        public async Task CreateAsync(JsonDocument province)
        {
            var bsonDocument = BsonDocument.Parse(province.RootElement.ToString());
            await _provinceCollection.InsertOneAsync(bsonDocument);
        }

        public async Task<JsonDocument> UpdateAsync(JsonDocument updatedProvince)
        {
            // Extract the ID from the provided JsonDocument
            if (!updatedProvince.RootElement.TryGetProperty("Id", out var idProperty) || string.IsNullOrEmpty(idProperty.GetString()))
            {
                return JsonDocument.Parse("{\"error\": \"Invalid or missing ID in the provided document.\"}");
            }

            var id = idProperty.GetString();

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

            // Perform the update
            var bsonDocument = BsonDocument.Parse(updatedProvince.RootElement.ToString());
            await _provinceCollection.ReplaceOneAsync(filter, bsonDocument);

            return JsonDocument.Parse("{\"message\": \"Document updated successfully.\"}");
        }

        public async Task<JsonDocument> DeleteAsync(string id)
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

            // Perform the deletion
            var result = await _provinceCollection.DeleteOneAsync(filter);

            if (result.DeletedCount > 0)
            {
                return JsonDocument.Parse("{\"message\": \"Document deleted successfully.\"}");
            }

            return JsonDocument.Parse("{\"error\": \"Failed to delete the document.\"}");
        }
    }
}
