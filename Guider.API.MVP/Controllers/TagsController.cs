

using Guider.API.MVP.Models;
using Guider.API.MVP.Services;
using Guider.API.MVP.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace Guider.API.MVP.Controllers
{
    [Route("")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly TagsService _tagsService;

        public TagsController(TagsService tagsService)
        {
            _tagsService = tagsService;
        }

        [HttpGet("tags")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin + "," + SD.Role_Manager)]
        public async Task<IActionResult> GetTags(
            [FromQuery] string q = null,
            [FromQuery] string name_en = null,
            [FromQuery] string name_sp = null,
            [FromQuery] string url = null,
            [FromQuery] string type = null,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 10,
            [FromQuery] string _sort = "name_en",
            [FromQuery] string _order = "ASC")
        {
            // Создаем объект фильтра для передачи в сервис
            var filter = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(q))
            {
                filter["q"] = q;
            }

            if (!string.IsNullOrEmpty(name_en))
            {
                filter["name_en"] = name_en;
            }

            if (!string.IsNullOrEmpty(name_sp))
            {
                filter["name_sp"] = name_sp;
            }

            if (!string.IsNullOrEmpty(url))
            {
                filter["url"] = url;
            }

            if (!string.IsNullOrEmpty(type))
            {
                filter["type"] = type;
            }

            // Добавляем параметры сортировки
            filter["_sort"] = _sort;
            filter["_order"] = _order;

            // Add pagination parameters
            filter["page"] = page.ToString();
            filter["perPage"] = perPage.ToString();

            var (tagsDocuments, totalCount) = await _tagsService.GetTagsAsync(filter);

            // Transform the data format to be compatible with react-admin
            var result = new List<object>();

            foreach (var doc in tagsDocuments)
            {
                try
                {
                    // Проверяем, есть ли поле error
                    if (doc.RootElement.TryGetProperty("error", out _))
                    {
                        // Если есть ошибка, возвращаем её
                        return BadRequest(doc);
                    }

                    // Получаем ID из документа
                    doc.RootElement.TryGetProperty("_id", out var idElement);
                    string id = idElement.GetProperty("$oid").GetString();

                    // Получаем английское название
                    string nameEn = string.Empty;
                    if (doc.RootElement.TryGetProperty("name_en", out var nameEnElement))
                    {
                        nameEn = nameEnElement.GetString();
                    }

                    // Получаем испанское название
                    string nameSp = string.Empty;
                    if (doc.RootElement.TryGetProperty("name_sp", out var nameSpElement))
                    {
                        nameSp = nameSpElement.GetString();
                    }

                    // Получаем URL из документа
                    string docUrl = string.Empty;
                    if (doc.RootElement.TryGetProperty("url", out var urlElement))
                    {
                        docUrl = urlElement.GetString();
                    }

                    // Получаем тип тега
                    string tagType = string.Empty;
                    if (doc.RootElement.TryGetProperty("type", out var typeElement))
                    {
                        tagType = typeElement.GetString();
                    }

                    // Формируем объект в формате для react-admin
                    result.Add(new
                    {
                        id,
                        name_en = nameEn,
                        name_sp = nameSp,
                        url = docUrl,
                        type = tagType
                    });
                }
                catch (Exception ex)
                {
                    // В случае ошибки добавляем информацию о ней
                    return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { $"Error processing tag: {ex.Message}" } });
                }
            }

            // Add total count header for react-admin pagination
            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");

            return Ok(result);
        }
    }
}