using Guider.API.MVP.Data;
using Guider.API.MVP.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace Guider.API.MVP.Services
{
    public class SitemapService
    {
        private readonly IMongoCollection<BsonDocument> _placeCollection;

        public SitemapService(IOptions<MongoDbSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);

            // Используем ту же коллекцию, что и PlaceService
            _placeCollection = database.GetCollection<BsonDocument>(
                mongoSettings.Value.Collections["Places"]);
        }

        /// <summary>
        /// Получить список URL (слагов) всех активных мест для карты сайта.
        /// </summary>
        public async Task<JsonDocument> GetPlaceSlugsAsync()
        {
            try
            {
                // 1. Фильтр: выбираем только опубликованные/активные места
                // Если у вас есть поле "status", лучше фильтровать по нему.
                // Если нет, используем пустой фильтр: Builders<BsonDocument>.Filter.Empty
                var filter = Builders<BsonDocument>.Filter.Eq("status", "active");

                // Если поля status в старых документах нет, можно использовать $or или просто брать все:
                // var filter = Builders<BsonDocument>.Filter.Empty;

                // 2. Проекция: Нам нужно только поле "url"
                var projection = Builders<BsonDocument>.Projection.Include("url").Exclude("_id");

                // 3. Выполняем запрос к БД
                var bsonDocuments = await _placeCollection
                    .Find(filter)
                    .Project(projection)
                    .ToListAsync();

                // 4. Извлекаем значения слагов в список строк
                var slugs = new List<string>();
                foreach (var doc in bsonDocuments)
                {
                    if (doc.Contains("url") && !doc["url"].IsBsonNull)
                    {
                        slugs.Add(doc["url"].AsString);
                    }
                }

                // 5. Формируем успешный ответ в стиле PlaceService
                var result = new
                {
                    success = true,
                    data = slugs
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(result));
            }
            catch (Exception ex)
            {
                // Обработка ошибок в стиле PlaceService
                var errorResult = new
                {
                    success = false,
                    error = $"An error occurred while retrieving sitemap slugs: {ex.Message}"
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(errorResult));
            }
        }
    }
}
