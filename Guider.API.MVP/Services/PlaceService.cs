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
    using System.Text.RegularExpressions;
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
                            filterBuilder.Regex("address.province", new BsonRegularExpression(q, "i")),
                            filterBuilder.Regex("address.city", new BsonRegularExpression(q, "i")),
                            filterBuilder.Regex("url", new BsonRegularExpression(q, "i"))
                        ));
                    }

                    // Улучшенный фильтр по провинции
                    if (filter.TryGetValue("province", out string province) && !string.IsNullOrEmpty(province))
                    {
                        // Создаем более гибкий паттерн для поиска провинции
                        // Ищем провинцию как точное совпадение или как часть названия
                        var provincePatterns = new List<FilterDefinition<BsonDocument>>();

                        // 1. Точное совпадение (case-insensitive)
                        provincePatterns.Add(filterBuilder.Regex("address.province", new BsonRegularExpression($"^{Regex.Escape(province)}$", "i")));

                        // 2. Поиск в начале строки + возможные суффиксы типа "Province", "State", etc.
                        provincePatterns.Add(filterBuilder.Regex("address.province", new BsonRegularExpression($"^{Regex.Escape(province)}\\s+(Province|State|Region)$", "i")));

                        // 3. Поиск провинции как подстроки (если предыдущие не сработали)
                        provincePatterns.Add(filterBuilder.Regex("address.province", new BsonRegularExpression(Regex.Escape(province), "i")));

                        filters.Add(filterBuilder.Or(provincePatterns));
                    }

                    // Улучшенный фильтр по городу
                    if (filter.TryGetValue("city", out string city) && !string.IsNullOrEmpty(city))
                    {
                        // Аналогично для городов - более гибкий поиск
                        var cityPatterns = new List<FilterDefinition<BsonDocument>>();

                        // 1. Точное совпадение (case-insensitive)
                        cityPatterns.Add(filterBuilder.Regex("address.city", new BsonRegularExpression($"^{Regex.Escape(city)}$", "i")));

                        // 2. Поиск города как подстроки
                        cityPatterns.Add(filterBuilder.Regex("address.city", new BsonRegularExpression(Regex.Escape(city), "i")));

                        filters.Add(filterBuilder.Or(cityPatterns));
                    }

                    // Фильтр по названию заведения
                    if (filter.TryGetValue("name", out string name) && !string.IsNullOrEmpty(name))
                    {
                        filters.Add(filterBuilder.Regex("name", new BsonRegularExpression(Regex.Escape(name), "i")));
                    }

                    // Фильтр по URL
                    if (filter.TryGetValue("url", out string url) && !string.IsNullOrEmpty(url))
                    {
                        filters.Add(filterBuilder.Regex("url", new BsonRegularExpression(Regex.Escape(url), "i")));
                    }

                    // Фильтр по статусу (если нужен)
                    if (filter.TryGetValue("status", out string status) && !string.IsNullOrEmpty(status))
                    {
                        filters.Add(filterBuilder.Eq("status", status));
                    }

                    if (filters.Count > 0)
                    {
                        filterDefinition = filterBuilder.And(filters);
                    }
                }

                long totalCount = await _placeCollection.CountDocumentsAsync(filterDefinition);

                // Сортировка с поддержкой вложенных полей
                string sortField = "name";
                bool isDescending = false;
                if (filter != null)
                {
                    if (filter.TryGetValue("_sort", out string sort) && !string.IsNullOrEmpty(sort))
                    {
                        sortField = sort;
                        // Поддержка сортировки по вложенным полям
                        if (sort == "address.city")
                            sortField = "address.city";
                        else if (sort == "address.province")
                            sortField = "address.province";
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

                
                // Create a copy of the place document without the _id field
                var placeCopy = place.DeepClone().AsBsonDocument;
                placeCopy.Remove("_id");

                // Add id field with the original string ID
                placeCopy["id"] = id;

                
                // Return only the place document with id field
                return JsonDocument.Parse(placeCopy.ToJson());
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


        /// Получить документ по локальнуому url
        public async Task<JsonDocument> GetByUrlAsync(string url, string status = null)
        {
            try
            {
                // Validate URL parameter
                if (string.IsNullOrEmpty(url))
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "URL parameter is required and cannot be null or empty."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Create filter by url
                var filterBuilder = Builders<BsonDocument>.Filter;
                var filter = filterBuilder.Eq("url", url);

                // Add status filtering logic (same as in GetPlacesNearbyAsync)
                if (status == null)
                {
                    // Если статус null - не добавляем фильтрацию по статусу (передаются все объекты)
                    // filter остается без фильтра по статусу
                }
                else if (string.IsNullOrWhiteSpace(status))
                {
                    // Если статус пустая строка - выбираются все объекты с любыми статусами или без статуса
                    // Не добавляем условие фильтрации - берем все
                }
                else
                {
                    // Если статус указан конкретный - фильтруем строго по этому статусу
                    // Объекты без поля status не будут выбраны
                    filter = filterBuilder.And(filter, filterBuilder.Eq("status", status));
                }

                // Execute query
                var place = await _placeCollection.Find(filter).FirstOrDefaultAsync();

                if (place == null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"Object with URL '{url}' not found or doesn't match status criteria."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Create a copy of the place document without the _id field
                var placeCopy = place.DeepClone().AsBsonDocument;
                placeCopy.Remove("_id");

                // Add id field with the original ObjectId as string
                placeCopy["id"] = place["_id"].AsObjectId.ToString();

                // Return success response with the place data
                var successResponse = new
                {
                    IsSuccess = true,
                    Message = "Place found successfully.",
                    Data = BsonTypeMapper.MapToDotNetValue(placeCopy)
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


        public async Task<JsonDocument> CreateAsync(JsonDocument jsonDocument)
        {
            try
            {
                var jsonString = jsonDocument.RootElement.GetRawText();
                var document = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(jsonString);

                // Преобразование latitude и longitude в GeoJSON location
                if (document.Contains("latitude") && document.Contains("longitude"))
                {
                    var latitude = document["latitude"].ToDouble();
                    var longitude = document["longitude"].ToDouble();

                    // Создаем GeoJSON Point структуру
                    var locationDocument = new BsonDocument
                    {
                        ["type"] = "Point",
                        ["coordinates"] = new BsonArray { longitude, latitude } // Важно: сначала longitude, потом latitude
                    };

                    // Добавляем location и удаляем отдельные поля
                    document["location"] = locationDocument;
                    document.Remove("latitude");
                    document.Remove("longitude");
                }

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

                // Создание заведения в базе данных
                await _placeCollection.InsertOneAsync(document);

                // Получение данных о новом заведении
                var createdDocument = await _placeCollection.Find(Builders<BsonDocument>.Filter.Eq("_id", document["_id"])).FirstOrDefaultAsync();
                if (createdDocument == null)
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new { success = false, message = "Failed to retrieve created document." }));
                }

                // Создание глубокой копии и изменение формата идентификатора
                var deepCopy = BsonDocument.Parse(createdDocument.ToJson());

                // Изменяем _id на id
                if (deepCopy.Contains("_id"))
                {
                    var idValue = deepCopy["_id"].ToString();
                    deepCopy.Remove("_id");
                    deepCopy.Add("id", idValue);
                }

                // Возврат успешного результата
                var resultData = JsonDocument.Parse(deepCopy.ToJson());
                return JsonDocument.Parse(JsonSerializer.Serialize(new { success = true, data = resultData }));
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new { success = false, message = ex.Message }));
            }
        }

        /// Обновить существующий документ в коллекции Places  
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

                // Удаляем поле _id, если оно присутствует в обновляемых данных
                if (updatedDocument.Contains("id"))
                {
                    updatedDocument.Remove("id");
                }

                // Преобразование latitude и longitude в GeoJSON location (как в методе создания)
                if (updatedDocument.Contains("latitude") && updatedDocument.Contains("longitude"))
                {
                    var latitude = updatedDocument["latitude"].ToDouble();
                    var longitude = updatedDocument["longitude"].ToDouble();
                    var locationDocument = new BsonDocument
                    {
                        ["type"] = "Point",
                        ["coordinates"] = new BsonArray { longitude, latitude }
                    };
                    updatedDocument["location"] = locationDocument;
                    updatedDocument.Remove("latitude");
                    updatedDocument.Remove("longitude");
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

                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var updateDefinition = new BsonDocument("$set", updatedDocument);
                var result = await _placeCollection.UpdateOneAsync(filter, updateDefinition);

                if (result.MatchedCount == 0)
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new { success = false, message = "Document with the specified ID was not found." }));
                }

                // Получение обновленного документа из базы данных (как в методе создания)
                var updatedDocumentFromDb = await _placeCollection.Find(Builders<BsonDocument>.Filter.Eq("_id", objectId)).FirstOrDefaultAsync();
                if (updatedDocumentFromDb == null)
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new { success = false, message = "Failed to retrieve updated document." }));
                }

                var deepCopy = BsonDocument.Parse(updatedDocumentFromDb.ToJson());

                if (deepCopy.Contains("_id"))
                {
                    var idValue = deepCopy["_id"].ToString();
                    deepCopy.Remove("_id");
                    deepCopy["id"] = idValue;
                }

                var resultData = JsonDocument.Parse(deepCopy.ToJson());
                return JsonDocument.Parse(JsonSerializer.Serialize(new { success = true, data = resultData }));
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new { success = false, message = ex.Message }));
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

        public async Task<JsonDocument> GetPlacesNearbyAsync(decimal lat, decimal lng, int maxDistanceMeters, bool isOpen = false, string status = null)
        {
            try
            {
                // Валидация входных параметров
                if (lat < -90 || lat > 90)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Invalid latitude value. Must be between -90 and 90."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                if (lng < -180 || lng > 180)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Invalid longitude value. Must be between -180 and 180."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                if (maxDistanceMeters <= 0)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Maximum distance must be greater than 0."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Создаем список стадий pipeline
                var pipelineStages = new List<BsonDocument>();

                // Стадия 1: $geoNear - поиск по геолокации
                pipelineStages.Add(new BsonDocument("$geoNear", new BsonDocument
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
                }));

                // Стадия 2: $match - фильтрация
                var matchConditions = new BsonDocument();

                // Логика фильтрации по статусу
                if (status == null)
                {
                    // Если статус null - не добавляем фильтрацию по статусу (передаются все объекты)
                    // matchConditions остается без фильтра по статусу
                }
                else if (string.IsNullOrWhiteSpace(status))
                {
                    // Если статус пустая строка - выбираются все объекты с любыми статусами или без статуса
                    // Не добавляем условие фильтрации - берем все
                }
                else
                {
                    // Если статус указан конкретный - фильтруем строго по этому статусу
                    // Объекты без поля status не будут выбраны
                    matchConditions.Add("status", status);
                }

                // Если нужно фильтровать по времени работы
                if (isOpen)
                {
                    var costaRicaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                    var currentTimeInCostaRica = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, costaRicaTimeZone);
                    var dayOfWeek = currentTimeInCostaRica.DayOfWeek.ToString();
                    var currentTimeString = currentTimeInCostaRica.ToString("h:mm tt"); // Например, "8:30 AM"

                    matchConditions.Add("schedule", new BsonDocument
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
                    });
                }

                pipelineStages.Add(new BsonDocument("$match", matchConditions));

                // Стадия 3: $project - выбор полей
                //pipelineStages.Add(new BsonDocument("$project", new BsonDocument
                //{
                //    { "_id", 1 },
                //    { "distance", 1 },
                //    { "name", 1 },
                //    { "img_link", new BsonDocument
                //        {
                //            { "$arrayElemAt", new BsonArray { "$img_link", 0 } } // Первая ссылка на изображение
                //        }
                //    },
                //    { "url", 1 }
                //}));

                var result = await _placeCollection.Aggregate<BsonDocument>(pipelineStages).ToListAsync();

                // Обработка результата и создание response в формате с id вместо _id
                var processedResults = result.Select(doc =>
                {
                    var docCopy = doc.DeepClone().AsBsonDocument;
                    if (docCopy.Contains("_id"))
                    {
                        docCopy["id"] = docCopy["_id"].ToString();
                        docCopy.Remove("_id");
                    }
                    return docCopy;
                }).ToList();

                var successResponse = new
                {
                    IsSuccess = true,
                    Message = $"Found {processedResults.Count} places within {maxDistanceMeters} meters.",
                    Data = processedResults.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList()
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(successResponse));
            }
            catch (TimeZoneNotFoundException ex)
            {
                var errorResponse = new
                {
                    IsSuccess = false,
                    Message = $"Time zone error: {ex.Message}"
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
            }
            catch (MongoException ex)
            {
                var errorResponse = new
                {
                    IsSuccess = false,
                    Message = $"Database error: {ex.Message}"
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
            }
            catch (ArgumentException ex)
            {
                var errorResponse = new
                {
                    IsSuccess = false,
                    Message = $"Invalid argument: {ex.Message}"
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

       public async Task<JsonDocument> GetPlacesWithAllKeywordsAsync(
            decimal? lat,
            decimal? lng,
            int? maxDistanceMeters,
            int limit,
            List<string>? filterKeywords,
            bool searchAllKeywords,
            bool isOpen,
            string? status = null)
        {
            try
            {
                // Валидация входных параметров
                bool hasCoordinates = lat.HasValue && lng.HasValue;

                if (hasCoordinates)
                {
                    if (lat < -90 || lat > 90)
                    {
                        var errorResponse = new
                        {
                            IsSuccess = false,
                            Message = "Invalid latitude value. Must be between -90 and 90."
                        };
                        return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                    }

                    if (lng < -180 || lng > 180)
                    {
                        var errorResponse = new
                        {
                            IsSuccess = false,
                            Message = "Invalid longitude value. Must be between -180 and 180."
                        };
                        return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                    }

                    if (maxDistanceMeters.HasValue && maxDistanceMeters <= 0)
                    {
                        var errorResponse = new
                        {
                            IsSuccess = false,
                            Message = "Maximum distance must be greater than 0."
                        };
                        return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                    }
                }

                // Создаем список стадий pipeline
                var pipelineStages = new List<BsonDocument>();

                // Стадия 1: $geoNear - поиск по геолокации (только если координаты указаны)
                if (hasCoordinates)
                {
                    var geoNearStage = new BsonDocument("$geoNear", new BsonDocument
                    {
                        { "near", new BsonDocument
                            {
                                { "type", "Point" },
                                { "coordinates", new BsonArray { lng.Value, lat.Value } }
                            }
                        },
                        { "distanceField", "distance" },
                        { "spherical", true }
                    });

                    if (maxDistanceMeters.HasValue)
                    {
                        geoNearStage["$geoNear"].AsBsonDocument.Add("maxDistance", maxDistanceMeters.Value);
                    }

                    pipelineStages.Add(geoNearStage);
                }

                // Стадия 2: $match - фильтрация
                var matchConditions = new BsonDocument();

                // Фильтрация по ключевым словам
                if (filterKeywords != null && filterKeywords.Any())
                {
                    var keywordConditions = new BsonArray();

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

                            if (searchAllKeywords)
                            {
                                // Для поиска по всем ключевым словам - каждое слово должно присутствовать
                                keywordConditions.Add(new BsonDocument("$or", fieldsOrCondition));
                            }
                            else
                            {
                                // Для поиска по любому ключевому слову - добавляем все условия в один $or
                                foreach (BsonDocument condition in fieldsOrCondition)
                                {
                                    keywordConditions.Add(condition);
                                }
                            }
                        }
                    }

                    if (keywordConditions.Count > 0)
                    {
                        if (searchAllKeywords)
                        {
                            // Все ключевые слова должны быть найдены
                            matchConditions.Add("$and", keywordConditions);
                        }
                        else
                        {
                            // Достаточно найти любое ключевое слово
                            matchConditions.Add("$or", keywordConditions);
                        }
                    }
                }

                // Фильтрация по статусу
                if (status == null)
                {
                    // Если статус null - не добавляем фильтрацию по статусу
                }
                else if (string.IsNullOrWhiteSpace(status))
                {
                    // Если статус пустая строка - выбираются все объекты с любыми статусами или без статуса
                }
                else
                {
                    // Если статус указан конкретный - фильтруем строго по этому статусу
                    matchConditions.Add("status", status);
                }

                // Фильтрация по времени работы
                if (isOpen)
                {
                    var costaRicaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                    var currentTimeInCostaRica = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, costaRicaTimeZone);
                    var dayOfWeek = currentTimeInCostaRica.DayOfWeek.ToString();
                    var currentTimeString = currentTimeInCostaRica.ToString("h:mm tt");

                    matchConditions.Add("schedule", new BsonDocument
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
                    });
                }

                // Добавляем стадию $match только если есть условия для фильтрации
                if (matchConditions.ElementCount > 0)
                {
                    pipelineStages.Add(new BsonDocument("$match", matchConditions));
                }

                // Стадия 3: $limit - ограничение количества результатов
                if (limit > 0)
                {
                    pipelineStages.Add(new BsonDocument("$limit", limit));
                }

                // Стадия 4: $project - выбор полей
                var projectStage = new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 1 },
                    { "name", 1 },
                    { "address.city", 1 },
                    { "img_link", new BsonDocument { { "$arrayElemAt", new BsonArray { "$img_link", 0 } } } },
                    { "url", 1 }
                });

                // Добавляем поле distance только если использовался геопоиск
                if (hasCoordinates)
                {
                    projectStage["$project"].AsBsonDocument.Add("distance", 1);
                }

                pipelineStages.Add(projectStage);

                // Выполнение агрегации
                var result = await _placeCollection.Aggregate<BsonDocument>(pipelineStages).ToListAsync();

                // Обработка результата и создание response в формате с id вместо _id
                var processedResults = result.Select(doc =>
                {
                    var docCopy = doc.DeepClone().AsBsonDocument;
                    if (docCopy.Contains("_id"))
                    {
                        docCopy["id"] = docCopy["_id"].ToString();
                        docCopy.Remove("_id");
                    }
                    return docCopy;
                }).ToList();

                string message;
                if (hasCoordinates && maxDistanceMeters.HasValue)
                {
                    message = $"Found {processedResults.Count} places within {maxDistanceMeters.Value} meters.";
                }
                else if (hasCoordinates)
                {
                    message = $"Found {processedResults.Count} places near the specified coordinates.";
                }
                else
                {
                    message = $"Found {processedResults.Count} places matching the search criteria.";
                }

                var successResponse = new
                {
                    IsSuccess = true,
                    Message = message,
                    Data = processedResults.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList()
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(successResponse));
            }
            catch (TimeZoneNotFoundException ex)
            {
                var errorResponse = new
                {
                    IsSuccess = false,
                    Message = $"Time zone error: {ex.Message}"
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
            }
            catch (MongoException ex)
            {
                var errorResponse = new
                {
                    IsSuccess = false,
                    Message = $"Database error: {ex.Message}"
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
            }
            catch (ArgumentException ex)
            {
                var errorResponse = new
                {
                    IsSuccess = false,
                    Message = $"Invalid argument: {ex.Message}"
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

