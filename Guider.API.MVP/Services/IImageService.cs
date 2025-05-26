
//using System.Text.Json;

//namespace Guider.API.MVP.Services
//{
//    public interface IImageService
//    {
//        Task<JsonDocument> SaveImageAsync(string province, string? city, string place, string imageName, IFormFile imageFile);

//        JsonDocument GetImage(string province, string? city, string place, string imageName);

//        JsonDocument GetImagesList(int page, int pageSize);

//        JsonDocument DeleteImage(string province, string? city, string place, string imageName);

//        Task<JsonDocument> UpdateImageAsync(
//            string oldProvince, string? oldCity, string oldPlace, string oldImageName,
//            string? newProvince = null, string? newCity = null, string? newPlace = null,
//            string? newImageName = null, IFormFile? newImageFile = null);
//    }
//}

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

        JsonDocument DeleteImage(string province, string? city, string place, string imageName);

        Task<JsonDocument> DeleteImageByIdAsync(string id);

        Task<JsonDocument> UpdateImageAsync(
            string oldProvince, string? oldCity, string oldPlace, string oldImageName,
            string? newProvince = null, string? newCity = null, string? newPlace = null,
            string? newImageName = null, IFormFile? newImageFile = null);
    }
}
