using Guider.API.MVP.Services;
using Guider.API.MVP.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Text.Json;

namespace Guider.API.MVP.Controllers
{
    [Route("sitemap")] // По аналогии с [Route("places")]
    [ApiController]
    [Produces("application/json")]
    [Tags("Sitemap")] // Группировка в Swagger
    public class SitemapController : ControllerBase
    {
        private readonly SitemapService _sitemapService;

        // Добавляем поле для кеша
        private readonly IMemoryCache _memoryCache;
        // Ключ, по которому будем хранить данные
        //private const string SITEMAP_CACHE_KEY = "sitemap_slugs_list";


        public SitemapController(SitemapService sitemapService, IMemoryCache memoryCache)
        {
            _sitemapService = sitemapService;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Получить данные для генерации sitemap.xml (URL + LastModified).
        /// </summary>
        /// <remarks>
        /// Возвращает JSON массив объектов.
        /// Формат: [{ "url": "slug-name", "lastMod": "2024-12-13" }, ...]
        /// </remarks>
        [HttpGet("places-slugs")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))] // Тип теперь object (динамический JSON)
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<IActionResult> GetPlaceSlugs()
        {
            try
            {
                // Пытаемся получить данные из кеша
                var sitemapData = await _memoryCache.GetOrCreateAsync(SD.SitemapCacheKey, async entry =>
                {
                    // Настройка: хранить 24 часа
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                    // --- Логика получения данных из БД ---
                    var result = await _sitemapService.GetPlaceSlugsAsync();

                    // Проверяем структуру ответа: { "success": true, "data": [...] }
                    if (result.RootElement.TryGetProperty("success", out var successElement) &&
                        successElement.GetBoolean() &&
                        result.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        // ИСПРАВЛЕНИЕ:
                        // Мы не десериализуем в List<string>, так как там теперь объекты.
                        // Мы используем .Clone(), чтобы создать копию JsonElement, 
                        // которая будет жить в кеше после того, как JsonDocument будет уничтожен.
                        return dataElement.Clone();
                    }

                    // Если данных нет или ошибка — возвращаем "undefined" (или пустой массив)
                    // Создаем пустой JsonElement
                    using var emptyDoc = JsonDocument.Parse("[]");
                    return emptyDoc.RootElement.Clone();
                    // -------------------------------------------------------
                });

                return Ok(sitemapData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка sitemap: {ex.Message}" });
            }
        }
    }
}
