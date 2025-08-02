using Microsoft.AspNetCore.Http;
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

        public MinioService(ILogger<MinioService> logger)
        {
            _logger = logger;

            // Инициализация настроек MinIO из переменных окружения
            _minioSettings = new MinioSettings
            {
                Endpoint = Environment.GetEnvironmentVariable("MINIOSETTINGS__ENDPOINT")
                    ?? throw new InvalidOperationException("MinIO Endpoint не настроен"),
                Port = int.TryParse(Environment.GetEnvironmentVariable("MINIOSETTINGS__PORT"), out var port) && port > 0 ? port : 0,
                AccessKey = Environment.GetEnvironmentVariable("MINIOSETTINGS__ACCESSKEY")
                    ?? throw new InvalidOperationException("MinIO AccessKey не настроен"),
                SecretKey = Environment.GetEnvironmentVariable("MINIOSETTINGS__SECRETKEY")
                    ?? throw new InvalidOperationException("MinIO SecretKey не настроен"),
                BucketName = Environment.GetEnvironmentVariable("MINIOSETTINGS__BUCKETNAME")
                    ?? throw new InvalidOperationException("MinIO BucketName не настроен"),
                UseSSL = bool.TryParse(Environment.GetEnvironmentVariable("MINIOSETTINGS__USESSL"), out var useSSL) && useSSL
            };

            // Создание MinIO клиента
            var clientBuilder = new MinioClient()
                .WithCredentials(_minioSettings.AccessKey, _minioSettings.SecretKey)
                .WithSSL(_minioSettings.UseSSL);

            if (_minioSettings.Port > 0)
                clientBuilder = clientBuilder.WithEndpoint(_minioSettings.Endpoint, _minioSettings.Port);
            else
                clientBuilder = clientBuilder.WithEndpoint(_minioSettings.Endpoint);

            _minioClient = clientBuilder.Build();

            // Инициализация bucket
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
                    return "Ошибка: Файл не выбран или пустой";

                if (file.Length > 10 * 1024 * 1024)
                    return "Ошибка: Размер файла превышает 10MB";

                var fullFileName = $"{fileName}.{fileExtension.TrimStart('.')}";

                if (await FileExistsAsync(fullFileName))
                    return "Ошибка: Файл с таким именем уже существует";

                await EnsureBucketExistsAsync();

                using var stream = file.OpenReadStream();
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_minioSettings.BucketName)
                    .WithObject(fullFileName)
                    .WithStreamData(stream)
                    .WithObjectSize(file.Length)
                    .WithContentType(GetContentType(fileExtension));

                await _minioClient.PutObjectAsync(putObjectArgs);

                return GetFileUrl(fullFileName);
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
        public async Task<DeleteFileResult> DeleteFileAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                    return new DeleteFileResult { IsDeleted = false, Message = "URL файла не указан" };

                var fileName = ExtractFileNameFromUrl(fileUrl);
                if (string.IsNullOrEmpty(fileName))
                    return new DeleteFileResult { IsDeleted = false, Message = "Не удалось извлечь имя файла из URL" };

                if (!await FileExistsAsync(fileName))
                    return new DeleteFileResult { IsDeleted = false, Message = "Файл не найден" };

                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(_minioSettings.BucketName)
                    .WithObject(fileName);

                await _minioClient.RemoveObjectAsync(removeObjectArgs);

                return new DeleteFileResult { IsDeleted = true, Message = "Файл успешно удален" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при удалении файла: {fileUrl}");
                return new DeleteFileResult { IsDeleted = false, Message = $"Ошибка при удалении: {ex.Message}" };
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

                var result = await _minioClient.StatObjectAsync(statObjectArgs);
                return result != null && result.Size > 0;
            }
            catch
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
            var portPart = _minioSettings.Port > 0 ? $":{_minioSettings.Port}" : "";
            return $"{protocol}://{_minioSettings.Endpoint}{portPart}/{_minioSettings.BucketName}/{fileName}";
        }

        private async Task EnsureBucketExistsAsync()
        {
            try
            {
                var bucketExistsArgs = new BucketExistsArgs().WithBucket(_minioSettings.BucketName);
                var bucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs);

                if (!bucketExists)
                {
                    var makeBucketArgs = new MakeBucketArgs().WithBucket(_minioSettings.BucketName);
                    await _minioClient.MakeBucketAsync(makeBucketArgs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании bucket");
                throw;
            }
        }

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
                _ => "application/octet-stream"
            };
        }

        private string ExtractFileNameFromUrl(string fileUrl)
        {
            try
            {
                var uri = new Uri(fileUrl);
                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length > 1)
                {
                    var fileName = string.Join("/", segments.Skip(1));
                    return WebUtility.UrlDecode(fileName);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public class DeleteFileResult
        {
            public bool IsDeleted { get; set; }
            public string Message { get; set; } = string.Empty;
        }
    }

    public class MinioSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public int Port { get; set; }
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public bool UseSSL { get; set; } = false;
    }
}