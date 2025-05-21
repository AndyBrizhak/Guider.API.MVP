

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

                string currentCityName = existingCity.Contains("name") ? existingCity["name"].AsString : "Unknown";
                string currentProvince = existingCity.Contains("province") ? existingCity["province"].AsString : "Unknown";

                var cityJson = cityData.RootElement.GetRawText();
                var updateBson = BsonDocument.Parse(cityJson);
                var mergedCityBson = existingCity.DeepClone().AsBsonDocument;

                // Only update fields that have non-empty/non-null values
                foreach (var element in updateBson.Elements)
                {
                    if (element.Value.IsBsonNull)
                        continue;

                    if (element.Value.BsonType == BsonType.String)
                    {
                        var strVal = element.Value.AsString;
                        if (!string.IsNullOrEmpty(strVal))
                            mergedCityBson[element.Name] = element.Value;
                        // else: skip updating this field
                    }
                    else if (element.Value.BsonType == BsonType.Double || element.Value.BsonType == BsonType.Int32 || element.Value.BsonType == BsonType.Int64)
                    {
                        // For numbers, only update if not null (already checked above)
                        mergedCityBson[element.Name] = element.Value;
                    }
                    else if (element.Value.BsonType == BsonType.Boolean)
                    {
                        mergedCityBson[element.Name] = element.Value;
                    }
                    else if (element.Value.BsonType == BsonType.Document || element.Value.BsonType == BsonType.Array)
                    {
                        mergedCityBson[element.Name] = element.Value;
                    }
                    // else: skip nulls and empty strings
                }

                // Handle coordinates: only update if both latitude and longitude are present and not null
                bool hasLatitude = updateBson.Contains("latitude") && !updateBson["latitude"].IsBsonNull;
                bool hasLongitude = updateBson.Contains("longitude") && !updateBson["longitude"].IsBsonNull;
                bool validLat = hasLatitude && (updateBson["latitude"].IsDouble || updateBson["latitude"].IsInt32 || updateBson["latitude"].IsInt64);
                bool validLon = hasLongitude && (updateBson["longitude"].IsDouble || updateBson["longitude"].IsInt32 || updateBson["longitude"].IsInt64);

                if (validLat && validLon)
                {
                    double lat = updateBson["latitude"].ToDouble();
                    double lon = updateBson["longitude"].ToDouble();
                    var location = new BsonDocument
                    {
                        { "type", "Point" },
                        { "coordinates", new BsonArray { lon, lat } }
                    };
                    mergedCityBson["location"] = location;
                    mergedCityBson.Remove("latitude");
                    mergedCityBson.Remove("longitude");
                }

                // url/web logic
                if (!mergedCityBson.Contains("url") && mergedCityBson.Contains("name"))
                {
                    string url = mergedCityBson["name"].AsString.ToLower().Replace(" ", "-");
                    mergedCityBson["url"] = url;
                }
                //else if (mergedCityBson.Contains("web") && !mergedCityBson.Contains("url"))
                //{
                //    mergedCityBson["url"] = mergedCityBson["web"];
                //    mergedCityBson.Remove("web");
                //}

                // Duplicate check if name or province changed
                string updatedCityName = mergedCityBson.Contains("name") ? mergedCityBson["name"].AsString : currentCityName;
                string updatedProvince = mergedCityBson.Contains("province") ? mergedCityBson["province"].AsString : currentProvince;
                if ((updateBson.Contains("name") && !string.IsNullOrEmpty(updateBson["name"].AsString) && updatedCityName != currentCityName) ||
                    (updateBson.Contains("province") && !string.IsNullOrEmpty(updateBson["province"].AsString) && updatedProvince != currentProvince))
                {
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

                mergedCityBson["_id"] = existingCity["_id"];

                await _citiesCollection.ReplaceOneAsync(filter, mergedCityBson);

                var updatedCity = await _citiesCollection.Find(filter).FirstOrDefaultAsync();

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
                        longitude = coordinates[0].IsDouble ? coordinates[0].AsDouble : null;
                        latitude = coordinates[1].IsDouble ? coordinates[1].AsDouble : null;
                    }
                }

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