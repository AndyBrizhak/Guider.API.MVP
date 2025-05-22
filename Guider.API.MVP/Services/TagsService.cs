
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

        public async Task<(List<JsonDocument> Documents, long TotalCount)> GetTagsAsync(Dictionary<string, string> filter = null)
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
                        var nameEnFilter = filterBuilder.Regex("name_en", new BsonRegularExpression(q, "i"));
                        var nameSpFilter = filterBuilder.Regex("name_sp", new BsonRegularExpression(q, "i"));
                        var urlFilter = filterBuilder.Regex("url", new BsonRegularExpression(q, "i"));
                        var typeFilter = filterBuilder.Regex("type", new BsonRegularExpression(q, "i"));

                        // Объединяем в один фильтр OR
                        filters.Add(filterBuilder.Or(nameEnFilter, nameSpFilter, urlFilter, typeFilter));
                    }

                    // Фильтр по английскому названию
                    if (filter.TryGetValue("name_en", out string nameEn) && !string.IsNullOrEmpty(nameEn))
                    {
                        filters.Add(filterBuilder.Regex("name_en", new BsonRegularExpression(nameEn, "i")));
                    }

                    // Фильтр по испанскому названию
                    if (filter.TryGetValue("name_sp", out string nameSp) && !string.IsNullOrEmpty(nameSp))
                    {
                        filters.Add(filterBuilder.Regex("name_sp", new BsonRegularExpression(nameSp, "i")));
                    }

                    // Фильтр по URL слагу
                    if (filter.TryGetValue("url", out string url) && !string.IsNullOrEmpty(url))
                    {
                        filters.Add(filterBuilder.Regex("url", new BsonRegularExpression(url, "i")));
                    }

                    // Фильтр по типу тега
                    if (filter.TryGetValue("type", out string type) && !string.IsNullOrEmpty(type))
                    {
                        filters.Add(filterBuilder.Regex("type", new BsonRegularExpression(type, "i")));
                    }

                    // Если есть фильтры, применяем их
                    if (filters.Count > 0)
                    {
                        filterDefinition = filterBuilder.And(filters);
                    }
                }

                // Get total count before applying pagination
                long totalCount = await _tagsCollection.CountDocumentsAsync(filterDefinition);

                // Применяем сортировку
                string sortField = "name_en";
                bool isDescending = false;
                if (filter != null)
                {
                    if (filter.TryGetValue("_sort", out string sort) && !string.IsNullOrEmpty(sort))
                    {
                        sortField = sort;
                    }
                    if (filter.TryGetValue("_order", out string order) && !string.IsNullOrEmpty(order))
                    {
                        isDescending = order.ToUpper() == "DESC";
                    }
                }

                // Set up sort definition
                var sortDefinition = isDescending
                    ? Builders<BsonDocument>.Sort.Descending(sortField)
                    : Builders<BsonDocument>.Sort.Ascending(sortField);

                // Apply pagination if specified in the filter
                IFindFluent<BsonDocument, BsonDocument> query = _tagsCollection.Find(filterDefinition).Sort(sortDefinition);

                if (filter != null)
                {
                    // Parse pagination parameters
                    if (filter.TryGetValue("page", out string pageStr) &&
                        filter.TryGetValue("perPage", out string perPageStr) &&
                        int.TryParse(pageStr, out int page) &&
                        int.TryParse(perPageStr, out int perPage))
                    {
                        // Apply skip and limit for pagination
                        // React Admin's page is 1-based, MongoDB skip is 0-based
                        int skip = (page - 1) * perPage;
                        query = query.Skip(skip).Limit(perPage);
                    }
                }

                var documents = await query.ToListAsync();
                var jsonDocuments = new List<JsonDocument>();
                foreach (var document in documents)
                {
                    jsonDocuments.Add(JsonDocument.Parse(document.ToJson()));
                }

                return (jsonDocuments, totalCount);
            }
            catch (Exception ex)
            {
                return (new List<JsonDocument>
                {
                    JsonDocument.Parse($"{{\"error\": \"An error occurred: {ex.Message}\"}}")
                }, 0);
            }
        }
    }
}