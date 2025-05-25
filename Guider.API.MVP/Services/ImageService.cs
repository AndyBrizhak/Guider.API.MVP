using System.Text.Json;

namespace Guider.API.MVP.Services
{
    public class ImageService : IImageService
    {
        private readonly string _baseImagePath;

        public ImageService(IConfiguration configuration)
        {
            _baseImagePath = configuration["StaticFiles:ImagesPath"] ?? "wwwroot/images";

            if (!Directory.Exists(_baseImagePath))
            {
                Directory.CreateDirectory(_baseImagePath);
            }
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

            // Проверка на дубликаты имен файлов
            string imageNameWithoutExtension = Path.GetFileNameWithoutExtension(imageName);
            string[] allFiles = Directory.GetFiles(_baseImagePath, "*.*", SearchOption.AllDirectories);

            string duplicateFilePath = allFiles.FirstOrDefault(file =>
                Path.GetFileNameWithoutExtension(file).Equals(imageNameWithoutExtension, StringComparison.OrdinalIgnoreCase));

            if (duplicateFilePath != null)
            {
                string relativeDuplicatePath = duplicateFilePath.Replace(_baseImagePath, "")
                    .TrimStart('\\', '/')
                    .Replace("\\", "/");

                return JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    Success = false,
                    Message = $"Файл с таким названием уже существует в папке: {relativeDuplicatePath}"
                }));
            }

            // Используем проверенное расширение из загружаемого файла
            string extension = fileExtension;

            // Формируем полное имя файла
            string fullImageName = string.IsNullOrEmpty(Path.GetExtension(imageName))
                ? $"{imageName}{extension}"
                : imageName;

            string fullPath = Path.Combine(directoryPath, fullImageName);

            try
            {
                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                string relativePath = BuildRelativePath(province, city, place, fullImageName);
                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = true, Path = relativePath }));
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = $"Ошибка при сохранении изображения: {ex.Message}" }));
            }
        }

        public JsonDocument GetImage(string province, string? city, string place, string imageName)
        {
            if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(place) || string.IsNullOrEmpty(imageName))
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = "Параметры запроса не могут быть пустыми" }));
            }

            string relativePath = BuildRelativePath(province, city, place, imageName);
            string absolutePath = Path.Combine(_baseImagePath, relativePath);

            if (!File.Exists(absolutePath))
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = $"Изображение не найдено" }));
            }

            try
            {
                byte[] imageBytes = File.ReadAllBytes(absolutePath);
                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = true, Image = imageBytes }));
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = $"Ошибка при получении изображения: {ex.Message}" }));
            }
        }

        public JsonDocument GetImagesList(int page, int pageSize)
        {
            try
            {
                if (page < 1)
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = "Номер страницы должен быть больше или равен 1" }));
                }

                if (pageSize < 1)
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = "Размер страницы должен быть больше или равен 1" }));
                }

                if (!Directory.Exists(_baseImagePath))
                {
                    return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = "Директория с изображениями не найдена" }));
                }

                string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

                var allImages = Directory.GetFiles(_baseImagePath, "*.*", SearchOption.AllDirectories)
                    .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()))
                    .Select(file => file.Replace(_baseImagePath, "").TrimStart('\\', '/'))
                    .ToList();

                int totalImages = allImages.Count;
                int totalPages = (int)Math.Ceiling((double)totalImages / pageSize);

                // Вычисляем индексы для текущей страницы
                int startIndex = (page - 1) * pageSize;

                // Получаем только элементы для текущей страницы
                var pagedImages = allImages
                    .Skip(startIndex)
                    .Take(pageSize)
                    .ToList();

                var result = new
                {
                    Success = true,
                    TotalImages = totalImages,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize,
                    Images = pagedImages
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(result));
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = $"Ошибка при получении списка изображений: {ex.Message}" }));
            }
        }

        public JsonDocument DeleteImage(string province, string? city, string place, string imageName)
        {
            if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(place) || string.IsNullOrEmpty(imageName))
            {
                return CreateJsonResponse(false, "Параметры запроса не могут быть пустыми");
            }

            string relativePath = BuildRelativePath(province, city, place, imageName);
            string absolutePath = Path.Combine(_baseImagePath, relativePath);

            if (!File.Exists(absolutePath))
            {
                return CreateJsonResponse(false, $"Файл не существует");
            }

            try
            {
                File.Delete(absolutePath);
                return CreateJsonResponse(true, $"Файл успешно удален");
            }
            catch (Exception ex)
            {
                return CreateJsonResponse(false, $"Ошибка при удалении изображения: {ex.Message}");
            }
        }

        public async Task<JsonDocument> UpdateImageAsync(
            string oldProvince, string? oldCity, string oldPlace, string oldImageName,
            string? newProvince = null, string? newCity = null, string? newPlace = null,
            string? newImageName = null, IFormFile? newImageFile = null)
        {
            try
            {
                // Проверка параметров
                if (string.IsNullOrEmpty(oldProvince) || string.IsNullOrEmpty(oldPlace) || string.IsNullOrEmpty(oldImageName))
                {
                    return CreateJsonResponse(false, "Путь к исходному изображению не может быть пустым");
                }

                // Если ничего не меняется, нечего обновлять
                if (newImageFile == null &&
                    string.IsNullOrEmpty(newProvince) &&
                    string.IsNullOrEmpty(newCity) &&
                    string.IsNullOrEmpty(newPlace) &&
                    string.IsNullOrEmpty(newImageName))
                {
                    return CreateJsonResponse(false, "Для обновления необходимо указать хотя бы один новый параметр");
                }

                // Проверка существования старого файла
                string oldRelativePath = BuildRelativePath(oldProvince, oldCity, oldPlace, oldImageName);
                string oldAbsolutePath = Path.Combine(_baseImagePath, oldRelativePath);

                if (!File.Exists(oldAbsolutePath))
                {
                    return CreateJsonResponse(false, $"Исходное изображение не найдено");
                }

                // Использовать новые значения, если они указаны, иначе старые
                string provinceToUse = newProvince ?? oldProvince;
                string? cityToUse = newCity ?? oldCity;
                string placeToUse = newPlace ?? oldPlace;
                string imageNameToUse = newImageName ?? oldImageName;

                // Определение директории для сохранения
                string newDirectoryPath = BuildDirectoryPath(provinceToUse, cityToUse, placeToUse);

                // Создание директории, если она не существует
                if (!Directory.Exists(newDirectoryPath))
                {
                    Directory.CreateDirectory(newDirectoryPath);
                }

                // Проверка на дубликаты, только если меняется путь
                if ((newProvince != null || newCity != null || newPlace != null || newImageName != null) &&
                    (newProvince != oldProvince || newCity != oldCity || newPlace != oldPlace || newImageName != oldImageName))
                {
                    string imageNameWithoutExtension = Path.GetFileNameWithoutExtension(imageNameToUse);
                    string[] allFiles = Directory.GetFiles(_baseImagePath, "*.*", SearchOption.AllDirectories);

                    string duplicateFilePath = allFiles.FirstOrDefault(file =>
                        Path.GetFileNameWithoutExtension(file).Equals(imageNameWithoutExtension, StringComparison.OrdinalIgnoreCase) &&
                        file != oldAbsolutePath);  // Исключаем текущий файл из проверки

                    if (duplicateFilePath != null)
                    {
                        string relativeDuplicatePath = duplicateFilePath.Replace(_baseImagePath, "")
                            .TrimStart('\\', '/')
                            .Replace("\\", "/");

                        return CreateJsonResponse(false, $"Файл с таким названием уже существует в папке: {relativeDuplicatePath}");
                    }
                }

                // Определение расширения файла
                string extension;

                if (newImageFile != null)
                {
                    // Проверка допустимых расширений
                    string[] validExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                    extension = Path.GetExtension(newImageFile.FileName).ToLower();

                    if (string.IsNullOrEmpty(extension))
                    {
                        extension = ".jpg"; // По умолчанию
                    }
                    else if (!validExtensions.Contains(extension))
                    {
                        return CreateJsonResponse(false, $"Недопустимое расширение файла: {extension}. Допустимые расширения: {string.Join(", ", validExtensions)}");
                    }
                }
                else
                {
                    // Если нет нового файла, используем расширение старого
                    extension = Path.GetExtension(oldImageName);
                }

                // Формирование имени файла с расширением
                string fullImageName = string.IsNullOrEmpty(Path.GetExtension(imageNameToUse))
                    ? $"{imageNameToUse}{extension}"
                    : imageNameToUse;

                string newAbsolutePath = Path.Combine(newDirectoryPath, fullImageName);

                // Формирование относительного пути
                string newRelativePath = BuildRelativePath(provinceToUse, cityToUse, placeToUse, fullImageName);

                // Проверка, не указывает ли новый путь на старый файл
                if (newAbsolutePath == oldAbsolutePath && newImageFile == null)
                {
                    return CreateJsonResponse(false, "Новый путь совпадает со старым, а новый файл не загружен. Нечего обновлять.");
                }

                // Обновление файла 
                if (newImageFile != null)
                {
                    // Если это тот же путь, удаляем старый файл
                    if (File.Exists(newAbsolutePath) && newAbsolutePath != oldAbsolutePath)
                    {
                        File.Delete(newAbsolutePath);
                    }

                    // Сохраняем новый файл
                    using (var fileStream = new FileStream(newAbsolutePath, FileMode.Create))
                    {
                        await newImageFile.CopyToAsync(fileStream);
                    }

                    // Если путь изменился, удаляем старый файл
                    if (newAbsolutePath != oldAbsolutePath)
                    {
                        File.Delete(oldAbsolutePath);
                    }
                }
                else
                {
                    // Если путь изменился, но файл тот же - перемещаем файл
                    if (newAbsolutePath != oldAbsolutePath)
                    {
                        // Если файл уже существует по новому пути, удаляем его
                        if (File.Exists(newAbsolutePath))
                        {
                            File.Delete(newAbsolutePath);
                        }

                        // Копируем старый файл в новое место
                        File.Copy(oldAbsolutePath, newAbsolutePath);

                        // Удаляем старый файл
                        File.Delete(oldAbsolutePath);
                    }
                }

                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = true, Path = newRelativePath }));
            }
            catch (Exception ex)
            {
                return CreateJsonResponse(false, $"Ошибка при обновлении изображения: {ex.Message}");
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
