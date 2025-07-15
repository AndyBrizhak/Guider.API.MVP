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
                        ["Message"] = "Изображение не найдено в базе данных"
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

                // Получаем URL файла из записи MongoDB
                string fileUrl = imageRecord["FilePath"].AsString;

                // Удаляем файл из MinIO
                var minioDeleteResult = await _minioService.DeleteFileAsync(fileUrl);

                // Удаляем запись из MongoDB
                var mongoDeleteResult = await _imageCollection.DeleteOneAsync(filter);

                // Формируем детальный результат операции
                string finalMessage;
                bool operationSuccess = false;

                if (mongoDeleteResult.DeletedCount > 0)
                {
                    if (minioDeleteResult.IsDeleted)
                    {
                        // Успешно удалено и из БД, и из хранилища
                        finalMessage = "Изображение успешно удалено из базы данных и хранилища";
                        operationSuccess = true;
                    }
                    else
                    {
                        // Удалено из БД, но проблемы с хранилищем
                        finalMessage = $"Изображение удалено из базы данных, но возникла проблема с удалением из хранилища: {minioDeleteResult.Message}";
                        operationSuccess = true; // Считаем успешным, так как основная запись удалена
                    }
                }
                else
                {
                    if (minioDeleteResult.IsDeleted)
                    {
                        // Удалено из хранилища, но проблемы с БД
                        finalMessage = "Файл удален из хранилища, но не удалось удалить запись из базы данных";
                        operationSuccess = false;
                    }
                    else
                    {
                        // Проблемы и с БД, и с хранилищем
                        finalMessage = $"Не удалось удалить запись из базы данных. Проблема с хранилищем: {minioDeleteResult.Message}";
                        operationSuccess = false;
                    }
                }

                var response = new Dictionary<string, object>
                {
                    ["Success"] = operationSuccess,
                    ["Message"] = finalMessage,
                    ["Details"] = new Dictionary<string, object>
                    {
                        ["DatabaseDeletion"] = new Dictionary<string, object>
                        {
                            ["Success"] = mongoDeleteResult.DeletedCount > 0,
                            ["Message"] = mongoDeleteResult.DeletedCount > 0
                                ? "Запись успешно удалена из базы данных"
                                : "Не удалось удалить запись из базы данных"
                        },
                        ["StorageDeletion"] = new Dictionary<string, object>
                        {
                            ["Success"] = minioDeleteResult.IsDeleted,
                            ["Message"] = minioDeleteResult.Message
                        }
                    }
                };

                if (operationSuccess)
                {
                    response["ImageInfo"] = imageInfo;
                }

                var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return JsonDocument.Parse(responseJson);
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

        
        public async Task<JsonDocument> UpdateImageAsync(string id, string? newImageName = null,
                    IFormFile? newImageFile = null, string? province = null,
                    string? city = null, string? place = null, string? description = null,
                    string? tags = null)
        {
            try
            {
                // Проверка формата ID
                if (!ObjectId.TryParse(id, out var objectId))
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        Success = false,
                        Message = "Неверный формат ID"
                    }));
                }

                // Поиск существующей записи в MongoDB
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var existingRecord = await _imageCollection.Find(filter).FirstOrDefaultAsync();

                if (existingRecord == null)
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        Success = false,
                        Message = "Изображение не найдено в базе данных"
                    }));
                }

                // Получаем текущий URL файла
                string currentFileUrl = existingRecord["FilePath"].AsString;
                bool needToUpdateFile = newImageFile != null && newImageFile.Length > 0;

                // Если есть новый файл, проверяем его расширение
                string? newFileExtension = null;
                if (needToUpdateFile)
                {
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                    newFileExtension = Path.GetExtension(newImageFile!.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(newFileExtension))
                    {
                        return JsonDocument.Parse(JsonSerializer.Serialize(new
                        {
                            Success = false,
                            Message = $"Недопустимый формат файла. Разрешены только следующие форматы: {string.Join(", ", allowedExtensions)}"
                        }));
                    }
                }

                // Если изменяется имя файла, проверяем на дубликаты
                if (!string.IsNullOrEmpty(newImageName))
                {
                    string newImageNameWithoutExtension = Path.GetFileNameWithoutExtension(newImageName);
                    string currentImageName = existingRecord["ImageName"].AsString;
                    string currentImageNameWithoutExtension = Path.GetFileNameWithoutExtension(currentImageName);

                    // Проверяем только если имя действительно изменилось
                    if (!string.Equals(newImageNameWithoutExtension, currentImageNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        var duplicateFilter = Builders<BsonDocument>.Filter.And(
                            Builders<BsonDocument>.Filter.Regex("ImageName", new BsonRegularExpression($"^{Regex.Escape(newImageNameWithoutExtension)}", "i")),
                            Builders<BsonDocument>.Filter.Ne("_id", objectId)
                        );
                        var duplicateDocument = await _imageCollection.Find(duplicateFilter).FirstOrDefaultAsync();

                        if (duplicateDocument != null)
                        {
                            return JsonDocument.Parse(JsonSerializer.Serialize(new
                            {
                                Success = false,
                                Message = $"Файл с названием '{newImageNameWithoutExtension}' уже существует"
                            }));
                        }
                    }
                }

                string? newFileUrl = null;
                bool fileUpdateSuccess = true;
                string fileUpdateMessage = "";

                // Если нужно обновить файл
                if (needToUpdateFile)
                {
                    // Формируем новое имя файла
                    string finalImageName = !string.IsNullOrEmpty(newImageName) ? newImageName : existingRecord["ImageName"].AsString;
                    string imageNameWithoutExtension = Path.GetFileNameWithoutExtension(finalImageName);

                    // Формируем путь в MinIO
                    string minioFileName = imageNameWithoutExtension;
                    var pathParts = new List<string>();

                    string finalProvince = province ?? (existingRecord.Contains("Province") && !existingRecord["Province"].IsBsonNull ? existingRecord["Province"].AsString : null);
                    string finalCity = city ?? (existingRecord.Contains("City") && !existingRecord["City"].IsBsonNull ? existingRecord["City"].AsString : null);
                    string finalPlace = place ?? (existingRecord.Contains("Place") && !existingRecord["Place"].IsBsonNull ? existingRecord["Place"].AsString : null);

                    if (!string.IsNullOrEmpty(finalProvince))
                        pathParts.Add(finalProvince);
                    if (!string.IsNullOrEmpty(finalCity))
                        pathParts.Add(finalCity);
                    if (!string.IsNullOrEmpty(finalPlace))
                        pathParts.Add(finalPlace);

                    if (pathParts.Count > 0)
                    {
                        minioFileName = string.Join("/", pathParts) + "/" + imageNameWithoutExtension;
                    }

                    // Формируем полное имя файла с расширением для проверки в хранилище
                    string fullMinioFileName = $"{minioFileName}.{newFileExtension!.TrimStart('.')}";

                    // 1. Проверяем существование файла с новым именем в хранилище
                    bool newFileExists = await _minioService.FileExistsAsync(fullMinioFileName);

                    if (newFileExists)
                    {
                        // 2. Если файл существует, удаляем его
                        var deleteExistingResult = await _minioService.DeleteFileAsync(_minioService.GetFileUrl(fullMinioFileName));

                        if (!deleteExistingResult.IsDeleted)
                        {
                            fileUpdateSuccess = false;
                            fileUpdateMessage = $"Не удалось удалить существующий файл с новым именем: {deleteExistingResult.Message}";
                        }
                        else
                        {
                            // 3. Проверяем, что файл действительно удален
                            bool fileStillExists = await _minioService.FileExistsAsync(fullMinioFileName);

                            if (fileStillExists)
                            {
                                fileUpdateSuccess = false;
                                fileUpdateMessage = "Файл с новым именем не был полностью удален из хранилища";
                            }
                        }
                    }

                    // 4. Если все проверки прошли успешно, загружаем новый файл
                    if (fileUpdateSuccess)
                    {
                        var uploadResult = await _minioService.UploadFileAsync(newImageFile!, minioFileName, newFileExtension!);

                        if (uploadResult.StartsWith("Ошибка"))
                        {
                            fileUpdateSuccess = false;
                            fileUpdateMessage = uploadResult;
                        }
                        else
                        {
                            newFileUrl = uploadResult;

                            // Удаляем старый файл только после успешной загрузки нового
                            //var deleteResult = await _minioService.DeleteFileAsync(currentFileUrl);
                            //if (!deleteResult.IsDeleted)
                            //{
                            //    fileUpdateMessage = $"Новый файл загружен, но не удалось удалить старый файл: {deleteResult.Message}";
                            //}
                        }
                    }
                }

                // Если обновление файла провалилось, возвращаем ошибку
                if (needToUpdateFile && !fileUpdateSuccess)
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        Success = false,
                        Message = fileUpdateMessage
                    }));
                }

                // Формируем обновленную запись
                var updateBuilder = Builders<BsonDocument>.Update;
                var updates = new List<UpdateDefinition<BsonDocument>>();

                // Обновляем поля только если они переданы
                if (!string.IsNullOrEmpty(newImageName))
                {
                    string fullImageName = string.IsNullOrEmpty(Path.GetExtension(newImageName))
                        ? $"{newImageName}{(needToUpdateFile ? newFileExtension! : Path.GetExtension(existingRecord["ImageName"].AsString))}"
                        : newImageName;
                    updates.Add(updateBuilder.Set("ImageName", fullImageName));
                }

                if (province != null)
                    updates.Add(updateBuilder.Set("Province", string.IsNullOrEmpty(province) ? BsonNull.Value : (BsonValue)province));

                if (city != null)
                    updates.Add(updateBuilder.Set("City", string.IsNullOrEmpty(city) ? BsonNull.Value : (BsonValue)city));

                if (place != null)
                    updates.Add(updateBuilder.Set("Place", string.IsNullOrEmpty(place) ? BsonNull.Value : (BsonValue)place));

                if (description != null)
                    updates.Add(updateBuilder.Set("Description", string.IsNullOrEmpty(description) ? BsonNull.Value : (BsonValue)description));

                if (tags != null)
                    updates.Add(updateBuilder.Set("Tags", string.IsNullOrEmpty(tags) ? BsonNull.Value : (BsonValue)tags));

                // Если был загружен новый файл, обновляем связанные поля
                if (needToUpdateFile && newFileUrl != null)
                {
                    updates.Add(updateBuilder.Set("FilePath", newFileUrl));
                    updates.Add(updateBuilder.Set("OriginalFileName", newImageFile!.FileName));
                    updates.Add(updateBuilder.Set("FileSize", newImageFile.Length));
                    updates.Add(updateBuilder.Set("ContentType", newImageFile.ContentType));
                    updates.Add(updateBuilder.Set("Extension", newFileExtension!));
                }

                // Всегда обновляем дату изменения
                updates.Add(updateBuilder.Set("UpdateDate", DateTime.UtcNow));

                // Выполняем обновление в MongoDB
                if (updates.Count > 0)
                {
                    var combinedUpdate = updateBuilder.Combine(updates);
                    var updateResult = await _imageCollection.UpdateOneAsync(filter, combinedUpdate);

                    if (updateResult.ModifiedCount == 0)
                    {
                        return JsonDocument.Parse(JsonSerializer.Serialize(new
                        {
                            Success = false,
                            Message = "Не удалось обновить запись в базе данных"
                        }));
                    }
                }

                // Получаем обновленную запись
                var updatedRecord = await _imageCollection.Find(filter).FirstOrDefaultAsync();

                var updatedImageObj = new
                {
                    id = updatedRecord["_id"].ToString(),
                    Province = updatedRecord.Contains("Province") && !updatedRecord["Province"].IsBsonNull ? updatedRecord["Province"].AsString : null,
                    City = updatedRecord.Contains("City") && !updatedRecord["City"].IsBsonNull ? updatedRecord["City"].AsString : null,
                    Place = updatedRecord.Contains("Place") && !updatedRecord["Place"].IsBsonNull ? updatedRecord["Place"].AsString : null,
                    ImageName = updatedRecord["ImageName"].AsString,
                    OriginalFileName = updatedRecord["OriginalFileName"].AsString,
                    FilePath = updatedRecord["FilePath"].AsString,
                    FileSize = updatedRecord["FileSize"].AsInt64,
                    ContentType = updatedRecord["ContentType"].AsString,
                    Extension = updatedRecord["Extension"].AsString,
                    Description = updatedRecord.Contains("Description") && !updatedRecord["Description"].IsBsonNull ? updatedRecord["Description"].AsString : null,
                    Tags = updatedRecord.Contains("Tags") && !updatedRecord["Tags"].IsBsonNull ? updatedRecord["Tags"].AsString : null,
                    UploadDate = updatedRecord["UploadDate"].ToUniversalTime(),
                    UpdateDate = updatedRecord["UpdateDate"].ToUniversalTime()
                };

                var result = new
                {
                    Success = true,
                    Message = needToUpdateFile ?
                        (string.IsNullOrEmpty(fileUpdateMessage) ? "Изображение успешно обновлено" : $"Изображение обновлено. {fileUpdateMessage}") :
                        "Данные изображения успешно обновлены",
                    Image = updatedImageObj
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(result));
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    Success = false,
                    Message = $"Ошибка при обновлении изображения: {ex.Message}"
                }));
            }
        }


    }

  
}