using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
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
                if (file == null || file.Length == 0)
                {
                    return "Ошибка: Файл не выбран или пустой";
                }

                // Проверяем размер файла (максимум 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return "Ошибка: Размер файла превышает 10MB";
                }

                // Формируем полное имя файла с расширением
                var fullFileName = $"public/{fileName}.{fileExtension.TrimStart('.')}";

                // Определяем Content-Type
                var contentType = GetContentType(fileExtension);

                // Проверяем существование bucket
                await EnsureBucketExistsAsync();

                // Загружаем файл
                using var stream = file.OpenReadStream();

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_minioSettings.BucketName)
                    .WithObject(fullFileName)
                    .WithStreamData(stream)
                    .WithObjectSize(file.Length)
                    .WithContentType(contentType);

               var response = await _minioClient.PutObjectAsync(putObjectArgs);

                // Возвращаем URL файла
                
                _logger.LogInformation($"Ответ от MinIO: Size={response.Size}, ETag={response.Etag}");
                var fileUrl = $"{_minioSettings.Endpoint}/{_minioSettings.BucketName}/{fullFileName}";

                return fileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при загрузке файла {fileName}");
                return $"Ошибка при загрузке файла: {ex.Message}";
            }
        }

        /// <summary>
        /// Удаляет файл из MinIO хранилища по URL
        /// </summary>
        public async Task<string> DeleteFileAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                {
                    return "Ошибка: URL файла не указан";
                }

                // Извлекаем имя файла из URL
                var fileName = ExtractFileNameFromUrl(fileUrl);
                if (string.IsNullOrEmpty(fileName))
                {
                    return "Ошибка: Не удалось извлечь имя файла из URL";
                }

                // Проверяем существование файла
                var exists = await FileExistsAsync(fileName);
                if (!exists)
                {
                    return "Файл не найден в хранилище";
                }

                // Удаляем файл
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(_minioSettings.BucketName)
                    .WithObject(fileName);

                await _minioClient.RemoveObjectAsync(removeObjectArgs);

                _logger.LogInformation($"Файл {fileName} успешно удален из MinIO");
                return "Файл успешно удален";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при удалении файла по URL: {fileUrl}");
                return $"Ошибка при удалении файла: {ex.Message}";
            }
        }

        /// <summary>
        /// Проверяет существование файла в хранилище
        /// </summary>
        public async Task<bool> FileExistsAsync(string fileName)
        {
            try
            {
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket(_minioSettings.BucketName)
                    .WithObject(fileName);

                await _minioClient.StatObjectAsync(statObjectArgs);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Получает URL файла для доступа
        /// </summary>
        public string GetFileUrl(string fileName)
        {
            var protocol = _minioSettings.UseSSL ? "https" : "http";
            var port = _minioSettings.Port/*.HasValue ? $":{_minioSettings.Port}" : ""*/;
            return $"{protocol}://{_minioSettings.Endpoint}{port}/{_minioSettings.BucketName}/{fileName}";
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
        /// Извлекает имя файла из URL
        /// </summary>
        private string ExtractFileNameFromUrl(string fileUrl)
        {
            try
            {
                var uri = new Uri(fileUrl);
                var segments = uri.Segments;

                // Последний сегмент должен быть именем файла
                if (segments.Length > 0)
                {
                    var fileName = segments[segments.Length - 1];
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