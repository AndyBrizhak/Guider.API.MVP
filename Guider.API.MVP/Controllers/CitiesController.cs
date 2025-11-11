
using Guider.API.MVP.Models;
using Guider.API.MVP.Services;
using Guider.API.MVP.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net;
using System.Text.Json;

namespace Guider.API.MVP.Controllers
{
    [Route("")]
    [ApiController]
    [Produces("application/json")] // Указываем, что контроллер всегда возвращает JSON
    [Tags("Cities")] // Группируем все эндпоинты в "Cities" (Города)
    public class CitiesController : ControllerBase
    {
        private readonly CitiesService _citiesService;
        private readonly PlaceService _placeService; // СЕРВИС ПЛЕЙСОВ

        public CitiesController(CitiesService citiesService, PlaceService placeService)
        {
            _citiesService = citiesService;
            _placeService = placeService; // ИНИЦИАЛИЗАЦИя ПЛЕЙСОВ
        }



        /// <summary>
        /// Получает постраничный список городов с фильтрацией и сортировкой.
        /// </summary>
        /// <remarks>
        /// Эндпоинт совместим с React-Admin. 
        /// Включает заголовок 'X-Total-Count' в ответе для пагинации.
        /// </remarks>
        /// <param name="q">Общий поисковый запрос (фильтрует по нескольким полям).</param>
        /// <param name="name">Фильтр по названию города (частичное совпадение).</param>
        /// <param name="province">Фильтр по названию провинции (частичное совпадение).</param>
        /// <param name="url">Фильтр по URL города (частичное совпадение).</param>
        /// <param name="page">Номер страницы (начиная с 1). По умолчанию 1.</param>
        /// <param name="perPage">Количество элементов на странице. По умолчанию 10.</param>
        /// <param name="_sort">Поле для сортировки. По умолчанию "name".</param>
        /// <param name="_order">Порядок сортировки (ASC или DESC). По умолчанию ASC.</param>
        /// <returns>Список объектов городов и заголовок X-Total-Count.</returns>
        [HttpGet("cities")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<object>))] // Успешный ответ
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))] // Ошибка валидации
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Не авторизован
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // Нет прав
        public async Task<IActionResult> GetCities(
            [FromQuery] string q = null,
            [FromQuery] string name = null,
            [FromQuery] string province = null,
            [FromQuery] string url = null,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 10,
            [FromQuery] string _sort = "name",
            [FromQuery] string _order = "ASC")
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

            if (!string.IsNullOrEmpty(province))
            {
                filter["province"] = province;
            }

            if (!string.IsNullOrEmpty(url))
            {
                filter["url"] = url;
            }

            // Добавляем параметры сортировки
            filter["_sort"] = _sort;
            filter["_order"] = _order;

            // Add pagination parameters
            filter["page"] = page.ToString();
            filter["perPage"] = perPage.ToString();

            var (citiesDocuments, totalCount) = await _citiesService.GetCitiesAsync(filter);

            // Transform the data format to be compatible with react-admin
            var result = new List<object>();

            foreach (var doc in citiesDocuments)
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

                    // Получаем провинцию из документа (если есть)
                    string docProvince = string.Empty;
                    if (doc.RootElement.TryGetProperty("province", out var provinceElement))
                    {
                        docProvince = provinceElement.GetString();
                    }

                    // Получаем URL из документа (если есть)
                    string docUrl = string.Empty;
                    if (doc.RootElement.TryGetProperty("url", out var urlElement))
                    {
                        docUrl = urlElement.GetString();
                    }

                    // Получаем геоданные из документа (если есть)
                    double? longitude = null;
                    double? latitude = null;

                    if (doc.RootElement.TryGetProperty("location", out var locationElement))
                    {
                        if (locationElement.TryGetProperty("coordinates", out var coordinatesElement) &&
                            coordinatesElement.GetArrayLength() >= 2)
                        {
                            longitude = coordinatesElement[0].GetDouble();
                            latitude = coordinatesElement[1].GetDouble();
                        }
                    }

                    // Формируем объект в формате для react-admin
                    result.Add(new
                    {
                        id,
                        name = docName,
                        province = docProvince,
                        url = docUrl,
                        location = new
                        {
                            longitude,
                            latitude
                        }
                    });
                }
                catch (Exception ex)
                {
                    // В случае ошибки добавляем информацию о ней
                    return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { $"Error processing city: {ex.Message}" } });
                }
            }

            // Add total count header for react-admin pagination
            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");

            return Ok(result);
        }

        /// <summary>
        /// Получает один город по его ID.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="cityId">MongoDB ObjectId города (в виде строки).</param>
        /// <returns>
        /// Объект города в формате:
        /// <br/>
        /// {
        /// <br/>
        /// &nbsp;&nbsp;"id": "string",
        /// <br/>
        /// &nbsp;&nbsp;"name": "string",
        /// <br/>
        /// &nbsp;&nbsp;"province": "string",
        /// <br/>
        /// &nbsp;&nbsp;"url": "string",
        /// <br/>
        /// &nbsp;&nbsp;"location": { "longitude": 0.0, "latitude": 0.0 }
        /// <br/>
        /// }
        /// </returns>
        [HttpGet]
        [Route("cities/{cityId}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))] // Успешный ответ
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))] // Неверный ID
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Не авторизован
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // Нет прав
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))] // Город не найден
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))] // Ошибка сервера
        public async Task<IActionResult> GetCityById(string cityId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cityId))
                {
                    return BadRequest(new { message = "City ID cannot be null or empty." });
                }

                var result = await _citiesService.GetCityByIdAsync(cityId);

                bool isSuccess = result.RootElement.GetProperty("IsSuccess").GetBoolean();

                if (!isSuccess)
                {
                    string errorMessage = result.RootElement.GetProperty("Message").GetString();

                    if (errorMessage.Contains("not found"))
                        return NotFound(new { message = errorMessage });
                    if (errorMessage.Contains("Invalid city ID format"))
                        return BadRequest(new { message = errorMessage });

                    return BadRequest(new { message = errorMessage });
                }

                var cityData = JsonDocument.Parse(result.RootElement.GetProperty("City").GetRawText());

                var cityResponse = new
                {
                    id = result.RootElement.GetProperty("Id").GetString(),
                    name = cityData.RootElement.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : string.Empty,
                    province = cityData.RootElement.TryGetProperty("province", out var provinceElement) ? provinceElement.GetString() : string.Empty,
                    url = cityData.RootElement.TryGetProperty("url", out var urlElement) ? urlElement.GetString() : string.Empty,
                    location = new
                    {
                        longitude = cityData.RootElement.TryGetProperty("longitude", out var longElement) ? longElement.GetDouble() :
                                   (cityData.RootElement.TryGetProperty("location", out var locElement) &&
                                    locElement.TryGetProperty("coordinates", out var coordElement) &&
                                    coordElement.GetArrayLength() >= 1 ? coordElement[0].GetDouble() : (double?)null),

                        latitude = cityData.RootElement.TryGetProperty("latitude", out var latElement) ? latElement.GetDouble() :
                                  (cityData.RootElement.TryGetProperty("location", out var locElement2) &&
                                   locElement2.TryGetProperty("coordinates", out var coordElement2) &&
                                   coordElement2.GetArrayLength() >= 2 ? coordElement2[1].GetDouble() : (double?)null)
                    }
                };

                return Ok(cityResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }


        /// <summary>
        /// Добавляет новый город.
        /// </summary>
        /// <remarks>
        /// Принимает произвольный JSON-объект.
        /// Доступ: Super_Admin, Admin, Manager.
        /// <br/>
        /// <b>Пример запроса (JSON):</b>
        /// <br/>
        /// {   
        /// <br/>
        /// &nbsp;&nbsp;"name": "Новый Город",
        /// <br/>
        /// &nbsp;&nbsp;"province": "Название Провинции",
        /// <br/>
        /// &nbsp;&nbsp;"latitude": 9.9281,
        /// <br/>
        /// &nbsp;&nbsp;"longitude": -84.0907,
        /// <br/>
        /// &nbsp;&nbsp;"url": "new-city-url"
        /// <br/>
        /// }
        /// <br/>
        /// </remarks>
        /// <param name="cityData">JSON документ с данными нового города.</param>
        /// <returns>Полный объект созданного города (включая ID) или сообщение об ошибке.</returns>
        [HttpPost]
        [Route("cities")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [Consumes("application/json")] // Указываем, что ожидаем JSON
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))] // Успешное создание (возвращает созданный объект)
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))] // Неверные данные
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Не авторизован
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // Нет прав
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))] // Ошибка сервера
        public async Task<IActionResult> AddCity([FromBody] JsonDocument cityData)
        {
            if (cityData == null)
            {
                return BadRequest(new { message = "City data cannot be null." });
            }

            // Позволяем создавать город даже с неполными данными (например, только name или только province)
            // Валидация на обязательные поля не проводится здесь, сервис сам обработает логику и вернет ошибку, если нужно

            var resultDocument = await _citiesService.AddCityAsync(cityData);
            if (resultDocument == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Service returned null result." });
            }
            bool isSuccess = resultDocument.RootElement.GetProperty("IsSuccess").GetBoolean();
            string message = resultDocument.RootElement.GetProperty("Message").GetString();
            if (isSuccess)
            {
                // Получаем полные данные о новом городе из поля Data
                if (resultDocument.RootElement.TryGetProperty("Data", out JsonElement cityDataElement))
                {
                    return Ok(cityDataElement);
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
        /// Обновляет существующий город по ID.
        /// </summary>
        /// <remarks>
        /// Принимает JSON с полями, которые нужно обновить.
        /// Доступ: Super_Admin, Admin, Manager.
        /// <br/>
        /// <b>Пример запроса (JSON):</b>
        /// <br/>
        /// {
        /// <br/>
        /// &nbsp;&nbsp;"name": "Обновленное Название",
        /// <br/>
        /// &nbsp;&nbsp;"latitude": 10.0
        /// <br/>
        /// }
        /// </remarks>
        /// <param name="cityId">MongoDB ObjectId города для обновления.</param>
        /// <param name="cityData">JSON документ с обновляемыми данными.</param>
        /// <returns>Полный объект обновленного города или сообщение об ошибке.</returns>
        [HttpPut]
        [Route("cities/{cityId}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        [Consumes("application/json")] // Указываем, что ожидаем JSON
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))] // Успешное обновление
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))] // Неверный ID или данные
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Не авторизован
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // Нет прав
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))] // Город не найден
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))] // Ошибка сервера
        public async Task<IActionResult> UpdateCity(string cityId, [FromBody] JsonDocument cityData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cityId))
                {
                    return BadRequest(new { message = "City ID cannot be null or empty." });
                }

                if (cityData == null)
                {
                    return BadRequest(new { message = "Updated city data cannot be null." });
                }

                var resultDocument = await _citiesService.UpdateCityAsync(cityId, cityData);

                if (resultDocument == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Service returned null result." });
                }

                bool isSuccess = resultDocument.RootElement.GetProperty("IsSuccess").GetBoolean();
                string message = resultDocument.RootElement.GetProperty("Message").GetString();

                if (isSuccess)
                {
                    // Получаем данные обновленного города из поля Data
                    if (resultDocument.RootElement.TryGetProperty("Data", out JsonElement cityDataElement))
                    {
                        return Ok(cityDataElement);
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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }


        /// <summary>
        /// Удаляет город по ID.
        /// </summary>
        /// <remarks>
        /// Доступ: Super_Admin, Admin.
        /// </remarks>
        /// <param name="cityId">MongoDB ObjectId города для удаления.</param>
        /// <returns>Сообщение об успехе или ошибке.</returns>
        [HttpDelete]
        [Route("cities/{cityId}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))] // Успешное удаление
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))] // Неверный ID
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Не авторизован
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // Нет прав
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))] // Город не найден
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))] // Ошибка сервера
        public async Task<IActionResult> RemoveCity(string cityId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cityId))
                {
                    return BadRequest(new { message = "City ID cannot be null or empty." });
                }

                var resultDocument = await _citiesService.RemoveCityAsync(cityId);

                if (resultDocument == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Service returned null result." });
                }

                bool isSuccess = resultDocument.RootElement.GetProperty("IsSuccess").GetBoolean();
                string message = resultDocument.RootElement.GetProperty("Message").GetString();

                if (isSuccess)
                {
                    return Ok(new { message });
                }
                else
                {
                    HttpStatusCode statusCode;

                    if (message.Contains("not found"))
                        statusCode = HttpStatusCode.NotFound;
                    else if (message.Contains("Invalid ID format"))
                        statusCode = HttpStatusCode.BadRequest;
                    else
                        statusCode = HttpStatusCode.BadRequest;

                    var errorObj = new { message };

                    return statusCode == HttpStatusCode.NotFound
                        ? NotFound(errorObj)
                        : BadRequest(errorObj);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        
        /// <summary>
        /// Получает список активных городов из коллекции Places с опциональной фильтрацией.
        /// </summary>
        /// <remarks>
        /// Возвращает уникальные названия городов, которые присутствуют в адресах мест в коллекции Places.
        /// Города отсортированы по алфавиту.
        /// <br/>
        /// <br/>
        /// <b>Примеры использования:</b>
        /// <br/>
        /// - GET /cities/active - все города
        /// <br/>
        /// - GET /cities/active?province=Guanacaste - города в провинции Guanacaste
        /// <br/>
        /// - GET /cities/active?category=to-eat - города с ресторанами
        /// <br/>
        /// - GET /cities/active?province=Guanacaste&amp;category=to-eat - города с ресторанами в Guanacaste
        /// </remarks>
        /// <param name="category">Опциональный фильтр по категории (например, "to-eat").</param>
        /// <param name="province">Опциональный фильтр по провинции (например, "Guanacaste").</param>
        /// <returns>Массив названий городов.</returns>
        [HttpGet("cities/active")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<string>))] // Успешный ответ
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))] // Ошибка сервера
        public async Task<IActionResult> GetActiveCities(
            [FromQuery] string category = null,
            [FromQuery] string province = null)
        {
            try
            {
                var result = await _placeService.GetActiveCitiesAsync(category, province);

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

                // Извлекаем массив городов
                var citiesData = result.RootElement.GetProperty("data");
                var citiesList = new List<string>();

                foreach (var city in citiesData.EnumerateArray())
                {
                    citiesList.Add(city.GetString());
                }

                // Добавляем заголовок с общим количеством городов
                Response.Headers.Add("X-Total-Count", citiesList.Count.ToString());
                Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");


                // Возвращаем просто массив строк
                return Ok(citiesList);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = $"An error occurred: {ex.Message}" });
            }
        }

    }

    // Extension method to help combine JSON objects
    public static class ObjectExtensions
    {
        public static T With<T>(this object obj, object values) where T : class
        {
            var result = Activator.CreateInstance<T>();
            var objProps = obj.GetType().GetProperties();
            var valuesProps = values.GetType().GetProperties();

            foreach (var objProp in objProps)
            {
                var resultProp = typeof(T).GetProperty(objProp.Name);
                if (resultProp != null && resultProp.CanWrite)
                {
                    resultProp.SetValue(result, objProp.GetValue(obj, null), null);
                }
            }

            foreach (var valuesProp in valuesProps)
            {
                var resultProp = typeof(T).GetProperty(valuesProp.Name);
                if (resultProp != null && resultProp.CanWrite)
                {
                    resultProp.SetValue(result, valuesProp.GetValue(values, null), null);
                }
            }

            return result;
        }

        public static object With(this object obj, object values)
        {
            var objDict = obj.GetType().GetProperties()
                .ToDictionary(x => x.Name, x => x.GetValue(obj, null));

            var valuesDict = values.GetType().GetProperties()
                .ToDictionary(x => x.Name, x => x.GetValue(values, null));

            foreach (var item in valuesDict)
            {
                objDict[item.Key] = item.Value;
            }

            return objDict;
        }
    }
}