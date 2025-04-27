namespace Guider.API.MVP.Models
{
    public class ImageUpdateRequest
    {
        public string OldImagePath { get; set; }
        public string ? NewImagePath { get; set; }
        public IFormFile ? NewImageFile { get; set; }
    }
}

