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
                //return JsonDocument.Parse(JsonSerializer.Serialize(successResponse));
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
        public async Task<JsonDocument> AddCityToProvinceAsync(string provinceName, string cityName)
        {
            try
            {
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

                var cities = province["cities"].AsBsonArray;
                if (cities.Any(city => city.AsBsonDocument["name"].AsString == cityName))
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "City with the same name already exists in the province."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                var newCity = new BsonDocument { { "name", cityName } };
                cities.Add(newCity);

                var update = Builders<BsonDocument>.Update.Set("cities", cities);
                await _citiesCollection.UpdateOneAsync(filter, update);

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
        public async Task<JsonDocument> UpdateCityNameAsync(string provinceName, string oldCityName, string newCityName)
        {
            try
            {
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

                var cities = province["cities"].AsBsonArray;
                var cityToUpdate = cities.FirstOrDefault(city => city.AsBsonDocument["name"].AsString == oldCityName);

                if (cityToUpdate == null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "City not found in the specified province."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                cityToUpdate["name"] = newCityName;

                var update = Builders<BsonDocument>.Update.Set("cities", cities);
                var updateResult = await _citiesCollection.UpdateOneAsync(filter, update);

                if (updateResult.ModifiedCount == 0)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "City name update failed due to an unknown reason."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                var successResponse = new
                {
                    IsSuccess = true,
                    Message = $"City name updated successfully from '{oldCityName}' to '{newCityName}' in province '{provinceName}'."
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
        public async Task<JsonDocument> DeleteCityFromProvinceAsync(string provinceName, string cityName)
        {
            try
            {
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
                var cities = province["cities"].AsBsonArray;
                var cityToDelete = cities.FirstOrDefault(city => city.AsBsonDocument["name"].AsString == cityName);
                if (cityToDelete == null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "City not found in the specified province."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }
                cities.Remove(cityToDelete);
                var update = Builders<BsonDocument>.Update.Set("cities", cities);
                await _citiesCollection.UpdateOneAsync(filter, update);
                var successResponse = new
                {
                    IsSuccess = true,
                    Message = $"City '{cityName}' has been successfully deleted from province '{provinceName}'."
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
