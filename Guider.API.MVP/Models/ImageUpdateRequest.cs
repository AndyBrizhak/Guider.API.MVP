


using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Guider.API.MVP.Models
{
    public class ImageUpdateRequest
    {
        // Новое название изображения (необязательное - согласно комментариям в контроллере)
        public string? NewImageName { get; set; }

        // Новый файл изображения (необязательный)
        public IFormFile? NewImageFile { get; set; }

        // Дополнительные необязательные поля
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? Place { get; set; }
        public string? Description { get; set; }
        public string? Tags { get; set; }
    }
}