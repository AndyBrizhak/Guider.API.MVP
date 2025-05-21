

using Guider.API.MVP.Data;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace Guider.API.MVP.Services
{
    public class CitiesService
    {
        private readonly IMongoCollection<BsonDocument> _citiesCollection;

        public CitiesService(IOptions<MongoDbSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _citiesCollection = database.GetCollection<BsonDocument>(
                mongoSettings.Value.Collections["Cities"]);
        }

        //public async Task<JsonDocument> GetCitiesByProvinceAsync(string provinceName)
        //{
        //    try
        //    {
        //        var filter = Builders<BsonDocument>.Filter.Eq("province", provinceName);
        //        var projection = Builders<BsonDocument>.Projection.Exclude("_id");
        //        var cities = await _citiesCollection.Find(filter).Project(projection).ToListAsync();

        //        if (cities == null || cities.Count == 0)
        //        {
        //            var errorResponse = new
        //            {
        //                IsSuccess = false,
        //                Message = "Province not found or no cities available."
        //            };
        //            return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
        //        }

        //        var successResponse = new
        //        {
        //            IsSuccess = true,
        //            Cities = cities
        //        }.ToJson();
        //        return JsonDocument.Parse(successResponse);
        //    }
        //    catch (Exception ex)
        //    {
        //        var errorResponse = new
        //        {
        //            IsSuccess = false,
        //            Message = $"An error occurred: {ex.Message}"
        //        };
        //        return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
        //    }
        //}

        public async Task<JsonDocument> GetCityByIdAsync(string cityId)
        {
            try
            {
                // Convert string ID to MongoDB ObjectId
                if (!ObjectId.TryParse(cityId, out ObjectId objectId))
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Invalid city ID format."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Create filter by ObjectId and execute query
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var city = await _citiesCollection.Find(filter).FirstOrDefaultAsync();

                if (city == null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"City with ID '{cityId}' not found."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Extract coordinates if available
                double latitude = 0.0;
                double longitude = 0.0;
                if (city.Contains("location") &&
                    city["location"].IsBsonDocument &&
                    city["location"].AsBsonDocument.Contains("coordinates") &&
                    city["location"]["coordinates"].IsBsonArray)
                {
                    var coordinates = city["location"]["coordinates"].AsBsonArray;
                    if (coordinates.Count >= 2)
                    {
                        longitude = coordinates[0].AsDouble;
                        latitude = coordinates[1].AsDouble;
                    }
                }

                // Create a copy of the city document without the _id field for cleaner response
                var cityCopy = city.DeepClone().AsBsonDocument;
                cityCopy.Remove("_id");

                // If location exists but we also want to expose latitude and longitude directly
                if (cityCopy.Contains("location") && !cityCopy.Contains("latitude") && !cityCopy.Contains("longitude"))
                {
                    cityCopy["latitude"] = latitude;
                    cityCopy["longitude"] = longitude;
                }

                // Формируем корректный JSON для успешного ответа
                var responseDoc = new BsonDocument
                {
                    { "IsSuccess", true },
                    { "City", cityCopy },
                    { "Id", cityId }
                };

                return JsonDocument.Parse(responseDoc.ToJson());
            }
            catch (FormatException)
            {
                var errorResponse = new
                {
                    IsSuccess = false,
                    Message = "Invalid city ID format."
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

        public async Task<(List<JsonDocument> Documents, long TotalCount)> GetCitiesAsync(Dictionary<string, string> filter = null)
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
                        var nameFilter = filterBuilder.Regex("name", new BsonRegularExpression(q, "i"));
                        var urlFilter = filterBuilder.Regex("url", new BsonRegularExpression(q, "i"));
                        var provinceFilter = filterBuilder.Regex("province", new BsonRegularExpression(q, "i"));
                        // Объединяем в один фильтр OR
                        filters.Add(filterBuilder.Or(nameFilter, urlFilter, provinceFilter));
                    }
                    // Фильтр по названию города
                    if (filter.TryGetValue("name", out string name) && !string.IsNullOrEmpty(name))
                    {
                        filters.Add(filterBuilder.Regex("name", new BsonRegularExpression(name, "i")));
                    }
                    // Фильтр по названию провинции
                    if (filter.TryGetValue("province", out string province) && !string.IsNullOrEmpty(province))
                    {
                        filters.Add(filterBuilder.Regex("province", new BsonRegularExpression(province, "i")));
                    }
                    // Фильтр по URL слагу
                    if (filter.TryGetValue("url", out string url) && !string.IsNullOrEmpty(url))
                    {
                        filters.Add(filterBuilder.Regex("url", new BsonRegularExpression(url, "i")));
                    }

                    // Если есть фильтры, применяем их
                    if (filters.Count > 0)
                    {
                        filterDefinition = filterBuilder.And(filters);
                    }
                }

                // Get total count before applying pagination
                long totalCount = await _citiesCollection.CountDocumentsAsync(filterDefinition);

                // Применяем сортировку
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

                // Set up sort definition
                var sortDefinition = isDescending
                    ? Builders<BsonDocument>.Sort.Descending(sortField)
                    : Builders<BsonDocument>.Sort.Ascending(sortField);

                // Apply pagination if specified in the filter
                IFindFluent<BsonDocument, BsonDocument> query = _citiesCollection.Find(filterDefinition).Sort(sortDefinition);

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

        public async Task<JsonDocument> AddCityAsync(JsonDocument cityData)
        {
            try
            {
                var cityJson = cityData.RootElement.GetRawText();
                var cityBson = BsonDocument.Parse(cityJson);

                // Проверка обязательных полей
                if (!cityBson.Contains("name") || cityBson["name"].BsonType != BsonType.String)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "City data must contain a 'name' field."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                if (!cityBson.Contains("province") || cityBson["province"].BsonType != BsonType.String)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "City data must contain a 'province' field."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                string cityName = cityBson["name"].AsString;
                string provinceName = cityBson["province"].AsString;

                // Проверка на существование города с таким же названием в провинции
                var existingCityFilter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("name", cityName),
                    Builders<BsonDocument>.Filter.Eq("province", provinceName)
                );
                var existingCity = await _citiesCollection.Find(existingCityFilter).FirstOrDefaultAsync();

                if (existingCity != null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"City '{cityName}' already exists in province '{provinceName}'."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Создание GeoJSON если координаты указаны
                if (!cityBson.Contains("location") &&
                    cityBson.Contains("latitude") && cityBson.Contains("longitude") &&
                    cityBson["latitude"].BsonType == BsonType.Double &&
                    cityBson["longitude"].BsonType == BsonType.Double)
                {
                    var location = new BsonDocument
                {
                    { "type", "Point" },
                    { "coordinates", new BsonArray
                        {
                            cityBson["longitude"].AsDouble,
                            cityBson["latitude"].AsDouble
                        }
                    }
                };
                    cityBson.Add("location", location);

                    // Удаляем отдельные поля широты и долготы, так как они теперь в location
                    cityBson.Remove("latitude");
                    cityBson.Remove("longitude");
                }

                // Добавление города в коллекцию
                await _citiesCollection.InsertOneAsync(cityBson);

                // Получаем только что добавленный город из базы данных
                var cityId = cityBson["_id"].AsObjectId;
                var filter = Builders<BsonDocument>.Filter.Eq("_id", cityId);
                var addedCity = await _citiesCollection.Find(filter).FirstOrDefaultAsync();

                if (addedCity == null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "City was added but could not be retrieved from the database."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Извлекаем координаты, если они есть
                double? longitude = null;
                double? latitude = null;
                if (addedCity.Contains("location") &&
                    addedCity["location"].IsBsonDocument &&
                    addedCity["location"].AsBsonDocument.Contains("coordinates") &&
                    addedCity["location"]["coordinates"].IsBsonArray)
                {
                    var coordinates = addedCity["location"]["coordinates"].AsBsonArray;
                    if (coordinates.Count >= 2)
                    {
                        longitude = coordinates[0].AsDouble;
                        latitude = coordinates[1].AsDouble;
                    }
                }

                // Формируем объект в нужном формате
                var formattedCity = new
                {
                    id = cityId.ToString(),
                    name = addedCity.Contains("name") ? addedCity["name"].AsString : string.Empty,
                    province = addedCity.Contains("province") ? addedCity["province"].AsString : string.Empty,
                    url = addedCity.Contains("url") ? addedCity["url"].AsString :
                        (addedCity.Contains("name") ? addedCity["name"].AsString.ToLower().Replace(" ", "-") : string.Empty),
                    location = new
                    {
                        longitude,
                        latitude
                    }
                };

                var successResponse = new
                {
                    IsSuccess = true,
                    Message = $"New City has been successfully added.",
                    Data = formattedCity
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

        public async Task<JsonDocument> UpdateCityAsync(string cityId, JsonDocument cityData)
        {
            try
            {
                var cityJson = cityData.RootElement.GetRawText();
                var updatedCityBson = BsonDocument.Parse(cityJson);

                // Проверяем, существует ли город с указанным идентификатором
                var objectId = new ObjectId(cityId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var existingCity = await _citiesCollection.Find(filter).FirstOrDefaultAsync();

                if (existingCity == null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"City with ID '{cityId}' not found."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Сохраняем текущие значения для формирования сообщения
                string currentCityName = existingCity.Contains("name") ? existingCity["name"].AsString : "Unknown";
                string currentProvince = existingCity.Contains("province") ? existingCity["province"].AsString : "Unknown";

                // Проверяем, меняется ли название города
                string updatedCityName = currentCityName;
                if (updatedCityBson.Contains("name") && updatedCityBson["name"].BsonType == BsonType.String)
                {
                    updatedCityName = updatedCityBson["name"].AsString;

                    // Если название меняется, проверяем, не существует ли уже город с таким названием в той же провинции
                    if (updatedCityName != currentCityName)
                    {
                        // Получаем провинцию из обновленных данных или из существующего города
                        string provinceName = existingCity["province"].AsString;
                        if (updatedCityBson.Contains("province") && updatedCityBson["province"].BsonType == BsonType.String)
                        {
                            provinceName = updatedCityBson["province"].AsString;
                        }

                        var duplicateFilter = Builders<BsonDocument>.Filter.And(
                            Builders<BsonDocument>.Filter.Eq("name", updatedCityName),
                            Builders<BsonDocument>.Filter.Eq("province", provinceName),
                            Builders<BsonDocument>.Filter.Ne("_id", objectId)
                        );
                        var duplicateCity = await _citiesCollection.Find(duplicateFilter).FirstOrDefaultAsync();

                        if (duplicateCity != null)
                        {
                            var errorResponse = new
                            {
                                IsSuccess = false,
                                Message = $"City with name '{updatedCityName}' already exists in province '{provinceName}'."
                            };
                            return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                        }
                    }
                }

                // Обработка координат, если они указаны
                if (!updatedCityBson.Contains("location") &&
                    updatedCityBson.Contains("latitude") && updatedCityBson.Contains("longitude") &&
                    updatedCityBson["latitude"].BsonType == BsonType.Double &&
                    updatedCityBson["longitude"].BsonType == BsonType.Double)
                {
                    var location = new BsonDocument
                {
                    { "type", "Point" },
                    { "coordinates", new BsonArray
                        {
                            updatedCityBson["longitude"].AsDouble,
                            updatedCityBson["latitude"].AsDouble
                        }
                    }
                };
                    updatedCityBson.Add("location", location);

                    // Удаляем отдельные поля широты и долготы
                    updatedCityBson.Remove("latitude");
                    updatedCityBson.Remove("longitude");
                }
                else if (!updatedCityBson.Contains("location") && existingCity.Contains("location"))
                {
                    // Сохраняем существующие координаты, если они не указаны в запросе
                    updatedCityBson["location"] = existingCity["location"];
                }

                // Обработка url/web полей
                if (!updatedCityBson.Contains("url") && updatedCityBson.Contains("name"))
                {
                    string url = updatedCityBson["name"].AsString.ToLower().Replace(" ", "-");
                    updatedCityBson.Add("url", url);
                }
                else if (updatedCityBson.Contains("web") && !updatedCityBson.Contains("url"))
                {
                    updatedCityBson.Add("url", updatedCityBson["web"]);
                    updatedCityBson.Remove("web");
                }

                // Сохраняем исходный _id
                updatedCityBson["_id"] = existingCity["_id"];

                // Сохраняем провинцию, если она не указана в обновляемых данных
                if (!updatedCityBson.Contains("province") && existingCity.Contains("province"))
                {
                    updatedCityBson["province"] = existingCity["province"];
                }

                // Обновляем документ
                await _citiesCollection.ReplaceOneAsync(filter, updatedCityBson);

                // Определяем название провинции для сообщения
                string updatedProvince = updatedCityBson.Contains("province") ?
                    updatedCityBson["province"].AsString : currentProvince;

                // Получаем обновленный город после обновления
                var updatedCity = await _citiesCollection.Find(filter).FirstOrDefaultAsync();

                // Извлекаем координаты, если они есть
                double? longitude = null;
                double? latitude = null;
                if (updatedCity.Contains("location") &&
                    updatedCity["location"].IsBsonDocument &&
                    updatedCity["location"].AsBsonDocument.Contains("coordinates") &&
                    updatedCity["location"]["coordinates"].IsBsonArray)
                {
                    var coordinates = updatedCity["location"]["coordinates"].AsBsonArray;
                    if (coordinates.Count >= 2)
                    {
                        longitude = coordinates[0].AsDouble;
                        latitude = coordinates[1].AsDouble;
                    }
                }

                // Формируем объект в нужном формате
                var formattedCity = new
                {
                    id = updatedCity["_id"].AsObjectId.ToString(),
                    name = updatedCity.Contains("name") ? updatedCity["name"].AsString : string.Empty,
                    province = updatedCity.Contains("province") ? updatedCity["province"].AsString : string.Empty,
                    url = updatedCity.Contains("url") ? updatedCity["url"].AsString :
                          (updatedCity.Contains("name") ? updatedCity["name"].AsString.ToLower().Replace(" ", "-") : string.Empty),
                    location = new
                    {
                        longitude,
                        latitude
                    }
                };

                var successResponse = new
                {
                    IsSuccess = true,
                    Message = $"City '{currentCityName}' has been successfully updated to '{updatedCityName}' in province '{updatedProvince}'.",
                    CityData = formattedCity
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(successResponse));
            }
            catch (FormatException)
            {
                var errorResponse = new
                {
                    IsSuccess = false,
                    Message = "Invalid city ID format."
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

        public async Task<JsonDocument> RemoveCityAsync(string cityId)
        {
            try
            {
                // Используем ObjectId для поиска документа по _id
                var objectId = MongoDB.Bson.ObjectId.Parse(cityId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);

                var deleteResult = await _citiesCollection.DeleteOneAsync(filter);

                if (deleteResult.DeletedCount == 0)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"City with ID '{cityId}' not found."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                var successResponse = new
                {
                    IsSuccess = true,
                    Message = $"City with ID '{cityId}' has been successfully removed."
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(successResponse));
            }
            catch (FormatException ex)
            {
                var errorResponse = new
                {
                    IsSuccess = false,
                    Message = $"Invalid ID format: {ex.Message}"
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
        
    }
}