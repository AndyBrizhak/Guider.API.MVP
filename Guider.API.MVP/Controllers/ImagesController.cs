
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
        [HttpGet("{id}")]
        public async Task<IActionResult> GetImageById(string id)
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
                    // Извлекаем сообщение об ошибке
                    string errorMessage = "Неизвестная ошибка при получении изображения";
                    if (jsonResult.RootElement.TryGetProperty("Message", out var messageElement))
                    {
                        errorMessage = messageElement.GetString() ?? errorMessage;
                    }

                    // Определяем статус код в зависимости от типа ошибки
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

                // Извлекаем данные изображения
                if (!jsonResult.RootElement.TryGetProperty("Image", out var imageElement))
                {
                    return StatusCode(500, new
                    {
                        error = "Внутренняя ошибка",
                        message = "Данные изображения отсутствуют в ответе сервиса"
                    });
                }

                if (!jsonResult.RootElement.TryGetProperty("ImageInfo", out var imageInfoElement))
                {
                    return StatusCode(500, new
                    {
                        error = "Внутренняя ошибка",
                        message = "Метаданные изображения отсутствуют в ответе сервиса"
                    });
                }

                // Получаем байты изображения
                byte[] imageBytes = imageElement.GetBytesFromBase64();

                // Извлекаем метаданные изображения
                var imageInfo = new
                {
                    Id = imageInfoElement.GetProperty("Id").GetString(),
                    Province = imageInfoElement.GetProperty("Province").GetString(),
                    City = imageInfoElement.TryGetProperty("City", out var cityProp) && cityProp.ValueKind != JsonValueKind.Null
                        ? cityProp.GetString() : null,
                    Place = imageInfoElement.GetProperty("Place").GetString(),
                    ImageName = imageInfoElement.GetProperty("ImageName").GetString(),
                    OriginalFileName = imageInfoElement.GetProperty("OriginalFileName").GetString(),
                    FileSize = imageInfoElement.GetProperty("FileSize").GetInt64(),
                    ContentType = imageInfoElement.GetProperty("ContentType").GetString(),
                    Extension = imageInfoElement.GetProperty("Extension").GetString(),
                    UploadDate = imageInfoElement.GetProperty("UploadDate").GetDateTime()
                };

                // Добавляем метаданные в заголовки ответа
                Response.Headers.Add("X-Image-Id", imageInfo.Id);
                Response.Headers.Add("X-Image-Province", imageInfo.Province);
                if (!string.IsNullOrEmpty(imageInfo.City))
                    Response.Headers.Add("X-Image-City", imageInfo.City);
                Response.Headers.Add("X-Image-Place", imageInfo.Place);
                Response.Headers.Add("X-Image-Name", imageInfo.ImageName);
                Response.Headers.Add("X-Original-Filename", imageInfo.OriginalFileName);
                Response.Headers.Add("X-File-Size", imageInfo.FileSize.ToString());
                Response.Headers.Add("X-Upload-Date", imageInfo.UploadDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

                // Возвращаем файл изображения
                return File(imageBytes, imageInfo.ContentType, imageInfo.OriginalFileName);
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
                    message = "У вас нет прав для получения этого изображения"
                });
            }
            catch (IOException ex)
            {
                return StatusCode(500, new
                {
                    error = "Ошибка файловой системы",
                    message = $"Не удалось прочитать файл: {ex.Message}"
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
                    message = "Произошла непредвиденная ошибка при получении изображения"
                });
            }
        }

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
                if (!jsonResult.RootElement.TryGetProperty("ImageInfo", out var imageInfoElement))
                {
                    return StatusCode(500, new
                    {
                        error = "Внутренняя ошибка",
                        message = "Метаданные изображения отсутствуют в ответе сервиса"
                    });
                }

                // Возвращаем метаданные изображения
                return Ok(new
                {
                    id = imageInfoElement.GetProperty("Id").GetString(),
                    province = imageInfoElement.GetProperty("Province").GetString(),
                    city = imageInfoElement.TryGetProperty("City", out var cityProp) && cityProp.ValueKind != JsonValueKind.Null
                        ? cityProp.GetString() : null,
                    place = imageInfoElement.GetProperty("Place").GetString(),
                    imageName = imageInfoElement.GetProperty("ImageName").GetString(),
                    originalFileName = imageInfoElement.GetProperty("OriginalFileName").GetString(),
                    fileSize = imageInfoElement.GetProperty("FileSize").GetInt64(),
                    contentType = imageInfoElement.GetProperty("ContentType").GetString(),
                    extension = imageInfoElement.GetProperty("Extension").GetString(),
                    uploadDate = imageInfoElement.GetProperty("UploadDate").GetDateTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                });
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

        /// <summary>
        /// Удаление изображения по ID
        /// </summary>
        /// <param name="id">Идентификатор изображения</param>
        /// <returns>JSON объект с информацией об удаленном изображении</returns>
        [HttpDelete("{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
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
                // Сначала получаем информацию об изображении для возврата после удаления
                var imageInfoResult = await _imageService.GetImageByIdAsync(id);

                // Проверяем, что изображение существует
                if (imageInfoResult.RootElement.TryGetProperty("Success", out var infoSuccessElement) &&
                    infoSuccessElement.GetBoolean() == false)
                {
                    string errorMessage = "Неизвестная ошибка при поиске изображения";
                    if (imageInfoResult.RootElement.TryGetProperty("Message", out var infoMessageElement))
                    {
                        errorMessage = infoMessageElement.GetString() ?? errorMessage;
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
                        error = "Ошибка поиска изображения",
                        message = errorMessage
                    });
                }

                // Извлекаем информацию об изображении перед удалением
                if (!imageInfoResult.RootElement.TryGetProperty("ImageInfo", out var imageInfoElement))
                {
                    return StatusCode(500, new
                    {
                        error = "Внутренняя ошибка",
                        message = "Не удалось получить информацию об изображении перед удалением"
                    });
                }

                // Сохраняем информацию об изображении для возврата
                var deletedImageInfo = new
                {
                    id = imageInfoElement.GetProperty("Id").GetString(),
                    province = imageInfoElement.GetProperty("Province").GetString(),
                    city = imageInfoElement.TryGetProperty("City", out var cityProp) && cityProp.ValueKind != JsonValueKind.Null
                        ? cityProp.GetString() : null,
                    place = imageInfoElement.GetProperty("Place").GetString(),
                    imageName = imageInfoElement.GetProperty("ImageName").GetString(),
                    originalFileName = imageInfoElement.GetProperty("OriginalFileName").GetString(),
                    fileSize = imageInfoElement.GetProperty("FileSize").GetInt64(),
                    contentType = imageInfoElement.GetProperty("ContentType").GetString(),
                    extension = imageInfoElement.GetProperty("Extension").GetString(),
                    uploadDate = imageInfoElement.GetProperty("UploadDate").GetDateTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    deletedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                // Выполняем удаление
                var deleteResult = await _imageService.DeleteImageByIdAsync(id);

                // Проверяем результат удаления
                if (deleteResult.RootElement.TryGetProperty("Success", out var deleteSuccessElement) &&
                    deleteSuccessElement.GetBoolean() == false)
                {
                    string errorMessage = "Неизвестная ошибка при удалении изображения";
                    if (deleteResult.RootElement.TryGetProperty("Message", out var deleteMessageElement))
                    {
                        errorMessage = deleteMessageElement.GetString() ?? errorMessage;
                    }

                    return BadRequest(new
                    {
                        error = "Ошибка удаления изображения",
                        message = errorMessage
                    });
                }

                // Возвращаем информацию об удаленном изображении (совместимо с react-admin)
                return Ok(deletedImageInfo);
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
                    message = "У вас нет прав для удаления этого изображения"
                });
            }
            catch (IOException ex)
            {
                return StatusCode(500, new
                {
                    error = "Ошибка файловой системы",
                    message = $"Не удалось удалить файл: {ex.Message}"
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
