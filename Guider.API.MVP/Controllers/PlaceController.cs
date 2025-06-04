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
        /// Получить документ по url и ID из заголовка
        /// </summary>
        /// <param name="url">Веб параметр</param>
        /// <param name="id">Идентификатор документа</param>
        /// <returns>Документ в формате JSON</returns>
        [HttpGet("name/id")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.GatewayTimeout)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.RequestTimeout)]
        public async Task<ActionResult<string>> GetPlaceByIdFromHeader(
            [FromQuery] string url,
             [FromQuery] string id)
        {
            var place = await _placeService.GetPlaceByIdFromHeaderAsync(id);

            if (place == null)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add($"Place with id {id} not found.");
                return NotFound(_response);
            }
            
            return Ok(place.ToJson());
        }



       


        /// <summary>
        /// Получить ближайшие места
        /// </summary>
        /// <param name="lat">Широта</param>
        /// <param name="lng">Долгота</param>
        /// <param name="maxDistance">Максимальное расстояние</param>
        /// <param name="isOpen">Фильтр по открытым заведениям (по времени Коста-Рики)</param>
        /// <returns>Список ближайших мест в формате JSON</returns>
        [HttpGet("geonear")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.GatewayTimeout)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.RequestTimeout)]
        public async Task<ActionResult<string>> GetNearbyPlaces(
            [FromHeader(Name = "X-Latitude")] decimal lat,
            [FromHeader(Name = "X-Longitude")] decimal lng,
            [FromHeader(Name = "X-Max-Distance")] int maxDistance,
            [FromHeader(Name = "X-Is-Open")] bool isOpen = false)
        {
            try
            {
                var places = await _placeService.GetPlacesNearbyAsync(lat, lng, maxDistance, isOpen);

                if (places == null || places.Count == 0)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add($"No places found within {maxDistance} meters{(isOpen ? " that are currently open" : "")}.");
                    return NotFound(_response);
                }

                return Content(places.ToJson(), "application/json");
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
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
        [HttpGet("nearby-with-keywords")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.GatewayTimeout)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.RequestTimeout)]
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
        [HttpGet("nearby-with-all-keywords")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.GatewayTimeout)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.RequestTimeout)]
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
        [HttpGet("available-tags")]
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


        /// <summary>
        /// Обновление существующего документа в коллекции Places.
        /// Доступно только для авторизованных пользователей с ролями Super Admin, Admin или Manager.
        /// В параметрах должны передаваться идентификатор объекта и валидный JSON-документ с данными.
        /// </summary>
        /// <param name="id">Строка с уникальным идентификатором объекта</param>
        /// <param name="jsonDocument">JSON-документ, представляющий данные для обновления существующего объекта.</param>
        /// <returns>Обновленный объект в формате JSON, обернутый в ApiResponse.</returns>
        //[HttpPut("{id}")]
        ////[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        //public async Task<IActionResult> Update(string id, [FromBody] JsonDocument jsonDocument)
        //{
        //    try
        //    {
        //        // Валидация входящих данных
        //        if (string.IsNullOrEmpty(id))
        //        {
        //            var idErrorResponse = new ApiResponse
        //            {
        //                StatusCode = HttpStatusCode.BadRequest,
        //                IsSuccess = false,
        //                ErrorMessages = new List<string> { "Object ID is required." }
        //            };
        //            return BadRequest(idErrorResponse);
        //        }

        //        if (jsonDocument == null || jsonDocument.RootElement.ValueKind != JsonValueKind.Object)
        //        {
        //            var validationResponse = new ApiResponse
        //            {
        //                StatusCode = HttpStatusCode.BadRequest,
        //                IsSuccess = false,
        //                ErrorMessages = new List<string> { "Invalid input. Expected a JSON object." }
        //            };
        //            return BadRequest(validationResponse);
        //        }

        //        // Отправляем в сервис для обновления
        //        var updatedDocument = await _placeService.UpdateAsync(id, jsonDocument);
        //        if (updatedDocument == null)
        //        {
        //            var notFoundResponse = new ApiResponse
        //            {
        //                StatusCode = HttpStatusCode.NotFound,
        //                IsSuccess = false,
        //                ErrorMessages = new List<string> { "Document not found or could not be updated." }
        //            };
        //            return NotFound(notFoundResponse);
        //        }

        //        // Проверка на неудачный результат операции(success = false)
        //        if (updatedDocument.RootElement.TryGetProperty("success", out var successElement) &&
        //           successElement.ValueKind == JsonValueKind.False)
        //        {
        //            string errorMessage = "Failed to update document.";
        //            // Добавляем сообщение об ошибке из сервиса, если оно присутствует
        //            if (updatedDocument.RootElement.TryGetProperty("message", out var serviceErrorElement) &&
        //                serviceErrorElement.ValueKind == JsonValueKind.String)
        //            {
        //                errorMessage = serviceErrorElement.GetString();
        //            }
        //            else if (updatedDocument.RootElement.TryGetProperty("error", out var errorElement) &&
        //                    errorElement.ValueKind == JsonValueKind.String)
        //            {
        //                errorMessage = errorElement.GetString();
        //            }

        //            var badRequestResponse = new ApiResponse
        //            {
        //                StatusCode = HttpStatusCode.BadRequest,
        //                IsSuccess = false,
        //                ErrorMessages = new List<string> { errorMessage }
        //            };
        //            return BadRequest(badRequestResponse);
        //        }

        //        // Извлекаем документ и сообщение об успешном обновлении
        //        string successMessage = "Document successfully updated.";
        //        JsonElement documentElement;

        //        // Проверяем, содержит ли ответ документ внутри структуры success
        //        if (updatedDocument.RootElement.TryGetProperty("document", out documentElement))
        //        {
        //            if (updatedDocument.RootElement.TryGetProperty("message", out var messageElement) &&
        //                messageElement.ValueKind == JsonValueKind.String)
        //            {
        //                successMessage = messageElement.GetString();
        //            }
        //        }
        //        else
        //        {
        //            // Если нет вложенного документа, то весь ответ и есть документ
        //            documentElement = updatedDocument.RootElement;
        //        }

        //        // Формируем успешный ответ
        //        var successResponse = new ApiResponse
        //        {
        //            StatusCode = HttpStatusCode.OK,
        //            IsSuccess = true,
        //            Result = documentElement.ToString(),
        //            //SuccessMessage = successMessage
        //        };
        //        return Ok(successResponse);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Формируем ошибочный ответ
        //        var errorResponse = new ApiResponse
        //        {
        //            StatusCode = HttpStatusCode.InternalServerError,
        //            IsSuccess = false,
        //            ErrorMessages = new List<string> { ex.Message }
        //        };
        //        return StatusCode((int)HttpStatusCode.InternalServerError, errorResponse);
        //    }
        //}
        //[HttpPut("{id}")]
        ////[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        //public async Task<IActionResult> Update(string id, [FromBody] JsonDocument jsonDocument)
        //{
        //    try
        //    {
        //        // Валидация входящих данных
        //        if (string.IsNullOrEmpty(id))
        //        {
        //            return BadRequest("Object ID is required.");
        //        }

        //        if (jsonDocument == null || jsonDocument.RootElement.ValueKind != JsonValueKind.Object)
        //        {
        //            return BadRequest("Invalid input. Expected a JSON object.");
        //        }

        //        // Отправляем в сервис для обновления
        //        var updatedDocument = await _placeService.UpdateAsync(id, jsonDocument);
        //        if (updatedDocument == null)
        //        {
        //            return NotFound("Document not found or could not be updated.");
        //        }

        //        // Проверка на неудачный результат операции (success = false)
        //        if (updatedDocument.RootElement.TryGetProperty("success", out var successElement) &&
        //           successElement.ValueKind == JsonValueKind.False)
        //        {
        //            string errorMessage = "Failed to update document.";

        //            // Добавляем сообщение об ошибке из сервиса, если оно присутствует
        //            if (updatedDocument.RootElement.TryGetProperty("message", out var serviceErrorElement) &&
        //                serviceErrorElement.ValueKind == JsonValueKind.String)
        //            {
        //                errorMessage = serviceErrorElement.GetString();
        //            }
        //            else if (updatedDocument.RootElement.TryGetProperty("error", out var errorElement) &&
        //                    errorElement.ValueKind == JsonValueKind.String)
        //            {
        //                errorMessage = errorElement.GetString();
        //            }

        //            return BadRequest(errorMessage);
        //        }

        //        // Извлекаем документ из ответа сервиса
        //        JsonElement documentElement;

        //        // Проверяем, есть ли вложенный документ
        //        if (updatedDocument.RootElement.TryGetProperty("document", out documentElement))
        //        {
        //            // Возвращаем вложенный документ
        //            var documentObject = JsonSerializer.Deserialize<object>(documentElement.GetRawText());
        //            return Ok(documentObject);
        //        }
        //        else
        //        {
        //            // Если нет вложенного документа, то весь ответ и есть документ
        //            var documentObject = JsonSerializer.Deserialize<object>(updatedDocument.RootElement.GetRawText());
        //            return Ok(documentObject);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, ex.Message);
        //    }
        //}

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
                    if (result.RootElement.TryGetProperty("document", out var dataElement))
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
