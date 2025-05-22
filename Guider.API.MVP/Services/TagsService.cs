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

       
    }
}
