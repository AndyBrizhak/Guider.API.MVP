//namespace Guider.API.MVP.Models
//{
//    public class ImageUpdateRequest
//    {
//        public string OldImagePath { get; set; }
//        public string ? NewImagePath { get; set; }
//        public IFormFile ? NewImageFile { get; set; }
//    }
//}

using Microsoft.AspNetCore.Http;

namespace Guider.API.MVP.Models
{
    public class ImageUpdateRequest
    {
        // Old image information
        public string OldProvince { get; set; }
        public string? OldCity { get; set; }
        public string OldPlace { get; set; }
        public string OldImageName { get; set; }

        // New image information (all optional)
        public string? NewProvince { get; set; }
        public string? NewCity { get; set; }
        public string? NewPlace { get; set; }
        public string? NewImageName { get; set; }

        // New image file (optional)
        public IFormFile? NewImageFile { get; set; }
    }
}

