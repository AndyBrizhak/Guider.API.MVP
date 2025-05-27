//using System.Text.Json;

//namespace Guider.API.MVP.Services
//{
//    public class ImageService : IImageService
//    {
//        private readonly string _baseImagePath;

//        public ImageService(IConfiguration configuration)
//        {
//            _baseImagePath = configuration["StaticFiles:ImagesPath"] ?? "wwwroot/images";

//            if (!Directory.Exists(_baseImagePath))
//            {
//                Directory.CreateDirectory(_baseImagePath);
//            }
//        }

//        public async Task<JsonDocument> SaveImageAsync(string province, string? city, string place, string imageName, IFormFile imageFile)
//        {
//            if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(place) ||
//                string.IsNullOrEmpty(imageName) || imageFile == null || imageFile.Length == 0)
//            {
//                return JsonDocument.Parse(JsonSerializer.Serialize(new
//                {
//                    Success = false,
//                    Message = "Недопустимые параметры для загрузки изображения"
//                }));
//            }

//            // Проверка допустимых расширений файлов изображений
//            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
//            string fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

//            if (!allowedExtensions.Contains(fileExtension))
//            {
//                return JsonDocument.Parse(JsonSerializer.Serialize(new
//                {
//                    Success = false,
//                    Message = $"Недопустимый формат файла. Разрешены только следующие форматы: {string.Join(", ", allowedExtensions)}"
//                }));
//            }

//            // Определяем путь для хранения изображения
//            string directoryPath = BuildDirectoryPath(province, city, place);

//            if (!Directory.Exists(directoryPath))
//            {
//                Directory.CreateDirectory(directoryPath);
//            }

//            // Проверка на дубликаты имен файлов
//            string imageNameWithoutExtension = Path.GetFileNameWithoutExtension(imageName);
//            string[] allFiles = Directory.GetFiles(_baseImagePath, "*.*", SearchOption.AllDirectories);

//            string duplicateFilePath = allFiles.FirstOrDefault(file =>
//                Path.GetFileNameWithoutExtension(file).Equals(imageNameWithoutExtension, StringComparison.OrdinalIgnoreCase));

//            if (duplicateFilePath != null)
//            {
//                string relativeDuplicatePath = duplicateFilePath.Replace(_baseImagePath, "")
//                    .TrimStart('\\', '/')
//                    .Replace("\\", "/");

//                return JsonDocument.Parse(JsonSerializer.Serialize(new
//                {
//                    Success = false,
//                    Message = $"Файл с таким названием уже существует в папке: {relativeDuplicatePath}"
//                }));
//            }

//            // Используем проверенное расширение из загружаемого файла
//            string extension = fileExtension;

//            // Формируем полное имя файла
//            string fullImageName = string.IsNullOrEmpty(Path.GetExtension(imageName))
//                ? $"{imageName}{extension}"
//                : imageName;

//            string fullPath = Path.Combine(directoryPath, fullImageName);

//            try
//            {
//                using (var fileStream = new FileStream(fullPath, FileMode.Create))
//                {
//                    await imageFile.CopyToAsync(fileStream);
//                }

//                string relativePath = BuildRelativePath(province, city, place, fullImageName);
//                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = true, Path = relativePath }));
//            }
//            catch (Exception ex)
//            {
//                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = $"Ошибка при сохранении изображения: {ex.Message}" }));
//            }
//        }

//        public JsonDocument GetImage(string province, string? city, string place, string imageName)
//        {
//            if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(place) || string.IsNullOrEmpty(imageName))
//            {
//                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = "Параметры запроса не могут быть пустыми" }));
//            }

//            string relativePath = BuildRelativePath(province, city, place, imageName);
//            string absolutePath = Path.Combine(_baseImagePath, relativePath);

//            if (!File.Exists(absolutePath))
//            {
//                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = $"Изображение не найдено" }));
//            }

//            try
//            {
//                byte[] imageBytes = File.ReadAllBytes(absolutePath);
//                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = true, Image = imageBytes }));
//            }
//            catch (Exception ex)
//            {
//                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = $"Ошибка при получении изображения: {ex.Message}" }));
//            }
//        }

//        public JsonDocument GetImagesList(int page, int pageSize)
//        {
//            try
//            {
//                if (page < 1)
//                {
//                    return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = "Номер страницы должен быть больше или равен 1" }));
//                }

//                if (pageSize < 1)
//                {
//                    return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = "Размер страницы должен быть больше или равен 1" }));
//                }

//                if (!Directory.Exists(_baseImagePath))
//                {
//                    return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = "Директория с изображениями не найдена" }));
//                }

//                string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

//                var allImages = Directory.GetFiles(_baseImagePath, "*.*", SearchOption.AllDirectories)
//                    .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()))
//                    .Select(file => file.Replace(_baseImagePath, "").TrimStart('\\', '/'))
//                    .ToList();

//                int totalImages = allImages.Count;
//                int totalPages = (int)Math.Ceiling((double)totalImages / pageSize);

//                // Вычисляем индексы для текущей страницы
//                int startIndex = (page - 1) * pageSize;

//                // Получаем только элементы для текущей страницы
//                var pagedImages = allImages
//                    .Skip(startIndex)
//                    .Take(pageSize)
//                    .ToList();

//                var result = new
//                {
//                    Success = true,
//                    TotalImages = totalImages,
//                    TotalPages = totalPages,
//                    CurrentPage = page,
//                    PageSize = pageSize,
//                    Images = pagedImages
//                };

//                return JsonDocument.Parse(JsonSerializer.Serialize(result));
//            }
//            catch (Exception ex)
//            {
//                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = $"Ошибка при получении списка изображений: {ex.Message}" }));
//            }
//        }

//        public JsonDocument DeleteImage(string province, string? city, string place, string imageName)
//        {
//            if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(place) || string.IsNullOrEmpty(imageName))
//            {
//                return CreateJsonResponse(false, "Параметры запроса не могут быть пустыми");
//            }

//            string relativePath = BuildRelativePath(province, city, place, imageName);
//            string absolutePath = Path.Combine(_baseImagePath, relativePath);

//            if (!File.Exists(absolutePath))
//            {
//                return CreateJsonResponse(false, $"Файл не существует");
//            }

//            try
//            {
//                File.Delete(absolutePath);
//                return CreateJsonResponse(true, $"Файл успешно удален");
//            }
//            catch (Exception ex)
//            {
//                return CreateJsonResponse(false, $"Ошибка при удалении изображения: {ex.Message}");
//            }
//        }

//        public async Task<JsonDocument> UpdateImageAsync(
//            string oldProvince, string? oldCity, string oldPlace, string oldImageName,
//            string? newProvince = null, string? newCity = null, string? newPlace = null,
//            string? newImageName = null, IFormFile? newImageFile = null)
//        {
//            try
//            {
//                // Проверка параметров
//                if (string.IsNullOrEmpty(oldProvince) || string.IsNullOrEmpty(oldPlace) || string.IsNullOrEmpty(oldImageName))
//                {
//                    return CreateJsonResponse(false, "Путь к исходному изображению не может быть пустым");
//                }

//                // Если ничего не меняется, нечего обновлять
//                if (newImageFile == null &&
//                    string.IsNullOrEmpty(newProvince) &&
//                    string.IsNullOrEmpty(newCity) &&
//                    string.IsNullOrEmpty(newPlace) &&
//                    string.IsNullOrEmpty(newImageName))
//                {
//                    return CreateJsonResponse(false, "Для обновления необходимо указать хотя бы один новый параметр");
//                }

//                // Проверка существования старого файла
//                string oldRelativePath = BuildRelativePath(oldProvince, oldCity, oldPlace, oldImageName);
//                string oldAbsolutePath = Path.Combine(_baseImagePath, oldRelativePath);

//                if (!File.Exists(oldAbsolutePath))
//                {
//                    return CreateJsonResponse(false, $"Исходное изображение не найдено");
//                }

//                // Использовать новые значения, если они указаны, иначе старые
//                string provinceToUse = newProvince ?? oldProvince;
//                string? cityToUse = newCity ?? oldCity;
//                string placeToUse = newPlace ?? oldPlace;
//                string imageNameToUse = newImageName ?? oldImageName;

//                // Определение директории для сохранения
//                string newDirectoryPath = BuildDirectoryPath(provinceToUse, cityToUse, placeToUse);

//                // Создание директории, если она не существует
//                if (!Directory.Exists(newDirectoryPath))
//                {
//                    Directory.CreateDirectory(newDirectoryPath);
//                }

//                // Проверка на дубликаты, только если меняется путь
//                if ((newProvince != null || newCity != null || newPlace != null || newImageName != null) &&
//                    (newProvince != oldProvince || newCity != oldCity || newPlace != oldPlace || newImageName != oldImageName))
//                {
//                    string imageNameWithoutExtension = Path.GetFileNameWithoutExtension(imageNameToUse);
//                    string[] allFiles = Directory.GetFiles(_baseImagePath, "*.*", SearchOption.AllDirectories);

//                    string duplicateFilePath = allFiles.FirstOrDefault(file =>
//                        Path.GetFileNameWithoutExtension(file).Equals(imageNameWithoutExtension, StringComparison.OrdinalIgnoreCase) &&
//                        file != oldAbsolutePath);  // Исключаем текущий файл из проверки

//                    if (duplicateFilePath != null)
//                    {
//                        string relativeDuplicatePath = duplicateFilePath.Replace(_baseImagePath, "")
//                            .TrimStart('\\', '/')
//                            .Replace("\\", "/");

//                        return CreateJsonResponse(false, $"Файл с таким названием уже существует в папке: {relativeDuplicatePath}");
//                    }
//                }

//                // Определение расширения файла
//                string extension;

//                if (newImageFile != null)
//                {
//                    // Проверка допустимых расширений
//                    string[] validExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
//                    extension = Path.GetExtension(newImageFile.FileName).ToLower();

//                    if (string.IsNullOrEmpty(extension))
//                    {
//                        extension = ".jpg"; // По умолчанию
//                    }
//                    else if (!validExtensions.Contains(extension))
//                    {
//                        return CreateJsonResponse(false, $"Недопустимое расширение файла: {extension}. Допустимые расширения: {string.Join(", ", validExtensions)}");
//                    }
//                }
//                else
//                {
//                    // Если нет нового файла, используем расширение старого
//                    extension = Path.GetExtension(oldImageName);
//                }

//                // Формирование имени файла с расширением
//                string fullImageName = string.IsNullOrEmpty(Path.GetExtension(imageNameToUse))
//                    ? $"{imageNameToUse}{extension}"
//                    : imageNameToUse;

//                string newAbsolutePath = Path.Combine(newDirectoryPath, fullImageName);

//                // Формирование относительного пути
//                string newRelativePath = BuildRelativePath(provinceToUse, cityToUse, placeToUse, fullImageName);

//                // Проверка, не указывает ли новый путь на старый файл
//                if (newAbsolutePath == oldAbsolutePath && newImageFile == null)
//                {
//                    return CreateJsonResponse(false, "Новый путь совпадает со старым, а новый файл не загружен. Нечего обновлять.");
//                }

//                // Обновление файла 
//                if (newImageFile != null)
//                {
//                    // Если это тот же путь, удаляем старый файл
//                    if (File.Exists(newAbsolutePath) && newAbsolutePath != oldAbsolutePath)
//                    {
//                        File.Delete(newAbsolutePath);
//                    }

//                    // Сохраняем новый файл
//                    using (var fileStream = new FileStream(newAbsolutePath, FileMode.Create))
//                    {
//                        await newImageFile.CopyToAsync(fileStream);
//                    }

//                    // Если путь изменился, удаляем старый файл
//                    if (newAbsolutePath != oldAbsolutePath)
//                    {
//                        File.Delete(oldAbsolutePath);
//                    }
//                }
//                else
//                {
//                    // Если путь изменился, но файл тот же - перемещаем файл
//                    if (newAbsolutePath != oldAbsolutePath)
//                    {
//                        // Если файл уже существует по новому пути, удаляем его
//                        if (File.Exists(newAbsolutePath))
//                        {
//                            File.Delete(newAbsolutePath);
//                        }

//                        // Копируем старый файл в новое место
//                        File.Copy(oldAbsolutePath, newAbsolutePath);

//                        // Удаляем старый файл
//                        File.Delete(oldAbsolutePath);
//                    }
//                }

//                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = true, Path = newRelativePath }));
//            }
//            catch (Exception ex)
//            {
//                return CreateJsonResponse(false, $"Ошибка при обновлении изображения: {ex.Message}");
//            }
//        }

//        private JsonDocument CreateJsonResponse(bool success, string message)
//        {
//            var options = new JsonWriterOptions { Indented = true };
//            using var stream = new MemoryStream();
//            using (var writer = new Utf8JsonWriter(stream, options))
//            {
//                writer.WriteStartObject();
//                writer.WriteBoolean("Success", success);
//                writer.WriteString("Message", message);
//                writer.WriteEndObject();
//            }

//            stream.Position = 0;
//            return JsonDocument.Parse(stream);
//        }

//        // Helper methods for building paths
//        private string BuildDirectoryPath(string province, string? city, string place)
//        {
//            if (string.IsNullOrEmpty(city))
//            {
//                return Path.Combine(_baseImagePath, province, place);
//            }
//            else
//            {
//                return Path.Combine(_baseImagePath, province, city, place);
//            }
//        }

//        private string BuildRelativePath(string province, string? city, string place, string imageName)
//        {
//            if (string.IsNullOrEmpty(city))
//            {
//                return Path.Combine(province, place, imageName).Replace("\\", "/");
//            }
//            else
//            {
//                return Path.Combine(province, city, place, imageName).Replace("\\", "/");
//            }
//        }
//    }
//}

//using System.Text.Json;
//using MongoDB.Driver;
//using MongoDB.Bson;
//using Microsoft.Extensions.Options;
//using Guider.API.MVP.Data;

//namespace Guider.API.MVP.Services
//{
//    public class ImageService : IImageService
//    {
//        private readonly string _baseImagePath;
//        private readonly IMongoCollection<BsonDocument> _imageCollection;

//        public ImageService(IConfiguration configuration, IOptions<MongoDbSettings> mongoSettings)
//        {
//            _baseImagePath = configuration["StaticFiles:ImagesPath"] ?? "wwwroot/images";

//            if (!Directory.Exists(_baseImagePath))
//            {
//                Directory.CreateDirectory(_baseImagePath);
//            }

//            // Инициализация MongoDB коллекции
//            var client = new MongoClient(mongoSettings.Value.ConnectionString);
//            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);

//            // Получаем имя коллекции из настроек или используем значение по умолчанию
//            string collectionName = "images";
//            if (mongoSettings.Value.Collections != null && mongoSettings.Value.Collections.ContainsKey("Images"))
//            {
//                collectionName = mongoSettings.Value.Collections["Images"];
//            }

//            _imageCollection = database.GetCollection<BsonDocument>(collectionName);
//        }

//        public async Task<JsonDocument> SaveImageAsync(string province, string? city, string place, string imageName, IFormFile imageFile)
//        {
//            if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(place) ||
//                string.IsNullOrEmpty(imageName) || imageFile == null || imageFile.Length == 0)
//            {
//                return JsonDocument.Parse(JsonSerializer.Serialize(new
//                {
//                    Success = false,
//                    Message = "Недопустимые параметры для загрузки изображения"
//                }));
//            }

//            // Проверка допустимых расширений файлов изображений
//            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
//            string fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

//            if (!allowedExtensions.Contains(fileExtension))
//            {
//                return JsonDocument.Parse(JsonSerializer.Serialize(new
//                {
//                    Success = false,
//                    Message = $"Недопустимый формат файла. Разрешены только следующие форматы: {string.Join(", ", allowedExtensions)}"
//                }));
//            }

//            // Определяем путь для хранения изображения
//            string directoryPath = BuildDirectoryPath(province, city, place);

//            if (!Directory.Exists(directoryPath))
//            {
//                Directory.CreateDirectory(directoryPath);
//            }

//            // Проверка на дубликаты имен файлов
//            string imageNameWithoutExtension = Path.GetFileNameWithoutExtension(imageName);
//            string[] allFiles = Directory.GetFiles(_baseImagePath, "*.*", SearchOption.AllDirectories);

//            string duplicateFilePath = allFiles.FirstOrDefault(file =>
//                Path.GetFileNameWithoutExtension(file).Equals(imageNameWithoutExtension, StringComparison.OrdinalIgnoreCase));

//            if (duplicateFilePath != null)
//            {
//                string relativeDuplicatePath = duplicateFilePath.Replace(_baseImagePath, "")
//                    .TrimStart('\\', '/')
//                    .Replace("\\", "/");

//                return JsonDocument.Parse(JsonSerializer.Serialize(new
//                {
//                    Success = false,
//                    Message = $"Файл с таким названием уже существует в папке: {relativeDuplicatePath}"
//                }));
//            }

//            // Используем проверенное расширение из загружаемого файла
//            string extension = fileExtension;

//            // Формируем полное имя файла
//            string fullImageName = string.IsNullOrEmpty(Path.GetExtension(imageName))
//                ? $"{imageName}{extension}"
//                : imageName;

//            string fullPath = Path.Combine(directoryPath, fullImageName);

//            try
//            {
//                using (var fileStream = new FileStream(fullPath, FileMode.Create))
//                {
//                    await imageFile.CopyToAsync(fileStream);
//                }

//                string relativePath = BuildRelativePath(province, city, place, fullImageName);
//                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = true, Path = relativePath }));
//            }
//            catch (Exception ex)
//            {
//                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = $"Ошибка при сохранении изображения: {ex.Message}" }));
//            }
//        }

//        public JsonDocument GetImage(string province, string? city, string place, string imageName)
//        {
//            if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(place) || string.IsNullOrEmpty(imageName))
//            {
//                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = "Параметры запроса не могут быть пустыми" }));
//            }

//            string relativePath = BuildRelativePath(province, city, place, imageName);
//            string absolutePath = Path.Combine(_baseImagePath, relativePath);

//            if (!File.Exists(absolutePath))
//            {
//                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = $"Изображение не найдено" }));
//            }

//            try
//            {
//                byte[] imageBytes = File.ReadAllBytes(absolutePath);
//                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = true, Image = imageBytes }));
//            }
//            catch (Exception ex)
//            {
//                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = $"Ошибка при получении изображения: {ex.Message}" }));
//            }
//        }

//        public JsonDocument GetImagesList(int page, int pageSize)
//        {
//            try
//            {
//                if (page < 1)
//                {
//                    return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = "Номер страницы должен быть больше или равен 1" }));
//                }

//                if (pageSize < 1)
//                {
//                    return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = "Размер страницы должен быть больше или равен 1" }));
//                }

//                if (!Directory.Exists(_baseImagePath))
//                {
//                    return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = "Директория с изображениями не найдена" }));
//                }

//                string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

//                var allImages = Directory.GetFiles(_baseImagePath, "*.*", SearchOption.AllDirectories)
//                    .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()))
//                    .Select(file => file.Replace(_baseImagePath, "").TrimStart('\\', '/'))
//                    .ToList();

//                int totalImages = allImages.Count;
//                int totalPages = (int)Math.Ceiling((double)totalImages / pageSize);

//                // Вычисляем индексы для текущей страницы
//                int startIndex = (page - 1) * pageSize;

//                // Получаем только элементы для текущей страницы
//                var pagedImages = allImages
//                    .Skip(startIndex)
//                    .Take(pageSize)
//                    .ToList();

//                var result = new
//                {
//                    Success = true,
//                    TotalImages = totalImages,
//                    TotalPages = totalPages,
//                    CurrentPage = page,
//                    PageSize = pageSize,
//                    Images = pagedImages
//                };

//                return JsonDocument.Parse(JsonSerializer.Serialize(result));
//            }
//            catch (Exception ex)
//            {
//                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = $"Ошибка при получении списка изображений: {ex.Message}" }));
//            }
//        }

//        public JsonDocument DeleteImage(string province, string? city, string place, string imageName)
//        {
//            if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(place) || string.IsNullOrEmpty(imageName))
//            {
//                return CreateJsonResponse(false, "Параметры запроса не могут быть пустыми");
//            }

//            string relativePath = BuildRelativePath(province, city, place, imageName);
//            string absolutePath = Path.Combine(_baseImagePath, relativePath);

//            if (!File.Exists(absolutePath))
//            {
//                return CreateJsonResponse(false, $"Файл не существует");
//            }

//            try
//            {
//                File.Delete(absolutePath);
//                return CreateJsonResponse(true, $"Файл успешно удален");
//            }
//            catch (Exception ex)
//            {
//                return CreateJsonResponse(false, $"Ошибка при удалении изображения: {ex.Message}");
//            }
//        }

//        public async Task<JsonDocument> UpdateImageAsync(
//            string oldProvince, string? oldCity, string oldPlace, string oldImageName,
//            string? newProvince = null, string? newCity = null, string? newPlace = null,
//            string? newImageName = null, IFormFile? newImageFile = null)
//        {
//            try
//            {
//                // Проверка параметров
//                if (string.IsNullOrEmpty(oldProvince) || string.IsNullOrEmpty(oldPlace) || string.IsNullOrEmpty(oldImageName))
//                {
//                    return CreateJsonResponse(false, "Путь к исходному изображению не может быть пустым");
//                }

//                // Если ничего не меняется, нечего обновлять
//                if (newImageFile == null &&
//                    string.IsNullOrEmpty(newProvince) &&
//                    string.IsNullOrEmpty(newCity) &&
//                    string.IsNullOrEmpty(newPlace) &&
//                    string.IsNullOrEmpty(newImageName))
//                {
//                    return CreateJsonResponse(false, "Для обновления необходимо указать хотя бы один новый параметр");
//                }

//                // Проверка существования старого файла
//                string oldRelativePath = BuildRelativePath(oldProvince, oldCity, oldPlace, oldImageName);
//                string oldAbsolutePath = Path.Combine(_baseImagePath, oldRelativePath);

//                if (!File.Exists(oldAbsolutePath))
//                {
//                    return CreateJsonResponse(false, $"Исходное изображение не найдено");
//                }

//                // Использовать новые значения, если они указаны, иначе старые
//                string provinceToUse = newProvince ?? oldProvince;
//                string? cityToUse = newCity ?? oldCity;
//                string placeToUse = newPlace ?? oldPlace;
//                string imageNameToUse = newImageName ?? oldImageName;

//                // Определение директории для сохранения
//                string newDirectoryPath = BuildDirectoryPath(provinceToUse, cityToUse, placeToUse);

//                // Создание директории, если она не существует
//                if (!Directory.Exists(newDirectoryPath))
//                {
//                    Directory.CreateDirectory(newDirectoryPath);
//                }

//                // Проверка на дубликаты, только если меняется путь
//                if ((newProvince != null || newCity != null || newPlace != null || newImageName != null) &&
//                    (newProvince != oldProvince || newCity != oldCity || newPlace != oldPlace || newImageName != oldImageName))
//                {
//                    string imageNameWithoutExtension = Path.GetFileNameWithoutExtension(imageNameToUse);
//                    string[] allFiles = Directory.GetFiles(_baseImagePath, "*.*", SearchOption.AllDirectories);

//                    string duplicateFilePath = allFiles.FirstOrDefault(file =>
//                        Path.GetFileNameWithoutExtension(file).Equals(imageNameWithoutExtension, StringComparison.OrdinalIgnoreCase) &&
//                        file != oldAbsolutePath);  // Исключаем текущий файл из проверки

//                    if (duplicateFilePath != null)
//                    {
//                        string relativeDuplicatePath = duplicateFilePath.Replace(_baseImagePath, "")
//                            .TrimStart('\\', '/')
//                            .Replace("\\", "/");

//                        return CreateJsonResponse(false, $"Файл с таким названием уже существует в папке: {relativeDuplicatePath}");
//                    }
//                }

//                // Определение расширения файла
//                string extension;

//                if (newImageFile != null)
//                {
//                    // Проверка допустимых расширений
//                    string[] validExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
//                    extension = Path.GetExtension(newImageFile.FileName).ToLower();

//                    if (string.IsNullOrEmpty(extension))
//                    {
//                        extension = ".jpg"; // По умолчанию
//                    }
//                    else if (!validExtensions.Contains(extension))
//                    {
//                        return CreateJsonResponse(false, $"Недопустимое расширение файла: {extension}. Допустимые расширения: {string.Join(", ", validExtensions)}");
//                    }
//                }
//                else
//                {
//                    // Если нет нового файла, используем расширение старого
//                    extension = Path.GetExtension(oldImageName);
//                }

//                // Формирование имени файла с расширением
//                string fullImageName = string.IsNullOrEmpty(Path.GetExtension(imageNameToUse))
//                    ? $"{imageNameToUse}{extension}"
//                    : imageNameToUse;

//                string newAbsolutePath = Path.Combine(newDirectoryPath, fullImageName);

//                // Формирование относительного пути
//                string newRelativePath = BuildRelativePath(provinceToUse, cityToUse, placeToUse, fullImageName);

//                // Проверка, не указывает ли новый путь на старый файл
//                if (newAbsolutePath == oldAbsolutePath && newImageFile == null)
//                {
//                    return CreateJsonResponse(false, "Новый путь совпадает со старым, а новый файл не загружен. Нечего обновлять.");
//                }

//                // Обновление файла 
//                if (newImageFile != null)
//                {
//                    // Если это тот же путь, удаляем старый файл
//                    if (File.Exists(newAbsolutePath) && newAbsolutePath != oldAbsolutePath)
//                    {
//                        File.Delete(newAbsolutePath);
//                    }

//                    // Сохраняем новый файл
//                    using (var fileStream = new FileStream(newAbsolutePath, FileMode.Create))
//                    {
//                        await newImageFile.CopyToAsync(fileStream);
//                    }

//                    // Если путь изменился, удаляем старый файл
//                    if (newAbsolutePath != oldAbsolutePath)
//                    {
//                        File.Delete(oldAbsolutePath);
//                    }
//                }
//                else
//                {
//                    // Если путь изменился, но файл тот же - перемещаем файл
//                    if (newAbsolutePath != oldAbsolutePath)
//                    {
//                        // Если файл уже существует по новому пути, удаляем его
//                        if (File.Exists(newAbsolutePath))
//                        {
//                            File.Delete(newAbsolutePath);
//                        }

//                        // Копируем старый файл в новое место
//                        File.Copy(oldAbsolutePath, newAbsolutePath);

//                        // Удаляем старый файл
//                        File.Delete(oldAbsolutePath);
//                    }
//                }

//                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = true, Path = newRelativePath }));
//            }
//            catch (Exception ex)
//            {
//                return CreateJsonResponse(false, $"Ошибка при обновлении изображения: {ex.Message}");
//            }
//        }

//        // Методы для работы с MongoDB коллекцией images

//        /// <summary>
//        /// Получить доступ к коллекции изображений в MongoDB
//        /// </summary>
//        /// <returns>Коллекция MongoDB для изображений</returns>
//        public IMongoCollection<BsonDocument> GetImageCollection()
//        {
//            return _imageCollection;
//        }

//        /// <summary>
//        /// Создать документ изображения в MongoDB
//        /// </summary>
//        /// <param name="imageDocument">Документ изображения в формате JSON</param>
//        /// <returns>Результат операции</returns>
//        public async Task<JsonDocument> CreateImageDocumentAsync(JsonDocument imageDocument)
//        {
//            try
//            {
//                var bsonDocument = BsonDocument.Parse(imageDocument.RootElement.ToString());
//                await _imageCollection.InsertOneAsync(bsonDocument);
//                return JsonDocument.Parse(bsonDocument.ToJson());
//            }
//            catch (Exception ex)
//            {
//                return CreateJsonResponse(false, $"Ошибка при создании документа изображения: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Получить документ изображения из MongoDB по ID
//        /// </summary>
//        /// <param name="id">ID документа</param>
//        /// <returns>Документ изображения или ошибка</returns>
//        public async Task<JsonDocument> GetImageDocumentByIdAsync(string id)
//        {
//            try
//            {
//                if (!ObjectId.TryParse(id, out var objectId))
//                {
//                    return CreateJsonResponse(false, "Неверный формат ID");
//                }

//                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
//                var document = await _imageCollection.Find(filter).FirstOrDefaultAsync();

//                if (document == null)
//                {
//                    return CreateJsonResponse(false, "Документ с указанным ID не найден");
//                }

//                return JsonDocument.Parse(document.ToJson());
//            }
//            catch (Exception ex)
//            {
//                return CreateJsonResponse(false, $"Ошибка при получении документа изображения: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Обновить документ изображения в MongoDB
//        /// </summary>
//        /// <param name="id">ID документа</param>
//        /// <param name="updatedDocument">Обновленный документ</param>
//        /// <returns>Результат операции</returns>
//        public async Task<JsonDocument> UpdateImageDocumentAsync(string id, JsonDocument updatedDocument)
//        {
//            try
//            {
//                if (!ObjectId.TryParse(id, out var objectId))
//                {
//                    return CreateJsonResponse(false, "Неверный формат ID");
//                }

//                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
//                var existingDocument = await _imageCollection.Find(filter).FirstOrDefaultAsync();

//                if (existingDocument == null)
//                {
//                    return CreateJsonResponse(false, "Документ с указанным ID не найден");
//                }

//                var bsonDocument = BsonDocument.Parse(updatedDocument.RootElement.ToString());
//                if (bsonDocument.Contains("_id"))
//                {
//                    bsonDocument.Remove("_id");
//                }
//                bsonDocument.Add("_id", objectId);

//                await _imageCollection.ReplaceOneAsync(filter, bsonDocument);
//                return JsonDocument.Parse(bsonDocument.ToJson());
//            }
//            catch (Exception ex)
//            {
//                return CreateJsonResponse(false, $"Ошибка при обновлении документа изображения: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Удалить документ изображения из MongoDB
//        /// </summary>
//        /// <param name="id">ID документа</param>
//        /// <returns>Результат операции</returns>
//        public async Task<bool> DeleteImageDocumentAsync(string id)
//        {
//            try
//            {
//                if (!ObjectId.TryParse(id, out var objectId))
//                {
//                    return false;
//                }

//                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
//                var result = await _imageCollection.DeleteOneAsync(filter);
//                return result.DeletedCount > 0;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        /// <summary>
//        /// Получить список документов изображений с фильтрацией и пагинацией
//        /// </summary>
//        /// <param name="filter">Фильтры</param>
//        /// <param name="page">Номер страницы</param>
//        /// <param name="pageSize">Размер страницы</param>
//        /// <returns>Список документов и общее количество</returns>
//        public async Task<(List<JsonDocument> Documents, int TotalCount)> GetImageDocumentsAsync(
//            Dictionary<string, string> filter = null,
//            int page = 1,
//            int pageSize = 10)
//        {
//            try
//            {
//                FilterDefinition<BsonDocument> filterDefinition = Builders<BsonDocument>.Filter.Empty;

//                if (filter != null && filter.Count > 0)
//                {
//                    var filterBuilder = Builders<BsonDocument>.Filter;
//                    var filters = new List<FilterDefinition<BsonDocument>>();

//                    foreach (var kvp in filter)
//                    {
//                        if (!string.IsNullOrEmpty(kvp.Value))
//                        {
//                            filters.Add(filterBuilder.Regex(kvp.Key, new BsonRegularExpression(kvp.Value, "i")));
//                        }
//                    }

//                    if (filters.Count > 0)
//                    {
//                        filterDefinition = filterBuilder.And(filters);
//                    }
//                }

//                var totalCount = await _imageCollection.CountDocumentsAsync(filterDefinition);

//                var documents = await _imageCollection.Find(filterDefinition)
//                    .Skip((page - 1) * pageSize)
//                    .Limit(pageSize)
//                    .ToListAsync();

//                var jsonDocuments = new List<JsonDocument>();
//                foreach (var document in documents)
//                {
//                    jsonDocuments.Add(JsonDocument.Parse(document.ToJson()));
//                }

//                return (jsonDocuments, (int)totalCount);
//            }
//            catch (Exception ex)
//            {
//                return (new List<JsonDocument>
//                {
//                    CreateJsonResponse(false, $"Ошибка при получении документов изображений: {ex.Message}")
//                }, 0);
//            }
//        }

//        private JsonDocument CreateJsonResponse(bool success, string message)
//        {
//            var options = new JsonWriterOptions { Indented = true };
//            using var stream = new MemoryStream();
//            using (var writer = new Utf8JsonWriter(stream, options))
//            {
//                writer.WriteStartObject();
//                writer.WriteBoolean("Success", success);
//                writer.WriteString("Message", message);
//                writer.WriteEndObject();
//            }

//            stream.Position = 0;
//            return JsonDocument.Parse(stream);
//        }

//        // Helper methods for building paths
//        private string BuildDirectoryPath(string province, string? city, string place)
//        {
//            if (string.IsNullOrEmpty(city))
//            {
//                return Path.Combine(_baseImagePath, province, place);
//            }
//            else
//            {
//                return Path.Combine(_baseImagePath, province, city, place);
//            }
//        }

//        private string BuildRelativePath(string province, string? city, string place, string imageName)
//        {
//            if (string.IsNullOrEmpty(city))
//            {
//                return Path.Combine(province, place, imageName).Replace("\\", "/");
//            }
//            else
//            {
//                return Path.Combine(province, city, place, imageName).Replace("\\", "/");
//            }
//        }
//    }
//}

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
                if (!ObjectId.TryParse(id, out var objectId))
                {
                    return CreateJsonResponse(false, "Неверный формат ID");
                }

                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);

                var imageRecord = await _imageCollection.Find(filter).FirstOrDefaultAsync();
                if (imageRecord == null)
                {
                    return CreateJsonResponse(false, "Изображение не найдено");
                }

                string filePath = imageRecord["FilePath"].AsString;
                string absolutePath = Path.Combine(_baseImagePath, filePath);

                // Удаляем файл с диска, если он существует
                if (File.Exists(absolutePath))
                {
                    File.Delete(absolutePath);
                }

                // Полностью удаляем запись из MongoDB
                var deleteResult = await _imageCollection.DeleteOneAsync(filter);

                if (deleteResult.DeletedCount == 0)
                {
                    return CreateJsonResponse(false, "Не удалось удалить запись из базы данных");
                }

                return CreateJsonResponse(true, "Изображение и запись успешно удалены");
            }
            catch (Exception ex)
            {
                return CreateJsonResponse(false, $"Ошибка при удалении изображения: {ex.Message}");
            }
        }

       private JsonDocument CreateJsonResponse(bool success, string message)
        {
            var options = new JsonWriterOptions { Indented = true };
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, options))
            {
                writer.WriteStartObject();
                writer.WriteBoolean("Success", success);
                writer.WriteString("Message", message);
                writer.WriteEndObject();
            }

            stream.Position = 0;
            return JsonDocument.Parse(stream);
        }

        // Helper methods for building paths
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