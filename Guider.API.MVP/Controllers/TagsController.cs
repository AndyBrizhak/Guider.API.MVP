using Guider.API.MVP.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Guider.API.MVP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly TagsService _tagsService;

        public TagsController(TagsService tagsService)
        {
            _tagsService = tagsService;
        }

        [HttpGet]
        [Route("GetTagsByType")]
        public async Task<IActionResult> GetCitiesByProvince(string typeName)
        {
            var apiResponse = new Models.ApiResponse();

            try
            {
                if (string.IsNullOrWhiteSpace(typeName))
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { "Type name cannot be null or empty." };
                    return BadRequest(apiResponse);
                }

                var tags = await _tagsService.GetTagsByTypeAsync(typeName);
                if (tags == null)
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.NotFound;
                    apiResponse.ErrorMessages = new List<string> { $"No tags found for type: {typeName}." };
                    return NotFound(apiResponse);
                }

                apiResponse.IsSuccess = true;
                apiResponse.StatusCode = HttpStatusCode.OK;
                apiResponse.Result = tags;
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
