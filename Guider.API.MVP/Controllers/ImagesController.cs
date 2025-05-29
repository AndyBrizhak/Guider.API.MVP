


using Guider.API.MVP.Models;
using Guider.API.MVP.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Guider.API.MVP.Controllers
{
    [Route("images")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly IImageService _imageService;

        public ImagesController(IImageService imageService)
        {
            _imageService = imageService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage([FromForm] ImageUploadRequest request)
        {
            // Валидация модели
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    error = "Некорректные данные",
                    message = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage))
                });
            }

            // Дополнительная проверка типа файла
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp", "image/webp" };
            if (!allowedTypes.Contains(request.ImageFile.ContentType?.ToLower()))
            {
                return BadRequest(new
                {
                    error = "Неподдерживаемый тип файла",
                    message = $"Тип файла {request.ImageFile.ContentType} не поддерживается"
                });
            }

            // Проверка размера файла (10MB)
            if (request.ImageFile.Length > 10485760)
            {
                return BadRequest(new
                {
                    error = "Слишком большой файл",
                    message = "Размер файла не должен превышать 10MB"
                });
            }

            try
            {
                var result = await _imageService.SaveImageAsync(
                    request.ImageName,
                    request.ImageFile,
                    request.Province,
                    request.City,
                    request.Place,
                    request.Description,
                    request.Tags);

                if (result.RootElement.TryGetProperty("Success", out var successElement) &&
                    successElement.GetBoolean() == false)
                {
                    var errorMessage = result.RootElement.TryGetProperty("Message", out var messageElement)
                        ? messageElement.GetString()
                        : "Неизвестная ошибка при загрузке изображения";

                    return BadRequest(new { error = errorMessage, message = errorMessage });
                }

                // Извлечение данных успешного результата
                var imagePath = result.RootElement.TryGetProperty("Path", out var pathElement)
                    ? pathElement.GetString() : "";
                var imageId = result.RootElement.TryGetProperty("Id", out var idElement)
                    ? idElement.GetString() : "";

                return Ok(new
                {
                    id = imageId,
                    path = imagePath,
                    imageName = request.ImageName,
                    originalFileName = request.ImageFile.FileName,
                    fileSize = request.ImageFile.Length,
                    contentType = request.ImageFile.ContentType,
                    uploadDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    province = request.Province,
                    city = request.City,
                    place = request.Place,
                    description = request.Description,
                    tags = request.Tags
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Внутренняя ошибка сервера",
                    message = "Произошла ошибка при загрузке изображения"
                });
            }
        }

        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetImageInfoById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new
                {
                    error = "Некорректный параметр",
                    message = "ID изображения не может быть пустым"
                });
            }

            try
            {
                var result = await _imageService.GetImageByIdAsync(id);

                if (result.RootElement.TryGetProperty("Success", out var successElement) &&
                    successElement.GetBoolean() == false)
                {
                    var errorMessage = result.RootElement.TryGetProperty("Message", out var messageElement)
                        ? messageElement.GetString()
                        : "Неизвестная ошибка при получении изображения";

                    if (errorMessage.Contains("не найдено"))
                    {
                        return NotFound(new { error = "Изображение не найдено", message = errorMessage });
                    }

                    return BadRequest(new { error = "Ошибка получения изображения", message = errorMessage });
                }

                return Ok(result.RootElement.GetProperty("Image"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Внутренняя ошибка сервера",
                    message = "Произошла ошибка при получении метаданных изображения"
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteImageById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new
                {
                    error = "Некорректный параметр",
                    message = "ID изображения не может быть пустым"
                });
            }

            try
            {
                var result = await _imageService.DeleteImageByIdAsync(id);

                if (!result.RootElement.TryGetProperty("Success", out var successElement) ||
                    !successElement.GetBoolean())
                {
                    var errorMessage = result.RootElement.TryGetProperty("Message", out var messageElement)
                        ? messageElement.GetString()
                        : "Неизвестная ошибка при удалении изображения";

                    if (errorMessage.Contains("не найдено"))
                    {
                        return NotFound(new { error = "Изображение не найдено", message = errorMessage });
                    }

                    return BadRequest(new { error = "Ошибка удаления изображения", message = errorMessage });
                }

                if (result.RootElement.TryGetProperty("ImageInfo", out var imageInfoElement))
                {
                    return Ok(imageInfoElement);
                }

                return Ok(new { message = "Изображение успешно удалено" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Внутренняя ошибка сервера",
                    message = "Произошла ошибка при удалении изображения"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetImages(
            [FromQuery] string q = null,
            [FromQuery] string imageName = null,
            [FromQuery] string province = null,
            [FromQuery] string place = null,
            [FromQuery] string description = null,
            [FromQuery] string tags = null,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 10,
            [FromQuery] string sortField = "imageName",
            [FromQuery] string sortOrder = "ASC")
        {
            var filter = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(q)) filter["q"] = q;
            if (!string.IsNullOrEmpty(imageName)) filter["imageName"] = imageName;
            if (!string.IsNullOrEmpty(province)) filter["province"] = province;
            if (!string.IsNullOrEmpty(place)) filter["place"] = place;
            if (!string.IsNullOrEmpty(description)) filter["description"] = description;
            if (!string.IsNullOrEmpty(tags)) filter["tags"] = tags;

            filter["_sort"] = sortField;
            filter["_order"] = sortOrder;
            filter["page"] = page.ToString();
            filter["perPage"] = perPage.ToString();

            try
            {
                var result = await _imageService.GetImagesAsync(filter);

                if (result.RootElement.TryGetProperty("success", out var successElement) &&
                    successElement.GetBoolean())
                {
                    var dataElement = result.RootElement.GetProperty("data");
                    var totalCount = dataElement.GetProperty("totalCount").GetInt64();
                    var imagesElement = dataElement.GetProperty("images");

                    Response.Headers.Add("X-Total-Count", totalCount.ToString());
                    Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");

                    var imagesArray = JsonSerializer.Deserialize<object[]>(imagesElement.GetRawText());
                    return Ok(imagesArray);
                }
                else
                {
                    var errorMessage = result.RootElement.GetProperty("error").GetString();
                    return BadRequest(new { error = errorMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении списка изображений: {ex.Message}" });
            }
        }
    }
}
