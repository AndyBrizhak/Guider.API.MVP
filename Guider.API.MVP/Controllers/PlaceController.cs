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

namespace Guider.API.MVP.Controllers
{
    [Route("places")]
    [ApiController]
    public class PlaceController : ControllerBase
    {
        private readonly PlaceService _placeService;
        private ApiResponse _response;

        public PlaceController(PlaceService placeService)
        {
            _placeService = placeService;
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
        /// 
        ///     GET /places/url/coffee-house-nicoya
        /// 
        /// Пример успешного ответа:
        /// 
        ///     {
        ///         "id": "664b1e2f8f1b2c001e3e4a1a",
        ///         "name": "Coffee House"
        ///     }
        /// </remarks>
        /// <param name="url">URL места</param>
        /// <param name="status">Статус (опционально)</param>
        /// <returns>Объект места или сообщение об ошибке</returns>
        /// <response code="200">Успешный запрос. Возвращает объект места</response>
        /// <response code="400">Некорректные параметры запроса</response>
        /// <response code="404">Место не найдено</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [HttpGet("url/{url}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
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
        /// 
        ///     GET /places/keywords?filterKeywords=кофе&amp;filterKeywords=WiFi&amp;lat=10.139&amp;lng=-85.452
        /// 
        /// Пример успешного ответа:
        /// 
        ///     [
        ///         {
        ///             "id": "664b1e2f8f1b2c001e3e4a1a",
        ///             "name": "Coffee House"
        ///         }
        ///     ]
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
        /// <response code="200">Успешный запрос. Возвращает массив мест</response>
        /// <response code="400">Некорректные параметры запроса или пустой список ключевых слов</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
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
        /// Получить доступные теги для мест.
        /// </summary>
        /// <remarks>
        /// Пример запроса:
        /// 
        ///     GET /places/tags-on-places?category=food&amp;selectedTags=кофе&amp;selectedTags=WiFi
        /// 
        /// Пример успешного ответа:
        /// 
        ///     {
        ///         "statusCode": 200,
        ///         "isSuccess": true,
        ///         "result": [ "кофе", "WiFi", "завтрак" ]
        ///     }
        /// </remarks>
        /// <param name="category">Категория</param>
        /// <param name="selectedTags">Список выбранных тегов</param>
        /// <returns>Список доступных тегов</returns>
        /// <response code="200">Успешный запрос. Возвращает список тегов</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [HttpGet("tags-on-places")]
        public async Task<ActionResult> GetAvailableTags(
           [FromQuery] string? category = null,
           [FromQuery] List<string>? selectedTags = null)
        {
            try
            {

                var result = await _placeService.GetAvailableTagsAsync(
                    category,
                    selectedTags);


                var response = new ApiResponse
                {
                    StatusCode = HttpStatusCode.OK,
                    IsSuccess = true,
                    Result = result
                };

                return Ok(response);
            }
            catch (Exception ex)
            {

                var errorResponse = new ApiResponse
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    IsSuccess = false,
                    ErrorMessages = new List<string> { ex.Message }
                };

                return StatusCode((int)HttpStatusCode.InternalServerError, errorResponse);
            }
        }


        /// <summary>
        /// Создать новое место.
        /// </summary>
        /// <remarks>
        /// Пример тела запроса:
        /// 
        ///     {
        ///         "name": "New Place",
        ///         "province": "Guanacaste",
        ///         "city": "Nicoya",
        ///         "address": "Main street, Nicoya",
        ///         "tags": ["кофе", "WiFi"],
        ///         "location": { "lat": 10.139, "lng": -85.452 },
        ///         "img_link": "https://example.com/image.jpg"
        ///     }
        /// 
        /// Пример успешного ответа (201):
        /// 
        ///     {
        ///         "id": "664b1e2f8f1b2c001e3e4a1a"
        ///     }
        /// </remarks>
        /// <param name="jsonDocument">Данные нового места (JSON)</param>
        /// <returns>Созданный объект места</returns>
        /// <response code="201">Место успешно создано</response>
        /// <response code="400">Некорректные данные запроса</response>
        [HttpPost]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> Create([FromBody] JsonDocument jsonDocument)
        {
            try
            {
                // Валидация входящих данных  
                if (jsonDocument == null || jsonDocument.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return BadRequest("Invalid input. Expected a JSON object.");
                }

                // Отправляем в сервис и получаем результат
                var result = await _placeService.CreateAsync(jsonDocument);

                // Проверяем результат из сервиса
                if (result.RootElement.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
                {
                    // Успешное создание - возвращаем 201 Created
                    if (result.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        return StatusCode(201, JsonDocument.Parse(dataElement.GetRawText()));
                    }
                    else
                    {
                        return BadRequest("Success response missing data field.");
                    }
                }
                else
                {
                    // Неудачное создание - возвращаем 400 Bad Request
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
        /// Пример тела запроса:
        /// 
        ///     {
        ///         "name": "Updated Place",
        ///         "tags": ["кофе", "WiFi", "завтрак"]
        ///     }
        /// 
        /// Пример успешного ответа:
        /// 
        ///     {
        ///         "id": "664b1e2f8f1b2c001e3e4a1a"
        ///     }
        /// </remarks>
        /// <param name="id">ID места</param>
        /// <param name="jsonDocument">Данные для обновления (JSON)</param>
        /// <returns>Обновленный объект места</returns>
        /// <response code="200">Место успешно обновлено</response>
        /// <response code="400">Некорректные данные запроса или ID</response>
        [HttpPut("{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> Update(string id, [FromBody] JsonDocument jsonDocument)
        {
            try
            {
                // Валидация входящих данных
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest("Object ID is required.");
                }
                if (jsonDocument == null || jsonDocument.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return BadRequest("Invalid input. Expected a JSON object.");
                }
                // Отправляем в сервис и получаем результат
                var result = await _placeService.UpdateAsync(id, jsonDocument);
                // Проверяем результат из сервиса
                if (result.RootElement.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
                {
                    // Успешное обновление - возвращаем 200 OK
                    if (result.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        //return Ok(JsonDocument.Parse(dataElement.GetRawText()));
                        return Ok(dataElement);
                    }
                    else
                    {
                        return BadRequest("Success response missing data field.");
                    }
                }
                else
                {
                    // Неудачное обновление - возвращаем 400 Bad Request
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
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        public async Task<IActionResult> Delete(string id)
        {
            var deleteResult = await _placeService.DeleteAsync(id);

            if (deleteResult == null || deleteResult.RootElement.ValueKind != JsonValueKind.Object)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "Unexpected error occurred while deleting the document.");
            }

            if (deleteResult.RootElement.TryGetProperty("success", out var successElement) && successElement.ValueKind == JsonValueKind.False)
            {
                string errorMessage = "Failed to delete the document.";
                if (deleteResult.RootElement.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.String)
                {
                    errorMessage = errorElement.GetString();
                }

                return BadRequest(errorMessage);
            }

            // Успешное удаление
            return NoContent();
        }

        /// <summary>
        /// Универсальный поиск c фильтрацией и сортировкой
        /// </summary>
        /// <remarks>
        /// Выполняет комплексный поиск мест с возможностью фильтрации по различным критериям:
        /// - Текстовый поиск по названию, описанию и другим полям
        /// - Географическая фильтрация по провинции и городу
        /// - Геопространственный поиск в радиусе от указанных координат
        /// - Фильтрация по категориям, статусам и тегам
        /// - Фильтрация по времени работы (открыто/закрыто)
        /// - Поддержка сортировки и пагинации результатов
        /// 
        /// **Примеры использования:**
        /// 
        /// 1. Поиск ресторанов в радиусе 5 км от центра города:
        ///    ```
        ///    GET /api/places/with-geo-status-tags?category=restaurant&amp;latitude=50.4501&amp;longitude=30.5234&amp;distance=5000
        ///    ```
        /// 
        /// 2. Поиск открытых кафе с тегами "wifi" или "терраса":
        ///    ```
        ///    GET /api/places/with-geo-status-tags?category=cafe&amp;isOpen=true&amp;tags=wifi,терраса&amp;tagsMode=any
        ///    ```
        /// 
        /// 3. Текстовый поиск с сортировкой по названию:
        ///    ```
        ///    GET /api/places/with-geo-status-tags?q=пицца&amp;sortField=name&amp;sortOrder=ASC&amp;page=1&amp;perPage=10
        ///    ```
        /// 
        /// **Ответ содержит заголовки:**
        /// - `X-Total-Count`: общее количество найденных записей
        /// - `Access-Control-Expose-Headers`: список доступных заголовков для CORS
        /// </remarks>
        /// <param name="q">Текстовый запрос для поиска по названию, описанию и другим полям места. Пример: "кафе центр"</param>
        /// <param name="province">Фильтр по провинции/области. Пример: "Киевская область"</param>
        /// <param name="city">Фильтр по городу. Пример: "Киев"</param>
        /// <param name="name">Фильтр по точному или частичному совпадению названия. Пример: "Старбакс"</param>
        /// <param name="url">Фильтр по URL/веб-сайту места. Пример: "starbucks.com"</param>
        /// <param name="category">Фильтр по категории места. Пример: "restaurant", "cafe", "hotel"</param>
        /// <param name="status">Фильтр по статусу места. Пример: "active", "inactive", "pending"</param>
        /// <param name="tags">Список тегов через запятую для фильтрации. Пример: "wifi,парковка,детская площадка"</param>
        /// <param name="tagsMode">Режим фильтрации по тегам: "any" (любой из тегов) или "all" (все теги). По умолчанию: "any"</param>
        /// <param name="latitude">Широта для геопространственного поиска в градусах. Пример: 50.4501</param>
        /// <param name="longitude">Долгота для геопространственного поиска в градусах. Пример: 30.5234</param>
        /// <param name="distance">Радиус поиска в метрах от указанных координат. Пример: 1000 (1 км), 5000 (5 км)</param>
        /// <param name="isOpen">Фильтр по времени работы: true - только открытые места, false - только закрытые, null - все</param>
        /// <param name="page">Номер страницы для пагинации (начиная с 1). По умолчанию: 1</param>
        /// <param name="perPage">Количество записей на странице (1-100). По умолчанию: 20</param>
        /// <param name="sortField">Поле для сортировки. Доступные значения: "name", "category", "status", "createdAt", "distance" (при геопоиске). По умолчанию: "name"</param>
        /// <param name="sortOrder">Порядок сортировки: "ASC" (по возрастанию) или "DESC" (по убыванию). По умолчанию: "ASC"</param>
        /// <returns>Массив мест с информацией о пагинации в заголовках ответа</returns>
        /// <response code="200">Успешно получен список мест. Возвращает массив объектов мест с заголовком X-Total-Count</response>
        /// <response code="400">Ошибка в параметрах запроса или логике фильтрации</response>
        /// <response code="500">Внутренняя ошибка сервера при выполнении поиска</response>
        [HttpGet("filters")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> GetPlacesWithGeoWithStatusWithTags(
            [FromQuery] string q = null,
            [FromQuery] string province = null,
            [FromQuery] string city = null,
            [FromQuery] string name = null,
            [FromQuery] string url = null,
            [FromQuery] string category = null,
            [FromQuery] string status = null,
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
    }
}
