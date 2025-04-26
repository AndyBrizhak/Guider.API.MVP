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




    }
}
