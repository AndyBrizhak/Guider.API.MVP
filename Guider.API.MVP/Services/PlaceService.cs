namespace Guider.API.MVP.Services
{
    using Guider.API.MVP.Data;
    using Guider.API.MVP.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using MongoDB.Bson;
    using MongoDB.Driver;
    using System.Collections.Generic;
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

              

        /// <summary>
        /// 
        /// Создать новый документ в коллекции Places
        /// 
        /// </summary>
        /// 
        /// <param name="jsonDocument">JSON-строка документа</param>
        /// 
        /// <returns>Созданный документ</returns>
        public async Task<BsonDocument> CreateAsync(JsonDocument jsonDocument)
        {
            try
            {
                // Convert JsonDocument to a JSON string
                var jsonString = jsonDocument.RootElement.GetRawText();

                // Deserialize the JSON string into a BsonDocument
                var document = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(jsonString);

                // Add additional fields if necessary
                if (!document.Contains("createdAt"))
                {
                    document.Add("createdAt", DateTime.UtcNow);
                }

                // Insert the document into the collection
                await _placeCollection.InsertOneAsync(document);

                // Return the created document
                return document;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating document: {ex.Message}");
            }
        }


        /// <summary>  
        /// Обновить существующий документ в коллекции Places  
        /// </summary>  
        /// <param name="jsonDocument">JSON-строка с обновленными данными, содержащая идентификатор</param>  
        /// <returns>Обновленный документ</returns>  
        public async Task<JsonDocument> UpdateAsync(JsonDocument jsonDocument)
        {
            try
            {
                // Convert JsonDocument to a JSON string  
                var jsonString = jsonDocument.RootElement.GetRawText();

                // Deserialize the JSON string into a BsonDocument  
                var updatedDocument = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(jsonString);

                // Ensure the "_id" field exists and is valid  
                if (!updatedDocument.Contains("_id") || !ObjectId.TryParse(updatedDocument["_id"].ToString(), out var objectId))
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new { success = false, message = "The document must contain a valid '_id' field." }));
                }

                // Define the filter to find the document by id  
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);

                // Replace the existing document with the updated one  
                await _placeCollection.ReplaceOneAsync(filter, updatedDocument);

                // Convert the updated BsonDocument back to JsonDocument  
                var updatedJsonString = updatedDocument.ToJson();
                return JsonDocument.Parse(updatedJsonString);
            }
             catch (Exception ex)
             {
                return JsonDocument.Parse(JsonSerializer.Serialize(new { success = false, error = ex.Message }));
             }
        }

        //public async Task DeleteAsync(string id)
        //{
        //    var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id));
        //    return await _placeCollection.DeleteOneAsync(filter);
        //}


        ///// <summary>
        ///// Гео с выводом отсортированного списка с id, distance, name, img_link
        ///// </summary>
        ///// <param name="lat"></param>
        ///// <param name="lng"></param>
        ///// <param name="maxDistanceMeters"></param>
        ///// <returns></returns>
        //public async Task<List<BsonDocument>> GetPlacesNearbyAsync(decimal lat, decimal lng, int maxDistanceMeters)
        //{
        //    var pipeline = new[]
        //    {
        //    new BsonDocument("$geoNear", new BsonDocument
        //    {
        //        { "near", new BsonDocument
        //            {
        //                { "type", "Point" },
        //                { "coordinates", new BsonArray { lng, lat } }
        //            }
        //        },
        //        { "distanceField", "distance" },
        //        { "maxDistance", maxDistanceMeters },
        //        { "spherical", true }
        //    }),
        //    new BsonDocument("$project", new BsonDocument
        //    {
        //        { "_id", 1 },
        //        { "distance", 1 },
        //        { "name", 1 },
        //        { "img_link", new BsonDocument
        //            {
        //                { "$arrayElemAt", new BsonArray { "$img_link", 0 } } // Первая ссылка на изображение
        //            }
        //        },
        //        {
        //            "web", 1
        //        }
        //    })
        //};

        //    var result = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
        //    return result;
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
                // Если не нужно учитывать расписание, используем существующий метод
                return await GetPlacesNearbyAsync(lat, lng, maxDistanceMeters);
            }

            // Получаем текущее время в Коста-Рике (GMT-6)
            var costaRicaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
            var currentTimeInCostaRica = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, costaRicaTimeZone);

            // Получаем текущий день недели и время
            var dayOfWeek = currentTimeInCostaRica.DayOfWeek.ToString();
            var currentTimeString = currentTimeInCostaRica.ToString("h:mm tt"); // Например, "8:30 AM"

            var pipeline = new[]
            {
        // Геопространственный поиск
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
        
        // Фильтрация по расписанию
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
        
        // Проекция нужных полей
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
                // Если не нужно учитывать расписание, используем существующий метод
                return await GetPlacesNearbyWithTextSearchAsync(lat, lng, maxDistanceMeters, limit, searchText);
            }

            // Получаем текущее время в Коста-Рике (GMT-6)
            var costaRicaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
            var currentTimeInCostaRica = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, costaRicaTimeZone);

            // Получаем текущий день недели и время
            var dayOfWeek = currentTimeInCostaRica.DayOfWeek.ToString();
            var currentTimeString = currentTimeInCostaRica.ToString("h:mm tt"); // Например, "8:30 AM"

            // Определяем этапы агрегационного пайплайна
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

            // Добавляем этап фильтрации по расписанию работы
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
            { "web", 1 }
        });

            var limitStage = new BsonDocument("$limit", limit);

            // Составляем полный пайплайн с учетом расписания работы
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

            // Добавляем фильтрацию по открытым заведениям, если isOpen = true
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
                { "web", 1 }
            });

            var limitStage = new BsonDocument("$limit", limit);

            // Формируем пайплайн в зависимости от наличия matchStage и scheduleMatchStage
            var pipeline = new List<BsonDocument> { geoNearStage };
            if (matchStage != null) pipeline.Add(matchStage);
            if (scheduleMatchStage != null) pipeline.Add(scheduleMatchStage);
            pipeline.Add(projectStage);
            pipeline.Add(limitStage);

            var results = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results;
        }


        /// <summary>
        ///
        /// Получить отсортированный список по дистанции с текстовым поиском, фильтрацией по ключевым словам
        /// 
        /// c использованием $and
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

            // Если список ключевых слов не пуст, создаем стадию $match
            BsonDocument? matchStage = null;
            if (filterKeywords != null && filterKeywords.Any())
            {
                // Создаем массив для условий AND
                var andConditions = new BsonArray();

                foreach (var keyword in filterKeywords)
                {
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        // Для каждого ключевого слова создаем OR-условие для всех полей
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

                        // Добавляем это OR-условие как один из элементов в массив AND-условий
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


        /// <summary>
        /// Получить отсортированный список по дистанции с текстовым поиском, фильтрацией по ключевым словам
        /// и фильтрацией по открытым заведениям
        /// с использованием $and
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <param name="maxDistanceMeters"></param>
        /// <param name="limit"></param>
        /// <param name="filterKeywords"></param>
        /// <param name="isOpen"></param>
        /// <returns></returns>
        public async Task<List<BsonDocument>> GetPlacesWithAllKeywordsAsync(
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

            // Если список ключевых слов не пуст, создаем стадию $match
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

            // Добавляем фильтрацию по открытым заведениям, если isOpen = true
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
               { "web", 1 }
           });

            var limitStage = new BsonDocument("$limit", limit);

            // Формируем пайплайн в зависимости от наличия matchStage и scheduleMatchStage
            var pipeline = new List<BsonDocument> { geoNearStage };
            if (matchStage != null) pipeline.Add(matchStage);
            if (scheduleMatchStage != null) pipeline.Add(scheduleMatchStage);
            pipeline.Add(projectStage);
            pipeline.Add(limitStage);

            var results = await _placeCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results;
        }


    }
}

