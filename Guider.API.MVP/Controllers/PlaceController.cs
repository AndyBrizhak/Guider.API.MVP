using Guider.API.MVP.Models;
using Guider.API.MVP.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Guider.API.MVP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlaceController : ControllerBase
    {
        private readonly PlaceService _placeService;

        public PlaceController(PlaceService placeService)
        {
            _placeService = placeService;
        }

        // 1️⃣ Получить все документы
        [HttpGet]
        public async Task<ActionResult<List<Place>>> GetAll()
        {
            var businesses = await _placeService.GetAllAsync();
            return Ok(businesses);
        }

        // 2️⃣ Получить документ по ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Place>> GetById(string id)
        {
            var business = await _placeService.GetByIdAsync(id);
            if (business == null) return NotFound();
            return Ok(business);
        }

        // 3️⃣ Добавить новый документ
        [HttpPost]
        public async Task<IActionResult> Create(Place business)
        {
            await _placeService.CreateAsync(business);
            return CreatedAtAction(nameof(GetById), new { id = business.Id }, business);
        }

        // 4️⃣ Обновить документ по ID
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, Place updatedBusiness)
        {
            var existingBusiness = await _placeService.GetByIdAsync(id);
            if (existingBusiness == null) return NotFound();

            updatedBusiness.Id = id; // Устанавливаем правильный ID
            await _placeService.UpdateAsync(id, updatedBusiness);
            return NoContent();
        }

        // 5️⃣ Удалить документ по ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var business = await _placeService.GetByIdAsync(id);
            if (business == null) return NotFound();

            await _placeService.DeleteAsync(id);
            return NoContent();
        }
    }
}
