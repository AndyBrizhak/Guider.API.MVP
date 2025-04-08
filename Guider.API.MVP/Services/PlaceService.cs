namespace Guider.API.MVP.Services
{
    using Guider.API.MVP.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using MongoDB.Bson;
    using MongoDB.Driver;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    public class PlaceService
    {
        private readonly IMongoCollection<BsonDocument> _placeCollection;

        public PlaceService(IOptions<MongoDbSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _placeCollection = database.GetCollection<BsonDocument>(mongoSettings.Value.CollectionName);
        }

        /// <summary>
        /// Получить все документы из коллекции Places
        /// </summary>
        /// <returns></returns>
        public async Task<List<BsonDocument>> GetAllAsync() =>
            await _placeCollection.Find(_ => true).ToListAsync();

        /// <summary>
        /// Получить документы из коллекции с пагинацией
        /// </summary>
        /// <param name="pageNumber">Номер страницы</param>
        /// <param name="pageSize">Размер страницы</param>
        /// <returns></returns>
        public async Task<List<BsonDocument>> GetAllPagedAsync(int pageNumber, int pageSize)
        {
            return await _placeCollection.Find(_ => true)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }



        //public async Task<BsonDocument?> GetByIdAsync(string id) =>
        //await _placeCollection.Find(b => b["_id"] == ObjectId.Parse(id)).FirstOrDefaultAsync();
        /// <summary>
        /// Получить объект по идентификатору
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<BsonDocument?> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return null; // Если id невалидный, просто возвращаем null
            }

            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
            return await _placeCollection.Find(filter).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Получить документ по локальнуому url
        /// </summary>
        /// <param name="web"></param>
        /// <returns></returns>
        public async Task<BsonDocument> GetPlaceByWebAsync(string web)
        {
            if (string.IsNullOrEmpty(web))
            {
                return null; // Избегаем ненужного запроса, если параметр пустой
            }

            var filter = Builders<BsonDocument>.Filter.Eq("web", web);
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
                return null; // Защита от невалидного ObjectId
            }

            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
            return await _placeCollection.Find(filter).FirstOrDefaultAsync();
        }


        //public async Task CreateAsync(BsonDocument place) =>
        //    await _placeCollection.InsertOneAsync(place);

        //public async Task UpdateAsync(string id, BsonDocument updatedPlace)
        //{
        //    var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id));
        //    return await _placeCollection.ReplaceOneAsync(filter, updatedPlace);
        //}

        //public async Task DeleteAsync(string id)
        //{
        //    var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id));
        //    return await _placeCollection.DeleteOneAsync(filter);
        //}


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
                    "web", 1
                }
            })
        };

            var result = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return result;
        }

        /// <summary>
        /// Получить отсортированный по дистанции список данных об объектах 
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="radiusMeters"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public async Task<string> GetNearbyPlacesAsyncCenter(decimal latitude, decimal longitude, int radiusMeters, int limit)
        {
            var pipeline = new[]
            {
            new BsonDocument("$geoNear", new BsonDocument
            {
                ///*{ "near", new BsonDocument { { "type", "Point" }, { "coordinates", new BsonArray { longitude, latitude } }*/ } },
                { "near", new BsonArray { longitude, latitude } },  // Массив координат
                { "distanceField", "distance" },
                { "maxDistance", radiusMeters },
                { "spherical", true },
                //{ "limit", limit }
            })
            ,
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 1 },
                { "category", 1 },
                { "name", 1 },
                { "location.coordinates", 1 },
                {
                    "web", 1
                }

            }),
            new BsonDocument("$limit", limit) // Ограничение результата
            };

            var results = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            if (results.Count == 0)
            {
                return "[]"; // Возвращаем пустой массив
            }
            return results.ToJson();  // Возвращаем JSON-строку
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
            { "web", 1 },
            { "category", 1 },
            { "tags", 1 }
        });
            // Фильтрация по категории
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
                    { "web", 1 }
                });

            var limitStage = new BsonDocument("$limit", limit);

            var pipeline = new[] { geoNearStage, matchStage, projectStage, limitStage };

            var results = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results;
        }

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

            // Если список ключевых слов не пуст, создаем стадию $match
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
        { "web", 1 }
    });

            var limitStage = new BsonDocument("$limit", limit);

            // Формируем пайплайн в зависимости от наличия matchStage
            var pipeline = matchStage != null
                ? new[] { geoNearStage, matchStage, projectStage, limitStage }
                : new[] { geoNearStage, projectStage, limitStage };

            var results = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results;
        }

    }
}

