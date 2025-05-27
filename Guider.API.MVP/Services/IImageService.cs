using System.Text.Json;

namespace Guider.API.MVP.Services
{
    public interface IImageService
    {
        
        Task<JsonDocument> SaveImageAsync(string province, string? city, string place, string imageName, IFormFile imageFile);

        
        Task<JsonDocument> GetImageByIdAsync(string id);

             
        Task<JsonDocument> DeleteImageByIdAsync(string id);

        Task<JsonDocument> GetImagesAsync(Dictionary<string, string> filter = null);

    }
}
