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

        [HttpGet]
        [Route("GetCitiesByProvince")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
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

                var cities = await _citiesService.GetCitiesByProvinceAsync(provinceName);
                if (cities == null)
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.NotFound;
                    apiResponse.ErrorMessages = new List<string> { $"No cities found for province: {provinceName}." };
                    return NotFound(apiResponse);
                }

                apiResponse.IsSuccess = true;
                apiResponse.StatusCode = HttpStatusCode.OK;
                apiResponse.Result = cities;
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

     
        [HttpGet]
        [Route("GetCityByNameAndProvince")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> GetCityByNameAndProvince(string provinceName, string cityName)
        {
            var apiResponse = new Models.ApiResponse();
            try
            {
                // Проверяем входные параметры
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

                // Вызываем метод сервиса с обоими параметрами
                var result = await _citiesService.GetCityByNameAndProvinceAsync(provinceName, cityName);

                // Парсим результат из JsonDocument
                bool isSuccess = result.RootElement.GetProperty("IsSuccess").GetBoolean();

                if (!isSuccess)
                {
                    string errorMessage = result.RootElement.GetProperty("Message").GetString();
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.NotFound;
                    apiResponse.ErrorMessages = new List<string> { errorMessage };
                    return NotFound(apiResponse);
                }

                // Создаем результирующий объект
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

        [HttpPost]
        [Route("AddCityToProvince")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> AddCityToProvince(string provinceName, [FromBody] JsonDocument cityData)
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

                if (cityData == null)
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { "City data cannot be null." };
                    return BadRequest(apiResponse);
                }

                var resultDocument = await _citiesService.AddCityToProvinceAsync(provinceName, cityData);
                if (resultDocument == null)
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                    apiResponse.ErrorMessages = new List<string> { "Service returned null result." };
                    return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
                }

                
                var resultJson = JsonSerializer.Deserialize<JsonElement>(resultDocument.RootElement.GetRawText());
                bool isSuccess = resultJson.GetProperty("IsSuccess").GetBoolean();
                string message = resultJson.GetProperty("Message").GetString();

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

        [HttpPut]
        [Route("UpdateCityInProvince")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> UpdateCityInProvince(string provinceName, string cityName, [FromBody] JsonDocument cityData)
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

                if (cityData == null)
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { "Updated city data cannot be null." };
                    return BadRequest(apiResponse);
                }

                var resultDocument = await _citiesService.UpdateCityInProvinceAsync(provinceName, cityName, cityData);

                if (resultDocument == null)
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                    apiResponse.ErrorMessages = new List<string> { "Service returned null result." };
                    return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
                }

                var resultJson = JsonSerializer.Deserialize<JsonElement>(resultDocument.RootElement.GetRawText());
                bool isSuccess = resultJson.GetProperty("IsSuccess").GetBoolean();
                string message = resultJson.GetProperty("Message").GetString();

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
                    {
                        statusCode = HttpStatusCode.NotFound;
                    }
                    else
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

        [HttpDelete]
        [Route("RemoveCityFromProvince")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        public async Task<IActionResult> RemoveCityFromProvince(string provinceName, string cityName)
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

                var resultDocument = await _citiesService.RemoveCityFromProvinceAsync(provinceName, cityName);
                if (resultDocument == null)
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                    apiResponse.ErrorMessages = new List<string> { "Service returned null result." };
                    return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
                }

                var resultJson = JsonSerializer.Deserialize<JsonElement>(resultDocument.RootElement.GetRawText());
                bool isSuccess = resultJson.GetProperty("IsSuccess").GetBoolean();
                string message = resultJson.GetProperty("Message").GetString();

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


    }
}
