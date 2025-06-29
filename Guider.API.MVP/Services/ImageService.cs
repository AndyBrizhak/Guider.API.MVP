using System.Text.Json;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Extensions.Options;
using Guider.API.MVP.Data;
using System.Text.RegularExpressions;

namespace Guider.API.MVP.Services
{
    public class ImageService : IImageService
    {
        private readonly string _baseImagePath;
        private readonly IMongoCollection<BsonDocument> _imageCollection;
        private readonly IMinioService _minioService;

        public ImageService(IConfiguration configuration, 
                            IOptions<MongoDbSettings> mongoSettings, 
                            IMinioService minioService)
        {
            _baseImagePath = configuration["StaticFiles:ImagesPath"] ?? "wwwroot/images";
            _minioService = minioService;

            if (!Directory.Exists(_baseImagePath))
            {
                Directory.CreateDirectory(_baseImagePath);
            }

            // Инициализация MongoDB коллекции
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);

            // Получаем имя коллекции из настроек или используем значение по умолчанию
            string collectionName = "images";
            if (mongoSettings.Value.Collections != null && mongoSettings.Value.Collections.ContainsKey("Images"))
            {
                collectionName = mongoSettings.Value.Collections["Images"];
            }

            _imageCollection = database.GetCollection<BsonDocument>(collectionName);
        }

        public async Task<JsonDocument> GetImageByIdAsync(string id)
        {
            try
            {
                if (!ObjectId.TryParse(id, out var objectId))
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        Success = false,
                        Message = "Неверный формат ID"
                    }));
                }

                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var imageRecord = await _imageCollection.Find(filter).FirstOrDefaultAsync();

                if (imageRecord == null)
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        Success = false,
                        Message = "Изображение не найдено"
                    }));
                }

                var imageObj = new
                {
                    id = imageRecord["_id"].ToString(),
                    Province = imageRecord.Contains("Province") && !imageRecord["Province"].IsBsonNull ? imageRecord["Province"].AsString : null,
                    City = imageRecord.Contains("City") && !imageRecord["City"].IsBsonNull ? imageRecord["City"].AsString : null,
                    Place = imageRecord.Contains("Place") && !imageRecord["Place"].IsBsonNull ? imageRecord["Place"].AsString : null,
                    ImageName = imageRecord["ImageName"].AsString,
                    OriginalFileName = imageRecord["OriginalFileName"].AsString,
                    FilePath = imageRecord["FilePath"].AsString,
                    FileSize = imageRecord["FileSize"].AsInt64,
                    ContentType = imageRecord["ContentType"].AsString,
                    Extension = imageRecord["Extension"].AsString,
                    Description = imageRecord.Contains("Description") && !imageRecord["Description"].IsBsonNull ? imageRecord["Description"].AsString : null,
                    Tags = imageRecord.Contains("Tags") && !imageRecord["Tags"].IsBsonNull ? imageRecord["Tags"].AsString : null,
                    UploadDate = imageRecord["UploadDate"].ToUniversalTime(),
                    UpdateDate = imageRecord.Contains("UpdateDate") ? imageRecord["UpdateDate"].ToUniversalTime() : imageRecord["UploadDate"].ToUniversalTime()
                };

                var result = new
                {
                    Success = true,
                    Image = imageObj
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(result));
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    Success = false,
                    Message = $"Ошибка при получении изображения: {ex.Message}"
                }));
            }
        }

        public async Task<JsonDocument> SaveImageAsync(string imageName, IFormFile imageFile,
             string? province = null, string? city = null, string? place = null,
             string? description = null, string? tags = null)
        {
            if (string.IsNullOrEmpty(imageName) || imageFile == null || imageFile.Length == 0)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    Success = false,
                    Message = "Название изображения и файл являются обязательными параметрами"
                }));
            }

            // Проверка допустимых расширений файлов изображений
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            string fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    Success = false,
                    Message = $"Недопустимый формат файла. Разрешены только следующие форматы: {string.Join(", ", allowedExtensions)}"
                }));
            }

            // Проверка на дубликаты имен файлов в MongoDB
            string imageNameWithoutExtension = Path.GetFileNameWithoutExtension(imageName);
            var duplicateFilter = Builders<BsonDocument>.Filter.Regex("ImageName",
                new BsonRegularExpression($"^{Regex.Escape(imageNameWithoutExtension)}", "i"));
            var existingDocument = await _imageCollection.Find(duplicateFilter).FirstOrDefaultAsync();

            if (existingDocument != null)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    Success = false,
                    Message = $"Файл с названием '{imageNameWithoutExtension}' уже существует"
                }));
            }

            // Формируем полное имя файла
            string fullImageName = string.IsNullOrEmpty(Path.GetExtension(imageName))
                ? $"{imageName}{fileExtension}"
                : imageName;

            try
            {
                // Формируем имя файла с путем для MinIO, если указаны location параметры
                string minioFileName = imageNameWithoutExtension;
                var pathParts = new List<string>();

                if (!string.IsNullOrEmpty(province))
                    pathParts.Add(province);
                if (!string.IsNullOrEmpty(city))
                    pathParts.Add(city);
                if (!string.IsNullOrEmpty(place))
                    pathParts.Add(place);

                if (pathParts.Count > 0)
                {
                    minioFileName = string.Join("/", pathParts) + "/" + imageNameWithoutExtension;
                }

                // Загружаем файл в MinIO
                var uploadResult = await _minioService.UploadFileAsync(imageFile, minioFileName, fileExtension);

                // Проверяем, что загрузка прошла успешно
                if (uploadResult.StartsWith("Ошибка"))
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        Success = false,
                        Message = uploadResult
                    }));
                }

                // Создаем запись в MongoDB
                var imageRecord = new BsonDocument
                {
                    ["Province"] = string.IsNullOrEmpty(province) ? BsonNull.Value : (BsonValue)province,
                    ["City"] = string.IsNullOrEmpty(city) ? BsonNull.Value : (BsonValue)city,
                    ["Place"] = string.IsNullOrEmpty(place) ? BsonNull.Value : (BsonValue)place,
                    ["ImageName"] = fullImageName,
                    ["OriginalFileName"] = imageFile.FileName,
                    ["FilePath"] = uploadResult, // Используем uploadResult вместо relativePath
                    ["FileSize"] = imageFile.Length,
                    ["ContentType"] = imageFile.ContentType,
                    ["Extension"] = fileExtension,
                    ["Description"] = string.IsNullOrEmpty(description) ? BsonNull.Value : (BsonValue)description,
                    ["Tags"] = string.IsNullOrEmpty(tags) ? BsonNull.Value : (BsonValue)tags,
                    ["UploadDate"] = DateTime.UtcNow,
                    ["UpdateDate"] = DateTime.UtcNow
                };

                await _imageCollection.InsertOneAsync(imageRecord);

                return JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Path = uploadResult, // Возвращаем uploadResult
                    Id = imageRecord["_id"].ToString()
                }));
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    Success = false,
                    Message = $"Ошибка при сохранении изображения: {ex.Message}"
                }));
            }
        }

        public async Task<JsonDocument> DeleteImageByIdAsync(string id)
        {
            try
            {
                // Проверка формата ID
                if (!ObjectId.TryParse(id, out var objectId))
                {
                    var errorResponse = new Dictionary<string, object>
                    {
                        ["Success"] = false,
                        ["Message"] = "Неверный формат ID"
                    };

                    var errorJson = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    return JsonDocument.Parse(errorJson);
                }

                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var imageRecord = await _imageCollection.Find(filter).FirstOrDefaultAsync();

                // Проверка существования записи
                if (imageRecord == null)
                {
                    var notFoundResponse = new Dictionary<string, object>
                    {
                        ["Success"] = false,
                        ["Message"] = "Изображение не найдено"
                    };

                    var notFoundJson = JsonSerializer.Serialize(notFoundResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    return JsonDocument.Parse(notFoundJson);
                }

                // Сохраняем информацию об изображении перед удалением
                var imageInfo = new
                {
                    Id = imageRecord["_id"].ToString(),
                    Province = imageRecord.Contains("Province") && !imageRecord["Province"].IsBsonNull ? imageRecord["Province"].AsString : null,
                    City = imageRecord.Contains("City") && imageRecord["City"] != BsonNull.Value ? imageRecord["City"].AsString : null,
                    Place = imageRecord.Contains("Place") && !imageRecord["Place"].IsBsonNull ? imageRecord["Place"].AsString : null,
                    ImageName = imageRecord.GetValue("ImageName", "").AsString,
                    OriginalFileName = imageRecord.GetValue("OriginalFileName", "").AsString,
                    FileSize = imageRecord.GetValue("FileSize", 0L).AsInt64,
                    ContentType = imageRecord.GetValue("ContentType", "").AsString,
                    Extension = imageRecord.GetValue("Extension", "").AsString,
                    Description = imageRecord.Contains("Description") && !imageRecord["Description"].IsBsonNull ? imageRecord["Description"].AsString : null,
                    Tags = imageRecord.Contains("Tags") && !imageRecord["Tags"].IsBsonNull ? imageRecord["Tags"].AsString : null,
                    UploadDate = imageRecord.GetValue("UploadDate", DateTime.UtcNow).ToUniversalTime(),
                    UpdateDate = imageRecord.Contains("UpdateDate") ? imageRecord["UpdateDate"].ToUniversalTime() : imageRecord["UploadDate"].ToUniversalTime(),
                    DeletedDate = DateTime.UtcNow
                };

                // Удаляем файл с диска, если он существует
                //string filePath = imageRecord["FilePath"].AsString;
                //string absolutePath = Path.Combine(_baseImagePath, filePath);

                //if (File.Exists(absolutePath))
                //{
                //    File.Delete(absolutePath);
                //}

                // Получаем URL файла из записи MongoDB
                string fileUrl = imageRecord["FilePath"].AsString;

                // Удаляем файл из MinIO
                var deleteResult = await _minioService.DeleteFileAsync(fileUrl);

                // Проверяем результат удаления из MinIO (логируем, но не останавливаем процесс)
                if (deleteResult.StartsWith("Ошибка"))
                {
                    // Файл может быть уже удален или не существовать, продолжаем удаление записи из БД
                    Console.WriteLine($"Предупреждение при удалении файла из MinIO: {deleteResult}");
                }

                // Удаляем запись из MongoDB
                //var deleteResult = await _imageCollection.DeleteOneAsync(filter);
                var mongoDeleteResult = await _imageCollection.DeleteOneAsync(filter);

                //if (deleteResult.DeletedCount == 0)
                if (mongoDeleteResult.DeletedCount == 0)
                {
                    var deleteFailResponse = new Dictionary<string, object>
                    {
                        ["Success"] = false,
                        ["Message"] = "Не удалось удалить запись из базы данных"
                    };

                    var deleteFailJson = JsonSerializer.Serialize(deleteFailResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    return JsonDocument.Parse(deleteFailJson);
                }

                // Успешное удаление
                var successResponse = new Dictionary<string, object>
                {
                    ["Success"] = true,
                    ["Message"] = "Изображение и запись успешно удалены",
                    ["ImageInfo"] = imageInfo
                };

                var successJson = JsonSerializer.Serialize(successResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return JsonDocument.Parse(successJson);
            }
            catch (UnauthorizedAccessException ex)
            {
                var unauthorizedResponse = new Dictionary<string, object>
                {
                    ["Success"] = false,
                    ["Message"] = $"Доступ запрещен: {ex.Message}"
                };

                var unauthorizedJson = JsonSerializer.Serialize(unauthorizedResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return JsonDocument.Parse(unauthorizedJson);
            }
            catch (IOException ex)
            {
                var ioResponse = new Dictionary<string, object>
                {
                    ["Success"] = false,
                    ["Message"] = $"Ошибка файловой системы: {ex.Message}"
                };

                var ioJson = JsonSerializer.Serialize(ioResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return JsonDocument.Parse(ioJson);
            }
            catch (Exception ex)
            {
                var generalErrorResponse = new Dictionary<string, object>
                {
                    ["Success"] = false,
                    ["Message"] = $"Ошибка при удалении изображения: {ex.Message}"
                };

                var generalErrorJson = JsonSerializer.Serialize(generalErrorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return JsonDocument.Parse(generalErrorJson);
            }
        }

       public async Task<JsonDocument> GetImagesAsync(Dictionary<string, string> filter = null)
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
                            filterBuilder.Regex("ImageName", new BsonRegularExpression(q, "i")),
                            filterBuilder.Regex("OriginalFileName", new BsonRegularExpression(q, "i")),
                            filterBuilder.Regex("Province", new BsonRegularExpression(q, "i")),
                            filterBuilder.Regex("Place", new BsonRegularExpression(q, "i")),
                            filterBuilder.Regex("Description", new BsonRegularExpression(q, "i")),
                            filterBuilder.Regex("Tags", new BsonRegularExpression(q, "i"))
                        ));
                    }

                    // Фильтр по названию изображения
                    if (filter.TryGetValue("imageName", out string imageName) && !string.IsNullOrEmpty(imageName))
                    {
                        filters.Add(filterBuilder.Regex("ImageName", new BsonRegularExpression(imageName, "i")));
                    }

                    // Фильтр по провинции
                    if (filter.TryGetValue("province", out string province) && !string.IsNullOrEmpty(province))
                    {
                        filters.Add(filterBuilder.Regex("Province", new BsonRegularExpression(province, "i")));
                    }

                    // Фильтр по месту
                    if (filter.TryGetValue("place", out string place) && !string.IsNullOrEmpty(place))
                    {
                        filters.Add(filterBuilder.Regex("Place", new BsonRegularExpression(place, "i")));
                    }

                    // Фильтр по описанию
                    if (filter.TryGetValue("description", out string description) && !string.IsNullOrEmpty(description))
                    {
                        filters.Add(filterBuilder.Regex("Description", new BsonRegularExpression(description, "i")));
                    }

                    // Фильтр по тегам
                    if (filter.TryGetValue("tags", out string tags) && !string.IsNullOrEmpty(tags))
                    {
                        filters.Add(filterBuilder.Regex("Tags", new BsonRegularExpression(tags, "i")));
                    }

                    // Фильтр по оригинальному имени файла
                    if (filter.TryGetValue("originalFileName", out string originalFileName) && !string.IsNullOrEmpty(originalFileName))
                    {
                        filters.Add(filterBuilder.Regex("OriginalFileName", new BsonRegularExpression(originalFileName, "i")));
                    }

                    if (filters.Count > 0)
                    {
                        filterDefinition = filterBuilder.And(filters);
                    }
                }

                long totalCount = await _imageCollection.CountDocumentsAsync(filterDefinition);

                // Сортировка
                string sortField = "UploadDate";
                bool isDescending = true;
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
                var sortDefinition = isDescending
                    ? Builders<BsonDocument>.Sort.Descending(sortField)
                    : Builders<BsonDocument>.Sort.Ascending(sortField);

                // Пагинация
                IFindFluent<BsonDocument, BsonDocument> query = _imageCollection.Find(filterDefinition).Sort(sortDefinition);
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

                // Формирование массива изображений с корректным форматом id
                var imagesList = new List<object>();
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

                    imagesList.Add(dict);
                    jsonDoc.Dispose();
                }

                // Формирование результирующего JSON документа
                var result = new
                {
                    success = true,
                    data = new
                    {
                        totalCount = totalCount,
                        images = imagesList
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


    }

  
}