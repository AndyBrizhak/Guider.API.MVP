using Guider.API.MVP.Models;
using Guider.API.MVP.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace Guider.API.MVP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Consumes("multipart/form-data")] 
    public class ImagesController : ControllerBase
    {
        private readonly IImageService _imageService;

        public ImagesController(IImageService imageService)
        {
            _imageService = imageService;
        }

       
        /// <summary>  
        /// Загрузка изображения  
        /// </summary>  
        /// <param name="request">Запрос на загрузку изображения</param>  
        /// <returns>Объект ApiResponse с путем к сохраненному изображению или ошибками</returns>  
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage([FromForm] ImageUploadRequest request)
        {
            var response = new ApiResponse();

            if (request == null || string.IsNullOrEmpty(request.ImagePath) || request.ImageFile == null)
            {
                response.IsSuccess = false;
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Отсутствуют необходимые параметры");
                return BadRequest(response);
            }

            try
            {
                 // Получаем JsonDocument из сервиса
                var jsonResult = await _imageService.SaveImageAsync(request.ImagePath, request.ImageFile);

                // Проверяем, успешно ли выполнена операция
                if (jsonResult.RootElement.TryGetProperty("Success", out var successElement) &&
                    successElement.GetBoolean() == false)
                {
                    // Операция не успешна, извлекаем сообщение об ошибке
                    response.IsSuccess = false;
                    response.StatusCode = System.Net.HttpStatusCode.BadRequest;

                    if (jsonResult.RootElement.TryGetProperty("Message", out var messageElement))
                    {
                        response.ErrorMessages.Add(messageElement.GetString());
                    }
                    else
                    {
                        response.ErrorMessages.Add("Неизвестная ошибка при загрузке изображения");
                    }

                    return BadRequest(response);
                }

                // Операция успешна, извлекаем путь к изображению
                string imagePath = "";
                if (jsonResult.RootElement.TryGetProperty("Path", out var pathElement))
                {
                    imagePath = pathElement.GetString();
                }

                response.StatusCode = System.Net.HttpStatusCode.OK;
                response.Result = new { Path = imagePath };

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("Внутренняя ошибка сервера при загрузке изображения");
                response.ErrorMessages.Add(ex.Message);
                return StatusCode(500, response);
            }
        }

        /// <summary>  
        /// Получение изображения по полному пути  
        /// </summary>  
        /// <param name="fullPath">Полный путь к изображению</param>  
        /// <returns>Файл изображения</returns>  
        [HttpGet("get")]
        public IActionResult GetImage([FromQuery] string fullPath)
        {
            var response = new ApiResponse();
            if (string.IsNullOrEmpty(fullPath))
            {
                response.IsSuccess = false;
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Отсутствуют необходимые параметры");
                return BadRequest(response);
            }

            try
            {
                var jsonResult = _imageService.GetImage(fullPath);

                if (jsonResult.RootElement.TryGetProperty("Success", out var successElement) &&
                    successElement.GetBoolean() == false)
                {
                    response.IsSuccess = false;
                    response.StatusCode = System.Net.HttpStatusCode.BadRequest;

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

                    string extension = Path.GetExtension(fullPath)?.ToLowerInvariant();
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
                    response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    response.ErrorMessages.Add("Данные изображения отсутствуют в ответе");
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add($"Внутренняя ошибка сервера при получении изображения: {ex.Message}");
                return StatusCode(500, response);
            }
        }


        /// <summary>  
        /// Удаление изображения по полному пути  
        /// </summary>  
        /// <param name="fullPath">Полный путь к изображению</param>  
        /// <returns>Результат операции удаления</returns>  
        [HttpDelete("delete")]
        public IActionResult DeleteImage([FromQuery] string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return BadRequest(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessages = new List<string> { "Путь к изображению не указан" }
                });
            }

            JsonDocument result = _imageService.DeleteImage(fullPath);
            // Извлекаем информацию из JsonDocument
            bool success = result.RootElement.GetProperty("Success").GetBoolean();
            string message = result.RootElement.GetProperty("Message").GetString() ?? string.Empty;

            ApiResponse response = new ApiResponse
            {
                IsSuccess = success,
                StatusCode = success ? HttpStatusCode.OK : HttpStatusCode.BadRequest
            };

            if (success)
            {
                response.Result = new { Message = message };
            }
            else
            {
                response.ErrorMessages.Add(message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>  
        /// Обновление существующего изображения  
        /// </summary>  
        /// <param name="request">Запрос на обновление изображения</param>  
        /// <returns>Результат операции обновления</returns>  
        [HttpPut("update")]
        public async Task<IActionResult> UpdateImage([FromForm] ImageUpdateRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ImagePath) || request.ImageFile == null)
            {
                return BadRequest("Отсутствуют необходимые параметры");
            }

            try
            {
                string updatedPath = await _imageService.UpdateImageAsync(request.ImagePath, request.ImageFile, request.CreateIfNotExists);
                return Ok(new
                {
                    Path = updatedPath,
                    IsCreated = !request.CreateIfNotExists
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Внутренняя ошибка сервера при обновлении изображения");
            }
        }

        /// <summary>  
        /// Замена существующего изображения  
        /// </summary>  
        /// <param name="fullPath">Полный путь к существующему изображению</param>  
        /// <param name="imageFile">Новый файл изображения</param>  
        /// <returns>Результат операции замены</returns>  
        //[HttpPatch("replace")]
        //public async Task<IActionResult> ReplaceImage([FromQuery] string fullPath, [FromForm] IFormFile imageFile)
        //{
        //    if (string.IsNullOrEmpty(fullPath) || imageFile == null)
        //    {
        //        return BadRequest("Отсутствуют необходимые параметры");
        //    }

        //    // Removed the incorrect reference to _baseImagePath
        //    if (!System.IO.File.Exists(fullPath))
        //    {
        //        return NotFound($"Изображение не найдено по пути: {fullPath}");
        //    }

        //    try
        //    {
        //        System.IO.File.Delete(fullPath);

        //        string directory = Path.GetDirectoryName(fullPath);
        //        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        //        {
        //            Directory.CreateDirectory(directory);
        //        }

        //        using (var fileStream = new FileStream(fullPath, FileMode.Create))
        //        {
        //            await imageFile.CopyToAsync(fileStream);
        //        }

        //        return Ok(new
        //        {
        //            Path = fullPath,
        //            Message = "Изображение успешно заменено"
        //        });
        //    }
        //    catch (Exception)
        //    {
        //        return StatusCode(500, "Внутренняя ошибка сервера при замене изображения");
        //    }
        //}
    }
}
