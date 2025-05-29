
//using Microsoft.AspNetCore.Http;

//namespace Guider.API.MVP.Models
//{
//    public class ImageUpdateRequest
//    {
//        // Old image information
//        public string OldProvince { get; set; }
//        public string? OldCity { get; set; }
//        public string OldPlace { get; set; }
//        public string OldImageName { get; set; }

//        // New image information (all optional)
//        public string? NewProvince { get; set; }
//        public string? NewCity { get; set; }
//        public string? NewPlace { get; set; }
//        public string? NewImageName { get; set; }

//        // New image file (optional)
//        public IFormFile? NewImageFile { get; set; }
//    }
//}


using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Guider.API.MVP.Models
{
    public class ImageUpdateRequest
    {
        // Идентификатор изображения для обновления (обязательный)
        [Required(ErrorMessage = "ID изображения является обязательным")]
        public string Id { get; set; }

        // Новое название изображения (обязательное)
        [Required(ErrorMessage = "Новое название изображения является обязательным")]
        public string NewImageName { get; set; }

        // Новый файл изображения (необязательный - если не указан, обновляется только название)
        public IFormFile? NewImageFile { get; set; }

        // Дополнительные необязательные поля
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? Place { get; set; }
        public string? Description { get; set; }
        public string? Tags { get; set; }
    }
}
