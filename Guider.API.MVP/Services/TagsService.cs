


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

        public async Task<(List<object> Tags, long TotalCount, string ErrorMessage)> GetTagsAsync(Dictionary<string, string> filter = null)
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
                var result = new List<object>();

                foreach (var document in documents)
                {
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(document.ToJson());

                        // Получаем ID из документа
                        jsonDoc.RootElement.TryGetProperty("_id", out var idElement);
                        string id = idElement.GetProperty("$oid").GetString();

                        // Получаем английское название
                        string nameEn = string.Empty;
                        if (jsonDoc.RootElement.TryGetProperty("name_en", out var nameEnElement))
                        {
                            nameEn = nameEnElement.GetString();
                        }

                        // Получаем испанское название
                        string nameSp = string.Empty;
                        if (jsonDoc.RootElement.TryGetProperty("name_sp", out var nameSpElement))
                        {
                            nameSp = nameSpElement.GetString();
                        }

                        // Получаем URL из документа
                        string docUrl = string.Empty;
                        if (jsonDoc.RootElement.TryGetProperty("url", out var urlElement))
                        {
                            docUrl = urlElement.GetString();
                        }

                        // Получаем тип тега
                        string tagType = string.Empty;
                        if (jsonDoc.RootElement.TryGetProperty("type", out var typeElement))
                        {
                            tagType = typeElement.GetString();
                        }

                        // Формируем объект в формате для react-admin
                        result.Add(new
                        {
                            id,
                            name_en = nameEn,
                            name_sp = nameSp,
                            url = docUrl,
                            type = tagType
                        });

                        jsonDoc.Dispose();
                    }
                    catch (Exception ex)
                    {
                        return (new List<object>(), 0, $"Error processing tag: {ex.Message}");
                    }
                }

                return (result, totalCount, null);
            }
            catch (Exception ex)
            {
                return (new List<object>(), 0, $"An error occurred: {ex.Message}");
            }
        }

        public async Task<(object Tag, string ErrorMessage)> GetTagByIdAsync(string id)
        {
            try
            {
                // Проверяем валидность ObjectId
                if (!ObjectId.TryParse(id, out ObjectId objectId))
                {
                    return (null, "Invalid tag ID format");
                }

                // Создаем фильтр для поиска по ID
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);

                // Выполняем поиск документа
                var document = await _tagsCollection.Find(filter).FirstOrDefaultAsync();

                if (document == null)
                {
                    return (null, "Tag not found");
                }

                try
                {
                    var jsonDoc = JsonDocument.Parse(document.ToJson());

                    // Получаем ID из документа
                    jsonDoc.RootElement.TryGetProperty("_id", out var idElement);
                    string tagId = idElement.GetProperty("$oid").GetString();

                    // Получаем английское название
                    string nameEn = string.Empty;
                    if (jsonDoc.RootElement.TryGetProperty("name_en", out var nameEnElement))
                    {
                        nameEn = nameEnElement.GetString();
                    }

                    // Получаем испанское название
                    string nameSp = string.Empty;
                    if (jsonDoc.RootElement.TryGetProperty("name_sp", out var nameSpElement))
                    {
                        nameSp = nameSpElement.GetString();
                    }

                    // Получаем URL из документа
                    string docUrl = string.Empty;
                    if (jsonDoc.RootElement.TryGetProperty("url", out var urlElement))
                    {
                        docUrl = urlElement.GetString();
                    }

                    // Получаем тип тега
                    string tagType = string.Empty;
                    if (jsonDoc.RootElement.TryGetProperty("type", out var typeElement))
                    {
                        tagType = typeElement.GetString();
                    }

                    // Формируем объект в требуемом формате
                    var result = new
                    {
                        id = tagId,
                        name_en = nameEn,
                        name_sp = nameSp,
                        url = docUrl,
                        type = tagType
                    };

                    jsonDoc.Dispose();
                    return (result, null);
                }
                catch (Exception ex)
                {
                    return (null, $"Error processing tag: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return (null, $"An error occurred: {ex.Message}");
            }
        }

        public async Task<JsonDocument> AddTagAsync(JsonDocument tagData)
        {
            try
            {
                var tagJson = tagData.RootElement.GetRawText();
                var tagBson = BsonDocument.Parse(tagJson);

                // Проверка обязательных полей
                string nameEn = tagBson.Contains("name_en") && tagBson["name_en"].BsonType == BsonType.String
                    ? tagBson["name_en"].AsString
                    : string.Empty;
                string nameSp = tagBson.Contains("name_sp") && tagBson["name_sp"].BsonType == BsonType.String
                    ? tagBson["name_sp"].AsString
                    : string.Empty;
                string tagType = tagBson.Contains("type") && tagBson["type"].BsonType == BsonType.String
                    ? tagBson["type"].AsString
                    : string.Empty;

                // Проверка на существование тега с таким же названием (английским)
                if (!string.IsNullOrEmpty(nameEn))
                {
                    var existingTagFilter = Builders<BsonDocument>.Filter.Eq("name_en", nameEn);
                    var existingTag = await _tagsCollection.Find(existingTagFilter).FirstOrDefaultAsync();

                    if (existingTag != null)
                    {
                        var errorResponse = new
                        {
                            IsSuccess = false,
                            Message = $"Tag with English name '{nameEn}' already exists."
                        };
                        return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                    }
                }

                // Генерация URL если не указан
                if (!tagBson.Contains("url") || string.IsNullOrEmpty(tagBson["url"].AsString))
                {
                    string urlBase = !string.IsNullOrEmpty(nameEn) ? nameEn : nameSp;
                    if (!string.IsNullOrEmpty(urlBase))
                    {
                        string generatedUrl = urlBase.ToLower()
                            .Replace(" ", "-")
                            .Replace("/", "-")
                            .Replace("-", "-");
                        tagBson["url"] = generatedUrl;
                    }
                }

                // Добавление тега в коллекцию
                await _tagsCollection.InsertOneAsync(tagBson);

                // Получаем только что добавленный тег из базы данных
                var tagId = tagBson["_id"].AsObjectId;
                var filter = Builders<BsonDocument>.Filter.Eq("_id", tagId);
                var addedTag = await _tagsCollection.Find(filter).FirstOrDefaultAsync();

                if (addedTag == null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Tag was added but could not be retrieved from the database."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Формируем объект в нужном формате
                var formattedTag = new
                {
                    id = tagId.ToString(),
                    name_en = addedTag.Contains("name_en") ? addedTag["name_en"].AsString : string.Empty,
                    name_sp = addedTag.Contains("name_sp") ? addedTag["name_sp"].AsString : string.Empty,
                    url = addedTag.Contains("url") ? addedTag["url"].AsString : string.Empty,
                    type = addedTag.Contains("type") ? addedTag["type"].AsString : string.Empty
                };

                var successResponse = new
                {
                    IsSuccess = true,
                    Message = "New Tag has been successfully added.",
                    Data = formattedTag
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