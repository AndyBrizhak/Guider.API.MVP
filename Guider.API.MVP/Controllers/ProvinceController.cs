
using Guider.API.MVP.Models;
using Guider.API.MVP.Services;
using Guider.API.MVP.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json.Nodes;
using System;
using Microsoft.Extensions.Caching.Memory;

namespace Guider.API.MVP.Controllers
{
    /// <summary>
    /// Управляет провинциями (областями).
    /// </summary>
    [Route("")]// Маршруты задаются на уровне методов (напр. "provinces")
    [ApiController]
    [Produces("application/json")] // Все ответы в формате JSON
    [Tags("Provinces")] // Группировка в Swagger
    public class ProvinceController : ControllerBase
    {
        private readonly ProvinceService _provinceService;
        private readonly PlaceService _placeService;
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Инициализирует новый экземпляр контроллера ProvinceController.
        /// </summary>
        /// <param name="provinceService">Сервис для операций с провинциями.</param>
        public ProvinceController(ProvinceService provinceService, 
                                    PlaceService placeService, 
                                    IMemoryCache memoryCache)
        {
            _provinceService = provinceService;
            _placeService = placeService;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Получает постраничный список провинций (для React-Admin).
        /// </summary>
        /// <remarks>
        /// Возвращает список провинций с поддержкой пагинации и фильтрации.
        /// Включает заголовок 'X-Total-Count' в ответе для React-Admin.
        /// </remarks>
        /// <param name="q">Поисковый запрос (фильтрация по всем полям).</param>
        /// <param name="name">Фильтр по названию провинции.</param>
        /// <param name="url">Фильтр по URL провинции.</param>
        /// <param name="page">Номер страницы (по умолчанию 1).</param>
        /// <param name="perPage">Количество элементов на странице (по умолчанию 10).</param>
        /// <returns>Список объектов провинций.</returns>
        [HttpGet("provinces")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<object>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string q = null,
            [FromQuery] string name = null,
            [FromQuery] string url = null,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 10)
        {
            // Создаем объект фильтра для передачи в сервис
            var filter = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(q))
            {
                filter["q"] = q;
            }
            if (!string.IsNullOrEmpty(name))
            {
                filter["name"] = name;
            }
            if (!string.IsNullOrEmpty(url))
            {
                filter["url"] = url;
            }

            // Получаем данные с пагинацией
            var (provincesDocuments, totalCount) = await _provinceService.GetAllAsync(filter, page, perPage);

            // Transform the data format to be compatible with react-admin
            var result = new List<object>();
            foreach (var doc in provincesDocuments)
            {
                try
                {
                    // Проверяем, есть ли поле error
                    if (doc.RootElement.TryGetProperty("error", out _))
                    {
                        // Если есть ошибка, возвращаем её
                        return BadRequest(doc);
                    }
                    // Получаем ID и имя из документа
                    doc.RootElement.TryGetProperty("_id", out var idElement);
                    string id = idElement.GetProperty("$oid").GetString();
                    string docName = string.Empty;
                    if (doc.RootElement.TryGetProperty("name", out var nameElement))
                    {
                        docName = nameElement.GetString();
                    }
                    // Получаем URL из документа (если есть)
                    string docUrl = string.Empty;
                    if (doc.RootElement.TryGetProperty("url", out var urlElement))
                    {
                        docUrl = urlElement.GetString();
                    }
                    // Формируем объект в формате для react-admin
                    result.Add(new
                    {
                        id,
                        name = docName,
                        url = docUrl
                    });
                }
                catch (Exception ex)
                {
                    // В случае ошибки добавляем информацию о ней
                    return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { $"Error processing province: {ex.Message}" } });
                }
            }

            // Add total count header for react-admin pagination
            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");
            return Ok(result);
        }

        /// <summary>
        /// Получает провинцию по ID.
        /// </summary>
        /// <remarks>
        /// <br/>
        /// <b>Пример успешного ответа:</b>
        /// <br/>
        /// {
        /// <br/>
        /// &nbsp;&nbsp;"id": "60d5f1b2c1b2f0001f1b2c3d",
        /// <br/>
        /// &nbsp;&nbsp;"name": "Guanacaste",
        /// <br/>
        /// &nbsp;&nbsp;"url": "guanacaste"
        /// <br/>
        /// }
        /// </remarks>
        /// <param name="id">ID провинции (MongoDB ObjectID).</param>
        /// <returns>Объект провинции.</returns>
        [HttpGet("provinces/{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
        public async Task<IActionResult> GetById(string id)
        {
            var provinceDoc = await _provinceService.GetByIdAsync(id);

            // Проверяем, есть ли поле error
            if (provinceDoc.RootElement.TryGetProperty("error", out var errorElement))
            {
                return NotFound(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { errorElement.GetString() } });
            }

            try
            {
                // Получаем ID и имя из документа
                provinceDoc.RootElement.TryGetProperty("_id", out var idElement);
                string docId = idElement.GetProperty("$oid").GetString();

                string name = string.Empty;
                if (provinceDoc.RootElement.TryGetProperty("name", out var nameElement))
                {
                    name = nameElement.GetString();
                }

                // Получаем URL из документа (если есть)
                string url = string.Empty;
                if (provinceDoc.RootElement.TryGetProperty("url", out var urlElement))
                {
                    url = urlElement.GetString();
                }

                // Формируем объект в формате для react-admin
                var result = new
                {
                    id = docId,
                    name,
                    url
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { $"Error processing province: {ex.Message}" } });
            }
        }

        /// <summary>
        /// Создает новую провинцию.
        /// </summary>
        /// <remarks>
        /// Доступ: Super_Admin, Admin, Manager.
        /// <br/>
        /// <b>Пример тела запроса (JSON):</b>
        /// <br/>
        /// {
        /// <br/>
        /// &nbsp;&nbsp;"name": "Новая Провинция",
        /// <br/>
        /// &nbsp;&nbsp;"url": "new-province-url"
        /// <br/>
        /// }
        /// <br/>
        /// <b>Пример успешного ответа:</b> (Такой же, как в GetById)
        /// </remarks>
        /// <param name="provinceData">JSON-объект с данными провинции (поле 'name' обязательно).</param>
        /// <returns>Созданный объект провинции.</returns>
        [HttpPost("provinces")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))] // Ваш код возвращает OK, а не 201
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] JsonElement provinceData)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { "Invalid data" } });

            try
            {
                // Проверяем наличие поля name
                if (!provinceData.TryGetProperty("name", out _))
                {
                    return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { "Province name is required" } });
                }

                // Создаем новый JsonDocument для передачи в сервис
                using (var ms = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(ms))
                    {
                        writer.WriteStartObject();
                        writer.WriteString("name", provinceData.GetProperty("name").GetString());

                        // Добавляем URL если он есть
                        if (provinceData.TryGetProperty("url", out var urlElement))
                        {
                            writer.WriteString("url", urlElement.GetString());
                        }

                        writer.WriteEndObject();
                    }

                    ms.Position = 0;
                    using var provinceDoc = JsonDocument.Parse(ms.ToArray());
                    var createdProvinceDoc = await _provinceService.CreateAsync(provinceDoc);

                    // Проверяем, есть ли поле error
                    if (createdProvinceDoc.RootElement.TryGetProperty("error", out var errorElement))
                    {
                        return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { errorElement.GetString() } });
                    }

                    // Получаем ID и имя из созданного документа
                    createdProvinceDoc.RootElement.TryGetProperty("_id", out var idElement);
                    string docId = idElement.GetProperty("$oid").GetString();

                    string name = string.Empty;
                    if (createdProvinceDoc.RootElement.TryGetProperty("name", out var nameElement))
                    {
                        name = nameElement.GetString();
                    }

                    // Получаем URL из документа (если есть)
                    string url = string.Empty;
                    if (createdProvinceDoc.RootElement.TryGetProperty("url", out var docUrlElement))
                    {
                        url = docUrlElement.GetString();
                    }

                    // Формируем объект в формате для react-admin
                    var result = new
                    {
                        id = docId,
                        name,
                        url
                    };

                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { $"Error creating province: {ex.Message}" } });
            }
        }

        /// <summary>
        /// Обновляет существующую провинцию.
        /// </summary>
        /// <remarks>
        /// Доступ: Super_Admin, Admin, Manager.
        /// <br/>
        /// <b>Пример тела запроса (JSON):</b>
        /// <br/>
        /// {
        /// <br/>
        /// &nbsp;&nbsp;"name": "Обновленное Название",
        /// <br/>
        /// &nbsp;&nbsp;"url": "updated-url"
        /// <br/>
        /// }
        /// </remarks>
        /// <param name="id">ID обновляемой провинции.</param>
        /// <param name="provinceData">JSON-объект с обновленными данными (поле 'name' обязательно).</param>
        /// <returns>Обновленный объект провинции.</returns>
        [HttpPut("provinces/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
        public async Task<IActionResult> Update(string id, [FromBody] JsonElement provinceData)
        {
            try
            {
                // Проверяем наличие поля name
                if (!provinceData.TryGetProperty("name", out _))
                {
                    return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { "Province name is required" } });
                }

                // Создаем новый JsonDocument для передачи в сервис
                using (var ms = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(ms))
                    {
                        writer.WriteStartObject();
                        writer.WriteString("name", provinceData.GetProperty("name").GetString());

                        // Добавляем URL если он есть
                        if (provinceData.TryGetProperty("url", out var urlElement))
                        {
                            writer.WriteString("url", urlElement.GetString());
                        }

                        writer.WriteEndObject();
                    }

                    ms.Position = 0;
                    using var provinceDoc = JsonDocument.Parse(ms.ToArray());
                    var updatedProvinceDoc = await _provinceService.UpdateAsync(id, provinceDoc);

                    // Проверяем, есть ли поле error
                    if (updatedProvinceDoc.RootElement.TryGetProperty("error", out var errorElement))
                    {
                        var errorMessage = errorElement.GetString();
                        if (errorMessage.Contains("not exist"))
                        {
                            return NotFound(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { errorMessage } });
                        }
                        return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { errorMessage } });
                    }

                    // Получаем ID и имя из обновленного документа
                    updatedProvinceDoc.RootElement.TryGetProperty("_id", out var idElement);
                    string docId = idElement.GetProperty("$oid").GetString();

                    string name = string.Empty;
                    if (updatedProvinceDoc.RootElement.TryGetProperty("name", out var nameElement))
                    {
                        name = nameElement.GetString();
                    }

                    // Получаем URL из документа (если есть)
                    string url = string.Empty;
                    if (updatedProvinceDoc.RootElement.TryGetProperty("url", out var docUrlElement))
                    {
                        url = docUrlElement.GetString();
                    }

                    // Формируем объект в формате для react-admin
                    var result = new
                    {
                        id = docId,
                        name,
                        url
                    };

                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { $"Error updating province: {ex.Message}" } });
            }
        }

        /// <summary>
        /// Удаляет провинцию по ID.
        /// </summary>
        /// <remarks>
        /// Доступ: Super_Admin, Admin.
        /// <br/>
        /// <b>Пример успешного ответа (для React-Admin):</b>
        /// <br/>
        /// { "id": "60d5f1b2c1b2f0001f1b2c3d" }
        /// </remarks>
        /// <param name="id">ID удаляемой провинции.</param>
        /// <returns>Объект с ID удаленной провинции.</returns>
        [HttpDelete("provinces/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _provinceService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { "Province not found or could not be deleted" } });
            }

            // Return the ID for react-admin compatibility
            return Ok(new { id });
        }

        
        /// <summary>
        /// Получает список активных провинций (name + slug).
        /// </summary>
        [HttpGet("provinces/active")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<object>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<IActionResult> GetActiveProvinces([FromQuery] string category = null)
        {
            try
            {
                // Формируем уникальный ключ для конкретного запроса (учитываем категорию)
                string cacheKey = string.IsNullOrEmpty(category)
                    ? "active_provinces_all"
                    : $"active_provinces_cat_{category.ToLower()}";

                var result = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    // 1. Привязываем кеш к токену из сервиса.
                    // Если в PlaceService вызовут InvalidateProvincesCache(), эта запись удалится.
                    entry.AddExpirationToken(_placeService.GetProvincesChangeToken());

                    // 2. Ставим "страховочное" время жизни (например, 168 часа), 
                    // на случай если инвалдиация не сработает или данные нужно просто освежить.
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(168);

                    // 3. Логика получения данных
                    return await _placeService.GetActiveProvincesAsync(category);
                });

                if (result == null)
                    return StatusCode(500, new { message = "Service returned null result." });

                bool isSuccess = result.RootElement.GetProperty("success").GetBoolean();
                if (!isSuccess)
                {
                    string errorMessage = result.RootElement.GetProperty("error").GetString();
                    return StatusCode(500, new { message = errorMessage });
                }

                var provincesData = result.RootElement.GetProperty("data");
                int count = provincesData.GetArrayLength();

                Response.Headers.Add("X-Total-Count", count.ToString());
                Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");

                return Ok(provincesData);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}