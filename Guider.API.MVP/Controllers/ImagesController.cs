
using Guider.API.MVP.Models;
using Guider.API.MVP.Services;
using Guider.API.MVP.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace Guider.API.MVP.Controllers
{
    [Route("images")]
    [ApiController]
    [Consumes("multipart/form-data")]
    public class ImagesController : ControllerBase
    {
        private readonly IImageService _imageService;

        public ImagesController(IImageService imageService)
        {
            _imageService = imageService;
        }


        [HttpPost]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> UploadImage([FromForm] ImageUploadRequest request)
        {
            // Проверка входных параметров
            if (request == null ||
                string.IsNullOrEmpty(request.Province) ||
                string.IsNullOrEmpty(request.Place) ||
                string.IsNullOrEmpty(request.ImageName) ||
                request.ImageFile == null)
            {
                return BadRequest(new
                {
                    error = "Отсутствуют необходимые параметры",
                    message = "Province, Place, ImageName и ImageFile являются обязательными полями"
                });
            }

            try
            {
                // Вызываем метод сервиса
                var jsonResult = await _imageService.SaveImageAsync(
                    request.Province,
                    request.City,
                    request.Place,
                    request.ImageName,
                    request.ImageFile);

                // Проверяем результат операции
                if (jsonResult.RootElement.TryGetProperty("Success", out var successElement) &&
                    successElement.GetBoolean() == false)
                {
                    // Операция не успешна - возвращаем ошибку
                    string errorMessage = "Неизвестная ошибка при загрузке изображения";

                    if (jsonResult.RootElement.TryGetProperty("Message", out var messageElement))
                    {
                        errorMessage = messageElement.GetString() ?? errorMessage;
                    }

                    return BadRequest(new
                    {
                        error = errorMessage,
                        message = errorMessage
                    });
                }

                // Операция успешна - извлекаем данные
                string imagePath = "";
                string imageId = "";

                if (jsonResult.RootElement.TryGetProperty("Path", out var pathElement))
                {
                    imagePath = pathElement.GetString() ?? "";
                }

                if (jsonResult.RootElement.TryGetProperty("Id", out var idElement))
                {
                    imageId = idElement.GetString() ?? "";
                }

                // Возвращаем успешный результат с данными изображения
                return Ok(new
                {
                    id = imageId,
                    path = imagePath,
                    province = request.Province,
                    city = request.City,
                    place = request.Place,
                    imageName = request.ImageName,
                    originalFileName = request.ImageFile.FileName,
                    fileSize = request.ImageFile.Length,
                    contentType = request.ImageFile.ContentType,
                    uploadDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    //isActive = true
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    error = "Некорректные входные данные",
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new
                {
                    error = "Доступ запрещен",
                    message = "У вас нет прав для загрузки изображений"
                });
            }
            catch (IOException ex)
            {
                return StatusCode(507, new
                {
                    error = "Ошибка файловой системы",
                    message = $"Не удалось сохранить файл: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Внутренняя ошибка сервера",
                    message = "Произошла непредвиденная ошибка при загрузке изображения"
                });
            }
        }

        /// <summary>  
        /// Получение изображения по компонентам пути
        /// </summary>  
        /// <param name="province">Название провинции</param>
        /// <param name="city">Название города (опционально)</param>
        /// <param name="place">Название заведения</param>
        /// <param name="imageName">Имя файла изображения</param>
        /// <returns>Файл изображения</returns>  
        [HttpGet("get/province/city/place/name")]
        public IActionResult GetImage(
            [FromQuery] string province,
            [FromQuery] string? city,
            [FromQuery] string place,
            [FromQuery] string imageName)
        {
            var response = new ApiResponse();

            if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(place) || string.IsNullOrEmpty(imageName))
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Отсутствуют необходимые параметры");
                return BadRequest(response);
            }

            try
            {
                var jsonResult = _imageService.GetImage(province, city, place, imageName);

                if (jsonResult.RootElement.TryGetProperty("Success", out var successElement) &&
                    successElement.GetBoolean() == false)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.BadRequest;

                    if (jsonResult.RootElement.TryGetProperty("Message", out var messageElement))
                    {
                        response.ErrorMessages.Add(messageElement.GetString());
                    }
                    else
                    {
                        response.ErrorMessages.Add("Неизвестная ошибка при получении изображения");
                    }

                    return BadRequest(response);
                }

                if (jsonResult.RootElement.TryGetProperty("Image", out var imageElement))
                {
                    byte[] imageBytes = imageElement.GetBytesFromBase64();

                    string extension = Path.GetExtension(imageName)?.ToLowerInvariant();
                    string contentType = extension switch
                    {
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".png" => "image/png",
                        ".gif" => "image/gif",
                        ".bmp" => "image/bmp",
                        ".webp" => "image/webp",
                        _ => "application/octet-stream"
                    };

                    return File(imageBytes, contentType);
                }
                else
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.ErrorMessages.Add("Данные изображения отсутствуют в ответе");
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add($"Внутренняя ошибка сервера при получении изображения: {ex.Message}");
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Получение изображения по ID
        /// </summary>
        /// <param name="id">Идентификатор изображения</param>
        /// <returns>Файл изображения с метаданными в заголовках</returns>
        //[HttpGet("{id}")]
        //public async Task<IActionResult> GetImageById(string id)
        //{
        //    var jsonResult = await _imageService.GetImageByIdAsync(id);

        //    if (jsonResult.RootElement.TryGetProperty("Success", out var successElement) &&
        //        !successElement.GetBoolean())
        //    {
        //        string message = jsonResult.RootElement.TryGetProperty("Message", out var msgProp)
        //            ? msgProp.GetString() ?? "Ошибка"
        //            : "Ошибка";

        //        if (message.Contains("не найдено", StringComparison.OrdinalIgnoreCase))
        //            return NotFound(jsonResult.RootElement.GetRawText());

        //        if (message.Contains("Неверный формат ID", StringComparison.OrdinalIgnoreCase))
        //            return BadRequest(jsonResult.RootElement.GetRawText());

        //        return StatusCode(500, jsonResult.RootElement.GetRawText());
        //    }

        //    return Ok(jsonResult.RootElement.GetRawText());
        //}

        /// <summary>
        /// Получение метаданных изображения по ID без загрузки самого файла
        /// </summary>
        /// <param name="id">Идентификатор изображения</param>
        /// <returns>JSON объект с метаданными изображения</returns>
        [HttpGet("{id}/info")]
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
                var jsonResult = await _imageService.GetImageByIdAsync(id);

                // Проверяем успешность операции
                if (jsonResult.RootElement.TryGetProperty("Success", out var successElement) &&
                    successElement.GetBoolean() == false)
                {
                    string errorMessage = "Неизвестная ошибка при получении изображения";
                    if (jsonResult.RootElement.TryGetProperty("Message", out var messageElement))
                    {
                        errorMessage = messageElement.GetString() ?? errorMessage;
                    }

                    if (errorMessage.Contains("не найдено") || errorMessage.Contains("not found"))
                    {
                        return NotFound(new
                        {
                            error = "Изображение не найдено",
                            message = errorMessage
                        });
                    }

                    if (errorMessage.Contains("Неверный формат ID"))
                    {
                        return BadRequest(new
                        {
                            error = "Некорректный ID",
                            message = errorMessage
                        });
                    }

                    return BadRequest(new
                    {
                        error = "Ошибка получения изображения",
                        message = errorMessage
                    });
                }

                // Извлекаем только метаданные изображения
                //if (!jsonResult.RootElement.TryGetProperty("Image", out var imageInfoElement))
                //{
                //    return StatusCode(500, new
                //    {
                //        error = "Внутренняя ошибка",
                //        message = "Метаданные изображения отсутствуют в ответе сервиса"
                //    });
                //}

                // Возвращаем метаданные изображения
                //return Ok(new
                //{
                //    id = imageInfoElement.GetProperty("Id").GetString(),
                //    province = imageInfoElement.GetProperty("Province").GetString(),
                //    city = imageInfoElement.TryGetProperty("City", out var cityProp) && cityProp.ValueKind != JsonValueKind.Null
                //        ? cityProp.GetString() : null,
                //    place = imageInfoElement.GetProperty("Place").GetString(),
                //    imageName = imageInfoElement.GetProperty("ImageName").GetString(),
                //    originalFileName = imageInfoElement.GetProperty("OriginalFileName").GetString(),
                //    fileSize = imageInfoElement.GetProperty("FileSize").GetInt64(),
                //    contentType = imageInfoElement.GetProperty("ContentType").GetString(),
                //    extension = imageInfoElement.GetProperty("Extension").GetString(),
                //    uploadDate = imageInfoElement.GetProperty("UploadDate").GetDateTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                //});

                return Ok(jsonResult.RootElement.GetProperty("Image"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Внутренняя ошибка сервера",
                    message = "Произошла непредвиденная ошибка при получении метаданных изображения"
                });
            }
        }

        [HttpGet]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public IActionResult GetImagesList([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1 || pageSize < 1)
                {
                    return BadRequest(new { message = "Некорректные параметры пагинации: номер страницы и размер страницы должны быть больше 0" });
                }

                var jsonResult = _imageService.GetImagesList(page, pageSize);

                // Проверяем успешность операции в сервисе
                if (jsonResult.RootElement.TryGetProperty("Success", out var successElement) &&
                    successElement.GetBoolean() == false)
                {
                    // Извлекаем сообщение об ошибке из ответа сервиса
                    var errorMessage = "Неизвестная ошибка при получении списка изображений";
                    if (jsonResult.RootElement.TryGetProperty("Message", out var messageElement))
                    {
                        errorMessage = messageElement.GetString();
                    }

                    return BadRequest(new { message = errorMessage });
                }

                // Извлекаем данные из успешного ответа сервиса
                var totalImages = jsonResult.RootElement.GetProperty("TotalImages").GetInt32();
                var totalPages = jsonResult.RootElement.GetProperty("TotalPages").GetInt32();
                var currentPage = jsonResult.RootElement.GetProperty("CurrentPage").GetInt32();
                var pageSizeResult = jsonResult.RootElement.GetProperty("PageSize").GetInt32();

                // Правильно извлекаем массив объектов изображений
                var images = jsonResult.RootElement.GetProperty("Images").EnumerateArray()
                    .Select(imageElement => new
                    {
                        Id = imageElement.GetProperty("Id").GetString(),
                        Province = imageElement.GetProperty("Province").GetString(),
                        City = imageElement.TryGetProperty("City", out var cityProp) && cityProp.ValueKind != JsonValueKind.Null
                            ? cityProp.GetString() : null,
                        Place = imageElement.GetProperty("Place").GetString(),
                        ImageName = imageElement.GetProperty("ImageName").GetString(),
                        OriginalFileName = imageElement.GetProperty("OriginalFileName").GetString(),
                        FilePath = imageElement.GetProperty("FilePath").GetString(),
                        FileSize = imageElement.GetProperty("FileSize").GetInt64(),
                        ContentType = imageElement.GetProperty("ContentType").GetString(),
                        Extension = imageElement.GetProperty("Extension").GetString(),
                        UploadDate = imageElement.GetProperty("UploadDate").GetDateTime()
                    })
                    .ToList();

                // Добавляем данные пагинации в заголовки ответа
                Response.Headers.Add("X-Total-Count", totalImages.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());
                Response.Headers.Add("X-Current-Page", currentPage.ToString());
                Response.Headers.Add("X-Page-Size", pageSizeResult.ToString());

                // Возвращаем только массив изображений
                return Ok(images);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Внутренняя ошибка сервера при получении списка изображений: {ex.Message}" });
            }
        }


        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
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
                var deleteResult = await _imageService.DeleteImageByIdAsync(id);

                // Проверяем результат операции
                if (!deleteResult.RootElement.TryGetProperty("Success", out var successElement) ||
                    !successElement.GetBoolean())
                {
                    string errorMessage = "Неизвестная ошибка при удалении изображения";

                    if (deleteResult.RootElement.TryGetProperty("Message", out var messageElement))
                    {
                        errorMessage = messageElement.GetString() ?? errorMessage;
                    }

                    // Определяем тип ошибки для правильного HTTP статуса
                    if (errorMessage.Contains("не найдено") || errorMessage.Contains("not found"))
                    {
                        return NotFound(new { error = "Изображение не найдено", message = errorMessage });
                    }

                    if (errorMessage.Contains("Неверный формат ID"))
                    {
                        return BadRequest(new { error = "Некорректный ID", message = errorMessage });
                    }

                    if (errorMessage.Contains("Доступ запрещен"))
                    {
                        return StatusCode(403, new { error = "Доступ запрещен", message = errorMessage });
                    }

                    if (errorMessage.Contains("файловой системы"))
                    {
                        return StatusCode(500, new { error = "Ошибка файловой системы", message = errorMessage });
                    }

                    return BadRequest(new { error = "Ошибка удаления изображения", message = errorMessage });
                }

                // Возвращаем ImageInfo напрямую как JsonElement
                if (deleteResult.RootElement.TryGetProperty("ImageInfo", out var imageInfoElement))
                {
                    return Ok(imageInfoElement);
                }

                return StatusCode(500, new
                {
                    error = "Внутренняя ошибка",
                    message = "Не удалось получить информацию об удаленном изображении"
                });
            }
            catch (JsonException ex)
            {
                return StatusCode(500, new
                {
                    error = "Ошибка обработки данных",
                    message = $"Ошибка при обработке ответа сервиса: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Внутренняя ошибка сервера",
                    message = "Произошла непредвиденная ошибка при удалении изображения"
                });
            }
        }


    }
}
