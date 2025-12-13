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
                var filter = Builders<BsonDocument>.Filter.Eq("status", "active");

                // 1.  Добавляем "updatedAt" в проекцию
                var projection = Builders<BsonDocument>.Projection
                    .Include("url")
                    .Include("updatedAt") // <-- Добавили поле
                    .Exclude("_id");

                var bsonDocuments = await _placeCollection
                    .Find(filter)
                    .Project(projection)
                    .ToListAsync();

                // 2.  Формируем список анонимных объектов вместо простого списка строк
                var sitemapData = new List<object>();

                foreach (var doc in bsonDocuments)
                {
                    if (doc.Contains("url") && !doc["url"].IsBsonNull)
                    {
                        string url = doc["url"].AsString;
                        string lastModDate = DateTime.UtcNow.ToString("yyyy-MM-dd"); // Значение по умолчанию

                        // Проверяем, есть ли дата в документе
                        if (doc.Contains("updatedAt") && !doc["updatedAt"].IsBsonNull)
                        {
                            // Конвертируем BsonDateTime в строку формата W3C (YYYY-MM-DD)
                            // BsonDateTime приводится к C# DateTime через .ToUniversalTime()
                            lastModDate = doc["updatedAt"].ToUniversalTime().ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            // ЛАЙФХАК: Если у старых документов нет даты, 
                            // можно временно отдавать фиксированную дату или текущую,
                            // пока вы их не пересохраните.
                            lastModDate = "2024-01-01";
                        }

                        // Добавляем объект в список
                        sitemapData.Add(new { url = url, lastMod = lastModDate });
                    }
                }

                // 3. Возвращаем структуру
                var result = new
                {
                    success = true,
                    data = sitemapData // Теперь это массив объектов
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(result));
            }
            catch (Exception ex)
            {
                var errorResult = new
                {
                    success = false,
                    error = $"Error: {ex.Message}"
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(errorResult));
            }
        }
    }
}
