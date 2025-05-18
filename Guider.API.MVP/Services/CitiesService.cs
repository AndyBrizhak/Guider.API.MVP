//using Guider.API.MVP.Data;
//using Microsoft.Extensions.Options;
//using MongoDB.Bson;
//using MongoDB.Driver;
//using System.Text.Json;

//namespace Guider.API.MVP.Services
//{
//    public class CitiesService
//    {
//        private readonly IMongoCollection<BsonDocument> _citiesCollection;

//        public CitiesService(IOptions<MongoDbSettings> mongoSettings)
//        {
//            var client = new MongoClient(mongoSettings.Value.ConnectionString);
//            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
//            _citiesCollection = database.GetCollection<BsonDocument>(
//                mongoSettings.Value.Collections["Cities"]);
//        }
//        public async Task<JsonDocument> GetCitiesByProvinceAsync(string provinceName)
//        {
//            try
//            {
//                var filter = Builders<BsonDocument>.Filter.Eq("name", provinceName);
//                var projection = Builders<BsonDocument>.Projection.Include("cities").Exclude("_id");
//                var result = await _citiesCollection.Find(filter).Project(projection).FirstOrDefaultAsync();

//                if (result == null || !result.Contains("cities"))
//                {
//                    var errorResponse = new
//                    {
//                        IsSuccess = false,
//                        Message = "Province not found or no cities available."
//                    };
//                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//                }

//                var cities = result["cities"].AsBsonArray.Select(city => city.AsBsonDocument).ToList();
//                var successResponse = new
//                {
//                    IsSuccess = true,
//                    Cities = cities
//                }.ToJson();
//                return JsonDocument.Parse(successResponse);

//            }
//            catch (Exception ex)
//            {
//                var errorResponse = new
//                {
//                    IsSuccess = false,
//                    Message = $"An error occurred: {ex.Message}"
//                };
//                return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//            }
//        }

//        public async Task<JsonDocument> AddCityToProvinceAsync(string provinceName, JsonDocument cityData)
//        {
//            try
//            {

//                var cityJson = cityData.RootElement.GetRawText();
//                var cityBson = BsonDocument.Parse(cityJson);


//                string cityName = "";
//                if (cityBson.Contains("name") && cityBson["name"].BsonType == BsonType.String)
//                {
//                    cityName = cityBson["name"].AsString;
//                }
//                else
//                {
//                    var errorResponse = new
//                    {
//                        IsSuccess = false,
//                        Message = "City data must contain a 'name' field."
//                    };
//                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//                }

//                var filter = Builders<BsonDocument>.Filter.Eq("name", provinceName);
//                var province = await _citiesCollection.Find(filter).FirstOrDefaultAsync();

//                if (province == null)
//                {
//                    var errorResponse = new
//                    {
//                        IsSuccess = false,
//                        Message = "Province not found."
//                    };
//                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//                }


//                if (!province.Contains("cities") || province["cities"].BsonType != BsonType.Array)
//                {

//                    var update = Builders<BsonDocument>.Update.Set("cities", new BsonArray());
//                    await _citiesCollection.UpdateOneAsync(filter, update);


//                    province = await _citiesCollection.Find(filter).FirstOrDefaultAsync();
//                }

//                var cities = province["cities"].AsBsonArray;


//                bool cityExists = false;
//                foreach (var city in cities)
//                {
//                    if (city.IsBsonDocument &&
//                        city.AsBsonDocument.Contains("name") &&
//                        city.AsBsonDocument["name"].BsonType == BsonType.String &&
//                        city.AsBsonDocument["name"].AsString == cityName)
//                    {
//                        cityExists = true;
//                        break;
//                    }
//                }

//                if (cityExists)
//                {
//                    var errorResponse = new
//                    {
//                        IsSuccess = false,
//                        Message = "City with the same name already exists in the province."
//                    };
//                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//                }


//                cities.Add(cityBson);
//                var updateCities = Builders<BsonDocument>.Update.Set("cities", cities);
//                await _citiesCollection.UpdateOneAsync(filter, updateCities);

//                var successResponse = new
//                {
//                    IsSuccess = true,
//                    Message = $"City '{cityName}' has been successfully added to province '{provinceName}'."
//                };
//                return JsonDocument.Parse(JsonSerializer.Serialize(successResponse));
//            }
//            catch (Exception ex)
//            {
//                var errorResponse = new
//                {
//                    IsSuccess = false,
//                    Message = $"An error occurred: {ex.Message}"
//                };
//                return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//            }
//        }

//        public async Task<JsonDocument> UpdateCityInProvinceAsync(string provinceName, string cityName, JsonDocument cityData)
//        {
//            try
//            {

//                var cityJson = cityData.RootElement.GetRawText();
//                var updatedCityBson = BsonDocument.Parse(cityJson);


//                string updatedCityName = "";
//                if (updatedCityBson.Contains("name") && updatedCityBson["name"].BsonType == BsonType.String)
//                {
//                    updatedCityName = updatedCityBson["name"].AsString;
//                }
//                else
//                {
//                    var errorResponse = new
//                    {
//                        IsSuccess = false,
//                        Message = "Updated city data must contain a 'name' field."
//                    };
//                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//                }


//                var filter = Builders<BsonDocument>.Filter.Eq("name", provinceName);
//                var province = await _citiesCollection.Find(filter).FirstOrDefaultAsync();

//                if (province == null)
//                {
//                    var errorResponse = new
//                    {
//                        IsSuccess = false,
//                        Message = "Province not found."
//                    };
//                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//                }


//                if (!province.Contains("cities") || province["cities"].BsonType != BsonType.Array)
//                {
//                    var errorResponse = new
//                    {
//                        IsSuccess = false,
//                        Message = $"Province '{provinceName}' does not have any cities."
//                    };
//                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//                }


//                var cities = province["cities"].AsBsonArray;


//                int cityIndex = -1;
//                for (int i = 0; i < cities.Count; i++)
//                {
//                    if (cities[i].IsBsonDocument &&
//                        cities[i].AsBsonDocument.Contains("name") &&
//                        cities[i].AsBsonDocument["name"].BsonType == BsonType.String &&
//                        cities[i].AsBsonDocument["name"].AsString == cityName)
//                    {
//                        cityIndex = i;
//                        break;
//                    }
//                }


//                if (cityIndex == -1)
//                {
//                    var errorResponse = new
//                    {
//                        IsSuccess = false,
//                        Message = $"City '{cityName}' not found in province '{provinceName}'."
//                    };
//                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//                }




//                cities[cityIndex] = updatedCityBson;


//                var updateCities = Builders<BsonDocument>.Update.Set("cities", cities);
//                await _citiesCollection.UpdateOneAsync(filter, updateCities);

//                var successResponse = new
//                {
//                    IsSuccess = true,
//                    Message = $"City '{cityName}' has been successfully updated to '{updatedCityName}' in province '{provinceName}'."
//                };

//                return JsonDocument.Parse(JsonSerializer.Serialize(successResponse));
//            }
//            catch (Exception ex)
//            {
//                var errorResponse = new
//                {
//                    IsSuccess = false,
//                    Message = $"An error occurred: {ex.Message}"
//                };
//                return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//            }
//        }

//        public async Task<JsonDocument> GetCityByNameAndProvinceAsync(string provinceName, string cityName)
//        {
//            try
//            {

//                var filter = Builders<BsonDocument>.Filter.Eq("name", provinceName);


//                var provinceDoc = await _citiesCollection.Find(filter)
//                    .Project(Builders<BsonDocument>.Projection.Exclude("_id"))
//                    .FirstOrDefaultAsync();


//                if (provinceDoc == null)
//                {
//                    var errorResponse = new
//                    {
//                        IsSuccess = false,
//                        Message = $"Province '{provinceName}' not found."
//                    };
//                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//                }


//                if (!provinceDoc.Contains("cities") || provinceDoc["cities"].AsBsonArray.Count == 0)
//                {
//                    var errorResponse = new
//                    {
//                        IsSuccess = false,
//                        Message = $"No cities found in province '{provinceName}'."
//                    };
//                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//                }


//                BsonDocument cityData = null;
//                foreach (var city in provinceDoc["cities"].AsBsonArray)
//                {
//                    var cityDoc = city.AsBsonDocument;
//                    if (cityDoc["name"].AsString.Equals(cityName, StringComparison.OrdinalIgnoreCase))
//                    {
//                        cityData = cityDoc;
//                        break;
//                    }
//                }




//                if (cityData == null)
//                {
//                    var errorResponse = new
//                    {
//                        IsSuccess = false,
//                        Message = $"City '{cityName}' not found in province '{provinceName}'."
//                    };
//                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//                }


//                var provinceInfo = new
//                {
//                    Name = provinceDoc["name"].AsString
//                };


//                var cityInfo = new
//                {
//                    Name = cityData["name"].AsString,
//                    Web = cityData.Contains("web") ? cityData["web"].AsString : string.Empty,
//                    Latitude = cityData.Contains("latitude") ? cityData["latitude"].AsDouble : 0.0,
//                    Longitude = cityData.Contains("longitude") ? cityData["longitude"].AsDouble : 0.0

//                };


//                var successResponse = new
//                {
//                    IsSuccess = true,
//                    Province = provinceInfo,
//                    City = cityInfo
//                };

//                return JsonDocument.Parse(JsonSerializer.Serialize(successResponse));
//            }
//            catch (Exception ex)
//            {
//                var errorResponse = new
//                {
//                    IsSuccess = false,
//                    Message = $"An error occurred: {ex.Message}"
//                };
//                return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//            }
//        }

//        public async Task<JsonDocument> RemoveCityFromProvinceAsync(string provinceName, string cityName)
//        {
//            try
//            {
//                // Filter to find the province by name
//                var filter = Builders<BsonDocument>.Filter.Eq("name", provinceName);

//                // Retrieve the province document
//                var province = await _citiesCollection.Find(filter).FirstOrDefaultAsync();

//                if (province == null)
//                {
//                    var errorResponse = new
//                    {
//                        IsSuccess = false,
//                        Message = $"Province '{provinceName}' not found."
//                    };
//                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//                }

//                // Check if the province contains cities
//                if (!province.Contains("cities") || province["cities"].BsonType != BsonType.Array)
//                {
//                    var errorResponse = new
//                    {
//                        IsSuccess = false,
//                        Message = $"No cities found in province '{provinceName}'."
//                    };
//                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//                }

//                var cities = province["cities"].AsBsonArray;

//                // Find the city to remove
//                var cityToRemove = cities.FirstOrDefault(city =>
//                    city.IsBsonDocument &&
//                    city.AsBsonDocument.Contains("name") &&
//                    city.AsBsonDocument["name"].BsonType == BsonType.String &&
//                    city.AsBsonDocument["name"].AsString.Equals(cityName, StringComparison.OrdinalIgnoreCase));

//                if (cityToRemove == null)
//                {
//                    var errorResponse = new
//                    {
//                        IsSuccess = false,
//                        Message = $"City '{cityName}' not found in province '{provinceName}'."
//                    };
//                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//                }

//                // Remove the city from the array
//                cities.Remove(cityToRemove);

//                // Update the province document with the modified cities array
//                var update = Builders<BsonDocument>.Update.Set("cities", cities);
//                await _citiesCollection.UpdateOneAsync(filter, update);

//                var successResponse = new
//                {
//                    IsSuccess = true,
//                    Message = $"City '{cityName}' has been successfully removed from province '{provinceName}'."
//                };
//                return JsonDocument.Parse(JsonSerializer.Serialize(successResponse));
//            }
//            catch (Exception ex)
//            {
//                var errorResponse = new
//                {
//                    IsSuccess = false,
//                    Message = $"An error occurred: {ex.Message}"
//                };
//                return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
//            }
//        }

//    }
//}

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

        public async Task<JsonDocument> GetCitiesByProvinceAsync(string provinceName)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("province", provinceName);
                var projection = Builders<BsonDocument>.Projection.Exclude("_id");
                var cities = await _citiesCollection.Find(filter).Project(projection).ToListAsync();

                if (cities == null || cities.Count == 0)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Province not found or no cities available."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                var successResponse = new
                {
                    IsSuccess = true,
                    Cities = cities
                }.ToJson();
                return JsonDocument.Parse(successResponse);
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

                // Если нет url (web), но есть name, создаем его
                if (!cityBson.Contains("url") && cityBson.Contains("name"))
                {
                    string url = cityBson["name"].AsString.ToLower().Replace(" ", "_");
                    cityBson.Add("url", url);
                }
                else if (cityBson.Contains("web") && !cityBson.Contains("url"))
                {
                    // Если есть web, но нет url, используем web как url
                    cityBson.Add("url", cityBson["web"]);
                    cityBson.Remove("web");
                }

                // Добавление города в коллекцию
                await _citiesCollection.InsertOneAsync(cityBson);

                var successResponse = new
                {
                    IsSuccess = true,
                    Message = $"City '{cityName}' has been successfully added to province '{provinceName}'.",
                    City = new
                    {
                        Id = cityBson["_id"].AsObjectId.ToString(),
                        Name = cityName,
                        Province = provinceName,
                        Url = cityBson.Contains("url") ? cityBson["url"].AsString : null,
                        Location = cityBson.Contains("location") ? new
                        {
                            Type = cityBson["location"]["type"].AsString,
                            Coordinates = new double[]
                            {
                        cityBson["location"]["coordinates"][0].AsDouble,
                        cityBson["location"]["coordinates"][1].AsDouble
                            }
                        } : null
                    }
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

                // Обработка url/web полей
                if (!updatedCityBson.Contains("url") && updatedCityBson.Contains("name"))
                {
                    string url = updatedCityBson["name"].AsString.ToLower().Replace(" ", "_");
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

                var successResponse = new
                {
                    IsSuccess = true,
                    Message = $"City '{currentCityName}' has been successfully updated to '{updatedCityName}' in province '{updatedProvince}'."
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

        public async Task<JsonDocument> GetCityByNameAndProvinceAsync(string provinceName, string cityName)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("name", cityName),
                    Builders<BsonDocument>.Filter.Eq("province", provinceName)
                );

                var projection = Builders<BsonDocument>.Projection.Exclude("_id");
                var cityDoc = await _citiesCollection.Find(filter).Project(projection).FirstOrDefaultAsync();

                if (cityDoc == null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"City '{cityName}' not found in province '{provinceName}'."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Извлекаем координаты из location если доступны
                double latitude = 0.0;
                double longitude = 0.0;
                if (cityDoc.Contains("location") &&
                    cityDoc["location"].IsBsonDocument &&
                    cityDoc["location"].AsBsonDocument.Contains("coordinates") &&
                    cityDoc["location"]["coordinates"].IsBsonArray)
                {
                    var coordinates = cityDoc["location"]["coordinates"].AsBsonArray;
                    if (coordinates.Count >= 2)
                    {
                        longitude = coordinates[0].AsDouble;
                        latitude = coordinates[1].AsDouble;
                    }
                }

                // Формируем ответ
                var provinceInfo = new
                {
                    Name = provinceName
                };

                var cityInfo = new
                {
                    Name = cityDoc["name"].AsString,
                    Url = cityDoc.Contains("url") ? cityDoc["url"].AsString :
                          (cityDoc.Contains("web") ? cityDoc["web"].AsString : string.Empty),
                    Latitude = latitude,
                    Longitude = longitude
                };

                var successResponse = new
                {
                    IsSuccess = true,
                    Province = provinceInfo,
                    City = cityInfo
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