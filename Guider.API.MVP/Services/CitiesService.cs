

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

                // Проверка обязательных полей (теперь не обязательны)
                string cityName = cityBson.Contains("name") && cityBson["name"].BsonType == BsonType.String
                    ? cityBson["name"].AsString
                    : string.Empty;
                string provinceName = cityBson.Contains("province") && cityBson["province"].BsonType == BsonType.String
                    ? cityBson["province"].AsString
                    : string.Empty;

                // Проверка на существование города с таким же названием в провинции, если оба поля есть
                if (!string.IsNullOrEmpty(cityName) && !string.IsNullOrEmpty(provinceName))
                {
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
                }

                // Создание GeoJSON если координаты указаны
                if (!cityBson.Contains("location") &&
                    cityBson.Contains("latitude") && cityBson.Contains("longitude") &&
                    (cityBson["latitude"].BsonType == BsonType.Double || cityBson["latitude"].BsonType == BsonType.Int32 || cityBson["latitude"].BsonType == BsonType.Int64) &&
                    (cityBson["longitude"].BsonType == BsonType.Double || cityBson["longitude"].BsonType == BsonType.Int32 || cityBson["longitude"].BsonType == BsonType.Int64))
                {
                    double lat = cityBson["latitude"].ToDouble();
                    double lon = cityBson["longitude"].ToDouble();
                    var location = new BsonDocument
                    {
                        { "type", "Point" },
                        { "coordinates", new BsonArray { lon, lat } }
                    };
                    cityBson["location"] = location;
                    cityBson.Remove("latitude");
                    cityBson.Remove("longitude");
                }
                else if (cityBson.Contains("location") && cityBson["location"].IsBsonDocument)
                {
                    var loc = cityBson["location"].AsBsonDocument;
                    // Ensure GeoJSON type is present and correct
                    loc["type"] = "Point";
                    // Ensure coordinates is an array of [lon, lat]
                    if (loc.Contains("coordinates") && loc["coordinates"].IsBsonArray)
                    {
                        var coords = loc["coordinates"].AsBsonArray;
                        if (coords.Count == 2 &&
                            (coords[0].IsDouble || coords[0].IsInt32 || coords[0].IsInt64) &&
                            (coords[1].IsDouble || coords[1].IsInt32 || coords[1].IsInt64))
                        {
                            // nothing to do, already correct
                        }
                        else if (loc.Contains("longitude") && loc.Contains("latitude") &&
                                 (loc["longitude"].IsDouble || loc["longitude"].IsInt32 || loc["longitude"].IsInt64) &&
                                 (loc["latitude"].IsDouble || loc["latitude"].IsInt32 || loc["latitude"].IsInt64))
                        {
                            double lon = loc["longitude"].ToDouble();
                            double lat = loc["latitude"].ToDouble();
                            loc["coordinates"] = new BsonArray { lon, lat };
                            loc.Remove("longitude");
                            loc.Remove("latitude");
                        }
                        else
                        {
                            loc["coordinates"] = new BsonArray();
                        }
                    }
                    else if (loc.Contains("longitude") && loc.Contains("latitude") &&
                             (loc["longitude"].IsDouble || loc["longitude"].IsInt32 || loc["longitude"].IsInt64) &&
                             (loc["latitude"].IsDouble || loc["latitude"].IsInt32 || loc["latitude"].IsInt64))
                    {
                        double lon = loc["longitude"].ToDouble();
                        double lat = loc["latitude"].ToDouble();
                        loc["coordinates"] = new BsonArray { lon, lat };
                        loc.Remove("longitude");
                        loc.Remove("latitude");
                    }
                    else
                    {
                        loc["coordinates"] = new BsonArray();
                    }
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
                    addedCity["location"].AsBsonDocument.Contains("type") &&
                    addedCity["location"]["coordinates"].IsBsonArray)
                {
                    var coordinates = addedCity["location"]["coordinates"].AsBsonArray;
                    if (coordinates.Count >= 2)
                    {
                        longitude = coordinates[0].IsDouble ? coordinates[0].AsDouble : null;
                        latitude = coordinates[1].IsDouble ? coordinates[1].AsDouble : null;
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
                    location = (addedCity.Contains("location") && addedCity["location"].IsBsonDocument &&
                addedCity["location"].AsBsonDocument.Contains("coordinates") &&
                addedCity["location"]["coordinates"].IsBsonArray &&
                addedCity["location"]["coordinates"].AsBsonArray.Count >= 2)
                ? new
                {
                    longitude = addedCity["location"]["coordinates"].AsBsonArray[0].ToDouble(),
                    latitude = addedCity["location"]["coordinates"].AsBsonArray[1].ToDouble()
                }
                : null
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
                if (!ObjectId.TryParse(cityId, out ObjectId objectId))
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Invalid city ID format."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

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

                // Преобразуем JSON в BsonDocument
                var cityJson = cityData.RootElement.GetRawText();
                var updateBson = BsonDocument.Parse(cityJson);

                // Создаем новый BsonDocument для обновленного города
                var updatedCityBson = new BsonDocument();

                // Сохраняем ID существующего города
                updatedCityBson["_id"] = existingCity["_id"];

                // Копируем name, province, url из обновления или из существующего документа
                updatedCityBson["name"] = updateBson.Contains("name") ? updateBson["name"] :
                    (existingCity.Contains("name") ? existingCity["name"] : BsonNull.Value);

                updatedCityBson["province"] = updateBson.Contains("province") ? updateBson["province"] :
                    (existingCity.Contains("province") ? existingCity["province"] : BsonNull.Value);

                // Обновляем URL на основе имени
                if (updatedCityBson.Contains("name") && !updatedCityBson["name"].IsBsonNull)
                {
                    string url = updatedCityBson["name"].AsString.ToLower().Replace(" ", "_");
                    updatedCityBson["url"] = url;
                }
                else if (existingCity.Contains("url"))
                {
                    updatedCityBson["url"] = existingCity["url"];
                }

                // Проверка на дубликат имени города в провинции
                if (updatedCityBson.Contains("name") && updatedCityBson.Contains("province") &&
                    !updatedCityBson["name"].IsBsonNull && !updatedCityBson["province"].IsBsonNull)
                {
                    string updatedCityName = updatedCityBson["name"].AsString;
                    string updatedProvince = updatedCityBson["province"].AsString;

                    var duplicateFilter = Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("name", updatedCityName),
                        Builders<BsonDocument>.Filter.Eq("province", updatedProvince),
                        Builders<BsonDocument>.Filter.Ne("_id", objectId)
                    );

                    var duplicateCity = await _citiesCollection.Find(duplicateFilter).FirstOrDefaultAsync();
                    if (duplicateCity != null)
                    {
                        var errorResponse = new
                        {
                            IsSuccess = false,
                            Message = $"City with name '{updatedCityName}' already exists in province '{updatedProvince}'."
                        };
                        return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                    }
                }

                // Обработка координат для GeoJSON
                if (updateBson.Contains("latitude") && updateBson.Contains("longitude") &&
                    !updateBson["latitude"].IsBsonNull && !updateBson["longitude"].IsBsonNull)
                {
                    double lat = updateBson["latitude"].ToDouble();
                    double lon = updateBson["longitude"].ToDouble();

                    var location = new BsonDocument
            {
                { "type", "Point" },
                { "coordinates", new BsonArray { lon, lat } }
            };

                    updatedCityBson["location"] = location;
                }
                else if (existingCity.Contains("location"))
                {
                    // Сохраняем существующую локацию, если новая не предоставлена
                    updatedCityBson["location"] = existingCity["location"];
                }

                // Заменяем существующий документ обновленным
                await _citiesCollection.ReplaceOneAsync(filter, updatedCityBson);

                // Получаем обновленный город из базы
                var updatedCity = await _citiesCollection.Find(filter).FirstOrDefaultAsync();

                // Извлекаем координаты, если они есть
                double? longitude = null;
                double? latitude = null;
                if (updatedCity.Contains("location") &&
                    updatedCity["location"].IsBsonDocument &&
                    updatedCity["location"]["coordinates"].IsBsonArray)
                {
                    var coordinates = updatedCity["location"]["coordinates"].AsBsonArray;
                    if (coordinates.Count >= 2)
                    {
                        longitude = coordinates[0].IsDouble ? coordinates[0].AsDouble : null;
                        latitude = coordinates[1].IsDouble ? coordinates[1].AsDouble : null;
                    }
                }

                // Формируем ответ в нужном формате
                var formattedCity = new
                {
                    id = updatedCity["_id"].AsObjectId.ToString(),
                    name = updatedCity.Contains("name") ? updatedCity["name"].AsString : string.Empty,
                    province = updatedCity.Contains("province") ? updatedCity["province"].AsString : string.Empty,
                    url = updatedCity.Contains("url") ? updatedCity["url"].AsString : string.Empty,
                    location = new
                    {
                        longitude,
                        latitude
                    }
                };

                var successResponse = new
                {
                    IsSuccess = true,
                    Message = "City has been successfully updated.",
                    Data = formattedCity
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