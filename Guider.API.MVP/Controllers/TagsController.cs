



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
    [Route("")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly TagsService _tagsService;

        public TagsController(TagsService tagsService)
        {
            _tagsService = tagsService;
        }

        [HttpGet("tags")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> GetTags(
            [FromQuery] string q = null,
            [FromQuery] string name_en = null,
            [FromQuery] string name_sp = null,
            [FromQuery] string url = null,
            [FromQuery] string type = null,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 10,
            [FromQuery] string _sort = "name_en",
            [FromQuery] string _order = "ASC")
        {
            // Создаем объект фильтра для передачи в сервис
            var filter = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(q))
            {
                filter["q"] = q;
            }

            if (!string.IsNullOrEmpty(name_en))
            {
                filter["name_en"] = name_en;
            }

            if (!string.IsNullOrEmpty(name_sp))
            {
                filter["name_sp"] = name_sp;
            }

            if (!string.IsNullOrEmpty(url))
            {
                filter["url"] = url;
            }

            if (!string.IsNullOrEmpty(type))
            {
                filter["type"] = type;
            }

            // Добавляем параметры сортировки
            filter["_sort"] = _sort;
            filter["_order"] = _order;

            // Add pagination parameters
            filter["page"] = page.ToString();
            filter["perPage"] = perPage.ToString();

            var (tags, totalCount, errorMessage) = await _tagsService.GetTagsAsync(filter);

            // Проверяем на ошибки
            if (!string.IsNullOrEmpty(errorMessage))
            {
                return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { errorMessage } });
            }

            // Add total count header for react-admin pagination
            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");

            return Ok(tags);
        }

        [HttpGet("tags/{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> GetTagById(string id)
        {
            // Проверяем, что ID не пустой
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new ApiResponse
                {
                    IsSuccess = false,
                    ErrorMessages = new List<string> { "Tag ID is required" }
                });
            }

            var (tag, errorMessage) = await _tagsService.GetTagByIdAsync(id);

            // Проверяем на ошибки
            if (!string.IsNullOrEmpty(errorMessage))
            {
                if (errorMessage == "Tag not found")
                {
                    return NotFound(new ApiResponse
                    {
                        IsSuccess = false,
                        ErrorMessages = new List<string> { errorMessage }
                    });
                }

                return BadRequest(new ApiResponse
                {
                    IsSuccess = false,
                    ErrorMessages = new List<string> { errorMessage }
                });
            }

            return Ok(tag);
        }

        [HttpPost]
        [Route("tags")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> AddTag([FromBody] JsonDocument tagData)
        {
            if (tagData == null)
            {
                return BadRequest(new { message = "Tag data cannot be null." });
            }

            // Позволяем создавать тег даже с неполными данными
            // Валидация на обязательные поля не проводится здесь, сервис сам обработает логику и вернет ошибку, если нужно
            var resultDocument = await _tagsService.AddTagAsync(tagData);

            if (resultDocument == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Service returned null result." });
            }

            bool isSuccess = resultDocument.RootElement.GetProperty("IsSuccess").GetBoolean();
            string message = resultDocument.RootElement.GetProperty("Message").GetString();

            if (isSuccess)
            {
                // Получаем полные данные о новом теге из поля Data
                if (resultDocument.RootElement.TryGetProperty("Data", out JsonElement tagDataElement))
                {
                    return Ok(tagDataElement);
                }
                else
                {
                    // Если нет данных, возвращаем только сообщение
                    return Ok(new { message });
                }
            }
            else
            {
                HttpStatusCode statusCode = message.Contains("not found")
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.BadRequest;

                var errorObj = new { message };

                return statusCode == HttpStatusCode.NotFound
                    ? NotFound(errorObj)
                    : BadRequest(errorObj);
            }
        }
        [HttpPut("tags/{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> UpdateTag(string id, [FromBody] JsonDocument updateData)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { message = "Tag ID is required." });
            }

            if (updateData == null)
            {
                return BadRequest(new { message = "Update data cannot be null." });
            }

            var resultDocument = await _tagsService.UpdateTagAsync(id, updateData);

            if (resultDocument == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Service returned null result." });
            }

            bool isSuccess = resultDocument.RootElement.GetProperty("IsSuccess").GetBoolean();
            string message = resultDocument.RootElement.GetProperty("Message").GetString();

            if (isSuccess)
            {
                if (resultDocument.RootElement.TryGetProperty("Data", out JsonElement tagDataElement))
                {
                    return Ok(tagDataElement);
                }
                else
                {
                    return Ok(new { message });
                }
            }
            else
            {
                HttpStatusCode statusCode = message.Contains("not found")
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.BadRequest;

                var errorObj = new { message };

                return statusCode == HttpStatusCode.NotFound
                    ? NotFound(errorObj)
                    : BadRequest(errorObj);
            }
        }
        [HttpDelete("tags/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _tagsService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { "Tag not found or could not be deleted" } });
            }

            // Return the ID for react-admin compatibility
            return Ok(new { id });
        }


    }
}