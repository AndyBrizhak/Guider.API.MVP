using Guider.API.MVP.Models;
using Guider.API.MVP.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
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

        /// <summary>
        /// Получить все документы из коллекции Places
        /// </summary>
        /// <returns>Список документов в формате JSON</returns>      
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var places = await _placeService.GetAllAsync();
            return Ok(places.ToJson());
        }

        /// <summary>
        /// Получить документ по ID
        /// </summary>
        /// <param name="id">Идентификатор документа</param>
        /// <returns>Документ в формате JSON</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetById(string id)
        {
            var place = await _placeService.GetByIdAsync(id);
            if (place == null) return NotFound();
            var placeJson = place.ToJson();
            return Ok(placeJson);
        }

        //[HttpGet("search/{web}")]
        //public async Task<ActionResult<string>> GetPlaceByWeb([FromRoute] string web)
        //{
        //    if (string.IsNullOrEmpty(web))
        //    {
        //        return BadRequest("Web parameter is required.");
        //    }

        //    var place = await _placeService.GetPlaceByWebAsync(web);

        //    if (place == null)
        //    {
        //        return NotFound($"No place found with web: {web}");
        //    }

        //    var placeJson = place.ToJson();
        //    return Ok(placeJson);
        //}

        /// <summary>
        /// Получить документ по ID из заголовка
        /// </summary>
        /// <param name="web">Веб параметр</param>
        /// <param name="id">Идентификатор документа</param>
        /// <returns>Документ в формате JSON</returns>
        [HttpGet("place/id")]
        public async Task<ActionResult<string>> GetPlaceByIdFromHeader(
            [FromQuery] string web,
             [FromQuery] string id)
        {
            var result = await _placeService.GetPlaceByIdFromHeaderAsync(id);

            if (result == null)
            {
                return NotFound(new { message = "Place not found" });
            }

            return Ok(result.ToJson());
        }

        // 3️⃣ Добавить новый документ
        //[HttpPost]
        //public async Task<IActionResult> Create(BsonDocument place)
        //{
        //    await _placeService.CreateAsync(place);

        //    // Получаем сгенерированный _id из BsonDocument
        //    var id = place.Contains("_id") ? place["_id"].ToString() : null;

        //    if (id == null)
        //    {
        //        return BadRequest("Не удалось получить _id нового документа.");
        //    }

        //    return CreatedAtAction(nameof(GetById), new { id }, place);
        //}

        // 4️⃣ Обновить документ по ID
        //[HttpPut("{id}")]
        //public async Task<IActionResult> Update(string id, BsonDocument updatedPlace)
        //{
        //    var id = updatedPlace.Contains("_id") ? updatedPlace["_id"].ToString() : null;

        //    if (id == null)
        //    {
        //        return BadRequest("Не удалось получить _id нового документа.");
        //    }

        //    var result = await _placeService.UpdateAsync(id, updatedPlace);

        //    if (result.ModifiedCount == 0)
        //    {
        //        return NotFound($"Документ с id {id} не найден.");
        //    }

        //    return Ok(updatedPlace);
        //}

        // 5️⃣ Удалить документ по ID
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> Delete(string id)
        //{
        //    var business = await _placeService.GetByIdAsync(id);
        //    if (business == null) return NotFound();

        //    await _placeService.DeleteAsync(id);
        //    return NoContent();
        //}

        /// <summary>
        /// Получить ближайшие места
        /// </summary>
        /// <param name="lat">Широта</param>
        /// <param name="lng">Долгота</param>
        /// <param name="maxDistance">Максимальное расстояние</param>
        /// <returns>Список ближайших мест в формате JSON</returns>
        [HttpGet("geonear")]
        public async Task<ActionResult<string>> GetNearbyPlaces(
        [FromHeader(Name = "X-Latitude")] decimal lat,
        [FromHeader(Name = "X-Longitude")] decimal lng,
        [FromHeader(Name = "X-Max-Distance")] int maxDistance)
        {
            var jsonResult = await _placeService.GetPlacesNearbyAsync(lat, lng, maxDistance);
            return Content(jsonResult, "application/json");
        }


        /// <summary>
        /// Получение списка Places для карты на главной странице, с выводом идентификатора,  
        /// категории, названия и координат каждого элепмента. Для локаллизации по гео можно использовать условный геометрический центр Коста-Рики, это lat 9.5 и long -84. Для мобильной версии может быть использованы данные по гео из API мобильного устройства.
        /// </summary>
        /// <param name="lat">Широта в decimal</param>
        /// <param name="lng">Долгота в decimal</param>
        /// <param name="radiusMeters">Радиус в метрах</param>
        /// <param name="limit">Лимит выводимых объектов на карте в integer</param>
        /// <returns>Список ближайших мест в формате JSON</returns>
        [HttpGet("geonearlimit")]
        public async Task<ActionResult<string>> GetNearbyPlacesCenter(
        [FromHeader] decimal lat,
        [FromHeader] decimal lng,
        [FromHeader] int radiusMeters = 500000,
        [FromHeader] int limit = 200)
        {
            var jsonResult = await _placeService.GetNearbyPlacesAsyncCenter(lat, lng, radiusMeters, limit);
            return Content(jsonResult, "application/json");
        }

        /// <summary>
        /// Получить места по категории и тегам
        /// </summary>
        /// <param name="lat">Широта</param>
        /// <param name="lng">Долгота</param>
        /// <param name="maxDistanceMeters">Максимальное расстояние в метрах</param>
        /// <param name="category">Категория</param>
        /// <param name="filterTags">Теги для фильтрации</param>
        /// <returns>Список мест в формате JSON</returns>
        [HttpGet("geo/category/tags")]
        public async Task<IActionResult> GetPlacesNearbyByCategoryByTagsAsync(
            [FromQuery] decimal lat,
            [FromQuery] decimal lng,
            [FromQuery] int maxDistanceMeters,
        [FromQuery] string category,
        [FromQuery] List<string>? filterTags = null)
        {
            var places = await _placeService.GetPlacesNearbyByCategoryByTagsAsyncAsync(lat, lng, maxDistanceMeters, category, filterTags);
            return Ok(places);
        }
    }
}
