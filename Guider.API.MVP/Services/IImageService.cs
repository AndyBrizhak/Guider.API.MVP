//using System.Text.Json;

//namespace Guider.API.MVP.Services
//{
//    public interface IImageService
//    {
//        Task<JsonDocument> SaveImageAsync(string imagePath, IFormFile imageFile);
//        JsonDocument GetImage(string fullPath);
//        JsonDocument GetImagesList(int page, int pageSize0);

//        JsonDocument DeleteImage(string fullPath);

//        Task<JsonDocument> UpdateImageAsync(string oldImagePath, string newImagePath = null, IFormFile newImageFile = null);

//        //Task<string> UpdateImageAsync(string imagePath, IFormFile imageFile, bool createIfNotExists = true);
//    }
//}

using System.Text.Json;

namespace Guider.API.MVP.Services
{
    public interface IImageService
    {
        Task<JsonDocument> SaveImageAsync(string province, string? city, string place, string imageName, IFormFile imageFile);

        JsonDocument GetImage(string province, string? city, string place, string imageName);

        JsonDocument GetImagesList(int page, int pageSize);

        JsonDocument DeleteImage(string province, string? city, string place, string imageName);

        Task<JsonDocument> UpdateImageAsync(
            string oldProvince, string? oldCity, string oldPlace, string oldImageName,
            string? newProvince = null, string? newCity = null, string? newPlace = null,
            string? newImageName = null, IFormFile? newImageFile = null);
    }
}
