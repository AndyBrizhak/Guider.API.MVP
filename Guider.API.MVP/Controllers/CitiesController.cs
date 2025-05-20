

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
    public class CitiesController : ControllerBase
    {
        private readonly CitiesService _citiesService;

        public CitiesController(CitiesService citiesService)
        {
            _citiesService = citiesService;
        }


       
        /// <summary>
        /// Retrieves all cities in a format compatible with react-admin.
        /// </summary>
        /// <param name="q">Search query for filtering.</param>
        /// <param name="name">Filter by city name.</param>
        /// <param name="province">Filter by province name.</param>
        /// <param name="url">Filter by city URL.</param>
        /// <param name="page">Page number for pagination (1-based).</param>
        /// <param name="perPage">Number of items per page.</param>
        /// <param name="_sort">Field to sort by, default is "name"</param>
        /// <param name="_order">Sort order (ASC or DESC), default is ASC</param>
        /// <returns>A list of cities.</returns>
        [HttpGet("cities")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
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
        /// Retrieves a city by its ID.
        /// </summary>
        /// <param name="cityId">The MongoDB ObjectId of the city to retrieve</param>
        /// <returns>The city details if found, or an appropriate error response.</returns>
        [HttpGet]
        [Route("cities/{cityId}")]
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
        /// Retrieves a list of cities for a given province.
        /// </summary>
        /// <param name="provinceName">The name of the province to retrieve cities for.</param>
        /// <returns>A list of cities in the specified province.</returns>
        //[HttpGet]
        //[Route("CitiesByProvince")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        //public async Task<IActionResult> GetCitiesByProvince(string provinceName)
        //{
        //    var apiResponse = new Models.ApiResponse();

        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(provinceName))
        //        {
        //            apiResponse.IsSuccess = false;
        //            apiResponse.StatusCode = HttpStatusCode.BadRequest;
        //            apiResponse.ErrorMessages = new List<string> { "Province name cannot be null or empty." };
        //            return BadRequest(apiResponse);
        //        }

        //        var result = await _citiesService.GetCitiesByProvinceAsync(provinceName);
        //        bool isSuccess = result.RootElement.GetProperty("IsSuccess").GetBoolean();

        //        if (!isSuccess)
        //        {
        //            string errorMessage = result.RootElement.GetProperty("Message").GetString();
        //            apiResponse.IsSuccess = false;
        //            apiResponse.StatusCode = HttpStatusCode.NotFound;
        //            apiResponse.ErrorMessages = new List<string> { errorMessage };
        //            return NotFound(apiResponse);
        //        }

        //        apiResponse.IsSuccess = true;
        //        apiResponse.StatusCode = HttpStatusCode.OK;
        //        apiResponse.Result = JsonDocument.Parse(result.RootElement.GetProperty("Cities").GetRawText());
        //        return Ok(apiResponse);
        //    }
        //    catch (Exception ex)
        //    {
        //        apiResponse.IsSuccess = false;
        //        apiResponse.StatusCode = HttpStatusCode.InternalServerError;
        //        apiResponse.ErrorMessages = new List<string> { ex.Message };
        //        return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
        //    }
        //}

        /// <summary>
        /// Retrieves a city by its name and the province it belongs to.
        /// </summary>
        /// <param name="provinceName">The name of the province where the city is located.</param>
        /// <param name="cityName">The name of the city to retrieve.</param>
        /// <returns>The city details if found, or an appropriate error response.</returns>
        //[HttpGet]
        //[Route("CityByNameAndProvince")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        //public async Task<IActionResult> GetCityByNameAndProvince(string provinceName, string cityName)
        //{
        //    var apiResponse = new Models.ApiResponse();
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(provinceName))
        //        {
        //            apiResponse.IsSuccess = false;
        //            apiResponse.StatusCode = HttpStatusCode.BadRequest;
        //            apiResponse.ErrorMessages = new List<string> { "Province name cannot be null or empty." };
        //            return BadRequest(apiResponse);
        //        }

        //        if (string.IsNullOrWhiteSpace(cityName))
        //        {
        //            apiResponse.IsSuccess = false;
        //            apiResponse.StatusCode = HttpStatusCode.BadRequest;
        //            apiResponse.ErrorMessages = new List<string> { "City name cannot be null or empty." };
        //            return BadRequest(apiResponse);
        //        }

        //        var result = await _citiesService.GetCityByNameAndProvinceAsync(provinceName, cityName);
        //        bool isSuccess = result.RootElement.GetProperty("IsSuccess").GetBoolean();

        //        if (!isSuccess)
        //        {
        //            string errorMessage = result.RootElement.GetProperty("Message").GetString();
        //            apiResponse.IsSuccess = false;
        //            apiResponse.StatusCode = HttpStatusCode.NotFound;
        //            apiResponse.ErrorMessages = new List<string> { errorMessage };
        //            return NotFound(apiResponse);
        //        }

        //        var cityData = new
        //        {
        //            Province = JsonDocument.Parse(result.RootElement.GetProperty("Province").GetRawText()),
        //            City = JsonDocument.Parse(result.RootElement.GetProperty("City").GetRawText())
        //        };

        //        apiResponse.IsSuccess = true;
        //        apiResponse.StatusCode = HttpStatusCode.OK;
        //        apiResponse.Result = cityData;
        //        return Ok(apiResponse);
        //    }
        //    catch (Exception ex)
        //    {
        //        apiResponse.IsSuccess = false;
        //        apiResponse.StatusCode = HttpStatusCode.InternalServerError;
        //        apiResponse.ErrorMessages = new List<string> { ex.Message };
        //        return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
        //    }
        //}

      
        /// <summary>
        /// Adds a new city.
        /// </summary>
        /// <param name="cityData">A valid JSON document containing the city details.</param>
        /// <remarks>
        /// The cityData parameter must be a valid JSON document with the required city information.
        /// Example:
        /// {
        ///   "name": "Puntarenas",
        ///   "province": "Puntarenas",
        ///   "latitude": 9.9763,
        ///   "longitude": -84.8383
        /// }
        /// </remarks>
        /// <returns>A response indicating the success or failure of the operation.</returns>
        [HttpPost]
        [Route("cities")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> AddCity([FromBody] JsonDocument cityData)
        {
            var apiResponse = new Models.ApiResponse();
            try
            {
                if (cityData == null)
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { "City data cannot be null." };
                    return BadRequest(apiResponse);
                }

                var resultDocument = await _citiesService.AddCityAsync(cityData);

                if (resultDocument == null)
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                    apiResponse.ErrorMessages = new List<string> { "Service returned null result." };
                    return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
                }

                bool isSuccess = resultDocument.RootElement.GetProperty("IsSuccess").GetBoolean();
                string message = resultDocument.RootElement.GetProperty("Message").GetString();

                if (isSuccess)
                {
                    apiResponse.IsSuccess = true;
                    apiResponse.StatusCode = HttpStatusCode.OK;
                    apiResponse.Result = message;
                    return Ok(apiResponse);
                }
                else
                {
                    HttpStatusCode statusCode = message.Contains("not found")
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.BadRequest;

                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = statusCode;
                    apiResponse.ErrorMessages = new List<string> { message };

                    return statusCode == HttpStatusCode.NotFound
                        ? NotFound(apiResponse)
                        : BadRequest(apiResponse);
                }
            }
            catch (Exception ex)
            {
                apiResponse.IsSuccess = false;
                apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                apiResponse.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }

              
        /// <summary>
        /// Updates the details of a city by its ID.
        /// </summary>
        /// <param name="cityId">The MongoDB ObjectId of the city to update.</param>
        /// <param name="cityData">
        /// A valid JSON document containing the updated city details.  
        /// Example:
        /// {
        ///   "name": "New City Name",
        ///   "province": "Province Name",
        ///   "latitude": 9.9281,
        ///   "longitude": -84.0907
        /// }
        /// </param>
        /// <remarks>
        /// The cityData parameter must be a valid JSON document with the city information to update.
        /// The province field will be preserved if not specified in the update data.
        /// </remarks>
        /// <returns>A response indicating the success or failure of the update operation, and the updated city data.</returns>
        [HttpPut]
        [Route("cities/{cityId}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
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
                    // Get the updated city data
                    if (resultDocument.RootElement.TryGetProperty("CityData", out JsonElement cityElement))
                    {
                        // Return both success message and the updated city data
                        return Ok(cityElement);
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Updated city data not found in the result." });
                    }
                }
                else
                {
                    HttpStatusCode statusCode = HttpStatusCode.BadRequest;
                    if (message.Contains("not found"))
                    {
                        statusCode = HttpStatusCode.NotFound;
                    }
                    else if (message.Contains("Invalid city ID format"))
                    {
                        statusCode = HttpStatusCode.BadRequest;
                    }
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
        /// Deletes a city by its ID.
        /// </summary>
        /// <param name="cityId">The ID of the city to delete.</param>
        /// <returns>A response indicating the success or failure of the delete operation.</returns>
        [HttpDelete]
        [Route("cities/{cityId}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        public async Task<IActionResult> RemoveCity(string cityId)
        {
            var apiResponse = new Models.ApiResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(cityId))
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { "City ID cannot be null or empty." };
                    return BadRequest(apiResponse);
                }

                var resultDocument = await _citiesService.RemoveCityAsync(cityId);

                if (resultDocument == null)
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                    apiResponse.ErrorMessages = new List<string> { "Service returned null result." };
                    return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
                }

                bool isSuccess = resultDocument.RootElement.GetProperty("IsSuccess").GetBoolean();
                string message = resultDocument.RootElement.GetProperty("Message").GetString();

                if (isSuccess)
                {
                    apiResponse.IsSuccess = true;
                    apiResponse.StatusCode = HttpStatusCode.OK;
                    apiResponse.Result = message;
                    return Ok(apiResponse);
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

                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = statusCode;
                    apiResponse.ErrorMessages = new List<string> { message };

                    return statusCode == HttpStatusCode.NotFound
                        ? NotFound(apiResponse)
                        : BadRequest(apiResponse);
                }
            }
            catch (Exception ex)
            {
                apiResponse.IsSuccess = false;
                apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                apiResponse.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
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