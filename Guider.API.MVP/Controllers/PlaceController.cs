using Guider.API.MVP.Models;
using Guider.API.MVP.Services;
using Guider.API.MVP.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Guider.API.MVP.Controllers
{
    [Route("places")]
    [ApiController]
    [Produces("application/json")] // Указываем, что все ответы в JSON
    [Tags("Places")] // Группируем все эндпоинты в Swagger
    public class PlaceController : ControllerBase
    {
        private readonly PlaceService _placeService;
        private ApiResponse _response;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        private readonly IMemoryCache _memoryCache;
        private const string SITEMAP_CACHE_KEY = "sitemap_slugs_list"; // Тот же ключ, что в SitemapController


        public PlaceController(PlaceService placeService,
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration,
            IMemoryCache memoryCache 
            )
        {
            _placeService = placeService;

            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _memoryCache = memoryCache;

            _response = new ApiResponse();
        }

        
        /// <summary>
        /// Получить список мест с возможностью фильтрации, сортировки и пагинации.
        /// </summary>
        /// <remarks>
        /// Пример запроса:
        ///
        ///     GET /places?q=кафе&amp;province=Guanacaste&amp;city=Nicoya&amp;name=Coffee%20House&amp;url=coffee-house-nicoya
        ///     &amp;page=1&amp;perPage=20&amp;sortField=name&amp;sortOrder=ASC
        ///
        /// Ожидаемый ответ (пример):
        ///
        ///     [
        ///         {
        ///             "id": "664b1e2f8f1b2c001e3e4a1a",
        ///             "name": "Coffee House",
        ///             "province": "Guanacaste",
        ///             "city": "Nicoya",
        ///             "url": "coffee-house-nicoya",
        ///             "address": "Main street, Nicoya",
        ///             "tags": ["кофе", "завтрак", "WiFi"],
        ///             "location": { "lat": 10.139, "lng": -85.452 },
        ///             "img_link": "https://example.com/image.jpg"
        ///         },
        ///         ...
        ///     ]
        ///
        /// Заголовки ответа:
        /// - X-Total-Count: Общее количество найденных мест
        /// - Access-Control-Expose-Headers: X-Total-Count
        /// </remarks>
        /// <param name="q">Текстовый поиск по названию, описанию и тегам</param>
        /// <param name="province">Фильтр по провинции</param>
        /// <param name="city">Фильтр по городу</param>
        /// <param name="name">Фильтр по имени</param>
        /// <param name="url">Фильтр по url</param>
        /// <param name="page">Номер страницы (по умолчанию 1)</param>
        /// <param name="perPage">Количество элементов на странице (по умолчанию 20)</param>
        /// <param name="sortField">Поле для сортировки (по умолчанию "name")</param>
        /// <param name="sortOrder">Порядок сортировки: ASC или DESC (по умолчанию ASC)</param>
        /// <returns>Массив объектов мест</returns>
        [HttpGet]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<object>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<IActionResult> GetPlaces(
        [FromQuery] string q = null,
        [FromQuery] string province = null,
        [FromQuery] string city = null,
        [FromQuery] string name = null,
        [FromQuery] string url = null,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20,
        [FromQuery] string sortField = "name",
        [FromQuery] string sortOrder = "ASC")
        {
            var filter = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(q)) filter["q"] = q;
            if (!string.IsNullOrEmpty(province)) filter["province"] = province;
            if (!string.IsNullOrEmpty(city)) filter["city"] = city;
            if (!string.IsNullOrEmpty(name)) filter["name"] = name;
            if (!string.IsNullOrEmpty(url)) filter["url"] = url;
            filter["_sort"] = sortField;
            filter["_order"] = sortOrder;
            filter["page"] = page.ToString();
            filter["perPage"] = perPage.ToString();

            try
            {
                var result = await _placeService.GetPlacesAsync(filter);
                if (result.RootElement.TryGetProperty("success", out var successElement) &&
                    successElement.GetBoolean())
                {
                    var dataElement = result.RootElement.GetProperty("data");
                    var totalCount = dataElement.GetProperty("totalCount").GetInt64();
                    var placesElement = dataElement.GetProperty("places");

                    Response.Headers.Add("X-Total-Count", totalCount.ToString());
                    Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");

                    var placesArray = JsonSerializer.Deserialize<object[]>(placesElement.GetRawText());
                    return Ok(placesArray);
                }
                else
                {
                    var errorMessage = result.RootElement.GetProperty("error").GetString();
                    return BadRequest(new { error = errorMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении списка мест: {ex.Message}" });
            }
        }

       
        /// <summary>
        /// Получить место по идентификатору.
        /// </summary>
        /// <remarks>
        /// Запрос возвращает объект места по его уникальному идентификатору.
        /// 
        /// Пример запроса:
        ///
        ///     GET /places/664b1e2f8f1b2c001e3e4a1a
        ///
        /// Пример успешного ответа (код 200):
        ///
        ///     {
        ///         "id": "664b1e2f8f1b2c001e3e4a1a",
        ///         "name": "Coffee House",
        ///         "province": "Guanacaste",
        ///         "city": "Nicoya",
        ///         "url": "coffee-house-nicoya",
        ///         "address": "Main street, Nicoya",
        ///         "tags": ["кофе", "завтрак", "WiFi"],
        ///         "location": { "lat": 10.139, "lng": -85.452 },
        ///         "img_link": "https://example.com/image.jpg"
        ///     }
        ///
        /// Пример ответа, если место не найдено (код 404):
        ///
        ///     {
        ///         "message": "Place with id 664b1e2f8f1b2c001e3e4a1a not found."
        ///     }
        ///
        /// Пример ответа при ошибке формата идентификатора (код 400):
        ///
        ///     {
        ///         "message": "Invalid id format."
        ///     }
        ///
        /// Пример ответа при внутренней ошибке сервера (код 500):
        ///
        ///     {
        ///         "message": "An error occurred"
        ///     }
        /// </remarks>
        /// <param name="id">Уникальный идентификатор места (строка, 24 символа)</param>
        /// <returns>Объект места или сообщение об ошибке</returns>
        [HttpGet("{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<ActionResult> GetById(string id)
        {
            var result = await _placeService.GetByIdAsync(id);

            // Проверяем, является ли результат ошибкой
            if (result.RootElement.TryGetProperty("IsSuccess", out var isSuccessElement) &&
                isSuccessElement.GetBoolean() == false)
            {
                // Получаем сообщение об ошибке
                var errorMessage = "An error occurred";
                if (result.RootElement.TryGetProperty("Message", out var messageElement))
                {
                    errorMessage = messageElement.GetString() ?? errorMessage;
                }

                // Возвращаем соответствующий статус код с сообщением об ошибке
                if (errorMessage.Contains("not found"))
                {
                    return NotFound(new { message = errorMessage });
                }
                else if (errorMessage.Contains("Invalid") && errorMessage.Contains("format"))
                {
                    return BadRequest(new { message = errorMessage });
                }

                // Для других ошибок возвращаем Internal Server Error
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = errorMessage });
            }

            // При успехе возвращаем только данные о месте
            return Ok(JsonSerializer.Deserialize<object>(result.RootElement.GetRawText()));
        }


        /// <summary>
        /// Получить место по URL.
        /// </summary>
        /// <remarks>
        /// Пример запроса:
        ///     GET /places/url/coffee-house-nicoya
        /// </remarks>
        /// <param name="url">URL места</param>
        /// <param name="status">Статус (опционально)</param>
        /// <returns>Объект места или сообщение об ошибке</returns>
        [HttpGet("url/{url}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<ActionResult> GetByUrl([FromRoute] string url, [FromQuery] string status = null)
        {
            var result = await _placeService.GetByUrlAsync(url, status);

            // Проверяем, является ли результат ошибкой
            if (result.RootElement.TryGetProperty("IsSuccess", out var isSuccessElement) &&
                isSuccessElement.GetBoolean() == false)
            {
                // Получаем сообщение об ошибке
                var errorMessage = "An error occurred";
                if (result.RootElement.TryGetProperty("Message", out var messageElement))
                {
                    errorMessage = messageElement.GetString() ?? errorMessage;
                }

                // Возвращаем соответствующий статус код с сообщением об ошибке
                if (errorMessage.Contains("not found"))
                {
                    return NotFound(new { message = errorMessage });
                }
                else if (errorMessage.Contains("required") || errorMessage.Contains("null or empty"))
                {
                    return BadRequest(new { message = errorMessage });
                }

                // Для других ошибок возвращаем Internal Server Error
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = errorMessage });
            }

            // При успехе возвращаем данные о месте
            if (result.RootElement.TryGetProperty("Data", out var dataElement))
            {
                return Ok(JsonSerializer.Deserialize<object>(dataElement.GetRawText()));
            }

            // Fallback - возвращаем весь результат если структура отличается
            return Ok(JsonSerializer.Deserialize<object>(result.RootElement.GetRawText()));
        }

        /// <summary>
        /// Получить места по ключевым словам.
        /// </summary>
        /// <remarks>
        /// Пример запроса:
        ///     GET /places/keywords?filterKeywords=кофе&amp;filterKeywords=WiFi&amp;lat=10.139&amp;lng=-85.452
        /// </remarks>
        /// <param name="lat">Широта</param>
        /// <param name="lng">Долгота</param>
        /// <param name="maxDistanceMeters">Максимальное расстояние в метрах</param>
        /// <param name="limit">Максимальное количество результатов</param>
        /// <param name="filterKeywords">Ключевые слова для поиска</param>
        /// <param name="searchAllKeywords">Искать по всем ключевым словам</param>
        /// <param name="isOpen">Только открытые</param>
        /// <param name="status">Статус</param>
        /// <returns>Список мест</returns>
        [HttpGet("keywords")]
        public async Task<IActionResult> GetPlacesWithAllKeywords(
            [FromQuery] decimal? lat = 10.539500881521633m,
            [FromQuery] decimal? lng = -85.68964788238951m,
            [FromQuery] int? maxDistanceMeters = 10000,
            [FromQuery] int limit = 100,
            [FromQuery] List<string>? filterKeywords = null,
            [FromQuery] bool searchAllKeywords = true,
            [FromQuery] bool isOpen = false,
            [FromQuery] string? status = null)
        {
            try
            {
                // Проверка filterKeywords на null и пустой список
                if (filterKeywords == null || !filterKeywords.Any())
                {
                    return BadRequest(new { error = "Filter keywords list is empty or not provided." });
                }

                // Передаем статус только если он явно задан (не null и не пустой)
                string? statusToPass = string.IsNullOrWhiteSpace(status) ? null : status;

                // Получение результата из сервиса
                var result = await _placeService.GetPlacesWithAllKeywordsAsync(
                    lat,
                    lng,
                    maxDistanceMeters,
                    limit,
                    filterKeywords,
                    searchAllKeywords,
                    isOpen,
                    statusToPass);

                if (result == null)
                {
                    return BadRequest(new { error = $"No places found with all the provided keywords{(isOpen ? " that are currently open" : "")}." });
                }

                // Проверяем структуру ответа
                if (result.RootElement.TryGetProperty("IsSuccess", out var isSuccessElement) &&
                    isSuccessElement.GetBoolean())
                {
                    // Если есть свойство data с местами
                    if (result.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        // Добавляем количество найденных документов в заголовок
                        var placesArray = JsonSerializer.Deserialize<object[]>(dataElement.GetRawText());
                        Response.Headers.Add("X-Total-Count", placesArray.Length.ToString());
                        Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");

                        return Ok(placesArray);
                    }
                    // Если нет свойства data, пытаемся получить массив напрямую
                    else
                    {
                        var placesObject = JsonSerializer.Deserialize<object>(result.RootElement.GetRawText());

                        // Если это массив, добавляем счетчик
                        if (placesObject is JsonElement element && element.ValueKind == JsonValueKind.Array)
                        {
                            var placesArray = JsonSerializer.Deserialize<object[]>(element.GetRawText());
                            Response.Headers.Add("X-Total-Count", placesArray.Length.ToString());
                            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");

                            return Ok(placesArray);
                        }

                        return Ok(placesObject);
                    }
                }
                else
                {
                    // Простой вывод ошибки из сервиса
                    var errorMessage = result.RootElement.TryGetProperty("Message", out var messageElement)
                        ? messageElement.GetString()
                        : "Unknown error occurred";

                    return BadRequest(new { error = errorMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении мест по ключевым словам: {ex.Message}" });
            }
        }

       
        /// <summary>
        /// Создать новое место.
        /// </summary>
        /// <remarks>
        /// Пример успешного ответа (201):
        ///     {
        ///         "id": "664b1e2f8f1b2c001e3e4a1a"
        ///     }
        /// </remarks>
        /// <param name="jsonDocument">Данные нового места (JSON)</param>
        /// <returns>Созданный объект места</returns>
        [HttpPost]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] JsonDocument jsonDocument)
        {
            try
            {
                if (jsonDocument == null || jsonDocument.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return BadRequest("Invalid input. Expected a JSON object.");
                }

                var result = await _placeService.CreateAsync(jsonDocument);

                if (result.RootElement.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
                {
                    if (result.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        _memoryCache.Remove(SITEMAP_CACHE_KEY);

                        // Проверяем и сбрасываем ПРОВИНЦИИ
                        if (ShouldInvalidateProvinces(jsonDocument))
                        {
                            _placeService.InvalidateProvincesCache();
                        }

                        // Проверяем и сбрасываем ГОРОДА
                        if (ShouldInvalidateCities(jsonDocument))
                        {
                            _placeService.InvalidateCitiesCache();
                        }

                        if (ShouldInvalidateTags(jsonDocument))
                        {
                            _placeService.InvalidateTagsCache();
                        }

                        return StatusCode(201, JsonDocument.Parse(dataElement.GetRawText()));
                    }
                    else
                    {
                        return BadRequest("Success response missing data field.");
                    }
                }
                else
                {
                    string message = "Unknown error occurred.";
                    if (result.RootElement.TryGetProperty("message", out var messageElement))
                    {
                        message = messageElement.GetString();
                    }
                    return BadRequest(message);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Обновить место по идентификатору.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="id">ID места</param>
        /// <param name="jsonDocument">Данные для обновления (JSON)</param>
        /// <returns>Обновленный объект места</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(string id, [FromBody] JsonDocument jsonDocument)
        {
            try
            {
                if (string.IsNullOrEmpty(id)) return BadRequest("Object ID is required.");
                if (jsonDocument == null || jsonDocument.RootElement.ValueKind != JsonValueKind.Object)
                    return BadRequest("Invalid input. Expected a JSON object.");

                var result = await _placeService.UpdateAsync(id, jsonDocument);

                if (result.RootElement.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
                {
                    if (result.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        _memoryCache.Remove(SITEMAP_CACHE_KEY);

                        string? placeUrl = null;
                        if (dataElement.TryGetProperty("url", out var urlElement))
                        {
                            placeUrl = urlElement.GetString();
                        }
                        if (!string.IsNullOrEmpty(placeUrl))
                        {
                            _ = TriggerCacheInvalidation($"place:{placeUrl}");
                        }

                        // Проверяем и сбрасываем ПРОВИНЦИИ
                        if (ShouldInvalidateProvinces(jsonDocument))
                        {
                            _placeService.InvalidateProvincesCache();
                        }

                        // Проверяем и сбрасываем ГОРОДА
                        if (ShouldInvalidateCities(jsonDocument))
                        {
                            _placeService.InvalidateCitiesCache();
                        }

                        if (ShouldInvalidateTags(jsonDocument))
                        {
                            _placeService.InvalidateTagsCache();
                        }

                        return Ok(dataElement);
                    }
                    else
                    {
                        return BadRequest("Success response missing data field.");
                    }
                }
                else
                {
                    string message = "Unknown error occurred.";
                    if (result.RootElement.TryGetProperty("message", out var messageElement))
                    {
                        message = messageElement.GetString();
                    }
                    else if (result.RootElement.TryGetProperty("error", out var errorElement))
                    {
                        message = errorElement.GetString();
                    }
                    return BadRequest(message);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Удалить место по идентификатору.
        /// </summary>
        /// <remarks>
        /// Пример запроса:
        /// DELETE /places/664b1e2f8f1b2c001e3e4a1a
        ///
        /// Пример успешного ответа (204 No Content)
        /// </remarks>
        /// <param name="id">ID места</param>
        /// <returns>204 No Content или сообщение об ошибке</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(string))]
        public async Task<IActionResult> Delete(string id)
        {
            string? placeUrl = null;
            try
            {
                var existingDoc = await _placeService.GetByIdAsync(id);
                if (existingDoc.RootElement.TryGetProperty("url", out var urlElement))
                {
                    placeUrl = urlElement.GetString();
                }
            }
            catch { }

            var deleteResult = await _placeService.DeleteAsync(id);

            if (deleteResult == null || deleteResult.RootElement.ValueKind != JsonValueKind.Object)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "Unexpected error occurred while deleting the document.");
            }

            if (deleteResult.RootElement.TryGetProperty("success", out var successElement) && successElement.ValueKind == JsonValueKind.False)
            {
                // При ошибке ничего не сбрасываем, просто возвращаем ошибку
                string errorMessage = "Failed to delete the document.";
                if (deleteResult.RootElement.TryGetProperty("error", out var errorElement))
                {
                    errorMessage = errorElement.GetString();
                }
                return BadRequest(errorMessage);
            }

            // Успешное удаление

            // 1. Сбрасываем ПРОВИНЦИИ (удаление могло убрать последнюю запись в провинции)
            _placeService.InvalidateProvincesCache();

            // 2. Сбрасываем ГОРОДА (удаление могло убрать последнюю запись в городе)
            _placeService.InvalidateCitiesCache();

            _placeService.InvalidateTagsCache(); // При удалении всегда сбрасываем

            _memoryCache.Remove(SITEMAP_CACHE_KEY);

            if (!string.IsNullOrEmpty(placeUrl))
            {
                _ = TriggerCacheInvalidation($"place:{placeUrl}");
            }

            return NoContent();
        }

        /// <summary>
        /// Универсальный поиск мест (фильтры)
        /// </summary>
        /// <remarks>
        /// Выполняет комплексный поиск мест в Коста-Рике с фильтрацией, гео-поиском и пагинацией.
        /// 
        /// **Ограничения:**
        /// - Гео-поиск (`distance`) ограничен **200,000 метрами (200 км)**.
        /// - При использовании `latitude` и `longitude` без `distance`, по умолчанию используется радиус **10,000 метров (10 км)**.
        /// 
        /// **Примеры использования (на основе данных Коста-Рики):**
        /// 
        /// 1. **Гео-поиск:** Найти все в радиусе 1 км от Zi Lounge в Playa del Coco.
        ///    ```
        ///    GET /places/filters?latitude=10.550185&longitude=-85.697221&distance=1000
        ///    ```
        /// 
        /// 2. **Фильтр по категории и тегам:** Найти все "рестораны" (to-eat) в Guanacaste, где есть "Seafood" И "Pizza".
        ///    ```
        ///    GET /places/filters?category=to-eat&province=Guanacaste&tags=Seafood,Pizza&tagsMode=all
        ///    ```
        /// 
        /// 3. **Текстовый поиск (q):** Найти места со словом "parties" в описании или названии.
        ///    ```
        ///    GET /places/filters?q=parties
        ///    ```
        /// 
        /// 4. **Фильтр "Открыто сейчас":** Найти все, что сейчас открыто, с сортировкой по имени (ASC).
        ///    ```
        ///    GET /places/filters?isOpen=true&sortField=name&sortOrder=ASC&page=1&perPage=20
        ///    ```
        /// 
        /// **Ответ содержит заголовки:**
        /// - `X-Total-Count`: Общее количество найденных записей.
        /// - `Access-Control-Expose-Headers`: X-Total-Count.
        /// </remarks>
        /// <param name="q">Текстовый запрос (поиск по name, description, category, address). Пример: "Lounge" или "parties"</param>
        /// <param name="province">Фильтр по провинции (нечувствителен к регистру). Пример: "Guanacaste"</param>
        /// <param name="city">Фильтр по городу (нечувствителен к регистру). Пример: "Playa del Coco"</param>
        /// <param name="name">Фильтр по точному или частичному совпадению названия. Пример: "Zi Lounge"</param>
        /// <param name="url">Фильтр по URL-слагу. Пример: "zi-lounge"</param>
        /// <param name="category">Фильтр по категории. Пример: "to-eat", "services", "shops"</param>
        /// <param name="status">Фильтр по статусу. По умолчанию (если не указан), сервис ищет только "active". Пример: "active"</param>
        /// <param name="tags">Список тегов через запятую. Пример: "Restaurant,Bar,Seafood"</param>
        /// <param name="tagsMode">Режим фильтрации: "any" (любой тег) или "all" (все теги). По умолчанию: "any"</param>
        /// <param name="latitude">Широта для гео-поиска. Пример: 10.550185</param>
        /// <param name="longitude">Долгота для гео-поиска. Пример: -85.697221</param>
        /// <param name="distance">Радиус поиска в метрах. По умолчанию 10000 (10км). Максимум 200000 (200км). Пример: 5000</param>
        /// <param name="isOpen">Фильтр по времени работы: true - только открытые, false - только закрытые, (не указано) - все</param>
        /// <param name="page">Номер страницы (начиная с 1). По умолчанию: 1</param>
        /// <param name="perPage">Количество на странице. По умолчанию: 20</param>
        /// <param name="sortField">Поле сортировки. Доступны: "name", "category", "status", "createdAt", "distance" (при гео-поиске). По умолчанию: "name"</pa>
        /// <param name="sortOrder">Порядок сортировки: "ASC" или "DESC". По умолчанию: "ASC"</param>
        /// <returns>Массив мест с информацией о пагинации в заголовках ответа</returns>
        [HttpGet("filters")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<object>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<IActionResult> GetPlacesWithGeoWithStatusWithTags(
            [FromQuery] string q = null,
            [FromQuery] string province = null, 
            [FromQuery] string city = null, 
            [FromQuery] string name = null,
            [FromQuery] string url = null,
            [FromQuery] string category = null, 
            [FromQuery] string status = null, //  (Blazor сам подставит "active")
            [FromQuery] string tags = null,
            [FromQuery] string tagsMode = "any", 
            [FromQuery] double? latitude = null, 
            [FromQuery] double? longitude = null, 
            [FromQuery] double? distance = null, 
            [FromQuery] bool? isOpen = null,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20, 
            [FromQuery] string sortField = "name", 
            [FromQuery] string sortOrder = "ASC")
        {
            const double MAX_DISTANCE_METERS = 200000; // 200 км
            //const double MAX_DISTANCE_METERS = 900000000; // тестовое ограничение

            if (distance.HasValue)
            {
                if (distance.Value > MAX_DISTANCE_METERS)
                {
                    return BadRequest(new { error = $"Search distance cannot exceed {MAX_DISTANCE_METERS} meters (200 km)." });
                }
                if (distance.Value <= 0)
                {
                    return BadRequest(new { error = "Search distance must be a positive number." });
                }
            }
            var filter = new Dictionary<string, string>();

            // Основные фильтры поиска
            if (!string.IsNullOrEmpty(q)) filter["q"] = q;
            if (!string.IsNullOrEmpty(province)) filter["province"] = province;
            if (!string.IsNullOrEmpty(city)) filter["city"] = city;
            if (!string.IsNullOrEmpty(name)) filter["name"] = name;
            if (!string.IsNullOrEmpty(url)) filter["url"] = url;
            if (!string.IsNullOrEmpty(category)) filter["category"] = category;
            if (!string.IsNullOrEmpty(status)) filter["status"] = status;

            // Фильтры по тегам
            if (!string.IsNullOrEmpty(tags)) filter["tags"] = tags;
            if (!string.IsNullOrEmpty(tagsMode)) filter["tagsMode"] = tagsMode;

            // Геопространственные параметры
            if (latitude.HasValue) filter["latitude"] = latitude.Value.ToString();
            if (longitude.HasValue) filter["longitude"] = longitude.Value.ToString();
            if (distance.HasValue) filter["distance"] = distance.Value.ToString();

            // Фильтр по времени работы
            if (isOpen.HasValue) filter["isOpen"] = isOpen.Value.ToString().ToLower();

            // Параметры сортировки и пагинации
            filter["_sort"] = sortField;
            filter["_order"] = sortOrder;
            filter["page"] = page.ToString();
            filter["perPage"] = perPage.ToString();

            try
            {
                var result = await _placeService.GetPlacesWithGeoWithStatusWithTagsAsync(filter);

                if (result.RootElement.TryGetProperty("success", out var successElement) &&
                    successElement.GetBoolean())
                {
                    var dataElement = result.RootElement.GetProperty("data");
                    var totalCount = dataElement.GetProperty("totalCount").GetInt64();
                    var placesElement = dataElement.GetProperty("places");

                    // Добавляем заголовки для пагинации
                    Response.Headers.Add("X-Total-Count", totalCount.ToString());
                    Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");

                    // Десериализуем массив мест
                    var placesArray = JsonSerializer.Deserialize<object[]>(placesElement.GetRawText());

                    return Ok(placesArray);
                }
                else
                {
                    var errorMessage = result.RootElement.GetProperty("error").GetString();
                    return BadRequest(new { error = errorMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении списка мест с геопоиском: {ex.Message}" });
            }
        }

        /// <summary>
        /// Отправляет запрос ("дергает вебхук") в Blazor приложение для сброса кеша
        /// </summary>
        private async Task TriggerCacheInvalidation(string? tag = null)
        {
            try
            {
                // Читаем настройки из переменных окружения
                // ASP.NET Core автоматически преобразует ENV переменные с "__" в иерархию с ":"
                var blazorUrl = _configuration["BLAZOR_APP:URL"];
                var secretKey = _configuration["BLAZOR_APP:CACHEKEY"];

                // Для отладки (если снова не заработает) можно раскомментировать:
                // Console.WriteLine($"DEBUG: BlazorURL='{blazorUrl}', Key='{secretKey}'");

                if (string.IsNullOrEmpty(blazorUrl) || string.IsNullOrEmpty(secretKey))
                {
                    // Если настроек нет, просто выходим (чтобы не ломать локальную разработку если не настроено)
                    return;
                }

                // Формируем URL: http://host:3000/cache/invalidate?key=...&tag=...
                var requestUrl = $"{blazorUrl}/cache/invalidate?key={secretKey}";
                if (!string.IsNullOrEmpty(tag))
                {
                    requestUrl += $"&tag={tag}";
                }

                // Создаем клиент и отправляем POST (fire and forget - не ждем ответа)
                var client = _httpClientFactory.CreateClient();

                // Мы не используем await, чтобы не задерживать ответ API пользователю.
                // API ответит "200 OK" мгновенно, а запрос уйдет в фоне.
                _ = client.PostAsync(requestUrl, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке вебхука инвалидации: {ex.Message}");
            }
        }

        // Метод Helper для проверки, нужно ли сбрасывать кеш провинций
        // Проверяет входящий JSON на наличие критических полей
        private bool ShouldInvalidateProvinces(JsonDocument jsonDoc)
        {
            if (jsonDoc == null) return false;

            // Если меняется статус (активен/неактивен) - список может измениться
            if (jsonDoc.RootElement.TryGetProperty("status", out _)) return true;

            // Если меняется категория - место может уйти из фильтра провинций
            if (jsonDoc.RootElement.TryGetProperty("category", out _)) return true;

            // Если меняется адрес
            if (jsonDoc.RootElement.TryGetProperty("address", out var addressElem))
            {
                // И внутри адреса меняется провинция
                if (addressElem.TryGetProperty("province", out _)) return true;
            }

            return false;
        }

        // Метод Helper для ГОРОДОВ
        private bool ShouldInvalidateCities(JsonDocument jsonDoc)
        {
            if (jsonDoc == null) return false;

            // 1. Статус или Категория меняют состав активных городов
            if (jsonDoc.RootElement.TryGetProperty("status", out _)) return true;
            if (jsonDoc.RootElement.TryGetProperty("category", out _)) return true;

            // 2. Изменение адреса
            if (jsonDoc.RootElement.TryGetProperty("address", out var addressElem))
            {
                // Если сменился Город - очевидно сбрасываем
                if (addressElem.TryGetProperty("city", out _)) return true;

                // Если сменилась Провинция - тоже сбрасываем, т.к. фильтр городов часто зависит от провинции
                // (активный город может "переехать" в другую провинцию)
                if (addressElem.TryGetProperty("province", out _)) return true;
            }

            return false;
        }

        // Метод Helper для ТЕГОВ
        private bool ShouldInvalidateTags(JsonDocument jsonDoc)
        {
            if (jsonDoc == null) return false;

            // 1. Статус или Категория меняют состав активных тегов
            if (jsonDoc.RootElement.TryGetProperty("status", out _)) return true;
            if (jsonDoc.RootElement.TryGetProperty("category", out _)) return true;

            // 2. Если изменился сам список тегов
            if (jsonDoc.RootElement.TryGetProperty("tags", out _)) return true;

            // 3. Изменение адреса (провинция/город) может повлиять на фильтры тегов
            if (jsonDoc.RootElement.TryGetProperty("address", out var addressElem))
            {
                if (addressElem.TryGetProperty("city", out _)) return true;
                if (addressElem.TryGetProperty("province", out _)) return true;
            }

            return false;
        }

    }
}
