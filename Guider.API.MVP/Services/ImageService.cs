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

            // Убедимся, что базовая директория существует
            if (!Directory.Exists(_baseImagePath))
            {
                Directory.CreateDirectory(_baseImagePath);
            }
        }
        public async Task<string> SaveImageAsync(string imagePath, IFormFile imageFile)
        {
            if (string.IsNullOrEmpty(imagePath) || imageFile == null || imageFile.Length == 0)
            {
                throw new ArgumentException("Недопустимые параметры для загрузки изображения");
            }

            // Нормализуем путь и заменяем все недопустимые символы
            imagePath = imagePath.Trim('/');
            string[] pathParts = imagePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (pathParts.Length < 3)
            {
                throw new ArgumentException("Путь должен содержать провинцию, город и название изображения");
            }

            string province = pathParts[0];
            string city = pathParts[1];
            string imageName = pathParts[2];

            // Создаем директории, если они не существуют
            string directoryPath = Path.Combine(_baseImagePath, province, city);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Получаем расширение файла
            string extension = Path.GetExtension(imageFile.FileName);
            if (string.IsNullOrEmpty(extension))
            {
                extension = ".jpg"; // Расширение по умолчанию
            }

            // Формируем полный путь к файлу
            string fullImageName = string.IsNullOrEmpty(Path.GetExtension(imageName))
                ? $"{imageName}{extension}"
                : imageName;

            string fullPath = Path.Combine(directoryPath, fullImageName);

            try
            {
                // Сохраняем файл
                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                // Возвращаем относительный путь к изображению
                return Path.Combine(province, city, fullImageName).Replace("\\", "/");
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Ошибка при сохранении изображения");
                throw new Exception("Ошибка при сохранении изображения", ex);
            }
        }

        public byte[] GetImage(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new ArgumentException("Путь к изображению не может быть пустым");
            }

            string absolutePath = Path.Combine(_baseImagePath, fullPath);

            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException($"Изображение не найдено по пути: {fullPath}");
            }

            try
            {
                return File.ReadAllBytes(absolutePath);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Ошибка при получении изображения");
                throw new Exception("Ошибка при получении изображения", ex);
            }
        }
        public bool DeleteImage(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return false;
            }

            string absolutePath = Path.Combine(_baseImagePath, fullPath);

            if (!File.Exists(absolutePath))
            {
                return false;
            }

            try
            {
                File.Delete(absolutePath);
                return true;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Ошибка при удалении изображения");
                return false;
            }
        }

        // Метод для обновления существующего изображения
        public async Task<string> UpdateImageAsync(string imagePath, IFormFile imageFile, bool createIfNotExists = true)
        {
            if (string.IsNullOrEmpty(imagePath) || imageFile == null || imageFile.Length == 0)
            {
                throw new ArgumentException("Недопустимые параметры для обновления изображения");
            }

            // Нормализуем путь
            imagePath = imagePath.Trim('/');
            string[] pathParts = imagePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (pathParts.Length < 3)
            {
                throw new ArgumentException("Путь должен содержать провинцию, город и название изображения");
            }

            string province = pathParts[0];
            string city = pathParts[1];
            string imageName = pathParts[2];

            // Формируем путь к директории
            string directoryPath = Path.Combine(_baseImagePath, province, city);

            // Получаем расширение файла
            string extension = Path.GetExtension(imageFile.FileName);
            if (string.IsNullOrEmpty(extension))
            {
                extension = ".jpg"; // Расширение по умолчанию
            }

            // Формируем полное имя файла
            string fullImageName = string.IsNullOrEmpty(Path.GetExtension(imageName))
                ? $"{imageName}{extension}"
                : imageName;

            string fullPath = Path.Combine(directoryPath, fullImageName);
            string relativePath = Path.Combine(province, city, fullImageName).Replace("\\", "/");

            // Проверяем существование файла и директории
            bool fileExists = File.Exists(fullPath);
            bool directoryExists = Directory.Exists(directoryPath);

            // Если файл не существует и не нужно создавать новый
            if (!fileExists && !createIfNotExists)
            {
                throw new FileNotFoundException($"Изображение для обновления не найдено по пути: {relativePath}");
            }

            try
            {
                // Создаем директорию, если она не существует
                if (!directoryExists)
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Если файл существует, удаляем его перед сохранением нового
                if (fileExists)
                {
                    File.Delete(fullPath);
                    //_logger.LogInformation($"Существующее изображение удалено: {relativePath}");
                }

                // Сохраняем новый файл
                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                //_logger.LogInformation($"Изображение {(fileExists ? "обновлено" : "создано")}: {relativePath}");

                // Возвращаем относительный путь к изображению
                return relativePath;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, $"Ошибка при {(fileExists ? "обновлении" : "создании")} изображения");
                throw new Exception($"Ошибка при {(fileExists ? "обновлении" : "создании")} изображения", ex);
            }
        }

        
    }
}
