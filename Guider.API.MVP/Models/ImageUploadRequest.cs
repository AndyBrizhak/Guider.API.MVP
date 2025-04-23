using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Guider.API.MVP.Models
{
    public class ImageUploadRequest
    {
        // Путь в формате "провинция/город/название" (локальная часть URL)
        public string ImagePath { get; set; }

        // Файл изображения
        public IFormFile ImageFile { get; set; }
    }
}
