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

        
        //public async Task<string> UpdateImageAsync(string imagePath, IFormFile imageFile, bool createIfNotExists = true)
        //{
        //    if (string.IsNullOrEmpty(imagePath) || imageFile == null || imageFile.Length == 0)
        //    {
        //        throw new ArgumentException("Недопустимые параметры для обновления изображения");
        //    }

            
        //    imagePath = imagePath.Trim('/');
        //    string[] pathParts = imagePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        //    if (pathParts.Length < 1)
        //    {
        //        throw new ArgumentException("Путь должен содержать провинцию и название изображения");
        //    }

        //    string province = pathParts[0];
        //    string city = pathParts[1];
        //    string imageName = pathParts[2];

            
        //    string directoryPath = Path.Combine(_baseImagePath, province, city);

            
        //    string extension = Path.GetExtension(imageFile.FileName);
        //    if (string.IsNullOrEmpty(extension))
        //    {
        //        extension = ".jpg"; 
        //    }

           
        //    string fullImageName = string.IsNullOrEmpty(Path.GetExtension(imageName))
        //        ? $"{imageName}{extension}"
        //        : imageName;

        //    string fullPath = Path.Combine(directoryPath, fullImageName);
        //    string relativePath = Path.Combine(province, city, fullImageName).Replace("\\", "/");

            
        //    bool fileExists = File.Exists(fullPath);
        //    bool directoryExists = Directory.Exists(directoryPath);

        //    /
        //    if (!fileExists && !createIfNotExists)
        //    {
        //        throw new FileNotFoundException($"Изображение для обновления не найдено по пути: {relativePath}");
        //    }

        //    try
        //    {
                
        //        if (!directoryExists)
        //        {
        //            Directory.CreateDirectory(directoryPath);
        //        }

                
        //        if (fileExists)
        //        {
        //            File.Delete(fullPath);
        //            //_logger.LogInformation($"Существующее изображение удалено: {relativePath}");
        //        }

               
        //        using (var fileStream = new FileStream(fullPath, FileMode.Create))
        //        {
        //            await imageFile.CopyToAsync(fileStream);
        //        }

        //        //_logger.LogInformation($"Изображение {(fileExists ? "обновлено" : "создано")}: {relativePath}");

                
        //        return relativePath;
        //    }
        //    catch (Exception ex)
        //    {
        //        //_logger.LogError(ex, $"Ошибка при {(fileExists ? "обновлении" : "создании")} изображения");
        //        throw new Exception($"Ошибка при {(fileExists ? "обновлении" : "создании")} изображения", ex);
        //    }
        //}

      
    }
}
