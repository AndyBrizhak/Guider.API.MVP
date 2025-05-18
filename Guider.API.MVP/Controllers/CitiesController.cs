//using Guider.API.MVP.Services;
//using Guider.API.MVP.Utility;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using System.Net;
//using System.Text.Json;

//namespace Guider.API.MVP.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class CitiesController : ControllerBase
//    {
//        private readonly CitiesService _citiesService;

//        public CitiesController(CitiesService citiesService)
//        {
//            _citiesService = citiesService;
//        }

//        /// <summary>
//        /// Retrieves a list of cities for a given province.
//        /// </summary>
//        /// <param name="provinceName">The name of the province to retrieve cities for.</param>
//        /// <returns>A list of cities in the specified province.</returns>
//        [HttpGet]
//        [Route("GetCitiesByProvince")]
//        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
//        public async Task<IActionResult> GetCitiesByProvince(string provinceName)
//        {
//            var apiResponse = new Models.ApiResponse();

//            try
//            {
//                if (string.IsNullOrWhiteSpace(provinceName))
//                {
//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
//                    apiResponse.ErrorMessages = new List<string> { "Province name cannot be null or empty." };
//                    return BadRequest(apiResponse);
//                }

//                var cities = await _citiesService.GetCitiesByProvinceAsync(provinceName);
//                if (cities == null)
//                {
//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = HttpStatusCode.NotFound;
//                    apiResponse.ErrorMessages = new List<string> { $"No cities found for province: {provinceName}." };
//                    return NotFound(apiResponse);
//                }

//                apiResponse.IsSuccess = true;
//                apiResponse.StatusCode = HttpStatusCode.OK;
//                apiResponse.Result = cities;
//                return Ok(apiResponse);
//            }
//            catch (Exception ex)
//            {
//                apiResponse.IsSuccess = false;
//                apiResponse.StatusCode = HttpStatusCode.InternalServerError;
//                apiResponse.ErrorMessages = new List<string> { ex.Message };
//                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
//            }
//        }


//        /// <summary>
//        /// Retrieves a city by its name and the province it belongs to.
//        /// </summary>
//        /// <param name="provinceName">The name of the province where the city is located.</param>
//        /// <param name="cityName">The name of the city to retrieve.</param>
//        /// <returns>The city details if found, or an appropriate error response.</returns>
//        [HttpGet]
//        [Route("GetCityByNameAndProvince")]
//        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
//        public async Task<IActionResult> GetCityByNameAndProvince(string provinceName, string cityName)
//        {
//            var apiResponse = new Models.ApiResponse();
//            try
//            {
//                // Проверяем входные параметры
//                if (string.IsNullOrWhiteSpace(provinceName))
//                {
//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
//                    apiResponse.ErrorMessages = new List<string> { "Province name cannot be null or empty." };
//                    return BadRequest(apiResponse);
//                }

//                if (string.IsNullOrWhiteSpace(cityName))
//                {
//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
//                    apiResponse.ErrorMessages = new List<string> { "City name cannot be null or empty." };
//                    return BadRequest(apiResponse);
//                }

//                // Вызываем метод сервиса с обоими параметрами
//                var result = await _citiesService.GetCityByNameAndProvinceAsync(provinceName, cityName);

//                // Парсим результат из JsonDocument
//                bool isSuccess = result.RootElement.GetProperty("IsSuccess").GetBoolean();

//                if (!isSuccess)
//                {
//                    string errorMessage = result.RootElement.GetProperty("Message").GetString();
//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = HttpStatusCode.NotFound;
//                    apiResponse.ErrorMessages = new List<string> { errorMessage };
//                    return NotFound(apiResponse);
//                }

//                // Создаем результирующий объект
//                var cityData = new
//                {
//                    Province = JsonDocument.Parse(result.RootElement.GetProperty("Province").GetRawText()),
//                    City = JsonDocument.Parse(result.RootElement.GetProperty("City").GetRawText())
//                };

//                apiResponse.IsSuccess = true;
//                apiResponse.StatusCode = HttpStatusCode.OK;
//                apiResponse.Result = cityData;
//                return Ok(apiResponse);
//            }
//            catch (Exception ex)
//            {
//                apiResponse.IsSuccess = false;
//                apiResponse.StatusCode = HttpStatusCode.InternalServerError;
//                apiResponse.ErrorMessages = new List<string> { ex.Message };
//                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
//            }
//        }


//        /// <summary>
//        /// Adds a new city to the specified province.
//        /// </summary>
//        /// <param name="provinceName">The name of the province where the city will be added.</param>
//        /// <param name="cityData">A valid JSON document containing the city details.</param>
//        /// <remarks>
//        /// The cityData parameter must be a valid JSON document with the required city information.
//        /// </remarks>
//        /// <returns>A response indicating the success or failure of the operation.</returns>
//        [HttpPost]
//        [Route("AddCityToProvince")]
//        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
//        public async Task<IActionResult> AddCityToProvince(string provinceName, [FromBody] JsonDocument cityData)
//        {
//            var apiResponse = new Models.ApiResponse();
//            try
//            {
//                if (string.IsNullOrWhiteSpace(provinceName))
//                {
//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
//                    apiResponse.ErrorMessages = new List<string> { "Province name cannot be null or empty." };
//                    return BadRequest(apiResponse);
//                }

//                if (cityData == null)
//                {
//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
//                    apiResponse.ErrorMessages = new List<string> { "City data cannot be null." };
//                    return BadRequest(apiResponse);
//                }

//                var resultDocument = await _citiesService.AddCityToProvinceAsync(provinceName, cityData);
//                if (resultDocument == null)
//                {
//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = HttpStatusCode.InternalServerError;
//                    apiResponse.ErrorMessages = new List<string> { "Service returned null result." };
//                    return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
//                }


//                var resultJson = JsonSerializer.Deserialize<JsonElement>(resultDocument.RootElement.GetRawText());
//                bool isSuccess = resultJson.GetProperty("IsSuccess").GetBoolean();
//                string message = resultJson.GetProperty("Message").GetString();

//                if (isSuccess)
//                {
//                    apiResponse.IsSuccess = true;
//                    apiResponse.StatusCode = HttpStatusCode.OK;
//                    apiResponse.Result = message;
//                    return Ok(apiResponse);
//                }
//                else
//                {

//                    HttpStatusCode statusCode = message.Contains("not found")
//                        ? HttpStatusCode.NotFound
//                        : HttpStatusCode.BadRequest;

//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = statusCode;
//                    apiResponse.ErrorMessages = new List<string> { message };

//                    return statusCode == HttpStatusCode.NotFound
//                        ? NotFound(apiResponse)
//                        : BadRequest(apiResponse);
//                }
//            }
//            catch (Exception ex)
//            {
//                apiResponse.IsSuccess = false;
//                apiResponse.StatusCode = HttpStatusCode.InternalServerError;
//                apiResponse.ErrorMessages = new List<string> { ex.Message };
//                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
//            }
//        }

//        /// <summary>
//        /// Updates the details of a city in the specified province.
//        /// </summary>
//        /// <param name="provinceName">The name of the province where the city is located.</param>
//        /// <param name="cityName">The name of the city to update.</param>
//        /// <param name="cityData">
//        /// A valid JSON document containing the updated city details.  
//        /// Example:
//        /// {
//        ///   "Population": 500000,
//        ///   "Area": 300.5,
//        ///   "IsCapital": true
//        /// }
//        /// </param>
//        /// <remarks>
//        /// The cityData parameter must be a valid JSON document with the required city information.
//        /// </remarks>
//        /// <returns>A response indicating the success or failure of the update operation.</returns>
//        [HttpPut]
//        [Route("UpdateCityInProvince")]
//        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
//        public async Task<IActionResult> UpdateCityInProvince(string provinceName, string cityName, [FromBody] JsonDocument cityData)
//        {
//            var apiResponse = new Models.ApiResponse();
//            try
//            {
//                if (string.IsNullOrWhiteSpace(provinceName))
//                {
//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
//                    apiResponse.ErrorMessages = new List<string> { "Province name cannot be null or empty." };
//                    return BadRequest(apiResponse);
//                }

//                if (string.IsNullOrWhiteSpace(cityName))
//                {
//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
//                    apiResponse.ErrorMessages = new List<string> { "City name cannot be null or empty." };
//                    return BadRequest(apiResponse);
//                }

//                if (cityData == null)
//                {
//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
//                    apiResponse.ErrorMessages = new List<string> { "Updated city data cannot be null." };
//                    return BadRequest(apiResponse);
//                }

//                var resultDocument = await _citiesService.UpdateCityInProvinceAsync(provinceName, cityName, cityData);

//                if (resultDocument == null)
//                {
//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = HttpStatusCode.InternalServerError;
//                    apiResponse.ErrorMessages = new List<string> { "Service returned null result." };
//                    return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
//                }

//                var resultJson = JsonSerializer.Deserialize<JsonElement>(resultDocument.RootElement.GetRawText());
//                bool isSuccess = resultJson.GetProperty("IsSuccess").GetBoolean();
//                string message = resultJson.GetProperty("Message").GetString();

//                if (isSuccess)
//                {
//                    apiResponse.IsSuccess = true;
//                    apiResponse.StatusCode = HttpStatusCode.OK;
//                    apiResponse.Result = message;
//                    return Ok(apiResponse);
//                }
//                else
//                {
//                    HttpStatusCode statusCode;

//                    if (message.Contains("not found"))
//                    {
//                        statusCode = HttpStatusCode.NotFound;
//                    }
//                    else
//                    {
//                        statusCode = HttpStatusCode.BadRequest;
//                    }

//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = statusCode;
//                    apiResponse.ErrorMessages = new List<string> { message };

//                    return statusCode == HttpStatusCode.NotFound
//                        ? NotFound(apiResponse)
//                        : BadRequest(apiResponse);
//                }
//            }
//            catch (Exception ex)
//            {
//                apiResponse.IsSuccess = false;
//                apiResponse.StatusCode = HttpStatusCode.InternalServerError;
//                apiResponse.ErrorMessages = new List<string> { ex.Message };
//                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
//            }

//        }

//        /// <summary>
//        /// 
//        /// Deletes a city from the specified province.
//        /// 
//        /// </summary>
//        [HttpDelete]
//        [Route("RemoveCityFromProvince")]
//        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
//        public async Task<IActionResult> RemoveCityFromProvince(string provinceName, string cityName)
//        {
//            var apiResponse = new Models.ApiResponse();
//            try
//            {
//                if (string.IsNullOrWhiteSpace(provinceName))
//                {
//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
//                    apiResponse.ErrorMessages = new List<string> { "Province name cannot be null or empty." };
//                    return BadRequest(apiResponse);
//                }

//                if (string.IsNullOrWhiteSpace(cityName))
//                {
//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
//                    apiResponse.ErrorMessages = new List<string> { "City name cannot be null or empty." };
//                    return BadRequest(apiResponse);
//                }

//                var resultDocument = await _citiesService.RemoveCityFromProvinceAsync(provinceName, cityName);
//                if (resultDocument == null)
//                {
//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = HttpStatusCode.InternalServerError;
//                    apiResponse.ErrorMessages = new List<string> { "Service returned null result." };
//                    return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
//                }

//                var resultJson = JsonSerializer.Deserialize<JsonElement>(resultDocument.RootElement.GetRawText());
//                bool isSuccess = resultJson.GetProperty("IsSuccess").GetBoolean();
//                string message = resultJson.GetProperty("Message").GetString();

//                if (isSuccess)
//                {
//                    apiResponse.IsSuccess = true;
//                    apiResponse.StatusCode = HttpStatusCode.OK;
//                    apiResponse.Result = message;
//                    return Ok(apiResponse);
//                }
//                else
//                {
//                    HttpStatusCode statusCode = message.Contains("not found")
//                        ? HttpStatusCode.NotFound
//                        : HttpStatusCode.BadRequest;

//                    apiResponse.IsSuccess = false;
//                    apiResponse.StatusCode = statusCode;
//                    apiResponse.ErrorMessages = new List<string> { message };

//                    return statusCode == HttpStatusCode.NotFound
//                        ? NotFound(apiResponse)
//                        : BadRequest(apiResponse);
//                }
//            }
//            catch (Exception ex)
//            {
//                apiResponse.IsSuccess = false;
//                apiResponse.StatusCode = HttpStatusCode.InternalServerError;
//                apiResponse.ErrorMessages = new List<string> { ex.Message };
//                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
//            }
//        }


//    }
//}


using Guider.API.MVP.Services;
using Guider.API.MVP.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace Guider.API.MVP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CitiesController : ControllerBase
    {
        private readonly CitiesService _citiesService;

        public CitiesController(CitiesService citiesService)
        {
            _citiesService = citiesService;
        }

        /// <summary>
        /// Retrieves a list of cities for a given province.
        /// </summary>
        /// <param name="provinceName">The name of the province to retrieve cities for.</param>
        /// <returns>A list of cities in the specified province.</returns>
        [HttpGet]
        [Route("GetCitiesByProvince")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> GetCitiesByProvince(string provinceName)
        {
            var apiResponse = new Models.ApiResponse();

            try
            {
                if (string.IsNullOrWhiteSpace(provinceName))
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { "Province name cannot be null or empty." };
                    return BadRequest(apiResponse);
                }

                var result = await _citiesService.GetCitiesByProvinceAsync(provinceName);
                bool isSuccess = result.RootElement.GetProperty("IsSuccess").GetBoolean();

                if (!isSuccess)
                {
                    string errorMessage = result.RootElement.GetProperty("Message").GetString();
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.NotFound;
                    apiResponse.ErrorMessages = new List<string> { errorMessage };
                    return NotFound(apiResponse);
                }

                apiResponse.IsSuccess = true;
                apiResponse.StatusCode = HttpStatusCode.OK;
                apiResponse.Result = JsonDocument.Parse(result.RootElement.GetProperty("Cities").GetRawText());
                return Ok(apiResponse);
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
        /// Retrieves a city by its name and the province it belongs to.
        /// </summary>
        /// <param name="provinceName">The name of the province where the city is located.</param>
        /// <param name="cityName">The name of the city to retrieve.</param>
        /// <returns>The city details if found, or an appropriate error response.</returns>
        [HttpGet]
        [Route("GetCityByNameAndProvince")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> GetCityByNameAndProvince(string provinceName, string cityName)
        {
            var apiResponse = new Models.ApiResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(provinceName))
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { "Province name cannot be null or empty." };
                    return BadRequest(apiResponse);
                }

                if (string.IsNullOrWhiteSpace(cityName))
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { "City name cannot be null or empty." };
                    return BadRequest(apiResponse);
                }

                var result = await _citiesService.GetCityByNameAndProvinceAsync(provinceName, cityName);
                bool isSuccess = result.RootElement.GetProperty("IsSuccess").GetBoolean();

                if (!isSuccess)
                {
                    string errorMessage = result.RootElement.GetProperty("Message").GetString();
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.NotFound;
                    apiResponse.ErrorMessages = new List<string> { errorMessage };
                    return NotFound(apiResponse);
                }

                var cityData = new
                {
                    Province = JsonDocument.Parse(result.RootElement.GetProperty("Province").GetRawText()),
                    City = JsonDocument.Parse(result.RootElement.GetProperty("City").GetRawText())
                };

                apiResponse.IsSuccess = true;
                apiResponse.StatusCode = HttpStatusCode.OK;
                apiResponse.Result = cityData;
                return Ok(apiResponse);
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
        [Route("AddCity")]
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
        /// <returns>A response indicating the success or failure of the update operation.</returns>
        [HttpPut]
        [Route("UpdateCity/{cityId}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> UpdateCity(string cityId, [FromBody] JsonDocument cityData)
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

                if (cityData == null)
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { "Updated city data cannot be null." };
                    return BadRequest(apiResponse);
                }

                var resultDocument = await _citiesService.UpdateCityAsync(cityId, cityData);

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
                    HttpStatusCode statusCode = HttpStatusCode.BadRequest;

                    if (message.Contains("not found"))
                    {
                        statusCode = HttpStatusCode.NotFound;
                    }
                    else if (message.Contains("Invalid city ID format"))
                    {
                        statusCode = HttpStatusCode.BadRequest;
                    }

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
        /// Deletes a city by its ID.
        /// </summary>
        /// <param name="cityId">The ID of the city to delete.</param>
        /// <returns>A response indicating the success or failure of the delete operation.</returns>
        [HttpDelete]
        [Route("RemoveCity/{cityId}")]
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