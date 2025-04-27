using System.Text.Json;

namespace Guider.API.MVP.Services
{
    public class ImageService : IImageService
    {
        private readonly string _baseImagePath;
        //private readonly ILogger<ImageService> _logger;
        public ImageService(IConfiguration configuration/*, ILogger<ImageService> logger*/)
        {
            _baseImagePath = configuration["StaticFiles:ImagesPath"] ?? "wwwroot/images";
            // _logger = logger;

            
            if (!Directory.Exists(_baseImagePath))
            {
                Directory.CreateDirectory(_baseImagePath);
            }
        }

        public async Task<JsonDocument> SaveImageAsync(string imagePath, IFormFile imageFile)
        {
            if (string.IsNullOrEmpty(imagePath) || imageFile == null || imageFile.Length == 0)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = "Недопустимые параметры для загрузки изображения" }));
            }

            
            imagePath = imagePath.Trim('/');
            string[] pathParts = imagePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (pathParts.Length < 1)
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = "Путь обязательно должен содержать хотя бы название провинции" }));
            }

            string province = pathParts[0];
            string city = pathParts.Length > 1 ? pathParts[1] : null;
            string imageName = pathParts.Length > 2 ? pathParts[2] : null;

            if (string.IsNullOrEmpty(imageName))
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = "Путь должен содержать название изображения" }));
            }

            
            string directoryPath = city == null
                ? Path.Combine(_baseImagePath, province)
                : Path.Combine(_baseImagePath, province, city);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            
            
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


            
            string extension = Path.GetExtension(imageFile.FileName);
            if (string.IsNullOrEmpty(extension))
            {
                extension = ".jpg"; // Расширение по умолчанию
            }

            
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

               
                string relativePath = city == null
                    ? Path.Combine(province, fullImageName).Replace("\\", "/")
                    : Path.Combine(province, city, fullImageName).Replace("\\", "/");

                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = true, Path = relativePath }));
            }
            catch (Exception ex)
            {
                
                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = $"Ошибка при сохранении изображения: {ex.Message}" }));
            }
        }

        public JsonDocument GetImage(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = "Путь к изображению не может быть пустым" }));
            }

            string absolutePath = Path.Combine(_baseImagePath, fullPath);

            if (!File.Exists(absolutePath))
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = $"Изображение не найдено по пути: {fullPath}" }));
            }

            try
            {
                byte[] imageBytes = File.ReadAllBytes(absolutePath);
                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = true,  Image = imageBytes}));
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Ошибка при получении изображения");
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
                //_logger.LogError(ex, "Ошибка при получении списка изображений");
                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = false, Message = $"Ошибка при получении списка изображений: {ex.Message}" }));
            }
        }

        public JsonDocument DeleteImage(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return CreateJsonResponse(false, "Путь к файлу не указан");
            }

            string absolutePath = Path.Combine(_baseImagePath, fullPath);

            if (!File.Exists(absolutePath))
            {
                return CreateJsonResponse(false, $"Файл не существует по пути: {absolutePath}");
            }

            try
            {
                File.Delete(absolutePath);
                return CreateJsonResponse(true, $"Файл успешно удален: {absolutePath}");
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Ошибка при удалении изображения");
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

        public async Task<JsonDocument> UpdateImageAsync(
                                                            string oldImagePath,
                                                            string newImagePath = null,
                                                            IFormFile newImageFile = null)
        {
            try
            {
                // Проверка на пустой старый путь
                if (string.IsNullOrEmpty(oldImagePath))
                {
                    return CreateJsonResponse(false, "Путь к исходному изображению не может быть пустым");
                }

                // Если и новый путь, и новый файл пустые, нечего обновлять
                if (string.IsNullOrEmpty(newImagePath) && (newImageFile == null || newImageFile.Length == 0))
                {
                    return CreateJsonResponse(false, "Для обновления необходимо указать новый путь или загрузить новый файл");
                }

                // Проверка существования старого файла
                string oldAbsolutePath = Path.Combine(_baseImagePath, oldImagePath);
                if (!File.Exists(oldAbsolutePath))
                {
                    return CreateJsonResponse(false, $"Исходное изображение не найдено по пути: {oldImagePath}");
                }

                // Если новый путь не указан, используем старый
                string pathToUse = newImagePath ?? oldImagePath;

                // Обработка пути
                pathToUse = pathToUse.Trim('/');
                string[] pathParts = pathToUse.Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (pathParts.Length < 1)
                {
                    return CreateJsonResponse(false, "Путь обязательно должен содержать хотя бы название провинции");
                }

                string province = pathParts[0];
                string city = pathParts.Length > 1 ? pathParts[1] : null;
                string imageName = pathParts.Length > 2 ? pathParts[2] : null;

                if (string.IsNullOrEmpty(imageName))
                {
                    // Если имя не указано в новом пути, используем имя из старого пути
                    imageName = Path.GetFileName(oldImagePath);
                }

                // Определение директории для сохранения
                string directoryPath = city == null
                    ? Path.Combine(_baseImagePath, province)
                    : Path.Combine(_baseImagePath, province, city);

                // Создание директории, если она не существует
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Проверка на дубликаты, только если меняется путь
                if (newImagePath != null && newImagePath != oldImagePath)
                {
                    string imageNameWithoutExtension = Path.GetFileNameWithoutExtension(imageName);
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
                    extension = Path.GetExtension(oldImagePath);
                }

                // Формирование имени файла с расширением
                string fullImageName = string.IsNullOrEmpty(Path.GetExtension(imageName))
                    ? $"{imageName}{extension}"
                    : imageName;

                string newAbsolutePath = Path.Combine(directoryPath, fullImageName);

                // Формирование относительного пути
                string relativePath = city == null
                    ? Path.Combine(province, fullImageName).Replace("\\", "/")
                    : Path.Combine(province, city, fullImageName).Replace("\\", "/");

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

                return JsonDocument.Parse(JsonSerializer.Serialize(new { Success = true, Path = relativePath }));
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Ошибка при обновлении изображения");
                return CreateJsonResponse(false, $"Ошибка при обновлении изображения: {ex.Message}");
            }
        }




    }
}
