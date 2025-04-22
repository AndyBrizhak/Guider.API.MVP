using Guider.API.MVP.Data;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace Guider.API.MVP.Services
{
    public class TagsService
    {
        private readonly IMongoCollection<BsonDocument> _tagsCollection;
        public TagsService(IOptions<MongoDbSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _tagsCollection = database.GetCollection<BsonDocument>(
                mongoSettings.Value.Collections["Tags"]);
        }

        public async Task<JsonDocument> GetTagsByTypeAsync(string typeName)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("name_en", typeName);
                var projection = Builders<BsonDocument>.Projection.Include("tags").Exclude("_id");
                var result = await _tagsCollection.Find(filter).Project(projection).FirstOrDefaultAsync();

                if (result == null || !result.Contains("tags"))
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Type tags not found or no tags available."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                var tags = result["tags"].AsBsonArray
                    .Select(tag => new
                    {
                        NameEn = tag["name_en"].AsString,
                        NameSp = tag["name_sp"].AsString,
                        Web = tag["web"].AsString
                    })
                    .ToList();

                var successResponse = new
                {
                    IsSuccess = true,
                    Tags = tags
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(successResponse));
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    IsSuccess = false,
                    Message = $"An error occurred: {ex.Message}"
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
            }
        }
    }
}
