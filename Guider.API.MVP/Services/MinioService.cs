using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.Net;

namespace Guider.API.MVP.Services
{
    /// <summary>
    /// Сервис для работы с MinIO S3-хранилищем
    /// </summary>
    public class MinioService : IMinioService
    {
        private readonly IMinioClient _minioClient;
        private readonly MinioSettings _minioSettings;
        private readonly ILogger<MinioService> _logger;

        public MinioService(IOptions<MinioSettings> minioSettings, ILogger<MinioService> logger)
        {
            _minioSettings = minioSettings.Value;
            _logger = logger;

            // Создаем MinIO клиент
            _minioClient = new MinioClient()
            .WithEndpoint(_minioSettings.Endpoint, _minioSettings.Port)
            .WithCredentials(_minioSettings.AccessKey, _minioSettings.SecretKey)
            .WithSSL(_minioSettings.UseSSL)
            .Build();


            // Инициализируем bucket при создании сервиса
            _ = Task.Run(async () => await EnsureBucketExistsAsync());
        }

        /// <summary>
        /// Загружает файл в MinIO хранилище
        /// </summary>
        public async Task<string> UploadFileAsync(IFormFile file, string fileName, string fileExtension)
        {
            try
            {
                _logger.LogInformation($"Начало загрузки файла: {fileName}.{fileExtension}");

                // Базовые проверки файла
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("Попытка загрузки пустого файла или файл не выбран");
                    return "Ошибка: Файл не выбран или пустой";
                }

                // Проверяем размер файла (максимум 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    _logger.LogWarning($"Размер файла {fileName} превышает лимит: {file.Length} байт (максимум 10MB)");
                    return "Ошибка: Размер файла превышает 10MB";
                }

                _logger.LogInformation($"Размер файла: {file.Length} байт, Content-Type: {file.ContentType}");

                // Формируем полное имя файла с расширением
                var fullFileName = $"{fileName}.{fileExtension.TrimStart('.')}";
                _logger.LogInformation($"Полное имя файла в хранилище: {fullFileName}");

                // Проверяем существование файла перед загрузкой
                _logger.LogInformation($"Проверка существования файла {fullFileName} перед загрузкой...");
                var fileExistsBefore = await FileExistsAsync(fullFileName);

                if (fileExistsBefore)
                {
                    _logger.LogWarning($"Файл {fullFileName} уже существует в хранилище. Загрузка отменена.");
                    return "Ошибка: Файл с таким именем уже существует в хранилище";
                }

                _logger.LogInformation($"Файл {fullFileName} не существует, продолжаем загрузку");

                // Определяем Content-Type
                var contentType = GetContentType(fileExtension);
                _logger.LogInformation($"Определен Content-Type: {contentType}");

                // Проверяем существование bucket
                _logger.LogInformation("Проверка существования bucket...");
                await EnsureBucketExistsAsync();

                // Загружаем файл
                _logger.LogInformation($"Начинаем загрузку файла в MinIO...");
                using var stream = file.OpenReadStream();

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_minioSettings.BucketName)
                    .WithObject(fullFileName)
                    .WithStreamData(stream)
                    .WithObjectSize(file.Length)
                    .WithContentType(contentType);

                var response = await _minioClient.PutObjectAsync(putObjectArgs);

                // Логируем все доступные свойства ответа от MinIO
                _logger.LogInformation($"Файл успешно загружен в MinIO:");
                _logger.LogInformation($"  - Размер: {response.Size} байт");
                _logger.LogInformation($"  - ETag: {response.Etag}");
                _logger.LogInformation($"  - Bucket: {_minioSettings.BucketName}");
                _logger.LogInformation($"  - Object Name: {fullFileName}");
                _logger.LogInformation($"  - Content-Type: {contentType}");

                // Проверяем существование файла после загрузки
                _logger.LogInformation($"Проверка существования файла {fullFileName} после загрузки...");
                var fileExistsAfter = await FileExistsAsync(fullFileName);

                if (!fileExistsAfter)
                {
                    _logger.LogError($"Файл {fullFileName} не найден в хранилище после загрузки!");
                    return "Ошибка: Файл не был сохранен в хранилище";
                }

                _logger.LogInformation($"Файл {fullFileName} успешно сохранен и подтвержден в хранилище");

                // Формируем полный URL файла с правильным протоколом и портом
                var protocol = _minioSettings.UseSSL ? "https" : "http";
                var portPart = _minioSettings.Port > 0 ? $":{_minioSettings.Port}" : "";
                var fileUrl = $"{protocol}://{_minioSettings.Endpoint}{portPart}/{_minioSettings.BucketName}/{fullFileName}";

                _logger.LogInformation($"Сформирован URL файла: {fileUrl}");
                _logger.LogInformation($"Загрузка файла {fileName}.{fileExtension} завершена успешно");

                return fileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Критическая ошибка при загрузке файла {fileName}: {ex.Message}");
                _logger.LogError($"Тип исключения: {ex.GetType().Name}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");

                return $"Ошибка при загрузке файла: {ex.Message}";
            }
        }

        /// <summary>
        /// Удаляет файл из MinIO хранилища по URL
        /// </summary>
        public async Task<DeleteFileResult> DeleteFileAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                {
                    return new DeleteFileResult
                    {
                        IsDeleted = false,
                        Message = "URL файла не указан"
                    };
                }

                // Извлекаем имя файла из URL
                var fileName = ExtractFileNameFromUrl(fileUrl);
                if (string.IsNullOrEmpty(fileName))
                {
                    return new DeleteFileResult
                    {
                        IsDeleted = false,
                        Message = "Не удалось извлечь имя файла из URL"
                    };
                }

                // Проверяем существование файла
                var exists = await FileExistsAsync(fileName);
                if (!exists)
                {
                    return new DeleteFileResult
                    {
                        IsDeleted = false,
                        Message = "Файл не найден в хранилище MinIO"
                    };
                }

                // Удаляем файл
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(_minioSettings.BucketName)
                    .WithObject(fileName);

                await _minioClient.RemoveObjectAsync(removeObjectArgs);

                _logger.LogInformation($"Файл {fileName} успешно удален из MinIO");

                return new DeleteFileResult
                {
                    IsDeleted = true,
                    Message = "Файл успешно удален из хранилища MinIO"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при удалении файла по URL: {fileUrl}");
                return new DeleteFileResult
                {
                    IsDeleted = false,
                    Message = $"Ошибка при удалении файла из хранилища: {ex.Message}"
                };
            }
        }


        /// <summary>
        /// Проверяет существование файла в хранилище
        /// </summary>
        /// <param name="fileName">Имя файла для проверки</param>
        /// <returns>True если файл существует, False если не существует</returns>
        public async Task<bool> FileExistsAsync(string fileName)
        {
            try
            {
                _logger.LogDebug($"Проверка существования файла: {fileName}");

                var statObjectArgs = new StatObjectArgs()
                    .WithBucket(_minioSettings.BucketName)
                    .WithObject(fileName);

                var result = await _minioClient.StatObjectAsync(statObjectArgs);

                // Файл найден - логируем детали
                _logger.LogDebug($"Файл {fileName} найден в хранилище. Размер: {result.Size} байт, последнее изменение: {result.LastModified}");

                // Дополнительная проверка на случай, если результат null (хотя это маловероятно)
                bool fileExists = result != null;
                _logger.LogDebug($"Результат проверки существования файла {fileName}: {fileExists}");

                return fileExists;
            }
            catch (ObjectNotFoundException)
            {
                // Это нормальная ситуация - файл просто не существует
                _logger.LogDebug($"Файл {fileName} не найден в хранилище MinIO (это нормально)");
                return false;
            }
            catch (BucketNotFoundException)
            {
                // Bucket не существует - это тоже не критическая ошибка для проверки файла
                _logger.LogWarning($"Bucket '{_minioSettings.BucketName}' не найден при проверке файла {fileName}");
                return false;
            }
            catch (MinioException minioEx)
            {
                // Специфичные ошибки MinIO - логируем как предупреждения
                _logger.LogWarning(minioEx, $"MinIO ошибка при проверке существования файла {fileName}: {minioEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Только неожиданные системные ошибки логируем как ошибки
                _logger.LogError(ex, $"Неожиданная системная ошибка при проверке существования файла {fileName} в MinIO: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Получает URL файла для доступа
        /// </summary>
        public string GetFileUrl(string fileName)
        {
            var protocol = _minioSettings.UseSSL ? "https" : "http";
            var portPart = _minioSettings.Port > 0 ? $":{_minioSettings.Port}" : "";
            var fileUrl = $"{protocol}://{_minioSettings.Endpoint}{portPart}/{_minioSettings.BucketName}/{fileName}";

            _logger.LogDebug($"Сформирован URL для файла {fileName}: {fileUrl}");

            return fileUrl;
        }

        /// <summary>
        /// Убеждается, что bucket существует
        /// </summary>
        private async Task EnsureBucketExistsAsync()
        {
            try
            {
                var bucketExistsArgs = new BucketExistsArgs()
                    .WithBucket(_minioSettings.BucketName);

                var bucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs);

                if (!bucketExists)
                {
                    var makeBucketArgs = new MakeBucketArgs()
                        .WithBucket(_minioSettings.BucketName);

                    await _minioClient.MakeBucketAsync(makeBucketArgs);
                    _logger.LogInformation($"Bucket {_minioSettings.BucketName} создан");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании/проверке bucket");
                throw;
            }
        }

        /// <summary>
        /// Определяет Content-Type по расширению файла
        /// </summary>
        private static string GetContentType(string fileExtension)
        {
            return fileExtension.ToLower().TrimStart('.') switch
            {
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "gif" => "image/gif",
                "bmp" => "image/bmp",
                "webp" => "image/webp",
                "svg" => "image/svg+xml",
                "pdf" => "application/pdf",
                "txt" => "text/plain",
                "doc" => "application/msword",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }
                
        /// <summary>
        /// Извлекает имя файла из URL с учетом структуры папок (провинция/город/место/файл)
        /// </summary>
        private string ExtractFileNameFromUrl(string fileUrl)
        {
            try
            {
                var uri = new Uri(fileUrl);
                var path = uri.AbsolutePath; // Получаем полный путь без домена

                // Убираем начальный слеш и название bucket
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

                // Первый сегмент обычно bucket name, остальные - это наш путь
                if (segments.Length > 1)
                {
                    // Собираем путь начиная со второго элемента (пропускаем bucket name)
                    var fileName = string.Join("/", segments.Skip(1));
                    return WebUtility.UrlDecode(fileName);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при извлечении имени файла из URL: {fileUrl}");
                return null;
            }
        }

        /// <summary>
        /// Результат удаления файла из MinIO хранилища
        /// </summary>
        public class DeleteFileResult
        {
            public bool IsDeleted { get; set; }
            public string Message { get; set; } = string.Empty;
        }
    }

    /// <summary>
    /// Настройки для MinIO
    /// </summary>
    public class MinioSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public int Port { get; set; }
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = "uploads";
        public bool UseSSL { get; set; } = false;
    }

    
}