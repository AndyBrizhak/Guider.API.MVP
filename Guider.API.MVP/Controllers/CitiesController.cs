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
    }
}
