using Guider.API.MVP.Models;
using Guider.API.MVP.Services;
using Guider.API.MVP.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Guider.API.MVP.Controllers
{
    /// <summary>
    /// Controller for managing provinces.
    /// </summary>
    [Route("api/[controller]")]
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
        /// Retrieves all provinces.
        /// </summary>
        /// <returns>A list of provinces.</returns>
        [HttpGet]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> GetAll()
        {
            var provinces = await _provinceService.GetAllAsync();
            return Ok(provinces);
        }

        /// <summary>
        /// Retrieves a province by its ID.
        /// </summary>
        /// <param name="id">The ID of the province.</param>
        /// <returns>The province details.</returns>
        [HttpGet("{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> GetById(string id)
        {
            var province = await _provinceService.GetByIdAsync(id);
            if (province == null)
                return NotFound(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { "Province not found" } });

            return Ok(province);
        }

        /// <summary>
        /// Creates a new province.
        /// </summary>
        /// <param name="province">The province data.</param>
        /// <returns>The created province.</returns>
        [HttpPost]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> Create([FromBody] JsonDocument province)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { "Invalid data" } });

            await _provinceService.CreateAsync(province);
            return Ok(province);
        }

        /// <summary>
        /// Updates an existing province.
        /// </summary>
        /// <param name="updatedProvince">The updated province data.</param>
        /// <returns>The updated province.</returns>
        [HttpPut()]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> Update([FromBody] JsonDocument updatedProvince)
        {
            await _provinceService.UpdateAsync(updatedProvince);
            return Ok(updatedProvince);
        }

        /// <summary>
        /// Deletes a province by its ID.
        /// </summary>
        /// <param name="id">The ID of the province to delete.</param>
        /// <returns>A confirmation of deletion.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        public async Task<IActionResult> Delete(string id)
        {
            await _provinceService.DeleteAsync(id);
            return Ok();
        }
    }
}
