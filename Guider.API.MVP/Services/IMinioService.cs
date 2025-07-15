using Microsoft.AspNetCore.Http;
namespace Guider.API.MVP.Services
{
    

    /// <summary>
    /// Интерфейс для работы с MinIO S3-хранилищем
    /// </summary>
    public interface IMinioService
    {
        /// <summary>
        /// Загружает файл в MinIO хранилище
        /// </summary>
        /// <param name="file">Файл для загрузки</param>
        /// <param name="fileName">Имя файла без расширения</param>
        /// <param name="fileExtension">Расширение файла</param>
        /// <returns>URL загруженного файла или сообщение об ошибке</returns>
        Task<string> UploadFileAsync(IFormFile file, string fileName, string fileExtension);

        /// <summary>
        /// Удаляет файл из MinIO хранилища по URL
        /// </summary>
        /// <param name="fileUrl">URL файла для удаления</param>
        /// <returns>Результат удаления с информацией об успехе операции</returns>
        Task<MinioService.DeleteFileResult> DeleteFileAsync(string fileUrl);

        /// <summary>
        /// Проверяет существование файла в хранилище
        /// </summary>
        /// <param name="fileName">Имя файла для проверки</param>
        /// <returns>True если файл существует, False если не существует</returns>
        Task<bool> FileExistsAsync(string fileName);

        /// <summary>
        /// Получает URL файла для доступа
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns>URL для доступа к файлу</returns>
        string GetFileUrl(string fileName);
    }
}
