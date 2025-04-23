namespace Guider.API.MVP.Services
{
    public interface IImageService
    {
        Task<string> SaveImageAsync(string imagePath, IFormFile imageFile);
        byte[] GetImage(string fullPath);
        bool DeleteImage(string fullPath);

        // Новый метод для обновления изображения
        Task<string> UpdateImageAsync(string imagePath, IFormFile imageFile, bool createIfNotExists = true);
    }
}
