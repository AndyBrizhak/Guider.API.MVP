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

        /// <summary>
        /// Получить все документы из коллекции Places
        /// </summary>
        /// <returns>Список документов в формате JSON</returns>      
        //[HttpGet]
        //public async Task<IActionResult> GetAll()
        //{
        //    var places = await _placeService.GetAllAsync();
        //    return Ok(places.ToJson());
        //}


        /// <summary>
        /// Получить по странично все документы коллекции,
        /// с размером не более 20 документов
        /// </summary>
        /// <param name="pageNumber">Номер страницы</param>
        /// <param name="pageSize">Размер страницы, не более 20</param>
        /// <returns></returns>
        [HttpGet("paged")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
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
        public async Task<ActionResult<string>> GetAllPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            // Проверяем, что pageNumber больше 0
            if (pageNumber < 1)
            {
                pageNumber = 1; // Устанавливаем значение по умолчанию
            }
            // Ограничиваем максимальный размер страницы до 20
            pageSize = Math.Min(pageSize, 20);

            var places = await _placeService.GetAllPagedAsync(pageNumber, pageSize);
            if (places == null || places.Count == 0)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("No places found.");
                return NotFound(_response);
            }
            return Ok(places.ToJson());
        }

        /// <summary>
        /// Получить документ по ID
        /// </summary>
        /// <param name="id">Идентификатор документа</param>
        /// <returns>Документ в формате JSON</returns>
        [HttpGet("{id}")]
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

        //[HttpGet("search/{web}")]
        //public async Task<ActionResult<string>> GetPlaceByWeb([FromRoute] string web)
        //{
        //    if (string.IsNullOrEmpty(web))
        //    {
        //        return BadRequest("Web parameter is required.");
        //    }

        //    var place = await _placeService.GetPlaceByWebAsync(web);

        //    if (place == null)
        //    {
        //        return NotFound($"No place found with web: {web}");
        //    }

        //    var placeJson = place.ToJson();
        //    return Ok(placeJson);
        //}

        /// <summary>
        /// Получить документ по web и ID из заголовка
        /// </summary>
        /// <param name="web">Веб параметр</param>
        /// <param name="id">Идентификатор документа</param>
        /// <returns>Документ в формате JSON</returns>
        [HttpGet("place/id")]
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
                    var validationResponse = new ApiResponse // Renamed to avoid conflict  
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        IsSuccess = false,
                        ErrorMessages = new List<string> { "Invalid input. Expected a JSON object." }
                    };
                    return BadRequest(validationResponse);
                }

                // Отправляем в сервис и получаем полный документ  
                var createdDocument = await _placeService.CreateAsync(jsonDocument);

                // Формируем успешный ответ  
                var successResponse = new ApiResponse // Renamed to avoid conflict  
                {
                    StatusCode = HttpStatusCode.Created,
                    IsSuccess = true,
                    Result = createdDocument.ToJson()
                };

                // Возвращаем созданный документ внутри ApiResponse  
                return CreatedAtAction(nameof(GetById),
                                     new { id = createdDocument["_id"].ToString() },
                                     successResponse);
            }
            catch (Exception ex)
            {
                // Формируем ошибочный ответ  
                var errorResponse = new ApiResponse // Renamed to avoid conflict  
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    IsSuccess = false,
                    ErrorMessages = new List<string> { ex.Message }
                };
                return BadRequest(errorResponse);
            }
        }



        // 4️⃣ Обновить документ по ID
        //[HttpPut("{id}")]
        //public async Task<IActionResult> Update(string id, BsonDocument updatedPlace)
        //{
        //    var id = updatedPlace.Contains("_id") ? updatedPlace["_id"].ToString() : null;

        //    if (id == null)
        //    {
        //        return BadRequest("Не удалось получить _id нового документа.");
        //    }

        //    var result = await _placeService.UpdateAsync(id, updatedPlace);

        //    if (result.ModifiedCount == 0)
        //    {
        //        return NotFound($"Документ с id {id} не найден.");
        //    }

        //    return Ok(updatedPlace);
        //}

        // 5️⃣ Удалить документ по ID
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> Delete(string id)
        //{
        //    var business = await _placeService.GetByIdAsync(id);
        //    if (business == null) return NotFound();

        //    await _placeService.DeleteAsync(id);
        //    return NoContent();
        //}

        ///// <summary>
        ///// Получить ближайшие места
        ///// </summary>
        ///// <param name="lat">Широта</param>
        ///// <param name="lng">Долгота</param>
        ///// <param name="maxDistance">Максимальное расстояние</param>
        ///// <returns>Список ближайших мест в формате JSON</returns>
        //[HttpGet("geonear")]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.InternalServerError)]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Forbidden)]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.ServiceUnavailable)]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.GatewayTimeout)]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.RequestTimeout)]
        //public async Task<ActionResult<string>> GetNearbyPlaces(
        //[FromHeader(Name = "X-Latitude")] decimal lat,
        //[FromHeader(Name = "X-Longitude")] decimal lng,
        //[FromHeader(Name = "X-Max-Distance")] int maxDistance)
        //{
        //    //var jsonResult = await _placeService.GetPlacesNearbyAsync(lat, lng, maxDistance);
        //    //return Content(jsonResult, "application/json");
        //    var places = await _placeService.GetPlacesNearbyAsync(lat, lng, maxDistance);
        //    if (places == null || places.Count ==0)
        //    {
        //        _response.StatusCode = HttpStatusCode.NotFound;
        //        _response.IsSuccess = false;
        //        _response.ErrorMessages.Add($"No places found within {maxDistance} meters.");
        //        return NotFound(_response);
        //    }
        //    return Content(places.ToJson(), "application/json");

        //}


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
        /// Получение списка Places для карты на главной странице, с выводом идентификатора,  
        /// категории, названия и координат каждого элепмента. 
        /// Для локаллизации по гео можно использовать условный геометрический центр Коста-Рики, 
        /// это lat 9.5 и long -84. 
        /// Для мобильной версии может быть использованы данные по гео из API мобильного устройства.
        /// </summary>
        /// <param name="lat">Широта в decimal</param>
        /// <param name="lng">Долгота в decimal</param>
        /// <param name="radiusMeters">Радиус в метрах</param>
        /// <param name="limit">Лимит выводимых объектов на карте в integer</param>
        /// <returns>Список ближайших мест в формате JSON</returns>
        //[HttpGet("geonearlimit")]
        //public async Task<ActionResult<string>> GetNearbyPlacesCenter(
        //[FromHeader] decimal lat,
        //[FromHeader] decimal lng,
        //[FromHeader] int radiusMeters = 500000,
        //[FromHeader] int limit = 200)
        //{
        //    //var jsonResult = await _placeService.GetNearbyPlacesAsyncCenter(lat, lng, radiusMeters, limit);
        //    //return Content(jsonResult, "application/json");
        //    var places = await _placeService.GetNearbyPlacesAsyncCenter(lat, lng, radiusMeters, limit);
        //    if (places == null)
        //    {
        //        _response.StatusCode = HttpStatusCode.NotFound;
        //        _response.IsSuccess = false;
        //        _response.ErrorMessages.Add($"No places found within {radiusMeters} meters.");
        //        return NotFound(_response);
        //    }
        //    return Content(places, "application/json");

        //}

        /// <summary>
        /// Получить места по категории и тегам
        /// </summary>
        /// <param name="lat">Широта</param>
        /// <param name="lng">Долгота</param>
        /// <param name="maxDistanceMeters">Максимальное расстояние в метрах</param>
        /// <param name="category">Категория</param>
        /// <param name="filterTags">Теги для фильтрации</param>
        /// <returns>Список мест в формате JSON</returns>
        //[HttpGet("geo/category/tags")]
        //public async Task<IActionResult> GetPlacesNearbyByCategoryByTagsAsync(
        //    [FromQuery] decimal lat,
        //    [FromQuery] decimal lng,
        //    [FromQuery] int maxDistanceMeters,
        //    [FromQuery] string category,
        //    [FromQuery] List<string>? filterTags = null)
        //{
        //    var places = await _placeService.GetPlacesNearbyByCategoryByTagsAsyncAsync(lat, lng, maxDistanceMeters, category, filterTags);
        //    if (places == null || places.Count == 0)
        //    {
        //        _response.StatusCode = HttpStatusCode.NotFound;
        //        _response.IsSuccess = false;
        //        _response.ErrorMessages.Add($"No places found within filters.");
        //        return NotFound(_response);
        //    }
        //    return Content(places.ToJson(), "application/json");
        //}

        ///// <summary>
        ///// Получить ближайшие места со строгим вхождением подстроки
        ///// </summary>
        ///// <param name="lat">Широта</param>
        ///// <param name="lng">Долгота</param>
        ///// <param name="maxDistanceMeters">Максимальное расстояние в метрах</param>
        ///// <param name="limit">Лимит результатов (не менее 10 и не более 100)</param>
        ///// <param name="searchText">Текст для поиска</param>
        ///// <returns>Список ближайших мест в формате JSON</returns>
        //[HttpGet("geonear/search")]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.InternalServerError)]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Forbidden)]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.ServiceUnavailable)]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.GatewayTimeout)]
        //[ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.RequestTimeout)]
        //public async Task<ActionResult<string>> GetPlacesNearbyWithTextSearch(
        //    [FromQuery] decimal lat,
        //    [FromQuery] decimal lng,
        //    [FromQuery] int maxDistanceMeters,
        //    [FromQuery] int limit,
        //    [FromQuery] string searchText)
        //{

        //    // Проверка и корректировка значения limit
        //    if (limit <= 0)
        //    {
        //        limit = 10; 
        //    }
        //    else if (limit > 100)
        //    {
        //        limit = 100; 
        //    }

        //    if (string.IsNullOrWhiteSpace(searchText))
        //    {
        //        _response.StatusCode = HttpStatusCode.BadRequest;
        //        _response.IsSuccess = false;
        //        _response.ErrorMessages.Add("Search text cannot be empty.");
        //        return BadRequest(_response);
        //    }

        //    // Проверка на наличие пробелов в строке    
        //    if (searchText.Contains(" "))
        //    {
        //        _response.StatusCode = HttpStatusCode.BadRequest;
        //        _response.IsSuccess = false;
        //        _response.ErrorMessages.Add("Search text cannot contain spaces.");
        //        return BadRequest(_response);
        //    }


        //    var places = await _placeService.GetPlacesNearbyWithTextSearchAsync(lat, lng, maxDistanceMeters, limit, searchText);
        //    if (places == null || places.Count == 0)
        //    {
        //        _response.StatusCode = HttpStatusCode.NotFound;
        //        _response.IsSuccess = false;
        //        _response.ErrorMessages.Add($"No places found within filters.");
        //        return NotFound(_response);
        //    }

        //    return Content(places.ToJson(), "application/json");
        //}

        /// <summary>
        /// Получить ближайшие места со строгим вхождением подстроки
        /// </summary>
        /// <param name="lat">Широта</param>
        /// <param name="lng">Долгота</param>
        /// <param name="maxDistanceMeters">Максимальное расстояние в метрах</param>
        /// <param name="limit">Лимит результатов (не менее 10 и не более 100)</param>
        /// <param name="searchText">Текст для поиска</param>
        /// <param name="isOpen">Учитывать ли расписание работы</param>
        /// <returns>Список ближайших мест в формате JSON</returns>
        [HttpGet("geonear/search")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.GatewayTimeout)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.RequestTimeout)]
        public async Task<ActionResult<string>> GetPlacesNearbyWithTextSearch(
            [FromQuery] decimal lat,
            [FromQuery] decimal lng,
            [FromQuery] int maxDistanceMeters,
            [FromQuery] int limit,
            [FromQuery] string searchText,
            [FromQuery] bool isOpen = false)
        {
            try
            {
                // Проверка и корректировка значения limit
                if (limit <= 0)
                {
                    limit = 10;
                }
                else if (limit > 100)
                {
                    limit = 100;
                }
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Search text cannot be empty.");
                    return BadRequest(_response);
                }
                // Проверка на наличие пробелов в строке    
                if (searchText.Contains(" "))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Search text cannot contain spaces.");
                    return BadRequest(_response);
                }

                // Используем новую версию метода, которая учитывает параметр isOpen
                var places = await _placeService.GetPlacesNearbyWithTextSearchAsync(lat, lng, maxDistanceMeters, limit, searchText, isOpen);

                if (places == null || places.Count == 0)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add($"No places found within filters{(isOpen ? " that are currently open" : "")}.");
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
            [FromQuery] decimal lat,
            [FromQuery] decimal lng,
            [FromQuery] int maxDistanceMeters,
            [FromQuery] int limit,
            [FromQuery] List<string>? filterKeywords,
            [FromQuery] bool isOpen = false)
        {
            // Проверка и корректировка значения limit
            if (limit < 1)
            {
                limit = 1;
            }
            else if (limit > 100)
            {
                limit = 100;
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
        public async Task<ActionResult<string>> GetPlacesWithAllKeywords(
            [FromQuery] decimal lat,
            [FromQuery] decimal lng,
            [FromQuery] int maxDistanceMeters,
            [FromQuery] int limit,
            [FromQuery] List<string>? filterKeywords,
            [FromQuery] bool isOpen = false)
        {
            // Проверка и корректировка значения limit
            if (limit < 1)
            {
                limit = 1;
            }
            else if (limit > 100)
            {
                limit = 100;
            }

            // Проверка filterKeywords на null и пустой список
            if (filterKeywords == null || !filterKeywords.Any())
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Filter keywords list is empty or not provided.");
                return BadRequest(_response);
            }

            List<BsonDocument> places;

            // Используем соответствующую перегрузку метода в зависимости от значения isOpen
            if (isOpen)
            {
                places = await _placeService.GetPlacesWithAllKeywordsAsync(lat, lng, maxDistanceMeters, limit, filterKeywords, isOpen);
            }
            else
            {
                places = await _placeService.GetPlacesWithAllKeywordsAsync(lat, lng, maxDistanceMeters, limit, filterKeywords);
            }

            if (places == null || places.Count == 0)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add($"No places found with all the provided keywords{(isOpen ? " that are currently open" : "")}.");
                return NotFound(_response);
            }

            return Content(places.ToJson(), "application/json");
        }
    }
}
