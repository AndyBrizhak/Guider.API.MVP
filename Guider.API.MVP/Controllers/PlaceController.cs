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
        /// Получить ближайшие места
        /// </summary>
        /// <param name="lat">Широта</param>
        /// <param name="lng">Долгота</param>
        /// <param name="maxDistance">Максимальное расстояние</param>
        /// <param name="isOpen">Фильтр по открытым заведениям (по времени Коста-Рики)</param>
        /// <returns>Список ближайших мест в формате JSON</returns>
        //[HttpGet("geo")]
        //public async Task<ActionResult<string>> GetNearbyPlaces(
        //    [FromHeader(Name = "X-Latitude")] decimal lat = 10.539500881521633m,
        //    [FromHeader(Name = "X-Longitude")] decimal lng = -85.68964788238951m,
        //    [FromHeader(Name = "X-Max-Distance")] int maxDistance = 10000,
        //    [FromHeader(Name = "X-Is-Open")] bool isOpen = false)
        //{
        //    try
        //    {
        //        var places = await _placeService.GetPlacesNearbyAsync(lat, lng, maxDistance, isOpen);

        //        if (places == null || places.Count == 0)
        //        {
        //            _response.StatusCode = HttpStatusCode.NotFound;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages.Add($"No places found within {maxDistance} meters{(isOpen ? " that are currently open" : "")}.");
        //            return NotFound(_response);
        //        }

        //        return Content(places.ToJson(), "application/json");
        //    }
        //    catch (Exception ex)
        //    {
        //        _response.StatusCode = HttpStatusCode.InternalServerError;
        //        _response.IsSuccess = false;
        //        _response.ErrorMessages.Add(ex.Message);
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}

        //[HttpGet("geo")]
        //public async Task<ActionResult<string>> GetNearbyPlaces(
        //[FromHeader(Name = "X-Latitude")] decimal lat = 10.539500881521633m,
        //[FromHeader(Name = "X-Longitude")] decimal lng = -85.68964788238951m,
        //[FromHeader(Name = "X-Max-Distance")] int maxDistance = 10000,
        //[FromHeader(Name = "X-Is-Open")] bool isOpen = false,
        //[FromHeader(Name = "X-Status")] string status = "active")
        //{
        //    try
        //    {
        //        var places = await _placeService.GetPlacesNearbyAsync(lat, lng, maxDistance, isOpen, status);
        //        if (places == null || places.Count == 0)
        //        {
        //            _response.StatusCode = HttpStatusCode.NotFound;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages.Add($"No places found within {maxDistance} meters{(isOpen ? " that are currently open" : "")} with status '{status}'.");
        //            return NotFound(_response);
        //        }
        //        return Content(places.ToJson(), "application/json");
        //    }
        //    catch (Exception ex)
        //    {
        //        _response.StatusCode = HttpStatusCode.InternalServerError;
        //        _response.IsSuccess = false;
        //        _response.ErrorMessages.Add(ex.Message);
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}

        [HttpGet("geo")]
        public async Task<ActionResult<string>> GetNearbyPlaces(
    [FromHeader(Name = "X-Latitude")] decimal lat = 10.539500881521633m,
    [FromHeader(Name = "X-Longitude")] decimal lng = -85.68964788238951m,
    [FromHeader(Name = "X-Max-Distance")] int maxDistance = 10000,
    [FromHeader(Name = "X-Is-Open")] bool isOpen = false,
    [FromHeader(Name = "X-Status")] string status = "active")
        {
            try
            {
                var result = await _placeService.GetPlacesNearbyAsync(lat, lng, maxDistance, isOpen, status);

                // Парсим JsonDocument для получения структуры ответа
                var rootElement = result.RootElement;

                // Проверяем успешность операции
                if (rootElement.TryGetProperty("IsSuccess", out var isSuccessElement) &&
                    isSuccessElement.GetBoolean())
                {
                    // Успешный ответ - возвращаем JSON как есть
                    return Content(result.RootElement.GetRawText(), "application/json");
                }
                else
                {
                    // Неуспешный ответ - получаем сообщение об ошибке
                    var errorMessage = rootElement.TryGetProperty("Message", out var messageElement)
                        ? messageElement.GetString()
                        : "Unknown error occurred";

                    // Определяем HTTP статус в зависимости от типа ошибки
                    if (errorMessage.Contains("Invalid") || errorMessage.Contains("Must be") ||
                        errorMessage.Contains("cannot be null"))
                    {
                        return BadRequest(result.RootElement.GetRawText());
                    }
                    else if (errorMessage.Contains("Found 0 places"))
                    {
                        return NotFound(result.RootElement.GetRawText());
                    }
                    else
                    {
                        return StatusCode((int)HttpStatusCode.InternalServerError, result.RootElement.GetRawText());
                    }
                }
            }
            catch (Exception ex)
            {
                // Создаем стандартный формат ошибки для исключений контроллера
                var errorResponse = new
                {
                    IsSuccess = false,
                    Message = $"Controller error: {ex.Message}"
                };

                return StatusCode((int)HttpStatusCode.InternalServerError,
                    JsonSerializer.Serialize(errorResponse));
            }
        }

        /// <summary>
        /// Получить ближайшие места с любым из ключевых слов
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <param name="maxDistanceMeters"></param>
        /// <param name="limit"></param>
        /// <param name="filterKeywords"></param>
        /// <returns></returns>
        [HttpGet("geo/keywords-any")]
        public async Task<ActionResult<string>> GetPlacesWithKeywordsList(
           [FromQuery] decimal lat = 10.539500881521633m,
           [FromQuery] decimal lng = -85.68964788238951m,
           [FromQuery] int maxDistanceMeters = 10000,
           [FromQuery] int limit = 100,
           [FromQuery] List<string>? filterKeywords = null,
           [FromQuery] bool isOpen = false)
        {
            // Проверка и корректировка значения limit
            if (limit < 1)
            {
                limit = 200;
            }
            else if (limit > 200)
            {
                limit = 200;
            }

            // Проверка filterKeywords на null и пустой список
            if (filterKeywords == null || !filterKeywords.Any())
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Filter keywords list is empty or not provided.");
                return NotFound(_response);
            }

            List<BsonDocument> places;

            // Используем соответствующую перегрузку метода в зависимости от значения isOpen
            if (isOpen)
            {
                places = await _placeService.GetPlacesNearbyAsync(lat, lng, maxDistanceMeters, isOpen);
            }
            else
            {
                places = await _placeService.GetPlacesWithKeywordsListAsync(lat, lng, maxDistanceMeters, limit, filterKeywords);
            }

            if (places == null || places.Count == 0)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add($"No places found with the provided filters{(isOpen ? " that are currently open" : "")}.");
                return NotFound(_response);
            }

            return Content(places.ToJson(), "application/json");
        }



        /// <summary>
        /// Получить ближайшие места, содержащие все указанные ключевые слова
        /// с учетом фильтра по времени работы
        /// </summary>
        /// <param name="lat">Широта</param>
        /// <param name="lng">Долгота</param>
        /// <param name="maxDistanceMeters">Максимальное расстояние в метрах</param>
        /// <param name="limit">Максимальное количество результатов</param>
        /// <param name="filterKeywords">Список ключевых слов (обязательны все слова)</param>
        /// <param name="isOpen">Учитывать ли расписание работы</param>
        /// <returns>Список найденных мест</returns>
        [HttpGet("geo/keywords-all")]
        public async Task<IActionResult> GetPlacesWithAllKeywords(
          [FromQuery] decimal lat = 10.539500881521633m,
          [FromQuery] decimal lng = -85.68964788238951m,
          [FromQuery] int maxDistanceMeters = 10000,
          [FromQuery] int limit = 100,
          [FromQuery] List<string>? filterKeywords = null,
          [FromQuery] bool isOpen = false)
        {
            
                // Проверка и корректировка значения limit
                if (limit < 1)
                {
                    limit = 200;
                }
                else if (limit > 200)
                {
                    limit = 200;
                }

                // Проверка filterKeywords на null и пустой список
                if (filterKeywords == null || !filterKeywords.Any())
                {
                    var badRequestResponse = new ApiResponse
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        IsSuccess = false,
                        ErrorMessages = new List<string> { "Filter keywords list is empty or not provided." }
                    };
                    return BadRequest(badRequestResponse);
                }

                // Получение результата из сервиса
                JsonDocument placesJson;
                placesJson = await _placeService.GetPlacesWithAllKeywordsAsync(lat, lng, maxDistanceMeters, limit, filterKeywords, isOpen);
                
                if (placesJson == null)
                {
                    var notFoundResponse = new ApiResponse
                    {
                        StatusCode = HttpStatusCode.NotFound,
                        IsSuccess = false,
                        ErrorMessages = new List<string> { $"No places found with all the provided keywords{(isOpen ? " that are currently open" : "")}." }
                    };
                    return NotFound(notFoundResponse);
                }

                // Формирование успешного ответа
                var successResponse = new ApiResponse
                {
                    StatusCode = HttpStatusCode.OK,
                    IsSuccess = true,
                    Result = placesJson
                };

                return Ok(successResponse);
            
        }


        /// <summary>
        /// Получить доступные теги, которые еще не выбраны и которые содержатся во описании 
        /// бизнеосв в выбранной категории.
        /// Этот метод позволяет получить список доступных тегов, которые можно использовать
        /// для фильтрации мест. Теги могут быть отфильтрованы по категории и/или с учетом
        /// уже выбранных тегов. Если категория не указана, возвращаются теги для всех категорий.
        /// 
        /// Пример использования:
        /// - Укажите категорию, чтобы получить теги, относящиеся только к этой категории.
        /// - Передайте список выбранных тегов, чтобы исключить их из результата.
        /// </summary>
        /// <param name="category">Категория, для которой нужно получить теги (опционально).</param>
        /// <param name="selectedTags">Список уже выбранных тегов, которые нужно исключить из результата (опционально).</param>
        /// <returns>Список доступных тегов в формате JSON, обернутый в ApiResponse.</returns>
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
    }
}
