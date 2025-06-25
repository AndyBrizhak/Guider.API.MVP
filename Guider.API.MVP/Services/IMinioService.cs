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
        /// <param name="fileName">Имя файла в хранилище</param>
        /// <param name="fileExtension">Расширение файла</param>
        /// <returns>URL файла в случае успеха, или сообщение об ошибке</returns>
        Task<string> UploadFileAsync(IFormFile file, string fileName, string fileExtension);

        /// <summary>
        /// Удаляет файл из MinIO хранилища по URL
        /// </summary>
        /// <param name="fileUrl">URL файла для удаления</param>
        /// <returns>Сообщение о результате операции</returns>
        Task<string> DeleteFileAsync(string fileUrl);

        /// <summary>
        /// Проверяет существование файла в хранилище
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns>True если файл существует</returns>
        Task<bool> FileExistsAsync(string fileName);

        /// <summary>
        /// Получает URL файла для доступа
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns>URL файла</returns>
        string GetFileUrl(string fileName);
    }
}
