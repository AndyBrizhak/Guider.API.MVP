using System.Text.Json;

namespace Guider.API.MVP.Services
{
    public interface IImageService
    {
        Task<JsonDocument> SaveImageAsync(string imagePath, IFormFile imageFile);
        JsonDocument GetImage(string fullPath);
        JsonDocument GetImagesList(int page, int pageSize0);

        JsonDocument DeleteImage(string fullPath);


        
        //Task<string> UpdateImageAsync(string imagePath, IFormFile imageFile, bool createIfNotExists = true);
    }
}
