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
            // Используем коллекцию Places
            _placeCollection = database.GetCollection<BsonDocument>(mongoSettings.Value.Collections["Places"]);
        }

        /// <summary>
        /// Генерирует полные данные для Sitemap:
        /// 1. Ссылки на страницы мест (/place/...)
        /// 2. Ссылки на страницы фильтров (Categories, Provinces, Cities и их комбинации)
        /// </summary>
        public async Task<JsonDocument> GetFullSitemapDataAsync()
        {
            try
            {
                // Итоговый список объектов для Sitemap
                var finalUrls = new List<object>();

                // ==================================================================================
                // ЧАСТЬ 1: Получаем все активные МЕСТА (URL + Date)
                // ==================================================================================
                // Находим все документы со статусом "active"
                var placesFilter = Builders<BsonDocument>.Filter.Eq("status", "active");
                var placesProjection = Builders<BsonDocument>.Projection
                    .Include("url")
                    .Include("updatedAt")
                    .Exclude("_id");

                var placesDocs = await _placeCollection
                    .Find(placesFilter)
                    .Project(placesProjection)
                    .ToListAsync();

                foreach (var doc in placesDocs)
                {
                    if (doc.Contains("url") && !doc["url"].IsBsonNull)
                    {
                        var lastMod = GetDateFromDoc(doc);
                        // Добавляем префикс "place/", чтобы фронтенд отличал их от фильтров
                        finalUrls.Add(new { url = $"place/{doc["url"].AsString}", lastMod });
                    }
                }

                // ==================================================================================
                // ЧАСТЬ 2: Генерируем маршруты ФИЛЬТРОВ через Агрегацию
                // Цель: Найти все существующие комбинации и самую свежую дату для каждой
                // ==================================================================================

                // Словарь для дедупликации: URL -> Самая свежая дата
                // Используем Dictionary, чтобы обновлять дату URL, если встретим более свежую группу
                var filterUrlsMap = new Dictionary<string, DateTime>();

                // 1. $match: Берем только активные
                var matchStage = new BsonDocument("$match", new BsonDocument("status", "active"));

                // 2. $group: Группируем по уникальным сочетаниям: Категория + Провинция + Город
                // И вычисляем MAX updatedAt для каждой такой группы
                var groupStage = new BsonDocument("$group", new BsonDocument
                {
                    { "_id", new BsonDocument
                        {
                            { "cat", "$category" },
                            { "prov", "$address.province" },
                            { "city", "$address.city" }
                        }
                    },
                    { "maxDate", new BsonDocument("$max", "$updatedAt") }
                });

                var pipeline = new[] { matchStage, groupStage };
                var aggregationResult = await _placeCollection.AggregateAsync<BsonDocument>(pipeline);
                var groupedDocs = await aggregationResult.ToListAsync();

                // 3. Перебираем группы и строим все возможные вариации URL
                foreach (var group in groupedDocs)
                {
                    var id = group["_id"].AsBsonDocument;

                    // Безопасное извлечение данных (могут быть null или отсутствовать)
                    string? cat = GetStringSafe(id, "cat");
                    string? prov = GetStringSafe(id, "prov");
                    string? city = GetStringSafe(id, "city");

                    // Дата обновления этой группы (если даты нет в БД, берем текущую или заглушку)
                    DateTime date = group.Contains("maxDate") && !group["maxDate"].IsBsonNull
                        ? group["maxDate"].ToUniversalTime()
                        : DateTime.UtcNow;

                    // --- ГЕНЕРАЦИЯ ВАРИАЦИЙ URL ---
                    // ВАЖНО: Используем Uri.EscapeDataString, так как города могут содержать пробелы (напр. "Playa Hermosa")

                    // 1. Только Категория (?Category=to-eat)
                    if (!string.IsNullOrEmpty(cat))
                    {
                        AddOrUpdateUrl(filterUrlsMap, $"?Category={cat}", date);
                    }

                    // 2. Только Провинция (?Province=guanacaste)
                    if (!string.IsNullOrEmpty(prov))
                    {
                        AddOrUpdateUrl(filterUrlsMap, $"?Province={Uri.EscapeDataString(prov)}", date);
                    }

                    // 3. [НОВОЕ] Только Город (?City=tamarindo)
                    // Генерируется даже если у города есть провинция, так как это валидный маршрут фильтра
                    if (!string.IsNullOrEmpty(city))
                    {
                        AddOrUpdateUrl(filterUrlsMap, $"?City={Uri.EscapeDataString(city)}", date);
                    }

                    // 4. Категория + Провинция
                    if (!string.IsNullOrEmpty(cat) && !string.IsNullOrEmpty(prov))
                    {
                        AddOrUpdateUrl(filterUrlsMap, $"?Category={cat}&Province={Uri.EscapeDataString(prov)}", date);
                    }

                    // 5. Провинция + Город
                    if (!string.IsNullOrEmpty(prov) && !string.IsNullOrEmpty(city))
                    {
                        AddOrUpdateUrl(filterUrlsMap, $"?Province={Uri.EscapeDataString(prov)}&City={Uri.EscapeDataString(city)}", date);
                    }

                    // 6. Категория + Провинция + Город
                    if (!string.IsNullOrEmpty(cat) && !string.IsNullOrEmpty(prov) && !string.IsNullOrEmpty(city))
                    {
                        AddOrUpdateUrl(filterUrlsMap, $"?Category={cat}&Province={Uri.EscapeDataString(prov)}&City={Uri.EscapeDataString(city)}", date);
                    }

                    // 7. Категория + Город (редкий кейс, но технически возможный: ?Category=to-eat&City=Tamarindo)
                    if (!string.IsNullOrEmpty(cat) && !string.IsNullOrEmpty(city))
                    {
                        AddOrUpdateUrl(filterUrlsMap, $"?Category={cat}&City={Uri.EscapeDataString(city)}", date);
                    }
                }

                // Переносим уникальные URL фильтров в итоговый список
                foreach (var kvp in filterUrlsMap)
                {
                    finalUrls.Add(new { url = kvp.Key, lastMod = kvp.Value.ToString("yyyy-MM-dd") });
                }

                // ==================================================================================
                // ФИНАЛ: Возвращаем структуру JSON
                // ==================================================================================
                var result = new
                {
                    success = true,
                    data = finalUrls
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(result));
            }
            catch (Exception ex)
            {
                var errorResult = new
                {
                    success = false,
                    error = $"Sitemap generation error: {ex.Message}"
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(errorResult));
            }
        }

        // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---

        /// <summary>
        /// Добавляет URL в словарь или обновляет дату, если новая дата свежее.
        /// </summary>
        private void AddOrUpdateUrl(Dictionary<string, DateTime> map, string url, DateTime newDate)
        {
            if (!map.ContainsKey(url))
            {
                map[url] = newDate;
            }
            else
            {
                // Если мы нашли этот URL снова (например, из другой группы агрегации),
                // проверяем, не свежее ли там дата. Оставляем самую актуальную.
                if (newDate > map[url])
                {
                    map[url] = newDate;
                }
            }
        }

        private string? GetStringSafe(BsonDocument doc, string fieldName)
        {
            if (doc.Contains(fieldName) && !doc[fieldName].IsBsonNull)
            {
                var val = doc[fieldName].AsString;
                return string.IsNullOrWhiteSpace(val) ? null : val;
            }
            return null;
        }

        private string GetDateFromDoc(BsonDocument doc)
        {
            if (doc.Contains("updatedAt") && !doc["updatedAt"].IsBsonNull)
            {
                return doc["updatedAt"].ToUniversalTime().ToString("yyyy-MM-dd");
            }
            // Дата по умолчанию для старых записей
            return "2024-01-01";
        }
    }
}