



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

            var (tags, totalCount, errorMessage) = await _tagsService.GetTagsAsync(filter);

            // Проверяем на ошибки
            if (!string.IsNullOrEmpty(errorMessage))
            {
                return BadRequest(new ApiResponse { IsSuccess = false, ErrorMessages = new List<string> { errorMessage } });
            }

            // Add total count header for react-admin pagination
            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");

            return Ok(tags);
        }
    }
}