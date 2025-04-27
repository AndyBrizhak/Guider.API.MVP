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
                var filter = Builders<BsonDocument>.Filter.Eq("name", provinceName);
                var projection = Builders<BsonDocument>.Projection.Include("cities").Exclude("_id");
                var result = await _citiesCollection.Find(filter).Project(projection).FirstOrDefaultAsync();

                if (result == null || !result.Contains("cities"))
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Province not found or no cities available."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                var cities = result["cities"].AsBsonArray.Select(city => city.AsBsonDocument).ToList();
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

        public async Task<JsonDocument> AddCityToProvinceAsync(string provinceName, JsonDocument cityData)
        {
            try
            {
                
                var cityJson = cityData.RootElement.GetRawText();
                var cityBson = BsonDocument.Parse(cityJson);

                
                string cityName = "";
                if (cityBson.Contains("name") && cityBson["name"].BsonType == BsonType.String)
                {
                    cityName = cityBson["name"].AsString;
                }
                else
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "City data must contain a 'name' field."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                var filter = Builders<BsonDocument>.Filter.Eq("name", provinceName);
                var province = await _citiesCollection.Find(filter).FirstOrDefaultAsync();

                if (province == null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Province not found."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                
                if (!province.Contains("cities") || province["cities"].BsonType != BsonType.Array)
                {
                    
                    var update = Builders<BsonDocument>.Update.Set("cities", new BsonArray());
                    await _citiesCollection.UpdateOneAsync(filter, update);

                    
                    province = await _citiesCollection.Find(filter).FirstOrDefaultAsync();
                }

                var cities = province["cities"].AsBsonArray;

                
                bool cityExists = false;
                foreach (var city in cities)
                {
                    if (city.IsBsonDocument &&
                        city.AsBsonDocument.Contains("name") &&
                        city.AsBsonDocument["name"].BsonType == BsonType.String &&
                        city.AsBsonDocument["name"].AsString == cityName)
                    {
                        cityExists = true;
                        break;
                    }
                }

                if (cityExists)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "City with the same name already exists in the province."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                
                cities.Add(cityBson);
                var updateCities = Builders<BsonDocument>.Update.Set("cities", cities);
                await _citiesCollection.UpdateOneAsync(filter, updateCities);

                var successResponse = new
                {
                    IsSuccess = true,
                    Message = $"City '{cityName}' has been successfully added to province '{provinceName}'."
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

        public async Task<JsonDocument> UpdateCityInProvinceAsync(string provinceName, string cityName, JsonDocument cityData)
        {
            try
            {
                // Преобразуем входные данные о городе из JsonDocument в BsonDocument
                var cityJson = cityData.RootElement.GetRawText();
                var updatedCityBson = BsonDocument.Parse(cityJson);

                // Проверяем, что в обновленных данных содержится поле name
                string updatedCityName = "";
                if (updatedCityBson.Contains("name") && updatedCityBson["name"].BsonType == BsonType.String)
                {
                    updatedCityName = updatedCityBson["name"].AsString;
                }
                else
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Updated city data must contain a 'name' field."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Находим провинцию по имени
                var filter = Builders<BsonDocument>.Filter.Eq("name", provinceName);
                var province = await _citiesCollection.Find(filter).FirstOrDefaultAsync();

                if (province == null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Province not found."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Проверяем, существует ли массив городов в провинции
                if (!province.Contains("cities") || province["cities"].BsonType != BsonType.Array)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"Province '{provinceName}' does not have any cities."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Получаем массив городов
                var cities = province["cities"].AsBsonArray;

                // Ищем город для обновления
                int cityIndex = -1;
                for (int i = 0; i < cities.Count; i++)
                {
                    if (cities[i].IsBsonDocument &&
                        cities[i].AsBsonDocument.Contains("name") &&
                        cities[i].AsBsonDocument["name"].BsonType == BsonType.String &&
                        cities[i].AsBsonDocument["name"].AsString == cityName)
                    {
                        cityIndex = i;
                        break;
                    }
                }

                // Если город не найден
                if (cityIndex == -1)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"City '{cityName}' not found in province '{provinceName}'."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Проверяем, если новое имя города уже существует в другом городе провинции
                //if (cityName != updatedCityName)
                //{
                //    bool nameExists = false;
                //    foreach (var city in cities)
                //    {
                //        if (city.IsBsonDocument &&
                //            city.AsBsonDocument.Contains("name") &&
                //            city.AsBsonDocument["name"].BsonType == BsonType.String &&
                //            city.AsBsonDocument["name"].AsString == updatedCityName &&
                //            city != cities[cityIndex])
                //        {
                //            nameExists = true;
                //            break;
                //        }
                //    }

                //    if (nameExists)
                //    {
                //        var errorResponse = new
                //        {
                //            IsSuccess = false,
                //            Message = $"Another city with the name '{updatedCityName}' already exists in the province."
                //        };
                //        return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                //    }
                //}

                // Обновляем город в массиве
                cities[cityIndex] = updatedCityBson;

                // Обновляем документ в коллекции
                var updateCities = Builders<BsonDocument>.Update.Set("cities", cities);
                await _citiesCollection.UpdateOneAsync(filter, updateCities);

                var successResponse = new
                {
                    IsSuccess = true,
                    Message = $"City '{cityName}' has been successfully updated to '{updatedCityName}' in province '{provinceName}'."
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

        public async Task<JsonDocument> GetCityByNameAndProvinceAsync(string provinceName, string cityName)
        {
            try
            {
                
                var filter = Builders<BsonDocument>.Filter.Eq("name", provinceName);

                
                var provinceDoc = await _citiesCollection.Find(filter)
                    .Project(Builders<BsonDocument>.Projection.Exclude("_id"))
                    .FirstOrDefaultAsync();

                
                if (provinceDoc == null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"Province '{provinceName}' not found."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                
                if (!provinceDoc.Contains("cities") || provinceDoc["cities"].AsBsonArray.Count == 0)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"No cities found in province '{provinceName}'."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                
                BsonDocument cityData = null;
                foreach (var city in provinceDoc["cities"].AsBsonArray)
                {
                    var cityDoc = city.AsBsonDocument;
                    if (cityDoc["name"].AsString.Equals(cityName, StringComparison.OrdinalIgnoreCase))
                    {
                        cityData = cityDoc;
                        break;
                    }
                }

                

                
                if (cityData == null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"City '{cityName}' not found in province '{provinceName}'."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                
                var provinceInfo = new
                {
                    Name = provinceDoc["name"].AsString
                };

                
                var cityInfo = new
                {
                    Name = cityData["name"].AsString,
                    Web = cityData.Contains("web") ? cityData["web"].AsString : string.Empty,
                    Latitude = cityData.Contains("latitude") ? cityData["latitude"].AsDouble : 0.0,
                    Longitude = cityData.Contains("longitude") ? cityData["longitude"].AsDouble : 0.0
                    
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

        public async Task<JsonDocument> RemoveCityFromProvinceAsync(string provinceName, string cityName)
        {
            try
            {
                // Filter to find the province by name
                var filter = Builders<BsonDocument>.Filter.Eq("name", provinceName);

                // Retrieve the province document
                var province = await _citiesCollection.Find(filter).FirstOrDefaultAsync();

                if (province == null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"Province '{provinceName}' not found."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Check if the province contains cities
                if (!province.Contains("cities") || province["cities"].BsonType != BsonType.Array)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"No cities found in province '{provinceName}'."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                var cities = province["cities"].AsBsonArray;

                // Find the city to remove
                var cityToRemove = cities.FirstOrDefault(city =>
                    city.IsBsonDocument &&
                    city.AsBsonDocument.Contains("name") &&
                    city.AsBsonDocument["name"].BsonType == BsonType.String &&
                    city.AsBsonDocument["name"].AsString.Equals(cityName, StringComparison.OrdinalIgnoreCase));

                if (cityToRemove == null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"City '{cityName}' not found in province '{provinceName}'."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Remove the city from the array
                cities.Remove(cityToRemove);

                // Update the province document with the modified cities array
                var update = Builders<BsonDocument>.Update.Set("cities", cities);
                await _citiesCollection.UpdateOneAsync(filter, update);

                var successResponse = new
                {
                    IsSuccess = true,
                    Message = $"City '{cityName}' has been successfully removed from province '{provinceName}'."
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
