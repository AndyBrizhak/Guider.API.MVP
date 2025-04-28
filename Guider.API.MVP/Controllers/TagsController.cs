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
    public class TagsController : ControllerBase
    {
        private readonly TagsService _tagsService;

        public TagsController(TagsService tagsService)
        {
            _tagsService = tagsService;
        }

        /// <summary>
        /// Retrieves tags by their type.
        /// </summary>
        /// <param name="typeName">The name of the tag type.</param>
        /// <returns>A list of tags matching the specified type.</returns>
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

        /// <summary>
        /// Creates a new tag for a specified type.
        /// </summary>
        /// <param name="typeName">The name of the tag type.</param>
        /// <param name="newTagData">The data for the new tag in JSON format.</param>
        /// <returns>The created tag or an error message.</returns>
        [HttpPost]
        [Route("CreateTag")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
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

                if (result.RootElement.GetProperty("IsSuccess").GetBoolean())
                {
                    apiResponse.IsSuccess = true;
                    apiResponse.StatusCode = HttpStatusCode.Created;
                    apiResponse.Result = result;
                    return StatusCode(StatusCodes.Status201Created, apiResponse);
                }
                else
                {
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

        /// <summary>
        /// Updates an existing tag for a specified type.
        /// </summary>
        /// <param name="typeName">The name of the tag type.</param>
        /// <param name="tagName">The name of the tag to update.</param>
        /// <param name="updateTagData">The updated data for the tag in JSON format.</param>
        /// <returns>The updated tag or an error message.</returns>
        [HttpPut]
        [Route("UpdateTag")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> UpdateTag(string typeName, string tagName, [FromBody] JsonDocument updateTagData)
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

                if (result.RootElement.GetProperty("IsSuccess").GetBoolean())
                {
                    apiResponse.IsSuccess = true;
                    apiResponse.StatusCode = HttpStatusCode.OK;
                    apiResponse.Result = result;
                    return Ok(apiResponse);
                }
                else
                {
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

        /// <summary>
        /// Deletes a tag for a specified type.
        /// </summary>
        /// <param name="typeName">The name of the tag type.</param>
        /// <param name="tagName">The name of the tag to delete.</param>
        /// <returns>A success message or an error message.</returns>
        [HttpDelete]
        [Route("DeleteTag")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
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

                if (result.RootElement.GetProperty("IsSuccess").GetBoolean())
                {
                    apiResponse.IsSuccess = true;
                    apiResponse.StatusCode = HttpStatusCode.OK;
                    apiResponse.Result = result;
                    return Ok(apiResponse);
                }
                else
                {
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

        /// <summary>
        /// Finds duplicate tags in the system.
        /// </summary>
        /// <returns>A list of duplicate tags or an error message.</returns>
        [HttpGet]
        [Route("FindDuplicateTags")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> FindDuplicateTags()
        {
            var apiResponse = new Models.ApiResponse();

            try
            {
                var result = await _tagsService.FindDuplicateTagsAsync();

                if (result.RootElement.GetProperty("IsSuccess").GetBoolean())
                {
                    apiResponse.IsSuccess = true;
                    apiResponse.StatusCode = HttpStatusCode.OK;
                    apiResponse.Result = result;
                    return Ok(apiResponse);
                }
                else
                {
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
