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
        private readonly Models.ApiResponse _apiResponse;

        public CitiesController(CitiesService citiesService, Models.ApiResponse apiResponse)
        {
            _citiesService = citiesService;
            _apiResponse = apiResponse;
        }

        [HttpGet]
        [Route("GetCitiesByProvince")]
        public async Task<IActionResult> GetCitiesByProvince(string provinceName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(provinceName))
                {
                    _apiResponse.IsSuccess = false;
                    _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    _apiResponse.ErrorMessages = new List<string> { "Province name cannot be null or empty." };
                    return BadRequest(_apiResponse);
                }

                var cities = await _citiesService.GetCitiesByProvinceAsync(provinceName);
                if (cities == null)
                {
                    _apiResponse.IsSuccess = false;
                    _apiResponse.StatusCode = HttpStatusCode.NotFound;
                    _apiResponse.ErrorMessages = new List<string> { $"No cities found for province: {provinceName}." };
                    return NotFound(_apiResponse);
                }

                _apiResponse.IsSuccess = true;
                _apiResponse.StatusCode = HttpStatusCode.OK;
                _apiResponse.Result = cities;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.IsSuccess = false;
                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                _apiResponse.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _apiResponse);
            }
        }
 
    }
}
