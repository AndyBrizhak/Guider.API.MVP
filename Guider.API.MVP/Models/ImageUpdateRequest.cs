namespace Guider.API.MVP.Models
{
    public class ImageUpdateRequest
    {
        // Путь в формате "провинция/город/название" (локальная часть URL)
        public string ImagePath { get; set; }

        // Файл изображения
        public IFormFile ImageFile { get; set; }

        // Создать, если не существует
        public bool CreateIfNotExists { get; set; } = true;
    }
}
