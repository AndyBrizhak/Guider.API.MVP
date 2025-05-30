
using System.ComponentModel.DataAnnotations;

namespace Guider.API.MVP.Models
{
    public class ImageDeleteRequest
    {
        // Идентификатор изображения для удаления (обязательный)
        [Required(ErrorMessage = "ID изображения является обязательным")]
        public string Id { get; set; }
    }
}
