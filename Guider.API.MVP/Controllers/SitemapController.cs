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
        /// Получить список слагов (URL) для генерации sitemap.xml.
        /// </summary>
        /// <remarks>
        /// Возвращает простой список строк с URL-адресами мест (поле "url" из БД).
        /// Используется frontend-приложением для генерации файла карты сайта.
        /// </remarks>
        /// <returns>Список строк (slugs)</returns>
        [HttpGet("places-slugs")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<string>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<IActionResult> GetPlaceSlugs()
        {
            try
            {
                // Пытаемся получить данные из кеша (или создать их, если нет)
                var slugs = await _memoryCache.GetOrCreateAsync(SD.SitemapCacheKey, async entry =>
                {
                    // Настройка: хранить 168 часа (но мы сбросим вручную раньше, если данные изменятся)
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                    // --- Логика получения данных из БД  ---
                    var result = await _sitemapService.GetPlaceSlugsAsync();

                    if (result.RootElement.TryGetProperty("success", out var successElement) &&
                        successElement.GetBoolean() &&
                        result.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        return JsonSerializer.Deserialize<List<string>>(dataElement.GetRawText());
                    }

                    return new List<string>();
                    // -------------------------------------------------------
                });

                return Ok(slugs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка sitemap: {ex.Message}" });
            }
        }
    }
}
