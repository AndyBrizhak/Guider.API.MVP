using Guider.API.MVP.Services;
using Guider.API.MVP.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Guider.API.MVP.Controllers
{
    [Route("sitemap")]
    [ApiController]
    [Produces("application/json")]
    [Tags("Sitemap")]
    public class SitemapController : ControllerBase
    {
        private readonly SitemapService _sitemapService;
        private readonly IMemoryCache _memoryCache;

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
        /// Формат: [{ "url": "slug", "lastMod": "2024-12-13" }, ...]
        /// </remarks>
        [HttpGet("places-slugs")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<IActionResult> GetSitemapData()
        {
            try
            {
                // Пытаемся получить данные из кеша
                // Используем тот же ключ, что и раньше, или новый, если хотите сбросить старый кеш
                var sitemapData = await _memoryCache.GetOrCreateAsync(SD.SitemapCacheKey, async entry =>
                {
                    // Настройка: хранить 24 часа
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                    // --- ИЗМЕНЕНИЕ ЗДЕСЬ: Вызываем новый метод сервиса ---
                    var result = await _sitemapService.GetFullSitemapDataAsync();
                    // -----------------------------------------------------

                    // Проверяем структуру ответа: { "success": true, "data": [...] }
                    if (result.RootElement.TryGetProperty("success", out var successElement) &&
                        successElement.GetBoolean() &&
                        result.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        // ВАЖНО: Клонируем данные, чтобы они сохранились в кеше после уничтожения JsonDocument
                        return dataElement.Clone();
                    }

                    // Если данных нет или ошибка — возвращаем пустой массив
                    using var emptyDoc = JsonDocument.Parse("[]");
                    return emptyDoc.RootElement.Clone();
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