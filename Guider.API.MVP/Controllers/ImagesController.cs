


using Guider.API.MVP.Models;
using Guider.API.MVP.Services;
using Guider.API.MVP.Utility;
using Microsoft.AspNetCore.Authorization;
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

        /// <summary>
        /// Uploads an image with metadata.
        /// </summary>
        /// <remarks>
        /// Expects multipart/form-data with the following fields:
        /// - imageName (string, required): Name of the image.
        /// - imageFile (file, required): Image file to upload.
        /// - province (string, optional): Province name.
        /// - city (string, optional): City name.
        /// - place (string, optional): Place name.
        /// - description (string, optional): Description of the image.
        /// - tags (string, optional): Tags for the image (comma-separated).
        ///
        /// Returns a JSON object with the uploaded image metadata:
        /// {
        ///   "id": "string",
        ///   "path": "string",
        ///   "imageName": "string",
        ///   "originalFileName": "string",
        ///   "fileSize": 12345,
        ///   "contentType": "image/jpeg",
        ///   "uploadDate": "2024-01-01T12:00:00.000Z",
        ///   "province": "string",
        ///   "city": "string",
        ///   "place": "string",
        ///   "description": "string",
        ///   "tags": "string"
        /// }
        /// </remarks>
        /// <param name="request">Image upload request.</param>
        /// <returns>Returns metadata of the uploaded image or error details.</returns>
        /// <response code="200">Image uploaded successfully.</response>
        /// <response code="400">Invalid input or unsupported file type.</response>
        /// <response code="500">Internal server error.</response>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
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

        
        /// <summary>
        /// Получает метаданные изображения по его идентификатору.
        /// </summary>
        /// <remarks>
        /// Возвращает объект с метаданными изображения, если изображение найдено.
        /// </remarks>
        /// <param name="id">Уникальный идентификатор изображения.</param>
        /// <returns>Метаданные изображения или сообщение об ошибке.</returns>
        /// <response code="200">Метаданные изображения успешно получены.</response>
        /// <response code="400">Некорректный идентификатор изображения.</response>
        /// <response code="404">Изображение не найдено.</response>
        /// <response code="500">Внутренняя ошибка сервера.</response>
        [HttpGet("{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
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

        /// <summary>
        /// Удаляет изображение по его идентификатору.
        /// </summary>
        /// <remarks>
        /// Удаляет изображение и связанные с ним метаданные по указанному идентификатору.
        /// </remarks>
        /// <param name="id">Уникальный идентификатор изображения.</param>
        /// <returns>Информация об удалённом изображении или сообщение об успешном удалении.</returns>
        /// <response code="200">Изображение успешно удалено.</response>
        /// <response code="400">Некорректный идентификатор изображения.</response>
        /// <response code="404">Изображение не найдено.</response>
        /// <response code="500">Внутренняя ошибка сервера.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
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


        /// <summary>
        /// Получает список изображений с поддержкой фильтрации, сортировки и пагинации.
        /// </summary>
        /// <remarks>
        /// Позволяет получить список изображений с возможностью фильтрации по различным полям, сортировки и постраничного вывода.
        ///
        /// Доступные параметры запроса:
        /// - q (string, optional): Поиск по всем полям.
        /// - imageName (string, optional): Фильтрация по названию изображения.
        /// - province (string, optional): Фильтрация по провинции.
        /// - place (string, optional): Фильтрация по месту.
        /// - description (string, optional): Фильтрация по описанию.
        /// - tags (string, optional): Фильтрация по тегам (через запятую).
        /// - page (int, optional): Номер страницы (по умолчанию 1).
        /// - perPage (int, optional): Количество элементов на странице (по умолчанию 10).
        /// - sortField (string, optional): Поле для сортировки (по умолчанию "imageName").
        /// - sortOrder (string, optional): Направление сортировки ("ASC" или "DESC", по умолчанию "ASC").
        ///
        /// В заголовке ответа возвращается X-Total-Count — общее количество найденных изображений.
        /// </remarks>
        /// <param name="q">Поисковый запрос по всем полям.</param>
        /// <param name="imageName">Название изображения для фильтрации.</param>
        /// <param name="province">Провинция для фильтрации.</param>
        /// <param name="place">Место для фильтрации.</param>
        /// <param name="description">Описание для фильтрации.</param>
        /// <param name="tags">Теги для фильтрации (через запятую).</param>
        /// <param name="page">Номер страницы (по умолчанию 1).</param>
        /// <param name="perPage">Количество элементов на странице (по умолчанию 10).</param>
        /// <param name="sortField">Поле для сортировки (по умолчанию "imageName").</param>
        /// <param name="sortOrder">Направление сортировки ("ASC" или "DESC", по умолчанию "ASC").</param>
        /// <returns>Массив изображений, соответствующих фильтру, с поддержкой пагинации.</returns>
        /// <response code="200">Список изображений успешно получен.</response>
        /// <response code="400">Ошибка в параметрах фильтрации или сортировки.</response>
        /// <response code="500">Внутренняя ошибка сервера.</response>
        [HttpGet]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
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
