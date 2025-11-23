using Guider.API.MVP.Services;
using Microsoft.AspNetCore.Mvc;
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

        public SitemapController(SitemapService sitemapService)
        {
            _sitemapService = sitemapService;
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
                var result = await _sitemapService.GetPlaceSlugsAsync();

                // Проверяем структуру ответа на наличие success = true
                if (result.RootElement.TryGetProperty("success", out var successElement) &&
                    successElement.GetBoolean())
                {
                    // Если успех, достаем данные ("data")
                    if (result.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        // Десериализуем в список строк и возвращаем 200 OK
                        var slugs = JsonSerializer.Deserialize<List<string>>(dataElement.GetRawText());
                        return Ok(slugs);
                    }

                    // Если поля data нет, возвращаем пустой список
                    return Ok(new List<string>());
                }
                else
                {
                    // Если ошибка, достаем сообщение
                    var errorMessage = "Unknown error occurred";
                    if (result.RootElement.TryGetProperty("error", out var errorElement))
                    {
                        errorMessage = errorElement.GetString();
                    }

                    return BadRequest(new { error = errorMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении данных для sitemap: {ex.Message}" });
            }
        }
    }
}
