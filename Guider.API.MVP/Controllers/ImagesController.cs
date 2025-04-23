using Guider.API.MVP.Models;
using Guider.API.MVP.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Guider.API.MVP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Consumes("multipart/form-data")] // Явно указываем, что контроллер работает с multipart/form-data
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
        /// <returns>Путь к сохраненному изображению</returns>  
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage([FromForm] ImageUploadRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ImagePath) || request.ImageFile == null)
            {
                return BadRequest("Отсутствуют необходимые параметры");
            }

            try
            {
                string savedPath = await _imageService.SaveImageAsync(request.ImagePath, request.ImageFile);
                return Ok(new { Path = savedPath });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Внутренняя ошибка сервера при загрузке изображения");
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
            if (string.IsNullOrEmpty(fullPath))
            {
                return BadRequest("Путь к изображению не указан");
            }

            try
            {
                byte[] imageData = _imageService.GetImage(fullPath);

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

                return File(imageData, contentType);
            }
            catch (FileNotFoundException)
            {
                return NotFound("Изображение не найдено");
            }
            catch (Exception)
            {
                return StatusCode(500, "Внутренняя ошибка сервера при получении изображения");
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
                return BadRequest("Путь к изображению не указан");
            }

            bool deleted = _imageService.DeleteImage(fullPath);

            if (deleted)
            {
                return Ok(new { Success = true, Message = "Изображение успешно удалено" });
            }
            else
            {
                return NotFound("Изображение не найдено или не может быть удалено");
            }
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
