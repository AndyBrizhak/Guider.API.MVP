using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Guider.API.MVP.Services;

namespace Guider.API.MVP.Controllers
{
    /// <summary>
    /// Контроллер для работы с файлами
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly IMinioService _minioService;
        private readonly ILogger<FileController> _logger;

        public FileController(IMinioService minioService, ILogger<FileController> logger)
        {
            _minioService = minioService;
            _logger = logger;
        }

        /// <summary>
        /// Загружает файл в хранилище
        /// </summary>
        /// <param name="file">Файл для загрузки</param>
        /// <param name="fileName">Имя файла (без расширения)</param>
        /// <returns>URL файла или сообщение об ошибке</returns>
        //[HttpPost("upload")]
        //[Consumes("multipart/form-data")]
        //public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string fileName)
        //{
        //    try
        //    {
        //        if (file == null || file.Length == 0)
        //        {
        //            return BadRequest("Файл не выбран");
        //        }

        //        if (string.IsNullOrEmpty(fileName))
        //        {
        //            return BadRequest("Имя файла не указано");
        //        }

        //        // Получаем расширение из оригинального файла
        //        var fileExtension = Path.GetExtension(file.FileName);
        //        if (string.IsNullOrEmpty(fileExtension))
        //        {
        //            return BadRequest("Не удалось определить расширение файла");
        //        }

        //        // Генерируем уникальное имя файла если не указано
        //        var uniqueFileName = string.IsNullOrEmpty(fileName)
        //            ? Guid.NewGuid().ToString()
        //            : fileName;

        //        var result = await _minioService.UploadFileAsync(file, uniqueFileName, fileExtension);

        //        // Проверяем, является ли результат URL (успешная загрузка) или сообщением об ошибке
        //        if (result.StartsWith("http://") || result.StartsWith("https://"))
        //        {
        //            return Ok(new { success = true, url = result, message = "Файл успешно загружен" });
        //        }
        //        else
        //        {
        //            return BadRequest(new { success = false, message = result });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Ошибка при загрузке файла");
        //        return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
        //    }
        //}

        /// <summary>
        /// Удаляет файл из хранилища
        /// </summary>
        /// <param name="fileUrl">URL файла для удаления</param>
        /// <returns>Результат операции</returns>
        //[HttpDelete("delete")]
        //public async Task<IActionResult> DeleteFile([FromQuery] string fileUrl)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(fileUrl))
        //        {
        //            return BadRequest(new { success = false, message = "URL файла не указан" });
        //        }

        //        var result = await _minioService.DeleteFileAsync(fileUrl);

        //        if (result == "Файл успешно удален")
        //        {
        //            return Ok(new { success = true, message = result });
        //        }
        //        else
        //        {
        //            return BadRequest(new { success = false, message = result });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Ошибка при удалении файла");
        //        return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
        //    }
        //}

        /// <summary>
        /// Проверяет существование файла
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns>Результат проверки</returns>
        //[HttpGet("exists/{fileName}")]
        //public async Task<IActionResult> CheckFileExists(string fileName)
        //{
        //    try
        //    {
        //        var exists = await _minioService.FileExistsAsync(fileName);
        //        return Ok(new { exists = exists, fileName = fileName });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Ошибка при проверке существования файла {fileName}");
        //        return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
        //    }
        //}

        [HttpGet("exists/{fileName}")]
        public async Task<IActionResult> CheckFileExists(string fileName)
        {
            try
            {
                var exists = await _minioService.FileExistsAsync(fileName);

                if (exists)
                {
                    return Ok(new { exists = true, fileName = fileName });
                }
                else
                {
                    return NotFound(new { exists = false, fileName = fileName, message = "Файл не найден в хранилище" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при проверке существования файла {fileName}");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получает URL файла
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns>URL файла</returns>
        [HttpGet("url/{fileName}")]
        public IActionResult GetFileUrl(string fileName)
        {
            try
            {
                var url = _minioService.GetFileUrl(fileName);
                return Ok(new { url = url, fileName = fileName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении URL файла {fileName}");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }
    }
}
