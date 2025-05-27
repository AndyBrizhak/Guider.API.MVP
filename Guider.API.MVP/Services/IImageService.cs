using System.Text.Json;

namespace Guider.API.MVP.Services
{
    public interface IImageService
    {
        // Основные операции с изображениями
        Task<JsonDocument> SaveImageAsync(string province, string? city, string place, string imageName, IFormFile imageFile);

        JsonDocument GetImage(string province, string? city, string place, string imageName);

        Task<JsonDocument> GetImageByIdAsync(string id);

        JsonDocument GetImagesList(int page, int pageSize);

     
        Task<JsonDocument> DeleteImageByIdAsync(string id);

    }
}
