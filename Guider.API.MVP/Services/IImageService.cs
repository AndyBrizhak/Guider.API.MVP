using System.Text.Json;

namespace Guider.API.MVP.Services
{
    public interface IImageService
    {
        Task<JsonDocument> SaveImageAsync(string imagePath, IFormFile imageFile);
        JsonDocument GetImage(string fullPath);
        JsonDocument DeleteImage(string fullPath);

        // Новый метод для обновления изображения
        Task<string> UpdateImageAsync(string imagePath, IFormFile imageFile, bool createIfNotExists = true);
    }
}
