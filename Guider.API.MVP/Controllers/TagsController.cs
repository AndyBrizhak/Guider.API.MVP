



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


        /// <summary>
        /// Retrieves a paginated and filtered list of tags.
        /// </summary>
        /// <param name="q">Search query for tag name or description (optional).</param>
        /// <param name="name_en">Filter by English tag name (optional).</param>
        /// <param name="name_sp">Filter by Spanish tag name (optional).</param>
        /// <param name="url">Filter by tag URL (optional).</param>
        /// <param name="type">Filter by tag type (optional).</param>
        /// <param name="page">Page number for pagination (default: 1).</param>
        /// <param name="perPage">Number of items per page (default: 10).</param>
        /// <param name="_sort">Field to sort by (default: name_en).</param>
        /// <param name="_order">Sort order: ASC or DESC (default: ASC).</param>
        /// <returns>Returns a list of tags and sets the X-Total-Count header for pagination.</returns>
        /// <response code="200">Returns the list of tags.</response>
        /// <response code="400">If an error occurs or invalid parameters are provided.</response>
        [HttpGet("tags")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
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



        /// <summary>
        /// Retrieves a tag by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the tag.</param>
        /// <returns>Returns the tag object if found.</returns>
        /// <response code="200">Returns the tag object.</response>
        /// <response code="400">If the tag ID is missing or invalid.</response>
        /// <response code="404">If the tag is not found.</response>
        [HttpGet("tags/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
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

        /// <summary>
        /// Creates a new tag.
        /// </summary>
        /// <param name="tagData">The JSON object containing tag data.</param>
        /// <remarks>
        /// Expected JSON format:
        /// {
        ///   "name_en": "string",         // English name of the tag (optional)
        ///   "name_sp": "string",         // Spanish name of the tag (optional)
        ///   "description": "string",     // Description of the tag (optional)
        ///   "url": "string",             // URL for the tag (optional)
        ///   "type": "string"             // Type/category of the tag (optional)
        /// }
        /// </remarks>
        /// <returns>Returns the created tag object or an error message.</returns>
        /// <response code="200">Returns the created tag object or a success message.</response>
        /// <response code="400">If the tag data is invalid or incomplete.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost]
        [Route("tags")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
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


        /// <summary>
        /// Updates an existing tag by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the tag.</param>
        /// <param name="updateData">
        /// The JSON object containing tag fields to update.
        /// <br/>
        /// Expected JSON format:
        /// <code>
        /// {
        ///   "name_en": "string",         // English name of the tag (optional)
        ///   "name_sp": "string",         // Spanish name of the tag (optional)
        ///   "description": "string",     // Description of the tag (optional)
        ///   "url": "string",             // URL for the tag (optional)
        ///   "type": "string"             // Type/category of the tag (optional)
        /// }
        /// </code>
        /// </param>
        /// <returns>Returns the updated tag object or an error message.</returns>
        /// <response code="200">Returns the updated tag object or a success message.</response>
        /// <response code="400">If the tag data is invalid or incomplete.</response>
        /// <response code="404">If the tag is not found.</response>
        [HttpPut("tags/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
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


        /// <summary>
        /// Deletes a tag by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the tag to delete.</param>
        /// <returns>Returns the deleted tag's ID if successful, or an error message if not found.</returns>
        /// <response code="200">Returns the ID of the deleted tag.</response>
        /// <response code="404">If the tag is not found or could not be deleted.</response>
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