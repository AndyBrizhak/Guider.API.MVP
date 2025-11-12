
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
    [Produces("application/json")] // Все ответы в формате JSON
    [Tags("Tags")] // Группировка в Swagger
    public class TagsController : ControllerBase
    {
        private readonly TagsService _tagsService;
        private readonly PlaceService _placeService;

        public TagsController(TagsService tagsService, PlaceService placeService)
        {
            _tagsService = tagsService;
            _placeService = placeService;
        }


        /// <summary>
        /// Получает постраничный список тегов (для React-Admin).
        /// </summary>
        /// <remarks>
        /// Возвращает список тегов с фильтрацией и пагинацией. 
        /// Включает заголовок 'X-Total-Count' в ответе для React-Admin.
        /// </remarks>
        /// <param name="q">Поисковый запрос (по имени или описанию, опционально).</param>
        /// <param name="name_en">Фильтр по английскому названию (опционально).</param>
        /// <param name="name_sp">Фильтр по испанскому названию (опционально).</param>
        /// <param name="url">Фильтр по URL тега (опционально).</param>
        /// <param name="type">Фильтр по типу тега (опционально).</param>
        /// <param name="page">Номер страницы (по умолчанию 1).</param>
        /// <param name="perPage">Количество элементов на странице (по умолчанию 10).</param>
        /// <param name="_sort">Поле для сортировки (по умолчанию name_en).</param>
        /// <param name="_order">Порядок сортировки: ASC или DESC (по умолчанию ASC).</param>
        /// <returns>Список объектов тегов.</returns>
        [HttpGet("tags")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<object>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        /// Получает тег по его ID.
        /// </summary>
        /// <remarks>
        /// <br/>
        /// <b>Пример успешного ответа:</b>
        /// <br/>
        /// {
        /// <br/>
        /// &nbsp;&nbsp;"id": "60d5f1b2c1b2f0001f1b2c3d",
        /// <br/>
        /// &nbsp;&nbsp;"name_en": "WiFi",
        /// <br/>
        /// &nbsp;&nbsp;"name_sp": "WiFi",
        /// <br/>
        /// &nbsp;&nbsp;"type": "amenity"
        /// <br/>
        /// }
        /// </remarks>
        /// <param name="id">Уникальный идентификатор тега (MongoDB ObjectID).</param>
        /// <returns>Возвращает объект тега.</returns>
        [HttpGet("tags/{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
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
        /// Создает новый тег.
        /// </summary>
        /// <remarks>
        /// Доступ: Super_Admin, Admin, Manager.
        /// <br/>
        /// <b>Ожидаемый формат JSON:</b>
        /// <br/>
        /// {
        /// <br/>
        /// &nbsp;&nbsp;"name_en": "string",         // (опционально)
        /// <br/>
        /// &nbsp;&nbsp;"name_sp": "string",         // (опционально)
        /// <br/>
        /// &nbsp;&nbsp;"description": "string",     // (опционально)
        /// <br/>
        /// &nbsp;&nbsp;"url": "string",             // (опционально)
        /// <br/>
        /// &nbsp;&nbsp;"type": "string"             // (опционально)
        /// <br/>
        /// }
        /// </remarks>
        /// <param name="tagData">JSON-объект с данными тега.</param>
        /// <returns>Возвращает созданный объект тега.</returns>
        [HttpPost]
        [Route("tags")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
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
        /// Обновляет существующий тег по ID.
        /// </summary>
        /// <param name="id">Уникальный идентификатор тега.</param>
        /// <param name="updateData">JSON-объект с обновляемыми полями.</param>
        /// <remarks>
        /// Доступ: Super_Admin, Admin, Manager.
        /// <br/>
        /// <b>Ожидаемый формат JSON (любое поле опционально):</b>
        /// <code>
        /// {
        ///   "name_en": "string",
        ///   "name_sp": "string",
        ///   "description": "string",
        ///   "url": "string",
        ///   "type": "string"
        /// }
        /// </code>
        /// </remarks>
        /// <returns>Возвращает обновленный объект тега.</returns>
        [HttpPut("tags/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
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
        /// Удаляет тег по ID.
        /// </summary>
        /// <remarks>
        /// Доступ: Super_Admin, Admin.
        /// <br/>
        /// <b>Пример успешного ответа (для React-Admin):</b>
        /// <br/>
        /// { "id": "60d5f1b2c1b2f0001f1b2c3d" }
        /// </remarks>
        /// <param name="id">Уникальный идентификатор тега для удаления.</param>
        /// <returns>Возвращает ID удаленного тега.</returns>
        [HttpDelete("tags/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
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

        /// <summary>
        /// Получает список активных тегов из коллекции Places с опциональной фильтрацией.
        /// </summary>
        /// <remarks>
        /// Возвращает уникальные теги, которые используются в местах в коллекции Places.
        /// Теги отсортированы по алфавиту.
        /// <br/>
        /// <br/>
        /// <b>Примеры использования:</b>
        /// <br/>
        /// - GET /tags/active - все теги
        /// <br/>
        /// - GET /tags/active?category=to-eat - теги из ресторанов
        /// <br/>
        /// - GET /tags/active?province=Guanacaste - теги из провинции Guanacaste
        /// <br/>
        /// - GET /tags/active?city=Liberia - теги из города Liberia
        /// <br/>
        /// - GET /tags/active?category=to-eat&amp;province=Guanacaste - теги из ресторанов в Guanacaste
        /// /// </remarks>
        /// <param name="category">Опциональный фильтр по категории (например, "to-eat").</param>
        /// <param name="province">Опциональный фильтр по провинции (например, "Guanacaste").</param>
        /// <param name="city">Опциональный фильтр по городу (например, "Liberia").</param>
        /// <returns>Массив названий тегов.</returns>
        [HttpGet("tags/active")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<string>))] // Успешный ответ
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))] // Ошибка сервера
        public async Task<IActionResult> GetActiveTags(
            [FromQuery] string category = null,
            [FromQuery] string province = null,
            [FromQuery] string city = null)
        {
            try
            {
                var result = await _placeService.GetActiveTagsAsync(category, province, city);

                if (result == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new { message = "Service returned null result." });
                }

                // Проверяем успешность операции
                bool isSuccess = result.RootElement.GetProperty("success").GetBoolean();

                if (!isSuccess)
                {
                    string errorMessage = result.RootElement.GetProperty("error").GetString();
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new { message = errorMessage });
                }

                // Извлекаем массив тегов
                var tagsData = result.RootElement.GetProperty("data");
                var tagsList = new List<string>();

                foreach (var tag in tagsData.EnumerateArray())
                {
                    tagsList.Add(tag.GetString());
                }

                // Добавляем заголовок с общим количеством тегов
                Response.Headers.Add("X-Total-Count", tagsList.Count.ToString());
                Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");

                // Возвращаем просто массив строк
                return Ok(tagsList);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = $"An error occurred: {ex.Message}" });
            }
        }


    }
}