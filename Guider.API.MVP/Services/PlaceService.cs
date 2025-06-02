namespace Guider.API.MVP.Services
{
    using Guider.API.MVP.Data;
    using Guider.API.MVP.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using MongoDB.Bson;
    using MongoDB.Driver;
    using MongoDB.Driver.GeoJsonObjectModel;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    public class PlaceService
    {
        private readonly IMongoCollection<BsonDocument> _placeCollection;

        public PlaceService(IOptions<MongoDbSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);

            // Old configuration
            //_placeCollection = database.GetCollection<BsonDocument>(mongoSettings.Value.CollectionName);

            // New configuration

            _placeCollection = database.GetCollection<BsonDocument>(
                mongoSettings.Value.Collections["Places"]);
        }

       
        /// Получить все документы из коллекции Places
        public async Task<List<BsonDocument>> GetAllAsync() =>
            await _placeCollection.Find(_ => true).ToListAsync();



        /// Получить документы из коллекции с пагинацией
        /// <param name="pageNumber">Номер страницы</param>
        /// <param name="pageSize">Размер страницы</param>
        /// <returns></returns>
        //public async Task<(List<JsonDocument> Documents, long TotalCount)> GetAllPagedAsync(int pageNumber, int pageSize)
        //{
        //    try
        //    {
        //        FilterDefinition<BsonDocument> filterDefinition = Builders<BsonDocument>.Filter.Empty;
        //        // Get total count before applying pagination
        //        long totalCount = await _placeCollection.CountDocumentsAsync(filterDefinition);
        //        // Применяем сортировку по названию
        //        var sortDefinition = Builders<BsonDocument>.Sort.Ascending("name");
        //        // Apply pagination
        //        IFindFluent<BsonDocument, BsonDocument> query = _placeCollection.Find(filterDefinition).Sort(sortDefinition);
        //        // Apply skip and limit for pagination
        //        int skip = (pageNumber - 1) * pageSize;
        //        query = query.Skip(skip).Limit(pageSize);
        //        var documents = await query.ToListAsync();
        //        var jsonDocuments = new List<JsonDocument>();

        //        foreach (var document in documents)
        //        {
        //            // Преобразуем ObjectId в строку для поля id
        //            var json = document.ToJson();
        //            var originalJsonDoc = JsonDocument.Parse(json);

        //            // Создаем глубокую копию с модификацией прямо здесь
        //            using var stream = new MemoryStream();
        //            using var writer = new Utf8JsonWriter(stream);

        //            writer.WriteStartObject();

        //            string idValue = null;
        //            var otherProperties = new List<JsonProperty>();

        //            // Собираем все свойства и извлекаем id
        //            foreach (var property in originalJsonDoc.RootElement.EnumerateObject())
        //            {
        //                if (property.Name == "_id")
        //                {
        //                    // Извлекаем значение ObjectId
        //                    if (property.Value.TryGetProperty("$oid", out var oidElement))
        //                    {
        //                        idValue = oidElement.GetString();
        //                    }
        //                }
        //                else
        //                {
        //                    otherProperties.Add(property);
        //                }
        //            }

        //            // Записываем id первым, если он найден
        //            if (!string.IsNullOrEmpty(idValue))
        //            {
        //                writer.WriteString("id", idValue);
        //            }

        //            // Записываем остальные свойства
        //            foreach (var property in otherProperties)
        //            {
        //                property.WriteTo(writer);
        //            }

        //            writer.WriteEndObject();
        //            writer.Flush();

        //            var modifiedJson = Encoding.UTF8.GetString(stream.ToArray());
        //            jsonDocuments.Add(JsonDocument.Parse(modifiedJson));

        //            // Освобождаем ресурсы исходного документа
        //            originalJsonDoc.Dispose();
        //        }

        //        return (jsonDocuments, totalCount);
        //    }
        //    catch (Exception ex)
        //    {
        //        return (new List<JsonDocument>
        //        {
        //            JsonDocument.Parse($"{{\"error\": \"An error occurred: {ex.Message}\"}}")
        //        }, 0);
        //    }
        //}

        //public async Task<(List<JsonDocument> Documents, long TotalCount)> GetAllPagedAsync(int pageNumber, int pageSize)
        //{
        //    try
        //    {
        //        FilterDefinition<BsonDocument> filterDefinition = Builders<BsonDocument>.Filter.Empty;

        //        // Get total count before applying pagination
        //        long totalCount = await _placeCollection.CountDocumentsAsync(filterDefinition);

        //        // Применяем сортировку по названию
        //        var sortDefinition = Builders<BsonDocument>.Sort.Ascending("name");

        //        // Apply pagination
        //        IFindFluent<BsonDocument, BsonDocument> query = _placeCollection.Find(filterDefinition).Sort(sortDefinition);

        //        // Apply skip and limit for pagination
        //        int skip = (pageNumber - 1) * pageSize;
        //        query = query.Skip(skip).Limit(pageSize);

        //        var documents = await query.ToListAsync();
        //        var jsonDocuments = new List<JsonDocument>();

        //        foreach (var document in documents)
        //        {
        //            try
        //            {
        //                var processedDocument = ProcessDocument(document);
        //                jsonDocuments.Add(processedDocument);
        //            }
        //            catch (Exception docEx)
        //            {
        //                // Логируем ошибку обработки конкретного документа, но продолжаем обработку остальных
        //                // _logger?.LogWarning($"Error processing document {document.GetValue("_id", "unknown")}: {docEx.Message}");

        //                // Добавляем документ с ошибкой для отладки
        //                var errorDoc = JsonDocument.Parse($"{{\"id\": \"{GetDocumentId(document)}\", \"error\": \"Document processing failed: {docEx.Message}\"}}");
        //                jsonDocuments.Add(errorDoc);
        //            }
        //        }

        //        return (jsonDocuments, totalCount);
        //    }
        //    catch (Exception ex)
        //    {
        //        return (new List<JsonDocument>
        //        {
        //            JsonDocument.Parse($"{{\"error\": \"An error occurred: {ex.Message}\"}}")
        //        }, 0);
        //    }
        //}

        public async Task<JsonDocument> GetPlacesAsync(Dictionary<string, string> filter = null)
        {
            try
            {
                FilterDefinition<BsonDocument> filterDefinition = Builders<BsonDocument>.Filter.Empty;
                if (filter != null && filter.Count > 0)
                {
                    var filterBuilder = Builders<BsonDocument>.Filter;
                    var filters = new List<FilterDefinition<BsonDocument>>();

                    // Общий текстовый поиск по нескольким полям
                    if (filter.TryGetValue("q", out string q) && !string.IsNullOrEmpty(q))
                    {
                        filters.Add(filterBuilder.Or(
                            filterBuilder.Regex("name", new BsonRegularExpression(q, "i")),
                            filterBuilder.Regex("province", new BsonRegularExpression(q, "i")),
                            filterBuilder.Regex("city", new BsonRegularExpression(q, "i")),
                            filterBuilder.Regex("url", new BsonRegularExpression(q, "i"))
                        ));
                    }

                    // Фильтр по провинции
                    if (filter.TryGetValue("province", out string province) && !string.IsNullOrEmpty(province))
                    {
                        filters.Add(filterBuilder.Regex("address.province", new BsonRegularExpression(province, "i")));
                    }

                    // Фильтр по городу
                    if (filter.TryGetValue("city", out string city) && !string.IsNullOrEmpty(city))
                    {
                        filters.Add(filterBuilder.Regex("address.city", new BsonRegularExpression(city, "i")));
                    }

                    // Фильтр по названию заведения
                    if (filter.TryGetValue("name", out string name) && !string.IsNullOrEmpty(name))
                    {
                        filters.Add(filterBuilder.Regex("name", new BsonRegularExpression(name, "i")));
                    }

                    // Фильтр по URL
                    if (filter.TryGetValue("url", out string url) && !string.IsNullOrEmpty(url))
                    {
                        filters.Add(filterBuilder.Regex("url", new BsonRegularExpression(url, "i")));
                    }

                    if (filters.Count > 0)
                    {
                        filterDefinition = filterBuilder.And(filters);
                    }
                }

                long totalCount = await _placeCollection.CountDocumentsAsync(filterDefinition);

                // Сортировка
                string sortField = "name";
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

                var sortDefinition = isDescending
                    ? Builders<BsonDocument>.Sort.Descending(sortField)
                    : Builders<BsonDocument>.Sort.Ascending(sortField);

                // Пагинация
                IFindFluent<BsonDocument, BsonDocument> query = _placeCollection.Find(filterDefinition).Sort(sortDefinition);
                if (filter != null)
                {
                    if (filter.TryGetValue("page", out string pageStr) &&
                        filter.TryGetValue("perPage", out string perPageStr) &&
                        int.TryParse(pageStr, out int page) &&
                        int.TryParse(perPageStr, out int perPage))
                    {
                        int skip = (page - 1) * perPage;
                        query = query.Skip(skip).Limit(perPage);
                    }
                }

                var documents = await query.ToListAsync();

                // Формирование массива мест с корректным форматом id
                var placesList = new List<object>();
                foreach (var document in documents)
                {
                    var jsonString = document.ToJson();
                    var jsonDoc = JsonDocument.Parse(jsonString);

                    // Преобразуем весь документ в словарь
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);

                    // Изменяем формат идентификатора
                    if (dict.ContainsKey("_id"))
                    {
                        var idObj = dict["_id"] as JsonElement?;
                        if (idObj.HasValue && idObj.Value.ValueKind == JsonValueKind.Object)
                        {
                            if (idObj.Value.TryGetProperty("$oid", out var oidElement))
                            {
                                dict["id"] = oidElement.GetString();
                            }
                        }
                        dict.Remove("_id");
                    }

                    placesList.Add(dict);
                    jsonDoc.Dispose();
                }

                // Формирование результирующего JSON документа
                var result = new
                {
                    success = true,
                    data = new
                    {
                        totalCount = totalCount,
                        places = placesList
                    }
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(result));
            }
            catch (Exception ex)
            {
                var errorResult = new
                {
                    success = false,
                    error = $"An error occurred: {ex.Message}"
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(errorResult));
            }
        }


        //private JsonDocument ProcessDocument(BsonDocument document)
        //{
        //    var json = document.ToJson();
        //    var originalJsonDoc = JsonDocument.Parse(json);

        //    using var stream = new MemoryStream();
        //    using var writer = new Utf8JsonWriter(stream);

        //    writer.WriteStartObject();

        //    string idValue = null;
        //    var processedProperties = new HashSet<string> { "_id" }; // Отслеживаем обработанные свойства

        //    // Извлекаем и обрабатываем _id
        //    foreach (var property in originalJsonDoc.RootElement.EnumerateObject())
        //    {
        //        if (property.Name == "_id")
        //        {
        //            idValue = ExtractObjectId(property.Value);
        //            break;
        //        }
        //    }

        //    // Записываем id первым, если он найден
        //    if (!string.IsNullOrEmpty(idValue))
        //    {
        //        writer.WriteString("id", idValue);
        //    }

        //    // Обрабатываем все остальные свойства с учетом специфики схемы
        //    foreach (var property in originalJsonDoc.RootElement.EnumerateObject())
        //    {
        //        if (processedProperties.Contains(property.Name))
        //            continue;

        //        ProcessProperty(writer, property);
        //        processedProperties.Add(property.Name);
        //    }

        //    writer.WriteEndObject();
        //    writer.Flush();

        //    var modifiedJson = Encoding.UTF8.GetString(stream.ToArray());
        //    var result = JsonDocument.Parse(modifiedJson);

        //    // Освобождаем ресурсы исходного документа
        //    originalJsonDoc.Dispose();

        //    return result;
        //}

        //private string ExtractObjectId(JsonElement idElement)
        //{
        //    // Обработка ObjectId согласно схеме
        //    if (idElement.ValueKind == JsonValueKind.Object && idElement.TryGetProperty("$oid", out var oidElement))
        //    {
        //        return oidElement.GetString();
        //    }

        //    // Fallback для других форматов ObjectId
        //    if (idElement.ValueKind == JsonValueKind.String)
        //    {
        //        return idElement.GetString();
        //    }

        //    return null;
        //}

        //private string GetDocumentId(BsonDocument document)
        //{
        //    try
        //    {
        //        if (document.Contains("_id"))
        //        {
        //            var idValue = document["_id"];
        //            if (idValue.IsObjectId)
        //            {
        //                return idValue.AsObjectId.ToString();
        //            }
        //            return idValue.ToString();
        //        }
        //    }
        //    catch
        //    {
        //        // Игнорируем ошибки извлечения ID
        //    }

        //    return "unknown";
        //}

        //private void ProcessProperty(Utf8JsonWriter writer, JsonProperty property)
        //{
        //    switch (property.Name)
        //    {
        //        // Обработка полей с особой логикой согласно схеме
        //        case "phone":
        //            ProcessPhoneProperty(writer, property);
        //            break;

        //        case "owner":
        //            ProcessOwnerProperty(writer, property);
        //            break;

        //        case "description":
        //            ProcessDescriptionProperty(writer, property);
        //            break;

        //        case "location":
        //            ProcessLocationProperty(writer, property);
        //            break;

        //        // Обработка полей с числовыми значениями MongoDB
        //        case var name when ContainsMongoNumbers(property.Value):
        //            ProcessPropertyWithMongoNumbers(writer, property);
        //            break;

        //        default:
        //            // Стандартная обработка для остальных полей
        //            property.WriteTo(writer);
        //            break;
        //    }
        //}

        //private void ProcessPhoneProperty(Utf8JsonWriter writer, JsonProperty property)
        //{
        //    writer.WritePropertyName(property.Name);

        //    if (property.Value.ValueKind == JsonValueKind.Object)
        //    {
        //        writer.WriteStartObject();

        //        foreach (var phoneField in property.Value.EnumerateObject())
        //        {
        //            writer.WritePropertyName(phoneField.Name);

        //            // Обработка различных форматов телефонных номеров
        //            if (phoneField.Value.ValueKind == JsonValueKind.Object &&
        //                phoneField.Value.TryGetProperty("$numberLong", out var numberLong))
        //            {
        //                // Конвертируем $numberLong в строку
        //                writer.WriteStringValue(numberLong.GetString());
        //            }
        //            else if (phoneField.Value.ValueKind == JsonValueKind.Object &&
        //                     phoneField.Value.TryGetProperty("$numberDouble", out var numberDouble))
        //            {
        //                // Обработка $numberDouble
        //                writer.WriteStringValue(numberDouble.GetString());
        //            }
        //            else
        //            {
        //                phoneField.Value.WriteTo(writer);
        //            }
        //        }

        //        writer.WriteEndObject();
        //    }
        //    else
        //    {
        //        property.Value.WriteTo(writer);
        //    }
        //}

        //private void ProcessOwnerProperty(Utf8JsonWriter writer, JsonProperty property)
        //{
        //    // owner может быть объектом или массивом согласно схеме
        //    writer.WritePropertyName(property.Name);

        //    if (property.Value.ValueKind == JsonValueKind.Array)
        //    {
        //        writer.WriteStartArray();
        //        foreach (var ownerItem in property.Value.EnumerateArray())
        //        {
        //            ProcessOwnerObject(writer, ownerItem);
        //        }
        //        writer.WriteEndArray();
        //    }
        //    else if (property.Value.ValueKind == JsonValueKind.Object)
        //    {
        //        ProcessOwnerObject(writer, property.Value);
        //    }
        //    else
        //    {
        //        property.Value.WriteTo(writer);
        //    }
        //}

        //private void ProcessOwnerObject(Utf8JsonWriter writer, JsonElement ownerElement)
        //{
        //    writer.WriteStartObject();

        //    foreach (var ownerField in ownerElement.EnumerateObject())
        //    {
        //        writer.WritePropertyName(ownerField.Name);

        //        // phone в owner может быть строкой или числом
        //        if (ownerField.Name == "phone")
        //        {
        //            if (ownerField.Value.ValueKind == JsonValueKind.Number)
        //            {
        //                writer.WriteStringValue(ownerField.Value.GetInt64().ToString());
        //            }
        //            else
        //            {
        //                ownerField.Value.WriteTo(writer);
        //            }
        //        }
        //        else
        //        {
        //            ownerField.Value.WriteTo(writer);
        //        }
        //    }

        //    writer.WriteEndObject();
        //}

        //private void ProcessDescriptionProperty(Utf8JsonWriter writer, JsonProperty property)
        //{
        //    // description может быть строкой или массивом строк
        //    writer.WritePropertyName(property.Name);
        //    property.Value.WriteTo(writer);
        //}

        //private void ProcessLocationProperty(Utf8JsonWriter writer, JsonProperty property)
        //{
        //    writer.WritePropertyName(property.Name);

        //    if (property.Value.ValueKind == JsonValueKind.Object)
        //    {
        //        writer.WriteStartObject();

        //        foreach (var locationField in property.Value.EnumerateObject())
        //        {
        //            writer.WritePropertyName(locationField.Name);

        //            if (locationField.Name == "coordinates" && locationField.Value.ValueKind == JsonValueKind.Array)
        //            {
        //                writer.WriteStartArray();
        //                foreach (var coordinate in locationField.Value.EnumerateArray())
        //                {
        //                    // Обработка координат с учетом MongoDB Double типов
        //                    if (coordinate.ValueKind == JsonValueKind.Object &&
        //                        coordinate.TryGetProperty("$numberDouble", out var doubleValue))
        //                    {
        //                        if (double.TryParse(doubleValue.GetString(), out var parsedDouble))
        //                        {
        //                            writer.WriteNumberValue(parsedDouble);
        //                        }
        //                        else
        //                        {
        //                            // Обработка специальных значений (Infinity, -Infinity, NaN)
        //                            writer.WriteStringValue(doubleValue.GetString());
        //                        }
        //                    }
        //                    else
        //                    {
        //                        coordinate.WriteTo(writer);
        //                    }
        //                }
        //                writer.WriteEndArray();
        //            }
        //            else
        //            {
        //                locationField.Value.WriteTo(writer);
        //            }
        //        }

        //        writer.WriteEndObject();
        //    }
        //    else
        //    {
        //        property.Value.WriteTo(writer);
        //    }
        //}

        //private bool ContainsMongoNumbers(JsonElement element)
        //{
        //    if (element.ValueKind == JsonValueKind.Object)
        //    {
        //        return element.TryGetProperty("$numberLong", out _) ||
        //               element.TryGetProperty("$numberDouble", out _) ||
        //               element.TryGetProperty("$numberInt", out _);
        //    }

        //    if (element.ValueKind == JsonValueKind.Array)
        //    {
        //        foreach (var item in element.EnumerateArray())
        //        {
        //            if (ContainsMongoNumbers(item))
        //                return true;
        //        }
        //    }

        //    return false;
        //}

        //private void ProcessPropertyWithMongoNumbers(Utf8JsonWriter writer, JsonProperty property)
        //{
        //    writer.WritePropertyName(property.Name);
        //    ProcessElementWithMongoNumbers(writer, property.Value);
        //}

        //private void ProcessElementWithMongoNumbers(Utf8JsonWriter writer, JsonElement element)
        //{
        //    switch (element.ValueKind)
        //    {
        //        case JsonValueKind.Object:
        //            if (element.TryGetProperty("$numberLong", out var numberLong))
        //            {
        //                if (long.TryParse(numberLong.GetString(), out var longValue))
        //                {
        //                    writer.WriteNumberValue(longValue);
        //                }
        //                else
        //                {
        //                    writer.WriteStringValue(numberLong.GetString());
        //                }
        //            }
        //            else if (element.TryGetProperty("$numberDouble", out var numberDouble))
        //            {
        //                var doubleStr = numberDouble.GetString();
        //                if (double.TryParse(doubleStr, out var doubleValue))
        //                {
        //                    writer.WriteNumberValue(doubleValue);
        //                }
        //                else
        //                {
        //                    // Обработка специальных значений
        //                    writer.WriteStringValue(doubleStr);
        //                }
        //            }
        //            else if (element.TryGetProperty("$numberInt", out var numberInt))
        //            {
        //                if (int.TryParse(numberInt.GetString(), out var intValue))
        //                {
        //                    writer.WriteNumberValue(intValue);
        //                }
        //                else
        //                {
        //                    writer.WriteStringValue(numberInt.GetString());
        //                }
        //            }
        //            else
        //            {
        //                writer.WriteStartObject();
        //                foreach (var property in element.EnumerateObject())
        //                {
        //                    writer.WritePropertyName(property.Name);
        //                    ProcessElementWithMongoNumbers(writer, property.Value);
        //                }
        //                writer.WriteEndObject();
        //            }
        //            break;

        //        case JsonValueKind.Array:
        //            writer.WriteStartArray();
        //            foreach (var item in element.EnumerateArray())
        //            {
        //                ProcessElementWithMongoNumbers(writer, item);
        //            }
        //            writer.WriteEndArray();
        //            break;

        //        default:
        //            element.WriteTo(writer);
        //            break;
        //    }
        //}


        /// <summary>
        /// Получить объект по идентификатору
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<JsonDocument> GetByIdAsync(string id)
        {
            try
            {
                // Convert string ID to MongoDB ObjectId
                if (!ObjectId.TryParse(id, out ObjectId objectId))
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Invalid object ID format."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Create filter by ObjectId and execute query
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var place = await _placeCollection.Find(filter).FirstOrDefaultAsync();

                if (place == null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"Object with ID '{id}' not found."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Extract coordinates if available
                double latitude = 0.0;
                double longitude = 0.0;
                if (place.Contains("location") &&
                    place["location"].IsBsonDocument &&
                    place["location"].AsBsonDocument.Contains("coordinates") &&
                    place["location"]["coordinates"].IsBsonArray)
                {
                    var coordinates = place["location"]["coordinates"].AsBsonArray;
                    if (coordinates.Count >= 2)
                    {
                        longitude = coordinates[0].AsDouble;
                        latitude = coordinates[1].AsDouble;
                    }
                }

                // Create a copy of the place document without the _id field for cleaner response
                var placeCopy = place.DeepClone().AsBsonDocument;
                placeCopy.Remove("_id");

                // If location exists but we also want to expose latitude and longitude directly
                if (placeCopy.Contains("location") && !placeCopy.Contains("latitude") && !placeCopy.Contains("longitude"))
                {
                    placeCopy["latitude"] = latitude;
                    placeCopy["longitude"] = longitude;
                }

                // Формируем корректный JSON для успешного ответа
                var responseDoc = new BsonDocument
        {
            { "IsSuccess", true },
            { "Place", placeCopy },
            { "Id", id }
        };

                return JsonDocument.Parse(responseDoc.ToJson());
            }
            catch (FormatException)
            {
                var errorResponse = new
                {
                    IsSuccess = false,
                    Message = "Invalid object ID format."
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
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

        /// <summary>
        /// Получить документ по локальнуому url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<BsonDocument> GetPlaceByWebAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null; 
            }

            var filter = Builders<BsonDocument>.Filter.Eq("url", url);
            return await _placeCollection.Find(filter).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Получить объект по идентификатору из параметров запроса
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<BsonDocument?> GetPlaceByIdFromHeaderAsync(string id)
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return null; 
            }

            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
            return await _placeCollection.Find(filter).FirstOrDefaultAsync();
        }

              

     /// Создать новый документ в коллекции Places  
        /// </summary>  
        /// <param name="jsonDocument">JSON-строка документа</param>  
        /// <returns>Созданный документ в формате JSON</returns>  
        public async Task<JsonDocument> CreateAsync(JsonDocument jsonDocument)
        {
            try
            {
                var jsonString = jsonDocument.RootElement.GetRawText();

                var document = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(jsonString);

                // Check for unique name  
                if (document.Contains("name"))
                {
                    var nameFilter = Builders<BsonDocument>.Filter.Eq("name", document["name"].AsString);
                    if (document.Contains("address.city") && document.Contains("address.province"))
                    {
                        nameFilter = Builders<BsonDocument>.Filter.And(
                            nameFilter,
                            Builders<BsonDocument>.Filter.Eq("address.city", document["address.city"].AsString),
                            Builders<BsonDocument>.Filter.Eq("address.province", document["address.province"].AsString)
                        );
                    }
                    var existingNameDocument = await _placeCollection.Find(nameFilter).FirstOrDefaultAsync();
                    if (existingNameDocument != null)
                    {
                        return JsonDocument.Parse(JsonSerializer.Serialize(new { success = false, message = "The 'name' field must be unique within the same city and province." }));
                    }
                }

                // Check for unique url  
                if (document.Contains("url"))
                {
                    var webFilter = Builders<BsonDocument>.Filter.Eq("url", document["url"].AsString);
                    var existingWebDocument = await _placeCollection.Find(webFilter).FirstOrDefaultAsync();
                    if (existingWebDocument != null)
                    {
                        return JsonDocument.Parse(JsonSerializer.Serialize(new { success = false, message = "The 'url' field must be unique." }));
                    }
                }

                if (!document.Contains("createdAt"))
                {
                    document.Add("createdAt", DateTime.UtcNow);
                }

                await _placeCollection.InsertOneAsync(document);

                var createdJsonString = document.ToJson();
                return JsonDocument.Parse(createdJsonString);
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new { success = false, error = ex.Message }));
            }
        }


       
        /// <summary>  
        /// Обновить существующий документ в коллекции Places  
        /// </summary>  
        /// <param name="id">Строка с уникальным идентификатором объекта</param>
        /// <param name="jsonDocument">JSON-строка с обновленными данными</param>  
        /// <returns>Обновленный документ</returns>  
        public async Task<JsonDocument> UpdateAsync(string id, JsonDocument jsonDocument)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out var objectId))
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new { success = false, message = "A valid object ID must be provided." }));
                }

                var jsonString = jsonDocument.RootElement.GetRawText();
                var updatedDocument = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(jsonString);

                // Проверяем, есть ли идентификатор в документе и соответствует ли он переданному
                if (updatedDocument.Contains("_id"))
                {
                    var docIdString = updatedDocument["_id"].ToString();
                    // Проверяем совпадение идентификаторов
                    if (docIdString != id && docIdString != objectId.ToString())
                    {
                        return JsonDocument.Parse(JsonSerializer.Serialize(new
                        {
                            success = false,
                            message = "The ID in the document does not match the provided ID parameter."
                        }));
                    }
                    // Если идентификаторы совпадают, убедимся что формат правильный
                    updatedDocument["_id"] = objectId;
                }
                else
                {
                    // Если в документе нет идентификатора, добавляем его
                    updatedDocument.Add("_id", objectId);
                }

                // Check for unique name
                if (updatedDocument.Contains("name"))
                {
                    var nameFilter = Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("name", updatedDocument["name"].AsString),
                        Builders<BsonDocument>.Filter.Ne("_id", objectId)
                    );
                    if (updatedDocument.Contains("address") &&
                        updatedDocument["address"].IsBsonDocument &&
                        updatedDocument["address"].AsBsonDocument.Contains("city") &&
                        updatedDocument["address"].AsBsonDocument.Contains("province"))
                    {
                        var addressDoc = updatedDocument["address"].AsBsonDocument;
                        nameFilter = Builders<BsonDocument>.Filter.And(
                            nameFilter,
                            Builders<BsonDocument>.Filter.Eq("address.city", addressDoc["city"].AsString),
                            Builders<BsonDocument>.Filter.Eq("address.province", addressDoc["province"].AsString)
                        );
                    }
                    var existingNameDocument = await _placeCollection.Find(nameFilter).FirstOrDefaultAsync();
                    if (existingNameDocument != null)
                    {
                        return JsonDocument.Parse(JsonSerializer.Serialize(new { success = false, message = "The 'name' field must be unique within the same city and province." }));
                    }
                }

                // Check for unique url
                if (updatedDocument.Contains("url"))
                {
                    var webFilter = Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("url", updatedDocument["url"].AsString),
                        Builders<BsonDocument>.Filter.Ne("_id", objectId)
                    );
                    var existingWebDocument = await _placeCollection.Find(webFilter).FirstOrDefaultAsync();
                    if (existingWebDocument != null)
                    {
                        return JsonDocument.Parse(JsonSerializer.Serialize(new { success = false, message = "The 'url' field must be unique." }));
                    }
                }

                if (!updatedDocument.Contains("updatedAt"))
                {
                    updatedDocument.Add("updatedAt", DateTime.UtcNow);
                }
                else
                {
                    updatedDocument["updatedAt"] = DateTime.UtcNow;
                }

                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var result = await _placeCollection.ReplaceOneAsync(filter, updatedDocument);

                if (result.MatchedCount == 0)
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new { success = false, message = "Document with the specified ID was not found." }));
                }

                // Создаем результат с оригинальным документом и флагами успеха
                var updatedJsonString = updatedDocument.ToJson();
                var responseObj = new
                {
                    success = true,
                    message = "Document successfully updated.",
                    document = JsonDocument.Parse(updatedJsonString).RootElement
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(responseObj));
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new { success = false, error = ex.Message }));
            }
        }



        public async Task<JsonDocument> DeleteAsync(string id)
        {
            try
            {
                
                var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id));
                var existingDocument = await _placeCollection.Find(filter).FirstOrDefaultAsync();

                if (existingDocument == null)
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = $"Document with id {id} does not exist."
                    }));
                }

               
                var deleteResult = await _placeCollection.DeleteOneAsync(filter);

                if (deleteResult.DeletedCount > 0)
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        success = true,
                        message = $"Document with id {id} was successfully deleted."
                    }));
                }
                else
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = $"Failed to delete document with id {id}."
                    }));
                }
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    success = false,
                    message = $"An error occurred while deleting the document: {ex.Message}"
                }));
            }
        }


      

        /// <summary>
        /// Гео с выводом отсортированного списка с id, distance, name, img_link
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <param name="maxDistanceMeters"></param>
        /// <returns></returns>
        public async Task<List<BsonDocument>> GetPlacesNearbyAsync(decimal lat, decimal lng, int maxDistanceMeters)
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
                "url", 1
            }
            })
            };
            var result = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return result;
        }

        /// <summary>
        /// Гео с выводом отсортированного списка открытых заведений по времени Коста-Рики
        /// </summary>
        /// <param name="lat">Широта</param>
        /// <param name="lng">Долгота</param>
        /// <param name="maxDistanceMeters">Максимальное расстояние в метрах</param>
        /// <param name="isOpen">Учитывать ли расписание работы</param>
        /// <returns>Список ближайших мест</returns>
        public async Task<List<BsonDocument>> GetPlacesNearbyAsync(decimal lat, decimal lng, int maxDistanceMeters, bool isOpen)
        {
            if (!isOpen)
            {
                
                return await GetPlacesNearbyAsync(lat, lng, maxDistanceMeters);
            }

            
            var costaRicaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
            var currentTimeInCostaRica = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, costaRicaTimeZone);

            
            var dayOfWeek = currentTimeInCostaRica.DayOfWeek.ToString();
            var currentTimeString = currentTimeInCostaRica.ToString("h:mm tt"); // Например, "8:30 AM"

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
        
        
        new BsonDocument("$match", new BsonDocument
        {
            { "schedule", new BsonDocument
                {
                    { "$elemMatch", new BsonDocument
                        {
                            { "days", new BsonDocument("$in", new BsonArray { dayOfWeek }) },
                            { "hours", new BsonDocument
                                {
                                    { "$elemMatch", new BsonDocument
                                        {
                                            { "start", new BsonDocument("$lte", currentTimeString) },
                                            { "end", new BsonDocument("$gte", currentTimeString) }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }),
        
        
        new BsonDocument("$project", new BsonDocument
        {
            { "_id", 1 },
            { "distance", 1 },
            { "name", 1 },
            { "img_link", new BsonDocument
                {
                    { "$arrayElemAt", new BsonArray { "$img_link", 0 } } 
                }
            },
            {
                "url", 1
            }
        })
    };

            var result = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return result;
        }


        
        public async Task<string> GetNearbyPlacesAsyncCenter(decimal latitude, decimal longitude, int radiusMeters, int limit)
        {
            var pipeline = new[]
            {
            new BsonDocument("$geoNear", new BsonDocument
            {
                
                { "near", new BsonArray { longitude, latitude } },  // Массив координат
                { "distanceField", "distance" },
                { "maxDistance", radiusMeters },
                { "spherical", true },
                
            })
            ,
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 1 },
                { "category", 1 },
                { "name", 1 },
                { "location.coordinates", 1 },
                {
                    "url", 1
                }

            }),
            new BsonDocument("$limit", limit) 
            };

            var results = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            if (results.Count == 0)
            {
                return "[]"; 
            }
            return results.ToJson();  
        }


        /// <summary>
        /// Гео поиск по категории и тегам c выводом отсортироанного по дистанции категории и тегам
        /// списка данных об объектах
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <param name="maxDistanceMeters"></param>
        /// <param name="category"></param>
        /// <param name="filterTags"></param>
        /// <returns></returns>
        public async Task<List<BsonDocument>> GetPlacesNearbyByCategoryByTagsAsyncAsync(decimal lat, decimal lng, int maxDistanceMeters, string? category = null, List<string>? filterTags = null)
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
            { "url", 1 },
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

            return results;
        }

        /// <summary>
        /// Получить отсоритрованный список по дистанции с текстовым поиском
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <param name="maxDistanceMeters"></param>
        /// <param name="limit"></param>
        /// <param name="searchText"></param>
        /// <returns></returns>
        public async Task<List<BsonDocument>> GetPlacesNearbyWithTextSearchAsync(decimal lat, decimal lng, int maxDistanceMeters, int limit, string searchText)
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

            var matchStage = new BsonDocument("$match", new BsonDocument
                {
                    { "$or", new BsonArray
                        {
                            new BsonDocument("name", new BsonDocument("$regex", searchText).Add("$options", "i")),
                            new BsonDocument("description", new BsonDocument("$regex", searchText).Add("$options", "i")),
                            new BsonDocument("address.city", new BsonDocument("$regex", searchText).Add("$options", "i")),
                            new BsonDocument("address.country", new BsonDocument("$regex", searchText).Add("$options", "i")),
                            new BsonDocument("address.province", new BsonDocument("$regex", searchText).Add("$options", "i")),
                            new BsonDocument("address.street", new BsonDocument("$regex", searchText).Add("$options", "i")),
                            new BsonDocument("category", new BsonDocument("$regex", searchText).Add("$options", "i")),
                            new BsonDocument("keywords", new BsonDocument("$regex", searchText).Add("$options", "i")),
                            new BsonDocument("tags", new BsonDocument("$regex", searchText).Add("$options", "i"))
                         }
                    }
                });

            var projectStage = new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 1 },
                    { "distance", 1 },
                    { "name", 1 },
                    { "address.city", 1 },
                    { "img_link", new BsonDocument { { "$arrayElemAt", new BsonArray { "$img_link", 0 } } } },
                    { "url", 1 }
                });

            var limitStage = new BsonDocument("$limit", limit);

            var pipeline = new[] { geoNearStage, matchStage, projectStage, limitStage };

            var results = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results;
        }


        /// <summary>
        /// Получить отсортированный список по дистанции с текстовым поиском и фильтрацией по времени работы
        /// </summary>
        /// <param name="lat">Широта</param>
        /// <param name="lng">Долгота</param>
        /// <param name="maxDistanceMeters">Максимальное расстояние в метрах</param>
        /// <param name="limit">Лимит результатов</param>
        /// <param name="searchText">Текст для поиска</param>
        /// <param name="isOpen">Учитывать ли расписание работы</param>
        /// <returns>Список найденных объектов</returns>
        public async Task<List<BsonDocument>> GetPlacesNearbyWithTextSearchAsync(decimal lat, decimal lng, int maxDistanceMeters, int limit, string searchText, bool isOpen)
        {
            if (!isOpen)
            {
                
                return await GetPlacesNearbyWithTextSearchAsync(lat, lng, maxDistanceMeters, limit, searchText);
            }

            
            var costaRicaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
            var currentTimeInCostaRica = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, costaRicaTimeZone);

            
            var dayOfWeek = currentTimeInCostaRica.DayOfWeek.ToString();
            var currentTimeString = currentTimeInCostaRica.ToString("h:mm tt"); // Например, "8:30 AM"

            
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

            var textSearchStage = new BsonDocument("$match", new BsonDocument
        {
            { "$or", new BsonArray
                {
                    new BsonDocument("name", new BsonDocument("$regex", searchText).Add("$options", "i")),
                    new BsonDocument("description", new BsonDocument("$regex", searchText).Add("$options", "i")),
                    new BsonDocument("address.city", new BsonDocument("$regex", searchText).Add("$options", "i")),
                    new BsonDocument("address.country", new BsonDocument("$regex", searchText).Add("$options", "i")),
                    new BsonDocument("address.province", new BsonDocument("$regex", searchText).Add("$options", "i")),
                    new BsonDocument("address.street", new BsonDocument("$regex", searchText).Add("$options", "i")),
                    new BsonDocument("category", new BsonDocument("$regex", searchText).Add("$options", "i")),
                    new BsonDocument("keywords", new BsonDocument("$regex", searchText).Add("$options", "i")),
                    new BsonDocument("tags", new BsonDocument("$regex", searchText).Add("$options", "i"))
                 }
            }
        });

            
            var scheduleMatchStage = new BsonDocument("$match", new BsonDocument
    {
        { "schedule", new BsonDocument
            {
                { "$elemMatch", new BsonDocument
                    {
                        { "days", new BsonDocument("$in", new BsonArray { dayOfWeek }) },
                        { "hours", new BsonDocument
                            {
                                { "$elemMatch", new BsonDocument
                                    {
                                        { "start", new BsonDocument("$lte", currentTimeString) },
                                        { "end", new BsonDocument("$gte", currentTimeString) }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    });

            var projectStage = new BsonDocument("$project", new BsonDocument
        {
            { "_id", 1 },
            { "distance", 1 },
            { "name", 1 },
            { "address.city", 1 },
            { "img_link", new BsonDocument { { "$arrayElemAt", new BsonArray { "$img_link", 0 } } } },
            { "url", 1 }
        });

            var limitStage = new BsonDocument("$limit", limit);

            
            var pipeline = new[] { geoNearStage, textSearchStage, scheduleMatchStage, projectStage, limitStage };

            var results = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results;
        }

        /// <summary>
        /// Получить отсортированный список по дистанции с текстовым поиском и фильтрацией по ключевым словам 
        /// c использованием $or
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <param name="maxDistanceMeters"></param>
        /// <param name="limit"></param>
        /// <param name="filterKeywords"></param>
        /// <returns></returns>
        public async Task<List<BsonDocument>> GetPlacesWithKeywordsListAsync(
          decimal lat,
          decimal lng,
          int maxDistanceMeters,
          int limit,
          List<string>? filterKeywords)
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

            
            BsonDocument? matchStage = null;
            if (filterKeywords != null && filterKeywords.Any())
            {
                var orConditions = new BsonArray();
                foreach (var keyword in filterKeywords)
                {
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        orConditions.Add(new BsonDocument("name", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        orConditions.Add(new BsonDocument("description", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        orConditions.Add(new BsonDocument("address.city", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        orConditions.Add(new BsonDocument("address.country", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        orConditions.Add(new BsonDocument("address.province", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        orConditions.Add(new BsonDocument("address.street", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        orConditions.Add(new BsonDocument("category", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        orConditions.Add(new BsonDocument("keywords", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        orConditions.Add(new BsonDocument("tags", new BsonDocument("$regex", keyword).Add("$options", "i")));
                    }
                }

                if (orConditions.Count > 0)
                {
                    matchStage = new BsonDocument("$match", new BsonDocument
            {
                { "$or", orConditions }
            });
                }
            }

            var projectStage = new BsonDocument("$project", new BsonDocument
    {
        { "_id", 1 },
        { "distance", 1 },
        { "name", 1 },
        { "address.city", 1 },
        { "img_link", new BsonDocument { { "$arrayElemAt", new BsonArray { "$img_link", 0 } } } },
        { "url", 1 }
    });

            var limitStage = new BsonDocument("$limit", limit);

            
            var pipeline = matchStage != null
                ? new[] { geoNearStage, matchStage, projectStage, limitStage }
                : new[] { geoNearStage, projectStage, limitStage };

            var results = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results;
        }

        /// <summary>
        /// Получить отсортированный список по дистанции с текстовым поиском, фильтрацией по ключевым словам 
        /// и фильтрацией по открытым заведениям
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <param name="maxDistanceMeters"></param>
        /// <param name="limit"></param>
        /// <param name="filterKeywords"></param>
        /// <param name="isOpen"></param>
        /// <returns></returns>
        public async Task<List<BsonDocument>> GetPlacesWithKeywordsListAsync(
            decimal lat,
            decimal lng,
            int maxDistanceMeters,
            int limit,
            List<string>? filterKeywords,
            bool isOpen)
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

            
            BsonDocument? matchStage = null;
            if (filterKeywords != null && filterKeywords.Any())
            {
                var orConditions = new BsonArray();
                foreach (var keyword in filterKeywords)
                {
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        orConditions.Add(new BsonDocument("name", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        orConditions.Add(new BsonDocument("description", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        orConditions.Add(new BsonDocument("address.city", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        orConditions.Add(new BsonDocument("address.country", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        orConditions.Add(new BsonDocument("address.province", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        orConditions.Add(new BsonDocument("address.street", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        orConditions.Add(new BsonDocument("category", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        orConditions.Add(new BsonDocument("keywords", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        orConditions.Add(new BsonDocument("tags", new BsonDocument("$regex", keyword).Add("$options", "i")));
                    }
                }

                if (orConditions.Count > 0)
                {
                    matchStage = new BsonDocument("$match", new BsonDocument
                    {
                        { "$or", orConditions }
                    });
                }
            }

            
            BsonDocument? scheduleMatchStage = null;
            if (isOpen)
            {
                var costaRicaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                var currentTimeInCostaRica = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, costaRicaTimeZone);

                var dayOfWeek = currentTimeInCostaRica.DayOfWeek.ToString();
                var currentTimeString = currentTimeInCostaRica.ToString("h:mm tt");

                scheduleMatchStage = new BsonDocument("$match", new BsonDocument
                {
                    { "schedule", new BsonDocument
                        {
                            { "$elemMatch", new BsonDocument
                                {
                                    { "days", new BsonDocument("$in", new BsonArray { dayOfWeek }) },
                                    { "hours", new BsonDocument
                                        {
                                            { "$elemMatch", new BsonDocument
                                                {
                                                    { "start", new BsonDocument("$lte", currentTimeString) },
                                                    { "end", new BsonDocument("$gte", currentTimeString) }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
            }

            var projectStage = new BsonDocument("$project", new BsonDocument
            {
                { "_id", 1 },
                { "distance", 1 },
                { "name", 1 },
                { "address.city", 1 },
                { "img_link", new BsonDocument { { "$arrayElemAt", new BsonArray { "$img_link", 0 } } } },
                { "url", 1 }
            });

            var limitStage = new BsonDocument("$limit", limit);

            
            var pipeline = new List<BsonDocument> { geoNearStage };
            if (matchStage != null) pipeline.Add(matchStage);
            if (scheduleMatchStage != null) pipeline.Add(scheduleMatchStage);
            pipeline.Add(projectStage);
            pipeline.Add(limitStage);

            var results = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results;
        }


        
        public async Task<List<BsonDocument>> GetPlacesWithAllKeywordsAsync(
            decimal lat,
            decimal lng,
            int maxDistanceMeters,
            int limit,
            List<string>? filterKeywords)
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

            
            BsonDocument? matchStage = null;
            if (filterKeywords != null && filterKeywords.Any())
            {
                
                var andConditions = new BsonArray();

                foreach (var keyword in filterKeywords)
                {
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        
                        var fieldsOrCondition = new BsonArray();
                        fieldsOrCondition.Add(new BsonDocument("name", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        fieldsOrCondition.Add(new BsonDocument("description", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        fieldsOrCondition.Add(new BsonDocument("address.city", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        fieldsOrCondition.Add(new BsonDocument("address.country", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        fieldsOrCondition.Add(new BsonDocument("address.province", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        fieldsOrCondition.Add(new BsonDocument("address.street", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        fieldsOrCondition.Add(new BsonDocument("category", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        fieldsOrCondition.Add(new BsonDocument("keywords", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        fieldsOrCondition.Add(new BsonDocument("tags", new BsonDocument("$regex", keyword).Add("$options", "i")));

                        
                        andConditions.Add(new BsonDocument("$or", fieldsOrCondition));
                    }
                }

                if (andConditions.Count > 0)
                {
                    matchStage = new BsonDocument("$match", new BsonDocument
                    {
                        { "$and", andConditions }
                    });
                }
            }

            var projectStage = new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 1 },
                    { "distance", 1 },
                    { "name", 1 },
                    { "address.city", 1 },
                    { "img_link", new BsonDocument { { "$arrayElemAt", new BsonArray { "$img_link", 0 } } } },
                    { "url", 1 }
                });

            var limitStage = new BsonDocument("$limit", limit);

            // Формируем пайплайн в зависимости от наличия matchStage
            var pipeline = matchStage != null
                ? new[] { geoNearStage, matchStage, projectStage, limitStage }
                : new[] { geoNearStage, projectStage, limitStage };

            var results = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results;
        }


        /// <summary>
        /// Получить с использованием $and отсортированный список по дистанции с текстовым поиском, фильтрацией по ключевым словам
        /// и фильтрацией по открытым заведениям
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <param name="maxDistanceMeters"></param>
        /// <param name="limit"></param>
        /// <param name="filterKeywords"></param>
        /// <param name="isOpen"></param>
        /// <returns></returns>
        public async Task<JsonDocument> GetPlacesWithAllKeywordsAsync(
          decimal lat,
          decimal lng,
          int maxDistanceMeters,
          int limit,
          List<string>? filterKeywords,
          bool isOpen)
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

            BsonDocument? matchStage = null;
            if (filterKeywords != null && filterKeywords.Any())
            {
                var andConditions = new BsonArray();

                foreach (var keyword in filterKeywords)
                {
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        var fieldsOrCondition = new BsonArray();
                        fieldsOrCondition.Add(new BsonDocument("name", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        fieldsOrCondition.Add(new BsonDocument("description", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        fieldsOrCondition.Add(new BsonDocument("address.city", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        fieldsOrCondition.Add(new BsonDocument("address.country", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        fieldsOrCondition.Add(new BsonDocument("address.province", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        fieldsOrCondition.Add(new BsonDocument("address.street", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        fieldsOrCondition.Add(new BsonDocument("category", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        fieldsOrCondition.Add(new BsonDocument("keywords", new BsonDocument("$regex", keyword).Add("$options", "i")));
                        fieldsOrCondition.Add(new BsonDocument("tags", new BsonDocument("$regex", keyword).Add("$options", "i")));

                        andConditions.Add(new BsonDocument("$or", fieldsOrCondition));
                    }
                }

                if (andConditions.Count > 0)
                {
                    matchStage = new BsonDocument("$match", new BsonDocument
                   {
                       { "$and", andConditions }
                   });
                }
            }

            BsonDocument? scheduleMatchStage = null;
            if (isOpen)
            {
                var costaRicaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                var currentTimeInCostaRica = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, costaRicaTimeZone);

                var dayOfWeek = currentTimeInCostaRica.DayOfWeek.ToString();
                var currentTimeString = currentTimeInCostaRica.ToString("h:mm tt");

                scheduleMatchStage = new BsonDocument("$match", new BsonDocument
               {
                   { "schedule", new BsonDocument
                       {
                           { "$elemMatch", new BsonDocument
                               {
                                   { "days", new BsonDocument("$in", new BsonArray { dayOfWeek }) },
                                   { "hours", new BsonDocument
                                       {
                                           { "$elemMatch", new BsonDocument
                                               {
                                                   { "start", new BsonDocument("$lte", currentTimeString) },
                                                   { "end", new BsonDocument("$gte", currentTimeString) }
                                               }
                                           }
                                       }
                                   }
                               }
                           }
                       }
                   }
               });
            }

            var projectStage = new BsonDocument("$project", new BsonDocument
           {
               { "_id", 1 },
               { "distance", 1 },
               { "name", 1 },
               { "address.city", 1 },
               { "img_link", new BsonDocument { { "$arrayElemAt", new BsonArray { "$img_link", 0 } } } },
               { "url", 1 }
           });

            var pipeline = new List<BsonDocument> { geoNearStage };
            if (matchStage != null) pipeline.Add(matchStage);
            if (scheduleMatchStage != null) pipeline.Add(scheduleMatchStage);
            pipeline.Add(projectStage);

            var results = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            var totalCount = results.Count;

            var finalResult = new BsonDocument
           {
               { "totalCount", totalCount },
               { "places", new BsonArray(results) }
           };

            var jsonString = finalResult.ToJson();
            return JsonDocument.Parse(jsonString);
        }


        /// <summary>
        /// 
        /// Получить доступные теги по категории и выбранным тегам
        /// 
        /// </summary>
        /// 
        /// <param name="category">Категория</param>
        /// 
        /// <param name="selectedTags">Выбранные теги</param>
        /// 
        public async Task<JsonDocument> GetAvailableTagsAsync(
                                                                string? category,
                                                                List<string>? selectedTags)
        {
            var simplePipeline = new List<BsonDocument>();

            
            if (!string.IsNullOrEmpty(category))
            {
                simplePipeline.Add(new BsonDocument("$match",
                    new BsonDocument("category", category)));
            }

            
            if (selectedTags != null && selectedTags.Count > 0)
            {
                simplePipeline.Add(new BsonDocument("$match",
                    new BsonDocument("tags",
                        new BsonDocument("$all", new BsonArray(selectedTags)))));
            }

            
            simplePipeline.Add(new BsonDocument("$unwind", "$tags"));

            
            if (selectedTags != null && selectedTags.Count > 0)
            {
                simplePipeline.Add(new BsonDocument("$match",
                    new BsonDocument("tags",
                        new BsonDocument("$nin", new BsonArray(selectedTags)))));
            }

            
            simplePipeline.Add(new BsonDocument("$group",
                new BsonDocument
                {
            { "_id", "$tags" }
                }));

            
            simplePipeline.Add(new BsonDocument("$sort",
                new BsonDocument("_id", 1)));

            
            simplePipeline.Add(new BsonDocument("$project",
                new BsonDocument
                {
            { "_id", 0 },
            { "tag", "$_id" }
                }));

            var result = await _placeCollection.Aggregate<BsonDocument>(simplePipeline).ToListAsync();

            
            var totalCount = result.Count;

            
            var tagsList = new BsonArray();
            foreach (var doc in result)
            {
                tagsList.Add(doc["tag"]);
            }

            var finalResult = new BsonDocument
                {
                    { "totalCount", totalCount },
                    { "tags", tagsList }
                };

            
            var jsonString = finalResult.ToJson();

            
            return JsonDocument.Parse(jsonString);
        }

        /// <summary>
        /// Найти документ по имени, городу и провинции в адресе.
        /// </summary>
        /// <param name="name">Имя объекта</param>
        /// <param name="city">Город</param>
        /// <param name="province">Провинция</param>
        /// <returns>JSON-документ, соответствующий критериям, или null</returns>
        public async Task<JsonDocument?> GetPlaceByNameCityProvinceAsync(string name, string city, string province)
        {
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("name", name),
                Builders<BsonDocument>.Filter.Eq("address.city", city),
                Builders<BsonDocument>.Filter.Eq("address.province", province)
            );

            var bsonDocument = await _placeCollection.Find(filter).FirstOrDefaultAsync();

            if (bsonDocument == null)
            {
                return null;
            }

            var jsonString = bsonDocument.ToJson();
            return JsonDocument.Parse(jsonString);
        }

    }
}

