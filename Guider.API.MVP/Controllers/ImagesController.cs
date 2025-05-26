
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


        [HttpPost("upload")]
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
                    isActive = true
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
        [HttpGet("get")]
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
        /// Получение списка изображений с разбивкой на страницы
        /// </summary>
        /// <param name="page">Номер страницы (начиная с 1)</param>
        /// <param name="pageSize">Размер страницы (количество изображений)</param>
        /// <returns>Список изображений с информацией о постраничной навигации</returns>
        [HttpGet("list")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public IActionResult GetImagesList([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var response = new ApiResponse();

            try
            {
                if (page < 1 || pageSize < 1)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.ErrorMessages.Add("Некорректные параметры пагинации: номер страницы и размер страницы должны быть больше 0");
                    return BadRequest(response);
                }

                var jsonResult = _imageService.GetImagesList(page, pageSize);

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
                        response.ErrorMessages.Add("Неизвестная ошибка при получении списка изображений");
                    }
                    return BadRequest(response);
                }

                // Создаем результирующий объект с данными пагинации и списком изображений
                var result = new
                {
                    TotalImages = jsonResult.RootElement.GetProperty("TotalImages").GetInt32(),
                    TotalPages = jsonResult.RootElement.GetProperty("TotalPages").GetInt32(),
                    CurrentPage = jsonResult.RootElement.GetProperty("CurrentPage").GetInt32(),
                    PageSize = jsonResult.RootElement.GetProperty("PageSize").GetInt32(),
                    Images = jsonResult.RootElement.GetProperty("Images").EnumerateArray()
                        .Select(image => image.GetString())
                        .ToList()
                };

                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add($"Внутренняя ошибка сервера при получении списка изображений: {ex.Message}");
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Обновление изображения
        /// </summary>
        /// <param name="request">Запрос на обновление изображения</param>
        /// <returns>Объект ApiResponse с путем к обновленному изображению или ошибками</returns>
        //[HttpPut("update")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        //public async Task<IActionResult> UpdateImage([FromForm] ImageUpdateRequest request)
        //{
        //    var response = new ApiResponse();
        //    if (request == null ||
        //        string.IsNullOrEmpty(request.OldProvince) ||
        //        string.IsNullOrEmpty(request.OldPlace) ||
        //        string.IsNullOrEmpty(request.OldImageName))
        //    {
        //        response.IsSuccess = false;
        //        response.StatusCode = HttpStatusCode.BadRequest;
        //        response.ErrorMessages.Add("Отсутствуют необходимые параметры исходного изображения");
        //        return BadRequest(response);
        //    }

        //    try
        //    {
        //        // Вызываем метод сервиса с разбитыми параметрами
        //        var jsonResult = await _imageService.UpdateImageAsync(
        //            request.OldProvince,
        //            request.OldCity,
        //            request.OldPlace,
        //            request.OldImageName,
        //            request.NewProvince,
        //            request.NewCity,
        //            request.NewPlace,
        //            request.NewImageName,
        //            request.NewImageFile);

        //        // Проверяем, успешно ли выполнена операция
        //        if (jsonResult.RootElement.TryGetProperty("Success", out var successElement) &&
        //            successElement.GetBoolean() == false)
        //        {
        //            // Операция не успешна, извлекаем сообщение об ошибке
        //            response.IsSuccess = false;
        //            response.StatusCode = HttpStatusCode.BadRequest;
        //            if (jsonResult.RootElement.TryGetProperty("Message", out var messageElement))
        //            {
        //                response.ErrorMessages.Add(messageElement.GetString());
        //            }
        //            else
        //            {
        //                response.ErrorMessages.Add("Неизвестная ошибка при обновлении изображения");
        //            }
        //            return BadRequest(response);
        //        }

        //        // Операция успешна, извлекаем путь к обновленному изображению
        //        string imagePath = "";
        //        if (jsonResult.RootElement.TryGetProperty("Path", out var pathElement))
        //        {
        //            imagePath = pathElement.GetString();
        //        }

        //        response.StatusCode = HttpStatusCode.OK;
        //        response.Result = new { Path = imagePath };
        //        return Ok(response);
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        response.IsSuccess = false;
        //        response.StatusCode = HttpStatusCode.BadRequest;
        //        response.ErrorMessages.Add(ex.Message);
        //        return BadRequest(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        response.IsSuccess = false;
        //        response.StatusCode = HttpStatusCode.InternalServerError;
        //        response.ErrorMessages.Add("Внутренняя ошибка сервера при обновлении изображения");
        //        response.ErrorMessages.Add(ex.Message);
        //        return StatusCode(500, response);
        //    }
        //}

        /// <summary>
        /// Удаление изображения
        /// </summary>
        /// <param name="request">Запрос на удаление изображения</param>
        /// <returns>Объект ApiResponse с результатом операции удаления</returns>
        [HttpDelete("delete")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public IActionResult DeleteImage([FromQuery] ImageDeleteRequest request)
        {
            var response = new ApiResponse();
            if (request == null ||
                string.IsNullOrEmpty(request.Province) ||
                string.IsNullOrEmpty(request.Place) ||
                string.IsNullOrEmpty(request.ImageName))
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Отсутствуют необходимые параметры для удаления изображения");
                return BadRequest(response);
            }

            try
            {
                // Вызываем метод сервиса с разбитыми параметрами
                var jsonResult = _imageService.DeleteImage(
                    request.Province,
                    request.City,
                    request.Place,
                    request.ImageName);

                // Проверяем, успешно ли выполнена операция
                if (jsonResult.RootElement.TryGetProperty("Success", out var successElement) &&
                    successElement.GetBoolean() == false)
                {
                    // Операция не успешна, извлекаем сообщение об ошибке
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.BadRequest;
                    if (jsonResult.RootElement.TryGetProperty("Message", out var message))
                    {
                        response.ErrorMessages.Add(message.GetString());
                    }
                    else
                    {
                        response.ErrorMessages.Add("Неизвестная ошибка при удалении изображения");
                    }
                    return BadRequest(response);
                }

                // Операция успешна
                response.StatusCode = HttpStatusCode.OK;
                if (jsonResult.RootElement.TryGetProperty("Message", out var messageElement))
                {
                    response.Result = new { Message = messageElement.GetString() };
                }
                else
                {
                    response.Result = new { Message = "Изображение успешно удалено" };
                }

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("Внутренняя ошибка сервера при удалении изображения");
                response.ErrorMessages.Add(ex.Message);
                return StatusCode(500, response);
            }
        }

    }
}
