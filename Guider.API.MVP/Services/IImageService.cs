
using System.Text.Json;

namespace Guider.API.MVP.Services
{
    public interface IImageService
    {
        // Сохранение изображения с упрощенными параметрами
        Task<JsonDocument> SaveImageAsync(string imageName, IFormFile imageFile,
            string? province = null, string? city = null, string? place = null,
            string? description = null, string? tags = null);

        // Обновление изображения
        //Task<JsonDocument> UpdateImageAsync(string id, string newImageName,
        //    IFormFile? newImageFile = null, string? province = null,
        //    string? city = null, string? place = null, string? description = null,
        //    string? tags = null);

        // Получение изображения по ID
        Task<JsonDocument> GetImageByIdAsync(string id);

        // Удаление изображения по ID
        Task<JsonDocument> DeleteImageByIdAsync(string id);

        // Получение списка изображений
        Task<JsonDocument> GetImagesAsync(Dictionary<string, string> filter = null);
    }
}
