using Guider.API.MVP.Models;
using Guider.API.MVP.Services;
using Guider.API.MVP.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Guider.API.MVP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProvinceController : ControllerBase
    {
        private readonly ProvinceService _provinceService;
        //private readonly ApiResponse _apiResponse;

        public ProvinceController(ProvinceService provinceService/*, ApiResponse apiResponse*/)
        {
            _provinceService = provinceService;
            //_apiResponse = apiResponse;
        }

        [HttpGet]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> GetAll()
        {
            var provinces = await _provinceService.GetAllAsync();
            return Ok(provinces);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> GetById(string id)
        {
            var province = await _provinceService.GetByIdAsync(id);
            if (province == null)
                return NotFound(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { "Province not found" } });

            return Ok(province);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> Create([FromBody] JsonDocument province)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { "Invalid data" } });

            await _provinceService.CreateAsync(province);
            return Ok(province);
        }

        [HttpPut()]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> Update([FromBody] JsonDocument updatedProvince)
        {
            await _provinceService.UpdateAsync(updatedProvince);
            return Ok(updatedProvince);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        public async Task<IActionResult> Delete(string id)
        {
            await _provinceService.DeleteAsync(id);
            return Ok();
        }
    }
}
