using System.ComponentModel.DataAnnotations;

namespace Guider.API.MVP.Models
{
    public class ImageDeleteRequest
    {
        [Required]
        public string Province { get; set; }

        public string? City { get; set; }

        [Required]
        public string Place { get; set; }

        [Required]
        public string ImageName { get; set; }
    }
}
