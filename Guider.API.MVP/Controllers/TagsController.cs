using Guider.API.MVP.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

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

        [HttpPost]
        [Route("CreateTag")]
        public async Task<IActionResult> CreateTag(string typeName, [FromBody] JsonDocument newTagData)

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

                if (newTagData == null)
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { "Tag data cannot be null." };
                    return BadRequest(apiResponse);
                }

                var result = await _tagsService.CreateTagAsync(typeName, newTagData);

                // Проверяем результат выполнения метода сервиса
                if (result.RootElement.GetProperty("IsSuccess").GetBoolean())
                {
                    apiResponse.IsSuccess = true;
                    apiResponse.StatusCode = HttpStatusCode.Created;
                    apiResponse.Result = result;
                    return StatusCode(StatusCodes.Status201Created, apiResponse);
                }
                else
                {
                    // Если в сервисе произошла ошибка, возвращаем сообщение об ошибке оттуда
                    string errorMessage = result.RootElement.GetProperty("Message").GetString();
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { errorMessage };
                    return BadRequest(apiResponse);
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
        [Route("UpdateTag")]
        public async Task<IActionResult> UpdateTag(string typeName, string tagName, [FromBody] System.Text.Json.JsonDocument updateTagData)
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

                if (string.IsNullOrWhiteSpace(tagName))
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { "Tag name cannot be null or empty." };
                    return BadRequest(apiResponse);
                }

                if (updateTagData == null)
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { "Update data cannot be null." };
                    return BadRequest(apiResponse);
                }

                var result = await _tagsService.UpdateTagAsync(typeName, tagName, updateTagData);

                // Проверяем результат выполнения метода сервиса
                if (result.RootElement.GetProperty("IsSuccess").GetBoolean())
                {
                    apiResponse.IsSuccess = true;
                    apiResponse.StatusCode = HttpStatusCode.OK;
                    apiResponse.Result = result;
                    return Ok(apiResponse);
                }
                else
                {
                    // Если в сервисе произошла ошибка, возвращаем сообщение об ошибке оттуда
                    string errorMessage = result.RootElement.GetProperty("Message").GetString();
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { errorMessage };
                    return BadRequest(apiResponse);
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
        [Route("DeleteTag")]
        public async Task<IActionResult> DeleteTag(string typeName, string tagName)
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

                if (string.IsNullOrWhiteSpace(tagName))
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { "Tag name cannot be null or empty." };
                    return BadRequest(apiResponse);
                }

                var result = await _tagsService.DeleteTagAsync(typeName, tagName);

                // Проверяем результат выполнения метода сервиса
                if (result.RootElement.GetProperty("IsSuccess").GetBoolean())
                {
                    apiResponse.IsSuccess = true;
                    apiResponse.StatusCode = HttpStatusCode.OK;
                    apiResponse.Result = result;
                    return Ok(apiResponse);
                }
                else
                {
                    // Если в сервисе произошла ошибка, возвращаем сообщение об ошибке оттуда
                    string errorMessage = result.RootElement.GetProperty("Message").GetString();
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { errorMessage };
                    return BadRequest(apiResponse);
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

        [HttpGet]
        [Route("FindDuplicateTags")]
        public async Task<IActionResult> FindDuplicateTags()
        {
            var apiResponse = new Models.ApiResponse();

            try
            {
                var result = await _tagsService.FindDuplicateTagsAsync();

                // Проверяем результат выполнения метода сервиса
                if (result.RootElement.GetProperty("IsSuccess").GetBoolean())
                {
                    apiResponse.IsSuccess = true;
                    apiResponse.StatusCode = HttpStatusCode.OK;
                    apiResponse.Result = result;
                    return Ok(apiResponse);
                }
                else
                {
                    // Если в сервисе произошла ошибка, возвращаем сообщение об ошибке оттуда
                    string errorMessage = result.RootElement.GetProperty("Message").GetString();
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    apiResponse.ErrorMessages = new List<string> { errorMessage };
                    return BadRequest(apiResponse);
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
