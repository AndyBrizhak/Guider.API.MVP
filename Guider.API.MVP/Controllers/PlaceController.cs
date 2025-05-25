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
    [Route("api/[controller]")]
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


        
        [HttpGet("paged")]
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
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> GetAllPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                // Получаем данные из сервиса (теперь возвращается кортеж)
                var (documents, totalCount) = await _placeService.GetAllPagedAsync(pageNumber, pageSize);

                // Проверяем наличие данных
                if (documents == null || documents.Count == 0)
                {
                    return NotFound("No places found.");
                }

                // Преобразуем JsonDocument в объекты для сериализации
                var result = new List<object>();
                foreach (var document in documents)
                {
                    try
                    {
                        // Преобразуем JsonDocument в объект для корректной сериализации
                        using (document)
                        {
                            var jsonString = document.RootElement.GetRawText();
                            var jsonObject = System.Text.Json.JsonSerializer.Deserialize<object>(jsonString);
                            result.Add(jsonObject);
                        }
                    }
                    catch (Exception ex)
                    {
                        // В случае ошибки обработки конкретного документа
                        return BadRequest($"Error processing document: {ex.Message}");
                    }
                }

                // Добавляем заголовки для пагинации (для react-admin)
                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить документ по ID
        /// </summary>
        /// <param name="id">Идентификатор документа</param>
        /// <returns>Документ в формате JSON</returns>
        [HttpGet("{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(typeof(ApiResponse), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.GatewayTimeout)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.RequestTimeout)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Conflict)]
        public async Task<ActionResult<string>> GetById(string id)
        {
            var place = await _placeService.GetByIdAsync(id);
            //if (place == null) return NotFound();
            //var placeJson = place.ToJson();
            //return Ok(placeJson);
            if (place == null)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add($"Place with id {id} not found.");
                return NotFound(_response);
            }
            //_response.Result = place.ToJson();
            //_response.StatusCode = HttpStatusCode.OK;
            return Ok(place.ToJson());
        }

        

        /// <summary>
        /// Получить документ по web и ID из заголовка
        /// </summary>
        /// <param name="web">Веб параметр</param>
        /// <param name="id">Идентификатор документа</param>
        /// <returns>Документ в формате JSON</returns>
        [HttpGet("place/id")]
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
            [FromQuery] string web,
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
        /// Получить доступные теги для фильтрации мест.
        /// 
        /// Этот метод позволяет получить список доступных тегов, которые можно использовать
        /// для фильтрации мест. Теги могут быть отфильтрованы по категории или с учетом
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


        /// <summary>  
        /// Создание нового документа в коллекции Places.  
        /// Доступно только для авторизованных пользователей с ролями Super Admin, Admin или Manager.  
        /// Во входящем параметре должен передаваться валидный JSON-документ.  
        /// </summary>  
        /// <param name="jsonDocument">JSON-документ, представляющий данные для создания нового объекта.</param>  
        /// <returns>Созданный объект в формате JSON, обернутый в ApiResponse.</returns>  
        [HttpPost("create")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> Create([FromBody] JsonDocument jsonDocument)
        {
            try
            {
                // Валидация входящих данных  
                if (jsonDocument == null || jsonDocument.RootElement.ValueKind != JsonValueKind.Object)
                {
                    var validationResponse = new ApiResponse
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        IsSuccess = false,
                        ErrorMessages = new List<string> { "Invalid input. Expected a JSON object." }
                    };
                    return BadRequest(validationResponse);
                }

                // Отправляем в сервис и получаем полный документ  
                var createdDocument = await _placeService.CreateAsync(jsonDocument);

                if (createdDocument == null)
                {
                    var notFoundResponse = new ApiResponse
                    {
                        StatusCode = HttpStatusCode.NotFound,
                        IsSuccess = false,
                        ErrorMessages = new List<string> { "Document could not be created." }
                    };
                    return NotFound(notFoundResponse);
                }

                // Проверка на неудачный результат операции(success = false)
                if (!(!createdDocument.RootElement.TryGetProperty("success", out var successElement) ||
                   successElement.ValueKind != JsonValueKind.False))
                {
                    string errorMessage = "Failed to create document or missing id.";
                    // Добавляем сообщение об ошибке из сервиса, если оно присутствует
                    if (createdDocument.RootElement.TryGetProperty("message", out var serviceErrorElement) &&
                        serviceErrorElement.ValueKind == JsonValueKind.String)
                    {
                        errorMessage += $" Service error: {serviceErrorElement.GetString()}";
                    }
                    var badRequestResponse = new ApiResponse
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        IsSuccess = false,
                        ErrorMessages = new List<string> { errorMessage }
                    };
                    return BadRequest(badRequestResponse);
                }

               
                
                    var successResponse = new ApiResponse
                    {
                        StatusCode = HttpStatusCode.Created,
                        IsSuccess = true,
                        Result = new
                        {
                          Data = createdDocument.ToJson() // Include the entire created object
                        }
                    };
                    return Ok(successResponse);
                
                
                

            }
            catch (Exception ex)
            {
                // Формируем ошибочный ответ  
                var errorResponse = new ApiResponse
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    IsSuccess = false,
                    ErrorMessages = new List<string> { ex.Message }
                };
                return BadRequest(errorResponse);
            }
        }

                
        /// <summary>
        /// Обновление существующего документа в коллекции Places.
        /// 
        /// Доступно только для авторизованных пользователей с ролями Super Admin, Admin или Manager.
        /// 
        /// В параметрах должны передаваться идентификатор объекта и валидный JSON-документ с данными.
        /// </summary>
        /// 
        /// <param name="id">Строка с уникальным идентификатором объекта</param>
        /// <param name="jsonDocument">JSON-документ, представляющий данные для обновления существующего объекта.</param>
        /// 
        /// <returns>Обновленный объект в формате JSON, обернутый в ApiResponse.</returns>
        /// 
        [HttpPut("update/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> Update(string id, [FromBody] JsonDocument jsonDocument)
        {
            try
            {
                // Валидация входящих данных
                if (string.IsNullOrEmpty(id))
                {
                    var idErrorResponse = new ApiResponse
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        IsSuccess = false,
                        ErrorMessages = new List<string> { "Object ID is required." }
                    };
                    return BadRequest(idErrorResponse);
                }

                if (jsonDocument == null || jsonDocument.RootElement.ValueKind != JsonValueKind.Object)
                {
                    var validationResponse = new ApiResponse
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        IsSuccess = false,
                        ErrorMessages = new List<string> { "Invalid input. Expected a JSON object." }
                    };
                    return BadRequest(validationResponse);
                }

                // Отправляем в сервис для обновления
                var updatedDocument = await _placeService.UpdateAsync(id, jsonDocument);
                if (updatedDocument == null)
                {
                    var notFoundResponse = new ApiResponse
                    {
                        StatusCode = HttpStatusCode.NotFound,
                        IsSuccess = false,
                        ErrorMessages = new List<string> { "Document not found or could not be updated." }
                    };
                    return NotFound(notFoundResponse);
                }

                // Проверка на неудачный результат операции(success = false)
                if (updatedDocument.RootElement.TryGetProperty("success", out var successElement) &&
                   successElement.ValueKind == JsonValueKind.False)
                {
                    string errorMessage = "Failed to update document.";
                    // Добавляем сообщение об ошибке из сервиса, если оно присутствует
                    if (updatedDocument.RootElement.TryGetProperty("message", out var serviceErrorElement) &&
                        serviceErrorElement.ValueKind == JsonValueKind.String)
                    {
                        errorMessage = serviceErrorElement.GetString();
                    }
                    else if (updatedDocument.RootElement.TryGetProperty("error", out var errorElement) &&
                            errorElement.ValueKind == JsonValueKind.String)
                    {
                        errorMessage = errorElement.GetString();
                    }

                    var badRequestResponse = new ApiResponse
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        IsSuccess = false,
                        ErrorMessages = new List<string> { errorMessage }
                    };
                    return BadRequest(badRequestResponse);
                }

                // Извлекаем документ и сообщение об успешном обновлении
                string successMessage = "Document successfully updated.";
                JsonElement documentElement;

                // Проверяем, содержит ли ответ документ внутри структуры success
                if (updatedDocument.RootElement.TryGetProperty("document", out documentElement))
                {
                    if (updatedDocument.RootElement.TryGetProperty("message", out var messageElement) &&
                        messageElement.ValueKind == JsonValueKind.String)
                    {
                        successMessage = messageElement.GetString();
                    }
                }
                else
                {
                    // Если нет вложенного документа, то весь ответ и есть документ
                    documentElement = updatedDocument.RootElement;
                }

                // Формируем успешный ответ
                var successResponse = new ApiResponse
                {
                    StatusCode = HttpStatusCode.OK,
                    IsSuccess = true,
                    Result = documentElement.ToString(),
                    //SuccessMessage = successMessage
                };
                return Ok(successResponse);
            }
            catch (Exception ex)
            {
                // Формируем ошибочный ответ
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
        /// 
        /// Удаление документа из коллекции Places.
        /// 
        /// Доступно только для авторизованных пользователей с ролями Super Admin или Admin.
        /// 
        /// </summary>
        /// 
        /// <param name="id">Идентификатор документа, который нужно удалить.</param>
        /// 
        /// <returns>Статус операции удаления в формате JSON, обернутый в ApiResponse.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        public async Task<IActionResult> Delete(string id)
        {
            //var business = await _placeService.GetByIdAsync(id);
            //if (business == null)
            //{
            //    _response.StatusCode = HttpStatusCode.NotFound;
            //    _response.IsSuccess = false;
            //    _response.ErrorMessages.Add($"Document with id {id} not found.");
            //    return NotFound(_response);
            //}

            var deleteResult = await _placeService.DeleteAsync(id);

            if (deleteResult == null || deleteResult.RootElement.ValueKind != JsonValueKind.Object)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Unexpected error occurred while deleting the document.");
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }

            if (deleteResult.RootElement.TryGetProperty("success", out var successElement) && successElement.ValueKind == JsonValueKind.False)
            {
                string errorMessage = "Failed to delete the document.";

                if (deleteResult.RootElement.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.String)
                {
                    errorMessage = errorElement.GetString();
                }

                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(errorMessage);
                return BadRequest(_response);
            }

            _response.StatusCode = HttpStatusCode.NoContent;
            _response.IsSuccess = true;
            return Ok(_response);
        }

    }
}
