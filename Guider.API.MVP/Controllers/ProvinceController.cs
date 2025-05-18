
using Guider.API.MVP.Models;
using Guider.API.MVP.Services;
using Guider.API.MVP.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json.Nodes;
using System;

namespace Guider.API.MVP.Controllers
{
    /// <summary>
    /// Controller for managing provinces.
    /// </summary>
    [Route("")]
    [ApiController]
    public class ProvinceController : ControllerBase
    {
        private readonly ProvinceService _provinceService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProvinceController"/> class.
        /// </summary>
        /// <param name="provinceService">Service for province operations.</param>
        public ProvinceController(ProvinceService provinceService)
        {
            _provinceService = provinceService;
        }

        /// <summary>
        /// Retrieves all provinces in a format compatible with react-admin.
        /// </summary>
        /// <returns>A list of provinces.</returns>
        [HttpGet("provinces")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> GetAll()
        {
            var provincesDocuments = await _provinceService.GetAllAsync();

            // Transform the data format to be compatible with react-admin
            var result = new List<object>();

            foreach (var doc in provincesDocuments)
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

                    string name = string.Empty;
                    if (doc.RootElement.TryGetProperty("name", out var nameElement))
                    {
                        name = nameElement.GetString();
                    }

                    // Формируем объект в формате для react-admin
                    result.Add(new
                    {
                        id,
                        name
                    });
                }
                catch (Exception ex)
                {
                    // В случае ошибки добавляем информацию о ней
                    return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { $"Error processing province: {ex.Message}" } });
                }
            }

            // Add total count header for react-admin pagination
            Response.Headers.Add("X-Total-Count", result.Count.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");

            return Ok(result);
        }

        /// <summary>
        /// Retrieves a province by its ID.
        /// </summary>
        /// <param name="id">The ID of the province.</param>
        /// <returns>The province details.</returns>
        [HttpGet("provinces/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> GetById(string id)
        {
            var provinceDoc = await _provinceService.GetByIdAsync(id);

            // Проверяем, есть ли поле error
            if (provinceDoc.RootElement.TryGetProperty("error", out var errorElement))
            {
                return NotFound(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { errorElement.GetString() } });
            }

            try
            {
                // Получаем ID и имя из документа
                provinceDoc.RootElement.TryGetProperty("_id", out var idElement);
                string docId = idElement.GetProperty("$oid").GetString();

                string name = string.Empty;
                if (provinceDoc.RootElement.TryGetProperty("name", out var nameElement))
                {
                    name = nameElement.GetString();
                }

                // Формируем объект в формате для react-admin
                var result = new
                {
                    id = docId,
                    name
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { $"Error processing province: {ex.Message}" } });
            }
        }

        /// <summary>
        /// Creates a new province.
        /// </summary>
        /// <param name="province">The province data.</param>
        /// <returns>The created province.</returns>
        [HttpPost("provinces")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> Create([FromBody] JsonElement provinceData)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { "Invalid data" } });

            try
            {
                // Проверяем наличие поля name
                if (!provinceData.TryGetProperty("name", out _))
                {
                    return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { "Province name is required" } });
                }

                // Создаем новый JsonDocument для передачи в сервис
                using (var ms = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(ms))
                    {
                        writer.WriteStartObject();
                        writer.WriteString("name", provinceData.GetProperty("name").GetString());
                        writer.WriteEndObject();
                    }

                    ms.Position = 0;
                    using var provinceDoc = JsonDocument.Parse(ms.ToArray());
                    var createdProvinceDoc = await _provinceService.CreateAsync(provinceDoc);

                    // Проверяем, есть ли поле error
                    if (createdProvinceDoc.RootElement.TryGetProperty("error", out var errorElement))
                    {
                        return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { errorElement.GetString() } });
                    }

                    // Получаем ID и имя из созданного документа
                    createdProvinceDoc.RootElement.TryGetProperty("_id", out var idElement);
                    string docId = idElement.GetProperty("$oid").GetString();

                    string name = string.Empty;
                    if (createdProvinceDoc.RootElement.TryGetProperty("name", out var nameElement))
                    {
                        name = nameElement.GetString();
                    }

                    // Формируем объект в формате для react-admin
                    var result = new
                    {
                        id = docId,
                        name
                    };

                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { $"Error creating province: {ex.Message}" } });
            }
        }

        /// <summary>
        /// Updates an existing province.
        /// </summary>
        /// <param name="id">The ID of the province to update.</param>
        /// <param name="provinceData">The updated province data.</param>
        /// <returns>The updated province.</returns>
        [HttpPut("provinces/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> Update(string id, [FromBody] JsonElement provinceData)
        {
            try
            {
                // Проверяем наличие поля name
                if (!provinceData.TryGetProperty("name", out _))
                {
                    return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { "Province name is required" } });
                }

                // Создаем новый JsonDocument для передачи в сервис
                using (var ms = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(ms))
                    {
                        writer.WriteStartObject();
                        writer.WriteString("name", provinceData.GetProperty("name").GetString());
                        writer.WriteEndObject();
                    }

                    ms.Position = 0;
                    using var provinceDoc = JsonDocument.Parse(ms.ToArray());
                    var updatedProvinceDoc = await _provinceService.UpdateAsync(id, provinceDoc);

                    // Проверяем, есть ли поле error
                    if (updatedProvinceDoc.RootElement.TryGetProperty("error", out var errorElement))
                    {
                        var errorMessage = errorElement.GetString();
                        if (errorMessage.Contains("not exist"))
                        {
                            return NotFound(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { errorMessage } });
                        }
                        return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { errorMessage } });
                    }

                    // Получаем ID и имя из обновленного документа
                    updatedProvinceDoc.RootElement.TryGetProperty("_id", out var idElement);
                    string docId = idElement.GetProperty("$oid").GetString();

                    string name = string.Empty;
                    if (updatedProvinceDoc.RootElement.TryGetProperty("name", out var nameElement))
                    {
                        name = nameElement.GetString();
                    }

                    // Формируем объект в формате для react-admin
                    var result = new
                    {
                        id = docId,
                        name
                    };

                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { $"Error updating province: {ex.Message}" } });
            }
        }

        /// <summary>
        /// Deletes a province by its ID.
        /// </summary>
        /// <param name="id">The ID of the province to delete.</param>
        /// <returns>A confirmation of deletion.</returns>
        [HttpDelete("provinces/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _provinceService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { "Province not found or could not be deleted" } });
            }

            // Return the ID for react-admin compatibility
            return Ok(new { id });
        }
    }
}