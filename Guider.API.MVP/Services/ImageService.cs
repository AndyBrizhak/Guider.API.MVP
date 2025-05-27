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

        public ImageService(IConfiguration configuration, IOptions<MongoDbSettings> mongoSettings)
        {
            _baseImagePath = configuration["StaticFiles:ImagesPath"] ?? "wwwroot/images";

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

        public async Task<JsonDocument> SaveImageAsync(string province, string? city, string place, string imageName, IFormFile imageFile)
        {
            if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(place) ||
                string.IsNullOrEmpty(imageName) || imageFile == null || imageFile.Length == 0)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    Success = false,
                    Message = "Недопустимые параметры для загрузки изображения"
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

            // Определяем путь для хранения изображения
            string directoryPath = BuildDirectoryPath(province, city, place);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
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

            // Используем проверенное расширение из загружаемого файла
            string extension = fileExtension;

            // Формируем полное имя файла
            string fullImageName = string.IsNullOrEmpty(Path.GetExtension(imageName))
                ? $"{imageName}{extension}"
                : imageName;

            string fullPath = Path.Combine(directoryPath, fullImageName);
            string relativePath = BuildRelativePath(province, city, place, fullImageName);

            try
            {
                // Сохраняем файл на диск
                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                // Создаем запись в MongoDB
                var imageRecord = new BsonDocument
                {
                    ["Province"] = province,
                    // В методе SaveImageAsync и других местах, где используется ["City"] = city ?? BsonNull.Value,
                    // замените на тернарный оператор с явным приведением типа:

                    ["City"] = string.IsNullOrEmpty(city) ? BsonNull.Value : (BsonValue)city,
                    //["City"] = city ?? BsonNull.Value,
                    ["Place"] = place,
                    ["ImageName"] = fullImageName,
                    ["OriginalFileName"] = imageFile.FileName,
                    ["FilePath"] = relativePath,
                    ["FileSize"] = imageFile.Length,
                    ["ContentType"] = imageFile.ContentType,
                    ["Extension"] = extension,
                    ["UploadDate"] = DateTime.UtcNow,
                    //["IsActive"] = true
                };

                await _imageCollection.InsertOneAsync(imageRecord);

                return JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Path = relativePath,
                    Id = imageRecord["_id"].ToString()
                }));
            }
            catch (Exception ex)
            {
                // Если произошла ошибка при сохранении в MongoDB, удаляем файл
                if (File.Exists(fullPath))
                {
                    try { File.Delete(fullPath); } catch { }
                }

                return JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    Success = false,
                    Message = $"Ошибка при сохранении изображения: {ex.Message}"
                }));
            }
        }

        public JsonDocument GetImage(string province, string? city, string place, string imageName)
        {
            if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(place) || string.IsNullOrEmpty(imageName))
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    Success = false,
                    Message = "Параметры запроса не могут быть пустыми"
                }));
            }

            try
            {
                // Ищем запись в MongoDB
                var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("Province", province),
                    Builders<BsonDocument>.Filter.Eq("Place", place),
                    Builders<BsonDocument>.Filter.Eq("ImageName", imageName)
                    //Builders<BsonDocument>.Filter.Eq("IsActive", true)
                );

                if (!string.IsNullOrEmpty(city))
                {
                    filter = Builders<BsonDocument>.Filter.And(filter,
                        Builders<BsonDocument>.Filter.Eq("City", city));
                }
                else
                {
                    filter = Builders<BsonDocument>.Filter.And(filter,
                        Builders<BsonDocument>.Filter.Or(
                            Builders<BsonDocument>.Filter.Eq("City", BsonNull.Value),
                            Builders<BsonDocument>.Filter.Not(Builders<BsonDocument>.Filter.Exists("City"))
                        ));
                }

                var imageRecord = _imageCollection.Find(filter).FirstOrDefault();

                if (imageRecord == null)
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        Success = false,
                        Message = "Изображение не найдено в базе данных"
                    }));
                }

                string filePath = imageRecord["FilePath"].AsString;
                string absolutePath = Path.Combine(_baseImagePath, filePath);

                if (!File.Exists(absolutePath))
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        Success = false,
                        Message = "Файл изображения не найден на диске"
                    }));
                }

                byte[] imageBytes = File.ReadAllBytes(absolutePath);
                return JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Image = imageBytes,
                    ImageInfo = new
                    {
                        Id = imageRecord["_id"].ToString(),
                        Province = imageRecord["Province"].AsString,
                        City = imageRecord.Contains("City") && !imageRecord["City"].IsBsonNull ? imageRecord["City"].AsString : null,
                        Place = imageRecord["Place"].AsString,
                        ImageName = imageRecord["ImageName"].AsString,
                        OriginalFileName = imageRecord["OriginalFileName"].AsString,
                        FileSize = imageRecord["FileSize"].AsInt64,
                        ContentType = imageRecord["ContentType"].AsString,
                        Extension = imageRecord["Extension"].AsString,
                        UploadDate = imageRecord["UploadDate"].ToUniversalTime()
                    }
                }));
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

                var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("_id", objectId)
                    //Builders<BsonDocument>.Filter.Eq("IsActive", true)
                );

                var imageRecord = await _imageCollection.Find(filter).FirstOrDefaultAsync();

                if (imageRecord == null)
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        Success = false,
                        Message = "Изображение не найдено"
                    }));
                }

                string filePath = imageRecord["FilePath"].AsString;
                string absolutePath = Path.Combine(_baseImagePath, filePath);

                if (!File.Exists(absolutePath))
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        Success = false,
                        Message = "Файл изображения не найден на диске"
                    }));
                }

                byte[] imageBytes = File.ReadAllBytes(absolutePath);
                return JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Image = imageBytes,
                    ImageInfo = new
                    {
                        Id = imageRecord["_id"].ToString(),
                        Province = imageRecord["Province"].AsString,
                        City = imageRecord.Contains("City") && !imageRecord["City"].IsBsonNull ? imageRecord["City"].AsString : null,
                        Place = imageRecord["Place"].AsString,
                        ImageName = imageRecord["ImageName"].AsString,
                        OriginalFileName = imageRecord["OriginalFileName"].AsString,
                        FileSize = imageRecord["FileSize"].AsInt64,
                        ContentType = imageRecord["ContentType"].AsString,
                        Extension = imageRecord["Extension"].AsString,
                        UploadDate = imageRecord["UploadDate"].ToUniversalTime()
                    }
                }));
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

        public JsonDocument GetImagesList(int page, int pageSize)
        {
            try
            {
                if (page < 1)
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        Success = false,
                        Message = "Номер страницы должен быть больше или равен 1"
                    }));
                }

                if (pageSize < 1)
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        Success = false,
                        Message = "Размер страницы должен быть больше или равен 1"
                    }));
                }

                // Получаем данные из MongoDB
                var filter = Builders<BsonDocument>.Filter.Empty;
                var totalImages = _imageCollection.CountDocuments(filter);
                var totalPages = (int)Math.Ceiling((double)totalImages / pageSize);

                var images = _imageCollection.Find(filter)
                    .Sort(Builders<BsonDocument>.Sort.Descending("UploadDate"))
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToList();

                var imageList = images.Select(doc => new
                {
                    Id = doc["_id"].ToString(),
                    Province = doc["Province"].AsString,
                    City = doc.Contains("City") && !doc["City"].IsBsonNull ? doc["City"].AsString : null,
                    Place = doc["Place"].AsString,
                    ImageName = doc["ImageName"].AsString,
                    OriginalFileName = doc["OriginalFileName"].AsString,
                    FilePath = doc["FilePath"].AsString,
                    FileSize = doc["FileSize"].AsInt64,
                    ContentType = doc["ContentType"].AsString,
                    Extension = doc["Extension"].AsString,
                    UploadDate = doc["UploadDate"].ToUniversalTime()
                }).ToList();

                var result = new
                {
                    Success = true,
                    TotalImages = (int)totalImages,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize,
                    Images = imageList
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(result));
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    Success = false,
                    Message = $"Ошибка при получении списка изображений: {ex.Message}"
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
                    return CreateJsonResponse(false, "Неверный формат ID", null);
                }

                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var imageRecord = await _imageCollection.Find(filter).FirstOrDefaultAsync();

                // Проверка существования записи
                if (imageRecord == null)
                {
                    return CreateJsonResponse(false, "Изображение не найдено", null);
                }

                // Сохраняем информацию об изображении перед удалением
                var imageInfo = new
                {
                    Id = imageRecord["_id"].ToString(),
                    Province = imageRecord.GetValue("Province", "").AsString,
                    City = imageRecord.Contains("City") && imageRecord["City"] != BsonNull.Value
                        ? imageRecord["City"].AsString : null,
                    Place = imageRecord.GetValue("Place", "").AsString,
                    ImageName = imageRecord.GetValue("ImageName", "").AsString,
                    OriginalFileName = imageRecord.GetValue("OriginalFileName", "").AsString,
                    FileSize = imageRecord.GetValue("FileSize", 0L).AsInt64,
                    ContentType = imageRecord.GetValue("ContentType", "").AsString,
                    Extension = imageRecord.GetValue("Extension", "").AsString,
                    UploadDate = imageRecord.GetValue("UploadDate", DateTime.UtcNow).ToUniversalTime(),
                    DeletedDate = DateTime.UtcNow
                };

                // Удаляем файл с диска, если он существует
                string filePath = imageRecord["FilePath"].AsString;
                string absolutePath = Path.Combine(_baseImagePath, filePath);

                if (File.Exists(absolutePath))
                {
                    File.Delete(absolutePath);
                }

                // Удаляем запись из MongoDB
                var deleteResult = await _imageCollection.DeleteOneAsync(filter);

                if (deleteResult.DeletedCount == 0)
                {
                    return CreateJsonResponse(false, "Не удалось удалить запись из базы данных", null);
                }

                return CreateJsonResponse(true, "Изображение и запись успешно удалены", imageInfo);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateJsonResponse(false, $"Доступ запрещен: {ex.Message}", null);
            }
            catch (IOException ex)
            {
                return CreateJsonResponse(false, $"Ошибка файловой системы: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return CreateJsonResponse(false, $"Ошибка при удалении изображения: {ex.Message}", null);
            }
        }

        private JsonDocument CreateJsonResponse(bool success, string message, object imageInfo = null)
        {
            var response = new Dictionary<string, object>
            {
                ["Success"] = success,
                ["Message"] = message
            };

            if (imageInfo != null)
            {
                response["ImageInfo"] = imageInfo;
            }

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return JsonDocument.Parse(json);
        }

        private string BuildDirectoryPath(string province, string? city, string place)
        {
            if (string.IsNullOrEmpty(city))
            {
                return Path.Combine(_baseImagePath, province, place);
            }
            else
            {
                return Path.Combine(_baseImagePath, province, city, place);
            }
        }

        private string BuildRelativePath(string province, string? city, string place, string imageName)
        {
            if (string.IsNullOrEmpty(city))
            {
                return Path.Combine(province, place, imageName).Replace("\\", "/");
            }
            else
            {
                return Path.Combine(province, city, place, imageName).Replace("\\", "/");
            }
        }
    }
}