using Guider.API.MVP.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

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

        // TODO: добавить передачу в параметрах полного объекта города
        //[HttpPost]
        //[Route("AddCityToProvince")]
        //public async Task<IActionResult> AddCityToProvince(string provinceName, string cityName)
        //{
        //    var apiResponse = new Models.ApiResponse();
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(provinceName) || string.IsNullOrWhiteSpace(cityName))
        //        {
        //            apiResponse.IsSuccess = false;
        //            apiResponse.StatusCode = HttpStatusCode.BadRequest;
        //            apiResponse.ErrorMessages = new List<string> { "Province name and city name cannot be null or empty." };
        //            return BadRequest(apiResponse);
        //        }
        //        var result = await _citiesService.AddCityToProvinceAsync(provinceName, cityName);
        //        if (result == null)
        //        {
        //            apiResponse.IsSuccess = false;
        //            apiResponse.StatusCode = HttpStatusCode.NotFound;
        //            apiResponse.ErrorMessages = new List<string> { $"Failed to add city: {cityName} to province: {provinceName}." };
        //            return NotFound(apiResponse);
        //        }
        //        apiResponse.IsSuccess = true;
        //        apiResponse.StatusCode = HttpStatusCode.OK;
        //        apiResponse.Result = result;
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
    }
}
