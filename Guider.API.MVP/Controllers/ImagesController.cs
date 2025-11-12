


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
    [Produces("application/json")] // Указываем, что все ответы в JSON
    [Tags("Images")] // Группируем все эндпоинты в раздел "Images"
    public class ImagesController : ControllerBase
    {
        private readonly IImageService _imageService;

        public ImagesController(IImageService imageService)
        {
            _imageService = imageService;
        }

        /// <summary>
        /// Загружает изображение с метаданными.
        /// </summary>
        /// <remarks>
        /// Ожидает запрос в формате multipart/form-data со следующими полями:
        /// - **imageName** (string, required): Название изображения.
        /// - **imageFile** (file, required): Файл изображения для загрузки.
        /// - **province** (string, optional): Название провинции.
        /// - **city** (string, optional): Название города.
        /// - **place** (string, optional): Название места.
        /// - **description** (string, optional): Описание изображения.
        /// - **tags** (string, optional): Теги для изображения (через запятую).
        ///
        /// <b>Ограничения:</b>
        /// - Допустимые типы файлов: image/jpeg, image/png, image/gif, image/webp и др.
        /// - Максимальный размер файла: 10MB.
        ///
        /// <b>Пример успешного ответа (JSON):</b>
        /// {
        ///   "id": "string (ObjectID)",
        ///   "path": "string (URL)",
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
        /// <param name="request">Запрос на загрузку изображения (DTO).</param>
        /// <returns>Возвращает метаданные загруженного изображения или объект ошибки.</returns>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
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
        /// Получает метаданные изображения по его ID.
        /// </summary>
        /// <remarks>
        /// Возвращает полный JSON-объект с метаданными изображения, если оно найдено.
        /// </remarks>
        /// <param name="id">Уникальный идентификатор изображения (MongoDB ObjectID).</param>
        /// <returns>Метаданные изображения (JSON) или объект ошибки.</returns>
        [HttpGet("{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
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
        /// Удаляет изображение по его ID.
        /// </summary>
        /// <remarks>
        /// Удаляет как сам файл изображения, так и его метаданные из базы.
        /// Доступ: Super_Admin, Admin.
        /// </remarks>
        /// <param name="id">Уникальный идентификатор изображения (MongoDB ObjectID).</param>
        /// <returns>Метаданные удаленного изображения или сообщение об успехе/ошибке.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
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
        /// Получает постраничный список изображений (React-Admin).
        /// </summary>
        /// <remarks>
        /// Получает список изображений с поддержкой фильтрации, сортировки и пагинации (для React-Admin).
        /// В заголовке ответа возвращается `X-Total-Count` — общее количество найденных изображений.
        /// </remarks>
        /// <param name="q">Общий поисковый запрос (поиск по нескольким полям).</param>
        /// <param name="imageName">Фильтр по названию изображения.</param>
        /// <param name="province">Фильтр по провинции.</param>
        /// <param name="place">Фильтр по месту.</param>
        /// <param name="description">Фильтр по описанию.</param>
        /// <param name="tags">Фильтр по тегам (через запятую).</param>
        /// <param name="page">Номер страницы (по умолчанию 1).</param>
        /// <param name="perPage">Количество элементов на странице (по умолчанию 10).</param>
        /// <param name="sortField">Поле для сортировки (по умолчанию "imageName").</param>
        /// <param name="sortOrder">Направление сортировки ("ASC" или "DESC", по умолчанию "ASC").</param>
        /// <returns>Массив объектов с метаданными изображений.</returns>
        [HttpGet]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<object>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
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

        /// <summary>
        /// Обновляет метаданные и (опционально) файл изображения.
        /// </summary>
        /// <remarks>
        /// Ожидает запрос в формате multipart/form-data. Все поля необязательны.
        /// Будут обновлены только переданные значения.
        ///
        /// - **newImageName** (string, optional): Новое название изображения.
        /// - **newImageFile** (file, optional): Новый файл для замены существующего.
        /// - **province** (string, optional): Новое название провинции.
        /// - **city** (string, optional): Новое название города.
        /// - **place** (string, optional): Новое название места.
        /// - **description** (string, optional): Новое описание.
        /// - **tags** (string, optional): Новые теги (через запятую).
        ///
        /// <b>Ограничения (для newImageFile):</b>
        /// - Допустимые типы: image/jpeg, image/png, и т.д.
        /// - Максимальный размер: 10MB.
        ///
        /// Доступ: Super_Admin, Admin, Manager.
        /// </remarks>
        /// <param name="id">Уникальный идентификатор изображения для обновления.</param>
        /// <param name="request">Запрос на обновление изображения (DTO).</param>
        /// <returns>Полный объект обновленных метаданных изображения.</returns>
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<IActionResult> UpdateImage(string id, [FromForm] ImageUpdateRequest request)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new
                {
                    error = "Некорректный параметр",
                    message = "ID изображения не может быть пустым"
                });
            }

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

            // Проверка типа файла, если передан новый файл
            if (request.NewImageFile != null && request.NewImageFile.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp", "image/webp" };
                if (!allowedTypes.Contains(request.NewImageFile.ContentType?.ToLower()))
                {
                    return BadRequest(new
                    {
                        error = "Неподдерживаемый тип файла",
                        message = $"Тип файла {request.NewImageFile.ContentType} не поддерживается"
                    });
                }

                // Проверка размера файла (10MB)
                if (request.NewImageFile.Length > 10485760)
                {
                    return BadRequest(new
                    {
                        error = "Слишком большой файл",
                        message = "Размер файла не должен превышать 10MB"
                    });
                }
            }

            try
            {
                var result = await _imageService.UpdateImageAsync(
                    id,
                    request.NewImageName,
                    request.NewImageFile,
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
                        : "Неизвестная ошибка при обновлении изображения";

                    if (errorMessage.Contains("не найдено"))
                    {
                        return NotFound(new { error = "Изображение не найдено", message = errorMessage });
                    }

                    return BadRequest(new { error = errorMessage, message = errorMessage });
                }

                // Извлечение обновленных данных
                var updatedImage = result.RootElement.GetProperty("Image");

                return Ok(JsonSerializer.Deserialize<object>(updatedImage.GetRawText()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Внутренняя ошибка сервера",
                    message = "Произошла ошибка при обновлении изображения"
                });
            }
        }
    }
}
