//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using System;
//using System.IO;
//using System.Threading.Tasks;

//namespace Guider.API.MVP.Models
//{
//    public class ImageUploadRequest
//    {
//        // Путь в формате "провинция/город/название" (локальная часть URL)
//        public string ImagePath { get; set; }

//        // Файл изображения
//        public IFormFile ImageFile { get; set; }




//    }

using Microsoft.AspNetCore.Http;

namespace Guider.API.MVP.Models
{
    public class ImageUploadRequest
    {
        // Province name (required)
        public string Province { get; set; }

        // City name (optional)
        public string? City { get; set; }

        // Place name (required)
        public string Place { get; set; }

        // Unique image name
        public string ImageName { get; set; }

        // Image file
        public IFormFile ImageFile { get; set; }
    }
}










