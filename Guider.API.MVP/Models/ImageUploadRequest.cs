
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Guider.API.MVP.Models
{
    public class ImageUploadRequest
    {
        // Название изображения (обязательное)
        [Required(ErrorMessage = "Название изображения является обязательным")]
        public string ImageName { get; set; }

        // Файл изображения (обязательный)
        [Required(ErrorMessage = "Файл изображения является обязательным")]
        public IFormFile ImageFile { get; set; }

        // Дополнительные необязательные поля для организации
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? Place { get; set; }
        public string? Description { get; set; }
        public string? Tags { get; set; }
    }
}








